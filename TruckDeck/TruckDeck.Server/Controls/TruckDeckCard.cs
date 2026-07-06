using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Controls
{
    public sealed class TruckDeckCard : Panel
    {
        string _cardTitle = "";

        public string CardTitle
        {
            get => _cardTitle;
            set
            {
                _cardTitle = value ?? "";
                Invalidate();
            }
        }

        public TruckDeckCard()
        {
            UiPaintHelper.EnableDoubleBuffer(this);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = TruckDeckTheme.Panel;
            ForeColor = TruckDeckTheme.Ink;
            Padding = new Padding(16, 28, 16, 14);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = UiPaintHelper.RoundedRect(bounds, UiPaintHelper.CardRadius))
            using (var fill = new SolidBrush(TruckDeckTheme.Panel))
            using (var border = new Pen(TruckDeckTheme.Line, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            using (var accent = new Pen(TruckDeckTheme.AccentDim, 2f))
            {
                g.DrawLine(accent, 16, 0, 56, 0);
            }

            if (!string.IsNullOrEmpty(_cardTitle))
            {
                using (var titleFont = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold))
                using (var brush = new SolidBrush(TruckDeckTheme.Label))
                {
                    var text = _cardTitle.ToUpperInvariant();
                    g.DrawString(text, titleFont, brush, 16, 10);
                }
            }

            base.OnPaint(e);
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            using (var path = UiPaintHelper.RoundedRect(new Rectangle(0, 0, Width, Height), UiPaintHelper.CardRadius))
            {
                Region = new Region(path);
            }
        }
    }
}
