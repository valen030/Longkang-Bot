using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using YamlDotNet.RepresentationModel;

namespace LKGBotConfiguration
{
    public class YouTubeHelper
    {
        private readonly string _workerFolder;
        private readonly string _serviceBotFileName;
        private readonly string _logFile;
        private readonly string _serverFolder;

        public YouTubeHelper(string workerFolder)
        {
            _workerFolder = workerFolder;
            _serviceBotFileName = "LKGServiceBot.exe";
            _serverFolder = Path.Combine(workerFolder, "Server");
            _logFile = Path.Combine(_serverFolder, "logs", "spring.log");
        }

        public bool CheckYoutubeRefreshToken()
        {
            string yamlPath = Path.Combine(_serverFolder, "application.yml");

            if (!File.Exists(yamlPath))
                throw new Exception("application.yml file not found.");

            using var reader = new StreamReader(yamlPath);
            var yaml = new YamlStream();
            yaml.Load(reader);

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

            // Navigate to plugins -> youtube -> oauth -> refreshToken
            if (mapping.Children.TryGetValue("plugins", out var pluginsNode) &&
                pluginsNode is YamlMappingNode pluginsMapping &&
                pluginsMapping.Children.TryGetValue("youtube", out var youtubeNode) &&
                youtubeNode is YamlMappingNode youtubeMapping &&
                youtubeMapping.Children.TryGetValue("oauth", out var oauthNode) &&
                oauthNode is YamlMappingNode oauthMapping &&
                oauthMapping.Children.TryGetValue("refreshToken", out var refreshTokenNode))
            {
                string token = refreshTokenNode.ToString();
                if (!string.IsNullOrEmpty(token))
                    return true;
            }

            return false;
        }

        public void UpdateYoutubeRefreshToken(string newToken)
        {
            string yamlPath = Path.Combine(_serverFolder, "application.yml");
            if (!File.Exists(yamlPath))
                throw new Exception("application.yml file not found.");

            StopWorkerService(); // stop any process that may lock the file

            // Load YAML safely with read-share
            var yaml = new YamlStream();
            using (var reader = new StreamReader(new FileStream(yamlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                yaml.Load(reader);
            }

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            if (mapping.Children.TryGetValue("plugins", out var pluginsNode) &&
                pluginsNode is YamlMappingNode pluginsMapping &&
                pluginsMapping.Children.TryGetValue("youtube", out var youtubeNode) &&
                youtubeNode is YamlMappingNode youtubeMapping &&
                youtubeMapping.Children.TryGetValue("oauth", out var oauthNode) &&
                oauthNode is YamlMappingNode oauthMapping)
            {
                oauthMapping.Children[new YamlScalarNode("refreshToken")] = new YamlScalarNode(newToken);
            }

            // Write to a temporary file first
            string tempFile = Path.Combine(_serverFolder, "application.tmp.yml");
            using (var writer = new StreamWriter(new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                yaml.Save(writer, assignAnchors: false);
            }

            // Atomically replace original file
            File.Copy(tempFile, yamlPath, overwrite: true);
            File.Delete(tempFile);
        }

        public void ClearLogFolder()
        {
            string logFolder = Path.Combine(_workerFolder, "Server", "logs");

            if (Directory.Exists(logFolder))
            {
                foreach (var file in Directory.GetFiles(logFolder))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore files that can't be deleted (in use, permissions, etc.)
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(logFolder);
            }
        }

        public bool IsWorkerRunning()
        {
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_serviceBotFileName));
            return processes.Length > 0;
        }

        public void StopWorkerService()
        {
            string exeName = Path.GetFileNameWithoutExtension(_serviceBotFileName);
            var processes = Process.GetProcessesByName(exeName);

            foreach (var process in processes)
            {
                try
                {
                    process.Kill();       // stop the process
                    process.WaitForExit(); // optional: wait until fully exited
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to stop WorkerService: {ex.Message}");
                }
            }

            KillWorkerService();
        }

        public void KillWorkerService()
        {
            var port = 2333;
            try
            {
                // Run netstat to find all TCP connections with PIDs
                var psi = new ProcessStartInfo
                {
                    FileName = "netstat.exe",
                    Arguments = "-ano -p tcp",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                // Find all PIDs using the target port
                var pids = lines
                    .Select(line => line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    .Where(parts => parts.Length >= 5 && parts[1].EndsWith($":{port}"))
                    .Select(parts => int.TryParse(parts[4], out int pid) ? pid : -1)
                    .Where(pid => pid > 0)
                    .Distinct()
                    .ToList();

                foreach (var pid in pids)
                {
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        Console.WriteLine($"Killing process {proc.ProcessName} (PID {pid}) using port {port}.");
                        proc.Kill();
                        proc.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to kill PID {pid}: {ex.Message}");
                    }
                }

                if (pids.Count == 0)
                    Console.WriteLine($"No processes found using port {port}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking port {port}: {ex.Message}");
            }
        }

        public void StartWorkerService()
        {
            // Full path to the WorkerService exe
            var exePath = Path.Combine(_workerFolder, _serviceBotFileName);

            if (!File.Exists(exePath))
                throw new FileNotFoundException($"{_serviceBotFileName} not found");

            var process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.WorkingDirectory = _workerFolder;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
        }

        public async Task StartWorkerAndMonitorDeviceCodeAsync(Action<string> onCodeFound)
        {
            if (IsWorkerRunning())
                StopWorkerService();

            ClearLogFolder();

            // Start worker 
            StartWorkerService();

            // Monitor the log continuously
            await MonitorLogForDeviceCodeAsync(onCodeFound);
        }

        public async Task MonitorLogForDeviceCodeAsync(Action<string> onCodeFound)
        {
            while (!File.Exists(_logFile))
                await Task.Delay(500);

            using var fs = new FileStream(_logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);

            // Start at the end so we only read new lines
            reader.BaseStream.Seek(0, SeekOrigin.End);

            Regex regex = new Regex(@"\b(\w{3}-\w{3}-\w{3,4})\b");

            while (true)
            {
                string line = await reader.ReadLineAsync();
                if (line != null)
                {
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        onCodeFound?.Invoke(match.Value);
                        return;
                    }
                }
                else
                {
                    await Task.Delay(500);
                }
            }
        }

        public async Task MonitorLogForOAuthTokenAsync(Action<string> onTokenRetrieved)
        {
            while (!File.Exists(_logFile))
                await Task.Delay(500);

            using var fs = new FileStream(_logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            reader.BaseStream.Seek(0, SeekOrigin.End);

            Regex tokenRegex = new Regex(@"OAUTH INTEGRATION: Token retrieved successfully.*?\((.+?)\)");

            while (true)
            {
                string line = await reader.ReadLineAsync();
                if (line != null)
                {
                    var match = tokenRegex.Match(line);
                    if (match.Success)
                    {
                        onTokenRetrieved?.Invoke(match.Groups[1].Value);
                        return;
                    }
                }
                else
                {
                    await Task.Delay(500);
                }
            }
        }

    }
}
