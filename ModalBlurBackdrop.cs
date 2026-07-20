using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CSVParserTool
{
    /// <summary>모달 창 뒤의 메인 화면을 캡처해 흐리게 표시하는 전용 배경 레이어.</summary>
    internal sealed class ModalBlurBackdrop : Form
    {
        private readonly Bitmap blurredImage;
        private readonly Timer fadeTimer;
        private int fadeFrame;

        private ModalBlurBackdrop(Form owner)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            ControlBox = false;
            TopMost = owner.TopMost;
            Bounds = owner.Bounds;
            BackColor = UiTheme.IsDarkMode ? Color.FromArgb(22, 22, 25) : Color.FromArgb(236, 238, 242);

            blurredImage = TryCreateBlurredSnapshot(owner.Bounds);
            if (blurredImage != null)
            {
                BackgroundImage = blurredImage;
                BackgroundImageLayout = ImageLayout.Stretch;
            }

            if (SystemInformation.IsMenuAnimationEnabled)
            {
                Opacity = 0D;
                fadeTimer = new Timer { Interval = 15 };
                fadeTimer.Tick += (_, __) => AdvanceFadeIn();
            }
            else
            {
                Opacity = 1D;
            }
        }

        public static DialogResult ShowDialog(Form owner, Form dialog)
        {
            if (owner == null || dialog == null || owner.IsDisposed)
                return dialog?.ShowDialog() ?? DialogResult.Cancel;

            using (var backdrop = new ModalBlurBackdrop(owner))
            {
                bool ownerWasEnabled = owner.Enabled;
                try
                {
                    backdrop.Show(owner);
                    backdrop.fadeTimer?.Start();
                    owner.Enabled = false;
                    backdrop.Activate();
                    return dialog.ShowDialog(backdrop);
                }
                finally
                {
                    owner.Enabled = ownerWasEnabled;
                    backdrop.Close();
                    if (!owner.IsDisposed && owner.Visible)
                        owner.Activate();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fadeTimer?.Dispose();
                BackgroundImage = null;
                blurredImage?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void AdvanceFadeIn()
        {
            fadeFrame++;
            double progress = Math.Min(1D, fadeFrame / 8D);
            Opacity = 1D - Math.Pow(1D - progress, 3D);
            if (progress >= 1D)
            {
                fadeTimer.Stop();
                Opacity = 1D;
            }
        }

        private static Bitmap TryCreateBlurredSnapshot(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return null;

            try
            {
                using (var source = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb))
                {
                    using (Graphics capture = Graphics.FromImage(source))
                        capture.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);

                    int smallWidth = Math.Max(1, bounds.Width / 12);
                    int smallHeight = Math.Max(1, bounds.Height / 12);
                    using (var reduced = new Bitmap(smallWidth, smallHeight, PixelFormat.Format32bppPArgb))
                    {
                        using (Graphics down = Graphics.FromImage(reduced))
                        {
                            down.CompositingMode = CompositingMode.SourceCopy;
                            down.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            down.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            down.DrawImage(source, new Rectangle(0, 0, smallWidth, smallHeight));
                        }

                        var result = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
                        using (Graphics up = Graphics.FromImage(result))
                        {
                            up.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            up.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            up.DrawImage(reduced, new Rectangle(0, 0, result.Width, result.Height));
                            Color tint = UiTheme.IsDarkMode
                                ? Color.FromArgb(72, 10, 10, 13)
                                : Color.FromArgb(48, 248, 249, 252);
                            using (var tintBrush = new SolidBrush(tint))
                                up.FillRectangle(tintBrush, 0, 0, result.Width, result.Height);
                        }
                        return result;
                    }
                }
            }
            catch
            {
                // 화면 캡처가 제한된 환경에서는 단색 배경으로 대체합니다.
                return null;
            }
        }
    }
}