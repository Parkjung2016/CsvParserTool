using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CSVParserTool.Exporting;

namespace CSVParserTool
{
    internal sealed class EngineSelectionForm : Form
    {
        private readonly EngineCard unityCard;
        private readonly EngineCard unrealCard;
        private readonly Timer entranceTimer;
        private int animationFrame;
        private Point targetLocation;

        public ExportPlatform SelectedPlatform { get; private set; }

        public EngineSelectionForm(ExportPlatform initialPlatform)
            : this(initialPlatform, selectionRequired: true)
        {
        }

        public EngineSelectionForm(ExportPlatform initialPlatform, bool selectionRequired)
        {
            SelectedPlatform = initialPlatform;
            Text = "엔진 선택";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ControlBox = !selectionRequired;
            ClientSize = new Size(640, 420);
            Padding = new Padding(32, 26, 32, 24);
            Font = UITheme.FontUI;
            BackColor = UITheme.AppBackground;
            ForeColor = UITheme.TextPrimary;
            Opacity = SystemInformation.IsMenuAnimationEnabled ? 0D : 1D;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = UITheme.AppBackground
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label
            {
                AutoSize = true,
                Text = "엔진 선택",
                Font = UITheme.FontTitle,
                ForeColor = UITheme.TextPrimary,
                Margin = new Padding(0, 0, 0, 22),
                Anchor = AnchorStyles.None
            };

            var cards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            unityCard = new EngineCard(ExportPlatform.Unity, "Unity", "unity.png", tintForDarkTheme: false);
            unrealCard = new EngineCard(ExportPlatform.Unreal, "Unreal Engine", "unreal.png", tintForDarkTheme: true);
            unityCard.Margin = new Padding(0, 0, 9, 0);
            unrealCard.Margin = new Padding(9, 0, 0, 0);
            unityCard.Selected += SelectPlatform;
            unrealCard.Selected += SelectPlatform;
            unityCard.Confirmed += ConfirmPlatform;
            unrealCard.Confirmed += ConfirmPlatform;
            cards.Controls.Add(unityCard, 0, 0);
            cards.Controls.Add(unrealCard, 1, 0);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0, 20, 0, 0)
            };
            var startButton = new Button
            {
                Text = "시작",
                AutoSize = true,
                Padding = new Padding(28, 6, 28, 6),
                Margin = Padding.Empty,
                DialogResult = DialogResult.OK
            };
            UITheme.StylePrimaryButton(startButton);
            actions.Controls.Add(startButton);

            if (!selectionRequired)
            {
                var cancelButton = new Button
                {
                    Text = "취소",
                    AutoSize = true,
                    Padding = new Padding(18, 6, 18, 6),
                    Margin = new Padding(0, 0, 8, 0),
                    DialogResult = DialogResult.Cancel
                };
                UITheme.StyleSecondaryButton(cancelButton);
                actions.Controls.Add(cancelButton);
                CancelButton = cancelButton;
            }

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(cards, 0, 1);
            root.Controls.Add(actions, 0, 2);
            Controls.Add(root);
            AcceptButton = startButton;

