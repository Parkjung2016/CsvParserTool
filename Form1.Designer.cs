using System.Drawing;
using System.Windows.Forms;

namespace CSVParserTool
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.Btn_DataSetting = new System.Windows.Forms.Button();
            this.Btn_SelectOutputFolder = new System.Windows.Forms.Button();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.Label_OutputPath = new System.Windows.Forms.Label();
            this.ListBox_CsvFiles = new System.Windows.Forms.ListBox();
            this.TextBox_SelectedCsv = new System.Windows.Forms.Label();
            this.TextBox_Preview = new System.Windows.Forms.RichTextBox();
            this.TextBox_Log = new System.Windows.Forms.RichTextBox();
            this.Label_OpenFile = new System.Windows.Forms.Label();
            this.Btn_OpenOutputFolder = new System.Windows.Forms.Button();
            this.Txt_CsvFilter = new System.Windows.Forms.TextBox();
            this.Combo_RecentCsv = new System.Windows.Forms.ComboBox();
            this.Label_CsvFilter = new System.Windows.Forms.Label();
            this.Panel_RecentCsv = new System.Windows.Forms.Panel();
            this.Label_RecentCsv = new System.Windows.Forms.Label();
            this.Panel_CsvFilter = new System.Windows.Forms.Panel();
            this.Combo_LogFilter = new System.Windows.Forms.ComboBox();
            this.Btn_NewCsv = new System.Windows.Forms.Button();
            this.TextBox_NewCsvName = new System.Windows.Forms.TextBox();
            this.Panel_RecentCsv.SuspendLayout();
            this.Panel_CsvFilter.SuspendLayout();
            this.SuspendLayout();
            // 
            // Btn_DataSetting
            // 
            this.Btn_DataSetting.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_DataSetting.BackColor = System.Drawing.Color.Aquamarine;
            this.Btn_DataSetting.Font = new System.Drawing.Font("Gulim", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.Btn_DataSetting.Location = new System.Drawing.Point(730, 520);
            this.Btn_DataSetting.Name = "Btn_DataSetting";
            this.Btn_DataSetting.Size = new System.Drawing.Size(150, 40);
            this.Btn_DataSetting.TabIndex = 0;
            this.Btn_DataSetting.Text = "데이터 설정";
            this.Btn_DataSetting.UseVisualStyleBackColor = false;
            this.Btn_DataSetting.Click += new System.EventHandler(this.Btn_DataSetting_Click);
            // 
            // Btn_SelectOutputFolder
            // 
            this.Btn_SelectOutputFolder.BackColor = System.Drawing.Color.Coral;
            this.Btn_SelectOutputFolder.Location = new System.Drawing.Point(10, 13);
            this.Btn_SelectOutputFolder.Name = "Btn_SelectOutputFolder";
            this.Btn_SelectOutputFolder.Size = new System.Drawing.Size(150, 30);
            this.Btn_SelectOutputFolder.TabIndex = 2;
            this.Btn_SelectOutputFolder.Text = "출력 폴더 경로 설정";
            this.Btn_SelectOutputFolder.UseVisualStyleBackColor = false;
            this.Btn_SelectOutputFolder.Click += new System.EventHandler(this.Btn_SelectOutputFolder_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // Label_OutputPath
            // 
            this.Label_OutputPath.AutoSize = true;
            this.Label_OutputPath.Location = new System.Drawing.Point(166, 22);
            this.Label_OutputPath.Name = "Label_OutputPath";
            this.Label_OutputPath.Size = new System.Drawing.Size(157, 12);
            this.Label_OutputPath.TabIndex = 5;
            this.Label_OutputPath.Text = "경로가 설정되지 않았습니다";
            // 
            // ListBox_CsvFiles
            // 
            this.ListBox_CsvFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.ListBox_CsvFiles.BackColor = System.Drawing.Color.LightCyan;
            this.ListBox_CsvFiles.FormattingEnabled = true;
            this.ListBox_CsvFiles.ItemHeight = 12;
            this.ListBox_CsvFiles.Location = new System.Drawing.Point(10, 148);
            this.ListBox_CsvFiles.Name = "ListBox_CsvFiles";
            this.ListBox_CsvFiles.Size = new System.Drawing.Size(300, 244);
            this.ListBox_CsvFiles.TabIndex = 6;
            this.ListBox_CsvFiles.SelectedIndexChanged += new System.EventHandler(this.ListBox_CsvFiles_SelectedIndexChanged);
            this.ListBox_CsvFiles.DoubleClick += new System.EventHandler(this.ListBox_CsvFiles_DoubleClick);
            // 
            // TextBox_SelectedCsv
            // 
            this.TextBox_SelectedCsv.Location = new System.Drawing.Point(8, 133);
            this.TextBox_SelectedCsv.Name = "TextBox_SelectedCsv";
            this.TextBox_SelectedCsv.Size = new System.Drawing.Size(302, 12);
            this.TextBox_SelectedCsv.TabIndex = 7;
            this.TextBox_SelectedCsv.Text = "설정할 CSV : ";
            // 
            // TextBox_Preview
            // 
            this.TextBox_Preview.BackColor = System.Drawing.Color.LightGray;
            this.TextBox_Preview.Location = new System.Drawing.Point(330, 100);
            this.TextBox_Preview.Name = "TextBox_Preview";
            this.TextBox_Preview.ReadOnly = true;
            this.TextBox_Preview.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.TextBox_Preview.Size = new System.Drawing.Size(550, 352);
            this.TextBox_Preview.TabIndex = 9;
            this.TextBox_Preview.Text = "";
            // 
            // TextBox_Log
            // 
            this.TextBox_Log.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_Log.BackColor = System.Drawing.Color.Silver;
            this.TextBox_Log.Font = new System.Drawing.Font("Consolas", 8F);
            this.TextBox_Log.ForeColor = System.Drawing.Color.DarkOrange;
            this.TextBox_Log.Location = new System.Drawing.Point(10, 458);
            this.TextBox_Log.Name = "TextBox_Log";
            this.TextBox_Log.ReadOnly = true;
            this.TextBox_Log.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.TextBox_Log.Size = new System.Drawing.Size(710, 102);
            this.TextBox_Log.TabIndex = 9;
            this.TextBox_Log.Text = "";
            // 
            // Label_OpenFile
            // 
            this.Label_OpenFile.AutoSize = true;
            this.Label_OpenFile.Location = new System.Drawing.Point(8, 400);
            this.Label_OpenFile.Name = "Label_OpenFile";
            this.Label_OpenFile.Size = new System.Drawing.Size(233, 12);
            this.Label_OpenFile.TabIndex = 11;
            this.Label_OpenFile.Text = "더블 클릭 하시면 선택된 파일이 열립니다.";
            // 
            // Btn_OpenOutputFolder
            // 
            this.Btn_OpenOutputFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_OpenOutputFolder.BackColor = System.Drawing.Color.Coral;
            this.Btn_OpenOutputFolder.Font = new System.Drawing.Font("Gulim", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.Btn_OpenOutputFolder.Location = new System.Drawing.Point(730, 488);
            this.Btn_OpenOutputFolder.Name = "Btn_OpenOutputFolder";
            this.Btn_OpenOutputFolder.Size = new System.Drawing.Size(150, 30);
            this.Btn_OpenOutputFolder.TabIndex = 12;
            this.Btn_OpenOutputFolder.Text = "출력 폴더 열기";
            this.Btn_OpenOutputFolder.UseVisualStyleBackColor = false;
            this.Btn_OpenOutputFolder.Click += new System.EventHandler(this.Btn_OpenOutputFolder_Click);
            // 
            // Txt_CsvFilter
            // 
            this.Txt_CsvFilter.Font = new System.Drawing.Font("Gulim", 9F);
            this.Txt_CsvFilter.Location = new System.Drawing.Point(93, 3);
            this.Txt_CsvFilter.Name = "Txt_CsvFilter";
            this.Txt_CsvFilter.Size = new System.Drawing.Size(204, 21);
            this.Txt_CsvFilter.TabIndex = 13;
            this.Txt_CsvFilter.TextChanged += new System.EventHandler(this.Txt_CsvFilter_TextChanged);
            // 
            // Combo_RecentCsv
            // 
            this.Combo_RecentCsv.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Combo_RecentCsv.Location = new System.Drawing.Point(93, 3);
            this.Combo_RecentCsv.Name = "Combo_RecentCsv";
            this.Combo_RecentCsv.Size = new System.Drawing.Size(204, 20);
            this.Combo_RecentCsv.TabIndex = 14;
            this.Combo_RecentCsv.SelectedIndexChanged += new System.EventHandler(this.Combo_RecentCsv_SelectedIndexChanged);
            // 
            // Label_CsvFilter
            // 
            this.Label_CsvFilter.AutoSize = true;
            this.Label_CsvFilter.Location = new System.Drawing.Point(3, 6);
            this.Label_CsvFilter.Name = "Label_CsvFilter";
            this.Label_CsvFilter.Size = new System.Drawing.Size(86, 12);
            this.Label_CsvFilter.TabIndex = 16;
            this.Label_CsvFilter.Text = "CSV 파일 검색";
            // 
            // Panel_RecentCsv
            // 
            this.Panel_RecentCsv.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.Panel_RecentCsv.Controls.Add(this.Combo_RecentCsv);
            this.Panel_RecentCsv.Controls.Add(this.Label_RecentCsv);
            this.Panel_RecentCsv.Location = new System.Drawing.Point(10, 49);
            this.Panel_RecentCsv.Name = "Panel_RecentCsv";
            this.Panel_RecentCsv.Size = new System.Drawing.Size(300, 26);
            this.Panel_RecentCsv.TabIndex = 19;
            // 
            // Label_RecentCsv
            // 
            this.Label_RecentCsv.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Label_RecentCsv.AutoSize = true;
            this.Label_RecentCsv.Location = new System.Drawing.Point(3, 6);
            this.Label_RecentCsv.Name = "Label_RecentCsv";
            this.Label_RecentCsv.Size = new System.Drawing.Size(86, 12);
            this.Label_RecentCsv.TabIndex = 15;
            this.Label_RecentCsv.Text = "최근 열린 CSV";
            // 
            // Panel_CsvFilter
            // 
            this.Panel_CsvFilter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.Panel_CsvFilter.Controls.Add(this.Txt_CsvFilter);
            this.Panel_CsvFilter.Controls.Add(this.Label_CsvFilter);
            this.Panel_CsvFilter.Location = new System.Drawing.Point(10, 81);
            this.Panel_CsvFilter.Name = "Panel_CsvFilter";
            this.Panel_CsvFilter.Size = new System.Drawing.Size(300, 26);
            this.Panel_CsvFilter.TabIndex = 20;
            // 
            // Combo_LogFilter
            // 
            this.Combo_LogFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Combo_LogFilter.FormattingEnabled = true;
            this.Combo_LogFilter.Items.AddRange(new object[] {
            "전체",
            "Info",
            "Warning",
            "Error"});
            this.Combo_LogFilter.Location = new System.Drawing.Point(10, 432);
            this.Combo_LogFilter.Name = "Combo_LogFilter";
            this.Combo_LogFilter.Size = new System.Drawing.Size(121, 20);
            this.Combo_LogFilter.TabIndex = 21;
            this.Combo_LogFilter.SelectedIndexChanged += new System.EventHandler(this.Combo_LogFilter_SelectedIndexChanged);
            // 
            // Btn_NewCsv
            // 
            this.Btn_NewCsv.BackColor = System.Drawing.Color.Gold;
            this.Btn_NewCsv.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.Btn_NewCsv.Location = new System.Drawing.Point(730, 13);
            this.Btn_NewCsv.Name = "Btn_NewCsv";
            this.Btn_NewCsv.Size = new System.Drawing.Size(150, 30);
            this.Btn_NewCsv.TabIndex = 22;
            this.Btn_NewCsv.Text = "새 CSV 만들기";
            this.Btn_NewCsv.UseVisualStyleBackColor = false;
            this.Btn_NewCsv.Click += new System.EventHandler(this.Btn_NewCsv_Click);
            // 
            // TextBox_NewCsvName
            // 
            this.TextBox_NewCsvName.Location = new System.Drawing.Point(730, 47);
            this.TextBox_NewCsvName.Name = "TextBox_NewCsvName";
            this.TextBox_NewCsvName.Size = new System.Drawing.Size(150, 21);
            this.TextBox_NewCsvName.TabIndex = 23;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 561);
            this.Controls.Add(this.TextBox_NewCsvName);
            this.Controls.Add(this.Btn_NewCsv);
            this.Controls.Add(this.Combo_LogFilter);
            this.Controls.Add(this.Panel_CsvFilter);
            this.Controls.Add(this.Panel_RecentCsv);
            this.Controls.Add(this.Btn_OpenOutputFolder);
            this.Controls.Add(this.Label_OpenFile);
            this.Controls.Add(this.TextBox_Log);
            this.Controls.Add(this.TextBox_Preview);
            this.Controls.Add(this.TextBox_SelectedCsv);
            this.Controls.Add(this.ListBox_CsvFiles);
            this.Controls.Add(this.Label_OutputPath);
            this.Controls.Add(this.Btn_SelectOutputFolder);
            this.Controls.Add(this.Btn_DataSetting);
            this.MaximumSize = new System.Drawing.Size(900, 600);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "Form1";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Panel_RecentCsv.ResumeLayout(false);
            this.Panel_RecentCsv.PerformLayout();
            this.Panel_CsvFilter.ResumeLayout(false);
            this.Panel_CsvFilter.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button Btn_DataSetting;
        private Button Btn_SelectOutputFolder;
        private ContextMenuStrip contextMenuStrip1;
        private Label Label_OutputPath;
        private ListBox ListBox_CsvFiles;
        private Label TextBox_SelectedCsv;
        private RichTextBox TextBox_Preview;
        private RichTextBox TextBox_Log;
        private Label Label_OpenFile;
        private Button Btn_OpenOutputFolder;
        private TextBox Txt_CsvFilter;
        private ComboBox Combo_RecentCsv;
        private Label Label_CsvFilter;
        private Panel Panel_RecentCsv;
        private Label Label_RecentCsv;
        private Panel Panel_CsvFilter;
        private ComboBox Combo_LogFilter;
        private Button Btn_NewCsv;
        private TextBox TextBox_NewCsvName;
    }
}

