using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Funbit.Ets.Telemetry.Server.Controls
{
    static class UiPaintHelper
    {
        public const int CardRadius = 14;

        public static void EnableDoubleBuffer(Control control)
        {
            control.GetType()
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int d = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(d, d));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawDashedRoad(Graphics g, Rectangle area, Color color)
        {
            using (var pen = new Pen(color, 2f))
            {
                pen.DashStyle = DashStyle.Dash;
                pen.DashPattern = new[] { 6f, 5f };
                int y = area.Y + area.Height / 2;
                g.DrawLine(pen, area.X, y, area.Right, y);
            }
        }
    }
}
