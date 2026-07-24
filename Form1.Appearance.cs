using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace CSVParserTool
{
    public partial class Form1
    {
        private void InitializeExportLogSplitter()
        {
            Panel_LogSection.SuspendLayout();
            try
            {
                splitExportAndLog.Name = "Split_ExportAndLog";
                splitExportAndLog.Dock = DockStyle.Fill;
                splitExportAndLog.Orientation = Orientation.Horizontal;
                splitExportAndLog.FixedPanel = FixedPanel.Panel1;
                splitExportAndLog.IsSplitterFixed = false;
                splitExportAndLog.Panel1MinSize = 82;
                splitExportAndLog.Panel2MinSize = 72;
                splitExportAndLog.SplitterWidth = 6;
                splitExportAndLog.TabStop = false;

                Panel_ExportProgress.Dock = DockStyle.Fill;
                Panel_LogHeader.Dock = DockStyle.Top;
                Panel_LogCard.Dock = DockStyle.Fill;
                splitExportAndLog.Panel1.Controls.Add(Panel_ExportProgress);
                splitExportAndLog.Panel2.Controls.Add(Panel_LogCard);
                splitExportAndLog.Panel2.Controls.Add(Panel_LogHeader);
                Panel_LogSection.Controls.Add(splitExportAndLog);
                splitExportAndLog.Panel1Collapsed = !Panel_ExportProgress.Visible;
            }
            finally
            {
                Panel_LogSection.ResumeLayout(true);
            }
        }

        private void InitializeExportResultControls()
        {
            Btn_CloseExportResults.Name = "Btn_CloseExportResults";
            Btn_CloseExportResults.Text = "결과 닫기";
            Btn_CloseExportResults.AccessibleName = "Export 결과 닫기";
            Btn_CloseExportResults.Cursor = Cursors.Hand;
            Btn_CloseExportResults.TabStop = true;
            Btn_CloseExportResults.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Btn_CloseExportResults.Click += (_, __) => CloseExportResultsPanel();
            Panel_ExportProgressTop.Controls.Add(Btn_CloseExportResults);
            Btn_CloseExportResults.BringToFront();
            Panel_ExportProgressTop.Resize += (_, __) => LayoutExportResultCloseButton();
        }

        private void LayoutExportResultCloseButton()
        {
            bool dimensional = UITheme.CurrentTheme != AppTheme.Default;
            int buttonWidth = dimensional ? 98 : 88;
            int buttonHeight = dimensional ? 38 : 30;
            Btn_CloseExportResults.Size = new Size(buttonWidth, buttonHeight);
            Btn_CloseExportResults.Location = new Point(
                Math.Max(2, Panel_ExportProgressTop.ClientSize.Width - buttonWidth - 4),
                2);
            Panel_ExportProgressTop.Padding = new Padding(2, 0, 2, 4);
            Label_ExportStatus.Padding = new Padding(0, 0, buttonWidth + 10, 4);
            Btn_CloseExportResults.BringToFront();
        }

        private void CloseExportResultsPanel()
        {
            if (splitExportAndLog.Panel1Collapsed)
                return;

            splitExportAndLog.Panel1Collapsed = true;
            Panel_ExportProgress.Visible = false;
            exportResultSplitterInitialized = false;
            lastExportResultLayoutHeight = -1;
            UpdateOuterSplitMinimums();
            LayoutSplitContainers(updateWorkWidth: false, updateLogHeight: true);
        }

        private void InitializeLogHeaderLayout()
        {
            Panel_LogHeader.SuspendLayout();
            try
            {
                Panel_LogHeader.Controls.Clear();
                logHeaderLayout.SuspendLayout();
                logHeaderLayout.Name = "Table_LogHeader";
                logHeaderLayout.Dock = DockStyle.Fill;
                logHeaderLayout.BackColor = Color.Transparent;
                logHeaderLayout.ColumnCount = 4;
                logHeaderLayout.RowCount = 1;
                logHeaderLayout.Margin = Padding.Empty;
                logHeaderLayout.Padding = Padding.Empty;
                logHeaderLayout.ColumnStyles.Clear();
                logHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                logHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
                logHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                logHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                logHeaderLayout.RowStyles.Clear();
                logHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                Label_SectionLog.Anchor = AnchorStyles.Left;
                Label_SectionLog.Margin = new Padding(2, 0, 14, 0);
                Combo_LogFilter.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                Combo_LogFilter.Margin = Padding.Empty;
                Btn_ClearLog.Anchor = AnchorStyles.Right;
                Btn_ClearLog.Dock = DockStyle.None;
                Btn_ClearLog.Margin = Padding.Empty;

                logHeaderLayout.Controls.Add(Label_SectionLog, 0, 0);
                logHeaderLayout.Controls.Add(Combo_LogFilter, 1, 0);
                logHeaderLayout.Controls.Add(Btn_ClearLog, 3, 0);
                Panel_LogHeader.Controls.Add(logHeaderLayout);
                logHeaderLayout.ResumeLayout(true);

                TextBox_Log.WordWrap = false;
                TextBox_Log.ScrollBars = RichTextBoxScrollBars.Both;
            }
            finally
            {
                Panel_LogHeader.ResumeLayout(true);
            }
        }

        private void ShowExportResultsPanel()
        {
            Panel_ExportProgress.Visible = true;
            if (splitExportAndLog.Panel1Collapsed)
                splitExportAndLog.Panel1Collapsed = false;
            UpdateOuterSplitMinimums();
        }

        private void InitializeExportResultSplitterDistance()
        {
            if (splitExportAndLog.Panel1Collapsed || splitExportAndLog.Height <= 0)
                return;
            if (exportResultSplitterInitialized && lastExportResultLayoutHeight == splitExportAndLog.Height)
                return;

            int maximum = splitExportAndLog.Height
                - splitExportAndLog.Panel2MinSize
                - splitExportAndLog.SplitterWidth;
            if (maximum < splitExportAndLog.Panel1MinSize)
                return;

            splitExportAndLog.SplitterDistance = Math.Max(
                splitExportAndLog.Panel1MinSize,
                Math.Min(250, maximum));
            exportResultSplitterInitialized = true;
            lastExportResultLayoutHeight = splitExportAndLog.Height;
        }
        private void InitializeInfoButton()
        {
            tableHeader.ColumnCount = 8;
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.SetColumn(Chk_DarkMode, 5);
            tableHeader.SetColumn(Btn_DataSetting, 7);
            Btn_DataSetting.Text = "전체 Export";

            Btn_Version.Name = "Btn_Version";
            Btn_Version.Text = "버전";
            Btn_Version.AccessibleName = "툴 버전 및 업데이트 정보";
            Btn_Version.Anchor = AnchorStyles.Right;
            Btn_Version.AutoSize = true;
            Btn_Version.Margin = new Padding(0, 0, 10, 0);
            Btn_Version.TabIndex = 2;
            Btn_Version.Click += Btn_Version_Click;
            tableHeader.Controls.Add(Btn_Version, 2, 0);
            tableHeader.SetRowSpan(Btn_Version, 2);

            Btn_Info.Name = "Btn_Info";
            Btn_Info.Text = "i";
            Btn_Info.AccessibleName = "사용 안내";
            Btn_Info.Anchor = AnchorStyles.Right;
            Btn_Info.Margin = new Padding(0, 0, 12, 0);
            Btn_Info.TabIndex = 2;
            Btn_Info.Click += Btn_Info_Click;
            tableHeader.Controls.Add(Btn_Info, 3, 0);
            tableHeader.SetRowSpan(Btn_Info, 2);
            Btn_Theme.Name = "Btn_Theme";
            Btn_Theme.Text = "테마";
            Btn_Theme.AccessibleName = "작업 공간 테마 선택";
            Btn_Theme.Anchor = AnchorStyles.Right;
            Btn_Theme.AutoSize = true;
            Btn_Theme.Margin = new Padding(0, 0, 10, 0);
            Btn_Theme.TabIndex = 2;
            Btn_Theme.Click += Btn_Theme_Click;
            tableHeader.Controls.Add(Btn_Theme, 4, 0);
            tableHeader.SetRowSpan(Btn_Theme, 2);

            Btn_ExportSelected.Name = "Btn_ExportSelected";
            Btn_ExportSelected.Text = "선택 Export";
            Btn_ExportSelected.AccessibleName = "체크한 테이블만 Export";
            Btn_ExportSelected.Anchor = AnchorStyles.Right;
            Btn_ExportSelected.AutoSize = true;
            Btn_ExportSelected.Margin = new Padding(0, 0, 10, 0);
            Btn_ExportSelected.TabIndex = 3;
            Btn_ExportSelected.Click += Btn_ExportSelected_Click;
            tableHeader.Controls.Add(Btn_ExportSelected, 6, 0);
            tableHeader.SetRowSpan(Btn_ExportSelected, 2);

            var listHeaderLayout = new TableLayoutPanel
            {
                Name = "Table_ListHeaderActions",
                ColumnCount = 3,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            listHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            listHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            listHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            listHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            listHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var selectionLayout = new TableLayoutPanel
            {
                Name = "Table_ListSelectionActions",
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            selectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            selectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            selectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Btn_EnumCatalog.Name = "Btn_EnumCatalog";
            Btn_EnumCatalog.Text = "Enum XLSX";
            Btn_EnumCatalog.AccessibleName = "Enum 관리 XLSX 생성 또는 열기";
            Btn_EnumCatalog.AutoSize = true;
            Btn_EnumCatalog.Dock = DockStyle.Fill;
            Btn_EnumCatalog.Margin = new Padding(0, 0, 4, 0);
            Btn_EnumCatalog.Padding = new Padding(6, 2, 6, 2);
            Btn_EnumCatalog.TabIndex = 1;
            Btn_EnumCatalog.Click += Btn_EnumCatalog_Click;

            Btn_CheckAll.Name = "Btn_CheckAll";
            Btn_CheckAll.Text = "\uC804\uCCB4 \uC120\uD0DD";
            Btn_CheckAll.AccessibleName = "\uBAA8\uB4E0 \uD14C\uC774\uBE14 \uCCB4\uD06C";
            Btn_CheckAll.Dock = DockStyle.Fill;
            Btn_CheckAll.Margin = new Padding(0, 4, 4, 0);
            Btn_CheckAll.TabIndex = 2;
            Btn_CheckAll.Click += Btn_CheckAll_Click;

            Btn_UncheckAll.Name = "Btn_UncheckAll";
            Btn_UncheckAll.Text = "\uC804\uCCB4 \uD574\uC81C";
            Btn_UncheckAll.AccessibleName = "\uBAA8\uB4E0 \uD14C\uC774\uBE14 \uCCB4\uD06C \uD574\uC81C";
            Btn_UncheckAll.Dock = DockStyle.Fill;
            Btn_UncheckAll.Margin = new Padding(0, 4, 0, 0);
            Btn_UncheckAll.TabIndex = 3;
            Btn_UncheckAll.Click += Btn_UncheckAll_Click;
            ListBox_CsvFiles.MouseUp += ListBox_CsvFiles_MouseUp;

            Panel_ListHeader.Height = 80;
            Label_SectionList.Dock = DockStyle.Fill;
            Label_SectionList.Padding = new Padding(2, 0, 0, 0);
            Panel_ListHeader.Controls.Remove(Btn_RefreshList);
            Panel_ListHeader.Controls.Remove(Label_SectionList);
            Btn_RefreshList.Margin = Padding.Empty;
            Btn_RefreshList.Dock = DockStyle.Fill;
            listHeaderLayout.Controls.Add(Label_SectionList, 0, 0);
            listHeaderLayout.Controls.Add(Btn_EnumCatalog, 1, 0);
            listHeaderLayout.Controls.Add(Btn_RefreshList, 2, 0);
            selectionLayout.Controls.Add(Btn_CheckAll, 0, 0);
            selectionLayout.Controls.Add(Btn_UncheckAll, 1, 0);
            listHeaderLayout.Controls.Add(selectionLayout, 0, 1);
            listHeaderLayout.SetColumnSpan(selectionLayout, 3);
            Panel_ListHeader.Controls.Add(listHeaderLayout);
            Activated += Form1_Activated;
            FormClosed += Form1_FormClosed;
        }

        private bool versionDialogOpen;
        private bool versionDialogShownThisSession;

        private void Btn_Version_Click(object sender, EventArgs e) => ShowVersionDialog();

        private void ShowVersionDialog(bool captureOwner = true)
        {
            if (versionDialogOpen || IsDisposed)
                return;

            versionDialogOpen = true;
            versionDialogShownThisSession = true;
            try
            {
                using (var dialog = new ToolVersionForm())
                {
                    if (Icon != null)
                        dialog.Icon = (Icon)Icon.Clone();
                    ModalBlurBackdrop.ShowDialog(this, dialog, captureOwner);
                }
            }
            finally
            {
                versionDialogOpen = false;
            }
        }
        private void Btn_Theme_Click(object sender, EventArgs e)
        {
            using (var dialog = new ThemeSelectionForm(UITheme.CurrentTheme))
            {
                if (Icon != null)
                    dialog.Icon = (Icon)Icon.Clone();
                if (ModalBlurBackdrop.ShowDialog(this, dialog) != DialogResult.OK)
                    return;

                ApplyThemeWithTransition(() =>
                {
                    UITheme.SetTheme(dialog.SelectedTheme, Chk_DarkMode.Checked);
                    ToolSettingsStore.ThemeName = dialog.SelectedTheme.ToString();
                    ToolSettingsStore.Save();
                });
            }
        }
        private void Btn_Info_Click(object sender, EventArgs e)
        {
            using (var dialog = new ToolInfoForm())
            {
                if (Icon != null)
                    dialog.Icon = (Icon)Icon.Clone();
                ModalBlurBackdrop.ShowDialog(this, dialog);
            }
        }

        private void ApplyAppIcon()
        {
            TrySetFormIcon();
            TrySetHeaderIcon();
        }

        private void ApplyThemeHeaderIcon()
        {
            // Main header always uses the stable app icon. Theme artwork is only shown in the picker.
            TrySetHeaderIcon();
        }
        private void TrySetFormIcon()
        {
            try
            {
                string iconPath = ResolveAppIconPath();
                if (!string.IsNullOrEmpty(iconPath))
                    Icon = Icon.ExtractAssociatedIcon(iconPath);
            }
            catch
            {
                // exe 아이콘 로드 실패 시 무시
            }
        }

        private void TrySetHeaderIcon()
        {
            try
            {
                using (Bitmap bitmap = LoadHeaderIconBitmap())
                {
                    if (bitmap == null)
                        return;

                    Image old = PictureBox_HeaderIcon.Image;
                    PictureBox_HeaderIcon.Image = new Bitmap(bitmap);
                    old?.Dispose();
                }
            }
            catch
            {
                // 헤더 아이콘 로드 실패 시 무시
            }
        }

        private static string ResolveAppIconPath() =>
            Application.ExecutablePath;

        private static Bitmap LoadHeaderIconBitmap()
        {
            Assembly asm = typeof(Form1).Assembly;
            foreach (string name in asm.GetManifestResourceNames())
            {
                if (!name.EndsWith("pjdev-icon-source-square.png", StringComparison.OrdinalIgnoreCase))
                    continue;

                using (var stream = asm.GetManifestResourceStream(name))
                {
                    if (stream != null)
                        return new Bitmap(stream);
                }
            }

            string iconPath = ResolveAppIconPath();
            if (!File.Exists(iconPath))
                return null;

            using (var icon = new Icon(iconPath, 32, 32))
            using (var temp = icon.ToBitmap())
                return new Bitmap(temp);
        }

        private void Chk_DarkMode_CheckedChanged(object sender, EventArgs e)
        {
            bool darkMode = Chk_DarkMode.Checked;
            ApplyThemeWithTransition(() =>
            {
                UITheme.SetTheme(UITheme.ParseTheme(ToolSettingsStore.ThemeName), darkMode);
                ToolSettingsStore.DarkMode = darkMode;
                ToolSettingsStore.Save();
            });
        }

        private void ApplyThemeWithTransition(Action updateTheme)
        {
            Bitmap snapshot = null;
            if (Visible && IsHandleCreated && SystemInformation.IsMenuAnimationEnabled)
            {
                try
                {
                    snapshot = ThemeTransitionOverlay.CaptureSnapshot(this);
                }
                catch
                {
                    snapshot?.Dispose();
                    snapshot = null;
                }
            }

            updateTheme();
            ApplyUITheme();
            ThemeTransitionOverlay.Show(this, snapshot);
        }
        private void SetPreviewCode(string code)
        {
            currentPreviewCode = code ?? string.Empty;
            CSharpPreviewHighlighter.Apply(TextBox_Preview, currentPreviewCode, UITheme.IsDarkMode);
        }

        private void ApplyUITheme()
        {
            if (!IsHandleCreated)
            {
                ApplyUIThemeCore();
                return;
            }

            SuspendLayout();
            SendMessage(Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
            try
            {
                ApplyUIThemeCore();
                SetPreviewCode(currentPreviewCode);
                RefreshLogDisplay();
                StyleExportResultRows();
            }
            finally
            {
                ResumeLayout(true);
                LayoutSplitContainers();
                SendMessage(Handle, WmSetRedraw, new IntPtr(1), IntPtr.Zero);
                Invalidate(true);
                Update();
            }

            QueueThemeContentRefresh();
        }

        private void QueueThemeContentRefresh()
        {
            int version = ++themeContentRefreshVersion;
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed || Disposing || version != themeContentRefreshVersion)
                    return;

                SetPreviewCode(currentPreviewCode);
                RefreshLogDisplay();
                StyleExportResultRows();
            }));
        }
        private void ApplyUIThemeCore()
        {
            BackColor = UITheme.AppBackground;
            Font = UITheme.FontUI;
            ForeColor = UITheme.TextPrimary;
            Text = "PJDev Data Tool";

            UITheme.StyleChromePanel(Panel_Header, accent: true);
            UITheme.StyleChromePanel(Panel_Top);
            UITheme.StyleChromePanel(Panel_Bottom);

            Label_AppTitle.Font = UITheme.FontTitle;
            Label_AppTitle.ForeColor = UITheme.Accent;
            Label_AppSubtitle.Font = UITheme.FontSubtitle;
            Label_AppSubtitle.ForeColor = UITheme.TextMuted;

            UITheme.StyleCheckBox(Chk_DarkMode);
            Chk_DarkMode.BackColor = Color.Transparent;

            PictureBox_HeaderIcon.BackColor = Color.Transparent;
            tableHeader.BackColor = Color.Transparent;
            tableTop.BackColor = Color.Transparent;
            tableBottom.BackColor = Color.Transparent;
            ApplyThemeHeaderIcon();

            UITheme.StyleSectionLabel(Label_SectionList);
            UITheme.StyleSectionLabel(Label_SectionPreview);
            Panel_ListHeader.BackColor = UITheme.AppBackground;
            Label_SectionLog.Font = UITheme.FontSection;
            Label_SectionLog.ForeColor = UITheme.LogInfo;
            Label_SectionLog.AutoSize = true;
            UITheme.StyleCaptionLabel(Label_CsvFilter);

            UITheme.StyleSurfacePanel(Panel_ListCard);
            UITheme.StylePreviewPanel(Panel_PreviewCard);
            UITheme.StyleLogPanel(Panel_LogCard);
            Panel_LogHeader.BackColor = UITheme.LogHeaderBackground;

            UITheme.StylePrimaryButton(Btn_DataSetting, tall: true);
            UITheme.StyleSecondaryButton(Btn_ExportSelected);
            UITheme.StyleSecondaryButton(Btn_EnumCatalog);
            UITheme.StyleSecondaryButton(Btn_Info);
            UITheme.StyleSecondaryButton(Btn_Version);
            UITheme.StyleSecondaryButton(Btn_Theme);
            Btn_Info.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Regular, GraphicsUnit.Point);
            Btn_Info.Padding = Padding.Empty;
            Btn_Info.MinimumSize = UITheme.CurrentTheme == AppTheme.Default ? new Size(34, 34) : new Size(40, 40);
            UITheme.StyleSecondaryButton(Btn_CheckAll);
            UITheme.StyleSecondaryButton(Btn_UncheckAll);
            Btn_Info.Size = UITheme.CurrentTheme == AppTheme.Default ? new Size(34, 34) : new Size(40, 40);
            UITheme.StyleSecondaryButton(Btn_SelectProjectRoot);
            UITheme.StyleSecondaryButton(Btn_SelectExcelFolder);
            UITheme.StyleSecondaryButton(Btn_OpenOutputFolder);
            UITheme.StyleSecondaryButton(Btn_OpenCsvFolder);
            UITheme.StyleSecondaryButton(Btn_OpenXlsxFolder);
            UITheme.StyleSecondaryButton(Btn_NewCsv);
            UITheme.StyleSecondaryButton(Btn_RefreshList);
            UITheme.StyleSecondaryButton(Btn_ClearLog);
            UITheme.StyleSecondaryButton(Btn_CloseExportResults);
            Btn_CloseExportResults.Font = UITheme.FontUIMedium;
            Btn_CloseExportResults.Padding = Padding.Empty;

            UITheme.StylePathLabel(Label_ProjectRoot);
            UITheme.StylePathLabel(Label_ExcelSourcePath);
            UITheme.StyleTextField(Txt_CsvFilter);
            UITheme.StyleTextField(TextBox_NewCsvName);
            UITheme.StyleTextField(Txt_ExportVersion);
            UITheme.StyleCheckBox(Chk_RemoveOrphanArtifacts);
            UITheme.StyleCombo(Combo_LogFilter);
            UITheme.StyleList(ListBox_CsvFiles);
            UITheme.StylePreviewBox(TextBox_Preview);
            UITheme.StyleLogBox(TextBox_Log);
            UITheme.StyleExportGrid(Grid_ExportResults);
            UITheme.StyleSectionLabel(Label_SectionExport);
            Panel_ExportProgress.BackColor = UITheme.SurfaceMuted;
            Panel_ExportProgressTop.BackColor = UITheme.SurfaceMuted;
            SegmentedExportProgress_Export.BackColor = UITheme.SurfaceMuted;
            Label_ExportStatus.Font = UITheme.FontUIMedium;
            Label_ExportStatus.ForeColor = UITheme.TextPrimary;
            Label_ExportStatus.BackColor = UITheme.SurfaceMuted;
            SegmentedExportProgress_Export.Invalidate();
            exportMiniGameForm?.ApplyTheme();

            splitOuter.BackColor = UITheme.Border;
            splitOuter.Panel1.BackColor = UITheme.AppBackground;
            splitOuter.Panel2.BackColor = UITheme.AppBackground;
            splitWork.BackColor = UITheme.Border;
            splitWork.Panel1.BackColor = UITheme.AppBackground;
            splitWork.Panel2.BackColor = UITheme.AppBackground;
            splitExportAndLog.BackColor = UITheme.Border;
            splitExportAndLog.Panel1.BackColor = UITheme.AppBackground;
            splitExportAndLog.Panel2.BackColor = UITheme.AppBackground;
            Panel_LogSection.BackColor = UITheme.AppBackground;
            Panel_MainContent.BackColor = UITheme.AppBackground;
            ApplyDimensionalLayout();
        }

        private void ApplyDimensionalLayout()
        {
            bool dimensional = UITheme.CurrentTheme != AppTheme.Default;
            MinimumSize = dimensional ? new Size(1000, 700) : new Size(920, 620);
            Panel_Header.MinimumSize = new Size(0, dimensional ? 84 : 76);
            Panel_Top.MinimumSize = new Size(0, dimensional ? 188 : 166);
            Panel_Bottom.MinimumSize = dimensional ? new Size(0, 70) : Size.Empty;
            UpdateOuterSplitMinimums();
            splitWork.Panel1MinSize = dimensional ? 220 : 200;
            splitWork.Panel2MinSize = dimensional ? 300 : 280;
            Panel_MainContent.Padding = dimensional
                ? new Padding(18, 12, 18, 14)
                : new Padding(16, 10, 16, 12);
            splitWork.SplitterWidth = dimensional ? 12 : 10;
            splitOuter.SplitterWidth = dimensional ? 12 : 10;
            splitExportAndLog.SplitterWidth = 10;
            splitWork.Panel1.Padding = dimensional ? new Padding(0, 0, 10, 0) : new Padding(0, 0, 10, 0);
            splitWork.Panel2.Padding = dimensional ? new Padding(10, 0, 0, 0) : new Padding(10, 0, 0, 0);
            Panel_ListHeader.Height = dimensional ? 88 : 84;
            Panel_LogHeader.Height = 38;
            Panel_ExportProgressTop.Height = dimensional ? 96 : 88;
            Label_SectionPreview.Height = 32;
            LayoutExportResultCloseButton();
        }
        private void UpdateOuterSplitMinimums()
        {
            bool dimensional = UITheme.CurrentTheme != AppTheme.Default;
            bool exportResultsVisible = Panel_ExportProgress.Visible && !splitExportAndLog.Panel1Collapsed;
            if (exportResultsVisible)
            {
                int exportMinimum = Panel_ExportProgressTop.Height
                    + Grid_ExportResults.ColumnHeadersHeight
                    + Grid_ExportResults.RowTemplate.Height
                    + Panel_ExportProgress.Padding.Bottom;
                splitExportAndLog.Panel1MinSize = exportMinimum;
                splitExportAndLog.Panel2MinSize = Math.Max(72, Panel_LogHeader.Height + 34);
                splitOuter.Panel1MinSize = 48;
                splitOuter.Panel2MinSize = splitExportAndLog.Panel1MinSize
                    + splitExportAndLog.SplitterWidth
                    + splitExportAndLog.Panel2MinSize;
            }
            else
            {
                splitExportAndLog.Panel1MinSize = 82;
                splitExportAndLog.Panel2MinSize = 72;
                splitOuter.Panel1MinSize = dimensional ? 150 : 180;
                splitOuter.Panel2MinSize = dimensional ? 140 : 180;
            }
        }
    }
}