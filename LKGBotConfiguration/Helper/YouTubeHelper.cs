using System.Text.RegularExpressions;

using YamlDotNet.RepresentationModel;

namespace LKGBotConfiguration.Helper
{
    public class YouTubeHelper
    {
        private readonly string _workerFolder;
        private readonly string _logFile;
        private readonly string _serverFolder;

        public YouTubeHelper(string workerFolder)
        {
            _workerFolder = workerFolder;
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

            ServiceHelper.StopWorkerService(); // stop any process that may lock the file

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
                    catch { }
                }
            }
            else
                Directory.CreateDirectory(logFolder);
        }

        public async Task StartWorkerAndMonitorDeviceCodeAsync(Action<string> onCodeFound)
        {
            if (ServiceHelper.IsWorkerRunning())
                ServiceHelper.StopWorkerService();

            ClearLogFolder();

            // Start worker 
            ServiceHelper.StartWorkerService(_workerFolder);

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
