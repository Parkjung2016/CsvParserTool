using System;
using System.Drawing;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal sealed class FirstRunWelcomeForm : Form
    {
        private readonly System.Windows.Forms.Timer entranceTimer;
        private Point targetLocation;
        private int animationFrame;

        public FirstRunWelcomeForm()
        {
            Text = "PJDev Data Tool 시작하기";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(640, 560);
            Padding = new Padding(30, 26, 30, 24);
            Font = UITheme.FontUI;
            BackColor = UITheme.AppBackground;
            ForeColor = UITheme.TextPrimary;
            Opacity = AnimationsEnabled ? 0D : 1D;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = UITheme.AppBackground
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var mascot = new MascotPresenter
            {
                Size = new Size(158, 158),
                Margin = new Padding(0, 0, 0, 6),
                Anchor = AnchorStyles.None,
                AccessibleName = "Data Tool 마스코트"
            };
            mascot.SetSequence(
                MascotPose.Hello,
                MascotPose.Point,
                MascotPose.Hello,
                MascotPose.Celebrate);            var title = new Label
            {
                AutoSize = true,
                Text = "처음 오셨군요!",
                Font = UITheme.FontTitle,
                ForeColor = UITheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 7),
                Anchor = AnchorStyles.None
            };
            var subtitle = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(560, 0),
                Text = "사용 안내를 먼저 확인하면 테이블을 더 빠르고 정확하게 만들 수 있습니다.",
                ForeColor = UITheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 20),
                Anchor = AnchorStyles.None
            };

            var guideCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = UITheme.Surface,
                Padding = new Padding(18, 13, 18, 13),
                Margin = new Padding(0, 0, 0, 16)
            };
            guideCard.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            guideCard.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            guideCard.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
            AddGuideRow(guideCard, 0, "1", "XLSX 기본 구조와 타입 작성 규칙");
            AddGuideRow(guideCard, 1, "2", "테이블 참조·배열·Enum 입력 방법");
            AddGuideRow(guideCard, 2, "3", "선택 Export와 오류 확인 방법");

            var note = new Label
            {
                AutoSize = true,
                Text = "안내는 오른쪽 위 i 버튼에서 언제든 다시 열 수 있습니다.",
                ForeColor = UITheme.TextMuted,
                Margin = new Padding(0, 0, 0, 18),
                Anchor = AnchorStyles.None
            };

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty
            };
            var guideButton = new Button
            {
                Text = "사용 안내 보기",
                AutoSize = true,
                DialogResult = DialogResult.Yes,
                Padding = new Padding(18, 5, 18, 5),
                Margin = Padding.Empty
            };
            var continueButton = new Button
            {
                Text = "바로 시작",
                AutoSize = true,
                DialogResult = DialogResult.No,
                Padding = new Padding(16, 5, 16, 5),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.StylePrimaryButton(guideButton);
            UITheme.StyleSecondaryButton(continueButton);
            actions.Controls.Add(guideButton);
            actions.Controls.Add(continueButton);

            root.Controls.Add(mascot, 0, 0);
            root.Controls.Add(title, 0, 1);
            root.Controls.Add(subtitle, 0, 2);
            root.Controls.Add(guideCard, 0, 3);
            root.Controls.Add(note, 0, 4);
            root.Controls.Add(actions, 0, 5);
            Controls.Add(root);

            AcceptButton = guideButton;
            CancelButton = continueButton;
            Shown += (_, __) => StartEntranceAnimation();
            entranceTimer = new System.Windows.Forms.Timer { Interval = 15 };
            entranceTimer.Tick += (_, __) => AdvanceEntranceAnimation();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                entranceTimer?.Dispose();
            base.Dispose(disposing);
        }

        private static void AddGuideRow(TableLayoutPanel table, int row, string number, string text)
        {
            var line = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(0, 3, 0, 3),
                BackColor = UITheme.Surface
            };
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var marker = new Label
            {
                Text = number,
                Size = new Size(26, 26),
                BackColor = UITheme.SurfaceMuted,
                ForeColor = UITheme.Accent,
                Font = UITheme.FontUIMedium,
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Left
            };
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = UITheme.TextPrimary,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 3, 0, 0)
            };
            line.Controls.Add(marker, 0, 0);
            line.Controls.Add(label, 1, 0);
            table.Controls.Add(line, 0, row);
        }

        private static bool AnimationsEnabled => SystemInformation.IsMenuAnimationEnabled && UITheme.CurrentTheme == AppTheme.Default;

        private void StartEntranceAnimation()
        {
            if (!AnimationsEnabled)
            {
                Opacity = 1D;
                return;
            }
            targetLocation = Location;
            Location = new Point(Location.X, Location.Y + 14);
            animationFrame = 0;
            entranceTimer.Start();
        }

        private void AdvanceEntranceAnimation()
        {
            animationFrame++;
            double progress = Math.Min(1D, animationFrame / 12D);
            double eased = 1D - Math.Pow(1D - progress, 3D);
            Opacity = eased;
            Location = new Point(targetLocation.X, targetLocation.Y + (int)Math.Round(14D * (1D - eased)));
            if (progress < 1D)
                return;
            entranceTimer.Stop();
            Opacity = 1D;
            Location = targetLocation;
        }
    }
}
