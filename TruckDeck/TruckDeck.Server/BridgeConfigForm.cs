using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Bridges;
using Funbit.Ets.Telemetry.Server.Controls;
using Funbit.Ets.Telemetry.Server.Helpers;
using Funbit.Ets.Telemetry.Server.Services;

namespace Funbit.Ets.Telemetry.Server
{
    public partial class BridgeConfigForm : Form
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly BridgeConfigService _bridge = BridgeConfigService.Instance;
        BridgeConfigSnapshot _snapshot;
        BridgeConfigSnapshot _loadedSnapshot;
        bool _dirty;
        bool _suppressDirty;
        CancellationTokenSource _captureCancel;

        DataGridViewTextBoxColumn _colCategory;
        DataGridViewTextBoxColumn _colLabel;
        DataGridViewTextBoxColumn _colActionId;
        DataGridViewTextBoxColumn _colCombo;
        DataGridViewButtonColumn _colRecord;
        DataGridViewButtonColumn _colTest;
        DataGridViewButtonColumn _colReset;

        public BridgeConfigForm()
        {
            InitializeComponent();
            ApplicationIconHelper.Apply(this);
            TruckDeckTheme.Apply(this);
            ApplyReadableTheme();
            header.TitleText = "BRIDGE CONFIG";
            header.TaglineText = "Joystick: cycle dashboards · Hotkeys: match in-game keys";
            header.VersionText = AssemblyHelper.Version;

            SetupHotkeysGrid();
            AddHotkeysExplanation();
            LoadConfig();
            RefreshJoystickTab();
        }

