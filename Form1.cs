using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Taskbar;
using CSVParserTool.MiniGames;

namespace CSVParserTool
{
    public enum LogLevel
    {
        Info,
        /// <summary>앱은 동작하지만 이 동작만 건너뜀·참고 수준.</summary>
        Warning,
        /// <summary>실패·조건 불충분. 메시지 박스로도 알림.</summary>
        Error
    }

    public partial class Form1 : Form
    {
        private const int maxLogLines = 300;
        private readonly Button Btn_Info = new Button();
        private readonly Button Btn_Version = new Button();
        private readonly Button Btn_Theme = new Button();
        private readonly Button Btn_ExportSelected = new Button();
        private readonly Button Btn_EnumCatalog = new Button();
        private readonly Button Btn_CheckAll = new Button();
        private readonly Button Btn_UncheckAll = new Button();
        private readonly SplitContainer splitExportAndLog = new SplitContainer();
        private bool exportResultSplitterInitialized;

        private readonly HashSet<string> checkedXlsxPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool allowCheckStateChange;
        private Icon taskbarExportStatusIcon;
        private NotifyIcon exportNotifyIcon;
        private System.Windows.Forms.Timer exportNotificationHideTimer;
        private System.Windows.Forms.Timer exportCompletionAnimationTimer;
        private int exportCompletionAnimationFrame;
        private Color exportCompletionAnimationStartColor;
        private string projectRootPath = "";
        private string excelSourceFolderPath = "";
        private string exportVersion = "1.0.0";

        private string dataCsvDir =>
            string.IsNullOrWhiteSpace(projectRootPath)
                ? string.Empty
                : DataProjectPaths.DataCsvDir(projectRootPath);

        private string gameDatasDir =>
            string.IsNullOrWhiteSpace(projectRootPath)
                ? string.Empty
                : DataProjectPaths.GameDatasDir(projectRootPath);

        private FileSystemWatcher excelDirWatcher;
        private System.Windows.Forms.Timer listReloadDebounceTimer;

        private string currentSelectedXlsxPath = "";
        private readonly List<FileListEntry> allListEntries = new List<FileListEntry>();
        private readonly Dictionary<string, string> previewCacheByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private int previewRequestVersion;
        private CancellationTokenSource previewCancellation;
        private readonly SemaphoreSlim previewGenerationLock = new SemaphoreSlim(1, 1);
        private string currentPreviewCode = "";
        private int themeContentRefreshVersion;

        private sealed class FileListEntry
        {
            public string FullPath { get; set; }
            public string DisplayName { get; set; }
        }

        private List<LogEntry> allLogs = new List<LogEntry>();
        private LogLevel? currentFilter = null;
        private bool splitContainersInitialized;
        private bool startupDialogsScheduled;
        private readonly Dictionary<string, ListViewItem> exportResultItems =
            new Dictionary<string, ListViewItem>(StringComparer.OrdinalIgnoreCase);

        private bool exportExcelPhaseRan;
        private bool exportHasTableFailure;
        private int exportActivePhaseIndex = -1;
        private ExportMiniGameForm exportMiniGameForm;

        public Form1()
        {
            InitializeComponent();
            InitializeExportLogSplitter();
            InitializeInfoButton();

            bool darkMode = ToolSettingsStore.DarkMode;
            UiTheme.SetTheme(UiTheme.ParseTheme(ToolSettingsStore.ThemeName), darkMode);
            Chk_DarkMode.Checked = darkMode;

            ApplyUiTheme();
            ApplyAppIcon();
        }

        private void InitializeExportLogSplitter()
        {
            Panel_LogSection.SuspendLayout();
            try
            {
                splitExportAndLog.Name = "Split_ExportAndLog";
                splitExportAndLog.Dock = DockStyle.Fill;
                splitExportAndLog.Orientation = Orientation.Horizontal;
                splitExportAndLog.FixedPanel = FixedPanel.Panel1;
                splitExportAndLog.IsSplitterFixed = false;
                splitExportAndLog.Panel1MinSize = 82;
                splitExportAndLog.Panel2MinSize = 54;
                splitExportAndLog.SplitterWidth = 6;
                splitExportAndLog.TabStop = false;

                Panel_ExportProgress.Dock = DockStyle.Fill;
                Panel_LogHeader.Dock = DockStyle.Top;
                Panel_LogCard.Dock = DockStyle.Fill;
                splitExportAndLog.Panel1.Controls.Add(Panel_ExportProgress);
                splitExportAndLog.Panel2.Controls.Add(Panel_LogCard);
                splitExportAndLog.Panel2.Controls.Add(Panel_LogHeader);
                Panel_LogSection.Controls.Add(splitExportAndLog);
                splitExportAndLog.Panel1Collapsed = !Panel_ExportProgress.Visible;
            }
            finally
            {
                Panel_LogSection.ResumeLayout(true);
            }
        }

        private void ShowExportResultsPanel()
        {
            Panel_ExportProgress.Visible = true;
            if (splitExportAndLog.Panel1Collapsed)
                splitExportAndLog.Panel1Collapsed = false;
        }

        private void InitializeExportResultSplitterDistance()
        {
            if (exportResultSplitterInitialized || splitExportAndLog.Panel1Collapsed)
                return;

            int maximum = splitExportAndLog.Height
                - splitExportAndLog.Panel2MinSize
                - splitExportAndLog.SplitterWidth;
            if (maximum < splitExportAndLog.Panel1MinSize)
                return;

            splitExportAndLog.SplitterDistance = Math.Max(
                splitExportAndLog.Panel1MinSize,
                Math.Min(168, maximum));
            exportResultSplitterInitialized = true;
        }

