using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LKGMusicBot
{
    public class Server
    {
        public const string run_Command = "java -jar Lavalink.jar";

        private static int _taskID = 0;
        public static bool IsServerForcedToStop = false;

        /// <summary>
        /// Starts the Lavalink server process and waits until it is ready to accept connections.
        /// </summary>
        /// <remarks>The method checks for the presence of the required Lavalink.jar file in the expected
        /// directory before attempting to start the server. If the file is missing or if cancellation is requested
        /// before the server is ready, the method returns <see langword="false"/>. The method polls the Lavalink API
        /// endpoint until it becomes available, indicating that the server is ready to accept requests.</remarks>
        /// <param name="stoppingToken">A cancellation token that can be used to request cancellation of the startup process before the server is
        /// ready.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the server
        /// started successfully and is ready; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Shuts down the Lavalink server process if it is currently running.
        /// </summary>
        /// <remarks>This method attempts to terminate the Lavalink process associated with the current
        /// task. If the process is not running, no action is taken. Any errors encountered during shutdown are logged
        /// to the console.</remarks>
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