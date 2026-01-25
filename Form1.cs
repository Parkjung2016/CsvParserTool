using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CSVParserTool
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
    public partial class Form1 : Form
    {
        private string generatedDir => Path.Combine(outputFolderPath, "Generated");
        private string dllPath => Path.Combine(outputFolderPath, "DataTables.dll");
        private string generatedCsvDir => Path.Combine(outputFolderPath, "GeneratedCsv");

        private const int MAX_LOG_LINES = 300;
        private const int MAX_RECENT_CSV = 5;

        private string outputFolderPath = "";
        private FileSystemWatcher csvDirWatcher;
        private string currentSelectedCsvPath = "";
        private string[] allCsvFiles = new string[0];

        private string[] recentCsvFiles = new string[MAX_RECENT_CSV];
        private List<LogEntry> allLogs = new List<LogEntry>();
        private LogLevel? currentFilter = null;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            outputFolderPath = Properties.Settings.Default.OutputFolderPath;

            if (!string.IsNullOrEmpty(outputFolderPath) && Directory.Exists(outputFolderPath))
            {
                Label_OutputPath.Text = outputFolderPath;
            }
            else
            {
                outputFolderPath = "";
                Label_OutputPath.Text = "경로가 설정되지 않았습니다";

                Properties.Settings.Default.OutputFolderPath = "";
            }

            Properties.Settings.Default.Save();
            LoadCsvList();
            LoadRecentCsv();
            RefreshRecentCsv();
            Btn_DataSetting.Enabled = ListBox_CsvFiles.SelectedItem != null;
            TextBox_Preview.Text = "CSV를 선택하면 생성될 클래스가 표시됩니다.";

            TextBox_Log.Clear();
            AddLog("CSV Parser Tool 시작됨", LogLevel.Info);
            AddLog($"저장된 출력 경로: {(string.IsNullOrEmpty(outputFolderPath) ? "없음" : outputFolderPath)}");

            if (string.IsNullOrEmpty(outputFolderPath))
                AddLog("출력 경로가 설정되지 않음", LogLevel.Warning);
            InitCsvDirectoryWatcher();
        }
        private void InitCsvDirectoryWatcher()
        {
            if (csvDirWatcher != null)
            {
                csvDirWatcher.EnableRaisingEvents = false;
                csvDirWatcher.Dispose();
                csvDirWatcher = null;
            }

            if (string.IsNullOrEmpty(generatedCsvDir) || !Directory.Exists(generatedCsvDir))
                return;

            csvDirWatcher = new FileSystemWatcher
            {
                Path = generatedCsvDir,
                Filter = "*.csv",
                NotifyFilter =
                    NotifyFilters.FileName |
                    NotifyFilters.LastWrite |
                    NotifyFilters.Size
            };

            csvDirWatcher.Created += OnCsvDirectoryChanged;
            csvDirWatcher.Deleted += OnCsvDirectoryChanged;
            csvDirWatcher.Renamed += OnCsvDirectoryChanged;

            csvDirWatcher.EnableRaisingEvents = true;

            AddLog("CSV 폴더 감시 시작", LogLevel.Info);
        }
        private void OnCsvDirectoryChanged(object sender, FileSystemEventArgs e)
        {
            if (IsDisposed) return;

            System.Threading.Thread.Sleep(100);

            string name = Path.GetFileNameWithoutExtension(e.Name);
            string action = e.ChangeType switch
            {
                WatcherChangeTypes.Created => "생성",
                WatcherChangeTypes.Deleted => "삭제",
                WatcherChangeTypes.Renamed => "이름 변경",
                _ => "변경"
            };

            AddLog($"CSV {action} 감지: {name}", LogLevel.Info);

            if (InvokeRequired)
                Invoke(new Action(ReloadCsvListSafe));
            else
                ReloadCsvListSafe();
        }
        private void ReloadCsvListSafe()
        {
            string prevSelected = ListBox_CsvFiles.SelectedItem?.ToString();

            LoadCsvList();
            RefreshRecentCsv();

            if (!string.IsNullOrEmpty(prevSelected) &&
                ListBox_CsvFiles.Items.Contains(prevSelected))
            {
                ListBox_CsvFiles.SelectedItem = prevSelected;

                string csvPath = Path.Combine(generatedCsvDir, prevSelected + ".csv");
                currentSelectedCsvPath = csvPath;

                RefreshPreview(csvPath);
                UpdateDataSettingReason();
            }
            else
            {
                currentSelectedCsvPath = "";
                Btn_DataSetting.Enabled = false;

                TextBox_SelectedCsv.Text = "CSV가 선택되지 않았습니다";
                TextBox_Preview.Text = "CSV를 선택하면 생성될 클래스가 표시됩니다.";

                AddLog("선택된 CSV가 삭제되어 미리보기를 초기화함", LogLevel.Warning);
            }

            AddLog("CSV 목록 / 최근 CSV / 미리보기 동기화 완료", LogLevel.Info);
        }

        private void LoadRecentCsv()
        {
            var saved = Properties.Settings.Default.RecentCsvFiles;
            if (saved != null)
            {
                recentCsvFiles = saved.Cast<string>().ToArray();
                Combo_RecentCsv.Items.Clear();
                Combo_RecentCsv.Items.AddRange(recentCsvFiles);
            }
        }
        private void RefreshRecentCsv()
        {
            if (recentCsvFiles == null || recentCsvFiles.Length == 0)
                return;

            var validSet = new HashSet<string>(
                allCsvFiles.Select(f => Path.GetFileNameWithoutExtension(f))
            );

            recentCsvFiles = recentCsvFiles
                .Where(name => !string.IsNullOrEmpty(name) && validSet.Contains(name))
                .ToArray();

            Combo_RecentCsv.Items.Clear();
            Combo_RecentCsv.Items.AddRange(recentCsvFiles);

            var sc = new System.Collections.Specialized.StringCollection();
            sc.AddRange(recentCsvFiles);
            Properties.Settings.Default.RecentCsvFiles = sc;
            Properties.Settings.Default.Save();

            AddLog("최근 CSV 목록 정리 완료", LogLevel.Info);
        }
        private void Btn_DataSetting_Click(object sender, EventArgs e)
        {
            if (!CheckDataSettingAvailable(out _))
            {
                AddLog("데이터 설정 실패 - 사전 조건 불충족", LogLevel.Error);
                return;
            }

            try
            {
                string csvName = Path.GetFileName(currentSelectedCsvPath);

                AddLog($"CSV 처리 시작: {csvName}", LogLevel.Info);

                CsvClassGenerator.GenerateAndSave(
                    currentSelectedCsvPath,
                    generatedDir
                );

                AddLog("클래스 파일 생성 완료", LogLevel.Info);

                DllBuilder.Build(
                    generatedDir,
                    dllPath
                );

                AddLog($"DLL 빌드 완료: {dllPath}", LogLevel.Info);

                MessageBox.Show(
                    "클래스 생성 및 DLL 빌드 완료!",
                    "완료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                AddLog(ex.Message, LogLevel.Error);
                MessageBox.Show(ex.Message, "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCsvList()
        {
            ListBox_CsvFiles.Items.Clear();

            if (string.IsNullOrEmpty(generatedCsvDir) || !Directory.Exists(generatedCsvDir))
            {
                AddLog("CSV 폴더가 유효하지 않아 리스트를 로드하지 않음", LogLevel.Warning);
                return;
            }

            allCsvFiles = Directory.GetFiles(generatedCsvDir, "*.csv");

            foreach (var file in allCsvFiles)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file); // .csv 제거
                ListBox_CsvFiles.Items.Add(fileNameWithoutExt);
                AddLog($"CSV 발견: {fileNameWithoutExt}", LogLevel.Info);
            }

            AddLog($"CSV 파일 총 {allCsvFiles.Length}개 로드 완료",
                allCsvFiles.Length > 0 ? LogLevel.Info : LogLevel.Warning);
        }

        private void Btn_SelectOutputFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "C# 클래스 출력 폴더를 선택하세요";

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    outputFolderPath = fbd.SelectedPath;
                    Label_OutputPath.Text = outputFolderPath;

                    Properties.Settings.Default.OutputFolderPath = outputFolderPath;
                    Properties.Settings.Default.Save();
                    AddLog($"출력 폴더 설정: {outputFolderPath}", LogLevel.Info);
                    LoadCsvList();
                    InitCsvDirectoryWatcher();
                }
                else
                {
                    AddLog("출력 폴더 선택 취소", LogLevel.Warning);
                }
            }
        }

        private void ListBox_CsvFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ListBox_CsvFiles.SelectedItem == null)
                return;

            string fileName = ListBox_CsvFiles.SelectedItem.ToString();
            TextBox_SelectedCsv.Text = $"설정할 CSV : {fileName}";

            string csvPath = Path.Combine(generatedCsvDir, ListBox_CsvFiles.SelectedItem.ToString() + ".csv");
            if (currentSelectedCsvPath != csvPath)
            {
                RefreshPreview(csvPath);

                AddLog($"CSV 선택됨: {fileName}", LogLevel.Info);
                AddLog($"CSV 전체 경로: {csvPath}", LogLevel.Info);
            }

            currentSelectedCsvPath = csvPath;
            UpdateDataSettingReason();
            //InitCsvWatcher();

            UpdateRecentCsv(fileName);
        }
        private void RefreshPreview(string csvPath)
        {
            try
            {
                AddLog("클래스 생성 미리보기 시작", LogLevel.Info);

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(csvPath);
                string csFileName = fileNameWithoutExt + ".cs"; // 생성될 C# 파일 이름

                var sb = new StringBuilder();
                sb.AppendLine("[클래스 미리보기]");
                sb.AppendLine($"생성될 C# 파일: {csFileName}");
                sb.AppendLine();
                sb.AppendLine(CsvClassGenerator.GenerateClass(csvPath));

                TextBox_Preview.Text = sb.ToString();
                AddLog("클래스 생성 미리보기 성공", LogLevel.Info);
            }
            catch (Exception ex)
            {
                AddLog($"미리보기 생성 실패: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
                TextBox_Preview.Text = "[ERROR]\n" + ex.Message;
            }
        }

        private void ListBox_CsvFiles_DoubleClick(object sender, EventArgs e)
        {
            if (ListBox_CsvFiles.SelectedItem == null)
                return;

            string fileName = ListBox_CsvFiles.SelectedItem.ToString();

            if (!File.Exists(currentSelectedCsvPath))
            {
                AddLog("CSV 파일이 존재하지 않습니다.", LogLevel.Error);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = currentSelectedCsvPath,
                    UseShellExecute = true
                });

                AddLog($"CSV 파일 열기: {fileName}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                AddLog($"CSV 열기 실패: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateDataSettingReason()
        {
            if (!CheckDataSettingAvailable(out string baseReason))
            {
                Btn_DataSetting.Enabled = false;
                return;
            }

            if (!CsvClassGenerator.ValidateCsv(currentSelectedCsvPath, out string csvError))
            {
                Btn_DataSetting.Enabled = false;

                AddLog("CSV 타입 검사 실패", LogLevel.Error);
                AddLog(csvError, LogLevel.Error);
                return;
            }

            Btn_DataSetting.Enabled = true;
        }
        private bool CheckDataSettingAvailable(out string reason)
        {
            var sb = new StringBuilder();

            if (string.IsNullOrEmpty(generatedCsvDir) || !Directory.Exists(generatedCsvDir))
                sb.AppendLine("- CSV 폴더 경로가 설정되지 않았거나 존재하지 않습니다.");

            if (string.IsNullOrEmpty(outputFolderPath) || !Directory.Exists(outputFolderPath))
                sb.AppendLine("- 출력 폴더 경로가 설정되지 않았거나 존재하지 않습니다.");

            if (ListBox_CsvFiles.SelectedItem == null)
                sb.AppendLine("- 변환할 CSV 파일이 선택되지 않았습니다.");

            reason = sb.ToString();
            return string.IsNullOrEmpty(reason);
        }

        private void AddLog(string message, LogLevel level = LogLevel.Info)
        {
            var entry = new LogEntry(level, message);
            allLogs.Add(entry);

            if (allLogs.Count > MAX_LOG_LINES)
                allLogs.RemoveRange(0, allLogs.Count - MAX_LOG_LINES);

            RefreshLogDisplay();
        }

        private void RefreshLogDisplay()
        {
            if (TextBox_Log.IsDisposed) return;

            if (TextBox_Log.InvokeRequired)
            {
                TextBox_Log.Invoke(new Action(RefreshLogDisplay));
                return;
            }

            TextBox_Log.Clear();

            var logsToShow = allLogs.Where(l => currentFilter == null || l.Level == currentFilter.Value);

            foreach (var log in logsToShow)
            {
                string time = log.Time.ToString("HH:mm:ss");
                string icon = "";
                switch (log.Level)
                {
                    case LogLevel.Info:
                        icon = "ℹ";
                        break;
                    case LogLevel.Warning:
                        icon = "⚠";
                        break;
                    case LogLevel.Error:
                        icon = "✖";
                        break;
                }

                string prefix = $"[{time}] {icon} [{log.Level.ToString().ToUpper()}] ";

                TextBox_Log.SelectionStart = TextBox_Log.TextLength;
                TextBox_Log.SelectionLength = 0;

                switch (log.Level)
                {
                    case LogLevel.Info:
                        TextBox_Log.SelectionColor = Color.Black;
                        break;
                    case LogLevel.Warning:
                        TextBox_Log.SelectionColor = Color.DarkOrchid;
                        break;
                    case LogLevel.Error:
                        TextBox_Log.SelectionColor = Color.Red;
                        break;
                }

                TextBox_Log.AppendText(prefix + log.Message + Environment.NewLine);
            }

            TextBox_Log.SelectionColor = TextBox_Log.ForeColor;
            TextBox_Log.ScrollToCaret();
        }

        private void Btn_OpenOutputFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(outputFolderPath) || !Directory.Exists(outputFolderPath))
            {
                AddLog("출력 폴더 경로가 유효하지 않습니다.", LogLevel.Warning);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = outputFolderPath,
                    UseShellExecute = true
                });

                AddLog($"출력 폴더 열기: {outputFolderPath}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                AddLog($"출력 폴더 열기 실패: {ex.Message}", LogLevel.Error);
            }
        }
        private void Txt_CsvFilter_TextChanged(object sender, EventArgs e)
        {
            string filter = Txt_CsvFilter.Text.ToLower();
            ListBox_CsvFiles.Items.Clear();

            foreach (var file in allCsvFiles)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                if (fileNameWithoutExt.ToLower().Contains(filter))
                    ListBox_CsvFiles.Items.Add(fileNameWithoutExt);
            }
        }
        private void UpdateRecentCsv(string fileName)
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

            recentCsvFiles = recentCsvFiles ?? new string[0];
            recentCsvFiles = recentCsvFiles.Where(f => !string.IsNullOrEmpty(f) && f != nameWithoutExt).ToArray();

            recentCsvFiles = (new string[] { nameWithoutExt }).Concat(recentCsvFiles).Take(MAX_RECENT_CSV).ToArray();

            Combo_RecentCsv.Items.Clear();
            Combo_RecentCsv.Items.AddRange(recentCsvFiles);

            var sc = new System.Collections.Specialized.StringCollection();
            sc.AddRange(recentCsvFiles);
            Properties.Settings.Default.RecentCsvFiles = sc;
            Properties.Settings.Default.Save();
        }
        private void Combo_RecentCsv_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Combo_RecentCsv.SelectedItem == null) return;

            string fileName = Combo_RecentCsv.SelectedItem.ToString();
            if (ListBox_CsvFiles.Items.Contains(fileName))
            {
                ListBox_CsvFiles.SelectedItem = fileName;
            }
        }

        private void Combo_LogFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (Combo_LogFilter.SelectedItem.ToString())
            {
                case "전체": currentFilter = null; break;
                case "Info": currentFilter = LogLevel.Info; break;
                case "Warning": currentFilter = LogLevel.Warning; break;
                case "Error": currentFilter = LogLevel.Error; break;
            }
            RefreshLogDisplay();
        }

        private void Btn_NewCsv_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(outputFolderPath) ||
                !Directory.Exists(outputFolderPath))
            {
                AddLog("CSV 생성 실패 - 출력 폴더 미설정", LogLevel.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(TextBox_NewCsvName.Text))
            {
                AddLog("CSV 생성 실패 - CSV 파일 이름 미설정", LogLevel.Warning);
                return;
            }
            try
            {
                string inputName = TextBox_NewCsvName.Text;

                string baseName = Path.GetFileNameWithoutExtension(inputName);
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    AddLog("CSV 생성 실패 - 잘못된 파일 이름", LogLevel.Warning);
                    return;
                }

                string newCsvPath = CsvCreator.CreateNew(
                    generatedCsvDir,
                    inputName
                );

                AddLog($"새 CSV 생성됨: {Path.GetFileName(newCsvPath)}", LogLevel.Info);

                LoadCsvList();

                string newName = Path.GetFileNameWithoutExtension(newCsvPath);
                ListBox_CsvFiles.SelectedItem = newName;

                MessageBox.Show(
                    "새 CSV 파일이 생성되었습니다.",
                    "완료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = generatedCsvDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddLog($"CSV 생성 실패: {ex.Message}", LogLevel.Error);
                MessageBox.Show(ex.Message, "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