        private void InitializeInfoButton()
        {
            tableHeader.ColumnCount = 8;
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.SetColumn(Chk_DarkMode, 5);
            tableHeader.SetColumn(Btn_DataSetting, 7);
            Btn_DataSetting.Text = "전체 Export";

            Btn_Version.Name = "Btn_Version";
            Btn_Version.Text = "버전";
            Btn_Version.AccessibleName = "툴 버전 및 업데이트 정보";
            Btn_Version.Anchor = AnchorStyles.Right;
            Btn_Version.AutoSize = true;
            Btn_Version.Margin = new Padding(0, 0, 8, 0);
            Btn_Version.TabIndex = 2;
            Btn_Version.Click += Btn_Version_Click;
            tableHeader.Controls.Add(Btn_Version, 2, 0);
            tableHeader.SetRowSpan(Btn_Version, 2);

            Btn_Info.Name = "Btn_Info";
            Btn_Info.Text = "i";
            Btn_Info.AccessibleName = "사용 안내";
            Btn_Info.Anchor = AnchorStyles.Right;
            Btn_Info.Margin = new Padding(0, 0, 12, 0);
            Btn_Info.TabIndex = 2;
            Btn_Info.Click += Btn_Info_Click;
            tableHeader.Controls.Add(Btn_Info, 3, 0);
            tableHeader.SetRowSpan(Btn_Info, 2);
            Btn_Theme.Name = "Btn_Theme";
            Btn_Theme.Text = "테마";
            Btn_Theme.AccessibleName = "작업 공간 테마 선택";
            Btn_Theme.Anchor = AnchorStyles.Right;
            Btn_Theme.AutoSize = true;
            Btn_Theme.Margin = new Padding(0, 0, 8, 0);
            Btn_Theme.TabIndex = 2;
            Btn_Theme.Click += Btn_Theme_Click;
            tableHeader.Controls.Add(Btn_Theme, 4, 0);
            tableHeader.SetRowSpan(Btn_Theme, 2);

            Btn_ExportSelected.Name = "Btn_ExportSelected";
            Btn_ExportSelected.Text = "선택 Export";
            Btn_ExportSelected.AccessibleName = "체크한 테이블만 Export";
            Btn_ExportSelected.Anchor = AnchorStyles.Right;
            Btn_ExportSelected.AutoSize = true;
            Btn_ExportSelected.Margin = new Padding(0, 0, 8, 0);
            Btn_ExportSelected.TabIndex = 3;
            Btn_ExportSelected.Click += Btn_ExportSelected_Click;
            tableHeader.Controls.Add(Btn_ExportSelected, 6, 0);
            tableHeader.SetRowSpan(Btn_ExportSelected, 2);

            var listHeaderLayout = new TableLayoutPanel
            {
                Name = "Table_ListHeaderActions",
                ColumnCount = 3,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            listHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            listHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            listHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            listHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            listHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var selectionLayout = new TableLayoutPanel
            {
                Name = "Table_ListSelectionActions",
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            selectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            selectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            selectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Btn_EnumCatalog.Name = "Btn_EnumCatalog";
            Btn_EnumCatalog.Text = "Enum XLSX";
            Btn_EnumCatalog.AccessibleName = "Enum 관리 XLSX 생성 또는 열기";
            Btn_EnumCatalog.AutoSize = true;
            Btn_EnumCatalog.Dock = DockStyle.Fill;
            Btn_EnumCatalog.Margin = new Padding(0, 0, 4, 0);
            Btn_EnumCatalog.Padding = new Padding(6, 2, 6, 2);
            Btn_EnumCatalog.TabIndex = 1;
            Btn_EnumCatalog.Click += Btn_EnumCatalog_Click;

            Btn_CheckAll.Name = "Btn_CheckAll";
            Btn_CheckAll.Text = "\uC804\uCCB4 \uC120\uD0DD";
            Btn_CheckAll.AccessibleName = "\uBAA8\uB4E0 \uD14C\uC774\uBE14 \uCCB4\uD06C";
            Btn_CheckAll.Dock = DockStyle.Fill;
            Btn_CheckAll.Margin = new Padding(0, 4, 4, 0);
            Btn_CheckAll.TabIndex = 2;
            Btn_CheckAll.Click += Btn_CheckAll_Click;

            Btn_UncheckAll.Name = "Btn_UncheckAll";
            Btn_UncheckAll.Text = "\uC804\uCCB4 \uD574\uC81C";
            Btn_UncheckAll.AccessibleName = "\uBAA8\uB4E0 \uD14C\uC774\uBE14 \uCCB4\uD06C \uD574\uC81C";
            Btn_UncheckAll.Dock = DockStyle.Fill;
            Btn_UncheckAll.Margin = new Padding(0, 4, 0, 0);
            Btn_UncheckAll.TabIndex = 3;
            Btn_UncheckAll.Click += Btn_UncheckAll_Click;
            ListBox_CsvFiles.MouseUp += ListBox_CsvFiles_MouseUp;

            Panel_ListHeader.Height = 80;
            Label_SectionList.Dock = DockStyle.Fill;
            Label_SectionList.Padding = new Padding(2, 0, 0, 0);
            Panel_ListHeader.Controls.Remove(Btn_RefreshList);
            Panel_ListHeader.Controls.Remove(Label_SectionList);
            Btn_RefreshList.Margin = Padding.Empty;
            Btn_RefreshList.Dock = DockStyle.Fill;
            listHeaderLayout.Controls.Add(Label_SectionList, 0, 0);
            listHeaderLayout.Controls.Add(Btn_EnumCatalog, 1, 0);
            listHeaderLayout.Controls.Add(Btn_RefreshList, 2, 0);
            selectionLayout.Controls.Add(Btn_CheckAll, 0, 0);
            selectionLayout.Controls.Add(Btn_UncheckAll, 1, 0);
            listHeaderLayout.Controls.Add(selectionLayout, 0, 1);
            listHeaderLayout.SetColumnSpan(selectionLayout, 3);
            Panel_ListHeader.Controls.Add(listHeaderLayout);
            Activated += Form1_Activated;
            FormClosed += Form1_FormClosed;
        }

        private bool versionDialogOpen;
        private bool versionDialogShownThisSession;

        private void Btn_Version_Click(object sender, EventArgs e) => ShowVersionDialog();

        private void ShowVersionDialog(bool captureOwner = true)
        {
            if (versionDialogOpen || IsDisposed)
                return;

            versionDialogOpen = true;
            versionDialogShownThisSession = true;
            try
            {
                using (var dialog = new ToolVersionForm())
                {
                    if (Icon != null)
                        dialog.Icon = (Icon)Icon.Clone();
                    ModalBlurBackdrop.ShowDialog(this, dialog, captureOwner);
                }
            }
            finally
            {
                versionDialogOpen = false;
            }
        }
        private void Btn_Theme_Click(object sender, EventArgs e)
        {
            using (var dialog = new ThemeSelectionForm(UiTheme.CurrentTheme))
            {
                if (Icon != null)
                    dialog.Icon = (Icon)Icon.Clone();
                if (ModalBlurBackdrop.ShowDialog(this, dialog) != DialogResult.OK)
                    return;

                UiTheme.SetTheme(dialog.SelectedTheme, Chk_DarkMode.Checked);
                ToolSettingsStore.ThemeName = dialog.SelectedTheme.ToString();
                ToolSettingsStore.Save();
                ApplyUiTheme();
            }
        }
        private void Btn_Info_Click(object sender, EventArgs e)
        {
            using (var dialog = new ToolInfoForm())
            {
                if (Icon != null)
                    dialog.Icon = (Icon)Icon.Clone();
                ModalBlurBackdrop.ShowDialog(this, dialog);
            }
        }

        private void ApplyAppIcon()
        {
            TrySetFormIcon();
            TrySetHeaderIcon();
        }

        private void ApplyThemeHeaderIcon()
        {
            // Main header always uses the stable app icon. Theme artwork is only shown in the picker.
            TrySetHeaderIcon();
        }
        private void TrySetFormIcon()
        {
            try
            {
                string iconPath = ResolveAppIconPath();
                if (!string.IsNullOrEmpty(iconPath))
                    Icon = Icon.ExtractAssociatedIcon(iconPath);
            }
            catch
            {
                // exe 아이콘 로드 실패 시 무시
            }
        }

        private void TrySetHeaderIcon()
        {
            try
            {
                using (Bitmap bitmap = LoadHeaderIconBitmap())
                {
                    if (bitmap == null)
                        return;

                    Image old = PictureBox_HeaderIcon.Image;
                    PictureBox_HeaderIcon.Image = new Bitmap(bitmap);
                    old?.Dispose();
                }
            }
            catch
            {
                // 헤더 아이콘 로드 실패 시 무시
            }
        }

        private static string ResolveAppIconPath() =>
            Application.ExecutablePath;

        private static Bitmap LoadHeaderIconBitmap()
        {
            Assembly asm = typeof(Form1).Assembly;
            foreach (string name in asm.GetManifestResourceNames())
            {
                if (!name.EndsWith("pjdev-icon-source-square.png", StringComparison.OrdinalIgnoreCase))
                    continue;

                using (var stream = asm.GetManifestResourceStream(name))
                {
                    if (stream != null)
                        return new Bitmap(stream);
                }
            }

            string iconPath = ResolveAppIconPath();
            if (!File.Exists(iconPath))
                return null;

            using (var icon = new Icon(iconPath, 32, 32))
            using (var temp = icon.ToBitmap())
                return new Bitmap(temp);
        }

        private void Chk_DarkMode_CheckedChanged(object sender, EventArgs e)
        {
            bool darkMode = Chk_DarkMode.Checked;
            UiTheme.SetTheme(UiTheme.ParseTheme(ToolSettingsStore.ThemeName), darkMode);
            ToolSettingsStore.DarkMode = darkMode;
            ToolSettingsStore.Save();

            ApplyUiTheme();
        }

        private void SetPreviewCode(string code)
        {
            currentPreviewCode = code ?? string.Empty;
            CSharpPreviewHighlighter.Apply(TextBox_Preview, currentPreviewCode, UiTheme.IsDarkMode);
        }

        private void ApplyUiTheme()
        {
            if (!IsHandleCreated)
            {
                ApplyUiThemeCore();
                return;
            }

            SuspendLayout();
            SendMessage(Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
            try
            {
                ApplyUiThemeCore();
                SetPreviewCode(currentPreviewCode);
                RefreshLogDisplay();
                StyleExportResultRows();
            }
            finally
            {
                ResumeLayout(true);
                LayoutSplitContainers();
                SendMessage(Handle, WmSetRedraw, new IntPtr(1), IntPtr.Zero);
                Invalidate(true);
                Update();
            }

            QueueThemeContentRefresh();
        }

        private void QueueThemeContentRefresh()
        {
            int version = ++themeContentRefreshVersion;
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed || Disposing || version != themeContentRefreshVersion)
                    return;

                SetPreviewCode(currentPreviewCode);
                RefreshLogDisplay();
                StyleExportResultRows();
            }));
        }
        private void ApplyUiThemeCore()
        {
            BackColor = UiTheme.AppBackground;
            Font = UiTheme.FontUi;
            ForeColor = UiTheme.TextPrimary;
            Text = "PJDev Data Tool";

            UiTheme.StyleChromePanel(Panel_Header, accent: true);
            UiTheme.StyleChromePanel(Panel_Top);
            UiTheme.StyleChromePanel(Panel_Bottom);

            Label_AppTitle.Font = UiTheme.FontTitle;
            Label_AppTitle.ForeColor = UiTheme.Accent;
            Label_AppSubtitle.Font = UiTheme.FontSubtitle;
            Label_AppSubtitle.ForeColor = UiTheme.TextMuted;

            UiTheme.StyleCheckBox(Chk_DarkMode);
            Chk_DarkMode.BackColor = Color.Transparent;

            PictureBox_HeaderIcon.BackColor = Color.Transparent;
            tableHeader.BackColor = Color.Transparent;
            tableTop.BackColor = Color.Transparent;
            tableBottom.BackColor = Color.Transparent;
            ApplyThemeHeaderIcon();

            UiTheme.StyleSectionLabel(Label_SectionList);
            UiTheme.StyleSectionLabel(Label_SectionPreview);
            Panel_ListHeader.BackColor = UiTheme.AppBackground;
            Label_SectionLog.Font = UiTheme.FontSection;
            Label_SectionLog.ForeColor = UiTheme.LogInfo;
            Label_SectionLog.AutoSize = true;
            UiTheme.StyleCaptionLabel(Label_CsvFilter);

            UiTheme.StyleSurfacePanel(Panel_ListCard);
            UiTheme.StylePreviewPanel(Panel_PreviewCard);
            UiTheme.StyleLogPanel(Panel_LogCard);
            Panel_LogHeader.BackColor = UiTheme.LogHeaderBackground;

            UiTheme.StylePrimaryButton(Btn_DataSetting, tall: true);
            UiTheme.StyleSecondaryButton(Btn_ExportSelected);
            UiTheme.StyleSecondaryButton(Btn_EnumCatalog);
            UiTheme.StyleSecondaryButton(Btn_Info);
            UiTheme.StyleSecondaryButton(Btn_Version);
            UiTheme.StyleSecondaryButton(Btn_Theme);
            Btn_Info.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Regular, GraphicsUnit.Point);
            Btn_Info.Padding = Padding.Empty;
            Btn_Info.MinimumSize = UiTheme.CurrentTheme == AppTheme.Default ? new Size(34, 34) : new Size(40, 40);
            UiTheme.StyleSecondaryButton(Btn_CheckAll);
            UiTheme.StyleSecondaryButton(Btn_UncheckAll);
            Btn_Info.Size = UiTheme.CurrentTheme == AppTheme.Default ? new Size(34, 34) : new Size(40, 40);
            UiTheme.StyleSecondaryButton(Btn_SelectProjectRoot);
            UiTheme.StyleSecondaryButton(Btn_SelectExcelFolder);
            UiTheme.StyleSecondaryButton(Btn_OpenOutputFolder);
            UiTheme.StyleSecondaryButton(Btn_OpenCsvFolder);
            UiTheme.StyleSecondaryButton(Btn_OpenXlsxFolder);
            UiTheme.StyleSecondaryButton(Btn_NewCsv);
            UiTheme.StyleSecondaryButton(Btn_RefreshList);
            UiTheme.StyleSecondaryButton(Btn_ClearLog);