            SetSelected(initialPlatform);
            entranceTimer = new Timer { Interval = 16 };
            entranceTimer.Tick += AdvanceEntrance;
            Shown += (_, __) => StartEntrance();
        }

        private void SelectPlatform(object sender, ExportPlatform platform) => SetSelected(platform);

        private void ConfirmPlatform(object sender, ExportPlatform platform)
        {
            SetSelected(platform);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void SetSelected(ExportPlatform platform)
        {
            SelectedPlatform = platform;
            unityCard.IsSelected = platform == ExportPlatform.Unity;
            unrealCard.IsSelected = platform == ExportPlatform.Unreal;
        }

        private void StartEntrance()
        {
            if (!SystemInformation.IsMenuAnimationEnabled)
            {
                Opacity = 1D;
                return;
            }

            targetLocation = Location;
            Location = new Point(Location.X, Location.Y + 12);
            animationFrame = 0;
            entranceTimer.Start();
        }

        private void AdvanceEntrance(object sender, EventArgs e)
        {
            animationFrame++;
            double progress = Math.Min(1D, animationFrame / 11D);
            double eased = 1D - Math.Pow(1D - progress, 3D);
            Opacity = eased;
            Location = new Point(targetLocation.X, targetLocation.Y + (int)Math.Round(12D * (1D - eased)));
            if (progress < 1D)
                return;

            entranceTimer.Stop();
            Opacity = 1D;
            Location = targetLocation;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                entranceTimer?.Dispose();
            base.Dispose(disposing);
        }

        private sealed class EngineCard : Panel
        {
            private readonly Image logo;
            private bool selected;
            private bool hovered;

            public ExportPlatform Platform { get; }
            public event EventHandler<ExportPlatform> Selected;
            public event EventHandler<ExportPlatform> Confirmed;

            public bool IsSelected
            {
                get => selected;
                set
                {
                    selected = value;
                    Invalidate();
                }
            }

            public EngineCard(ExportPlatform platform, string title, string logoResourceName, bool tintForDarkTheme)
            {
                Platform = platform;
                logo = LoadLogo(logoResourceName, tintForDarkTheme);
                Dock = DockStyle.Fill;
                Cursor = Cursors.Hand;
                AccessibleName = title;
                AccessibleRole = AccessibleRole.PushButton;
                TabStop = true;
                Padding = new Padding(18);
                BackColor = UITheme.Surface;
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint |
                    ControlStyles.StandardClick |
                    ControlStyles.StandardDoubleClick,
                    true);

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    BackColor = Color.Transparent,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

                var logoSurface = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Padding = new Padding(34, 24, 34, 20),
                    Margin = Padding.Empty
                };
                var logoBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Image = logo,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    TabStop = false,
                    AccessibleName = title + " 로고"
                };
                logoSurface.Controls.Add(logoBox);

                var titleLabel = new Label
                {
                    Text = title,
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI Semibold", 12F, FontStyle.Regular, GraphicsUnit.Point),
                    ForeColor = UITheme.TextPrimary,
                    TextAlign = ContentAlignment.BottomCenter,
                    Margin = Padding.Empty
                };

                layout.Controls.Add(logoSurface, 0, 0);
                layout.Controls.Add(titleLabel, 0, 1);
                Controls.Add(layout);
                Wire(layout);
                MouseEnter += (_, __) => SetHovered(true);
                MouseLeave += (_, __) => SetHovered(false);
                Click += (_, __) =>
                {
                    Focus();
                    Selected?.Invoke(this, Platform);
                };
                DoubleClick += (_, __) => Confirmed?.Invoke(this, Platform);
                KeyDown += (_, e) =>
                {
                    if (e.KeyCode == Keys.Space)
                    {
                        Selected?.Invoke(this, Platform);
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Enter)
                    {
                        Confirmed?.Invoke(this, Platform);
                        e.Handled = true;
                    }
                };
                GotFocus += (_, __) => Invalidate();
                LostFocus += (_, __) => Invalidate();
            }

            private static Image LoadLogo(string resourceFileName, bool tintForDarkTheme)
            {
                Assembly assembly = typeof(EngineSelectionForm).Assembly;
                string resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith(resourceFileName, StringComparison.OrdinalIgnoreCase));
                if (resourceName == null)
                    return null;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (var source = stream == null ? null : new Bitmap(stream))
                {
                    if (source == null)
                        return null;
                    if (!tintForDarkTheme || !UITheme.IsDarkMode)
                        return new Bitmap(source);
                    return CreateTintedLogo(source, UITheme.TextPrimary);
                }
            }

            private static Bitmap CreateTintedLogo(Image source, Color color)
            {
                var result = new Bitmap(
                    source.Width,
                    source.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics graphics = Graphics.FromImage(result))
                using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                {
                    var matrix = new System.Drawing.Imaging.ColorMatrix
                    {
                        Matrix00 = 0F,
                        Matrix11 = 0F,
                        Matrix22 = 0F,
                        Matrix33 = 1F,
                        Matrix40 = color.R / 255F,
                        Matrix41 = color.G / 255F,
                        Matrix42 = color.B / 255F,
                        Matrix44 = 1F
                    };
                    attributes.SetColorMatrix(matrix);
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.DrawImage(
                        source,
                        new Rectangle(0, 0, result.Width, result.Height),
                        0,
                        0,
                        source.Width,
                        source.Height,
                        GraphicsUnit.Pixel,
                        attributes);
                }
                return result;
            }

            private void Wire(Control control)
            {
                control.Click += (_, __) =>
                {
                    Focus();
                    Selected?.Invoke(this, Platform);
                };
                control.DoubleClick += (_, __) => Confirmed?.Invoke(this, Platform);
                control.MouseEnter += (_, __) => SetHovered(true);
                foreach (Control child in control.Controls)
                    Wire(child);
            }

            private void SetHovered(bool value)
            {
                hovered = value;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle bounds = new Rectangle(1, 1, Width - 3, Height - 3);
                using (GraphicsPath path = Rounded(bounds, 13))
                using (var border = new Pen(
                    selected || Focused ? UITheme.Accent : hovered ? UITheme.BorderStrong : UITheme.Border,
                    selected ? 2F : 1F))
                    e.Graphics.DrawPath(border, path);

                if (!selected)
                    return;

                using (var fill = new SolidBrush(UITheme.Accent))
                    e.Graphics.FillEllipse(fill, Width - 30, 12, 16, 16);
                using (var check = new Pen(Color.White, 2F))
                {
                    e.Graphics.DrawLine(check, Width - 26, 20, Width - 23, 23);
                    e.Graphics.DrawLine(check, Width - 23, 23, Width - 18, 17);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    logo?.Dispose();
                base.Dispose(disposing);
            }

            private static GraphicsPath Rounded(Rectangle bounds, int radius)
            {
                int diameter = radius * 2;
                var path = new GraphicsPath();
                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}