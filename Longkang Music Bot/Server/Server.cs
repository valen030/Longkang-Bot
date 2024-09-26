using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LKGMusicBot
{
    public class Server
    {
        public const string run_Command = "java -jar Lavalink.jar";

        public static bool ServerStartup() 
        {
            var command = run_Command;
            var currentDirectory = Directory.GetCurrentDirectory();

            // Check if Lavalink.jar exists
            var jarFilePath = Path.Combine(currentDirectory, "Lavalink.jar");
            if (!File.Exists(jarFilePath))
            {
                Console.WriteLine($"Lavalink.jar not found in {currentDirectory}");
                return false;
            }

            // Create a new process to execute the command
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C " + command;
            process.StartInfo.WorkingDirectory = currentDirectory;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            //process.StartInfo.CreateNoWindow = true; // Hide the CMD window

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine("Error: " + e.Data);
                }
            };

            process.Start();

            // Begin async reading to capture output
            process.BeginErrorReadLine();

            Console.WriteLine("Lavalink server started. Process ID: " + process.Id);
            return true;
        } 
    }
}