            UiTheme.StylePathLabel(Label_ProjectRoot);
            UiTheme.StylePathLabel(Label_ExcelSourcePath);
            UiTheme.StyleTextField(Txt_CsvFilter);
            UiTheme.StyleTextField(TextBox_NewCsvName);
            UiTheme.StyleTextField(Txt_ExportVersion);
            UiTheme.StyleCheckBox(Chk_RemoveOrphanArtifacts);
            UiTheme.StyleCombo(Combo_LogFilter);
            UiTheme.StyleList(ListBox_CsvFiles);
            UiTheme.StylePreviewBox(TextBox_Preview);
            UiTheme.StyleLogBox(TextBox_Log);
            UiTheme.StyleExportListView(ListView_ExportResults);
            UiTheme.StyleSectionLabel(Label_SectionExport);
            Panel_ExportProgress.BackColor = UiTheme.SurfaceMuted;
            Panel_ExportProgressTop.BackColor = UiTheme.SurfaceMuted;
            SegmentedExportProgress_Export.BackColor = UiTheme.SurfaceMuted;
            Label_ExportStatus.Font = UiTheme.FontUiMedium;
            Label_ExportStatus.ForeColor = UiTheme.TextPrimary;
            Label_ExportStatus.BackColor = UiTheme.SurfaceMuted;
            SegmentedExportProgress_Export.Invalidate();
            exportMiniGameForm?.ApplyTheme();

