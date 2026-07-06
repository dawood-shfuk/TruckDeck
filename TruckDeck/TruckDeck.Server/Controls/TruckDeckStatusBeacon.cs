using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Controls
{
    public sealed class TruckDeckStatusBeacon : Control
    {
        readonly Timer _pulseTimer;
        string _statusText = "Checking...";
        Color _statusColor = TruckDeckTheme.Label;
        float _pulse = 0f;
        bool _pulsing;

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value ?? "";
                Invalidate();
            }
        }

        public Color StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                Invalidate();
            }
        }

        public bool Pulsing
        {
            get => _pulsing;
            set
            {
                _pulsing = value;
                _pulseTimer.Enabled = value;
                if (!value)
                    _pulse = 0f;
                Invalidate();
            }
        }

        public TruckDeckStatusBeacon()
        {
            UiPaintHelper.EnableDoubleBuffer(this);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Height = 56;
            BackColor = TruckDeckTheme.Panel;

            _pulseTimer = new Timer { Interval = 40 };
            _pulseTimer.Tick += (_, __) =>
            {
                _pulse += 0.08f;
                if (_pulse > Math.PI * 2)
                    _pulse -= (float)(Math.PI * 2);
                Invalidate();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _pulseTimer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int dotX = 4;
            int dotY = Height / 2;
            float core = 7f;
            float ring = core + (_pulsing ? (float)(Math.Sin(_pulse) * 3 + 4) : 0f);

            if (_pulsing)
            {
                using (var halo = new SolidBrush(Color.FromArgb(48, _statusColor)))
                    g.FillEllipse(halo, dotX - ring, dotY - ring, ring * 2, ring * 2);
            }

            using (var coreBrush = new SolidBrush(_statusColor))
                g.FillEllipse(coreBrush, dotX - core, dotY - core, core * 2, core * 2);

            using (var titleFont = new Font("Segoe UI Semibold", 11f, FontStyle.Bold))
            using (var detailFont = new Font("Segoe UI", 9.75f))
            using (var ink = new SolidBrush(TruckDeckTheme.Ink))
            using (var detail = new SolidBrush(_statusColor))
            {
                g.DrawString("SIM LINK", titleFont, ink, 28, 6);
                g.DrawString(_statusText, detailFont, detail, 28, 28);
            }
        }
    }
}
