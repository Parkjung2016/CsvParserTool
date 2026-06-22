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
            this.Label_SectionList = new System.Windows.Forms.Label();
            this.Panel_ListCard = new System.Windows.Forms.Panel();
            this.ListBox_CsvFiles = new System.Windows.Forms.ListBox();
            this.Label_SectionPreview = new System.Windows.Forms.Label();
            this.Panel_PreviewCard = new System.Windows.Forms.Panel();
            this.TextBox_Preview = new System.Windows.Forms.RichTextBox();
            this.Panel_LogSection = new System.Windows.Forms.Panel();
            this.Panel_LogHeader = new System.Windows.Forms.Panel();
            this.Combo_LogFilter = new System.Windows.Forms.ComboBox();
            this.Label_SectionLog = new System.Windows.Forms.Label();
            this.Panel_LogCard = new System.Windows.Forms.Panel();
            this.TextBox_Log = new System.Windows.Forms.RichTextBox();
            this.Panel_Header.SuspendLayout();
            this.tableHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox_HeaderIcon)).BeginInit();
            this.Panel_Top.SuspendLayout();
            this.tableTop.SuspendLayout();
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
            this.Panel_PreviewCard.SuspendLayout();
            this.Panel_LogSection.SuspendLayout();
            this.Panel_LogHeader.SuspendLayout();
            this.Panel_LogCard.SuspendLayout();
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
            this.Panel_Header.Size = new System.Drawing.Size(1140, 72);
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
            this.tableHeader.Size = new System.Drawing.Size(1140, 72);
            this.tableHeader.TabIndex = 0;
            // 
            // PictureBox_HeaderIcon
            // 
            this.PictureBox_HeaderIcon.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.PictureBox_HeaderIcon.Location = new System.Drawing.Point(0, 8);
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
            this.Label_AppTitle.Location = new System.Drawing.Point(0, 0);
            this.Label_AppTitle.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.Label_AppTitle.Name = "Label_AppTitle";
            this.Label_AppTitle.Size = new System.Drawing.Size(52, 22);
            this.Label_AppTitle.TabIndex = 0;
            this.Label_AppTitle.Text = "PJDev";
            // 
            // Label_AppSubtitle
            // 
            this.Label_AppSubtitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_AppSubtitle.AutoEllipsis = true;
            this.Label_AppSubtitle.Location = new System.Drawing.Point(0, 26);
            this.Label_AppSubtitle.Margin = new System.Windows.Forms.Padding(0, 4, 12, 0);
            this.Label_AppSubtitle.Name = "Label_AppSubtitle";
            this.Label_AppSubtitle.Size = new System.Drawing.Size(734, 20);
            this.Label_AppSubtitle.TabIndex = 1;
            this.Label_AppSubtitle.Text = "Data Tool · Unity XLSX → CSV · Export";
            this.Label_AppSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Chk_DarkMode
            // 
            this.Chk_DarkMode.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Chk_DarkMode.AutoSize = true;
            this.Chk_DarkMode.Location = new System.Drawing.Point(746, 8);
            this.Chk_DarkMode.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.Chk_DarkMode.Name = "Chk_DarkMode";
            this.tableHeader.SetRowSpan(this.Chk_DarkMode, 2);
            this.Chk_DarkMode.Size = new System.Drawing.Size(78, 19);
            this.Chk_DarkMode.TabIndex = 3;
            this.Chk_DarkMode.Text = "다크 모드";
            this.Chk_DarkMode.UseVisualStyleBackColor = true;
            this.Chk_DarkMode.CheckedChanged += new System.EventHandler(this.Chk_DarkMode_CheckedChanged);
            // 
            // Btn_DataSetting
            // 
            this.Btn_DataSetting.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Btn_DataSetting.AutoSize = true;
            this.Btn_DataSetting.Location = new System.Drawing.Point(836, 0);
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
            this.Panel_Top.Location = new System.Drawing.Point(0, 72);
            this.Panel_Top.MinimumSize = new System.Drawing.Size(0, 128);
            this.Panel_Top.Name = "Panel_Top";
            this.Panel_Top.Size = new System.Drawing.Size(1140, 128);
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
            this.tableTop.Controls.Add(this.tableFilterRow, 0, 2);
            this.tableTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableTop.Location = new System.Drawing.Point(12, 10);
            this.tableTop.Name = "tableTop";
            this.tableTop.RowCount = 3;
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableTop.Size = new System.Drawing.Size(1116, 116);
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
            this.Label_ProjectRoot.Size = new System.Drawing.Size(981, 34);
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
            this.Label_ExcelSourcePath.Size = new System.Drawing.Size(981, 34);
            this.Label_ExcelSourcePath.TabIndex = 11;
            this.Label_ExcelSourcePath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            this.tableFilterRow.Location = new System.Drawing.Point(0, 80);
            this.tableFilterRow.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.tableFilterRow.Name = "tableFilterRow";
            this.tableFilterRow.RowCount = 1;
            this.tableFilterRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableFilterRow.Size = new System.Drawing.Size(1116, 36);
            this.tableFilterRow.TabIndex = 20;
            // 
            // Label_CsvFilter
            // 
            this.Label_CsvFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Label_CsvFilter.AutoSize = true;
            this.Label_CsvFilter.Location = new System.Drawing.Point(0, 10);
            this.Label_CsvFilter.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.Label_CsvFilter.Name = "Label_CsvFilter";
            this.Label_CsvFilter.Size = new System.Drawing.Size(57, 15);
            this.Label_CsvFilter.TabIndex = 0;
            this.Label_CsvFilter.Text = "목록 필터";
            // 
            // Txt_CsvFilter
            // 
            this.Txt_CsvFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_CsvFilter.Location = new System.Drawing.Point(69, 7);
            this.Txt_CsvFilter.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.Txt_CsvFilter.Name = "Txt_CsvFilter";
            this.Txt_CsvFilter.Size = new System.Drawing.Size(804, 23);
            this.Txt_CsvFilter.TabIndex = 1;
            this.Txt_CsvFilter.TextChanged += new System.EventHandler(this.Txt_CsvFilter_TextChanged);
            // 
            // Btn_NewCsv
            // 
            this.Btn_NewCsv.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Btn_NewCsv.AutoSize = true;
            this.Btn_NewCsv.Location = new System.Drawing.Point(881, 4);
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
            this.TextBox_NewCsvName.Location = new System.Drawing.Point(968, 7);
            this.TextBox_NewCsvName.Margin = new System.Windows.Forms.Padding(0);
            this.TextBox_NewCsvName.Name = "TextBox_NewCsvName";
            this.TextBox_NewCsvName.Size = new System.Drawing.Size(148, 23);
            this.TextBox_NewCsvName.TabIndex = 9;
            // 
            // Panel_Bottom
            // 
            this.Panel_Bottom.AutoSize = true;
            this.Panel_Bottom.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Panel_Bottom.Controls.Add(this.tableBottom);
            this.Panel_Bottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.Panel_Bottom.Location = new System.Drawing.Point(0, 652);
            this.Panel_Bottom.Name = "Panel_Bottom";
            this.Panel_Bottom.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            this.Panel_Bottom.Size = new System.Drawing.Size(1140, 52);
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
            this.tableBottom.Location = new System.Drawing.Point(16, 10);
            this.tableBottom.Name = "tableBottom";
            this.tableBottom.RowCount = 1;
            this.tableBottom.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableBottom.Size = new System.Drawing.Size(1108, 30);
            this.tableBottom.TabIndex = 0;
            // 
            // Btn_OpenOutputFolder
            // 
            this.Btn_OpenOutputFolder.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Btn_OpenOutputFolder.AutoSize = true;
            this.Btn_OpenOutputFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Btn_OpenOutputFolder.Location = new System.Drawing.Point(774, 0);
            this.Btn_OpenOutputFolder.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.Btn_OpenOutputFolder.Name = "Btn_OpenOutputFolder";
            this.Btn_OpenOutputFolder.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
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
            this.Btn_OpenCsvFolder.Location = new System.Drawing.Point(899, 0);
            this.Btn_OpenCsvFolder.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.Btn_OpenCsvFolder.Name = "Btn_OpenCsvFolder";
            this.Btn_OpenCsvFolder.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
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
            this.Btn_OpenXlsxFolder.Location = new System.Drawing.Point(1008, 0);
            this.Btn_OpenXlsxFolder.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.Btn_OpenXlsxFolder.Name = "Btn_OpenXlsxFolder";
            this.Btn_OpenXlsxFolder.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
            this.Btn_OpenXlsxFolder.TabIndex = 3;
            this.Btn_OpenXlsxFolder.Text = "XLSX 폴더";
            this.Btn_OpenXlsxFolder.UseCompatibleTextRendering = true;
            this.Btn_OpenXlsxFolder.Click += new System.EventHandler(this.Btn_OpenXlsxFolder_Click);
            // 
            // Panel_MainContent
            // 
            this.Panel_MainContent.Controls.Add(this.splitOuter);
            this.Panel_MainContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_MainContent.Location = new System.Drawing.Point(0, 200);
            this.Panel_MainContent.Name = "Panel_MainContent";
            this.Panel_MainContent.Padding = new System.Windows.Forms.Padding(12, 4, 12, 4);
            this.Panel_MainContent.Size = new System.Drawing.Size(1140, 452);
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
            this.splitOuter.Panel2MinSize = 120;
            this.splitOuter.Size = new System.Drawing.Size(1116, 444);
            this.splitOuter.SplitterDistance = 280;
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
            this.splitWork.Panel1.Controls.Add(this.Label_SectionList);
            this.splitWork.Panel1.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.splitWork.Panel1MinSize = 200;
            // 
            // splitWork.Panel2
            // 
            this.splitWork.Panel2.Controls.Add(this.Panel_PreviewCard);
            this.splitWork.Panel2.Controls.Add(this.Label_SectionPreview);
            this.splitWork.Panel2.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.splitWork.Panel2MinSize = 280;
            this.splitWork.Size = new System.Drawing.Size(1116, 280);
            this.splitWork.SplitterDistance = 288;
            this.splitWork.SplitterWidth = 6;
            this.splitWork.TabIndex = 0;
            // 
            // Label_SectionList
            // 
            this.Label_SectionList.Dock = System.Windows.Forms.DockStyle.Top;
            this.Label_SectionList.Location = new System.Drawing.Point(0, 0);
            this.Label_SectionList.Name = "Label_SectionList";
            this.Label_SectionList.Padding = new System.Windows.Forms.Padding(2, 0, 0, 8);
            this.Label_SectionList.Size = new System.Drawing.Size(274, 28);
            this.Label_SectionList.TabIndex = 1;
            this.Label_SectionList.Text = "테이블 목록";
            // 
            // Panel_ListCard
            // 
            this.Panel_ListCard.Controls.Add(this.ListBox_CsvFiles);
            this.Panel_ListCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_ListCard.Location = new System.Drawing.Point(0, 28);
            this.Panel_ListCard.Name = "Panel_ListCard";
            this.Panel_ListCard.Size = new System.Drawing.Size(274, 252);
            this.Panel_ListCard.TabIndex = 0;
            // 
            // ListBox_CsvFiles
            // 
            this.ListBox_CsvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListBox_CsvFiles.IntegralHeight = false;
            this.ListBox_CsvFiles.ItemHeight = 26;
            this.ListBox_CsvFiles.Location = new System.Drawing.Point(0, 0);
            this.ListBox_CsvFiles.Name = "ListBox_CsvFiles";
            this.ListBox_CsvFiles.Size = new System.Drawing.Size(274, 252);
            this.ListBox_CsvFiles.TabIndex = 0;
            this.ListBox_CsvFiles.SelectedIndexChanged += new System.EventHandler(this.ListBox_CsvFiles_SelectedIndexChanged);
            this.ListBox_CsvFiles.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ListBox_CsvFiles_MouseDoubleClick);
            // 
            // Label_SectionPreview
            // 
            this.Label_SectionPreview.Dock = System.Windows.Forms.DockStyle.Top;
            this.Label_SectionPreview.Location = new System.Drawing.Point(8, 0);
            this.Label_SectionPreview.Name = "Label_SectionPreview";
            this.Label_SectionPreview.Padding = new System.Windows.Forms.Padding(2, 0, 0, 8);
            this.Label_SectionPreview.Size = new System.Drawing.Size(810, 28);
            this.Label_SectionPreview.TabIndex = 1;
            this.Label_SectionPreview.Text = "코드 미리보기";
            // 
            // Panel_PreviewCard
            // 
            this.Panel_PreviewCard.Controls.Add(this.TextBox_Preview);
            this.Panel_PreviewCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_PreviewCard.Location = new System.Drawing.Point(8, 28);
            this.Panel_PreviewCard.Name = "Panel_PreviewCard";
            this.Panel_PreviewCard.Size = new System.Drawing.Size(810, 252);
            this.Panel_PreviewCard.TabIndex = 0;
            // 
            // TextBox_Preview
            // 
            this.TextBox_Preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextBox_Preview.Location = new System.Drawing.Point(0, 0);
            this.TextBox_Preview.Name = "TextBox_Preview";
            this.TextBox_Preview.ReadOnly = true;
            this.TextBox_Preview.Size = new System.Drawing.Size(810, 252);
            this.TextBox_Preview.TabIndex = 0;
            this.TextBox_Preview.Text = "";
            // 
            // Panel_LogSection
            // 
            this.Panel_LogSection.Controls.Add(this.Panel_LogCard);
            this.Panel_LogSection.Controls.Add(this.Panel_LogHeader);
            this.Panel_LogSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_LogSection.Location = new System.Drawing.Point(0, 0);
            this.Panel_LogSection.Name = "Panel_LogSection";
            this.Panel_LogSection.Size = new System.Drawing.Size(1116, 158);
            this.Panel_LogSection.TabIndex = 0;
            // 
            // Panel_LogHeader
            // 
            this.Panel_LogHeader.Controls.Add(this.Combo_LogFilter);
            this.Panel_LogHeader.Controls.Add(this.Label_SectionLog);
            this.Panel_LogHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.Panel_LogHeader.Location = new System.Drawing.Point(0, 0);
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
            this.Combo_LogFilter.Size = new System.Drawing.Size(120, 23);
            this.Combo_LogFilter.TabIndex = 2;
            this.Combo_LogFilter.SelectedIndexChanged += new System.EventHandler(this.Combo_LogFilter_SelectedIndexChanged);
            // 
            // Label_SectionLog
            // 
            this.Label_SectionLog.AutoSize = true;
            this.Label_SectionLog.Location = new System.Drawing.Point(2, 8);
            this.Label_SectionLog.Name = "Label_SectionLog";
            this.Label_SectionLog.Size = new System.Drawing.Size(27, 15);
            this.Label_SectionLog.TabIndex = 0;
            this.Label_SectionLog.Text = "로그";
            // 
            // Panel_LogCard
            // 
            this.Panel_LogCard.Controls.Add(this.TextBox_Log);
            this.Panel_LogCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel_LogCard.Location = new System.Drawing.Point(0, 32);
            this.Panel_LogCard.Name = "Panel_LogCard";
            this.Panel_LogCard.Size = new System.Drawing.Size(1116, 126);
            this.Panel_LogCard.TabIndex = 0;
            // 
            // TextBox_Log
            // 
            this.TextBox_Log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextBox_Log.Location = new System.Drawing.Point(0, 0);
            this.TextBox_Log.Name = "TextBox_Log";
            this.TextBox_Log.ReadOnly = true;
            this.TextBox_Log.Size = new System.Drawing.Size(1116, 126);
            this.TextBox_Log.TabIndex = 0;
            this.TextBox_Log.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1140, 704);
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
            this.Panel_PreviewCard.ResumeLayout(false);
            this.Panel_LogSection.ResumeLayout(false);
            this.Panel_LogHeader.ResumeLayout(false);
            this.Panel_LogHeader.PerformLayout();
            this.Panel_LogCard.ResumeLayout(false);
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

        private Panel Panel_ListCard;
        private Panel Panel_PreviewCard;
        private Panel Panel_LogSection;
        private Panel Panel_LogHeader;
        private Panel Panel_LogCard;

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

        private Label Label_ProjectRoot;
        private Label Label_ExcelSourcePath;
        private TextBox TextBox_NewCsvName;
        private TextBox Txt_CsvFilter;

        private ComboBox Combo_LogFilter;

        private ListBox ListBox_CsvFiles;
        private RichTextBox TextBox_Preview;
        private RichTextBox TextBox_Log;

        private Label Label_CsvFilter;
    }
}
