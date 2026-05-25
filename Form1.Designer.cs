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
            this.Panel_Top = new System.Windows.Forms.Panel();
            this.tableTop = new System.Windows.Forms.TableLayoutPanel();
            this.Btn_SelectProjectRoot = new System.Windows.Forms.Button();
            this.Label_ProjectRoot = new System.Windows.Forms.Label();
            this.Btn_SelectExcelFolder = new System.Windows.Forms.Button();
            this.Label_ExcelSourcePath = new System.Windows.Forms.Label();
            this.tableFilterRow = new System.Windows.Forms.TableLayoutPanel();
            this.Label_CsvFilter = new System.Windows.Forms.Label();
            this.Txt_CsvFilter = new System.Windows.Forms.TextBox();
            this.Panel_RightToolbar = new System.Windows.Forms.Panel();
            this.Btn_NewCsv = new System.Windows.Forms.Button();
            this.TextBox_NewCsvName = new System.Windows.Forms.TextBox();
            this.Panel_Bottom = new System.Windows.Forms.Panel();
            this.tableBottom = new System.Windows.Forms.TableLayoutPanel();
            this.Combo_LogFilter = new System.Windows.Forms.ComboBox();
            this.Btn_OpenOutputFolder = new System.Windows.Forms.Button();
            this.Btn_OpenCsvFolder = new System.Windows.Forms.Button();
            this.Btn_OpenXlsxFolder = new System.Windows.Forms.Button();
            this.Btn_DataSetting = new System.Windows.Forms.Button();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.ListBox_CsvFiles = new System.Windows.Forms.ListBox();
            this.splitRight = new System.Windows.Forms.SplitContainer();
            this.TextBox_Preview = new System.Windows.Forms.RichTextBox();
            this.TextBox_Log = new System.Windows.Forms.RichTextBox();
            this.Panel_Top.SuspendLayout();
            this.tableTop.SuspendLayout();
            this.tableFilterRow.SuspendLayout();
            this.Panel_RightToolbar.SuspendLayout();
            this.Panel_Bottom.SuspendLayout();
            this.tableBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).BeginInit();
            this.splitRight.Panel1.SuspendLayout();
            this.splitRight.Panel2.SuspendLayout();
            this.splitRight.SuspendLayout();
            this.SuspendLayout();
            // 
            // Panel_Top
            // 
            this.Panel_Top.AutoSize = true;
            this.Panel_Top.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Panel_Top.BackColor = System.Drawing.SystemColors.Control;
            this.Panel_Top.Controls.Add(this.tableTop);
            this.Panel_Top.Controls.Add(this.Panel_RightToolbar);
            this.Panel_Top.Dock = System.Windows.Forms.DockStyle.Top;
            this.Panel_Top.Location = new System.Drawing.Point(0, 0);
            this.Panel_Top.MinimumSize = new System.Drawing.Size(0, 120);
            this.Panel_Top.Name = "Panel_Top";
            this.Panel_Top.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.Panel_Top.Size = new System.Drawing.Size(1000, 120);
            this.Panel_Top.TabIndex = 2;
            // 
            // tableTop
            // 
            this.tableTop.AutoSize = true;
            this.tableTop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableTop.ColumnCount = 2;
            this.tableTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tableTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableTop.Controls.Add(this.Btn_SelectProjectRoot, 0, 0);
            this.tableTop.Controls.Add(this.Label_ProjectRoot, 1, 0);
            this.tableTop.Controls.Add(this.Btn_SelectExcelFolder, 0, 1);
            this.tableTop.Controls.Add(this.Label_ExcelSourcePath, 1, 1);
            this.tableTop.Controls.Add(this.tableFilterRow, 0, 2);
            this.tableTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableTop.Location = new System.Drawing.Point(0, 4);
            this.tableTop.Name = "tableTop";
            this.tableTop.Padding = new System.Windows.Forms.Padding(8, 2, 4, 2);
            this.tableTop.RowCount = 3;
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableTop.Size = new System.Drawing.Size(862, 112);
            this.tableTop.TabIndex = 0;
            // 
            // Btn_SelectProjectRoot
            // 
            this.Btn_SelectProjectRoot.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Btn_SelectProjectRoot.AutoSize = true;
            this.Btn_SelectProjectRoot.Location = new System.Drawing.Point(11, 5);
            this.Btn_SelectProjectRoot.Name = "Btn_SelectProjectRoot";
            this.Btn_SelectProjectRoot.Size = new System.Drawing.Size(119, 22);
            this.Btn_SelectProjectRoot.TabIndex = 0;
            this.Btn_SelectProjectRoot.Text = "프로젝트 경로 지정";
            this.Btn_SelectProjectRoot.Click += new System.EventHandler(this.Btn_SelectProjectRoot_Click);
            // 
            // Label_ProjectRoot
            // 
            this.Label_ProjectRoot.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_ProjectRoot.AutoEllipsis = true;
            this.Label_ProjectRoot.Location = new System.Drawing.Point(138, 2);
            this.Label_ProjectRoot.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.Label_ProjectRoot.Name = "Label_ProjectRoot";
            this.Label_ProjectRoot.Size = new System.Drawing.Size(716, 28);
            this.Label_ProjectRoot.TabIndex = 1;
            this.Label_ProjectRoot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Btn_SelectExcelFolder
            // 
            this.Btn_SelectExcelFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Btn_SelectExcelFolder.AutoSize = true;
            this.Btn_SelectExcelFolder.Location = new System.Drawing.Point(11, 33);
            this.Btn_SelectExcelFolder.Name = "Btn_SelectExcelFolder";
            this.Btn_SelectExcelFolder.Size = new System.Drawing.Size(102, 22);
            this.Btn_SelectExcelFolder.TabIndex = 10;
            this.Btn_SelectExcelFolder.Text = "XLSX 원본 지정";
            this.Btn_SelectExcelFolder.Click += new System.EventHandler(this.Btn_SelectExcelFolder_Click);
            // 
            // Label_ExcelSourcePath
            // 
            this.Label_ExcelSourcePath.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_ExcelSourcePath.AutoEllipsis = true;
            this.Label_ExcelSourcePath.Location = new System.Drawing.Point(138, 30);
            this.Label_ExcelSourcePath.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.Label_ExcelSourcePath.Name = "Label_ExcelSourcePath";
            this.Label_ExcelSourcePath.Size = new System.Drawing.Size(716, 28);
            this.Label_ExcelSourcePath.TabIndex = 11;
            this.Label_ExcelSourcePath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableFilterRow
            // 
            this.tableFilterRow.ColumnCount = 2;
            this.tableTop.SetColumnSpan(this.tableFilterRow, 2);
            this.tableFilterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableFilterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableFilterRow.Controls.Add(this.Label_CsvFilter, 0, 0);
            this.tableFilterRow.Controls.Add(this.Txt_CsvFilter, 1, 0);
            this.tableFilterRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableFilterRow.Location = new System.Drawing.Point(8, 58);
            this.tableFilterRow.Margin = new System.Windows.Forms.Padding(0);
            this.tableFilterRow.Name = "tableFilterRow";
            this.tableFilterRow.RowCount = 1;
            this.tableFilterRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableFilterRow.Size = new System.Drawing.Size(850, 52);
            this.tableFilterRow.TabIndex = 20;
            // 
            // Label_CsvFilter
            // 
            this.Label_CsvFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Label_CsvFilter.AutoSize = true;
            this.Label_CsvFilter.Location = new System.Drawing.Point(3, 20);
            this.Label_CsvFilter.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
            this.Label_CsvFilter.Name = "Label_CsvFilter";
            this.Label_CsvFilter.Size = new System.Drawing.Size(57, 12);
            this.Label_CsvFilter.TabIndex = 0;
            this.Label_CsvFilter.Text = "목록 필터";
            // 
            // Txt_CsvFilter
            // 
            this.Txt_CsvFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_CsvFilter.Location = new System.Drawing.Point(68, 15);
            this.Txt_CsvFilter.Margin = new System.Windows.Forms.Padding(0);
            this.Txt_CsvFilter.Name = "Txt_CsvFilter";
            this.Txt_CsvFilter.Size = new System.Drawing.Size(782, 21);
            this.Txt_CsvFilter.TabIndex = 1;
            this.Txt_CsvFilter.TextChanged += new System.EventHandler(this.Txt_CsvFilter_TextChanged);
            // 
            // Panel_RightToolbar
            // 
            this.Panel_RightToolbar.Controls.Add(this.Btn_NewCsv);
            this.Panel_RightToolbar.Controls.Add(this.TextBox_NewCsvName);
            this.Panel_RightToolbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.Panel_RightToolbar.Location = new System.Drawing.Point(862, 4);
            this.Panel_RightToolbar.MinimumSize = new System.Drawing.Size(138, 0);
            this.Panel_RightToolbar.Name = "Panel_RightToolbar";
            this.Panel_RightToolbar.Padding = new System.Windows.Forms.Padding(6, 4, 10, 4);
            this.Panel_RightToolbar.Size = new System.Drawing.Size(138, 112);
            this.Panel_RightToolbar.TabIndex = 1;
            // 
            // Btn_NewCsv
            // 
            this.Btn_NewCsv.Dock = System.Windows.Forms.DockStyle.Top;
            this.Btn_NewCsv.Location = new System.Drawing.Point(6, 25);
            this.Btn_NewCsv.Name = "Btn_NewCsv";
            this.Btn_NewCsv.Size = new System.Drawing.Size(122, 25);
            this.Btn_NewCsv.TabIndex = 8;
            this.Btn_NewCsv.Text = "새 XLSX 만들기";
            this.Btn_NewCsv.Click += new System.EventHandler(this.Btn_NewCsv_Click);
            // 
            // TextBox_NewCsvName
            // 
            this.TextBox_NewCsvName.Dock = System.Windows.Forms.DockStyle.Top;
            this.TextBox_NewCsvName.Location = new System.Drawing.Point(6, 4);
            this.TextBox_NewCsvName.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.TextBox_NewCsvName.Name = "TextBox_NewCsvName";
            this.TextBox_NewCsvName.Size = new System.Drawing.Size(122, 21);
            this.TextBox_NewCsvName.TabIndex = 9;
            // 
            // Panel_Bottom
            // 
            this.Panel_Bottom.BackColor = System.Drawing.SystemColors.Control;
            this.Panel_Bottom.Controls.Add(this.tableBottom);
            this.Panel_Bottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.Panel_Bottom.Location = new System.Drawing.Point(0, 596);
            this.Panel_Bottom.MinimumSize = new System.Drawing.Size(0, 54);
            this.Panel_Bottom.Name = "Panel_Bottom";
            this.Panel_Bottom.Padding = new System.Windows.Forms.Padding(8, 8, 10, 8);
            this.Panel_Bottom.Size = new System.Drawing.Size(1000, 54);
            this.Panel_Bottom.TabIndex = 1;
            // 
            // tableBottom
            // 
            this.tableBottom.ColumnCount = 6;
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableBottom.Controls.Add(this.Combo_LogFilter, 0, 0);
            this.tableBottom.Controls.Add(this.Btn_OpenOutputFolder, 2, 0);
            this.tableBottom.Controls.Add(this.Btn_OpenCsvFolder, 3, 0);
            this.tableBottom.Controls.Add(this.Btn_OpenXlsxFolder, 4, 0);
            this.tableBottom.Controls.Add(this.Btn_DataSetting, 5, 0);
            this.tableBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableBottom.Location = new System.Drawing.Point(8, 8);
            this.tableBottom.Name = "tableBottom";
            this.tableBottom.RowCount = 1;
            this.tableBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableBottom.Size = new System.Drawing.Size(982, 38);
            this.tableBottom.TabIndex = 0;
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
            this.Combo_LogFilter.Location = new System.Drawing.Point(0, 9);
            this.Combo_LogFilter.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.Combo_LogFilter.Name = "Combo_LogFilter";
            this.Combo_LogFilter.Size = new System.Drawing.Size(121, 20);
            this.Combo_LogFilter.TabIndex = 2;
            this.Combo_LogFilter.SelectedIndexChanged += new System.EventHandler(this.Combo_LogFilter_SelectedIndexChanged);
            // 
            // Btn_OpenOutputFolder
            // 
            this.Btn_OpenOutputFolder.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Btn_OpenOutputFolder.AutoSize = true;
            this.Btn_OpenOutputFolder.Location = new System.Drawing.Point(489, 7);
            this.Btn_OpenOutputFolder.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.Btn_OpenOutputFolder.Name = "Btn_OpenOutputFolder";
            this.Btn_OpenOutputFolder.Size = new System.Drawing.Size(119, 23);
            this.Btn_OpenOutputFolder.TabIndex = 1;
            this.Btn_OpenOutputFolder.Text = "프로젝트 폴더 열기";
            this.Btn_OpenOutputFolder.Click += new System.EventHandler(this.Btn_OpenOutputFolder_Click);
            // 
            // Btn_OpenCsvFolder
            // 
            this.Btn_OpenCsvFolder.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Btn_OpenCsvFolder.AutoSize = true;
            this.Btn_OpenCsvFolder.Location = new System.Drawing.Point(614, 7);
            this.Btn_OpenCsvFolder.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.Btn_OpenCsvFolder.Name = "Btn_OpenCsvFolder";
            this.Btn_OpenCsvFolder.Size = new System.Drawing.Size(103, 23);
            this.Btn_OpenCsvFolder.TabIndex = 4;
            this.Btn_OpenCsvFolder.Text = "출력 폴더 열기";
            this.Btn_OpenCsvFolder.Click += new System.EventHandler(this.Btn_OpenCsvFolder_Click);
            // 
            // Btn_OpenXlsxFolder
            // 
            this.Btn_OpenXlsxFolder.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Btn_OpenXlsxFolder.AutoSize = true;
            this.Btn_OpenXlsxFolder.Location = new System.Drawing.Point(723, 7);
            this.Btn_OpenXlsxFolder.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.Btn_OpenXlsxFolder.Name = "Btn_OpenXlsxFolder";
            this.Btn_OpenXlsxFolder.Size = new System.Drawing.Size(103, 23);
            this.Btn_OpenXlsxFolder.TabIndex = 3;
            this.Btn_OpenXlsxFolder.Text = "XLSX 폴더 열기";
            this.Btn_OpenXlsxFolder.Click += new System.EventHandler(this.Btn_OpenXlsxFolder_Click);
            // 
            // Btn_DataSetting
            // 
            this.Btn_DataSetting.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Btn_DataSetting.AutoSize = true;
            this.Btn_DataSetting.Location = new System.Drawing.Point(832, 4);
            this.Btn_DataSetting.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.Btn_DataSetting.Name = "Btn_DataSetting";
            this.Btn_DataSetting.Size = new System.Drawing.Size(150, 30);
            this.Btn_DataSetting.TabIndex = 0;
            this.Btn_DataSetting.Text = "데이터 설정";
            this.Btn_DataSetting.Click += new System.EventHandler(this.Btn_DataSetting_Click);
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitMain.Location = new System.Drawing.Point(0, 120);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.ListBox_CsvFiles);
            this.splitMain.Panel1MinSize = 160;
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.splitRight);
            this.splitMain.Panel2MinSize = 200;
            this.splitMain.Size = new System.Drawing.Size(1000, 476);
            this.splitMain.SplitterDistance = 730;
            this.splitMain.SplitterWidth = 6;
            this.splitMain.TabIndex = 0;
            // 
            // ListBox_CsvFiles
            // 
            this.ListBox_CsvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListBox_CsvFiles.IntegralHeight = false;
            this.ListBox_CsvFiles.ItemHeight = 12;
            this.ListBox_CsvFiles.Location = new System.Drawing.Point(0, 0);
            this.ListBox_CsvFiles.Name = "ListBox_CsvFiles";
            this.ListBox_CsvFiles.Size = new System.Drawing.Size(730, 476);
            this.ListBox_CsvFiles.TabIndex = 0;
            this.ListBox_CsvFiles.SelectedIndexChanged += new System.EventHandler(this.ListBox_CsvFiles_SelectedIndexChanged);
            this.ListBox_CsvFiles.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ListBox_CsvFiles_MouseDoubleClick);
            // 
            // splitRight
            // 
            this.splitRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRight.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitRight.Location = new System.Drawing.Point(0, 0);
            this.splitRight.Name = "splitRight";
            this.splitRight.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitRight.Panel1
            // 
            this.splitRight.Panel1.Controls.Add(this.TextBox_Preview);
            this.splitRight.Panel1MinSize = 120;
            // 
            // splitRight.Panel2
            // 
            this.splitRight.Panel2.Controls.Add(this.TextBox_Log);
            this.splitRight.Panel2MinSize = 72;
            this.splitRight.Size = new System.Drawing.Size(264, 476);
            this.splitRight.SplitterDistance = 330;
            this.splitRight.SplitterWidth = 6;
            this.splitRight.TabIndex = 0;
            // 
            // TextBox_Preview
            // 
            this.TextBox_Preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextBox_Preview.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextBox_Preview.Location = new System.Drawing.Point(0, 0);
            this.TextBox_Preview.Name = "TextBox_Preview";
            this.TextBox_Preview.ReadOnly = true;
            this.TextBox_Preview.Size = new System.Drawing.Size(264, 330);
            this.TextBox_Preview.TabIndex = 0;
            this.TextBox_Preview.Text = "";
            // 
            // TextBox_Log
            // 
            this.TextBox_Log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextBox_Log.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.TextBox_Log.Location = new System.Drawing.Point(0, 0);
            this.TextBox_Log.Name = "TextBox_Log";
            this.TextBox_Log.ReadOnly = true;
            this.TextBox_Log.Size = new System.Drawing.Size(264, 140);
            this.TextBox_Log.TabIndex = 0;
            this.TextBox_Log.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1000, 650);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.Panel_Bottom);
            this.Controls.Add(this.Panel_Top);
            this.MinimumSize = new System.Drawing.Size(720, 480);
            this.Name = "Form1";
            this.Text = "게임 데이터 툴";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.Panel_Top.ResumeLayout(false);
            this.Panel_Top.PerformLayout();
            this.tableTop.ResumeLayout(false);
            this.tableTop.PerformLayout();
            this.tableFilterRow.ResumeLayout(false);
            this.tableFilterRow.PerformLayout();
            this.Panel_RightToolbar.ResumeLayout(false);
            this.Panel_RightToolbar.PerformLayout();
            this.Panel_Bottom.ResumeLayout(false);
            this.tableBottom.ResumeLayout(false);
            this.tableBottom.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.splitRight.Panel1.ResumeLayout(false);
            this.splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).EndInit();
            this.splitRight.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Panel Panel_Top;
        private Panel Panel_RightToolbar;
        private TableLayoutPanel tableTop;
        private TableLayoutPanel tableFilterRow;
        private Panel Panel_Bottom;
        private TableLayoutPanel tableBottom;

        private SplitContainer splitMain;
        private SplitContainer splitRight;

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
