using System;
using System.Drawing;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal sealed class ThemeSelectionForm : Form
    {
        private readonly ThemeCard[] cards;
        private readonly System.Windows.Forms.Timer entranceTimer;
        private Point targetLocation;
        private int animationFrame;

        public AppTheme SelectedTheme { get; private set; }

        public ThemeSelectionForm(AppTheme currentTheme)
        {
            SelectedTheme = currentTheme;
            Text = "테마 선택";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(790, 520);
            Padding = new Padding(26, 22, 26, 22);
            Font = UiTheme.FontUi;
            BackColor = UiTheme.AppBackground;
            ForeColor = UiTheme.TextPrimary;
            Opacity = AnimationsEnabled ? 0D : 1D;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = UiTheme.AppBackground
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label
            {
                AutoSize = true,
                Text = "작업 공간 테마",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                Margin = new Padding(0, 0, 0, 5)
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Text = "색상과 분위기를 선택하세요. 다크 모드는 선택한 테마에 맞춰 함께 적용됩니다.",
                ForeColor = UiTheme.TextMuted,
                Margin = new Padding(0, 0, 0, 20)
            };

            var cardLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = UiTheme.AppBackground
            };
            for (int i = 0; i < 3; i++)
                cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));

            cards = new[]
            {
                new ThemeCard(AppTheme.Default, "기본 테마", "깔끔하고 익숙한 기본 작업 환경"),
                new ThemeCard(AppTheme.Grassland, "초원 테마", "따뜻한 초록빛 3D 데이터 작업장"),
                new ThemeCard(AppTheme.Ocean, "바다 테마", "시원한 청록빛 3D 해양 연구소")
            };
            for (int i = 0; i < cards.Length; i++)
            {
                ThemeCard card = cards[i];
                card.Margin = new Padding(i == 0 ? 0 : 7, 0, i == cards.Length - 1 ? 0 : 7, 0);
                card.ThemeSelected += (_, theme) => SelectTheme(theme);
                cardLayout.Controls.Add(card, i, 0);
            }

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0, 18, 0, 0)
            };
            var applyButton = new Button
            {
                Text = "이 테마 적용",
                AutoSize = true,
                DialogResult = DialogResult.OK,
                Padding = new Padding(18, 5, 18, 5),
                Margin = Padding.Empty
            };
            var cancelButton = new Button
            {
                Text = "취소",
                AutoSize = true,
                DialogResult = DialogResult.Cancel,
                Padding = new Padding(16, 5, 16, 5),
                Margin = new Padding(0, 0, 8, 0)
            };
            UiTheme.StylePrimaryButton(applyButton);
            UiTheme.StyleSecondaryButton(cancelButton);
            actions.Controls.Add(applyButton);
            actions.Controls.Add(cancelButton);

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(subtitle, 0, 1);
            root.Controls.Add(cardLayout, 0, 2);
            root.Controls.Add(actions, 0, 3);
            Controls.Add(root);
            AcceptButton = applyButton;
            CancelButton = cancelButton;
            SelectTheme(currentTheme);

            entranceTimer = new System.Windows.Forms.Timer { Interval = 15 };
            entranceTimer.Tick += (_, __) => AdvanceEntranceAnimation();
            Shown += (_, __) => StartEntranceAnimation();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                entranceTimer?.Dispose();
            base.Dispose(disposing);
        }

        private void SelectTheme(AppTheme theme)
        {
            SelectedTheme = theme;
            foreach (ThemeCard card in cards)
                card.Selected = card.Theme == theme;
        }

        private static bool AnimationsEnabled => SystemInformation.IsMenuAnimationEnabled && UiTheme.CurrentTheme == AppTheme.Default;

        private void StartEntranceAnimation()
        {
            if (!AnimationsEnabled)
            {
                Opacity = 1D;
                return;
            }
            targetLocation = Location;
            Location = new Point(Location.X, Location.Y + 12);
            animationFrame = 0;
            entranceTimer.Start();
        }

        private void AdvanceEntranceAnimation()
        {
            animationFrame++;
            double progress = Math.Min(1D, animationFrame / 12D);
            double eased = 1D - Math.Pow(1D - progress, 3D);
            Opacity = eased;
            Location = new Point(targetLocation.X, targetLocation.Y + (int)Math.Round(12D * (1D - eased)));
            if (progress < 1D)
                return;
            entranceTimer.Stop();
            Opacity = 1D;
            Location = targetLocation;
        }

        private sealed class ThemeCard : Panel
        {
            private bool selected;
            private bool hovered;
            private readonly Bitmap artwork;

            public AppTheme Theme { get; }
            public event EventHandler<AppTheme> ThemeSelected;

            public bool Selected
            {
                get => selected;
                set { selected = value; Invalidate(); }
            }

            public ThemeCard(AppTheme theme, string title, string description)
            {
                Theme = theme;
                AccessibleName = title;
                AccessibleDescription = description;
                AccessibleRole = AccessibleRole.PushButton;
                TabStop = true;
                Dock = DockStyle.Fill;
                BackColor = UiTheme.Surface;
                Cursor = Cursors.Hand;
                Padding = new Padding(10);
                artwork = ThemeArtwork.Load(theme);

                var image = new PictureBox
                {
                    Dock = DockStyle.Top,
                    Height = 235,
                    Image = artwork,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = theme == AppTheme.Default ? Color.FromArgb(24, 31, 48) : UiTheme.SurfaceMuted,
                    Cursor = Cursors.Hand
                };
                var titleLabel = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 34,
                    Text = title,
                    Font = UiTheme.FontUiMedium,
                    ForeColor = UiTheme.TextPrimary,
                    TextAlign = ContentAlignment.BottomLeft,
                    Cursor = Cursors.Hand
                };
                var descriptionLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = description,
                    ForeColor = UiTheme.TextMuted,
                    TextAlign = ContentAlignment.TopLeft,
                    Padding = new Padding(0, 5, 0, 0),
                    Cursor = Cursors.Hand
                };
                Controls.Add(descriptionLabel);
                Controls.Add(titleLabel);
                Controls.Add(image);
                WireClick(this);
                MouseEnter += (_, __) => SetHovered(true);
                MouseLeave += (_, __) => SetHovered(false);
                KeyDown += (_, e) =>
                {
                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                        ThemeSelected?.Invoke(this, Theme);
                };
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    artwork?.Dispose();
                base.Dispose(disposing);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Color border = selected ? UiTheme.Accent : hovered ? UiTheme.BorderStrong : UiTheme.Border;
                int width = selected ? 3 : 1;
                using (var pen = new Pen(border, width))
                    e.Graphics.DrawRectangle(pen, width / 2, width / 2, Width - width, Height - width);
                if (Theme != AppTheme.Default)
                {
                    using (var shadow = new Pen(UiTheme.DepthShadow, 3))
                    {
                        e.Graphics.DrawLine(shadow, 5, Height - 3, Width - 3, Height - 3);
                        e.Graphics.DrawLine(shadow, Width - 3, 5, Width - 3, Height - 3);
                    }
                }
            }

            private void WireClick(Control control)
            {
                control.Click += (_, __) => ThemeSelected?.Invoke(this, Theme);
                control.MouseEnter += (_, __) => SetHovered(true);
                foreach (Control child in control.Controls)
                    WireClick(child);
            }

            private void SetHovered(bool value)
            {
                hovered = value;
                BackColor = value ? UiTheme.SurfaceMuted : UiTheme.Surface;
                Invalidate();
            }
        }
    }
}
