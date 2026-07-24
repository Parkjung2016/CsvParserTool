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

        private static string BuildPreviewCacheKey(string xlsxPath, string xlsxFolder, string previewExportVersion)
        {
            var key = new StringBuilder();
            key.Append(xlsxPath).Append('|').Append(previewExportVersion);
            string folder = !string.IsNullOrWhiteSpace(xlsxFolder) && Directory.Exists(xlsxFolder)
                ? xlsxFolder
                : Path.GetDirectoryName(xlsxPath);

            try
            {
                if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                {
                    foreach (string path in Directory.GetFiles(folder, "*.xlsx")
                        .Where(path => !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal))
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                    {
                        var info = new FileInfo(path);
                        key.Append('|')
                            .Append(info.Name)
                            .Append(':')
                            .Append(info.LastWriteTimeUtc.Ticks)
                            .Append(':')
                            .Append(info.Length);
                    }
                }
            }
            catch (IOException)
            {
                key.Append('|').Append(File.GetLastWriteTimeUtc(xlsxPath).Ticks);
            }
            catch (UnauthorizedAccessException)
            {
                key.Append('|').Append(File.GetLastWriteTimeUtc(xlsxPath).Ticks);
            }

            return key.ToString();
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

            string previewFolder = excelSourceFolderPath;
            string previewExportVersion = exportVersion;
            string cacheKey = BuildPreviewCacheKey(xlsxPath, previewFolder, previewExportVersion);
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
            TextBox_Preview.ForeColor = UITheme.TextMuted;

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
                        : CsvClassGenerator.GenerateValidatedPreviewFromXlsx(
                            xlsxPath,
                            previewFolder,
                            previewExportVersion,
                            token), token);

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
                string previewError = "// Preview 검사 실패\n// " +
                    (ex.GetBaseException().Message ?? ex.Message).Replace("\r\n", "\n").Replace("\n", "\n// ");
                SetPreviewCode(previewError);
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
    }
}