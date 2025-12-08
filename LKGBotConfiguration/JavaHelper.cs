using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

using Microsoft.Win32;

namespace LKGBotConfiguration
{

    public static class JavaHelper
    {
        private const string JavaDownloadUrl = "https://download.oracle.com/java/17/archive/jdk-17.0.12_windows-x64_bin.exe";
        private const string TempInstallerPath = "C:\\Temp\\jdk-17.0.12_windows-x64_bin.exe";

        /// <summary>
        /// Ensures Java 17 is installed. Reports status through the progress callback.
        /// </summary>
        public static async Task EnsureJava17InstalledAsync(Action<string> progress)
        {
            progress ??= s => { }; // default to do nothing if null

            if (IsJava17Installed())
            {
                progress("(Installed)");
                return;
            }

            progress("(Downloading from Oracle...)");

            Directory.CreateDirectory("C:\\Temp");

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(JavaDownloadUrl))
            {
                response.EnsureSuccessStatusCode();

                using (var fs = new FileStream(TempInstallerPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            progress("(Launching installer...)");

            // Run installer visibly
            var installProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = TempInstallerPath,
                    UseShellExecute = true, // show installer UI
                    Verb = "runas"          // run as admin
                }
            };

            installProcess.Start();
            installProcess.WaitForExit();

            progress("(Installed)");

            // Optionally add Java to system PATH (if you want to be sure)
            AddJavaToSystemPath(progress);
        }

        public static bool IsJava17Installed()
        {
            string javaHome = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\JavaSoft\Java Development Kit\17", "JavaHome", null) ??
                              (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\JavaSoft\Java Development Kit\17", "JavaHome", null);

            if (!string.IsNullOrEmpty(javaHome) && File.Exists(Path.Combine(javaHome, "bin", "java.exe")))
                return true;

            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "java",
                        Arguments = "-version",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                string output = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                return output.Contains("17.");
            }
            catch
            {
                return false;
            }
        }

        private static void AddJavaToSystemPath(Action<string> progress)
        {
            string javaHome = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\JavaSoft\Java Development Kit\17", "JavaHome", null) ??
                              (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\JavaSoft\Java Development Kit\17", "JavaHome", null);

            if (string.IsNullOrEmpty(javaHome))
            {
                progress("JavaHome not found, skipping PATH update.");
                return;
            }

            string javaBin = Path.Combine(javaHome, "bin");
            string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine) ?? "";

            if (!path.Split(';').Contains(javaBin, StringComparer.OrdinalIgnoreCase))
            {
                path += ";" + javaBin;
                Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Machine);
                progress("Java bin added to system PATH.");
            }
            else
            {
                progress("Java bin already in system PATH.");
            }
        }
    }
}
