using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

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
        private string currentPreviewCode = "";

        private sealed class FileListEntry
        {
            public string FullPath { get; set; }
            public string DisplayName { get; set; }
        }

        private List<LogEntry> allLogs = new List<LogEntry>();
        private LogLevel? currentFilter = null;
        private bool splitContainersInitialized;
        private readonly Dictionary<string, ListViewItem> exportResultItems =
            new Dictionary<string, ListViewItem>(StringComparer.OrdinalIgnoreCase);

        private bool exportExcelPhaseRan;
        private bool exportHasTableFailure;
        private int exportActivePhaseIndex = -1;

        public Form1()
        {
            InitializeComponent();

            bool darkMode = Properties.Settings.Default.DarkMode;
            UiTheme.SetDarkMode(darkMode);
            Chk_DarkMode.Checked = darkMode;

            ApplyUiTheme();
            ApplyAppIcon();
        }

        private void ApplyAppIcon()
        {
            TrySetFormIcon();
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
            UiTheme.SetDarkMode(darkMode);
            Properties.Settings.Default.DarkMode = darkMode;
            Properties.Settings.Default.Save();

            ApplyUiTheme();
            SetPreviewCode(currentPreviewCode);
            RefreshLogDisplay();
            StyleExportResultRows();
        }

        private void SetPreviewCode(string code)
        {
            currentPreviewCode = code ?? string.Empty;
            CSharpPreviewHighlighter.Apply(TextBox_Preview, currentPreviewCode, UiTheme.IsDarkMode);
        }

        private void ApplyUiTheme()
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
            Chk_DarkMode.BackColor = UiTheme.HeaderBackground;

            PictureBox_HeaderIcon.BackColor = Color.Transparent;

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
            UiTheme.StyleSecondaryButton(Btn_SelectProjectRoot);
            UiTheme.StyleSecondaryButton(Btn_SelectExcelFolder);
            UiTheme.StyleSecondaryButton(Btn_OpenOutputFolder);
            UiTheme.StyleSecondaryButton(Btn_OpenCsvFolder);
            UiTheme.StyleSecondaryButton(Btn_OpenXlsxFolder);
            UiTheme.StyleSecondaryButton(Btn_NewCsv);
            UiTheme.StyleSecondaryButton(Btn_RefreshList);

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

            splitOuter.BackColor = UiTheme.Border;
            splitOuter.Panel1.BackColor = UiTheme.AppBackground;
            splitOuter.Panel2.BackColor = UiTheme.AppBackground;
            splitWork.BackColor = UiTheme.Border;
            splitWork.Panel1.BackColor = UiTheme.AppBackground;
            splitWork.Panel2.BackColor = UiTheme.AppBackground;
            Panel_LogSection.BackColor = UiTheme.AppBackground;
            Panel_MainContent.BackColor = UiTheme.AppBackground;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (!splitContainersInitialized)
            {
                LayoutSplitContainers();
                splitContainersInitialized = true;
            }

            if (Combo_LogFilter.Items.Count > 0 && Combo_LogFilter.SelectedIndex < 0)
                Combo_LogFilter.SelectedIndex = 0;
        }

        /// <summary>사이드바·미리보기·하단 로그 패널 초기 비율.</summary>
        private void LayoutSplitContainers()
        {
            if (!IsHandleCreated || splitOuter.Width <= 0 || splitWork.Width <= 0)
                return;

            const int sidebarWidth = 288;
            int maxList = splitWork.Width - splitWork.Panel2MinSize - splitWork.SplitterWidth;
            int listDist = Math.Max(splitWork.Panel1MinSize, Math.Min(sidebarWidth, maxList));
            if (listDist > 0)
                splitWork.SplitterDistance = listDist;

            int logHeight = Panel_ExportProgress.Visible ? 300 : 200;
            int maxWork = splitOuter.Height - splitOuter.Panel2MinSize - splitOuter.SplitterWidth;
            int workDist = Math.Max(splitOuter.Panel1MinSize, maxWork - logHeight);
            if (workDist > 0)
                splitOuter.SplitterDistance = workDist;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            projectRootPath = Properties.Settings.Default.ProjectRootPath ?? "";
            excelSourceFolderPath = Properties.Settings.Default.ExcelSourceFolderPath ?? "";
            exportVersion = NormalizeExportVersion(Properties.Settings.Default.ExportVersion);

            UiTheme.UpdatePathLabel(Label_ProjectRoot, projectRootPath);
            UiTheme.UpdatePathLabel(Label_ExcelSourcePath, excelSourceFolderPath);
            Txt_ExportVersion.Text = exportVersion;
            Chk_RemoveOrphanArtifacts.Checked = Properties.Settings.Default.RemoveOrphanArtifactsOnExport;

            ReloadDataFileList();

            AddLog("툴 시작", LogLevel.Info);
            InitDirectoryWatchers();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
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
            try
            {
                SaveExportVersionSetting();
                BeginExportProgressUi();
                AddLog($"데이터 Export 시작… (버전 {exportVersion}, 고아 정리 {(Chk_RemoveOrphanArtifacts.Checked ? "ON" : "OFF")})", LogLevel.Info);

                DataExportResult result = await Task.Run(() => DataExportService.RunExport(
                    projectRootPath,
                    excelSourceFolderPath,
                    refresh,
                    ExportLog,
                    ReportExportProgress,
                    exportVersion: exportVersion,
                    removeOrphanArtifacts: Chk_RemoveOrphanArtifacts.Checked));

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
            }
        }

        private void BeginExportProgressUi()
        {
            exportResultItems.Clear();
            ListView_ExportResults.Items.Clear();
            Panel_ExportProgress.Visible = true;
            exportExcelPhaseRan = false;
            exportHasTableFailure = false;
            exportActivePhaseIndex = -1;
            Label_ExportStatus.ForeColor = UiTheme.StatusRunning;
            Label_ExportStatus.Text = "Export 준비 중…";
            ResetExportSteps();
            LayoutSplitContainers();
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

            ListBox_CsvFiles.BeginUpdate();
            ListBox_CsvFiles.Items.Clear();
            foreach (var entry in allListEntries)
            {
                if (filter.Length == 0 || entry.DisplayName.ToLowerInvariant().Contains(filter))
                    ListBox_CsvFiles.Items.Add(entry.DisplayName);
            }

            ListBox_CsvFiles.EndUpdate();
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
            if (string.IsNullOrWhiteSpace(xlsxPath) || !File.Exists(xlsxPath))
            {
                SetPreviewCode(string.Empty);
                return;
            }

            int version = ++previewRequestVersion;
            string cacheKey = xlsxPath + "|" + exportVersion + "|" + File.GetLastWriteTimeUtc(xlsxPath).Ticks.ToString();
            if (previewCacheByPath.TryGetValue(cacheKey, out string cached))
            {
                SetPreviewCode(cached);
                return;
            }

            SetPreviewCode("// preview loading...");
            try
            {
                string preview = await Task.Run(() =>
                    CsvClassGenerator.GeneratePreviewFromXlsxFast(xlsxPath, maxRows: 64, exportVersion: exportVersion));
                if (version != previewRequestVersion)
                    return;
                SetPreviewCode(preview);
                previewCacheByPath[cacheKey] = preview;
            }
            catch (Exception ex)
            {
                if (version != previewRequestVersion)
                    return;
                SetPreviewCode(string.Empty);
                AddLog($"미리보기 생성 실패: {ex.Message}", LogLevel.Warning);
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

                    Properties.Settings.Default.ProjectRootPath = projectRootPath;
                    Properties.Settings.Default.Save();

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

                    Properties.Settings.Default.ExcelSourceFolderPath = excelSourceFolderPath;
                    Properties.Settings.Default.Save();

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
            Properties.Settings.Default.RemoveOrphanArtifactsOnExport = Chk_RemoveOrphanArtifacts.Checked;
            Properties.Settings.Default.Save();
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
            Properties.Settings.Default.ExportVersion = exportVersion;
            Properties.Settings.Default.Save();
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