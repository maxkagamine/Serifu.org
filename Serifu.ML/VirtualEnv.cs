// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

using System.Diagnostics.CodeAnalysis;

namespace Serifu.ML;

/// <summary>
/// Provides information about the Python venv.
/// </summary>
internal static class VirtualEnv
{
    private const string VirtualEnvName = ".python"; // Created in csproj
    private static Dictionary<string, string>? venvConfig;

    /// <summary>
    /// Gets the absolute path to the venv directory, searching from the current directory upward.
    /// </summary>
    [field: MaybeNull]
    public static string VirtualEnvDirectory
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            DirectoryInfo? dir = new(Environment.CurrentDirectory);

            while (dir is not null)
            {
                string venv = Path.Combine(dir.FullName, VirtualEnvName);

                if (Directory.Exists(venv))
                {
                    field = venv;
                    return field;
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
