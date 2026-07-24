using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal sealed class ThemeTransitionOverlay : Control
    {
        private readonly Bitmap snapshot;
        private readonly Timer timer;
        private int frame;

        private ThemeTransitionOverlay(Bitmap snapshot)
        {
            this.snapshot = snapshot;
            Dock = DockStyle.Fill;
            TabStop = false;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.Opaque,
                true);

            timer = new Timer { Interval = 16 };
            timer.Tick += Advance;
        }

        public static Bitmap CaptureSnapshot(Control source)
        {
            if (source == null || source.ClientSize.Width < 2 || source.ClientSize.Height < 2)
                return null;

            const int scale = 2;
            int width = Math.Max(1, source.ClientSize.Width / scale);
            int height = Math.Max(1, source.ClientSize.Height / scale);
            using (var fullSize = new Bitmap(source.ClientSize.Width, source.ClientSize.Height, PixelFormat.Format32bppPArgb))
            {
                source.DrawToBitmap(fullSize, source.ClientRectangle);
                var reduced = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
                using (Graphics graphics = Graphics.FromImage(reduced))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.InterpolationMode = InterpolationMode.Low;
                    graphics.DrawImage(fullSize, new Rectangle(0, 0, width, height));
                }
                return reduced;
            }
        }

        public static void Show(Control owner, Bitmap snapshot)
        {
            if (owner == null || snapshot == null || owner.IsDisposed)
            {
                snapshot?.Dispose();
                return;
            }

            var overlay = new ThemeTransitionOverlay(snapshot);
            owner.Controls.Add(overlay);
            overlay.BringToFront();
            overlay.timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            double progress = Math.Min(1D, frame / 11D);
            float opacity = (float)Math.Pow(1D - progress, 2D);
            using (var attributes = new ImageAttributes())
            {
                var matrix = new ColorMatrix { Matrix33 = opacity };
                attributes.SetColorMatrix(matrix);
                e.Graphics.InterpolationMode = InterpolationMode.Low;
                e.Graphics.DrawImage(
                    snapshot,
                    ClientRectangle,
                    0,
                    0,
                    snapshot.Width,
                    snapshot.Height,
                    GraphicsUnit.Pixel,
                    attributes);
            }
        }

        private void Advance(object sender, EventArgs e)
        {
            frame++;
            if (frame < 12)
            {
                Invalidate();
                return;
            }

            timer.Stop();
            Parent?.Controls.Remove(this);
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer?.Dispose();
                snapshot?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}