            splitOuter.BackColor = UiTheme.Border;
            splitOuter.Panel1.BackColor = UiTheme.AppBackground;
            splitOuter.Panel2.BackColor = UiTheme.AppBackground;
            splitWork.BackColor = UiTheme.Border;
            splitWork.Panel1.BackColor = UiTheme.AppBackground;
            splitWork.Panel2.BackColor = UiTheme.AppBackground;
            splitExportAndLog.BackColor = UiTheme.Border;
            splitExportAndLog.Panel1.BackColor = UiTheme.AppBackground;
            splitExportAndLog.Panel2.BackColor = UiTheme.AppBackground;
            Panel_LogSection.BackColor = UiTheme.AppBackground;
            Panel_MainContent.BackColor = UiTheme.AppBackground;
            ApplyDimensionalLayout();
        }

        private void ApplyDimensionalLayout()
        {
            bool dimensional = UiTheme.CurrentTheme != AppTheme.Default;
            MinimumSize = dimensional ? new Size(1000, 700) : new Size(920, 580);
            Panel_Header.MinimumSize = dimensional ? new Size(0, 84) : Size.Empty;
            Panel_Top.MinimumSize = new Size(0, dimensional ? 188 : 166);
            Panel_Bottom.MinimumSize = dimensional ? new Size(0, 70) : Size.Empty;
            splitOuter.Panel1MinSize = dimensional ? 150 : 180;
            splitOuter.Panel2MinSize = dimensional ? 140 : 180;
            splitWork.Panel1MinSize = dimensional ? 220 : 200;
            splitWork.Panel2MinSize = dimensional ? 300 : 280;
            Panel_MainContent.Padding = dimensional
                ? new Padding(18, 12, 18, 14)
                : new Padding(12, 4, 12, 4);
            splitWork.SplitterWidth = dimensional ? 12 : 6;
            splitOuter.SplitterWidth = dimensional ? 12 : 6;
            splitExportAndLog.SplitterWidth = dimensional ? 10 : 6;
            splitWork.Panel1.Padding = dimensional ? new Padding(0, 0, 10, 0) : new Padding(0, 0, 8, 0);
            splitWork.Panel2.Padding = dimensional ? new Padding(10, 0, 0, 0) : new Padding(8, 0, 0, 0);
            Panel_ListHeader.Height = dimensional ? 88 : 80;
            Panel_LogHeader.Height = dimensional ? 38 : 32;
            Label_SectionPreview.Height = dimensional ? 32 : 28;
        }
        private void StartExportCompletionAnimation(bool success)
        {
            StopExportCompletionAnimation(resetColors: true);
            if (!SystemInformation.IsMenuAnimationEnabled || UiTheme.CurrentTheme != AppTheme.Default)
                return;

            exportCompletionAnimationFrame = 0;
            exportCompletionAnimationStartColor = BlendColor(
                UiTheme.SurfaceMuted,
                success ? UiTheme.LogSuccess : UiTheme.LogError,
                0.42D);
            ApplyExportCompletionAnimationColor(exportCompletionAnimationStartColor);
            if (exportCompletionAnimationTimer == null)
            {
                exportCompletionAnimationTimer = new System.Windows.Forms.Timer { Interval = 30 };
                exportCompletionAnimationTimer.Tick += (_, __) => AdvanceExportCompletionAnimation();
            }
            exportCompletionAnimationTimer.Start();
        }

        private void AdvanceExportCompletionAnimation()
        {
            exportCompletionAnimationFrame++;
            double progress = Math.Min(1D, exportCompletionAnimationFrame / 18D);
            double eased = 1D - Math.Pow(1D - progress, 3D);
            ApplyExportCompletionAnimationColor(BlendColor(
                exportCompletionAnimationStartColor,
                UiTheme.SurfaceMuted,
                eased));
            if (progress >= 1D)
                StopExportCompletionAnimation(resetColors: true);
        }

        private void StopExportCompletionAnimation(bool resetColors)
        {
            exportCompletionAnimationTimer?.Stop();
            if (resetColors)
                ApplyExportCompletionAnimationColor(UiTheme.SurfaceMuted);
        }

        private void ApplyExportCompletionAnimationColor(Color color)
        {
            Panel_ExportProgressTop.BackColor = color;
            Label_ExportStatus.BackColor = color;
            SegmentedExportProgress_Export.BackColor = color;
            Panel_ExportProgressTop.Invalidate();
        }

        private static Color BlendColor(Color from, Color to, double amount)
        {
            amount = Math.Max(0D, Math.Min(1D, amount));
            return Color.FromArgb(
                (int)Math.Round(from.A + (to.A - from.A) * amount),
                (int)Math.Round(from.R + (to.R - from.R) * amount),
                (int)Math.Round(from.G + (to.G - from.G) * amount),
                (int)Math.Round(from.B + (to.B - from.B) * amount));
        }
        private void ShowExportTaskbarNotification(bool success)
        {
            if (!IsHandleCreated || (ContainsFocus && WindowState != FormWindowState.Minimized))
                return;

            ClearExportTaskbarNotification();
            try
            {
                taskbarExportStatusIcon = CreateTaskbarStatusIcon(success);
                if (TaskbarManager.IsPlatformSupported)
                {
                    TaskbarManager.Instance.SetOverlayIcon(
                        Handle,
                        taskbarExportStatusIcon,
                        success ? "Export 완료" : "Export 실패");
                    TaskbarManager.Instance.SetProgressState(
                        success ? TaskbarProgressBarState.Normal : TaskbarProgressBarState.Error,
                        Handle);
                    TaskbarManager.Instance.SetProgressValue(100, 100, Handle);
                }

                FlashTaskbar(stop: false);
            }
            catch
            {
                // 작업 표시줄 기능을 지원하지 않는 환경에서도 Export 결과에는 영향이 없어야 한다.
            }

            ShowExportWindowsNotification(success);
        }
        private void ClearExportTaskbarNotification()
        {
            try
            {
                if (IsHandleCreated && TaskbarManager.IsPlatformSupported)
                {
                    TaskbarManager.Instance.SetOverlayIcon(Handle, null, null);
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, Handle);
                }
            }
            catch
            {
                // Explorer 재시작 등으로 작업 표시줄 핸들이 바뀐 경우 무시한다.
            }

            FlashTaskbar(stop: true);
            taskbarExportStatusIcon?.Dispose();
            taskbarExportStatusIcon = null;
            HideExportWindowsNotification();
        }

        private void ShowExportWindowsNotification(bool success)
        {
            try
            {
                if (exportNotifyIcon == null)
                {
                    exportNotifyIcon = new NotifyIcon
                    {
                        Text = "PJDev Data Tool",
                        Icon = Icon ?? SystemIcons.Application
                    };
                    exportNotifyIcon.BalloonTipClicked += (_, __) => RestoreFromExportNotification();
                    exportNotifyIcon.Click += (_, __) => RestoreFromExportNotification();
                }

                exportNotifyIcon.Visible = true;
                exportNotifyIcon.ShowBalloonTip(
                    5000,
                    success ? "Data Export 완료" : "Data Export 실패",
                    success
                        ? "데이터 Export가 완료되었습니다."
                        : "Data Tool에서 실패 내용을 확인하세요.",
                    success ? ToolTipIcon.Info : ToolTipIcon.Error);

                if (exportNotificationHideTimer == null)
                {
                    exportNotificationHideTimer = new System.Windows.Forms.Timer { Interval = 10000 };
                    exportNotificationHideTimer.Tick += (_, __) => HideExportWindowsNotification();
                }
                exportNotificationHideTimer.Stop();
                exportNotificationHideTimer.Start();
            }
            catch
            {
                // Windows 알림이 꺼져 있거나 Explorer가 재시작 중인 경우 무시한다.
            }
        }

        private void HideExportWindowsNotification()
        {
            exportNotificationHideTimer?.Stop();
            if (exportNotifyIcon != null)
                exportNotifyIcon.Visible = false;
        }

        private void RestoreFromExportNotification()
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Show();
            Activate();
            BringToFront();
        }
        private static Icon CreateTaskbarStatusIcon(bool success)
        {
            using (var bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var font = new Font("Segoe UI Symbol", success ? 18F : 20F, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(Color.White))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                using (var background = new SolidBrush(success
                    ? Color.FromArgb(46, 125, 50)
                    : Color.FromArgb(198, 40, 40)))
                {
                    graphics.FillEllipse(background, 1, 1, 30, 30);
                }

                graphics.DrawString(success ? "\u2713" : "!", font, brush, new RectangleF(0, -1, 32, 33), format);
                IntPtr iconHandle = bitmap.GetHicon();
                try
                {
                    using (Icon borrowed = Icon.FromHandle(iconHandle))
                        return (Icon)borrowed.Clone();
                }
                finally
                {
                    DestroyIcon(iconHandle);
                }
            }
        }

        private void Form1_Activated(object sender, EventArgs e) =>
            ClearExportTaskbarNotification();

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            ClearExportTaskbarNotification();
            exportNotificationHideTimer?.Dispose();
            exportNotificationHideTimer = null;
            exportCompletionAnimationTimer?.Dispose();
            exportCompletionAnimationTimer = null;
            exportNotifyIcon?.Dispose();
            exportNotifyIcon = null;
            exportMiniGameForm?.Dispose();
            exportMiniGameForm = null;
            previewCancellation?.Cancel();
            previewCancellation = null;
        }

        private void FlashTaskbar(bool stop)
        {
            if (!IsHandleCreated)
                return;

            var info = new FlashWindowInfo
            {
                Size = (uint)Marshal.SizeOf(typeof(FlashWindowInfo)),
                WindowHandle = Handle,
                Flags = stop ? FlashWindowStop : FlashWindowTray | FlashWindowTimerNoForeground,
                Count = stop ? 0U : 3U,
                Timeout = 0U
            };
            FlashWindowEx(ref info);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FlashWindowInfo
        {
            public uint Size;
            public IntPtr WindowHandle;
            public uint Flags;
            public uint Count;
            public uint Timeout;
        }

        private const int WmSetRedraw = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);
        private const uint FlashWindowStop = 0;
        private const uint FlashWindowTray = 0x00000002;
        private const uint FlashWindowTimerNoForeground = 0x0000000C;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FlashWindowInfo info);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr iconHandle);
        private void Form1_Shown(object sender, EventArgs e)
        {
            if (!splitContainersInitialized)
            {
                LayoutSplitContainers();
                splitContainersInitialized = true;
            }

            if (Combo_LogFilter.Items.Count > 0 && Combo_LogFilter.SelectedIndex < 0)
                Combo_LogFilter.SelectedIndex = 0;

            if (!startupDialogsScheduled)
            {
                startupDialogsScheduled = true;
                Application.Idle += ShowStartupDialogsAfterFirstPaint;
            }
        }


        private void ShowStartupDialogsAfterFirstPaint(object sender, EventArgs e)
        {
            Application.Idle -= ShowStartupDialogsAfterFirstPaint;
            if (IsDisposed || Disposing || !Visible)
                return;

            CompleteInitialLayout();
            BeginInvoke(new Action(RunStartupDialogs));
        }

        private void CompleteInitialLayout()
        {
            SuspendLayout();
            try
            {
                PerformLayout();
                Panel_Header.PerformLayout();
                tableHeader.PerformLayout();
                Panel_Top.PerformLayout();
                tableTop.PerformLayout();
                Panel_Bottom.PerformLayout();
                tableBottom.PerformLayout();
                Panel_MainContent.PerformLayout();
                splitOuter.PerformLayout();
                splitWork.PerformLayout();
                LayoutSplitContainers();
            }
            finally
            {
                ResumeLayout(true);
            }

            Invalidate(true);
            Update();
        }
        private async void RunStartupDialogs()
        {
            if (ToolSettingsStore.IsFirstRun)
                ShowFirstRunWelcome();

            if (IsDisposed || Disposing)
                return;

            try
            {
                ToolUpdateInfo update = await ToolUpdateService.CheckAsync(CancellationToken.None);
                if (update?.IsNewer == true && !versionDialogShownThisSession && !IsDisposed && !Disposing)
                    ShowVersionDialog(captureOwner: false);
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                    AddLog("시작 시 업데이트 확인 실패: " + ex.Message, LogLevel.Warning);
            }
        }
        private void ShowFirstRunWelcome()
        {
            using (var welcome = new FirstRunWelcomeForm())
            {
                if (Icon != null)
                    welcome.Icon = (Icon)Icon.Clone();
                DialogResult result = ModalBlurBackdrop.ShowDialog(this, welcome, captureOwner: false);
                if (result != DialogResult.Yes)
                    return;
            }

            using (var guide = new ToolInfoForm())
            {
                if (Icon != null)
                    guide.Icon = (Icon)Icon.Clone();
                ModalBlurBackdrop.ShowDialog(this, guide, captureOwner: false);
            }
        }

        /// <summary>사이드바·미리보기·하단 로그 패널 초기 비율.</summary>
        private void LayoutSplitContainers()
        {
            if (!IsHandleCreated || splitOuter.Width <= 0 || splitWork.Width <= 0)
                return;

            int maxList = splitWork.Width - splitWork.Panel2MinSize - splitWork.SplitterWidth;
            if (maxList >= splitWork.Panel1MinSize)
            {
                int desiredList = Math.Max(260, Math.Min(360, (int)(splitWork.Width * 0.30F)));
                splitWork.SplitterDistance = Math.Min(maxList, Math.Max(splitWork.Panel1MinSize, desiredList));
            }

            int maxWork = splitOuter.Height - splitOuter.Panel2MinSize - splitOuter.SplitterWidth;
            if (maxWork >= splitOuter.Panel1MinSize)
            {
                int desiredLog = Panel_ExportProgress.Visible
                    ? Math.Max(220, Math.Min(320, (int)(splitOuter.Height * 0.42F)))
                    : Math.Max(180, Math.Min(240, (int)(splitOuter.Height * 0.30F)));
                int desiredWork = splitOuter.Height - splitOuter.SplitterWidth - desiredLog;
                splitOuter.SplitterDistance = Math.Min(maxWork, Math.Max(splitOuter.Panel1MinSize, desiredWork));
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!IsHandleCreated || WindowState == FormWindowState.Minimized)
                return;

            LayoutSplitContainers();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            projectRootPath = ToolSettingsStore.ProjectRootPath ?? "";
            excelSourceFolderPath = ToolSettingsStore.ExcelSourceFolderPath ?? "";
            exportVersion = NormalizeExportVersion(ToolSettingsStore.ExportVersion);

            UiTheme.UpdatePathLabel(Label_ProjectRoot, projectRootPath);
            UiTheme.UpdatePathLabel(Label_ExcelSourcePath, excelSourceFolderPath);
            Txt_ExportVersion.Text = exportVersion;
            Chk_RemoveOrphanArtifacts.Checked = ToolSettingsStore.RemoveOrphanArtifactsOnExport;

            ReloadDataFileList();

            AddLog("툴 시작", LogLevel.Info);
            InitDirectoryWatchers();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Application.Idle -= ShowStartupDialogsAfterFirstPaint;
            excelDirWatcher?.Dispose();
            excelDirWatcher = null;
            listReloadDebounceTimer?.Stop();
            listReloadDebounceTimer?.Dispose();
            listReloadDebounceTimer = null;
            base.OnFormClosed(e);
        }

        // =========================
        // 데이터 Export (in-process — 단일 EXE 배포)
        // =========================
        private async void Btn_DataSetting_Click(object sender, EventArgs e)
        {
            await RunDataExportAsync(null);
        }

        private async void Btn_ExportSelected_Click(object sender, EventArgs e)
        {
            string[] selectedStems = GetCheckedTableStems();
            if (selectedStems.Length == 0)
            {
                MessageBox.Show(
                    "테이블 목록 왼쪽에서 Export할 항목을 먼저 체크하세요.",
                    "선택 Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            await RunDataExportAsync(selectedStems);
        }

        private async Task RunDataExportAsync(IReadOnlyCollection<string> selectedTableStems)
        {
            bool selectedOnly = selectedTableStems != null;
            bool refresh = !string.IsNullOrWhiteSpace(excelSourceFolderPath) && Directory.Exists(excelSourceFolderPath);
            if (refresh && (string.IsNullOrWhiteSpace(projectRootPath) || !Directory.Exists(projectRootPath)))
            {
                AddLog("엑셀→CSV 갱신을 쓰려면 먼저 「프로젝트 경로 지정」을 하세요.", LogLevel.Warning);
                MessageBox.Show(
                    "XLSX 원본 폴더를 쓰려면 먼저 「프로젝트 경로 지정」으로 프로젝트 루트를 선택하세요.\n(Assets 폴더가 아니라, 그 상위 프로젝트 폴더입니다.)",
                    "데이터 설정");
                return;
            }

            if (!CheckDataSettingAvailable(out string reason, willRefreshExcelToCsvFirst: refresh))
            {
                AddLog("데이터 설정 조건이 맞지 않습니다.", LogLevel.Error, suppressErrorDialog: true);
                if (!string.IsNullOrWhiteSpace(reason))
                    AddLog(reason.Trim(), LogLevel.Warning);
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(reason)
                        ? "데이터 설정 조건이 맞지 않습니다."
                        : $"데이터 설정 조건이 맞지 않습니다.\n\n{reason.Trim()}",
                    "데이터 설정",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Btn_DataSetting.Enabled = false;
            Btn_ExportSelected.Enabled = false;
            try
            {
                SaveExportVersionSetting();
                BeginExportProgressUi();
                string targetText = selectedOnly ? $"선택 {selectedTableStems.Count}개" : "전체";
                AddLog($"데이터 Export 시작… ({targetText}, 버전 {exportVersion}, 원본 없는 이전 파일 삭제 {(Chk_RemoveOrphanArtifacts.Checked ? "ON" : "OFF")})", LogLevel.Info);

                Task<DataExportResult> exportTask = Task.Run(() => DataExportService.RunExport(
                    projectRootPath,
                    excelSourceFolderPath,
                    refresh,
                    ExportLog,
                    ReportExportProgress,
                    exportVersion: exportVersion,
                    removeOrphanArtifacts: Chk_RemoveOrphanArtifacts.Checked,
                    selectedTableStems: selectedTableStems));

                ShowExportMiniGame();
                DataExportResult result = await exportTask;

                FinishExportProgressUi(result);

                if (result.Ok)
                {
                    AddLog(result.SummaryLines, result.FailedCount > 0 ? LogLevel.Warning : LogLevel.Info);
                }
                else
                {
                    AddLog(result.ErrorMessage ?? "Export failed.", LogLevel.Error, suppressErrorDialog: true);
                    if (result.FailedCount > 0)
                        AddLog(BuildFailureSummaryText(result), LogLevel.Error, suppressErrorDialog: true);
                }
            }
            catch (Exception ex)
            {
                FinishExportProgressUi(null);
                AddLog(ex.Message, LogLevel.Error);
            }
            finally
            {
                Btn_DataSetting.Enabled = true;
                Btn_ExportSelected.Enabled = true;
            }
        }

        private void ShowExportMiniGame()
        {
            try
            {
                if (exportMiniGameForm == null || exportMiniGameForm.IsDisposed)
                {
                    exportMiniGameForm = new ExportMiniGameForm(message => AddLog("미니게임 등록: " + message, LogLevel.Warning));
                    if (Icon != null)
                        exportMiniGameForm.Icon = (Icon)Icon.Clone();
                    exportMiniGameForm.FormClosed += (_, __) => exportMiniGameForm = null;
                }

                exportMiniGameForm.StartExport();
                if (!exportMiniGameForm.Visible)
                    exportMiniGameForm.ShowCentered(this);
                else
                    exportMiniGameForm.BringToFront();
            }
            catch (Exception ex)
            {
                exportMiniGameForm?.Dispose();
                exportMiniGameForm = null;
                AddLog("미니게임을 열지 못했습니다. Export는 계속 진행합니다: " + ex.Message, LogLevel.Warning);
            }
        }

        private void CompleteExportMiniGame(bool success, string message)
        {
            if (exportMiniGameForm == null || exportMiniGameForm.IsDisposed)
                return;
            exportMiniGameForm.CompleteExport(success, message);
        }
        private void BeginExportProgressUi()
        {
            ClearExportTaskbarNotification();
            StopExportCompletionAnimation(resetColors: true);
            exportResultItems.Clear();
            ListView_ExportResults.Items.Clear();
            ShowExportResultsPanel();
            exportExcelPhaseRan = false;
            exportHasTableFailure = false;
            exportActivePhaseIndex = -1;
            Label_ExportStatus.ForeColor = UiTheme.StatusRunning;
            Label_ExportStatus.Text = "Export 준비 중…";
            ResetExportSteps();
            LayoutSplitContainers();
            InitializeExportResultSplitterDistance();
            AdjustExportResultColumns();
        }

        private void FinishExportProgressUi(DataExportResult result)
        {
            if (result == null)
            {
                Label_ExportStatus.ForeColor = UiTheme.LogError;
                Label_ExportStatus.Text = "Export 중단";
                if (exportActivePhaseIndex >= 0)
                    SetExportStep(exportActivePhaseIndex, SegmentedPhaseState.Failed);
                StartExportCompletionAnimation(success: false);
                ShowExportTaskbarNotification(success: false);
                CompleteExportMiniGame(false, Label_ExportStatus.Text);
                return;
            }

            int total = result.TableResults?.Count ?? 0;
            if (!exportExcelPhaseRan)
                SetExportStep(0, SegmentedPhaseState.Skipped, "Excel → CSV (생략)");

            if (total > 0)
            {
                SegmentedExportProgress_Export.SetTableProgress(
                    result.SucceededCount + result.FailedCount,
                    total,
                    result.FailedCount > 0);
            }
            else if (exportExcelPhaseRan && !result.Ok)
            {
                SetExportStep(0, SegmentedPhaseState.Failed);
            }

            if (result.Ok)
                SetExportStep(2, SegmentedPhaseState.Done);
            else if (exportActivePhaseIndex >= 2)
                SetExportStep(2, SegmentedPhaseState.Failed);

            if (result.FailedCount > 0)
            {
                Label_ExportStatus.ForeColor = UiTheme.LogError;
                Label_ExportStatus.Text =
                    $"Export 실패 — 성공 {result.SucceededCount} · 실패 {result.FailedCount} (아래 목록 확인)";
            }
            else if (result.Ok)
            {
                Label_ExportStatus.ForeColor = UiTheme.LogSuccess;
                Label_ExportStatus.Text =
                    total > 0
                        ? $"Export 완료 — {result.SucceededCount}/{total} 테이블"
                        : "Export 완료";
            }
            else
            {
                Label_ExportStatus.ForeColor = UiTheme.LogError;
                Label_ExportStatus.Text = result.ErrorMessage ?? "Export 실패";
            }

            StyleExportResultRows();
            SelectFirstFailedExportRowInternal(result);
            if (result.FailedCount > 0)
                Combo_LogFilter.SelectedItem = "Error";
            LayoutSplitContainers();
            bool exportSucceeded = result.Ok && result.FailedCount == 0;
            StartExportCompletionAnimation(exportSucceeded);
            ShowExportTaskbarNotification(exportSucceeded);
            CompleteExportMiniGame(exportSucceeded, Label_ExportStatus.Text);
        }

        private void ReportExportProgress(DataExportProgressInfo info)
        {
            if (IsDisposed || info == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ReportExportProgress(info)));
                return;
            }

            string miniGameProgress = info.Kind == DataExportProgressKind.TableCompleted
                ? $"테이블 Export {info.CompletedCount}/{info.TotalCount}" +
                  (string.IsNullOrWhiteSpace(info.ItemName) ? "" : $" · {info.ItemName}")
                : info.PhaseLabel;
            exportMiniGameForm?.UpdateExportProgress(miniGameProgress);

            switch (info.Kind)
            {
                case DataExportProgressKind.PhaseChanged:
                    Label_ExportStatus.ForeColor = UiTheme.StatusRunning;
                    Label_ExportStatus.Text = info.PhaseLabel ?? "Export 진행 중…";
                    if (info.PhaseIndex == 0)
                    {
                        exportExcelPhaseRan = true;
                        exportActivePhaseIndex = 0;
                        SetExportStep(0, SegmentedPhaseState.Running);
                    }
                    else if (info.PhaseIndex == 2)
                    {
                        exportActivePhaseIndex = 2;
                        SetExportStep(1, SegmentedPhaseState.Done);
                        SetExportStep(2, SegmentedPhaseState.Running);
                    }
                    break;

                case DataExportProgressKind.TablesStarted:
                    if (exportExcelPhaseRan)
                        SetExportStep(0, SegmentedPhaseState.Done);
                    else
                        SetExportStep(0, SegmentedPhaseState.Skipped, "Excel → CSV (생략)");

                    exportActivePhaseIndex = 1;
                    SetExportStep(1, SegmentedPhaseState.Running);
                    SegmentedExportProgress_Export.SetTableProgress(0, Math.Max(1, info.TotalCount));
                    Label_ExportStatus.ForeColor = UiTheme.StatusRunning;
                    Label_ExportStatus.Text = $"테이블 Export 0/{info.TotalCount}";
                    InitializeExportResultRows(info.PendingItemNames);
                    break;

                case DataExportProgressKind.TableCompleted:
                    if (!info.Success)
                        exportHasTableFailure = true;
                    SegmentedExportProgress_Export.SetTableProgress(
                        Math.Min(info.TotalCount, info.CompletedCount),
                        Math.Max(1, info.TotalCount),
                        exportHasTableFailure);
                    Label_ExportStatus.Text =
                        $"테이블 Export {info.CompletedCount}/{info.TotalCount}" +
                        (string.IsNullOrEmpty(info.ItemName) ? string.Empty : $" — {info.ItemName}");
                    UpdateExportResultRow(info.ItemName, info.Success, info.Message);
                    break;

                case DataExportProgressKind.Finished:
                    exportActivePhaseIndex = info.PhaseIndex;
                    if (info.PhaseIndex == 1)
                    {
                        if (!info.Success)
                        {
                            SegmentedExportProgress_Export.SetTableProgress(
                                Math.Max(0, info.CompletedCount),
                                Math.Max(1, info.TotalCount),
                                hasFailure: true);
                        }
                        else
                        {
                            SetExportStep(1, SegmentedPhaseState.Done);
                        }
                    }
                    else if (info.PhaseIndex == 2)
                    {
                        SetExportStep(2, info.Success ? SegmentedPhaseState.Done : SegmentedPhaseState.Failed);
                    }

                    Label_ExportStatus.ForeColor = info.Success ? UiTheme.LogSuccess : UiTheme.LogError;
                    Label_ExportStatus.Text = info.PhaseLabel ?? (info.Success ? "Export 완료" : "Export 실패");
                    break;
            }
        }

        private void ResetExportSteps()
        {
            SegmentedExportProgress_Export.Reset();
        }

        private void SetExportStep(int stepIndex, SegmentedPhaseState state, string caption = null)
        {
            SegmentedExportProgress_Export.SetPhaseState(stepIndex, state, caption);
        }

        private void ListView_ExportResults_Resize(object sender, EventArgs e)
        {
            AdjustExportResultColumns();
        }

        private void AdjustExportResultColumns()
        {
            if (ListView_ExportResults.Columns.Count < 3)
                return;

            int fixedWidth = ListView_ExportResults.Columns[0].Width + ListView_ExportResults.Columns[1].Width + 8;
            int messageWidth = ListView_ExportResults.ClientSize.Width - fixedWidth;
            ListView_ExportResults.Columns[2].Width = Math.Max(120, messageWidth);
        }

        private void InitializeExportResultRows(IReadOnlyList<string> itemNames)
        {
            ListView_ExportResults.BeginUpdate();
            ListView_ExportResults.Items.Clear();
            exportResultItems.Clear();

            if (itemNames != null)
            {
                foreach (string name in itemNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var item = new ListViewItem(name);
                    item.SubItems.Add("대기");
                    item.SubItems.Add(string.Empty);
                    item.ForeColor = UiTheme.StatusPending;
                    ListView_ExportResults.Items.Add(item);
                    exportResultItems[name] = item;
                }
            }

            ListView_ExportResults.EndUpdate();
        }

        private void UpdateExportResultRow(string itemName, bool success, string message)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return;

            if (!exportResultItems.TryGetValue(itemName, out ListViewItem item))
            {
                item = new ListViewItem(itemName);
                item.SubItems.Add(success ? "성공" : "실패");
                item.SubItems.Add(message ?? string.Empty);
                item.ForeColor = success ? UiTheme.LogSuccess : UiTheme.LogError;
                ListView_ExportResults.Items.Add(item);
                exportResultItems[itemName] = item;
                return;
            }

            if (item.SubItems.Count < 2)
                item.SubItems.Add(success ? "성공" : "실패");
            else
                item.SubItems[1].Text = success ? "성공" : "실패";

            if (item.SubItems.Count < 3)
                item.SubItems.Add(message ?? string.Empty);
            else
                item.SubItems[2].Text = message ?? string.Empty;

            item.ForeColor = success ? UiTheme.LogSuccess : UiTheme.LogError;
        }

        private void StyleExportResultRows()
        {
            foreach (ListViewItem item in ListView_ExportResults.Items)
            {
                string status = item.SubItems.Count > 1 ? item.SubItems[1].Text : string.Empty;
                if (status == "성공")
                    item.ForeColor = UiTheme.LogSuccess;
                else if (status == "실패")
                    item.ForeColor = UiTheme.LogError;
                else
                    item.ForeColor = UiTheme.StatusPending;
            }
        }

        private void SelectFirstFailedExportRowInternal(DataExportResult result)
        {
            if (result?.TableResults == null)
                return;

            foreach (DataExportTableResult row in result.TableResults)
            {
                if (row.Success)
                    continue;

                if (exportResultItems.TryGetValue(row.SourceFileName, out ListViewItem item))
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        private static string BuildFailureSummaryText(DataExportResult result)
        {
            if (result?.TableResults == null || result.FailedCount == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("실패한 테이블:");
            foreach (DataExportTableResult row in result.TableResults.Where(r => !r.Success))
            {
                sb.Append("· ");
                sb.Append(row.SourceFileName);
                if (!string.IsNullOrWhiteSpace(row.ErrorMessage))
                {
                    sb.Append(": ");
                    sb.Append(row.ErrorMessage);
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private void ExportLog(string message)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ExportLog(message)));
                return;
            }

            AddLog(message, LogLevel.Info);
        }

        /// <summary>입력은 테이블 이름(확장자 없음). 파일명으로 쓸 수 있게 정리하고, 없으면 앞에 DT_.</summary>
        private static string BuildDtPrefixedTableBaseName(string raw)
        {
            string name = Path.GetFileNameWithoutExtension(raw.Trim());
            if (string.IsNullOrEmpty(name))
                return null;

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c.ToString(), string.Empty);

            if (string.IsNullOrEmpty(name))
                return null;

            const string prefix = "DT_";
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                name = prefix + name;

            return name;
        }

        // =========================
        // CSV / XLSX 목록 · 감시
        // =========================
        private void RebuildAllListEntries()
        {
            allListEntries.Clear();

            if (!string.IsNullOrWhiteSpace(excelSourceFolderPath) && Directory.Exists(excelSourceFolderPath))
            {
                foreach (var f in Directory.GetFiles(excelSourceFolderPath, "*.xlsx"))
                {
                    if (Path.GetFileName(f).StartsWith("~$", StringComparison.Ordinal))
                        continue;
                    allListEntries.Add(new FileListEntry
                    {
                        FullPath = f,
                        DisplayName = Path.GetFileName(f)
                    });
                }
            }

            allListEntries.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshListBoxFromFilter()
        {
            string filter = Txt_CsvFilter.Text.Trim().ToLowerInvariant();
            checkedXlsxPaths.RemoveWhere(path => !File.Exists(path));

            allowCheckStateChange = true;
            ListBox_CsvFiles.BeginUpdate();
            try
            {
                ListBox_CsvFiles.Items.Clear();
                foreach (var entry in allListEntries)
                {
                    if (filter.Length == 0 || entry.DisplayName.ToLowerInvariant().Contains(filter))
                    {
                        int index = ListBox_CsvFiles.Items.Add(entry.DisplayName);
                        if (checkedXlsxPaths.Contains(entry.FullPath))
                            ListBox_CsvFiles.SetItemChecked(index, true);
                    }
                }
            }
            finally
            {
                ListBox_CsvFiles.EndUpdate();
                allowCheckStateChange = false;
            }
        }
        private string[] GetCheckedTableStems() =>
            checkedXlsxPaths
                .Where(File.Exists)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(stem => !string.IsNullOrWhiteSpace(stem))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(stem => stem, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        private void Btn_CheckAll_Click(object sender, EventArgs e)
        {
            foreach (FileListEntry entry in allListEntries)
                checkedXlsxPaths.Add(entry.FullPath);

            RefreshListBoxFromFilter();
        }

        private void Btn_UncheckAll_Click(object sender, EventArgs e)
        {
            checkedXlsxPaths.Clear();
            RefreshListBoxFromFilter();
        }

        /// <param name="quietLog">파일 감시 등으로 잦이 갱신될 때 로그 생략</param>
        private void ReloadDataFileList(bool quietLog = false)
        {
            string previousPath = currentSelectedXlsxPath;
            RebuildAllListEntries();
            RefreshListBoxFromFilter();
            RestoreListSelection(previousPath);

            if (!quietLog)
            {
                int nXlsx = allListEntries.Count;
                if (nXlsx == 0)
                    AddLog("목록에 XLSX가 없습니다. 「XLSX 원본 지정」 경로를 확인하세요.", LogLevel.Warning);
                else
                    AddLog($"목록: XLSX {nXlsx}개", LogLevel.Info);
            }
        }

        private void RestoreListSelection(string preferredPath)
        {
            if (string.IsNullOrWhiteSpace(preferredPath))
                return;

            FileListEntry entry = allListEntries.FirstOrDefault(e =>
                string.Equals(e.FullPath, preferredPath, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                if (!File.Exists(preferredPath))
                {
                    currentSelectedXlsxPath = string.Empty;
                    SetPreviewCode(string.Empty);
                }

                return;
            }

            int idx = ListBox_CsvFiles.Items.IndexOf(entry.DisplayName);
            if (idx < 0)
                return;

            currentSelectedXlsxPath = entry.FullPath;
            if (ListBox_CsvFiles.SelectedIndex != idx)
                ListBox_CsvFiles.SelectedIndex = idx;
            else
                RefreshPreviewFromXlsx(entry.FullPath);
        }

        private static void SetFolderDialogInitialPath(CommonOpenFileDialog dialog, string currentPath)
        {
            if (string.IsNullOrWhiteSpace(currentPath))
                return;

            try
            {
                string full = Path.GetFullPath(currentPath);
                if (Directory.Exists(full))
                {
                    dialog.DefaultDirectory = full;
                    return;
                }

                string parent = Path.GetDirectoryName(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                    dialog.DefaultDirectory = parent;
            }
            catch
            {
                /* ignore */
            }
        }

        private void InitDirectoryWatchers()
        {
            excelDirWatcher?.Dispose();
            excelDirWatcher = null;

            if (!string.IsNullOrWhiteSpace(excelSourceFolderPath) && Directory.Exists(excelSourceFolderPath))
            {
                excelDirWatcher = new FileSystemWatcher(excelSourceFolderPath, "*.xlsx")
                {
                    NotifyFilter = NotifyFilters.FileName
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.LastWrite
                        | NotifyFilters.Size
                        | NotifyFilters.CreationTime,
                    IncludeSubdirectories = false
                };
                excelDirWatcher.Changed += OnExcelDirChanged;
                excelDirWatcher.Created += OnExcelDirChanged;
                excelDirWatcher.Deleted += OnExcelDirChanged;
                excelDirWatcher.Renamed += OnExcelDirRenamed;
                excelDirWatcher.EnableRaisingEvents = true;
            }
        }

        private void OnExcelDirChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnoreWatcherPath(e.FullPath))
                return;

            if (e.ChangeType == WatcherChangeTypes.Deleted
                && !string.IsNullOrEmpty(currentSelectedXlsxPath)
                && string.Equals(currentSelectedXlsxPath, e.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                currentSelectedXlsxPath = string.Empty;
            }

            ScheduleReloadDataFileList();
        }

        private void OnExcelDirRenamed(object sender, RenamedEventArgs e)
        {
            if (ShouldIgnoreWatcherPath(e.OldFullPath) && ShouldIgnoreWatcherPath(e.FullPath))
                return;

            if (!string.IsNullOrEmpty(currentSelectedXlsxPath)
                && string.Equals(currentSelectedXlsxPath, e.OldFullPath, StringComparison.OrdinalIgnoreCase))
            {
                currentSelectedXlsxPath = e.FullPath;
            }

            ScheduleReloadDataFileList();
        }

        private static bool ShouldIgnoreWatcherPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            string name = Path.GetFileName(path);
            return name.StartsWith("~$", StringComparison.Ordinal);
        }

        private void ScheduleReloadDataFileList()
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(ScheduleReloadDataFileList));
                return;
            }

            if (listReloadDebounceTimer == null)
            {
                listReloadDebounceTimer = new System.Windows.Forms.Timer { Interval = 350 };
                listReloadDebounceTimer.Tick += (_, __) =>
                {
                    listReloadDebounceTimer.Stop();
                    ReloadDataFileListSafe();
                };
            }

            listReloadDebounceTimer.Stop();
            listReloadDebounceTimer.Start();
        }

        private void ReloadDataFileListSafe()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ReloadDataFileListSafe));
                return;
            }

            ReloadDataFileList(quietLog: true);
        }

        private void Btn_RefreshList_Click(object sender, EventArgs e)
        {
            ReloadDataFileList();
        }

        // =========================
        // XLSX 목록 선택
        // =========================
        private void ListBox_CsvFiles_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            int index = ListBox_CsvFiles.IndexFromPoint(e.Location);
            if (index == ListBox.NoMatches || index < 0 || index >= ListBox_CsvFiles.Items.Count)
                return;

            Rectangle itemBounds = ListBox_CsvFiles.GetItemRectangle(index);
            int checkRight = itemBounds.Left + SystemInformation.MenuCheckSize.Width + 6;
            if (e.X < itemBounds.Left || e.X > checkRight)
                return;

            allowCheckStateChange = true;
            try
            {
                ListBox_CsvFiles.SetItemChecked(index, !ListBox_CsvFiles.GetItemChecked(index));
            }
            finally
            {
                allowCheckStateChange = false;
            }
        }

        private void ListBox_CsvFiles_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!allowCheckStateChange)
            {
                e.NewValue = e.CurrentValue;
                return;
            }

            if (e.Index < 0 || e.Index >= ListBox_CsvFiles.Items.Count)
                return;

            string displayName = ListBox_CsvFiles.Items[e.Index]?.ToString();
            FileListEntry entry = allListEntries.FirstOrDefault(item =>
                string.Equals(item.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                return;

            if (e.NewValue == CheckState.Checked)
                checkedXlsxPaths.Add(entry.FullPath);
            else
                checkedXlsxPaths.Remove(entry.FullPath);
        }
        private void ListBox_CsvFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ListBox_CsvFiles.SelectedItem == null) return;

            string displayName = ListBox_CsvFiles.SelectedItem.ToString();
            FileListEntry entry = allListEntries.FirstOrDefault(e => e.DisplayName == displayName);
            if (entry == null) return;

            currentSelectedXlsxPath = entry.FullPath;
            RefreshPreviewFromXlsx(entry.FullPath);
            AddLog($"XLSX 선택: {entry.DisplayName}", LogLevel.Info);
        }

        private void TryRefreshXlsxTypeValidation(string xlsxPath)
        {
            if (EnumCatalogService.IsCatalogPath(xlsxPath))
                return;

            FileSystemWatcher watcher = excelDirWatcher;
            bool resumeWatcher = watcher != null && watcher.EnableRaisingEvents;
            try
            {
                if (resumeWatcher)
                    watcher.EnableRaisingEvents = false;

                if (XlsxTemplateCreator.RefreshTypeValidationFormula(xlsxPath))
                    AddLog($"XLSX 타입 표시 규칙 업데이트됨: {Path.GetFileName(xlsxPath)}", LogLevel.Info);
            }
            catch (IOException)
            {
                // Excel에서 열려 있는 파일은 건드리지 않고 다음 선택 때 다시 시도한다.
            }
            catch (UnauthorizedAccessException)
            {
                // 읽기 전용 또는 잠긴 파일은 자동 보정을 조용히 건너뛴다.
            }
            catch (Exception ex)
            {
                AddLog($"XLSX 타입 표시 규칙 확인 실패: {ex.Message}", LogLevel.Warning);
            }
            finally
            {
                if (resumeWatcher && ReferenceEquals(excelDirWatcher, watcher))
                {
                    try { watcher.EnableRaisingEvents = true; }
                    catch (ObjectDisposedException) { }
                }
            }
        }
        private void ListBox_CsvFiles_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int idx = ListBox_CsvFiles.IndexFromPoint(e.Location);
            if (idx == ListBox.NoMatches || idx < 0 || idx >= ListBox_CsvFiles.Items.Count)
                return;

            string displayName = ListBox_CsvFiles.Items[idx]?.ToString();
            if (string.IsNullOrEmpty(displayName))
                return;

            FileListEntry entry = allListEntries.FirstOrDefault(x => x.DisplayName == displayName);
            if (entry == null || string.IsNullOrEmpty(entry.FullPath) || !File.Exists(entry.FullPath))
                return;

            try
            {
                ListBox_CsvFiles.SelectedIndex = idx;
                TryRefreshXlsxTypeValidation(entry.FullPath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = entry.FullPath,
                    UseShellExecute = true
                });
                AddLog($"XLSX 열기: {entry.DisplayName}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                AddLog($"XLSX 열기 실패: {ex.Message}", LogLevel.Error);
            }
        }

        private void ListBox_CsvFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            TryDeleteSelectedXlsxFromList();
        }

        private void TryDeleteSelectedXlsxFromList()
        {
            if (ListBox_CsvFiles.SelectedItems.Count == 0)
                return;

            var toDelete = new List<FileListEntry>();
            foreach (object item in ListBox_CsvFiles.SelectedItems)
            {
                string displayName = item?.ToString();
                if (string.IsNullOrEmpty(displayName))
                    continue;

                FileListEntry entry = allListEntries.FirstOrDefault(x => x.DisplayName == displayName);
                if (entry != null && !string.IsNullOrEmpty(entry.FullPath) && File.Exists(entry.FullPath))
                    toDelete.Add(entry);
            }

            if (toDelete.Count == 0)
                return;

            string message = toDelete.Count == 1
                ? $"「{toDelete[0].DisplayName}」을(를) 삭제할까요?\n\nXLSX 원본 폴더에서 파일이 제거됩니다."
                : $"선택한 XLSX {toDelete.Count}개를 삭제할까요?\n\nXLSX 원본 폴더에서 파일이 제거됩니다.";

            DialogResult confirm = MessageBox.Show(
                message,
                "XLSX 삭제",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
                return;

            foreach (FileListEntry entry in toDelete)
            {
                try
                {
                    checkedXlsxPaths.Remove(entry.FullPath);
                    File.Delete(entry.FullPath);
                    AddLog($"XLSX 삭제됨: {entry.DisplayName}", LogLevel.Info);

                    if (string.Equals(currentSelectedXlsxPath, entry.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        currentSelectedXlsxPath = string.Empty;
                        SetPreviewCode(string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"XLSX 삭제 실패 ({entry.DisplayName}): {ex.Message}", LogLevel.Error);
                }
            }

            ReloadDataFileList(quietLog: true);
        }

        private async void RefreshPreviewFromXlsx(string xlsxPath)
        {
            int version = ++previewRequestVersion;
            previewCancellation?.Cancel();

            if (string.IsNullOrWhiteSpace(xlsxPath) || !File.Exists(xlsxPath))
            {
                SetPreviewCode(string.Empty);
                return;
            }

            string cacheKey = xlsxPath + "|" + exportVersion + "|" + File.GetLastWriteTimeUtc(xlsxPath).Ticks.ToString();
            if (previewCacheByPath.TryGetValue(cacheKey, out string cached))
            {
                SetPreviewCode(cached);
                return;
            }

            var cancellation = new CancellationTokenSource();
            previewCancellation = cancellation;
            CancellationToken token = cancellation.Token;
            currentPreviewCode = string.Empty;
            TextBox_Preview.Text = "Preview 준비 중…";
            TextBox_Preview.ForeColor = UiTheme.TextMuted;

            bool lockEntered = false;
            try
            {
                // 빠르게 목록을 이동할 때 선택되지 않을 파일의 XLSX 로드를 시작하지 않는다.
                await Task.Delay(80, token);
                await previewGenerationLock.WaitAsync(token);
                lockEntered = true;
                token.ThrowIfCancellationRequested();

                string preview = await Task.Run(() =>
                    EnumCatalogService.IsCatalogPath(xlsxPath)
                        ? EnumCatalogService.GenerateSource(EnumCatalogService.ParseXlsx(xlsxPath))
                        : CsvClassGenerator.GeneratePreviewFromXlsxFast(
                            xlsxPath,
                            maxRows: 64,
                            exportVersion: exportVersion), token);

                token.ThrowIfCancellationRequested();
                if (version != previewRequestVersion)
                    return;

                if (previewCacheByPath.Count > 128)
                    previewCacheByPath.Clear();
                previewCacheByPath[cacheKey] = preview;
                SetPreviewCode(preview);
            }
            catch (OperationCanceledException)
            {
                // 더 최근에 선택한 테이블이 있으면 이전 Preview 결과는 조용히 폐기한다.
            }
            catch (Exception ex)
            {
                if (version != previewRequestVersion)
                    return;
                SetPreviewCode(string.Empty);
                AddLog($"미리보기 생성 실패: {ex.Message}", LogLevel.Warning);
            }
            finally
            {
                if (lockEntered)
                    previewGenerationLock.Release();
                if (ReferenceEquals(previewCancellation, cancellation))
                    previewCancellation = null;
                cancellation.Dispose();
            }
        }
        // =========================
        // 필터
        // =========================
        private void Txt_CsvFilter_TextChanged(object sender, EventArgs e)
        {
            RefreshListBoxFromFilter();
        }

        private void Combo_LogFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (Combo_LogFilter.SelectedItem?.ToString())
            {
                case "Info": currentFilter = LogLevel.Info; break;
                case "Warning": currentFilter = LogLevel.Warning; break;
                case "Error": currentFilter = LogLevel.Error; break;
                default: currentFilter = null; break;
            }

            RefreshLogDisplay();
        }

        private void Btn_ClearLog_Click(object sender, EventArgs e)
        {
            allLogs.Clear();
            RefreshLogDisplay();
        }

        // =========================
        // 경로 설정
        // =========================
        private void Btn_SelectProjectRoot_Click(object sender, EventArgs e)
        {
            using (var cfd = new CommonOpenFileDialog())
            {
                cfd.IsFolderPicker = true;
                SetFolderDialogInitialPath(cfd, projectRootPath);

                if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    projectRootPath = cfd.FileName;

                    ToolSettingsStore.ProjectRootPath = projectRootPath;
                    ToolSettingsStore.Save();

                    UiTheme.UpdatePathLabel(Label_ProjectRoot, projectRootPath);

                    AddLog(
                        $"프로젝트 루트: {projectRootPath}\n" +
                        "→ 출력: DataTables\\Content\\CSV·Bytes, DataTables\\Scripts",
                        LogLevel.Info);
                    ReloadDataFileList();
                    InitDirectoryWatchers();
                }
            }
        }

        private void Btn_SelectExcelFolder_Click(object sender, EventArgs e)
        {
            using (var cfd = new CommonOpenFileDialog())
            {
                cfd.IsFolderPicker = true;
                SetFolderDialogInitialPath(cfd, excelSourceFolderPath);

                if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    excelSourceFolderPath = cfd.FileName;
                    UiTheme.UpdatePathLabel(Label_ExcelSourcePath, excelSourceFolderPath);

                    ToolSettingsStore.ExcelSourceFolderPath = excelSourceFolderPath;
                    ToolSettingsStore.Save();

                    AddLog($"XLSX 원본 폴더: {excelSourceFolderPath}", LogLevel.Info);
                    ReloadDataFileList();
                    InitDirectoryWatchers();
                }
            }
        }

        // =========================
        // 체크
        // =========================
        /// <param name="willRefreshExcelToCsvFirst">true이면 엑셀→CSV로 전 테이블이 갱신되므로, 기존 CSV가 없어도 됩니다.</param>
        private bool CheckDataSettingAvailable(out string reason, bool willRefreshExcelToCsvFirst)
        {
            var sb = new StringBuilder();

            if (string.IsNullOrWhiteSpace(projectRootPath) || !Directory.Exists(projectRootPath))
                sb.AppendLine("· 「프로젝트 경로 지정」으로 프로젝트 루트를 선택하세요. (Assets 폴더가 아닙니다.)");

            if (!willRefreshExcelToCsvFirst)
            {
                if (string.IsNullOrWhiteSpace(dataCsvDir) || !Directory.Exists(dataCsvDir)
                    || Directory.GetFiles(dataCsvDir, "DT_*.csv").Length == 0)
                {
                    sb.AppendLine(
                        "· 작업 CSV 폴더에 DT_*.csv 가 없습니다. XLSX 원본을 지정한 뒤 데이터 설정으로 엑셀→CSV 갱신을 하거나, DT_ 접두 CSV를 넣어 주세요.");
                }
            }

            if (string.IsNullOrWhiteSpace(exportVersion))
                sb.AppendLine("· Export 버전을 입력하세요. (예: 1.0.0)");
            else if (!DataVersion.TryParse(exportVersion, out _))
                sb.AppendLine("· Export 버전 형식이 올바르지 않습니다. (예: 1.0.0)");

            reason = sb.ToString();
            return string.IsNullOrEmpty(reason);
        }

        // =========================
        // 로그
        // =========================
        private void AddLog(string msg, LogLevel level = LogLevel.Info, bool suppressErrorDialog = false)
        {
            allLogs.Add(new LogEntry(level, msg));

            if (allLogs.Count > maxLogLines)
                allLogs.RemoveAt(0);

            RefreshLogDisplay();

            if (level == LogLevel.Error && !suppressErrorDialog)
                MessageBox.Show(msg, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static Color LogLevelLineColor(LogLevel level) => UiTheme.LogColor(level);

        private void RefreshLogDisplay()
        {
            TextBox_Log.SuspendLayout();
            TextBox_Log.Clear();
            Color resetColor = TextBox_Log.ForeColor;

            foreach (var log in allLogs
                .Where(l => currentFilter == null || l.Level == currentFilter))
            {
                TextBox_Log.SelectionStart = TextBox_Log.TextLength;
                TextBox_Log.SelectionLength = 0;
                TextBox_Log.SelectionColor = LogLevelLineColor(log.Level);
                TextBox_Log.AppendText(
                    $"[{log.Time:HH:mm:ss}] [{log.Level}] {log.Message}\n");
            }

            TextBox_Log.SelectionColor = resetColor;
            TextBox_Log.SelectionStart = TextBox_Log.TextLength;
            TextBox_Log.ScrollToCaret();
            TextBox_Log.ResumeLayout();
        }
        private static void TryOpenFolderInExplorer(string path, string labelForLog, Action<string, LogLevel> log)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                log?.Invoke($"{labelForLog} 경로가 없습니다. 상단에서 폴더를 지정하세요.", LogLevel.Warning);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                log?.Invoke($"{labelForLog} 열기: {path}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                log?.Invoke($"{labelForLog} 열기 실패: {ex.Message}", LogLevel.Error);
            }
        }

        private void Btn_OpenOutputFolder_Click(object sender, EventArgs e)
        {
            TryOpenFolderInExplorer(projectRootPath, "프로젝트 루트", (m, l) => AddLog(m, l));
        }

        private void Btn_OpenCsvFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath) || !Directory.Exists(projectRootPath))
            {
                AddLog("먼저 「프로젝트 경로 지정」을 하세요.", LogLevel.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(gameDatasDir);
            }
            catch (Exception ex)
            {
                AddLog($"출력(Datas) 폴더 준비 실패: {ex.Message}", LogLevel.Error);
                return;
            }

            TryOpenFolderInExplorer(gameDatasDir, "Assets\\_Game\\DataTables", (m, l) => AddLog(m, l));
        }

        private void Btn_OpenXlsxFolder_Click(object sender, EventArgs e)
        {
            TryOpenFolderInExplorer(excelSourceFolderPath, "XLSX 원본 폴더", (m, l) => AddLog(m, l));
        }

        private void Btn_EnumCatalog_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(excelSourceFolderPath) || !Directory.Exists(excelSourceFolderPath))
            {
                MessageBox.Show(
                    "먼저 XLSX 원본 폴더를 지정하세요.",
                    "Enum XLSX",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string path = Path.Combine(excelSourceFolderPath, EnumCatalogService.WorkbookFileName);
            try
            {
                if (!File.Exists(path))
                {
                    XlsxTemplateCreator.CreateEnumCatalog(path);
                    AddLog($"Enum 관리 XLSX 생성됨: {EnumCatalogService.WorkbookFileName}", LogLevel.Info);
                    ReloadDataFileList(quietLog: true);
                }

                ListBox_CsvFiles.SelectedItem = EnumCatalogService.WorkbookFileName;
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddLog($"Enum 관리 XLSX 열기 실패: {ex.Message}", LogLevel.Error);
            }
        }

        private void Btn_NewCsv_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(excelSourceFolderPath) || !Directory.Exists(excelSourceFolderPath))
            {
                AddLog("먼저 「XLSX 원본 지정」으로 엑셀 폴더를 선택하세요.", LogLevel.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TextBox_NewCsvName.Text))
            {
                AddLog("새 XLSX 이름을 입력하세요. (파일명 앞에 DT_ 가 붙습니다)", LogLevel.Error);
                return;
            }

            string baseName = BuildDtPrefixedTableBaseName(TextBox_NewCsvName.Text);
            if (string.IsNullOrEmpty(baseName))
            {
                AddLog("파일 이름이 올바르지 않습니다.", LogLevel.Error);
                return;
            }

            string newPath = Path.Combine(excelSourceFolderPath, baseName + ".xlsx");

            if (File.Exists(newPath))
            {
                AddLog("같은 이름의 XLSX가 이미 있습니다.", LogLevel.Warning);
                return;
            }

            try
            {
                XlsxTemplateCreator.CreateNew(newPath, baseName);

                AddLog($"XLSX 생성됨: {baseName}.xlsx", LogLevel.Info);

                ReloadDataFileList();
                ListBox_CsvFiles.SelectedItem = baseName + ".xlsx";
            }
            catch (Exception ex)
            {
                AddLog($"XLSX 생성 실패: {ex.Message}", LogLevel.Error);
            }
        }

        private void Chk_RemoveOrphanArtifacts_CheckedChanged(object sender, EventArgs e)
        {
            ToolSettingsStore.RemoveOrphanArtifactsOnExport = Chk_RemoveOrphanArtifacts.Checked;
            ToolSettingsStore.Save();
        }

        private void Txt_ExportVersion_TextChanged(object sender, EventArgs e)
        {
            exportVersion = Txt_ExportVersion.Text?.Trim() ?? string.Empty;
            previewCacheByPath.Clear();
            if (!string.IsNullOrWhiteSpace(currentSelectedXlsxPath))
                RefreshPreviewFromXlsx(currentSelectedXlsxPath);
        }

        private void SaveExportVersionSetting()
        {
            exportVersion = NormalizeExportVersion(Txt_ExportVersion.Text);
            Txt_ExportVersion.Text = exportVersion;
            ToolSettingsStore.ExportVersion = exportVersion;
            ToolSettingsStore.Save();
        }

        private static string NormalizeExportVersion(string raw)
        {
            raw = raw?.Trim() ?? string.Empty;
            return string.IsNullOrEmpty(raw) ? "1.0.0" : raw;
        }
    }

    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }

        public LogEntry(LogLevel level, string msg)
        {
            Level = level;
            Message = msg;
            Time = DateTime.Now;
        }
    }
}