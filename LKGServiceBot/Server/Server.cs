using System.Diagnostics;

namespace LKGMusicBot
{
    public class Server
    {
        public const string run_Command = "java -jar Lavalink.jar";

        private static int _taskID = 0;

        public static async Task<bool> ServerStartup()
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
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C " + run_Command;
            process.StartInfo.WorkingDirectory = currentDirectory;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

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
            for (int i = 0; i < 60; i++) // up to 30s
            {
                try
                {
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

            Console.WriteLine("Lavalink did not respond in time.");
            return false;
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
                        process.Kill();
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