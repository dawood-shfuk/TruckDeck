using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Funbit.Ets.Telemetry.Server.Bridges
{
    public static class KeyComboCaptureHelper
    {
        public static string CaptureCombo(IWin32Window owner)
        {
            using (var dlg = new KeyCaptureDialog())
            {
                return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.CapturedCombo : null;
            }
        }

        sealed class KeyCaptureDialog : Form
        {
            static readonly Dictionary<Keys, string> KeyNames = BuildKeyNames();

            public string CapturedCombo { get; private set; }

            public KeyCaptureDialog()
            {
                Text = "Record key";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                KeyPreview = true;
                Width = 360;
                Height = 140;
                var label = new Label
                {
                    Text = "Press the key combination to send to the game.\r\nEsc to cancel.",
                    Dock = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Padding = new Padding(12)
                };
                Controls.Add(label);
            }

            protected override bool ProcessDialogKey(Keys keyData)
            {
                if (keyData == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    return true;
                }

                var combo = FormatKeyData(keyData);
                if (!string.IsNullOrEmpty(combo) && BridgeActionCatalog.IsValidCombo(combo))
                {
                    CapturedCombo = combo;
                    DialogResult = DialogResult.OK;
                    return true;
                }

                return base.ProcessDialogKey(keyData);
            }

            static string FormatKeyData(Keys keyData)
            {
                var mods = new List<string>();
                if (keyData.HasFlag(Keys.Control))
                    mods.Add("ctrl");
                if (keyData.HasFlag(Keys.Shift))
                    mods.Add("shift");
                if (keyData.HasFlag(Keys.Alt))
                    mods.Add("alt");

                var key = keyData & Keys.KeyCode;
                if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu || key == Keys.LWin || key == Keys.RWin)
                    return null;

                if (!KeyNames.TryGetValue(key, out var name))
                    return null;

                mods.Add(name);
                return string.Join("+", mods);
            }

            static Dictionary<Keys, string> BuildKeyNames()
            {
                return new Dictionary<Keys, string>
                {
                    [Keys.Escape] = "esc",
                    [Keys.Tab] = "tab",
                    [Keys.Space] = "space",
                    [Keys.Back] = "backspace",
                    [Keys.Enter] = "enter",
                    [Keys.Oemcomma] = ",",
                    [Keys.OemPeriod] = ".",
                    [Keys.OemSemicolon] = ";",
                    [Keys.OemQuotes] = "'",
                    [Keys.OemOpenBrackets] = "[",
                    [Keys.OemCloseBrackets] = "]",
                    [Keys.OemMinus] = "-",
                    [Keys.Oemplus] = "=",
                    [Keys.OemBackslash] = "\\",
                    [Keys.Oemtilde] = "`",
                    [Keys.D0] = "0", [Keys.D1] = "1", [Keys.D2] = "2", [Keys.D3] = "3", [Keys.D4] = "4",
                    [Keys.D5] = "5", [Keys.D6] = "6", [Keys.D7] = "7", [Keys.D8] = "8", [Keys.D9] = "9",
                    [Keys.A] = "a", [Keys.B] = "b", [Keys.C] = "c", [Keys.D] = "d", [Keys.E] = "e",
                    [Keys.F] = "f", [Keys.G] = "g", [Keys.H] = "h", [Keys.I] = "i", [Keys.J] = "j",
                    [Keys.K] = "k", [Keys.L] = "l", [Keys.M] = "m", [Keys.N] = "n", [Keys.O] = "o",
                    [Keys.P] = "p", [Keys.Q] = "q", [Keys.R] = "r", [Keys.S] = "s", [Keys.T] = "t",
                    [Keys.U] = "u", [Keys.V] = "v", [Keys.W] = "w", [Keys.X] = "x", [Keys.Y] = "y",
                    [Keys.Z] = "z",
                    [Keys.F1] = "f1", [Keys.F2] = "f2", [Keys.F3] = "f3", [Keys.F4] = "f4",
                    [Keys.F5] = "f5", [Keys.F6] = "f6", [Keys.F7] = "f7", [Keys.F8] = "f8",
                    [Keys.F9] = "f9", [Keys.F10] = "f10", [Keys.F11] = "f11", [Keys.F12] = "f12",
                    [Keys.Up] = "up", [Keys.Down] = "down", [Keys.Left] = "left", [Keys.Right] = "right",
                    [Keys.PageUp] = "pgup", [Keys.PageDown] = "pgdown",
                    [Keys.Insert] = "insert", [Keys.Delete] = "delete",
                    [Keys.Home] = "home", [Keys.End] = "end"
                };
            }
        }
    }
}
