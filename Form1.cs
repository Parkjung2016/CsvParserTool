using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private string dataCsvDir =>
            string.IsNullOrWhiteSpace(projectRootPath)
                ? string.Empty
                : DataProjectPaths.DataCsvDir(projectRootPath);

        private string gameDatasDir =>
            string.IsNullOrWhiteSpace(projectRootPath)
                ? string.Empty
                : DataProjectPaths.GameDatasDir(projectRootPath);

        private FileSystemWatcher excelDirWatcher;

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

        private static string ResolveAppIconPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "Properties", "AppIcon.ico"),
                Path.Combine(baseDir, "AppIcon.ico"),
                Application.ExecutablePath
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            return Application.ExecutablePath;
        }

        private static Bitmap LoadHeaderIconBitmap()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string pngPath = Path.Combine(baseDir, "assets", "pjdev-icon-source-square.png");
            if (File.Exists(pngPath))
                return new Bitmap(pngPath);

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

            UiTheme.StylePathLabel(Label_ProjectRoot);
            UiTheme.StylePathLabel(Label_ExcelSourcePath);
            UiTheme.StyleTextField(Txt_CsvFilter);
            UiTheme.StyleTextField(TextBox_NewCsvName);
            UiTheme.StyleCombo(Combo_LogFilter);
            UiTheme.StyleList(ListBox_CsvFiles);
            UiTheme.StylePreviewBox(TextBox_Preview);
            UiTheme.StyleLogBox(TextBox_Log);

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

            const int logHeight = 168;
            int maxWork = splitOuter.Height - splitOuter.Panel2MinSize - splitOuter.SplitterWidth;
            int workDist = Math.Max(splitOuter.Panel1MinSize, maxWork - logHeight);
            if (workDist > 0)
                splitOuter.SplitterDistance = workDist;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            projectRootPath = Properties.Settings.Default.ProjectRootPath ?? "";
            excelSourceFolderPath = Properties.Settings.Default.ExcelSourceFolderPath ?? "";

            UiTheme.UpdatePathLabel(Label_ProjectRoot, projectRootPath);
            UiTheme.UpdatePathLabel(Label_ExcelSourcePath, excelSourceFolderPath);

            ReloadDataFileList();

            AddLog("툴 시작", LogLevel.Info);
            InitDirectoryWatchers();
        }

        // =========================
        // 데이터 설정: CLI(DataTool.exe)만 사용 — 완료 후 콘솔에서 pause 로 종료
        // =========================
        private void Btn_DataSetting_Click(object sender, EventArgs e)
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

            string baseDir = AppContext.BaseDirectory;
            string guiBat = Path.Combine(baseDir, "DataToolGui.bat");
            string exe = Path.Combine(baseDir, "DataTool.exe");
            if (!File.Exists(guiBat) || !File.Exists(exe))
            {
                AddLog("DataToolGui.bat / DataTool.exe 가 실행 폴더에 있어야 합니다. 솔루션 전체를 빌드하세요.", LogLevel.Error);
                MessageBox.Show(
                    "DataToolGui.bat 과 DataTool.exe 를 찾을 수 없습니다.\n솔루션을 빌드해 CLI 출력을 DataToolGUI 와 같은 폴더에 두세요.",
                    "데이터 설정",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string pr = QuoteCmdArg(projectRootPath);
            string exportArgs = $"export --project {pr}";
            if (refresh)
                exportArgs += $" --excel {QuoteCmdArg(excelSourceFolderPath)} --refresh-xlsx";

            try
            {
                // /c: 배치 실행 후 pause 로 "아무 키나 누르면" 창 종료 ( /k 는 창이 안 닫힘 )
                string batForCmd = "\"" + guiBat + "\"";
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + batForCmd + " " + exportArgs,
                    WorkingDirectory = baseDir,
                    UseShellExecute = true
                });
                AddLog("CLI에서 데이터 설정을 실행했습니다. 콘솔에서 끝나면 아무 키나 눌러 창을 닫으세요.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                AddLog(ex.Message, LogLevel.Error);
            }
        }

        private static string QuoteCmdArg(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "\"\"";
            if (s.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
                return s;
            return "\"" + s.Replace("\"", "\\\"") + "\"";
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
            RebuildAllListEntries();
            RefreshListBoxFromFilter();

            if (!quietLog)
            {
                int nXlsx = allListEntries.Count;
                if (nXlsx == 0)
                    AddLog("목록에 XLSX가 없습니다. 「XLSX 원본 지정」 경로를 확인하세요.", LogLevel.Warning);
                else
                    AddLog($"목록: XLSX {nXlsx}개", LogLevel.Info);
            }
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
                excelDirWatcher = new FileSystemWatcher(excelSourceFolderPath, "*.xlsx");
                excelDirWatcher.Changed += (_, __) => ReloadDataFileListSafe();
                excelDirWatcher.Created += (_, __) => ReloadDataFileListSafe();
                excelDirWatcher.Deleted += (_, __) => ReloadDataFileListSafe();
                excelDirWatcher.EnableRaisingEvents = true;
            }
        }

        private void ReloadDataFileListSafe()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ReloadDataFileListSafe));
                return;
            }

            ReloadDataFileList(quietLog: true);
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

        private async void RefreshPreviewFromXlsx(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath) || !File.Exists(xlsxPath))
            {
                SetPreviewCode(string.Empty);
                return;
            }

            int version = ++previewRequestVersion;
            string cacheKey = xlsxPath + "|" + File.GetLastWriteTimeUtc(xlsxPath).Ticks.ToString();
            if (previewCacheByPath.TryGetValue(cacheKey, out string cached))
            {
                SetPreviewCode(cached);
                return;
            }

            SetPreviewCode("// preview loading...");
            try
            {
                string preview = await Task.Run(() => CsvClassGenerator.GeneratePreviewFromXlsxFast(xlsxPath, maxRows: 64));
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
                using (var wb = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = wb.AddWorksheet("Sheet1");
                    ws.Cell(1, 1).Value = "#설명";
                    ws.Cell(1, 2).Value = "Id";
                    ws.Cell(2, 1).Value = "플레이어";
                    ws.Cell(2, 2).Value = 0;
                    wb.SaveAs(newPath);
                }

                AddLog($"XLSX 생성됨: {baseName}.xlsx", LogLevel.Info);

                ReloadDataFileList();
                ListBox_CsvFiles.SelectedItem = baseName + ".xlsx";
            }
            catch (Exception ex)
            {
                AddLog($"XLSX 생성 실패: {ex.Message}", LogLevel.Error);
            }
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