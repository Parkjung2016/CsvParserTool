using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using CSVParserTool.Exporting;

namespace CSVParserTool
{
    public partial class Form1
    {
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
                IEngineExportTarget target = EngineExportTargetRegistry.Get(currentExportPlatform);
                cfd.Title = target.DisplayName + " 프로젝트 루트 선택";
                SetFolderDialogInitialPath(cfd, projectRootPath);

                if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    try
                    {
                        target.ValidateProject(cfd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, target.DisplayName + " 프로젝트 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    projectRootPath = cfd.FileName;

                    ToolSettingsStore.SetProjectRootPath(currentExportPlatform.ToString(), projectRootPath);
                    ToolSettingsStore.ProjectRootPath = projectRootPath;
                    ToolSettingsStore.Save();

                    UITheme.UpdatePathLabel(Label_ProjectRoot, projectRootPath);

                    ExportTargetLayout layout = target.CreateLayout(projectRootPath);
                    string outputDetail = currentExportPlatform == ExportPlatform.Unity
                        ? $"→ Import 원본: {layout.StagingCsvDirectory}"
                        : "→ UDataTable: /Game/PJDevData/DataTables";
                    AddLog(
                        $"프로젝트 루트: {projectRootPath}\n" +
                        $"→ 생성 코드: {layout.GeneratedCodeDirectory}\n" +
                        outputDetail,
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
                    UITheme.UpdatePathLabel(Label_ExcelSourcePath, excelSourceFolderPath);

                    ToolSettingsStore.SetExcelSourceFolderPath(currentExportPlatform.ToString(), excelSourceFolderPath);
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
            var entry = new LogEntry(level, msg);
            allLogs.Add(entry);
            bool removedOldest = allLogs.Count > maxLogLines;
            if (removedOldest)
                allLogs.RemoveAt(0);

            // 보이는 로그 한 줄만 추가한다. 최대 개수 초과로 첫 줄이 제거될 때만 전체를 다시 그린다.
            if (removedOldest)
                RefreshLogDisplay();
            else if (currentFilter == null || currentFilter == level)
                AppendLogEntryToDisplay(entry);

            if (level == LogLevel.Error && !suppressErrorDialog)
                MessageBox.Show(msg, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void AppendLogEntryToDisplay(LogEntry entry)
        {
            Color resetColor = TextBox_Log.ForeColor;
            TextBox_Log.SelectionStart = TextBox_Log.TextLength;
            TextBox_Log.SelectionLength = 0;
            TextBox_Log.SelectionColor = LogLevelLineColor(entry.Level);
            TextBox_Log.AppendText($"[{entry.Time:HH:mm:ss}] [{entry.Level}] {entry.Message}\n");
            TextBox_Log.SelectionColor = resetColor;
            TextBox_Log.SelectionStart = TextBox_Log.TextLength;
            TextBox_Log.ScrollToCaret();
        }
        private static Color LogLevelLineColor(LogLevel level) => UITheme.LogColor(level);

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

        private async void Btn_CleanupAll_Click(object sender, EventArgs e)
        {
            if (exportInProgress)
            {
                MessageBox.Show(
                    "Export가 끝난 뒤 다시 시도하세요.",
                    "모두 정리",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DataExportCleanupPlan plan;
            try
            {
                plan = DataExportCleanupService.CreatePlan(projectRootPath, currentExportPlatform);
            }
            catch (Exception ex)
            {
                AddLog("정리 준비 실패: " + ex.Message, LogLevel.Error, suppressErrorDialog: true);
                MessageBox.Show(ex.Message, "모두 정리", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            IReadOnlyList<string> existingPaths = plan.ExistingPaths;
            if (existingPaths.Count == 0)
            {
                MessageBox.Show(
                    "정리할 Data Tool 산출물이 없습니다.",
                    "모두 정리",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string relativePaths = string.Join(
                Environment.NewLine,
                existingPaths.Select(path => "• " + ToProjectRelativePath(plan.ProjectRoot, path)));
            string engineName = currentExportPlatform == ExportPlatform.Unity ? "Unity" : "Unreal Engine";
            DialogResult confirm = MessageBox.Show(
                engineName + "에서 Data Tool이 만든 다음 산출물을 모두 삭제합니다."
                + Environment.NewLine + Environment.NewLine
                + relativePaths
                + Environment.NewLine + Environment.NewLine
                + "XLSX 원본과 위 경로 밖의 사용자 코드는 삭제하지 않습니다."
                + Environment.NewLine
                + "삭제한 파일은 복구할 수 없습니다. 계속할까요?",
                "모두 정리",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
                return;

            Btn_CleanupAll.Enabled = false;
            Btn_DataSetting.Enabled = false;
            Btn_ExportSelected.Enabled = false;
            Btn_EngineTarget.Enabled = false;
            Btn_SelectProjectRoot.Enabled = false;
            try
            {
                var cleanupLogs = new List<string>();
                DataExportCleanupResult result = await Task.Run(
                    () => DataExportCleanupService.Execute(plan, cleanupLogs.Add));
                foreach (string line in cleanupLogs)
                    AddLog(line, LogLevel.Info);

                string summary = $"{engineName} 산출물 모두 정리 완료 · 폴더 {result.RemovedDirectoryCount}개 · 파일 {result.RemovedFileCount}개";
                AddLog(summary, LogLevel.Info);
                MessageBox.Show(summary, "모두 정리", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddLog("모두 정리 실패: " + ex.Message, LogLevel.Error, suppressErrorDialog: true);
                MessageBox.Show(ex.Message, "모두 정리 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Btn_CleanupAll.Enabled = true;
                Btn_DataSetting.Enabled = true;
                Btn_ExportSelected.Enabled = true;
                Btn_EngineTarget.Enabled = true;
                Btn_SelectProjectRoot.Enabled = true;
            }
        }

        private static string ToProjectRelativePath(string projectRoot, string path)
        {
            string root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(root.Length + 1);
            return fullPath;
        }
        private void Btn_OpenOutputFolder_Click(object sender, EventArgs e)
        {
            TryOpenFolderInExplorer(projectRootPath, "프로젝트 루트", (m, l) => AddLog(m, l));
        }

        private void Btn_OpenCsvFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath) || !Directory.Exists(projectRootPath))
            {
                const string message = "프로젝트 경로가 없습니다.\r\n먼저 「프로젝트 경로 지정」을 해주세요.";
                AddLog(message.Replace("\r\n", " "), LogLevel.Warning);
                MessageBox.Show(this, message, "출력 폴더", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(gameDatasDir) || !Directory.Exists(gameDatasDir))
            {
                string outputLabel = currentExportPlatform == ExportPlatform.Unity
                    ? "Assets\\_Game\\DataTables"
                    : "Content\\PJDevData\\DataTables";
                string message = $"출력 폴더가 아직 없습니다.\r\n먼저 Export를 진행해주세요.\r\n\r\n{outputLabel}";
                AddLog($"출력 폴더가 없습니다: {gameDatasDir}", LogLevel.Warning);
                MessageBox.Show(this, message, "출력 폴더", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TryOpenFolderInExplorer(gameDatasDir, currentExportPlatform == ExportPlatform.Unity ? "Assets\\_Game\\DataTables" : "Content\\PJDevData\\DataTables", (m, l) => AddLog(m, l));
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

        private void Txt_ExportVersion_Leave(object sender, EventArgs e)
        {
            TrySaveExportVersionSetting(showWarning: true);
        }

        private void Txt_ExportVersion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            SelectNextControl(Txt_ExportVersion, forward: true, tabStopOnly: true, nested: true, wrap: true);
        }

        private bool TrySaveExportVersionSetting(bool showWarning)
        {
            string nextVersion = NormalizeExportVersion(Txt_ExportVersion.Text);
            if (!DataVersion.TryParse(nextVersion, out _))
            {
                if (showWarning && !string.Equals(lastWarnedInvalidExportVersion, nextVersion, StringComparison.Ordinal))
                {
                    AddLog($"Export 버전 '{nextVersion}' 형식이 올바르지 않습니다. (예: 1.0.0)", LogLevel.Warning);
                    lastWarnedInvalidExportVersion = nextVersion;
                }
                return false;
            }

            lastWarnedInvalidExportVersion = string.Empty;
            bool changed = !string.Equals(exportVersion, nextVersion, StringComparison.Ordinal);
            exportVersion = nextVersion;
            if (!string.Equals(Txt_ExportVersion.Text, nextVersion, StringComparison.Ordinal))
                Txt_ExportVersion.Text = nextVersion;

            ToolSettingsStore.ExportVersion = exportVersion;
            ToolSettingsStore.Save();

            if (changed)
            {
                previewCacheByPath.Clear();
                if (!string.IsNullOrWhiteSpace(currentSelectedXlsxPath))
                    RefreshPreviewFromXlsx(currentSelectedXlsxPath);
            }

            return true;
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
