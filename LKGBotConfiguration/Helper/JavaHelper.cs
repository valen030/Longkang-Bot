using System.Diagnostics;

using Microsoft.Win32;

public static class JavaHelper
{
    private const string JavaDownloadUrl = "https://download.oracle.com/java/17/archive/jdk-17.0.12_windows-x64_bin.exe";
    private const string TempInstallerPath = "C:\\Temp\\jdk-17.0.12_windows-x64_bin.exe";

    // Registry paths where Java 17 may be registered (varies by vendor and JDK version)
    private static readonly string[] RegistryPaths =
    [
        // Oracle JDK 9+ (modern path — used on Windows 11)
        @"HKEY_LOCAL_MACHINE\SOFTWARE\JavaSoft\JDK\17",
            // Oracle JDK 8 and earlier (legacy path — used on Windows 10)
            @"HKEY_LOCAL_MACHINE\SOFTWARE\JavaSoft\Java Development Kit\17",
            // WOW6432Node variants (32-bit registry view on 64-bit OS)
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\JavaSoft\JDK\17",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\JavaSoft\Java Development Kit\17",
            // Eclipse Adoptium / Temurin
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Eclipse Adoptium\JDK\17\hotspot\MSI",
            // Microsoft Build of OpenJDK
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\JDK\17\hotspot\MSI",
        ];

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
        // Check JAVA_HOME environment variable first
        string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome) && File.Exists(Path.Combine(javaHome, "bin", "java.exe")))
        {
            if (CheckJavaVersionAtPath(Path.Combine(javaHome, "bin", "java.exe"), "17."))
                return true;
        }

        // Check all known registry locations
        javaHome = FindJavaHomeFromRegistry();
        if (!string.IsNullOrEmpty(javaHome) && File.Exists(Path.Combine(javaHome, "bin", "java.exe")))
            return true;

        // Check common installation directories (fallback for portable installs)
        string[] commonPaths =
        [
            @"C:\Program Files\Java\jdk-17",
                @"C:\Program Files\Eclipse Adoptium\jdk-17",
                @"C:\Program Files\Microsoft\jdk-17",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java", "jdk-17"),
            ];

        foreach (var path in commonPaths)
        {
            // Match directories that start with the base path (e.g., jdk-17.0.12)
            string parent = Path.GetDirectoryName(path)!;
            string prefix = Path.GetFileName(path);

            if (Directory.Exists(parent))
            {
                foreach (var dir in Directory.EnumerateDirectories(parent, $"{prefix}*"))
                {
                    string javaExe = Path.Combine(dir, "bin", "java.exe");
                    if (File.Exists(javaExe))
                        return true;
                }
            }
        }

        // Last resort: try running java -version from PATH
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

    /// <summary>
    /// Searches all known registry paths for a valid JavaHome value.
    /// </summary>
    private static string FindJavaHomeFromRegistry()
    {
        foreach (var regPath in RegistryPaths)
        {
            // Try "JavaHome" (Oracle) and "Path" (Adoptium/Microsoft) value names
            string value = (string)Registry.GetValue(regPath, "JavaHome", null)
                        ?? (string)Registry.GetValue(regPath, "Path", null);

            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return null;
    }

    private static void AddJavaToSystemPath(Action<string> progress)
    {
        string javaHome = FindJavaHomeFromRegistry();

        // Also check JAVA_HOME as fallback
        if (string.IsNullOrEmpty(javaHome))
            javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");

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

            // Also update the current process PATH so subsequent checks work
            string currentPath = Environment.GetEnvironmentVariable("Path") ?? "";
            if (!currentPath.Split(';').Contains(javaBin, StringComparer.OrdinalIgnoreCase))
                Environment.SetEnvironmentVariable("Path", currentPath + ";" + javaBin);

            progress("Java bin added to system PATH.");
        }
        else
        {
            progress("Java bin already in system PATH.");
        }
    }

    /// <summary>
    /// Runs a specific java.exe and checks if its version output contains the expected version string.
    /// </summary>
    private static bool CheckJavaVersionAtPath(string javaExePath, string expectedVersion)
    {
        try
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = javaExePath,
                    Arguments = "-version",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            string output = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return output.Contains(expectedVersion);
        }
        catch
        {
            return false;
        }
    }
}