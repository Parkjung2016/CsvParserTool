using System;
using System.Collections.Concurrent;
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
        private readonly Button Btn_CloseExportResults = new Button();
        private readonly SplitContainer splitExportAndLog = new SplitContainer();
        private readonly TableLayoutPanel logHeaderLayout = new TableLayoutPanel();
        private bool exportResultSplitterInitialized;
        private int lastExportResultLayoutHeight = -1;
        private int exportResultViewportResetVersion;

        private readonly HashSet<string> checkedXlsxPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool allowCheckStateChange;
        private Icon taskbarExportStatusIcon;
        private NotifyIcon exportNotifyIcon;
        private System.Windows.Forms.Timer exportNotificationHideTimer;
        private System.Windows.Forms.Timer exportCompletionAnimationTimer;
        private readonly System.Windows.Forms.Timer exportLogFlushTimer = new System.Windows.Forms.Timer { Interval = 80 };
        private readonly ConcurrentQueue<string> pendingExportLogs = new ConcurrentQueue<string>();
        private int exportLogFlushScheduled;
        private int exportCompletionAnimationFrame;
        private Color exportCompletionAnimationStartColor;
        private string projectRootPath = "";
        private string excelSourceFolderPath = "";
        private string exportVersion = "1.0.0";
        private string lastWarnedInvalidExportVersion = string.Empty;

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
        private Size pendingResizeClientSize;
        private bool isInteractiveResize;

        private sealed class FileListEntry
        {
            public string FullPath { get; set; }
            public string DisplayName { get; set; }
        }

        private List<LogEntry> allLogs = new List<LogEntry>();
        private LogLevel? currentFilter = null;
        private bool splitContainersInitialized;
        private Size lastSplitLayoutClientSize = Size.Empty;
        private bool startupDialogsScheduled;
        private readonly Dictionary<string, DataGridViewRow> exportResultItems =
            new Dictionary<string, DataGridViewRow>(StringComparer.OrdinalIgnoreCase);

        private bool exportExcelPhaseRan;
        private bool exportHasTableFailure;
        private int exportActivePhaseIndex = -1;
        private ExportMiniGameForm exportMiniGameForm;

        public Form1()
        {
            InitializeComponent();
            InitializeExportLogSplitter();
            InitializeExportResultControls();
            InitializeLogHeaderLayout();
            InitializeInfoButton();
            exportLogFlushTimer.Tick += (_, __) => FlushPendingExportLogs();

            bool darkMode = ToolSettingsStore.DarkMode;
            UITheme.SetTheme(UITheme.ParseTheme(ToolSettingsStore.ThemeName), darkMode);
            Chk_DarkMode.Checked = darkMode;

            ApplyUITheme();
            ApplyAppIcon();
        }


    }
}