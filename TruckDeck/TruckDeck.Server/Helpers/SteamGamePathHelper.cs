using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    public static class SteamGamePathHelper
    {
        public const string Ets2Folder = "Euro Truck Simulator 2";
        public const string AtsFolder = "American Truck Simulator";

        public static bool IsValidGameInstall(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            path = path.Trim().TrimEnd('\\');
            return File.Exists(Path.Combine(path, "base.scs"))
                   && Directory.Exists(Path.Combine(path, "bin"));
        }

        public static string GetRecommendedServerInstallPath()
        {
            var ets2 = DetectGamePath(Ets2Folder);
            if (IsValidGameInstall(ets2))
                return Path.Combine(ets2, "Telemetry Server");
            var ats = DetectGamePath(AtsFolder);
            if (IsValidGameInstall(ats))
                return Path.Combine(ats, "Telemetry Server");
            return null;
        }

        public static string DetectGamePath(string gameFolderName)
        {
            foreach (var library in GetSteamLibraryRoots())
            {
                var candidate = Path.Combine(library, "steamapps", "common", gameFolderName);
                if (IsValidGameInstall(candidate))
                    return candidate;
            }
            return null;
        }

        public static string DetectEts2Path() => DetectGamePath(Ets2Folder);
        public static string DetectAtsPath() => DetectGamePath(AtsFolder);

        public static IList<string> GetSteamLibraryRoots()
        {
            var roots = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddRoot(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;
                path = path.Replace('/', '\\').TrimEnd('\\');
                if (seen.Add(path) && Directory.Exists(path))
                    roots.Add(path);
            }

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    AddRoot(key?.GetValue("SteamPath") as string);
                }
            }
            catch { /* ignore */ }

            foreach (var root in roots.ToList())
            {
                var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf))
                    continue;
                try
                {
                    var text = File.ReadAllText(vdf);
                    foreach (Match m in Regex.Matches(text, "\"path\"\\s+\"([^\"]+)\""))
                        AddRoot(UnescapeVdfPath(m.Groups[1].Value));
                    foreach (Match m in Regex.Matches(text, "\"\\d+\"\\s+\"([^\"]+)\""))
                    {
                        var val = UnescapeVdfPath(m.Groups[1].Value);
                        if (val.Length > 1 && val[1] == ':')
                            AddRoot(val);
                    }
                }
                catch { /* ignore */ }
            }

            for (var drive = 'C'; drive <= 'Z'; drive++)
            {
                AddRoot($@"{drive}:\SteamLibrary");
                AddRoot($@"{drive}:\Program Files (x86)\Steam");
                AddRoot($@"{drive}:\Program Files\Steam");
            }

            return roots;
        }

        static string UnescapeVdfPath(string path) =>
            path.Replace(@"\\", @"\").Replace(@"\\", @"\");

        public static string PluginDirectory(string gamePath) =>
            Path.Combine(gamePath, @"bin\win_x64\plugins");

        public static string DocumentsModFolder(string gameFolderName)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docs, gameFolderName, "mod");
        }
    }
}
