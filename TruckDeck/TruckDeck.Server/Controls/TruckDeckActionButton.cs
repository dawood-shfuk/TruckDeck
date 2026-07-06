using System.Drawing;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Controls
{
    public sealed class TruckDeckActionButton : Button
    {
        bool _primary = true;
        bool _hover;

        public bool Primary
        {
            get => _primary;
            set
            {
                _primary = value;
                ApplyColors();
            }
        }

        public TruckDeckActionButton()
        {
            FlatStyle = FlatStyle.Flat;
            Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold);
            Height = 38;
            Cursor = Cursors.Hand;
            TabStop = true;

            FlatAppearance.BorderSize = 1;
            MouseEnter += (_, __) => { _hover = true; ApplyColors(); };
            MouseLeave += (_, __) => { _hover = false; ApplyColors(); };
            ApplyColors();
        }

        void ApplyColors()
        {
            if (_primary)
            {
                BackColor = _hover ? TruckDeckTheme.Accent : Color.FromArgb(182, 255, 31);
                ForeColor = Color.FromArgb(12, 16, 8);
                FlatAppearance.BorderColor = TruckDeckTheme.Accent;
            }
            else
            {
                BackColor = _hover ? Color.FromArgb(48, 54, 40) : TruckDeckTheme.ButtonBg;
                ForeColor = TruckDeckTheme.Accent;
                FlatAppearance.BorderColor = TruckDeckTheme.AccentDim;
            }
        }
    }
}
