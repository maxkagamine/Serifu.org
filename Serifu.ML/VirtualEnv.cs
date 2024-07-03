﻿namespace Serifu.ML;

/// <summary>
/// Provides information about the Python venv.
/// </summary>
internal static class VirtualEnv
{
    private const string VirtualEnvName = ".python"; // Created in csproj

    private static string? venvDirectory;
    private static Dictionary<string, string>? venvConfig;

    /// <summary>
    /// Gets the absolute path to the venv directory, searching from the current directory upward.
    /// </summary>
    public static string VirtualEnvDirectory
    {
        get
        {
            if (venvDirectory is not null)
            {
                return venvDirectory;
            }

            DirectoryInfo? dir = new(Environment.CurrentDirectory);

            while (dir is not null)
            {
                string venv = Path.Combine(dir.FullName, VirtualEnvName);

                if (Directory.Exists(venv))
                {
                    venvDirectory = venv;
                    return venvDirectory;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException($"No {VirtualEnvName} directory exists in the folder hierarchy starting from \"{Environment.CurrentDirectory}\".");
        }
    }

    /// <summary>
    /// Gets the absolute path to the python installation used by the venv.
    /// </summary>
    public static string PythonHome
    {
        get
        {
            string home = GetVirtualEnvConfigValue("home");

            if (!Directory.Exists(home))
            {
                throw new DirectoryNotFoundException($"Virtual env config refers to a python installation at \"{home}\" which no longer exists.");
            }

            return home;
        }
    }

    /// <summary>
    /// Gets the absolute path to the python DLL.
    /// </summary>
    public static string PythonDll
    {
        get
        {
            // Hardcoding the Windows dll, but in theory we could run this in Linux, too.
            var version = PythonVersion;
            return Path.Combine(PythonHome, $"python{version.Major}{version.Minor}.dll");
        }
    }

    /// <summary>
    /// Gets the python version used by the venv.
    /// </summary>
    public static Version PythonVersion => Version.Parse(GetVirtualEnvConfigValue("version"));

    private static string GetVirtualEnvConfigValue(string key)
    {
        if (venvConfig is null)
        {
            using var configFile = File.OpenRead(Path.Combine(VirtualEnvDirectory, "pyvenv.cfg"));
            using var reader = new StreamReader(configFile);

            venvConfig = [];

            while (reader.ReadLine() is string line)
            {
                int equalsIndex = line.IndexOf('=');
                if (equalsIndex < 0)
                {
                    continue;
                }

                venvConfig.Add(line[..equalsIndex].Trim(), line[(equalsIndex + 1)..].Trim());
            }
        }

        if (!venvConfig.TryGetValue(key, out string? value))
        {
            throw new KeyNotFoundException($"Virtual env config is missing \"{key}\".");
        }

        return value;
    }
}
