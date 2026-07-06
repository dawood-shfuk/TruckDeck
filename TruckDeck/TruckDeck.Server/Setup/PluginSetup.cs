using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Setup
{
    public class PluginSetup : ISetup
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        const string Ets2 = "ETS2";
        const string Ats = "ATS";
        const string PrimaryPluginDllName = "trucksim-gps-telemetry.dll";
        const string AlternatePluginDllName = "scs-telemetry.dll";
        const string LegacyPluginDllName = "ets2-telemetry-server.dll";

        SetupStatus _status;

        public PluginSetup()
        {
            try
            {
                Log.Info("Checking TruckSim GPS / RenCloud telemetry plugin DLL files...");

                var ets2State = new GameState(Ets2, Settings.Instance.Ets2GamePath);
                var atsState = new GameState(Ats, Settings.Instance.AtsGamePath);

                _status = IsPluginStepComplete(ets2State, atsState)
                    ? SetupStatus.Installed
                    : SetupStatus.Uninstalled;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                _status = SetupStatus.Failed;
            }
        }

        public SetupStatus Status => _status;

        static bool IsPluginStepComplete(GameState ets2, GameState ats)
        {
            if (!IsGameSlotComplete(ets2) || !IsGameSlotComplete(ats))
                return false;
            var ets2Active = ets2.HasActiveGame();
            var atsActive = ats.HasActiveGame();
            return ets2Active || atsActive;
        }

        static bool IsGameSlotComplete(GameState state)
        {
            if (state.IsSkipped())
                return true;
            return state.IsPathValid() && state.IsPluginValid();
        }

        public SetupStatus Install(IWin32Window owner)
        {
            if (_status == SetupStatus.Installed)
                return _status;

            try
            {
                var ets2State = new GameState(Ets2, Settings.Instance.Ets2GamePath);
                var atsState = new GameState(Ats, Settings.Instance.AtsGamePath);

                if (!ets2State.IsPluginValid())
                {
                    if (ets2State.IsSkipped())
                    {
                        // keep N/A
                    }
                    else if (!ets2State.IsPathValid())
                    {
                        if (string.IsNullOrEmpty(ets2State.GamePath))
                            ets2State.DetectPath();
                        if (!ets2State.IsPathValid() && !ets2State.IsSkipped())
                            ets2State.ResolvePath(owner);
                    }

                    if (ets2State.HasActiveGame())
                        ets2State.InstallPlugin();
                    else if (string.IsNullOrEmpty(ets2State.GamePath))
                        ets2State.GamePath = "N/A";
                }

                if (!atsState.IsPluginValid())
                {
                    if (atsState.IsSkipped())
                    {
                        // keep N/A
                    }
                    else if (!atsState.IsPathValid())
                    {
                        if (string.IsNullOrEmpty(atsState.GamePath))
                            atsState.DetectPath();
                        if (!atsState.IsPathValid() && !atsState.IsSkipped())
                            atsState.ResolvePath(owner);
                    }

                    if (atsState.HasActiveGame())
                        atsState.InstallPlugin();
                    else if (string.IsNullOrEmpty(atsState.GamePath))
                        atsState.GamePath = "N/A";
                }

                Settings.Instance.Ets2GamePath = ets2State.GamePath ?? "N/A";
                Settings.Instance.AtsGamePath = atsState.GamePath ?? "N/A";
                Settings.Instance.Save();

                _status = IsPluginStepComplete(ets2State, atsState)
                    ? SetupStatus.Installed
                    : SetupStatus.Failed;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                _status = SetupStatus.Failed;
                throw;
            }

            return _status;
        }

        public SetupStatus Uninstall(IWin32Window owner)
        {
            if (_status == SetupStatus.Uninstalled)
                return _status;

            try
            {
                var ets2State = new GameState(Ets2, Settings.Instance.Ets2GamePath);
                var atsState = new GameState(Ats, Settings.Instance.AtsGamePath);
                ets2State.UninstallPlugin();
                atsState.UninstallPlugin();
                _status = SetupStatus.Uninstalled;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                _status = SetupStatus.Failed;
            }
            return _status;
        }

        class GameState
        {
            const string InstallationSkippedPath = "N/A";
            readonly string _gameName;

            public GameState(string gameName, string gamePath)
            {
                _gameName = gameName;
                GamePath = gamePath;
            }

            string GameDirectoryName =>
                _gameName == Ats ? "American Truck Simulator" : "Euro Truck Simulator 2";

            public string GamePath { get; set; }

            public bool IsPathValid()
            {
                if (GamePath == InstallationSkippedPath)
                    return true;
                if (string.IsNullOrEmpty(GamePath))
                    return false;
                var validated = File.Exists(Path.Combine(GamePath, "base.scs"))
                                && Directory.Exists(Path.Combine(GamePath, "bin"));
                Log.InfoFormat("Validating {2} path: '{0}' ... {1}", GamePath, validated ? "OK" : "Fail", _gameName);
                return validated;
            }

            public bool IsPluginValid()
            {
                if (GamePath == InstallationSkippedPath)
                    return true;
                if (!IsPathValid())
                    return false;
                return File.Exists(GetPluginDllPath(GamePath));
            }

            public void InstallPlugin()
            {
                if (GamePath == InstallationSkippedPath)
                    return;

                var dest = GetPluginDllPath(GamePath);
                var source = LocalBundledPluginPath(PrimaryPluginDllName);
                if (!File.Exists(source))
                    throw new FileNotFoundException(
                        "Bundled trucksim-gps-telemetry.dll not found. Run build\\build_plugins.ps1 first.", source);

                RemoveConflictingPlugins(GetPluginDirectory(GamePath));
                Log.InfoFormat("Copying TruckSim GPS plugin to {0} for {1}", dest, _gameName);
                File.Copy(source, dest, true);
            }

            public void UninstallPlugin()
            {
                if (GamePath == InstallationSkippedPath)
                    return;

                Log.InfoFormat("Backing up plugin DLL for {0}...", _gameName);
                BackupIfExists(GetPluginDllPath(GamePath));
                BackupIfExists(Path.Combine(GetPluginDirectory(GamePath), AlternatePluginDllName));
                BackupIfExists(Path.Combine(GetPluginDirectory(GamePath), LegacyPluginDllName));
            }

            static void RemoveConflictingPlugins(string pluginDir)
            {
                foreach (var name in new[] { LegacyPluginDllName, AlternatePluginDllName })
                {
                    var path = Path.Combine(pluginDir, name);
                    if (!File.Exists(path))
                        continue;
                    try { File.Delete(path); }
                    catch { BackupIfExists(path); }
                }
            }

            static void BackupIfExists(string path)
            {
                if (!File.Exists(path))
                    return;
                var bak = Path.ChangeExtension(path, ".bak");
                if (File.Exists(bak))
                    File.Delete(bak);
                File.Move(path, bak);
            }

            static string LocalBundledPluginPath(string dllName) =>
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Plugins\win_x64\plugins", dllName);

            static string GetPluginDirectory(string gamePath)
            {
                var path = Path.Combine(gamePath, @"bin\win_x64\plugins");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }

            static string GetPluginDllPath(string gamePath) =>
                Path.Combine(GetPluginDirectory(gamePath), PrimaryPluginDllName);

            public void DetectPath()
            {
                GamePath = _gameName == Ats
                    ? SteamGamePathHelper.DetectAtsPath()
                    : SteamGamePathHelper.DetectEts2Path();
            }

            public bool IsSkipped() => GamePath == InstallationSkippedPath;

            public bool HasActiveGame() => !IsSkipped() && IsPathValid();

            public void ResolvePath(IWin32Window owner)
            {
                DetectPath();
                if (IsPathValid() || IsSkipped())
                    return;

                if (owner == null)
                {
                    Log.WarnFormat(
                        "Could not detect {0} path during silent install; skipping plugin for this game.",
                        _gameName);
                    GamePath = InstallationSkippedPath;
                    return;
                }

                BrowserForValidPath(owner);
            }

            public void BrowserForValidPath(IWin32Window owner)
            {
                while (!IsPathValid())
                {
                    var result = MessageBox.Show(owner,
                        @"Could not detect " + _gameName + @" game path. " +
                        @"If you do not have this game installed press [Cancel] to skip, " +
                        @"otherwise press [OK] to select path manually." + Environment.NewLine + Environment.NewLine +
                        @"For example:" + Environment.NewLine + @"D:\STEAM\SteamApps\common\" + GameDirectoryName,
                        @"TruckDeck Setup", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                    if (result == DialogResult.Cancel)
                    {
                        GamePath = InstallationSkippedPath;
                        return;
                    }
                    var browser = new FolderBrowserDialog
                    {
                        Description = @"Select " + _gameName + @" game path",
                        ShowNewFolderButton = false
                    };
                    result = browser.ShowDialog(owner);
                    if (result == DialogResult.Cancel)
                        Environment.Exit(1);
                    GamePath = browser.SelectedPath;
                }
            }
        }
    }
}
