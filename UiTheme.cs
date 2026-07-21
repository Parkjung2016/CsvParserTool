using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal enum AppTheme
    {
        Default,
        Grassland,
        Ocean
    }

    /// <summary>라이트/다크 테마 + 공통 컨트롤 스타일.</summary>
    internal static class UiTheme
    {
        public static bool IsDarkMode { get; private set; }
        public static AppTheme CurrentTheme { get; private set; }

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
        public static Color LogSuccess { get; private set; }
        public static Color StatusPending { get; private set; }
        public static Color StatusRunning { get; private set; }
        public static Color DepthHighlight { get; private set; }
        public static Color DepthShadow { get; private set; }

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

        public static AppTheme ParseTheme(string value) =>
            Enum.TryParse(value, true, out AppTheme parsed) ? parsed : AppTheme.Default;

        public static void SetTheme(AppTheme theme, bool dark)
        {
            CurrentTheme = theme;
            SetDarkMode(dark);
        }

        public static void SetDarkMode(bool dark)
        {
            IsDarkMode = dark;
            switch (CurrentTheme)
            {
                case AppTheme.Grassland:
                    ApplyGrasslandPalette(dark);
                    break;
                case AppTheme.Ocean:
                    ApplyOceanPalette(dark);
                    break;
                default:
                    ApplyDefaultPalette(dark);
                    break;
            }

            TextOnAccent = Color.White;
            LogWarning = Color.FromArgb(251, 191, 36);
            LogError = Color.FromArgb(248, 113, 113);
            LogSuccess = Color.FromArgb(74, 222, 128);
            StatusPending = TextMuted;
            StatusRunning = Accent;
        }

        private static void ApplyDefaultPalette(bool dark)
        {
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
                DepthHighlight = Color.FromArgb(72, 72, 78);
                DepthShadow = Color.FromArgb(12, 12, 14);
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
                DepthHighlight = Color.White;
                DepthShadow = Color.FromArgb(205, 210, 218);
            }
            Accent = Color.FromArgb(37, 99, 235);
            AccentHover = Color.FromArgb(29, 78, 216);
            AccentPressed = Color.FromArgb(30, 64, 175);
        }

        private static void ApplyGrasslandPalette(bool dark)
        {
            if (dark)
            {
                AppBackground = Color.FromArgb(19, 30, 23);
                Surface = Color.FromArgb(31, 46, 36);
                SurfaceMuted = Color.FromArgb(25, 38, 30);
                HeaderBackground = Color.FromArgb(24, 43, 31);
                Border = Color.FromArgb(55, 78, 62);
                BorderStrong = Color.FromArgb(78, 105, 84);
                TextPrimary = Color.FromArgb(239, 247, 240);
                TextSecondary = Color.FromArgb(197, 216, 201);
                TextMuted = Color.FromArgb(139, 164, 145);
                PreviewBackground = Color.FromArgb(18, 29, 22);
                LogBackground = Color.FromArgb(12, 21, 16);
                LogHeaderBackground = Color.FromArgb(17, 29, 22);
                LogText = Color.FromArgb(224, 237, 226);
                LogInfo = Color.FromArgb(143, 171, 150);
                DepthHighlight = Color.FromArgb(75, 103, 82);
                DepthShadow = Color.FromArgb(9, 17, 12);
                Accent = Color.FromArgb(75, 164, 99);
                AccentHover = Color.FromArgb(62, 145, 84);
                AccentPressed = Color.FromArgb(48, 119, 68);
            }
            else
            {
                AppBackground = Color.FromArgb(239, 245, 235);
                Surface = Color.FromArgb(252, 254, 249);
                SurfaceMuted = Color.FromArgb(244, 249, 240);
                HeaderBackground = Color.FromArgb(232, 243, 226);
                Border = Color.FromArgb(202, 219, 196);
                BorderStrong = Color.FromArgb(171, 197, 166);
                TextPrimary = Color.FromArgb(29, 48, 34);
                TextSecondary = Color.FromArgb(67, 91, 72);
                TextMuted = Color.FromArgb(119, 143, 123);
                PreviewBackground = Color.FromArgb(248, 252, 246);
                LogBackground = Color.FromArgb(20, 33, 24);
                LogHeaderBackground = Color.FromArgb(27, 43, 31);
                LogText = Color.FromArgb(229, 241, 231);
                LogInfo = Color.FromArgb(150, 178, 155);
                DepthHighlight = Color.White;
                DepthShadow = Color.FromArgb(175, 195, 169);
                Accent = Color.FromArgb(62, 139, 79);
                AccentHover = Color.FromArgb(49, 119, 66);
                AccentPressed = Color.FromArgb(38, 96, 52);
            }
        }

        private static void ApplyOceanPalette(bool dark)
        {
            if (dark)
            {
                AppBackground = Color.FromArgb(13, 27, 43);
                Surface = Color.FromArgb(24, 45, 64);
                SurfaceMuted = Color.FromArgb(18, 36, 54);
                HeaderBackground = Color.FromArgb(15, 38, 58);
                Border = Color.FromArgb(43, 75, 96);
                BorderStrong = Color.FromArgb(61, 101, 124);
                TextPrimary = Color.FromArgb(236, 248, 250);
                TextSecondary = Color.FromArgb(190, 216, 222);
                TextMuted = Color.FromArgb(125, 162, 174);
                PreviewBackground = Color.FromArgb(12, 25, 39);
                LogBackground = Color.FromArgb(8, 18, 29);
                LogHeaderBackground = Color.FromArgb(12, 28, 43);
                LogText = Color.FromArgb(221, 239, 243);
                LogInfo = Color.FromArgb(126, 171, 181);
                DepthHighlight = Color.FromArgb(57, 94, 115);
                DepthShadow = Color.FromArgb(5, 13, 22);
                Accent = Color.FromArgb(22, 170, 188);
                AccentHover = Color.FromArgb(14, 145, 165);
                AccentPressed = Color.FromArgb(12, 117, 138);
            }
            else
            {
                AppBackground = Color.FromArgb(234, 246, 249);
                Surface = Color.FromArgb(249, 253, 254);
                SurfaceMuted = Color.FromArgb(240, 249, 251);
                HeaderBackground = Color.FromArgb(225, 243, 247);
                Border = Color.FromArgb(190, 219, 225);
                BorderStrong = Color.FromArgb(153, 198, 207);
                TextPrimary = Color.FromArgb(22, 49, 60);
                TextSecondary = Color.FromArgb(56, 88, 99);
                TextMuted = Color.FromArgb(109, 146, 157);
                PreviewBackground = Color.FromArgb(245, 251, 252);
                LogBackground = Color.FromArgb(13, 29, 42);
                LogHeaderBackground = Color.FromArgb(17, 38, 54);
                LogText = Color.FromArgb(225, 242, 246);
                LogInfo = Color.FromArgb(137, 177, 187);
                DepthHighlight = Color.White;
                DepthShadow = Color.FromArgb(160, 199, 207);
                Accent = Color.FromArgb(15, 145, 165);
                AccentHover = Color.FromArgb(10, 121, 143);
                AccentPressed = Color.FromArgb(8, 96, 116);
            }
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
            bool dimensional = CurrentTheme != AppTheme.Default;
            panel.Padding = dimensional
                ? accent ? new Padding(24, 18, 28, 20) : new Padding(20, 14, 24, 20)
                : accent ? new Padding(20, 16, 20, 12) : new Padding(16, 12, 16, 12);
            Theme3DRenderer.AttachPanel(panel, accent);
        }

        internal static void StyleSurfacePanel(Panel panel)
        {
            panel.BackColor = Surface;
            panel.Padding = CurrentTheme == AppTheme.Default
                ? new Padding(12, 10, 12, 10)
                : new Padding(14, 12, 20, 20);
            Theme3DRenderer.AttachPanel(panel, accent: false);
        }

        internal static void StylePreviewPanel(Panel panel)
        {
            panel.BackColor = PreviewBackground;
            panel.Padding = CurrentTheme == AppTheme.Default
                ? new Padding(10, 8, 10, 8)
                : new Padding(14, 12, 20, 20);
            Theme3DRenderer.AttachPanel(panel, accent: false);
        }

        internal static void StyleLogPanel(Panel panel)
        {
            panel.BackColor = LogBackground;
            panel.Padding = CurrentTheme == AppTheme.Default
                ? new Padding(12, 10, 12, 10)
                : new Padding(14, 12, 20, 20);
            Theme3DRenderer.AttachPanel(panel, accent: false);
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
            label.BorderStyle = CurrentTheme == AppTheme.Default ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
        }

        internal static void StyleTextField(TextBox textBox)
        {
            textBox.Font = FontUi;
            textBox.BorderStyle = CurrentTheme == AppTheme.Default ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
            textBox.BackColor = Surface;
            textBox.ForeColor = TextPrimary;
            textBox.Height = 28;
        }

        internal static void StyleCombo(ComboBox combo)
        {
            combo.Font = FontUi;
            combo.FlatStyle = CurrentTheme == AppTheme.Default ? FlatStyle.Flat : FlatStyle.Standard;
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

        internal static void StyleProgressBar(ProgressBar bar)
        {
            bar.Style = ProgressBarStyle.Continuous;
            bar.Minimum = 0;
            bar.Maximum = 100;
            bar.Value = 0;
            bar.Height = 18;
        }

        internal static void StyleExportListView(ListView listView)
        {
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listView.BorderStyle = BorderStyle.None;
            listView.BackColor = Surface;
            listView.ForeColor = TextPrimary;
            listView.Font = FontUi;
            listView.GridLines = true;
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
            button.MinimumSize = new Size(0, CurrentTheme == AppTheme.Default ? (tall ? 36 : 30) : (tall ? 46 : 38));
            Theme3DRenderer.AttachButton(button, primary: true);
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
            button.MinimumSize = new Size(0, CurrentTheme == AppTheme.Default ? 30 : 38);
            Theme3DRenderer.AttachButton(button, primary: false);
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
