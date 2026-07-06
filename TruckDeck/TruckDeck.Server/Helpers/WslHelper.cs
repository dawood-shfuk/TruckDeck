using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    public static class WslHelper
    {
        public const string DefaultDistroName = "TruckDeckUbuntu";
        public const string WslMapToolsRoot = "~/.truckdeck/map-tools/maps";

        static readonly string[] PreferredDistroNames =
        {
            DefaultDistroName,
            "Ubuntu-24.04",
            "Ubuntu-22.04",
            "Ubuntu",
            "Debian"
        };

        static string _cachedDistro;
        static DateTime _cachedDistroUtc = DateTime.MinValue;

        public static void InvalidateDistroCache()
        {
            _cachedDistro = null;
            _cachedDistroUtc = DateTime.MinValue;
        }

        public static bool IsWslAvailable()
        {
            try
            {
                string output, error;
                return RunWsl(out output, out error, "--status", 10) == 0;
            }
            catch
            {
                return false;
            }
        }

        public static string GetDefaultDistro()
        {
            if (_cachedDistro != null && (DateTime.UtcNow - _cachedDistroUtc).TotalSeconds < 10)
                return _cachedDistro;

            string found = null;
            foreach (var preferred in PreferredDistroNames)
            {
                if (IsDistroRegistered(preferred))
                {
                    found = preferred;
                    break;
                }
            }

            if (found == null && IsWslAvailable())
            {
                var distros = ListDistros();
                foreach (var preferred in PreferredDistroNames)
                {
                    var match = distros.FirstOrDefault(d =>
                        string.Equals(d.Name, preferred, StringComparison.OrdinalIgnoreCase));
                    if (match != null && match.State != "Uninstalling")
                    {
                        found = match.Name;
                        break;
                    }
                }

                if (found == null && distros.Count > 0)
                    found = distros.FirstOrDefault(d => d.State == "Running" || d.State == "Stopped")?.Name;
            }

            _cachedDistro = found;
            _cachedDistroUtc = DateTime.UtcNow;
            return found;
        }

        public static bool IsDistroRegistered(string distro)
        {
            if (string.IsNullOrWhiteSpace(distro))
                return false;

            try
            {
                string output, error;
                // Probe without UTF-16 list parsing — exit code is enough.
                var code = ProcessHelper.RunAndWait(out output, out error,
                    "wsl", $"-d {QuoteArg(distro)} -e true", 15);
                return code == 0;
            }
            catch
            {
                return false;
            }
        }

        static List<DistroInfo> ListDistros()
        {
            try
            {
                string output, error;
                RunWsl(out output, out error, "-l -v", 10);
                return ParseDistroList(output + "\n" + error);
            }
            catch
            {
                return new List<DistroInfo>();
            }
        }

        static int RunWsl(out string output, out string error, string arguments, int timeoutSeconds = 10)
        {
            var code = ProcessHelper.RunAndWait(out output, out error, "wsl", arguments, timeoutSeconds, Encoding.Unicode);
            output = NormalizeWslOutput(output);
            error = NormalizeWslOutput(error);
            return code;
        }

        static string NormalizeWslOutput(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            text = text.Replace("\0", "");
            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var normalized = new StringBuilder();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                normalized.AppendLine(CollapseSpacedWideChars(line.Trim()));
            }
            return normalized.ToString();
        }

        /// <summary>wsl.exe sometimes prints UTF-16 list output as "T r u c k D e c k".</summary>
        static string CollapseSpacedWideChars(string line)
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
                return line;

            var singleCharParts = parts.Count(p => p.Length == 1);
            if (singleCharParts < parts.Length / 2)
                return line;

            var sb = new StringBuilder();
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 1)
                {
                    var run = new StringBuilder();
                    while (i < parts.Length && parts[i].Length == 1)
                        run.Append(parts[i++]);
                    i--;
                    if (sb.Length > 0)
                        sb.Append(' ');
                    sb.Append(run);
                }
                else
                {
                    if (sb.Length > 0)
                        sb.Append(' ');
                    sb.Append(parts[i]);
                }
            }
            return sb.ToString();
        }

        public static object ProbeWslCommand(string distro, string command, string versionArgs = "--version")
        {
            if (string.IsNullOrWhiteSpace(distro))
                return new { available = false, version = (string)null };

            try
            {
                var shellCmd = $"{command} {versionArgs} 2>&1";
                string output, error;
                var args = $"-d {QuoteArg(distro)} -e bash -lc {QuoteArg(shellCmd)}";
                var code = RunWsl(out output, out error, args, 20);
                var version = (output + error).Trim().Split('\n').FirstOrDefault()?.Trim();
                return new { available = code == 0 && !string.IsNullOrWhiteSpace(version), version };
            }
            catch
            {
                return new { available = false, version = (string)null };
            }
        }

        public static bool WslPathExists(string distro, string wslPath)
        {
            if (string.IsNullOrWhiteSpace(distro) || string.IsNullOrWhiteSpace(wslPath))
                return false;

            try
            {
                var shellCmd = $"test -e {QuoteBash(wslPath)}";
                string output, error;
                var args = $"-d {QuoteArg(distro)} -e bash -lc {QuoteArg(shellCmd)}";
                return RunWsl(out output, out error, args, 15) == 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsWslMapToolsInstalled(string distro)
        {
            if (string.IsNullOrWhiteSpace(distro))
                return false;

            try
            {
                string output, error;
                // Direct test avoids bash -lc "$HOME" — Windows argument parsing can break that.
                var rootArgs = $"-d {QuoteArg(distro)} -e test -d /root/.truckdeck/map-tools/maps/node_modules";
                if (RunWsl(out output, out error, rootArgs, 15) == 0)
                    return true;

                var userArgs = $"-d {QuoteArg(distro)} -e bash -lc 'test -d ~/.truckdeck/map-tools/maps/node_modules'";
                return RunWsl(out output, out error, userArgs, 15) == 0;
            }
            catch
            {
                return false;
            }
        }

        public static string ToWslPath(string windowsPath)
        {
            if (string.IsNullOrWhiteSpace(windowsPath))
                return windowsPath;

            if (windowsPath.StartsWith("/mnt/", StringComparison.Ordinal))
                return windowsPath.Replace('\\', '/').TrimEnd('/');

            var full = windowsPath;
            try
            {
                if (Directory.Exists(windowsPath) || File.Exists(windowsPath))
                    full = Path.GetFullPath(windowsPath);
            }
            catch
            {
                full = windowsPath.Trim().TrimEnd('\\', '/');
            }

            var match = Regex.Match(full, @"^([A-Za-z]):[\\/](.*)$");
            if (!match.Success)
                throw new ArgumentException("Not a drive path: " + windowsPath);

            var drive = match.Groups[1].Value.ToLowerInvariant();
            var rest = match.Groups[2].Value.Replace('\\', '/').TrimEnd('/');
            return string.IsNullOrEmpty(rest) ? $"/mnt/{drive}" : $"/mnt/{drive}/{rest}";
        }

        public static string ToWindowsPath(string wslPath)
        {
            if (string.IsNullOrWhiteSpace(wslPath))
                return wslPath;

            var match = Regex.Match(wslPath.Replace('\\', '/'), @"^/mnt/([a-zA-Z])(?:/(.*))?$");
            if (!match.Success)
                return wslPath;

            var drive = char.ToUpperInvariant(match.Groups[1].Value[0]);
            var rest = match.Groups[2].Value;
            return string.IsNullOrEmpty(rest)
                ? $"{drive}:\\"
                : $"{drive}:\\{rest.Replace('/', '\\')}";
        }

        public static string QuoteArg(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        static string QuoteBash(string value)
        {
            return "'" + value.Replace("'", "'\\''") + "'";
        }

        static List<DistroInfo> ParseDistroList(string output)
        {
            var list = new List<DistroInfo>();
            foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase) || line.StartsWith("-"))
                    continue;

                var running = line.StartsWith("*");
                if (running)
                    line = line.Substring(1).TrimStart();

                var parts = Regex.Split(line, @"\s{2,}").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
                if (parts.Length < 2)
                {
                    parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;
                }

                list.Add(new DistroInfo
                {
                    Name = parts[0].Trim(),
                    State = parts[1].Trim(),
                    IsDefault = running
                });
            }
            return list;
        }

        sealed class DistroInfo
        {
            public string Name { get; set; }
            public string State { get; set; }
            public bool IsDefault { get; set; }
        }
    }
}
