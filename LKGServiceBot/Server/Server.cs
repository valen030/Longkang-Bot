using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LKGMusicBot
{
    public class Server
    {
        public const string run_Command = "java -jar Lavalink.jar";

        private static int _taskID = 0;
        public static bool IsServerForcedToStop = false;

        public static async Task<bool> ServerStartup(CancellationToken stoppingToken)
        {
            var currentDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Server");

            // Check if Lavalink.jar exists
            var jarFilePath = Path.Combine(currentDirectory, "Lavalink.jar");
            if (!File.Exists(jarFilePath))
            {
                Console.WriteLine($"Lavalink.jar not found in {currentDirectory}");
                return false;
            }

            var process = new Process();
            process.StartInfo.WorkingDirectory = currentDirectory;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C " + run_Command;
            }
            else
            {
                // Linux/macOS: just call Java directly
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = "-c \"" + run_Command + "\"";
            }

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine("[Lavalink Error] " + e.Data);
            };

            process.Start();
            process.BeginErrorReadLine();

            Console.WriteLine("Lavalink process started. PID: " + process.Id);
            _taskID = process.Id;

            // Poll the Lavalink API until it's ready
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "youshallnotpass");

            var url = "http://localhost:2333/version";

            while (true) 
            {
                try
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        IsServerForcedToStop = true;
                        Console.WriteLine("Cancellation requested, stopping Lavalink startup.");
                        return false;
                    }

                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Lavalink is ready!");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Status {response.StatusCode}, waiting...");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Lavalink not ready yet: {ex.Message}");
                }

                await Task.Delay(1000);
            }
        }

        public static void ServerShutdown()
        {
            try
            {
                if (_taskID != 0)
                {
                    var process = Process.GetProcessById(_taskID);
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                        process.WaitForExit();
                        Console.WriteLine("Lavalink process terminated.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error terminating Lavalink process: " + ex.Message);
            }
        }
    }
}