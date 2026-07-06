using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;
using Funbit.Ets.Telemetry.Server.Services;

namespace Funbit.Ets.Telemetry.Server
{
    public partial class MapGeneratorForm : Form
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly MapGenerationService _maps = MapGenerationService.Instance;
        readonly Timer _pollTimer = new Timer();
        readonly Dictionary<string, string> _installedToolLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static readonly Regex ToolMarkerRegex = new Regex(@"TRUCKDECK_TOOL:(\w+):(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        string _activeJobId;

        public MapGeneratorForm()
        {
            InitializeComponent();
            ApplicationIconHelper.Apply(this);
            TruckDeckTheme.Apply(this);
            header.TitleText = "MAP GENERATOR";
            header.TaglineText = "WSL · tippecanoe · PMTiles";
            header.VersionText = AssemblyHelper.Version;

            LoadSettings();
            RefreshStatus();

            Shown += (_, __) => RefreshStatus();

            _pollTimer.Interval = 1500;
            _pollTimer.Tick += (_, __) => PollJob();
            _pollTimer.Start();
        }

        void LoadSettings()
        {
            var wslPath = Settings.Instance.WslInstallPath;
            wslInstallPathTextBox.Text = string.IsNullOrWhiteSpace(wslPath) ? GuessWslInstallPath() : wslPath;
            ets2PathTextBox.Text = Settings.Instance.Ets2GamePath ?? "";
            atsPathTextBox.Text = Settings.Instance.AtsGamePath ?? "";
        }

        static string GuessWslInstallPath()
        {
            foreach (var drive in DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                var root = drive.Name.TrimEnd('\\');
                if (!string.Equals(root, "C:", StringComparison.OrdinalIgnoreCase))
                    return Path.Combine(root, "WSL");
            }

            return @"D:\WSL";
        }

        void RefreshStatus()
        {
            RestoreInstallStateFromLogIfNeeded();

            var wslAvailable = WslHelper.IsWslAvailable();
            var distro = WslHelper.GetDefaultDistro();
            var hasDistro = !string.IsNullOrEmpty(distro);
            var mapToolsOk = false;

            wslCard.Visible = !hasDistro;

            if (!wslAvailable)
                SetStatusLabel(wslDistroStatusLabel, false, "WSL2 not installed");
            else if (!hasDistro)
                SetStatusLabel(wslDistroStatusLabel, false, "WSL installed — no Linux distro yet");
            else
                SetStatusLabel(wslDistroStatusLabel, true, "WSL distro: " + distro);

            if (hasDistro)
            {
                RefreshToolLabel(nodeStatusLabel, "node", "Node.js", WslHelper.ProbeWslCommand(distro, "node"));
                RefreshToolLabel(gitStatusLabel, "git", "Git", WslHelper.ProbeWslCommand(distro, "git"));
                RefreshToolLabel(tippecanoeStatusLabel, "tippecanoe", "tippecanoe",
                    WslHelper.ProbeWslCommand(distro, "tippecanoe"));
                mapToolsOk = WslHelper.IsWslMapToolsInstalled(distro) ||
                                 _installedToolLabels.ContainsKey("maptools");
                RefreshMapToolsLabel(mapToolsOk);
            }
            else
            {
                SetStatusLabel(nodeStatusLabel, false, "Node.js");
                SetStatusLabel(gitStatusLabel, false, "Git");
                SetStatusLabel(tippecanoeStatusLabel, false, "tippecanoe");
                SetStatusLabel(mapToolsStatusLabel, false, "Map tools");
            }

            var jobRunning = IsJobRunning();
            installWslButton.Enabled = !jobRunning;
            installToolsButton.Enabled = wslAvailable && hasDistro && !jobRunning;

            var mapToolsReady = !hasDistro || mapToolsOk;
            var backend = _maps.GetRecommendedBackend();
            generateButton.Enabled = !jobRunning && backend != "none" &&
                                     (!hasDistro || backend != "wsl" || mapToolsReady);

            UpdateReadinessTagline(hasDistro, mapToolsReady);

            var ets2Valid = MapGenerationService.IsValidGamePath(ets2PathTextBox.Text);
            var atsValid = MapGenerationService.IsValidGamePath(atsPathTextBox.Text);
            ets2PathLabel.ForeColor = ets2Valid ? TruckDeckTheme.Accent : TruckDeckTheme.Disconnected;
            atsPathLabel.ForeColor = atsValid ? TruckDeckTheme.Accent : TruckDeckTheme.Disconnected;
        }

        void RefreshToolLabel(Label label, string key, string name, object probe)
        {
            if (_installedToolLabels.TryGetValue(key, out var cached))
            {
                var text = key == "maptools" ? cached : name + " · " + cached;
                SetStatusLabel(label, true, text);
                return;
            }

            SetToolProbe(label, name, probe);
        }

        void RefreshMapToolsLabel(bool installed)
        {
            if (_installedToolLabels.TryGetValue("maptools", out var cached))
            {
                SetStatusLabel(mapToolsStatusLabel, true, cached);
                return;
            }

            SetStatusLabel(mapToolsStatusLabel, installed,
                installed ? "Map tools installed" : "Map tools not installed");
        }

        void RememberInstalledTool(string key, string detail)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(detail))
                return;