        void ApplyReadableTheme()
        {
            var ink = Color.Black;
            var surface = Color.White;
            var surfaceAlt = Color.FromArgb(245, 245, 245);

            tabs.BackColor = surfaceAlt;
            footerPanel.BackColor = surfaceAlt;
            statusLabel.ForeColor = ink;

            foreach (TabPage tab in tabs.TabPages)
            {
                tab.BackColor = surfaceAlt;
                tab.ForeColor = ink;
                ApplyControlTextColor(tab.Controls, ink, surface, surfaceAlt);
            }

            hotkeysGrid.BackgroundColor = surface;
            hotkeysGrid.GridColor = Color.FromArgb(220, 220, 220);
            hotkeysGrid.DefaultCellStyle.BackColor = surface;
            hotkeysGrid.DefaultCellStyle.ForeColor = ink;
            hotkeysGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 230, 255);
            hotkeysGrid.DefaultCellStyle.SelectionForeColor = ink;
            hotkeysGrid.ColumnHeadersDefaultCellStyle.BackColor = surfaceAlt;
            hotkeysGrid.ColumnHeadersDefaultCellStyle.ForeColor = ink;
            hotkeysGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            hotkeysGrid.EnableHeadersVisualStyles = false;
            hotkeysGrid.RowHeadersDefaultCellStyle.BackColor = surfaceAlt;
            hotkeysGrid.RowHeadersDefaultCellStyle.ForeColor = ink;
        }

        static void ApplyControlTextColor(Control.ControlCollection controls, Color ink, Color surface, Color surfaceAlt)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case TruckDeckHeader _:
                    case TruckDeckActionButton _:
                        break;
                    case DataGridView _:
                        break;
                    case TextBox textBox:
                        textBox.ForeColor = ink;
                        textBox.BackColor = surface;
                        break;
                    case ListBox listBox:
                        listBox.ForeColor = ink;
                        listBox.BackColor = surface;
                        break;
                    case NumericUpDown numeric:
                        numeric.ForeColor = ink;
                        numeric.BackColor = surface;
                        break;
                    case Button button:
                        button.ForeColor = ink;
                        button.BackColor = surfaceAlt;
                        button.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
                        break;
                    case Label label when label != null && label.Name != "duplicateWarningLabel" && label.Name != "joyHintLabel":
                        label.ForeColor = ink;
                        label.BackColor = Color.Transparent;
                        break;
                }

                if (control.HasChildren)
                    ApplyControlTextColor(control.Controls, ink, surface, surfaceAlt);
            }
        }

        void AddHotkeysExplanation()
        {
            var hint = new Label
            {
                Name = "hotkeysHintLabel",
                Location = new Point(15, 38),
                Size = new Size(682, 32),
                ForeColor = Color.Black,
                Text = "TruckDeck dashboard buttons send keyboard keys to the game. Match each action to the same key in ETS2/ATS → Options → Keys & Buttons."
            };
            hotkeysTab.Controls.Add(hint);
            duplicateWarningLabel.Location = new Point(15, 72);
            hotkeysGrid.Location = new Point(15, 92);
            hotkeysGrid.Height = hotkeysGrid.Height - 24;
        }

        void SetupHotkeysGrid()
        {
            _colCategory = new DataGridViewTextBoxColumn
            {
                Name = "Category",
                HeaderText = "Category",
                ReadOnly = true,
                FillWeight = 90
            };
            _colLabel = new DataGridViewTextBoxColumn
            {
                Name = "Label",
                HeaderText = "Action",
                ReadOnly = true,
                FillWeight = 140
            };
            _colActionId = new DataGridViewTextBoxColumn
            {
                Name = "ActionId",
                HeaderText = "Id",
                ReadOnly = true,
                FillWeight = 80
            };
            _colCombo = new DataGridViewTextBoxColumn
            {
                Name = "Combo",
                HeaderText = "In-game key",
                FillWeight = 110
            };
            _colRecord = new DataGridViewButtonColumn
            {
                Name = "Record",
                HeaderText = "",
                Text = "Record",
                UseColumnTextForButtonValue = true,
                FillWeight = 45
            };
            _colTest = new DataGridViewButtonColumn
            {
                Name = "Test",
                HeaderText = "",
                Text = "Test",
                UseColumnTextForButtonValue = true,
                FillWeight = 40
            };
            _colReset = new DataGridViewButtonColumn
            {
                Name = "Reset",
                HeaderText = "",
                Text = "Reset",
                UseColumnTextForButtonValue = true,
                FillWeight = 45
            };

            hotkeysGrid.Columns.AddRange(_colCategory, _colLabel, _colActionId, _colCombo,
                _colRecord, _colTest, _colReset);
        }

        void LoadConfig()
        {
            _suppressDirty = true;
            try
            {
                _snapshot = _bridge.Load();
                _loadedSnapshot = CloneSnapshot(_snapshot);
                screenCycleJoyTextBox.Text = _snapshot.ScreenCycleJoy ?? "";
                tapHoldNumeric.Value = Math.Max(tapHoldNumeric.Minimum, Math.Min(tapHoldNumeric.Maximum, _snapshot.TapHoldMs));
                portValueLabel.Text = _bridge.EffectivePort.ToString();
                PopulateHotkeysGrid();
                SetDirty(false);
                statusLabel.Text = "Loaded " + _bridge.ConfigPath;
            }
            finally
            {
                _suppressDirty = false;
            }
        }

        void PopulateHotkeysGrid()
        {
            hotkeysGrid.Rows.Clear();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in BridgeActionCatalog.All)
            {
                seen.Add(entry.Id);
                AddHotkeyRow(entry.Category, entry.Label, entry.Id,
                    _snapshot.Keys.TryGetValue(entry.Id, out var combo) ? combo : "");
            }

            foreach (var pair in _snapshot.Keys.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (seen.Contains(pair.Key))
                    continue;
                AddHotkeyRow("Custom", pair.Key, pair.Key, pair.Value);
            }

            ApplySearchFilter();
            UpdateDuplicateWarning();
        }

        void AddHotkeyRow(string category, string label, string actionId, string combo)
        {
            var rowIndex = hotkeysGrid.Rows.Add(category, label, actionId, combo ?? "", "Record", "Test", "Reset");
            hotkeysGrid.Rows[rowIndex].Tag = actionId;
        }

        void RefreshJoystickTab()
        {
            deviceListBox.Items.Clear();
            var devices = JoyCaptureHelper.ListDevices().Where(d => d.Active).ToList();
            foreach (var device in devices)
                deviceListBox.Items.Add("joy" + (device.Index + 1) + ": " + device.Name + " (connected)");
            if (deviceListBox.Items.Count == 0)
            {
                deviceListBox.Items.Add("No joystick connected — plug in your wheel to capture a screen-cycle button.");
                deviceListBox.Items.Add("You can still type a binding manually (e.g. joy1.b1) to match ETS2 → Options → Controls.");
            }

            var joy = _bridge.GetJoyStatus();
            joyStatusLabel.Text = joy.Enabled
                ? "Joystick monitor: active on " + joy.Binding
                : "Joystick monitor: disabled" + (string.IsNullOrEmpty(joy.Reason) ? "" : " — " + joy.Reason);

            bridgeHealthLabel.Text = "Bridge port " + _bridge.EffectivePort
                                     + " · " + (_snapshot?.Keys?.Count ?? 0) + " key mappings";
        }

        void SetDirty(bool dirty)
        {
            _dirty = dirty;
            statusLabel.Text = dirty ? "Unsaved changes" : "Ready";
            Text = dirty ? "Bridge Config *" : "Bridge Config";
        }

        void OnFieldChanged(object sender, EventArgs e)
        {
            if (!_suppressDirty)
                SetDirty(true);
        }

        void ApplySnapshotToModel()
        {
            _snapshot.ScreenCycleJoy = screenCycleJoyTextBox.Text.Trim();
            _snapshot.TapHoldMs = (int)tapHoldNumeric.Value;
            _snapshot.Keys = ReadKeysFromGrid();
        }

        Dictionary<string, string> ReadKeysFromGrid()
        {
            var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var gridIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataGridViewRow row in hotkeysGrid.Rows)
            {
                if (row.IsNewRow)
                    continue;
                var actionId = row.Tag as string ?? row.Cells[_colActionId.Index].Value?.ToString();
                if (string.IsNullOrWhiteSpace(actionId))
                    continue;
                gridIds.Add(actionId);
                var combo = row.Cells[_colCombo.Index].Value?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(combo))
                    keys[actionId] = combo;
            }

            foreach (var pair in _loadedSnapshot.Keys)
            {
                if (gridIds.Contains(pair.Key))
                    continue;
                if (BridgeActionCatalog.Find(pair.Key) != null)
                    continue;
                keys[pair.Key] = pair.Value;
            }

            return keys;
        }

        static BridgeConfigSnapshot CloneSnapshot(BridgeConfigSnapshot source)
        {
            return new BridgeConfigSnapshot
            {
                Port = source.Port,
                TapHoldMs = source.TapHoldMs,
                ScreenCycleJoy = source.ScreenCycleJoy,
                Keys = new Dictionary<string, string>(source.Keys, StringComparer.OrdinalIgnoreCase)
            };
        }

        void saveButton_Click(object sender, EventArgs e)
        {
            try
            {
                ApplySnapshotToModel();
                var invalid = _snapshot.Keys
                    .Where(p => !string.IsNullOrWhiteSpace(p.Value) && !BridgeActionCatalog.IsValidCombo(p.Value))
                    .Select(p => p.Key)
                    .ToList();
                if (invalid.Count > 0)
                {
                    MessageBox.Show(this,
                        "Invalid key combo for: " + string.Join(", ", invalid.Take(5))
                        + (invalid.Count > 5 ? "…" : ""),
                        "Bridge Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _bridge.Save(_snapshot);
                _loadedSnapshot = CloneSnapshot(_snapshot);
                SetDirty(false);
                statusLabel.Text = "Saved — bridge reloaded";
                RefreshJoystickTab();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to save bridge config", ex);
                MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        void BridgeConfigForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_dirty)
                return;
            var result = MessageBox.Show(this,
                "Discard unsaved bridge config changes?",
                "Bridge Config",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                e.Cancel = true;
        }

        async void captureJoyButton_Click(object sender, EventArgs e)
        {
            captureJoyButton.Enabled = false;
            statusLabel.Text = "Press a joystick button…";
            _captureCancel?.Cancel();
            _captureCancel = new CancellationTokenSource();
            var token = _captureCancel.Token;
            try
            {
                var result = await Task.Run(() => JoyCaptureHelper.CaptureNextButton(60000, token), token);
                screenCycleJoyTextBox.Text = result.Binding;
                SetDirty(true);
                statusLabel.Text = "Captured " + result.Binding + " on " + result.DeviceName;
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Capture cancelled";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Joystick capture", MessageBoxButtons.OK, MessageBoxIcon.Information);
                statusLabel.Text = "Capture failed";
            }
            finally
            {
                captureJoyButton.Enabled = true;
            }
        }

        void testJoyButton_Click(object sender, EventArgs e)
        {
            _bridge.TriggerScreenCycleTest();
            statusLabel.Text = "Queued screen-cycle test event";
        }

        void searchTextBox_TextChanged(object sender, EventArgs e) => ApplySearchFilter();

        void ApplySearchFilter()
        {
            var q = (searchTextBox.Text ?? "").Trim();
            foreach (DataGridViewRow row in hotkeysGrid.Rows)
            {
                if (row.IsNewRow)
                    continue;
                if (string.IsNullOrEmpty(q))
                {
                    row.Visible = true;
                    continue;
                }

                var hay = string.Join(" ",
                    row.Cells[_colCategory.Index].Value,
                    row.Cells[_colLabel.Index].Value,
                    row.Cells[_colActionId.Index].Value,
                    row.Cells[_colCombo.Index].Value);
                row.Visible = hay.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        void hotkeysGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (hotkeysGrid.IsCurrentCellDirty)
                hotkeysGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        void hotkeysGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != _colCombo.Index)
                return;
            if (!_suppressDirty)
                SetDirty(true);
            UpdateDuplicateWarning();
        }

        void hotkeysGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var row = hotkeysGrid.Rows[e.RowIndex];
            var actionId = row.Tag as string;
            if (string.IsNullOrWhiteSpace(actionId))
                return;

            if (e.ColumnIndex == _colRecord.Index)
            {
                var combo = KeyComboCaptureHelper.CaptureCombo(this);
                if (!string.IsNullOrWhiteSpace(combo))
                {
                    row.Cells[_colCombo.Index].Value = combo;
                    SetDirty(true);
                    UpdateDuplicateWarning();
                }
                return;
            }

            if (e.ColumnIndex == _colTest.Index)
            {
                var combo = row.Cells[_colCombo.Index].Value?.ToString();
                if (string.IsNullOrWhiteSpace(combo))
                {
                    MessageBox.Show(this, "Set a key combo first.", "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (!BridgeActionCatalog.IsValidCombo(combo))
                {
                    MessageBox.Show(this, "Invalid combo: " + combo, "Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MessageBox.Show(this,
                    "Sending key combo to the active window.\nFocus ETS2/ATS first, then click OK.",
                    "Test key", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SendInputHelper.TapCombo(combo, (int)tapHoldNumeric.Value);
                return;
            }

            if (e.ColumnIndex == _colReset.Index)
            {
                var entry = BridgeActionCatalog.Find(actionId);
                row.Cells[_colCombo.Index].Value = entry?.DefaultCombo ?? "";
                SetDirty(true);
                UpdateDuplicateWarning();
            }
        }

        void UpdateDuplicateWarning()
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in hotkeysGrid.Rows)
            {
                if (row.IsNewRow || !row.Visible)
                    continue;
                var combo = row.Cells[_colCombo.Index].Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(combo))
                    continue;
                if (!counts.ContainsKey(combo))
                    counts[combo] = 0;
                counts[combo]++;
            }

            var dupes = counts.Where(p => p.Value > 1).Select(p => p.Key).ToList();
            duplicateWarningLabel.Text = dupes.Count == 0
                ? ""
                : "Duplicate combos: " + string.Join(", ", dupes.Take(4)) + (dupes.Count > 4 ? "…" : "");

            var dupeSet = new HashSet<string>(dupes, StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in hotkeysGrid.Rows)
            {
                if (row.IsNewRow)
                    continue;
                var combo = row.Cells[_colCombo.Index].Value?.ToString()?.Trim();
                var isDupe = !string.IsNullOrWhiteSpace(combo) && dupeSet.Contains(combo);
                row.Cells[_colCombo.Index].Style.BackColor = isDupe ? Color.FromArgb(255, 250, 220) : Color.Empty;
            }
        }

        void exportButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                dlg.FileName = "InputBridgeConfig.json";
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                ApplySnapshotToModel();
                _bridge.ExportSnapshot(_snapshot, dlg.FileName);
                statusLabel.Text = "Exported " + dlg.FileName;
            }
        }

        void importButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                _suppressDirty = true;
                try
                {
                    _snapshot = _bridge.ImportFrom(dlg.FileName);
                    screenCycleJoyTextBox.Text = _snapshot.ScreenCycleJoy ?? "";
                    tapHoldNumeric.Value = Math.Max(tapHoldNumeric.Minimum, Math.Min(tapHoldNumeric.Maximum, _snapshot.TapHoldMs));
                    PopulateHotkeysGrid();
                    RefreshJoystickTab();
                    SetDirty(true);
                    statusLabel.Text = "Imported " + dlg.FileName + " (not saved yet)";
                }
                finally
                {
                    _suppressDirty = false;
                }
            }
        }

        void restoreDefaultsButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(this,
                "Restore all catalog defaults in this dialog?\nYou must click Save to write the file.",
                "Restore defaults",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            _snapshot = _bridge.CreateDefaultsSnapshot();
            screenCycleJoyTextBox.Text = _snapshot.ScreenCycleJoy ?? "";
            tapHoldNumeric.Value = _snapshot.TapHoldMs;
            PopulateHotkeysGrid();
            SetDirty(true);
        }
    }
}
