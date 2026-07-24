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
using CSVParserTool.MiniGames;

namespace CSVParserTool
{
    public partial class Form1
    {
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

            if (!TrySaveExportVersionSetting(showWarning: true))
                return;

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
                BeginExportProgressUI();
                string targetText = selectedOnly ? $"선택 {selectedTableStems.Count}개" : "전체";
                AddLog($"데이터 Export 시작… ({targetText}, 버전 {exportVersion}, 원본 없는 산출물 정리 {(Chk_RemoveOrphanArtifacts.Checked ? "ON" : "OFF")})", LogLevel.Info);

                // UI values must be captured before the worker starts. Reading WinForms
                // controls from the export thread is unsafe and can also stall rendering.
                string projectRoot = projectRootPath;
                string excelSourceFolder = excelSourceFolderPath;
                string version = exportVersion;
                bool removeOrphanArtifacts = Chk_RemoveOrphanArtifacts.Checked;
                string[] selectedStems = selectedTableStems?.ToArray();

                // Open and paint the game before the CPU-heavy export begins. Show() only
                // creates the window; painting happens after control returns to the message loop.
                ShowExportMiniGame();
                await PrepareExportMiniGameAsync();

                Task<DataExportResult> exportTask = StartExportWorker(
                    projectRoot,
                    excelSourceFolder,
                    refresh,
                    version,
                    removeOrphanArtifacts,
                    selectedStems);
                DataExportResult result = await exportTask;
                FlushPendingExportLogs();

