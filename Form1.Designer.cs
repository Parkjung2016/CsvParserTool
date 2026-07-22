using System.Drawing;
using System.Windows.Forms;

namespace CSVParserTool
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.Panel_Header = new System.Windows.Forms.Panel();
            this.tableHeader = new System.Windows.Forms.TableLayoutPanel();
            this.PictureBox_HeaderIcon = new System.Windows.Forms.PictureBox();
            this.Label_AppTitle = new System.Windows.Forms.Label();
            this.Label_AppSubtitle = new System.Windows.Forms.Label();
            this.Chk_DarkMode = new System.Windows.Forms.CheckBox();
            this.Btn_DataSetting = new System.Windows.Forms.Button();
            this.Panel_Top = new System.Windows.Forms.Panel();
            this.tableTop = new System.Windows.Forms.TableLayoutPanel();
            this.Btn_SelectProjectRoot = new System.Windows.Forms.Button();
            this.Label_ProjectRoot = new System.Windows.Forms.Label();
            this.Btn_SelectExcelFolder = new System.Windows.Forms.Button();
            this.Label_ExcelSourcePath = new System.Windows.Forms.Label();
            this.Label_ExportVersion = new System.Windows.Forms.Label();
            this.Flow_ExportOptions = new System.Windows.Forms.FlowLayoutPanel();
            this.Txt_ExportVersion = new System.Windows.Forms.TextBox();
            this.Chk_RemoveOrphanArtifacts = new System.Windows.Forms.CheckBox();
            this.tableFilterRow = new System.Windows.Forms.TableLayoutPanel();
            this.Label_CsvFilter = new System.Windows.Forms.Label();
            this.Txt_CsvFilter = new System.Windows.Forms.TextBox();
            this.Btn_NewCsv = new System.Windows.Forms.Button();
            this.TextBox_NewCsvName = new System.Windows.Forms.TextBox();
            this.Panel_Bottom = new System.Windows.Forms.Panel();
            this.tableBottom = new System.Windows.Forms.TableLayoutPanel();
            this.Btn_OpenOutputFolder = new System.Windows.Forms.Button();
            this.Btn_OpenCsvFolder = new System.Windows.Forms.Button();
            this.Btn_OpenXlsxFolder = new System.Windows.Forms.Button();
            this.Panel_MainContent = new System.Windows.Forms.Panel();
            this.splitOuter = new System.Windows.Forms.SplitContainer();
            this.splitWork = new System.Windows.Forms.SplitContainer();
            this.Panel_ListCard = new System.Windows.Forms.Panel();
            this.ListBox_CsvFiles = new System.Windows.Forms.CheckedListBox();
            this.Panel_ListHeader = new System.Windows.Forms.Panel();
            this.Label_SectionList = new System.Windows.Forms.Label();
            this.Btn_RefreshList = new System.Windows.Forms.Button();
            this.Panel_PreviewCard = new System.Windows.Forms.Panel();
            this.TextBox_Preview = new System.Windows.Forms.RichTextBox();
            this.Label_SectionPreview = new System.Windows.Forms.Label();
            this.Panel_LogSection = new System.Windows.Forms.Panel();
            this.Panel_LogCard = new System.Windows.Forms.Panel();
            this.TextBox_Log = new System.Windows.Forms.RichTextBox();
            this.Panel_LogHeader = new System.Windows.Forms.Panel();
            this.Combo_LogFilter = new System.Windows.Forms.ComboBox();
            this.Label_SectionLog = new System.Windows.Forms.Label();
            this.Btn_ClearLog = new System.Windows.Forms.Button();
            this.Panel_ExportProgress = new System.Windows.Forms.Panel();
            this.Grid_ExportResults = new CSVParserTool.BufferedDataGridView();
            this.ColumnExportTable = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnExportStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnExportMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Panel_ExportProgressTop = new System.Windows.Forms.Panel();
            this.SegmentedExportProgress_Export = new CSVParserTool.SegmentedExportProgressBar();
            this.Label_ExportStatus = new System.Windows.Forms.Label();
            this.Label_SectionExport = new System.Windows.Forms.Label();
            this.Panel_Header.SuspendLayout();
            this.tableHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox_HeaderIcon)).BeginInit();
            this.Panel_Top.SuspendLayout();
            this.tableTop.SuspendLayout();
            this.Flow_ExportOptions.SuspendLayout();
            this.tableFilterRow.SuspendLayout();
            this.Panel_Bottom.SuspendLayout();
            this.tableBottom.SuspendLayout();
            this.Panel_MainContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitOuter)).BeginInit();
            this.splitOuter.Panel1.SuspendLayout();
            this.splitOuter.Panel2.SuspendLayout();
            this.splitOuter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitWork)).BeginInit();
            this.splitWork.Panel1.SuspendLayout();
            this.splitWork.Panel2.SuspendLayout();
            this.splitWork.SuspendLayout();
            this.Panel_ListCard.SuspendLayout();
            this.Panel_ListHeader.SuspendLayout();
            this.Panel_PreviewCard.SuspendLayout();
            this.Panel_LogSection.SuspendLayout();
            this.Panel_LogCard.SuspendLayout();
            this.Panel_LogHeader.SuspendLayout();
            this.Panel_ExportProgress.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Grid_ExportResults)).BeginInit();
            this.Panel_ExportProgressTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // Panel_Header
            // 
            this.Panel_Header.AutoSize = true;
            this.Panel_Header.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Panel_Header.Controls.Add(this.tableHeader);
            this.Panel_Header.Dock = System.Windows.Forms.DockStyle.Top;
            this.Panel_Header.Location = new System.Drawing.Point(0, 0);
            this.Panel_Header.Name = "Panel_Header";
            this.Panel_Header.Size = new System.Drawing.Size(1140, 40);
            this.Panel_Header.TabIndex = 3;
            // 
            // tableHeader
            // 
            this.tableHeader.AutoSize = true;
            this.tableHeader.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableHeader.ColumnCount = 4;
            this.tableHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableHeader.Controls.Add(this.PictureBox_HeaderIcon, 0, 0);
            this.tableHeader.Controls.Add(this.Label_AppTitle, 1, 0);
            this.tableHeader.Controls.Add(this.Label_AppSubtitle, 1, 1);
            this.tableHeader.Controls.Add(this.Chk_DarkMode, 2, 0);
            this.tableHeader.Controls.Add(this.Btn_DataSetting, 3, 0);
            this.tableHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableHeader.Location = new System.Drawing.Point(0, 0);
            this.tableHeader.Name = "tableHeader";
            this.tableHeader.RowCount = 2;
            this.tableHeader.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableHeader.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableHeader.Size = new System.Drawing.Size(1140, 40);
            this.tableHeader.TabIndex = 0;
            // 
            // PictureBox_HeaderIcon
            // 
            this.PictureBox_HeaderIcon.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.PictureBox_HeaderIcon.Location = new System.Drawing.Point(0, 0);
            this.PictureBox_HeaderIcon.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.PictureBox_HeaderIcon.Name = "PictureBox_HeaderIcon";
            this.tableHeader.SetRowSpan(this.PictureBox_HeaderIcon, 2);
            this.PictureBox_HeaderIcon.Size = new System.Drawing.Size(40, 40);
            this.PictureBox_HeaderIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PictureBox_HeaderIcon.TabIndex = 4;
            this.PictureBox_HeaderIcon.TabStop = false;
            // 
            // Label_AppTitle
            // 
            this.Label_AppTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Label_AppTitle.AutoSize = true;
            this.Label_AppTitle.Location = new System.Drawing.Point(52, 0);
            this.Label_AppTitle.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.Label_AppTitle.Name = "Label_AppTitle";
            this.Label_AppTitle.Size = new System.Drawing.Size(40, 12);
            this.Label_AppTitle.TabIndex = 0;
            this.Label_AppTitle.Text = "PJDev";
            // 
            // Label_AppSubtitle
            // 
            this.Label_AppSubtitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_AppSubtitle.AutoEllipsis = true;
            this.Label_AppSubtitle.Location = new System.Drawing.Point(52, 16);
            this.Label_AppSubtitle.Margin = new System.Windows.Forms.Padding(0, 4, 12, 0);
            this.Label_AppSubtitle.Name = "Label_AppSubtitle";
            this.Label_AppSubtitle.Size = new System.Drawing.Size(826, 20);
            this.Label_AppSubtitle.TabIndex = 1;
            this.Label_AppSubtitle.Text = "Data Tool · Unity XLSX → CSV · Export";
            this.Label_AppSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Chk_DarkMode
            // 
            this.Chk_DarkMode.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Chk_DarkMode.AutoSize = true;
            this.Chk_DarkMode.Location = new System.Drawing.Point(890, 12);
            this.Chk_DarkMode.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.Chk_DarkMode.Name = "Chk_DarkMode";
            this.tableHeader.SetRowSpan(this.Chk_DarkMode, 2);
            this.Chk_DarkMode.Size = new System.Drawing.Size(76, 16);
            this.Chk_DarkMode.TabIndex = 3;
            this.Chk_DarkMode.Text = "다크 모드";
            this.Chk_DarkMode.UseVisualStyleBackColor = true;
            this.Chk_DarkMode.CheckedChanged += new System.EventHandler(this.Chk_DarkMode_CheckedChanged);
            // 
            // Btn_DataSetting
            // 
            this.Btn_DataSetting.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Btn_DataSetting.AutoSize = true;
            this.Btn_DataSetting.Location = new System.Drawing.Point(978, 0);
            this.Btn_DataSetting.Margin = new System.Windows.Forms.Padding(0);
            this.Btn_DataSetting.Name = "Btn_DataSetting";
            this.tableHeader.SetRowSpan(this.Btn_DataSetting, 2);
            this.Btn_DataSetting.Size = new System.Drawing.Size(162, 40);
            this.Btn_DataSetting.TabIndex = 2;
            this.Btn_DataSetting.Text = "데이터 Export";
            this.Btn_DataSetting.Click += new System.EventHandler(this.Btn_DataSetting_Click);
            // 
            // Panel_Top
            // 
            this.Panel_Top.AutoSize = true;
            this.Panel_Top.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Panel_Top.Controls.Add(this.tableTop);
            this.Panel_Top.Dock = System.Windows.Forms.DockStyle.Top;
            this.Panel_Top.Location = new System.Drawing.Point(0, 40);
            this.Panel_Top.MinimumSize = new System.Drawing.Size(0, 166);
            this.Panel_Top.Name = "Panel_Top";
            this.Panel_Top.Size = new System.Drawing.Size(1140, 166);
            this.Panel_Top.TabIndex = 2;
            // 
            // tableTop
            // 
            this.tableTop.AutoSize = true;
            this.tableTop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableTop.ColumnCount = 2;
            this.tableTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 132F));
            this.tableTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableTop.Controls.Add(this.Btn_SelectProjectRoot, 0, 0);
            this.tableTop.Controls.Add(this.Label_ProjectRoot, 1, 0);
            this.tableTop.Controls.Add(this.Btn_SelectExcelFolder, 0, 1);
            this.tableTop.Controls.Add(this.Label_ExcelSourcePath, 1, 1);
            this.tableTop.Controls.Add(this.Label_ExportVersion, 0, 2);
            this.tableTop.Controls.Add(this.Flow_ExportOptions, 1, 2);
            this.tableTop.Controls.Add(this.tableFilterRow, 0, 3);
            this.tableTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableTop.Location = new System.Drawing.Point(0, 0);
            this.tableTop.Name = "tableTop";
            this.tableTop.RowCount = 4;
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableTop.Size = new System.Drawing.Size(1140, 166);
            this.tableTop.TabIndex = 0;
            // 
            // Btn_SelectProjectRoot
            // 
            this.Btn_SelectProjectRoot.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Btn_SelectProjectRoot.AutoSize = true;
            this.Btn_SelectProjectRoot.Location = new System.Drawing.Point(0, 5);
            this.Btn_SelectProjectRoot.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.Btn_SelectProjectRoot.Name = "Btn_SelectProjectRoot";
            this.Btn_SelectProjectRoot.Size = new System.Drawing.Size(119, 28);
            this.Btn_SelectProjectRoot.TabIndex = 0;
            this.Btn_SelectProjectRoot.Text = "프로젝트 경로";
            this.Btn_SelectProjectRoot.Click += new System.EventHandler(this.Btn_SelectProjectRoot_Click);
            // 
            // Label_ProjectRoot
            // 
            this.Label_ProjectRoot.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_ProjectRoot.AutoEllipsis = true;
            this.Label_ProjectRoot.Location = new System.Drawing.Point(135, 0);
            this.Label_ProjectRoot.Margin = new System.Windows.Forms.Padding(3, 0, 0, 4);
            this.Label_ProjectRoot.Name = "Label_ProjectRoot";
            this.Label_ProjectRoot.Size = new System.Drawing.Size(1005, 34);
            this.Label_ProjectRoot.TabIndex = 1;
            this.Label_ProjectRoot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Btn_SelectExcelFolder
            // 
            this.Btn_SelectExcelFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Btn_SelectExcelFolder.AutoSize = true;
            this.Btn_SelectExcelFolder.Location = new System.Drawing.Point(0, 43);
            this.Btn_SelectExcelFolder.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.Btn_SelectExcelFolder.Name = "Btn_SelectExcelFolder";
            this.Btn_SelectExcelFolder.Size = new System.Drawing.Size(102, 28);
            this.Btn_SelectExcelFolder.TabIndex = 10;
            this.Btn_SelectExcelFolder.Text = "XLSX 원본";
            this.Btn_SelectExcelFolder.Click += new System.EventHandler(this.Btn_SelectExcelFolder_Click);
            // 
            // Label_ExcelSourcePath
            // 
            this.Label_ExcelSourcePath.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_ExcelSourcePath.AutoEllipsis = true;
            this.Label_ExcelSourcePath.Location = new System.Drawing.Point(135, 38);
            this.Label_ExcelSourcePath.Margin = new System.Windows.Forms.Padding(3, 0, 0, 4);
            this.Label_ExcelSourcePath.Name = "Label_ExcelSourcePath";
            this.Label_ExcelSourcePath.Size = new System.Drawing.Size(1005, 34);
            this.Label_ExcelSourcePath.TabIndex = 11;
            this.Label_ExcelSourcePath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label_ExportVersion
            // 
            this.Label_ExportVersion.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Label_ExportVersion.AutoSize = true;
            this.Label_ExportVersion.Location = new System.Drawing.Point(0, 89);
            this.Label_ExportVersion.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.Label_ExportVersion.Name = "Label_ExportVersion";
            this.Label_ExportVersion.Size = new System.Drawing.Size(69, 12);
            this.Label_ExportVersion.TabIndex = 12;
            this.Label_ExportVersion.Text = "Export 버전";
            this.Label_ExportVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Flow_ExportOptions
            // 
            this.Flow_ExportOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Flow_ExportOptions.AutoSize = true;
            this.Flow_ExportOptions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Flow_ExportOptions.Controls.Add(this.Txt_ExportVersion);
            this.Flow_ExportOptions.Controls.Add(this.Chk_RemoveOrphanArtifacts);
            this.Flow_ExportOptions.Location = new System.Drawing.Point(135, 81);
            this.Flow_ExportOptions.Margin = new System.Windows.Forms.Padding(3, 0, 0, 4);
            this.Flow_ExportOptions.Name = "Flow_ExportOptions";
            this.Flow_ExportOptions.Size = new System.Drawing.Size(1005, 23);
            this.Flow_ExportOptions.TabIndex = 14;
            this.Flow_ExportOptions.WrapContents = false;
            // 
            // Txt_ExportVersion
            // 
            this.Txt_ExportVersion.Location = new System.Drawing.Point(0, 2);
            this.Txt_ExportVersion.Margin = new System.Windows.Forms.Padding(0, 2, 12, 0);
            this.Txt_ExportVersion.Name = "Txt_ExportVersion";
            this.Txt_ExportVersion.Size = new System.Drawing.Size(120, 21);
            this.Txt_ExportVersion.TabIndex = 0;
            this.Txt_ExportVersion.Text = "1.0.0";
            this.Txt_ExportVersion.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Txt_ExportVersion_KeyDown);
            this.Txt_ExportVersion.Leave += new System.EventHandler(this.Txt_ExportVersion_Leave);
            // 
            // Chk_RemoveOrphanArtifacts
            // 
            this.Chk_RemoveOrphanArtifacts.AutoSize = true;
            this.Chk_RemoveOrphanArtifacts.Checked = true;
            this.Chk_RemoveOrphanArtifacts.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Chk_RemoveOrphanArtifacts.Location = new System.Drawing.Point(132, 4);
            this.Chk_RemoveOrphanArtifacts.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.Chk_RemoveOrphanArtifacts.Name = "Chk_RemoveOrphanArtifacts";
            this.Chk_RemoveOrphanArtifacts.Size = new System.Drawing.Size(184, 16);
            this.Chk_RemoveOrphanArtifacts.TabIndex = 1;
            this.Chk_RemoveOrphanArtifacts.Text = "원본 없는 테이블 산출물 정리";
            this.Chk_RemoveOrphanArtifacts.UseVisualStyleBackColor = true;
            this.Chk_RemoveOrphanArtifacts.CheckedChanged += new System.EventHandler(this.Chk_RemoveOrphanArtifacts_CheckedChanged);
            // 
            // tableFilterRow
            // 
            this.tableFilterRow.ColumnCount = 4;
            this.tableTop.SetColumnSpan(this.tableFilterRow, 2);
            this.tableFilterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableFilterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableFilterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableFilterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 148F));
            this.tableFilterRow.Controls.Add(this.Label_CsvFilter, 0, 0);
            this.tableFilterRow.Controls.Add(this.Txt_CsvFilter, 1, 0);
            this.tableFilterRow.Controls.Add(this.Btn_NewCsv, 2, 0);
            this.tableFilterRow.Controls.Add(this.TextBox_NewCsvName, 3, 0);
            this.tableFilterRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableFilterRow.Location = new System.Drawing.Point(0, 118);
            this.tableFilterRow.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.tableFilterRow.Name = "tableFilterRow";
            this.tableFilterRow.RowCount = 1;
            this.tableFilterRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableFilterRow.Size = new System.Drawing.Size(1140, 48);
            this.tableFilterRow.TabIndex = 20;
            // 
            // Label_CsvFilter
            // 
            this.Label_CsvFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Label_CsvFilter.AutoSize = true;
            this.Label_CsvFilter.Location = new System.Drawing.Point(0, 18);
            this.Label_CsvFilter.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.Label_CsvFilter.Name = "Label_CsvFilter";
            this.Label_CsvFilter.Size = new System.Drawing.Size(57, 12);
            this.Label_CsvFilter.TabIndex = 0;
            this.Label_CsvFilter.Text = "목록 필터";
            // 
            // Txt_CsvFilter
            // 
            this.Txt_CsvFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_CsvFilter.Location = new System.Drawing.Point(69, 13);
            this.Txt_CsvFilter.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.Txt_CsvFilter.Name = "Txt_CsvFilter";
            this.Txt_CsvFilter.Size = new System.Drawing.Size(828, 21);
            this.Txt_CsvFilter.TabIndex = 1;
            this.Txt_CsvFilter.TextChanged += new System.EventHandler(this.Txt_CsvFilter_TextChanged);
            // 
            // Btn_NewCsv
            // 
            this.Btn_NewCsv.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Btn_NewCsv.AutoSize = true;
            this.Btn_NewCsv.Location = new System.Drawing.Point(905, 10);
            this.Btn_NewCsv.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.Btn_NewCsv.Name = "Btn_NewCsv";
            this.Btn_NewCsv.Size = new System.Drawing.Size(79, 28);
            this.Btn_NewCsv.TabIndex = 8;
            this.Btn_NewCsv.Text = "새 XLSX";
            this.Btn_NewCsv.Click += new System.EventHandler(this.Btn_NewCsv_Click);
            // 
            // TextBox_NewCsvName
            // 
            this.TextBox_NewCsvName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_NewCsvName.Location = new System.Drawing.Point(992, 13);
            this.TextBox_NewCsvName.Margin = new System.Windows.Forms.Padding(0);
            this.TextBox_NewCsvName.Name = "TextBox_NewCsvName";
            this.TextBox_NewCsvName.Size = new System.Drawing.Size(148, 21);
            this.TextBox_NewCsvName.TabIndex = 9;
            // 
            // Panel_Bottom
            // 
            this.Panel_Bottom.AutoSize = true;
            this.Panel_Bottom.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Panel_Bottom.Controls.Add(this.tableBottom);
            this.Panel_Bottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.Panel_Bottom.Location = new System.Drawing.Point(0, 903);
            this.Panel_Bottom.Name = "Panel_Bottom";
            this.Panel_Bottom.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            this.Panel_Bottom.Size = new System.Drawing.Size(1140, 58);
            this.Panel_Bottom.TabIndex = 1;
            // 
            // tableBottom
            // 
            this.tableBottom.AutoSize = true;
            this.tableBottom.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableBottom.ColumnCount = 4;
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableBottom.Controls.Add(this.Btn_OpenOutputFolder, 1, 0);
            this.tableBottom.Controls.Add(this.Btn_OpenCsvFolder, 2, 0);
            this.tableBottom.Controls.Add(this.Btn_OpenXlsxFolder, 3, 0);
            this.tableBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableBottom.Location = new System.Drawing.Point(16, 12);
            this.tableBottom.Name = "tableBottom";
            this.tableBottom.RowCount = 1;
            this.tableBottom.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableBottom.Size = new System.Drawing.Size(1108, 34);
            this.tableBottom.TabIndex = 0;
            // 
            // Btn_OpenOutputFolder
            // 
            this.Btn_OpenOutputFolder.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Btn_OpenOutputFolder.AutoSize = true;
            this.Btn_OpenOutputFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Btn_OpenOutputFolder.Location = new System.Drawing.Point(802, 0);
            this.Btn_OpenOutputFolder.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.Btn_OpenOutputFolder.Name = "Btn_OpenOutputFolder";
            this.Btn_OpenOutputFolder.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
            this.Btn_OpenOutputFolder.Size = new System.Drawing.Size(113, 34);
            this.Btn_OpenOutputFolder.TabIndex = 1;
            this.Btn_OpenOutputFolder.Text = "프로젝트 폴더";
            this.Btn_OpenOutputFolder.UseCompatibleTextRendering = true;
            this.Btn_OpenOutputFolder.Click += new System.EventHandler(this.Btn_OpenOutputFolder_Click);
            // 
            // Btn_OpenCsvFolder
            // 
            this.Btn_OpenCsvFolder.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Btn_OpenCsvFolder.AutoSize = true;
            this.Btn_OpenCsvFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Btn_OpenCsvFolder.Location = new System.Drawing.Point(921, 0);
            this.Btn_OpenCsvFolder.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.Btn_OpenCsvFolder.Name = "Btn_OpenCsvFolder";
            this.Btn_OpenCsvFolder.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
            this.Btn_OpenCsvFolder.Size = new System.Drawing.Size(88, 34);
            this.Btn_OpenCsvFolder.TabIndex = 4;
            this.Btn_OpenCsvFolder.Text = "출력 폴더";
            this.Btn_OpenCsvFolder.UseCompatibleTextRendering = true;
            this.Btn_OpenCsvFolder.Click += new System.EventHandler(this.Btn_OpenCsvFolder_Click);
            // 
            // Btn_OpenXlsxFolder
            // 
            this.Btn_OpenXlsxFolder.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Btn_OpenXlsxFolder.AutoSize = true;
            this.Btn_OpenXlsxFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Btn_OpenXlsxFolder.Location = new System.Drawing.Point(1015, 0);
            this.Btn_OpenXlsxFolder.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.Btn_OpenXlsxFolder.Name = "Btn_OpenXlsxFolder";
            this.Btn_OpenXlsxFolder.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
            this.Btn_OpenXlsxFolder.Size = new System.Drawing.Size(93, 34);
            this.Btn_OpenXlsxFolder.TabIndex = 3;
            this.Btn_OpenXlsxFolder.Text = "XLSX 폴더";
            this.Btn_OpenXlsxFolder.UseCompatibleTextRendering = true;
            this.Btn_OpenXlsxFolder.Click += new System.EventHandler(this.Btn_OpenXlsxFolder_Click);
            // 
            // Panel_MainContent
            // 
            this.Panel_MainContent.Controls.Add(this.splitOuter);
            this.Panel_MainContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_MainContent.Location = new System.Drawing.Point(0, 206);
            this.Panel_MainContent.Name = "Panel_MainContent";
            this.Panel_MainContent.Padding = new System.Windows.Forms.Padding(12, 4, 12, 4);
            this.Panel_MainContent.Size = new System.Drawing.Size(1140, 697);
            this.Panel_MainContent.TabIndex = 0;
            // 
            // splitOuter
            // 
            this.splitOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitOuter.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitOuter.Location = new System.Drawing.Point(12, 4);
            this.splitOuter.Name = "splitOuter";
            this.splitOuter.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitOuter.Panel1
            // 
            this.splitOuter.Panel1.Controls.Add(this.splitWork);
            this.splitOuter.Panel1MinSize = 180;
            // 
            // splitOuter.Panel2
            // 
            this.splitOuter.Panel2.Controls.Add(this.Panel_LogSection);
            this.splitOuter.Panel2MinSize = 180;
            this.splitOuter.Size = new System.Drawing.Size(1116, 689);
            this.splitOuter.SplitterDistance = 503;
            this.splitOuter.SplitterWidth = 6;
            this.splitOuter.TabIndex = 0;
            // 
            // splitWork
            // 
            this.splitWork.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitWork.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitWork.Location = new System.Drawing.Point(0, 0);
            this.splitWork.Name = "splitWork";
            // 
            // splitWork.Panel1
            // 
            this.splitWork.Panel1.Controls.Add(this.Panel_ListCard);
            this.splitWork.Panel1.Controls.Add(this.Panel_ListHeader);
            this.splitWork.Panel1.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.splitWork.Panel1MinSize = 200;
            // 
            // splitWork.Panel2
            // 
            this.splitWork.Panel2.Controls.Add(this.Panel_PreviewCard);
            this.splitWork.Panel2.Controls.Add(this.Label_SectionPreview);
            this.splitWork.Panel2.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.splitWork.Panel2MinSize = 280;
            this.splitWork.Size = new System.Drawing.Size(1116, 503);
            this.splitWork.SplitterDistance = 288;
            this.splitWork.SplitterWidth = 6;
            this.splitWork.TabIndex = 0;
            // 
            // Panel_ListCard
            //
            this.Panel_ListCard.Controls.Add(this.ListBox_CsvFiles);
            this.Panel_ListCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_ListCard.Location = new System.Drawing.Point(0, 28);
            this.Panel_ListCard.Name = "Panel_ListCard";
            this.Panel_ListCard.Size = new System.Drawing.Size(280, 475);
            this.Panel_ListCard.TabIndex = 0;
            //
            // ListBox_CsvFiles
            //
            this.ListBox_CsvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListBox_CsvFiles.IntegralHeight = false;
            this.ListBox_CsvFiles.Location = new System.Drawing.Point(0, 0);
            this.ListBox_CsvFiles.Name = "ListBox_CsvFiles";
            this.ListBox_CsvFiles.Size = new System.Drawing.Size(280, 475);
            this.ListBox_CsvFiles.TabIndex = 0;
            this.ListBox_CsvFiles.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.ListBox_CsvFiles_ItemCheck);
            this.ListBox_CsvFiles.SelectedIndexChanged += new System.EventHandler(this.ListBox_CsvFiles_SelectedIndexChanged);
            this.ListBox_CsvFiles.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ListBox_CsvFiles_KeyDown);
            this.ListBox_CsvFiles.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ListBox_CsvFiles_MouseDoubleClick);
            //
            // Panel_ListHeader
            // 
            this.Panel_ListHeader.Controls.Add(this.Label_SectionList);
            this.Panel_ListHeader.Controls.Add(this.Btn_RefreshList);
            this.Panel_ListHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.Panel_ListHeader.Location = new System.Drawing.Point(0, 0);
            this.Panel_ListHeader.Name = "Panel_ListHeader";
            this.Panel_ListHeader.Size = new System.Drawing.Size(280, 28);
            this.Panel_ListHeader.TabIndex = 2;
            // 
            // Label_SectionList
            // 
            this.Label_SectionList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Label_SectionList.Location = new System.Drawing.Point(0, 0);
            this.Label_SectionList.Name = "Label_SectionList";
            this.Label_SectionList.Padding = new System.Windows.Forms.Padding(2, 0, 0, 8);
            this.Label_SectionList.Size = new System.Drawing.Size(204, 28);
            this.Label_SectionList.TabIndex = 0;
            this.Label_SectionList.Text = "테이블 · Enum";
            // 
            // Btn_RefreshList
            // 
            this.Btn_RefreshList.AutoSize = true;
            this.Btn_RefreshList.Dock = System.Windows.Forms.DockStyle.Right;
            this.Btn_RefreshList.Location = new System.Drawing.Point(204, 0);
            this.Btn_RefreshList.Margin = new System.Windows.Forms.Padding(0);
            this.Btn_RefreshList.Name = "Btn_RefreshList";
            this.Btn_RefreshList.Padding = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.Btn_RefreshList.Size = new System.Drawing.Size(76, 28);
            this.Btn_RefreshList.TabIndex = 1;
            this.Btn_RefreshList.Text = "새로고침";
            this.Btn_RefreshList.UseCompatibleTextRendering = true;
            this.Btn_RefreshList.Click += new System.EventHandler(this.Btn_RefreshList_Click);
            // 
            // Panel_PreviewCard
            // 
            this.Panel_PreviewCard.Controls.Add(this.TextBox_Preview);
            this.Panel_PreviewCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_PreviewCard.Location = new System.Drawing.Point(8, 28);
            this.Panel_PreviewCard.Name = "Panel_PreviewCard";
            this.Panel_PreviewCard.Size = new System.Drawing.Size(814, 475);
            this.Panel_PreviewCard.TabIndex = 0;
            // 
            // TextBox_Preview
            // 
            this.TextBox_Preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextBox_Preview.Location = new System.Drawing.Point(0, 0);
            this.TextBox_Preview.Name = "TextBox_Preview";
            this.TextBox_Preview.ReadOnly = true;
            this.TextBox_Preview.Size = new System.Drawing.Size(814, 475);
            this.TextBox_Preview.TabIndex = 0;
            this.TextBox_Preview.Text = "";
            // 
            // Label_SectionPreview
            //
            this.Label_SectionPreview.Dock = System.Windows.Forms.DockStyle.Top;
            this.Label_SectionPreview.Location = new System.Drawing.Point(8, 0);
            this.Label_SectionPreview.Name = "Label_SectionPreview";
            this.Label_SectionPreview.Padding = new System.Windows.Forms.Padding(2, 0, 0, 8);
            this.Label_SectionPreview.Size = new System.Drawing.Size(814, 28);
            this.Label_SectionPreview.TabIndex = 1;
            this.Label_SectionPreview.Text = "코드 미리보기";
            //
            // Panel_LogSection
            // 
            this.Panel_LogSection.Controls.Add(this.Panel_LogCard);
            this.Panel_LogSection.Controls.Add(this.Panel_LogHeader);
            this.Panel_LogSection.Controls.Add(this.Panel_ExportProgress);
            this.Panel_LogSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_LogSection.Location = new System.Drawing.Point(0, 0);
            this.Panel_LogSection.Name = "Panel_LogSection";
            this.Panel_LogSection.Size = new System.Drawing.Size(1116, 180);
            this.Panel_LogSection.TabIndex = 0;
            // 
            // Panel_LogCard
            //
            this.Panel_LogCard.Controls.Add(this.TextBox_Log);
            this.Panel_LogCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_LogCard.Location = new System.Drawing.Point(0, 200);
            this.Panel_LogCard.Name = "Panel_LogCard";
            this.Panel_LogCard.Size = new System.Drawing.Size(1116, 0);
            this.Panel_LogCard.TabIndex = 0;
            //
            // TextBox_Log
            //
            this.TextBox_Log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextBox_Log.Location = new System.Drawing.Point(0, 0);
            this.TextBox_Log.Name = "TextBox_Log";
            this.TextBox_Log.ReadOnly = true;
            this.TextBox_Log.Size = new System.Drawing.Size(1116, 0);
            this.TextBox_Log.TabIndex = 0;
            this.TextBox_Log.Text = "";
            //
            // Panel_LogHeader
            //
            this.Panel_LogHeader.Controls.Add(this.Combo_LogFilter);
            this.Panel_LogHeader.Controls.Add(this.Label_SectionLog);
            this.Panel_LogHeader.Controls.Add(this.Btn_ClearLog);
            this.Panel_LogHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.Panel_LogHeader.Location = new System.Drawing.Point(0, 168);
            this.Panel_LogHeader.Name = "Panel_LogHeader";
            this.Panel_LogHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.Panel_LogHeader.Size = new System.Drawing.Size(1116, 32);
            this.Panel_LogHeader.TabIndex = 1;
            //
            // Combo_LogFilter
            //
            this.Combo_LogFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Combo_LogFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Combo_LogFilter.Items.AddRange(new object[] {
            "전체",
            "Info",
            "Warning",
            "Error"});
            this.Combo_LogFilter.Location = new System.Drawing.Point(52, 4);
            this.Combo_LogFilter.Margin = new System.Windows.Forms.Padding(0);
            this.Combo_LogFilter.Name = "Combo_LogFilter";
            this.Combo_LogFilter.Size = new System.Drawing.Size(120, 20);
            this.Combo_LogFilter.TabIndex = 2;
            this.Combo_LogFilter.SelectedIndexChanged += new System.EventHandler(this.Combo_LogFilter_SelectedIndexChanged);
            //
            // Label_SectionLog
            //
            this.Label_SectionLog.AutoSize = true;
            this.Label_SectionLog.Location = new System.Drawing.Point(2, 8);
            this.Label_SectionLog.Name = "Label_SectionLog";
            this.Label_SectionLog.Size = new System.Drawing.Size(29, 12);
            this.Label_SectionLog.TabIndex = 0;
            this.Label_SectionLog.Text = "로그";
            //
            // Btn_ClearLog
            //
            this.Btn_ClearLog.AutoSize = true;
            this.Btn_ClearLog.Dock = System.Windows.Forms.DockStyle.Right;
            this.Btn_ClearLog.Location = new System.Drawing.Point(1024, 0);
            this.Btn_ClearLog.Margin = new System.Windows.Forms.Padding(0);
            this.Btn_ClearLog.Name = "Btn_ClearLog";
            this.Btn_ClearLog.Padding = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.Btn_ClearLog.Size = new System.Drawing.Size(92, 26);
            this.Btn_ClearLog.TabIndex = 3;
            this.Btn_ClearLog.Text = "전체 지우기";
            this.Btn_ClearLog.UseCompatibleTextRendering = true;
            this.Btn_ClearLog.Click += new System.EventHandler(this.Btn_ClearLog_Click);
            //
            // Panel_ExportProgress
            // 
            this.Panel_ExportProgress.Controls.Add(this.Grid_ExportResults);
            this.Panel_ExportProgress.Controls.Add(this.Panel_ExportProgressTop);
            this.Panel_ExportProgress.Dock = System.Windows.Forms.DockStyle.Top;
            this.Panel_ExportProgress.Location = new System.Drawing.Point(0, 0);
            this.Panel_ExportProgress.Name = "Panel_ExportProgress";
            this.Panel_ExportProgress.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.Panel_ExportProgress.Size = new System.Drawing.Size(1116, 168);
            this.Panel_ExportProgress.TabIndex = 2;
            this.Panel_ExportProgress.Visible = false;
            // 
            // Grid_ExportResults
            // 
            this.Grid_ExportResults.AllowUserToAddRows = false;
            this.Grid_ExportResults.AllowUserToDeleteRows = false;
            this.Grid_ExportResults.AllowUserToResizeColumns = false;
            this.Grid_ExportResults.AllowUserToResizeRows = false;
            this.Grid_ExportResults.ColumnHeadersHeight = 30;
            this.Grid_ExportResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.Grid_ExportResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnExportTable,
            this.ColumnExportStatus,
            this.ColumnExportMessage});
            this.Grid_ExportResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Grid_ExportResults.Location = new System.Drawing.Point(0, 88);
            this.Grid_ExportResults.MultiSelect = false;
            this.Grid_ExportResults.Name = "Grid_ExportResults";
            this.Grid_ExportResults.ReadOnly = true;
            this.Grid_ExportResults.RowHeadersVisible = false;
            this.Grid_ExportResults.RowTemplate.Height = 26;
            this.Grid_ExportResults.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.Grid_ExportResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.Grid_ExportResults.Size = new System.Drawing.Size(1116, 74);
            this.Grid_ExportResults.TabIndex = 1;
            this.Grid_ExportResults.SizeChanged += new System.EventHandler(this.Grid_ExportResults_SizeChanged);
            // 
            // ColumnExportTable
            // 
            this.ColumnExportTable.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnExportTable.FillWeight = 30F;
            this.ColumnExportTable.HeaderText = "테이블";
            this.ColumnExportTable.MinimumWidth = 140;
            this.ColumnExportTable.Name = "ColumnExportTable";
            this.ColumnExportTable.ReadOnly = true;
            this.ColumnExportTable.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // ColumnExportStatus
            // 
            this.ColumnExportStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.ColumnExportStatus.HeaderText = "상태";
            this.ColumnExportStatus.Name = "ColumnExportStatus";
            this.ColumnExportStatus.ReadOnly = true;
            this.ColumnExportStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.ColumnExportStatus.Width = 76;
            // 
            // ColumnExportMessage
            // 
            this.ColumnExportMessage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnExportMessage.FillWeight = 70F;
            this.ColumnExportMessage.HeaderText = "내용";
            this.ColumnExportMessage.MinimumWidth = 160;
            this.ColumnExportMessage.Name = "ColumnExportMessage";
            this.ColumnExportMessage.ReadOnly = true;
            this.ColumnExportMessage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Panel_ExportProgressTop
            // 
            this.Panel_ExportProgressTop.Controls.Add(this.SegmentedExportProgress_Export);
            this.Panel_ExportProgressTop.Controls.Add(this.Label_ExportStatus);
            this.Panel_ExportProgressTop.Controls.Add(this.Label_SectionExport);
            this.Panel_ExportProgressTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.Panel_ExportProgressTop.Location = new System.Drawing.Point(0, 0);
            this.Panel_ExportProgressTop.Name = "Panel_ExportProgressTop";
            this.Panel_ExportProgressTop.Padding = new System.Windows.Forms.Padding(2, 0, 2, 4);
            this.Panel_ExportProgressTop.Size = new System.Drawing.Size(1116, 88);
            this.Panel_ExportProgressTop.TabIndex = 0;
            // 
            // SegmentedExportProgress_Export
            // 
            this.SegmentedExportProgress_Export.Dock = System.Windows.Forms.DockStyle.Top;
            this.SegmentedExportProgress_Export.Location = new System.Drawing.Point(2, 39);
            this.SegmentedExportProgress_Export.MinimumSize = new System.Drawing.Size(240, 41);
            this.SegmentedExportProgress_Export.Name = "SegmentedExportProgress_Export";
            this.SegmentedExportProgress_Export.Size = new System.Drawing.Size(1112, 41);
            this.SegmentedExportProgress_Export.TabIndex = 2;
            this.SegmentedExportProgress_Export.TabStop = false;
            // 
            // Label_ExportStatus
            // 
            this.Label_ExportStatus.AutoEllipsis = true;
            this.Label_ExportStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.Label_ExportStatus.Location = new System.Drawing.Point(2, 14);
            this.Label_ExportStatus.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.Label_ExportStatus.Name = "Label_ExportStatus";
            this.Label_ExportStatus.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.Label_ExportStatus.Size = new System.Drawing.Size(1112, 25);
            this.Label_ExportStatus.TabIndex = 1;
            this.Label_ExportStatus.Text = "Export 준비";
            this.Label_ExportStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label_SectionExport
            // 
            this.Label_SectionExport.AutoSize = true;
            this.Label_SectionExport.Dock = System.Windows.Forms.DockStyle.Top;
            this.Label_SectionExport.Location = new System.Drawing.Point(2, 0);
            this.Label_SectionExport.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.Label_SectionExport.Name = "Label_SectionExport";
            this.Label_SectionExport.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.Label_SectionExport.Size = new System.Drawing.Size(41, 14);
            this.Label_SectionExport.TabIndex = 0;
            this.Label_SectionExport.Text = "Export";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1140, 961);
            this.Controls.Add(this.Panel_MainContent);
            this.Controls.Add(this.Panel_Bottom);
            this.Controls.Add(this.Panel_Top);
            this.Controls.Add(this.Panel_Header);
            this.MinimumSize = new System.Drawing.Size(920, 580);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PJDev Data Tool";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.Panel_Header.ResumeLayout(false);
            this.Panel_Header.PerformLayout();
            this.tableHeader.ResumeLayout(false);
            this.tableHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox_HeaderIcon)).EndInit();
            this.Panel_Top.ResumeLayout(false);
            this.Panel_Top.PerformLayout();
            this.tableTop.ResumeLayout(false);
            this.tableTop.PerformLayout();
            this.Flow_ExportOptions.ResumeLayout(false);
            this.Flow_ExportOptions.PerformLayout();
            this.tableFilterRow.ResumeLayout(false);
            this.tableFilterRow.PerformLayout();
            this.Panel_Bottom.ResumeLayout(false);
            this.Panel_Bottom.PerformLayout();
            this.tableBottom.ResumeLayout(false);
            this.tableBottom.PerformLayout();
            this.Panel_MainContent.ResumeLayout(false);
            this.splitOuter.Panel1.ResumeLayout(false);
            this.splitOuter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitOuter)).EndInit();
            this.splitOuter.ResumeLayout(false);
            this.splitWork.Panel1.ResumeLayout(false);
            this.splitWork.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitWork)).EndInit();
            this.splitWork.ResumeLayout(false);
            this.Panel_ListCard.ResumeLayout(false);
            this.Panel_ListHeader.ResumeLayout(false);
            this.Panel_ListHeader.PerformLayout();
            this.Panel_PreviewCard.ResumeLayout(false);
            this.Panel_LogSection.ResumeLayout(false);
            this.Panel_LogCard.ResumeLayout(false);
            this.Panel_LogHeader.ResumeLayout(false);
            this.Panel_LogHeader.PerformLayout();
            this.Panel_ExportProgress.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Grid_ExportResults)).EndInit();
            this.Panel_ExportProgressTop.ResumeLayout(false);
            this.Panel_ExportProgressTop.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Panel Panel_Header;
        private TableLayoutPanel tableHeader;
        private PictureBox PictureBox_HeaderIcon;
        private Label Label_AppTitle;
        private Label Label_AppSubtitle;
        private CheckBox Chk_DarkMode;
        private Panel Panel_Top;
        private TableLayoutPanel tableTop;
        private TableLayoutPanel tableFilterRow;
        private Panel Panel_Bottom;
        private TableLayoutPanel tableBottom;
        private Panel Panel_MainContent;

        private SplitContainer splitOuter;
        private SplitContainer splitWork;

        private Panel Panel_ListHeader;
        private Panel Panel_ListCard;
        private Panel Panel_PreviewCard;
        private Panel Panel_LogSection;
        private Panel Panel_ExportProgress;
        private Panel Panel_ExportProgressTop;
        private Panel Panel_LogHeader;
        private Panel Panel_LogCard;

        private Label Label_SectionExport;
        private Label Label_ExportStatus;
        private SegmentedExportProgressBar SegmentedExportProgress_Export;
        private BufferedDataGridView Grid_ExportResults;
        private DataGridViewTextBoxColumn ColumnExportTable;
        private DataGridViewTextBoxColumn ColumnExportStatus;
        private DataGridViewTextBoxColumn ColumnExportMessage;

        private Label Label_SectionList;
        private Label Label_SectionPreview;
        private Label Label_SectionLog;

        private Button Btn_DataSetting;
        private Button Btn_SelectProjectRoot;
        private Button Btn_SelectExcelFolder;
        private Button Btn_OpenOutputFolder;
        private Button Btn_OpenXlsxFolder;
        private Button Btn_OpenCsvFolder;
        private Button Btn_NewCsv;
        private Button Btn_RefreshList;
        private Button Btn_ClearLog;

        private Label Label_ProjectRoot;
        private Label Label_ExcelSourcePath;
        private Label Label_ExportVersion;
        private FlowLayoutPanel Flow_ExportOptions;
        private TextBox Txt_ExportVersion;
        private CheckBox Chk_RemoveOrphanArtifacts;
        private TextBox TextBox_NewCsvName;
        private TextBox Txt_CsvFilter;

        private ComboBox Combo_LogFilter;

        private CheckedListBox ListBox_CsvFiles;
        private RichTextBox TextBox_Preview;
        private RichTextBox TextBox_Log;

        private Label Label_CsvFilter;
    }
}
