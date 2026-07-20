using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CSVParserTool
{
    /// <summary>메인 창 내부에 한 번만 그려지는 Blur 레이어를 올린 뒤 모달 창을 표시합니다.</summary>
    internal static class ModalBlurBackdrop
    {
        private sealed class BlurOverlayPanel : Panel
        {
            private readonly Bitmap blurredImage;

            public BlurOverlayPanel(Bitmap image)
            {
                blurredImage = image;
                BackColor = UiTheme.IsDarkMode
                    ? Color.FromArgb(28, 28, 32)
                    : Color.FromArgb(238, 240, 244);
                BackgroundImage = blurredImage;
                BackgroundImageLayout = ImageLayout.Stretch;
                TabStop = false;
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint,
                    true);
                DoubleBuffered = true;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    BackgroundImage = null;
                    blurredImage?.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        public static DialogResult ShowDialog(Form owner, Form dialog)
        {
            if (owner == null || dialog == null || owner.IsDisposed)
                return dialog?.ShowDialog() ?? DialogResult.Cancel;

            Bitmap snapshot = TryCreateBlurredSnapshot(owner);
            using (var overlay = new BlurOverlayPanel(snapshot))
            {
                overlay.Bounds = owner.ClientRectangle;
                overlay.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                owner.Controls.Add(overlay);
                overlay.BringToFront();
                overlay.Update();

                try
                {
                    return dialog.ShowDialog(owner);
                }
                finally
                {
                    owner.Controls.Remove(overlay);
                    if (!owner.IsDisposed && owner.Visible)
                    {
                        owner.Invalidate(true);
                        owner.Update();
                        owner.Activate();
                    }
                }
            }
        }

        private static Bitmap TryCreateBlurredSnapshot(Form owner)
        {
            Rectangle clientBounds = owner.RectangleToScreen(owner.ClientRectangle);
            if (clientBounds.Width <= 0 || clientBounds.Height <= 0)
                return null;

            try
            {
                using (var source = new Bitmap(clientBounds.Width, clientBounds.Height, PixelFormat.Format32bppPArgb))
                {
                    using (Graphics capture = Graphics.FromImage(source))
                        capture.CopyFromScreen(clientBounds.Location, Point.Empty, clientBounds.Size, CopyPixelOperation.SourceCopy);

                    int smallWidth = Math.Max(1, clientBounds.Width / 10);
                    int smallHeight = Math.Max(1, clientBounds.Height / 10);
                    using (var reduced = new Bitmap(smallWidth, smallHeight, PixelFormat.Format32bppPArgb))
                    {
                        using (Graphics down = Graphics.FromImage(reduced))
                        {
                            down.CompositingMode = CompositingMode.SourceCopy;
                            down.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            down.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            down.DrawImage(source, new Rectangle(0, 0, smallWidth, smallHeight));
                        }

                        var result = new Bitmap(clientBounds.Width, clientBounds.Height, PixelFormat.Format32bppPArgb);
                        using (Graphics up = Graphics.FromImage(result))
                        {
                            up.CompositingMode = CompositingMode.SourceCopy;
                            up.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            up.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            up.DrawImage(reduced, new Rectangle(0, 0, result.Width, result.Height));

                            up.CompositingMode = CompositingMode.SourceOver;
                            Color tint = UiTheme.IsDarkMode
                                ? Color.FromArgb(68, 10, 10, 13)
                                : Color.FromArgb(44, 248, 249, 252);
                            using (var tintBrush = new SolidBrush(tint))
                                up.FillRectangle(tintBrush, 0, 0, result.Width, result.Height);
                        }
                        return result;
                    }
                }
            }
            catch
            {
                // 화면 캡처가 제한된 환경에서는 단색 레이어를 사용합니다.
                return null;
            }
        }
    }
}