                FinishExportProgressUI(result);

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
                FlushPendingExportLogs();
                FinishExportProgressUI(null);
                AddLog(ex.Message, LogLevel.Error);
            }
            finally
            {
                Btn_DataSetting.Enabled = true;
                Btn_ExportSelected.Enabled = true;
            }
        }

        private async Task PrepareExportMiniGameAsync()
        {
            if (exportMiniGameForm == null || exportMiniGameForm.IsDisposed || !exportMiniGameForm.Visible)
                return;

            exportMiniGameForm.RenderFirstFrame();
            await Task.Delay(32);
        }

        private Task<DataExportResult> StartExportWorker(
            string projectRoot,
            string excelSourceFolder,
            bool refresh,
            string version,
            bool removeOrphanArtifacts,
            IReadOnlyCollection<string> selectedTableStems)
        {
            return Task.Factory.StartNew(
                () =>
                {
                    // A dedicated, lower-priority worker leaves UI input and animation responsive
                    // when parallel table conversion is using every available CPU core.
                    try
                    {
                        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    }
                    catch (Exception)
                    {
                        // Some hosts do not allow changing thread priority. Export remains valid.
                    }

                    return DataExportService.RunExport(
                        projectRoot,
                        excelSourceFolder,
                        refresh,
                        ExportLog,
                        ReportExportProgress,
                        exportVersion: version,
                        removeOrphanArtifacts: removeOrphanArtifacts,
                        selectedTableStems: selectedTableStems);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
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
        private void BeginExportProgressUI()
        {
            ClearExportTaskbarNotification();
            StopExportCompletionAnimation(resetColors: true);
            exportResultItems.Clear();
            Grid_ExportResults.Rows.Clear();
            ShowExportResultsPanel();
            exportExcelPhaseRan = false;
            exportHasTableFailure = false;
            exportActivePhaseIndex = -1;
            Label_ExportStatus.ForeColor = UITheme.StatusRunning;
            Label_ExportStatus.Text = "Export 준비 중…";
            ResetExportSteps();
            LayoutSplitContainers();
            InitializeExportResultSplitterDistance();
        }

        private void FinishExportProgressUI(DataExportResult result)
        {
            if (result == null)
            {
                Label_ExportStatus.ForeColor = UITheme.LogError;
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
                Label_ExportStatus.ForeColor = UITheme.LogError;
                Label_ExportStatus.Text =
                    $"Export 실패 — 성공 {result.SucceededCount} · 실패 {result.FailedCount} (아래 목록 확인)";
            }
            else if (result.Ok)
            {
                Label_ExportStatus.ForeColor = UITheme.LogSuccess;
                Label_ExportStatus.Text =
                    total > 0
                        ? $"Export 완료 — {result.SucceededCount}/{total} 테이블"
                        : "Export 완료";
            }
            else
            {
                Label_ExportStatus.ForeColor = UITheme.LogError;
                Label_ExportStatus.Text = result.ErrorMessage ?? "Export 실패";
            }

            StyleExportResultRows();
            SelectFirstFailedExportRowInternal(result);
            if (result.FailedCount > 0)
                Combo_LogFilter.SelectedItem = "Error";
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
                    Label_ExportStatus.ForeColor = UITheme.StatusRunning;
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
                    Label_ExportStatus.ForeColor = UITheme.StatusRunning;
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

                    Label_ExportStatus.ForeColor = info.Success ? UITheme.LogSuccess : UITheme.LogError;
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

        private void Grid_ExportResults_SizeChanged(object sender, EventArgs e)
        {
            int version = ++exportResultViewportResetVersion;
            if (!IsHandleCreated || IsDisposed || Disposing)
                return;

            BeginInvoke(new Action(() =>
            {
                if (version != exportResultViewportResetVersion
                    || IsDisposed
                    || Disposing
                    || !Grid_ExportResults.IsHandleCreated)
                {
                    return;
                }

                if (Grid_ExportResults.Rows.Count > 0
                    && Grid_ExportResults.ClientSize.Height > Grid_ExportResults.ColumnHeadersHeight)
                {
                    try
                    {
                        Grid_ExportResults.FirstDisplayedScrollingRowIndex = 0;
                    }
                    catch (InvalidOperationException)
                    {
                        // 레이아웃 중 표시 가능한 행이 아직 없으면 다음 Resize에서 다시 맞춘다.
                    }
                }

                Grid_ExportResults.Invalidate(true);
            }));
        }


        private void InitializeExportResultRows(IReadOnlyList<string> itemNames)
        {
            Grid_ExportResults.SuspendLayout();
            try
            {
                Grid_ExportResults.Rows.Clear();
                exportResultItems.Clear();

                if (itemNames == null)
                    return;

                foreach (string name in itemNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    int index = Grid_ExportResults.Rows.Add(name, "대기", string.Empty);
                    DataGridViewRow row = Grid_ExportResults.Rows[index];
                    row.DefaultCellStyle.ForeColor = UITheme.StatusPending;
                    exportResultItems[name] = row;
                }
            }
            finally
            {
                Grid_ExportResults.ResumeLayout(true);
                Grid_ExportResults.ClearSelection();
                Grid_ExportResults.CurrentCell = null;
                Grid_ExportResults.Invalidate(true);
            }
        }

        private void UpdateExportResultRow(string itemName, bool success, string message)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return;

            if (!exportResultItems.TryGetValue(itemName, out DataGridViewRow row))
            {
                int index = Grid_ExportResults.Rows.Add(
                    itemName,
                    success ? "성공" : "실패",
                    message ?? string.Empty);
                row = Grid_ExportResults.Rows[index];
                exportResultItems[itemName] = row;
            }
            else
            {
                row.Cells[1].Value = success ? "성공" : "실패";
                row.Cells[2].Value = message ?? string.Empty;
            }

            row.DefaultCellStyle.ForeColor = success ? UITheme.LogSuccess : UITheme.LogError;
            Grid_ExportResults.InvalidateRow(row.Index);
        }

        private void StyleExportResultRows()
        {
            foreach (DataGridViewRow row in Grid_ExportResults.Rows)
            {
                string status = Convert.ToString(row.Cells[1].Value);
                if (status == "성공")
                    row.DefaultCellStyle.ForeColor = UITheme.LogSuccess;
                else if (status == "실패")
                    row.DefaultCellStyle.ForeColor = UITheme.LogError;
                else
                    row.DefaultCellStyle.ForeColor = UITheme.StatusPending;
            }

            Grid_ExportResults.Invalidate(true);
        }

        private void SelectFirstFailedExportRowInternal(DataExportResult result)
        {
            if (result?.TableResults == null)
                return;

            Grid_ExportResults.ClearSelection();
            foreach (DataExportTableResult resultRow in result.TableResults)
            {
                if (resultRow.Success)
                    continue;

                if (exportResultItems.TryGetValue(resultRow.SourceFileName, out DataGridViewRow gridRow))
                {
                    gridRow.Selected = true;
                    Grid_ExportResults.CurrentCell = gridRow.Cells[0];
                    Grid_ExportResults.FirstDisplayedScrollingRowIndex = gridRow.Index;
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
            if (IsDisposed || string.IsNullOrEmpty(message))
                return;

            pendingExportLogs.Enqueue(message);
            if (Interlocked.Exchange(ref exportLogFlushScheduled, 1) != 0)
                return;

            try
            {
                BeginInvoke(new Action(() => exportLogFlushTimer.Start()));
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ObjectDisposedException)
            {
                Interlocked.Exchange(ref exportLogFlushScheduled, 0);
            }
        }

        private void FlushPendingExportLogs()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(FlushPendingExportLogs));
                return;
            }

            exportLogFlushTimer.Stop();
            int added = 0;
            while (pendingExportLogs.TryDequeue(out string message))
            {
                allLogs.Add(new LogEntry(LogLevel.Info, message));
                added++;
            }

            if (allLogs.Count > maxLogLines)
                allLogs.RemoveRange(0, allLogs.Count - maxLogLines);
            if (added > 0)
                RefreshLogDisplay();

            Interlocked.Exchange(ref exportLogFlushScheduled, 0);
            if (!pendingExportLogs.IsEmpty && Interlocked.Exchange(ref exportLogFlushScheduled, 1) == 0)
                exportLogFlushTimer.Start();
        }

        /// <summary>입력은 테이블 이름(확장자 없음). 파일명으로 쓸 수 있게 정리하고, 없으면 앞에 DT_.</summary>
    }
}