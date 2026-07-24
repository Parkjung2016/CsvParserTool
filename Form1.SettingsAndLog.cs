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
                SetFolderDialogInitialPath(cfd, projectRootPath);

                if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    projectRootPath = cfd.FileName;

                    ToolSettingsStore.ProjectRootPath = projectRootPath;
                    ToolSettingsStore.Save();

                    UITheme.UpdatePathLabel(Label_ProjectRoot, projectRootPath);

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
                    UITheme.UpdatePathLabel(Label_ExcelSourcePath, excelSourceFolderPath);

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