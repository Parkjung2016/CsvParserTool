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
        private void StartExportCompletionAnimation(bool success)
        {
            StopExportCompletionAnimation(resetColors: true);
            if (!SystemInformation.IsMenuAnimationEnabled)
                return;

            exportCompletionAnimationFrame = 0;
            exportCompletionAnimationStartColor = BlendColor(
                UITheme.SurfaceMuted,
                success ? UITheme.LogSuccess : UITheme.LogError,
                0.42D);
            ApplyExportCompletionAnimationColor(exportCompletionAnimationStartColor);
            if (exportCompletionAnimationTimer == null)
            {
                exportCompletionAnimationTimer = new System.Windows.Forms.Timer { Interval = 30 };
                exportCompletionAnimationTimer.Tick += (_, __) => AdvanceExportCompletionAnimation();
            }
            exportCompletionAnimationTimer.Start();
        }

        private void AdvanceExportCompletionAnimation()
        {
            exportCompletionAnimationFrame++;
            double progress = Math.Min(1D, exportCompletionAnimationFrame / 18D);
            double eased = 1D - Math.Pow(1D - progress, 3D);
            ApplyExportCompletionAnimationColor(BlendColor(
                exportCompletionAnimationStartColor,
                UITheme.SurfaceMuted,
                eased));
            if (progress >= 1D)
                StopExportCompletionAnimation(resetColors: true);
        }

        private void StopExportCompletionAnimation(bool resetColors)
        {
            exportCompletionAnimationTimer?.Stop();
            if (resetColors)
                ApplyExportCompletionAnimationColor(UITheme.SurfaceMuted);
        }

        private void ApplyExportCompletionAnimationColor(Color color)
        {
            Panel_ExportProgressTop.BackColor = color;
            Label_ExportStatus.BackColor = color;
            SegmentedExportProgress_Export.BackColor = color;
            Panel_ExportProgressTop.Invalidate();
        }

        private static Color BlendColor(Color from, Color to, double amount)
        {
            amount = Math.Max(0D, Math.Min(1D, amount));
            return Color.FromArgb(
                (int)Math.Round(from.A + (to.A - from.A) * amount),
                (int)Math.Round(from.R + (to.R - from.R) * amount),
                (int)Math.Round(from.G + (to.G - from.G) * amount),
                (int)Math.Round(from.B + (to.B - from.B) * amount));
        }
        private void ShowExportTaskbarNotification(bool success)
        {
            if (!IsHandleCreated || (ContainsFocus && WindowState != FormWindowState.Minimized))
                return;

            ClearExportTaskbarNotification();
            try
            {
                taskbarExportStatusIcon = CreateTaskbarStatusIcon(success);
                if (TaskbarManager.IsPlatformSupported)
                {
                    TaskbarManager.Instance.SetOverlayIcon(
                        Handle,
                        taskbarExportStatusIcon,
                        success ? "Export 완료" : "Export 실패");
                    TaskbarManager.Instance.SetProgressState(
                        success ? TaskbarProgressBarState.Normal : TaskbarProgressBarState.Error,
                        Handle);
                    TaskbarManager.Instance.SetProgressValue(100, 100, Handle);
                }

                FlashTaskbar(stop: false);
            }
            catch
            {
                // 작업 표시줄 기능을 지원하지 않는 환경에서도 Export 결과에는 영향이 없어야 한다.
            }

            ShowExportWindowsNotification(success);
        }
        private void ClearExportTaskbarNotification()
        {
            try
            {
                if (IsHandleCreated && TaskbarManager.IsPlatformSupported)
                {
                    TaskbarManager.Instance.SetOverlayIcon(Handle, null, null);
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, Handle);
                }
            }
            catch
            {
                // Explorer 재시작 등으로 작업 표시줄 핸들이 바뀐 경우 무시한다.
            }

            FlashTaskbar(stop: true);
            taskbarExportStatusIcon?.Dispose();
            taskbarExportStatusIcon = null;
            HideExportWindowsNotification();
        }

        private void ShowExportWindowsNotification(bool success)
        {
            try
            {
                if (exportNotifyIcon == null)
                {
                    exportNotifyIcon = new NotifyIcon
                    {
                        Text = "PJDev Data Tool",
                        Icon = Icon ?? SystemIcons.Application
                    };
                    exportNotifyIcon.BalloonTipClicked += (_, __) => RestoreFromExportNotification();
                    exportNotifyIcon.Click += (_, __) => RestoreFromExportNotification();
                }

                exportNotifyIcon.Visible = true;
                exportNotifyIcon.ShowBalloonTip(
                    5000,
                    success ? "Data Export 완료" : "Data Export 실패",
                    success
                        ? "데이터 Export가 완료되었습니다."
                        : "Data Tool에서 실패 내용을 확인하세요.",
                    success ? ToolTipIcon.Info : ToolTipIcon.Error);

                if (exportNotificationHideTimer == null)
                {
                    exportNotificationHideTimer = new System.Windows.Forms.Timer { Interval = 10000 };
                    exportNotificationHideTimer.Tick += (_, __) => HideExportWindowsNotification();
                }
                exportNotificationHideTimer.Stop();
                exportNotificationHideTimer.Start();
            }
            catch
            {
                // Windows 알림이 꺼져 있거나 Explorer가 재시작 중인 경우 무시한다.
            }
        }

        private void HideExportWindowsNotification()
        {
            exportNotificationHideTimer?.Stop();
            if (exportNotifyIcon != null)
                exportNotifyIcon.Visible = false;
        }

        private void RestoreFromExportNotification()
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Show();
            Activate();
            BringToFront();
        }
        private static Icon CreateTaskbarStatusIcon(bool success)
        {
            using (var bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var font = new Font("Segoe UI Symbol", success ? 18F : 20F, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(Color.White))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                using (var background = new SolidBrush(success
                    ? Color.FromArgb(46, 125, 50)
                    : Color.FromArgb(198, 40, 40)))
                {
                    graphics.FillEllipse(background, 1, 1, 30, 30);
                }

                graphics.DrawString(success ? "\u2713" : "!", font, brush, new RectangleF(0, -1, 32, 33), format);
                IntPtr iconHandle = bitmap.GetHicon();
                try
                {
                    using (Icon borrowed = Icon.FromHandle(iconHandle))
                        return (Icon)borrowed.Clone();
                }
                finally
                {
                    DestroyIcon(iconHandle);
                }
            }
        }

        private void Form1_Activated(object sender, EventArgs e) =>
            ClearExportTaskbarNotification();

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            ClearExportTaskbarNotification();
            exportNotificationHideTimer?.Dispose();
            exportNotificationHideTimer = null;
            exportCompletionAnimationTimer?.Dispose();
            exportCompletionAnimationTimer = null;
            exportLogFlushTimer.Dispose();
            exportNotifyIcon?.Dispose();
            exportNotifyIcon = null;
            exportMiniGameForm?.Dispose();
            exportMiniGameForm = null;
            previewCancellation?.Cancel();
            previewCancellation = null;
        }

        private void FlashTaskbar(bool stop)
        {
            if (!IsHandleCreated)
                return;

            var info = new FlashWindowInfo
            {
                Size = (uint)Marshal.SizeOf(typeof(FlashWindowInfo)),
                WindowHandle = Handle,
                Flags = stop ? FlashWindowStop : FlashWindowTray | FlashWindowTimerNoForeground,
                Count = stop ? 0U : 3U,
                Timeout = 0U
            };
            FlashWindowEx(ref info);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FlashWindowInfo
        {
            public uint Size;
            public IntPtr WindowHandle;
            public uint Flags;
            public uint Count;
            public uint Timeout;
        }

        private const int WmSetRedraw = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);
        private const uint FlashWindowStop = 0;
        private const uint FlashWindowTray = 0x00000002;
        private const uint FlashWindowTimerNoForeground = 0x0000000C;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FlashWindowInfo info);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr iconHandle);
        private void Form1_Shown(object sender, EventArgs e)
        {
            if (!splitContainersInitialized)
            {
                LayoutSplitContainers();
                splitContainersInitialized = true;
            }

            if (Combo_LogFilter.Items.Count > 0 && Combo_LogFilter.SelectedIndex < 0)
                Combo_LogFilter.SelectedIndex = 0;

            if (!startupDialogsScheduled)
            {
                startupDialogsScheduled = true;
                Application.Idle += ShowStartupDialogsAfterFirstPaint;
            }
        }


        private void ShowStartupDialogsAfterFirstPaint(object sender, EventArgs e)
        {
            Application.Idle -= ShowStartupDialogsAfterFirstPaint;
            if (IsDisposed || Disposing || !Visible)
                return;

            CompleteInitialLayout();
            BeginInvoke(new Action(RunStartupDialogs));
        }

        private void CompleteInitialLayout()
        {
            SuspendLayout();
            try
            {
                PerformLayout();
                Panel_Header.PerformLayout();
                tableHeader.PerformLayout();
                Panel_Top.PerformLayout();
                tableTop.PerformLayout();
                Panel_Bottom.PerformLayout();
                tableBottom.PerformLayout();
                Panel_MainContent.PerformLayout();
                splitOuter.PerformLayout();
                splitWork.PerformLayout();
                LayoutSplitContainers();
            }
            finally
            {
                ResumeLayout(true);
            }

            Invalidate(true);
            Update();
        }
        private async void RunStartupDialogs()
        {
            if (ToolSettingsStore.IsFirstRun)
                ShowFirstRunWelcome();

            if (IsDisposed || Disposing)
                return;

            if (!ToolRuntimeEnvironment.UpdatesAllowed)
                return;

            try
            {
                ToolUpdateInfo update = await ToolUpdateService.CheckAsync(CancellationToken.None);
                if (update?.IsNewer == true && !versionDialogShownThisSession && !IsDisposed && !Disposing)
                    ShowVersionDialog(captureOwner: false);
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                    AddLog("시작 시 업데이트 확인 실패: " + ex.Message, LogLevel.Warning);
            }
        }
        private void ShowFirstRunWelcome()
        {
            using (var welcome = new FirstRunWelcomeForm())
            {
                if (Icon != null)
                    welcome.Icon = (Icon)Icon.Clone();
                DialogResult result = ModalBlurBackdrop.ShowDialog(this, welcome, captureOwner: false);
                if (result != DialogResult.Yes)
                    return;
            }

            using (var guide = new ToolInfoForm())
            {
                if (Icon != null)
                    guide.Icon = (Icon)Icon.Clone();
                ModalBlurBackdrop.ShowDialog(this, guide, captureOwner: false);
            }
        }

        /// <summary>사이드바·미리보기·하단 로그 패널 초기 비율.</summary>
        private void LayoutSplitContainers(bool updateWorkWidth = true, bool updateLogHeight = true)
        {
            if (!IsHandleCreated || splitOuter.Width <= 0 || splitWork.Width <= 0)
                return;

            int maxList = splitWork.Width - splitWork.Panel2MinSize - splitWork.SplitterWidth;
            if (updateWorkWidth && maxList >= splitWork.Panel1MinSize)
            {
                int desiredList = Math.Max(260, Math.Min(360, (int)(splitWork.Width * 0.30F)));
                int targetDistance = Math.Min(maxList, Math.Max(splitWork.Panel1MinSize, desiredList));
                if (Math.Abs(splitWork.SplitterDistance - targetDistance) >= 3)
                    splitWork.SplitterDistance = targetDistance;
            }

            int maxWork = splitOuter.Height - splitOuter.Panel2MinSize - splitOuter.SplitterWidth;
            if (updateLogHeight && maxWork >= splitOuter.Panel1MinSize)
            {
                int desiredLog = Panel_ExportProgress.Visible
                    ? Math.Max(340, Math.Min(520, (int)(splitOuter.Height * 0.68F)))
                    : Math.Max(180, Math.Min(240, (int)(splitOuter.Height * 0.30F)));
                int desiredWork = splitOuter.Height - splitOuter.SplitterWidth - desiredLog;
                int targetDistance = Math.Min(maxWork, Math.Max(splitOuter.Panel1MinSize, desiredWork));
                if (Math.Abs(splitOuter.SplitterDistance - targetDistance) >= 3)
                    splitOuter.SplitterDistance = targetDistance;
            }

            if (Panel_ExportProgress.Visible && !splitExportAndLog.Panel1Collapsed)
                InitializeExportResultSplitterDistance();
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            isInteractiveResize = true;
            SuspendLayout();
            SendMessage(Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
            SetHeavyControlsRedraw(enabled: false);
            base.OnResizeBegin(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!IsHandleCreated || WindowState == FormWindowState.Minimized)
                return;

            pendingResizeClientSize = ClientSize;
            if (isInteractiveResize)
                return;

            ApplyPendingResizeLayout();
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            isInteractiveResize = false;
            pendingResizeClientSize = ClientSize;
            ApplyPendingResizeLayout();
            SetHeavyControlsRedraw(enabled: true);
            ResumeLayout(true);
            SendMessage(Handle, WmSetRedraw, new IntPtr(1), IntPtr.Zero);
            Invalidate(true);
            base.OnResizeEnd(e);
        }

        private void SetHeavyControlsRedraw(bool enabled)
        {
            IntPtr redrawFlag = enabled ? new IntPtr(1) : IntPtr.Zero;
            Control[] controls =
            {
                TextBox_Preview,
                TextBox_Log,
                Grid_ExportResults,
                ListBox_CsvFiles
            };

            foreach (Control control in controls)
            {
                if (control.IsHandleCreated)
                    SendMessage(control.Handle, WmSetRedraw, redrawFlag, IntPtr.Zero);
            }

            if (!enabled)
                return;

            foreach (Control control in controls)
                control.Invalidate();
        }
        private void ApplyPendingResizeLayout()
        {
            if (IsDisposed || Disposing || pendingResizeClientSize.IsEmpty)
                return;

            Size currentSize = pendingResizeClientSize;
            bool firstLayout = lastSplitLayoutClientSize.IsEmpty;
            bool widthChanged = firstLayout || currentSize.Width != lastSplitLayoutClientSize.Width;
            bool heightChanged = firstLayout || currentSize.Height != lastSplitLayoutClientSize.Height;
            if (!widthChanged && !heightChanged)
                return;

            LayoutSplitContainers(updateWorkWidth: widthChanged, updateLogHeight: heightChanged);
            lastSplitLayoutClientSize = currentSize;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            projectRootPath = ToolSettingsStore.ProjectRootPath ?? "";
            excelSourceFolderPath = ToolSettingsStore.ExcelSourceFolderPath ?? "";
            exportVersion = NormalizeExportVersion(ToolSettingsStore.ExportVersion);

            UITheme.UpdatePathLabel(Label_ProjectRoot, projectRootPath);
            UITheme.UpdatePathLabel(Label_ExcelSourcePath, excelSourceFolderPath);
            Txt_ExportVersion.Text = exportVersion;
            Chk_RemoveOrphanArtifacts.Checked = ToolSettingsStore.RemoveOrphanArtifactsOnExport;

            ReloadDataFileList();

            AddLog("툴 시작", LogLevel.Info);
            InitDirectoryWatchers();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Application.Idle -= ShowStartupDialogsAfterFirstPaint;
            excelDirWatcher?.Dispose();
            excelDirWatcher = null;
            listReloadDebounceTimer?.Stop();
            listReloadDebounceTimer?.Dispose();
            listReloadDebounceTimer = null;
            base.OnFormClosed(e);
        }

        // =========================
        // 데이터 Export (in-process — 단일 EXE 배포)
        // =========================
    }
}