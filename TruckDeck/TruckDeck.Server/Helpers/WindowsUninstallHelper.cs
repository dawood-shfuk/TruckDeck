using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    static class WindowsUninstallHelper
    {
        const string InnoAppId = @"A7B3C9D1-4E5F-6789-ABCD-EF0123456789";
        const string DisplayName = "TruckDeck";

        public static bool TryLaunchUninstaller(out string errorMessage)
        {
            errorMessage = null;
            var command = FindUninstallCommand();
            if (string.IsNullOrWhiteSpace(command))
            {
                errorMessage = "Windows uninstaller entry was not found. Use a portable cleanup instead.";
                return false;
            }

            if (!TryParseCommandLine(command, out var fileName, out var arguments))
            {
                errorMessage = "Could not parse the Windows uninstall command.";
                return false;
            }

            if (!File.Exists(fileName))
            {
                errorMessage = "Windows uninstaller file is missing: " + fileName;
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(fileName) ?? ""
                });
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        static string FindUninstallCommand()
        {
            foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                foreach (var subKeyName in new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                })
                {
                    using (var uninstallKey = root.OpenSubKey(subKeyName))
                    {
                        if (uninstallKey == null)
                            continue;

                        var direct = uninstallKey.OpenSubKey(InnoAppId + "_is1");
                        var fromDirect = ReadUninstallString(direct);
                        if (!string.IsNullOrWhiteSpace(fromDirect))
                            return fromDirect;

                        foreach (var childName in uninstallKey.GetSubKeyNames())
                        {
                            using (var child = uninstallKey.OpenSubKey(childName))
                            {
                                var displayName = child?.GetValue("DisplayName") as string;
                                if (!string.Equals(displayName, DisplayName, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                var uninstall = ReadUninstallString(child);
                                if (!string.IsNullOrWhiteSpace(uninstall))
                                    return uninstall;
                            }
                        }
                    }
                }
            }

            return null;
        }

        static string ReadUninstallString(RegistryKey key)
        {
            return key?.GetValue("UninstallString") as string
                   ?? key?.GetValue("QuietUninstallString") as string;
        }

        static bool TryParseCommandLine(string command, out string fileName, out string arguments)
        {
            fileName = null;
            arguments = null;
            if (string.IsNullOrWhiteSpace(command))
                return false;

            command = command.Trim();
            if (command.StartsWith("\"", StringComparison.Ordinal))
            {
                var end = command.IndexOf('"', 1);
                if (end < 0)
                    return false;
                fileName = command.Substring(1, end - 1);
                arguments = end + 1 < command.Length ? command.Substring(end + 1).Trim() : "";
                return true;
            }

            var space = command.IndexOf(' ');
            if (space < 0)
            {
                fileName = command;
                arguments = "";
                return true;
            }

            fileName = command.Substring(0, space);
            arguments = command.Substring(space + 1).Trim();
            return true;
        }
    }
}
