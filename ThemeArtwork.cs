using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CSVParserTool
{
    internal static class ThemeArtwork
    {
        public static Bitmap Load(AppTheme theme)
        {
            if (theme == AppTheme.Default)
                return CreateDefaultPreview();

            string suffix = theme == AppTheme.Grassland
                ? "assets.themes.grassland.png"
                : "assets.themes.ocean.png";
            Assembly assembly = typeof(ThemeArtwork).Assembly;
            string resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
                return CreateDefaultPreview();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (var image = Image.FromStream(stream))
                return new Bitmap(image);
        }

        private static Bitmap CreateDefaultPreview()
        {
            var bitmap = new Bitmap(512, 512);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var background = new LinearGradientBrush(
                    new Rectangle(0, 0, 512, 512),
                    Color.FromArgb(35, 45, 66),
                    Color.FromArgb(15, 21, 34),
                    45F))
                    graphics.FillRectangle(background, 0, 0, 512, 512);

                using (var shadow = new SolidBrush(Color.FromArgb(90, 3, 8, 18)))
                    graphics.FillRoundedRectangle(shadow, new Rectangle(105, 145, 302, 235), 34);
                using (var surface = new LinearGradientBrush(
                    new Rectangle(95, 125, 302, 235),
                    Color.FromArgb(70, 104, 190),
                    Color.FromArgb(36, 65, 136),
                    90F))
                    graphics.FillRoundedRectangle(surface, new Rectangle(95, 125, 302, 235), 34);

                using (var tile = new SolidBrush(Color.FromArgb(225, 235, 255)))
                using (var accent = new SolidBrush(Color.FromArgb(87, 156, 255)))
                {
                    for (int row = 0; row < 3; row++)
                    for (int column = 0; column < 4; column++)
                    {
                        var rect = new Rectangle(135 + column * 61, 165 + row * 51, 43, 31);
                        graphics.FillRoundedRectangle(row == 1 && column == 2 ? accent : tile, rect, 8);
                    }
                }
            }
            return bitmap;
        }

        private static void FillRoundedRectangle(
            this Graphics graphics,
            Brush brush,
            Rectangle bounds,
            int radius)
        {
            int diameter = radius * 2;
            using (var path = new GraphicsPath())
            {
                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                graphics.FillPath(brush, path);
            }
        }
    }
}
