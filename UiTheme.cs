using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CSVParserTool
{
    /// <summary>라이트/다크 테마 + 공통 컨트롤 스타일.</summary>
    internal static class UiTheme
    {
        public static bool IsDarkMode { get; private set; }

        public static Color AppBackground { get; private set; }
        public static Color Surface { get; private set; }
        public static Color SurfaceMuted { get; private set; }
        public static Color HeaderBackground { get; private set; }
        public static Color Border { get; private set; }
        public static Color BorderStrong { get; private set; }

        public static Color TextPrimary { get; private set; }
        public static Color TextSecondary { get; private set; }
        public static Color TextMuted { get; private set; }
        public static Color TextOnAccent { get; private set; }

        public static Color Accent { get; private set; }
        public static Color AccentHover { get; private set; }
        public static Color AccentPressed { get; private set; }

        public static Color PreviewBackground { get; private set; }
        public static Color LogBackground { get; private set; }
        public static Color LogHeaderBackground { get; private set; }
        public static Color LogText { get; private set; }
        public static Color LogInfo { get; private set; }
        public static Color LogWarning { get; private set; }
        public static Color LogError { get; private set; }

        public static readonly Font FontUi = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontUiMedium = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontTitle = new Font("Segoe UI Semibold", 13F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontSubtitle = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontSection = new Font("Segoe UI Semibold", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontMono = new Font("Cascadia Mono", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontMonoFallback = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

        static UiTheme()
        {
            SetDarkMode(false);
        }

        public static void SetDarkMode(bool dark)
        {
            IsDarkMode = dark;
            if (dark)
            {
                AppBackground = Color.FromArgb(24, 24, 27);
                Surface = Color.FromArgb(39, 39, 42);
                SurfaceMuted = Color.FromArgb(31, 31, 35);
                HeaderBackground = Color.FromArgb(33, 33, 38);
                Border = Color.FromArgb(63, 63, 70);
                BorderStrong = Color.FromArgb(82, 82, 91);

                TextPrimary = Color.FromArgb(244, 244, 245);
                TextSecondary = Color.FromArgb(212, 212, 216);
                TextMuted = Color.FromArgb(161, 161, 170);

                PreviewBackground = Color.FromArgb(30, 30, 30);
                LogBackground = Color.FromArgb(18, 18, 20);
                LogHeaderBackground = Color.FromArgb(24, 24, 27);
                LogText = Color.FromArgb(228, 228, 231);
                LogInfo = Color.FromArgb(161, 161, 170);
            }
            else
            {
                AppBackground = Color.FromArgb(243, 244, 246);
                Surface = Color.White;
                SurfaceMuted = Color.FromArgb(249, 250, 251);
                HeaderBackground = Color.White;
                Border = Color.FromArgb(229, 231, 235);
                BorderStrong = Color.FromArgb(209, 213, 219);

                TextPrimary = Color.FromArgb(17, 24, 39);
                TextSecondary = Color.FromArgb(75, 85, 99);
                TextMuted = Color.FromArgb(156, 163, 175);

                PreviewBackground = Color.FromArgb(250, 250, 250);
                LogBackground = Color.FromArgb(24, 24, 27);
                LogHeaderBackground = Color.FromArgb(24, 24, 27);
                LogText = Color.FromArgb(228, 228, 231);
                LogInfo = Color.FromArgb(161, 161, 170);
            }

            TextOnAccent = Color.White;
            Accent = Color.FromArgb(37, 99, 235);
            AccentHover = Color.FromArgb(29, 78, 216);
            AccentPressed = Color.FromArgb(30, 64, 175);
            LogWarning = Color.FromArgb(251, 191, 36);
            LogError = Color.FromArgb(248, 113, 113);
        }

        public static Color LogColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Warning: return LogWarning;
                case LogLevel.Error: return LogError;
                default: return LogInfo;
            }
        }

        internal static void StyleChromePanel(Panel panel, bool accent = false)
        {
            panel.BackColor = accent ? HeaderBackground : SurfaceMuted;
            panel.Padding = accent
                ? new Padding(20, 16, 20, 12)
                : new Padding(16, 12, 16, 12);
        }

        internal static void StyleSurfacePanel(Panel panel)
        {
            panel.BackColor = Surface;
            panel.Padding = new Padding(12, 10, 12, 10);
        }

        internal static void StylePreviewPanel(Panel panel)
        {
            panel.BackColor = PreviewBackground;
            panel.Padding = new Padding(10, 8, 10, 8);
        }

        internal static void StyleLogPanel(Panel panel)
        {
            panel.BackColor = LogBackground;
            panel.Padding = new Padding(12, 10, 12, 10);
        }

        internal static void StyleSectionLabel(Label label)
        {
            label.Font = FontSection;
            label.ForeColor = TextSecondary;
            label.AutoSize = false;
            label.Height = 24;
        }

        internal static void StyleCaptionLabel(Label label)
        {
            label.Font = FontUiMedium;
            label.ForeColor = TextSecondary;
        }

        internal static void StylePathLabel(Label label)
        {
            label.Font = FontMono;
            label.ForeColor = TextSecondary;
            label.BackColor = Surface;
            label.Padding = new Padding(10, 6, 10, 6);
            label.BorderStyle = BorderStyle.FixedSingle;
        }

        internal static void StyleTextField(TextBox textBox)
        {
            textBox.Font = FontUi;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = Surface;
            textBox.ForeColor = TextPrimary;
            textBox.Height = 28;
        }

        internal static void StyleCombo(ComboBox combo)
        {
            combo.Font = FontUi;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = Surface;
            combo.ForeColor = TextPrimary;
        }

        internal static void StyleCheckBox(CheckBox checkBox)
        {
            checkBox.Font = FontUi;
            checkBox.ForeColor = TextSecondary;
            checkBox.BackColor = Color.Transparent;
            checkBox.AutoSize = true;
        }

        internal static void StyleList(ListBox listBox)
        {
            listBox.Font = FontUiMedium;
            listBox.BorderStyle = BorderStyle.None;
            listBox.BackColor = Surface;
            listBox.ForeColor = TextPrimary;
            listBox.IntegralHeight = false;
            listBox.ItemHeight = 26;
        }

        internal static void StylePreviewBox(RichTextBox box)
        {
            try
            {
                box.Font = FontMono;
            }
            catch
            {
                box.Font = FontMonoFallback;
            }

            box.BorderStyle = BorderStyle.None;
            box.BackColor = PreviewBackground;
            box.ForeColor = TextPrimary;
            box.ReadOnly = true;
        }

        internal static void StyleLogBox(RichTextBox box)
        {
            box.Font = FontMonoFallback;
            box.BorderStyle = BorderStyle.None;
            box.BackColor = LogBackground;
            box.ForeColor = LogText;
        }

        internal static void StylePrimaryButton(Button button, bool tall = false)
        {
            WirePrimaryHover(button);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Accent;
            button.ForeColor = TextOnAccent;
            button.Font = FontUiMedium;
            button.Cursor = Cursors.Hand;
            button.Padding = new Padding(12, tall ? 8 : 4, 12, tall ? 8 : 4);
            button.MinimumSize = new Size(0, tall ? 36 : 30);
        }

        public static void UpdatePathLabel(Label label, string path, string emptyText = "경로 없음")
        {
            bool hasPath = !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
            label.Text = hasPath ? path : emptyText;
            label.ForeColor = hasPath ? TextSecondary : TextMuted;
            label.BackColor = Surface;
        }

        internal static void StyleSecondaryButton(Button button)
        {
            WireSecondaryHover(button);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = BorderStrong;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = Surface;
            button.ForeColor = TextPrimary;
            button.Font = FontUi;
            button.Cursor = Cursors.Hand;
            button.Padding = new Padding(10, 4, 10, 4);
            button.MinimumSize = new Size(0, 30);
        }

        private static void WirePrimaryHover(Button button)
        {
            if (ReferenceEquals(button.Tag, "ui_primary"))
                return;

            button.Tag = "ui_primary";
            button.MouseEnter += (_, __) => button.BackColor = AccentHover;
            button.MouseLeave += (_, __) => button.BackColor = Accent;
            button.MouseDown += (_, __) => button.BackColor = AccentPressed;
            button.MouseUp += (_, __) => button.BackColor = AccentHover;
        }

        private static void WireSecondaryHover(Button button)
        {
            if (ReferenceEquals(button.Tag, "ui_secondary"))
                return;

            button.Tag = "ui_secondary";
            button.MouseEnter += (_, __) =>
                button.BackColor = IsDarkMode ? Color.FromArgb(52, 52, 58) : SurfaceMuted;
            button.MouseLeave += (_, __) => button.BackColor = Surface;
        }
    }
}