            _installedToolLabels[key] = detail.Trim();
        }

        void ApplyInstalledTool(string key, Label label, string fallbackName)
        {
            if (!_installedToolLabels.TryGetValue(key, out var detail))
                return;

            SetStatusLabel(label, true, key == "maptools" ? detail : fallbackName + " · " + detail);
        }

        void UpdateToolStatusFromLog(string log)
        {
            if (string.IsNullOrWhiteSpace(log))
                return;

            foreach (Match match in ToolMarkerRegex.Matches(log))
            {
                var tool = match.Groups[1].Value.ToLowerInvariant();
                var detail = match.Groups[2].Value.Trim();
                switch (tool)
                {
                    case "git":
                        RememberInstalledTool("git", detail);
                        ApplyInstalledTool("git", gitStatusLabel, "Git");
                        break;
                    case "node":
                        RememberInstalledTool("node", detail);
                        ApplyInstalledTool("node", nodeStatusLabel, "Node.js");
                        break;
                    case "tippecanoe":
                        RememberInstalledTool("tippecanoe", detail);
                        ApplyInstalledTool("tippecanoe", tippecanoeStatusLabel, "tippecanoe");
                        break;
                    case "maptools":
                        RememberInstalledTool("maptools", "Map tools installed");
                        ApplyInstalledTool("maptools", mapToolsStatusLabel, "Map tools");
                        break;
                }
            }

            var gitMatch = Regex.Match(log, @"git version [\d.]+", RegexOptions.IgnoreCase);
            if (gitMatch.Success)
            {
                RememberInstalledTool("git", gitMatch.Value);
                ApplyInstalledTool("git", gitStatusLabel, "Git");
            }

            var nodeMatch = Regex.Match(log, @"(?:^|\n)\[[^\]]+\]\s*Node:\s*(v[\d.]+)", RegexOptions.Multiline);
            if (nodeMatch.Success)
            {
                RememberInstalledTool("node", nodeMatch.Groups[1].Value);
                ApplyInstalledTool("node", nodeStatusLabel, "Node.js");
            }

            var tippecanoeMatch = Regex.Match(log,
                @"(?:^|\n)\[[^\]]+\]\s*tippecanoe(?:\s+already\s+installed)?:\s*(.+)$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (tippecanoeMatch.Success)
            {
                RememberInstalledTool("tippecanoe", tippecanoeMatch.Groups[1].Value.Trim());
                ApplyInstalledTool("tippecanoe", tippecanoeStatusLabel, "tippecanoe");
            }

            if (log.IndexOf("TRUCKDECK_PROGRESS:100 Map tools ready", StringComparison.OrdinalIgnoreCase) >= 0 ||
                log.IndexOf("TRUCKDECK_DONE: map-tools", StringComparison.OrdinalIgnoreCase) >= 0 ||
                Regex.IsMatch(log, @"TRUCKDECK_DONE:\s*/.+/map-tools/maps", RegexOptions.IgnoreCase))
            {
                RememberInstalledTool("maptools", "Map tools installed");
                ApplyInstalledTool("maptools", mapToolsStatusLabel, "Map tools");
            }
        }

        void RestoreInstallStateFromLogIfNeeded()
        {
            if (_installedToolLabels.ContainsKey("maptools"))
                return;

            var log = _maps.GetLatestCompletedSetupLog();
            if (!string.IsNullOrWhiteSpace(log))
                UpdateToolStatusFromLog(log);
        }

        void UpdateReadinessTagline(bool hasDistro, bool mapToolsReady)
        {
            if (IsJobRunning())
                return;

            if (!hasDistro)
                header.TaglineText = "Install WSL to get started";
            else if (!mapToolsReady)
                header.TaglineText = "WSL ready · install map tools next";
            else
                header.TaglineText = "Ready · generate PMTiles";
        }

        void ClearInstallToolCache()
        {
            _installedToolLabels.Clear();
        }

        static void SetToolProbe(Label label, string name, object probe)
        {
            var available = false;
            string version = null;
            if (probe != null)
            {
                var t = probe.GetType();
                available = (bool)(t.GetProperty("available")?.GetValue(probe) ?? false);
                version = t.GetProperty("version")?.GetValue(probe) as string;
            }

            var detail = available && !string.IsNullOrWhiteSpace(version) ? name + " · " + version : name;
            SetStatusLabel(label, available, detail);
        }

        static void SetStatusLabel(Label label, bool ok, string text)
        {
            label.Text = (ok ? "✓ " : "✗ ") + text;
            label.ForeColor = ok ? TruckDeckTheme.Accent : TruckDeckTheme.Disconnected;
        }

        bool IsJobRunning()
        {
            var job = _maps.GetActiveJob();
            return job != null && (job.Status == "running" || job.Status == "pending");
        }

        void PollJob()
        {
            var job = string.IsNullOrEmpty(_activeJobId)
                ? _maps.GetActiveJob()
                : _maps.GetJob(_activeJobId);

            if (job == null)
            {
                if (string.IsNullOrEmpty(_activeJobId))
                    RefreshStatus();
                return;
            }

            _activeJobId = job.Id;
            UpdateHeaderProgress(job);

            if (!string.IsNullOrEmpty(job.LogTail) && job.LogTail != logTextBox.Text)
            {
                logTextBox.Text = job.LogTail;
                logTextBox.SelectionStart = logTextBox.TextLength;
                logTextBox.ScrollToCaret();
            }

            installWslButton.Enabled = false;
            installToolsButton.Enabled = false;
            generateButton.Enabled = false;

            if (job.Kind == "setup")
                UpdateToolStatusFromLog(job.LogTail);

            if (job.Status != "running" && job.Status != "pending")
            {
                _activeJobId = null;
                if (job.Status == "completed")
                {
                    var doneMsg = job.Kind == "generate"
                        ? (job.Message ?? "Map generation completed.")
                        : "Completed.";
                    UpdateHeaderProgress(job, doneMsg);
                }
                else if (job.Status == "failed")
                    UpdateHeaderProgress(job, "Failed: " + (job.Message ?? "see log below"));

                if (job.Kind == "wsl-install" && job.Status == "completed")
                {
                    WslHelper.InvalidateDistroCache();
                    header.TaglineText = "WSL ready · install map tools next";
                }

                RefreshStatus();
            }
        }

        void UpdateHeaderProgress(MapGenerationJob job, string overrideMessage = null)
        {
            var message = FormatStatusMessage(overrideMessage
                ?? (string.IsNullOrWhiteSpace(job.Message) ? job.Status : job.Message));
            header.StatusText = message;
            header.ProgressValue = job.Status == "running" || job.Status == "pending" || job.Progress > 0
                ? Math.Min(100, Math.Max(0, job.Progress))
                : job.Status == "completed" ? 100 : -1;
            if (job.Status == "failed")
                header.ProgressValue = job.Progress > 0 ? job.Progress : -1;
        }

        static string FormatStatusMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = text.Trim();
            var progress = System.Text.RegularExpressions.Regex.Match(text,
                @"(?:\[\d{2}:\d{2}:\d{2}\]\s*)?TRUCKDECK_PROGRESS:\d+\s+(.*)");
            if (progress.Success)
                return progress.Groups[1].Value.Trim();

            if (text.StartsWith("[") && text.Length > 11 && text[9] == ']')
            {
                var trimmed = text.Substring(10).TrimStart();
                if (!trimmed.StartsWith("TRUCKDECK_", StringComparison.Ordinal))
                    return trimmed;
            }

            return text;
        }

        void TrackJob(MapGenerationJob job)
        {
            if (job.Kind == "setup")
                ClearInstallToolCache();

            _activeJobId = job.Id;
            header.ProgressValue = 0;
            header.StatusText = job.Message ?? "Starting…";
            logTextBox.Clear();
            PollJob();
        }

        string PickFolder(string title, string initialPath = null)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = title;
                if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                    dlg.SelectedPath = initialPath;
                return dlg.ShowDialog(this) == DialogResult.OK ? dlg.SelectedPath : null;
            }
        }

        void browseWslButton_Click(object sender, EventArgs e)
        {
            var picked = PickFolder("Select folder for WSL install (e.g. D:\\WSL)", wslInstallPathTextBox.Text);
            if (!string.IsNullOrWhiteSpace(picked))
                wslInstallPathTextBox.Text = picked;
        }

        void installWslButton_Click(object sender, EventArgs e)
        {
            try
            {
                var path = wslInstallPathTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(path))
                {
                    MessageBox.Show(this, "Choose a folder for the WSL install first.", "WSL",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                TrackJob(_maps.StartWslInstall(path));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                MessageBox.Show(this, ex.Message, "WSL install", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void installToolsButton_Click(object sender, EventArgs e)
        {
            try
            {
                TrackJob(_maps.StartSetupTools());
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                MessageBox.Show(this, ex.Message, "Map tools", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void refreshStatusButton_Click(object sender, EventArgs e)
        {
            WslHelper.InvalidateDistroCache();
            RefreshStatus();
        }

        void browseEts2Button_Click(object sender, EventArgs e)
        {
            var picked = PickFolder("Select Euro Truck Simulator 2 folder", ets2PathTextBox.Text);
            if (!string.IsNullOrWhiteSpace(picked))
                ets2PathTextBox.Text = picked;
            RefreshStatus();
        }

        void browseAtsButton_Click(object sender, EventArgs e)
        {
            var picked = PickFolder("Select American Truck Simulator folder", atsPathTextBox.Text);
            if (!string.IsNullOrWhiteSpace(picked))
                atsPathTextBox.Text = picked;
            RefreshStatus();
        }

        void detectEts2Button_Click(object sender, EventArgs e)
        {
            var path = MapGenerationService.DetectSteamGamePath("ets2");
            if (!string.IsNullOrWhiteSpace(path))
                ets2PathTextBox.Text = path;
            else
                MessageBox.Show(this, "Could not find ETS2 via Steam library folders.", "Detect Steam",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshStatus();
        }

        void detectAtsButton_Click(object sender, EventArgs e)
        {
            var path = MapGenerationService.DetectSteamGamePath("ats");
            if (!string.IsNullOrWhiteSpace(path))
                atsPathTextBox.Text = path;
            else
                MessageBox.Show(this, "Could not find ATS via Steam library folders.", "Detect Steam",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshStatus();
        }

        void savePathsButton_Click(object sender, EventArgs e)
        {
            try
            {
                _maps.UpdateSettings(ets2PathTextBox.Text, atsPathTextBox.Text, "wsl", wslInstallPathTextBox.Text);
                MessageBox.Show(this, "Game paths saved.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshStatus();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                MessageBox.Show(this, ex.Message, "Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void generateButton_Click(object sender, EventArgs e)
        {
            if (!generateEts2CheckBox.Checked && !generateAtsCheckBox.Checked)
            {
                MessageBox.Show(this, "Select at least one game to generate.", "Generate",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _maps.UpdateSettings(ets2PathTextBox.Text, atsPathTextBox.Text, "wsl", wslInstallPathTextBox.Text);

                if (generateEts2CheckBox.Checked)
                    TrackJob(_maps.StartGenerate("ets2", activateCheckBox.Checked));

                if (generateAtsCheckBox.Checked)
                {
                    if (generateEts2CheckBox.Checked && IsJobRunning())
                    {
                        MessageBox.Show(this,
                            "ETS2 generation is running. Run ATS after it finishes, or uncheck ETS2.",
                            "Generate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    TrackJob(_maps.StartGenerate("ats", activateCheckBox.Checked));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                MessageBox.Show(this, ex.Message, "Generate", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
