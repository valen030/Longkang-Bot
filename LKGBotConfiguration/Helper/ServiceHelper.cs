using System.Diagnostics;
using System.ServiceProcess;

namespace LKGBotConfiguration.Helper
{
    public class ServiceHelper
    {
        public static void StartWorkerService(string workerFolder)
        {
            // Full path to the WorkerService exe
            var exePath = Path.Combine(workerFolder, Const.ServiceBotFileName);

            if (!File.Exists(exePath))
                throw new FileNotFoundException($"{Const.ServiceBotFileName} not found");

            var process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.WorkingDirectory = workerFolder;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
        }

        public static bool IsWorkerRunning()
        {
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Const.ServiceBotFileName));
            return processes.Length > 0;
        }

        public static void StopWorkerService()
        {
            string serviceName = Const.ServiceName; // Your Worker Service name
            string exeName = Path.GetFileNameWithoutExtension(Const.ServiceBotFileName);
            int port = Const.ServicePort;

            // Try to stop service gracefully
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    Console.WriteLine($"Stopping service {serviceName}...");
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                    Console.WriteLine($"Service {serviceName} stopped gracefully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to stop service gracefully: {ex.Message}");
            }

            // kill any leftover processes by exe name
            try
            {
                var processes = Process.GetProcessesByName(exeName);
                foreach (var process in processes)
                {
                    try
                    {
                        Console.WriteLine($"Killing leftover process {process.ProcessName} (PID {process.Id})...");
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to kill process {process.ProcessName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error killing leftover processes: {ex.Message}");
            }

            // kill processes using the service port
            KillProcessesUsingPort(port);
        }

        private static void KillProcessesUsingPort(int port)
        {
            try
            {
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

    }
}
