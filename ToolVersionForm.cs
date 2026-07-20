using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal sealed class ToolVersionForm : Form
    {
        private readonly Label statusLabel;
        private readonly Label latestVersionLabel;
        private readonly Label latestDateLabel;
        private readonly Button checkButton;
        private readonly Button updateButton;
        private readonly LinkLabel releaseLink;
        private readonly ProgressBar progressBar;
        private CancellationTokenSource cancellation;
        private ToolUpdateInfo availableUpdate;
        private bool checking;

        public ToolVersionForm()
        {
            Text = "버전 정보";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(600, 438);
            Font = UiTheme.FontUi;
            BackColor = UiTheme.AppBackground;
            ForeColor = UiTheme.TextPrimary;
            Padding = new Padding(24);
            Opacity = 0;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = UiTheme.AppBackground,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label
            {
                AutoSize = true,
                Text = "PJDev Data Tool 버전 정보",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.Accent,
                Margin = new Padding(0, 0, 0, 5)
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Text = "현재 설치된 버전과 GitHub의 최신 배포 버전을 확인합니다.",
                ForeColor = UiTheme.TextMuted,
                Margin = new Padding(0, 0, 0, 18)
            };

            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                BackColor = UiTheme.Surface,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 0, 16)
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145F));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 5; i++) card.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

            latestVersionLabel = CreateValueLabel("확인 전");
            latestDateLabel = CreateValueLabel("확인 전");
            AddInfoRow(card, 0, "현재 버전", "v" + ToolVersionInfo.VersionText);
            AddInfoRow(card, 1, "업데이트 날짜", ToolVersionInfo.UpdateDate);
            AddInfoRow(card, 2, "최신 버전", latestVersionLabel);
            AddInfoRow(card, 3, "최신 배포일", latestDateLabel);

            releaseLink = new LinkLabel
            {
                AutoSize = true,
                Text = "GitHub 저장소 열기",
                LinkColor = UiTheme.Accent,
                ActiveLinkColor = UiTheme.AccentPressed,
                VisitedLinkColor = UiTheme.Accent,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Left
            };
            releaseLink.LinkClicked += (_, __) => OpenUrl(
                availableUpdate?.NotesUrl ?? ToolVersionInfo.RepositoryUrl);
            AddInfoRow(card, 4, "프로젝트", releaseLink);

            var statusPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = UiTheme.SurfaceMuted,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 0, 0, 14)
            };
            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "최신 버전을 확인하고 있습니다…",
                ForeColor = UiTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            statusPanel.Controls.Add(statusLabel);

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 5,
                Style = ProgressBarStyle.Continuous,
                Visible = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty
            };
            var closeButton = new Button { Text = "닫기", AutoSize = true, DialogResult = DialogResult.Cancel, Margin = Padding.Empty, Padding = new Padding(12, 4, 12, 4) };
            checkButton = new Button { Text = "다시 확인", AutoSize = true, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(12, 4, 12, 4) };
            updateButton = new Button { Text = "업데이트", AutoSize = true, Visible = false, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(16, 4, 16, 4) };
            UiTheme.StyleSecondaryButton(closeButton);
            UiTheme.StyleSecondaryButton(checkButton);
            UiTheme.StylePrimaryButton(updateButton);
            checkButton.Click += async (_, __) => await CheckForUpdateAsync(forceRefresh: true);
            updateButton.Click += async (_, __) => await InstallUpdateAsync();
            actions.Controls.Add(closeButton);
            actions.Controls.Add(checkButton);
            actions.Controls.Add(updateButton);

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(subtitle, 0, 1);
            root.Controls.Add(card, 0, 2);
            root.Controls.Add(statusPanel, 0, 3);
            root.Controls.Add(progressBar, 0, 4);
            root.Controls.Add(actions, 0, 5);
            root.RowCount = 6;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);
            CancelButton = closeButton;

            Shown += async (_, __) =>
            {
                await FadeInAsync();
                await CheckForUpdateAsync();
            };
            FormClosed += (_, __) => cancellation?.Cancel();
        }

        private async Task CheckForUpdateAsync(bool forceRefresh = false)
        {
            if (checking) return;
            checking = true;
            cancellation?.Cancel();
            cancellation = new CancellationTokenSource();
            checkButton.Enabled = false;
            updateButton.Visible = false;
            progressBar.Visible = false;
            statusLabel.ForeColor = UiTheme.TextSecondary;
            statusLabel.Text = "GitHub에서 최신 버전을 확인하고 있습니다…";
            latestVersionLabel.Text = "확인 중…";
            latestDateLabel.Text = "확인 중…";

            try
            {
                availableUpdate = await ToolUpdateService.CheckAsync(cancellation.Token, forceRefresh);
                latestVersionLabel.Text = "v" + availableUpdate.VersionText;
                latestDateLabel.Text = availableUpdate.PublishedAt;
                releaseLink.Text = "릴리스 내용 보기";

                if (availableUpdate.IsNewer)
                {
                    statusLabel.Text = $"새 버전 v{availableUpdate.VersionText}을 사용할 수 있습니다.";
                    statusLabel.ForeColor = UiTheme.Accent;
                    updateButton.Visible = true;
                    updateButton.Enabled = !string.IsNullOrWhiteSpace(availableUpdate.DownloadUrl);
                    if (!updateButton.Enabled)
                        statusLabel.Text += " 배포 ZIP이 없어 자동 업데이트는 아직 사용할 수 없습니다.";
                }
                else
                {
                    statusLabel.Text = "현재 최신 버전을 사용하고 있습니다.";
                    statusLabel.ForeColor = UiTheme.LogSuccess;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                availableUpdate = null;
                latestVersionLabel.Text = "확인 실패";
                latestDateLabel.Text = "확인 실패";
                releaseLink.Text = "GitHub 저장소 열기";
                statusLabel.Text = ex.Message;
                statusLabel.ForeColor = UiTheme.LogWarning;
            }
            finally
            {
                checking = false;
                checkButton.Enabled = true;
            }
        }

        private async Task InstallUpdateAsync()
        {
            if (availableUpdate == null || !availableUpdate.IsNewer) return;
            DialogResult answer = MessageBox.Show(
                $"v{availableUpdate.VersionText}을 다운로드하고 설치할까요?\r\n\r\n설치가 시작되면 툴이 자동으로 종료된 뒤 다시 실행됩니다.",
                "업데이트 설치",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;

            checkButton.Enabled = false;
            updateButton.Enabled = false;
            progressBar.Value = 0;
            progressBar.Visible = true;
            statusLabel.ForeColor = UiTheme.Accent;
            statusLabel.Text = "업데이트를 다운로드하고 있습니다…";

            try
            {
                var progress = new Progress<int>(value =>
                {
                    progressBar.Value = Math.Max(0, Math.Min(100, value));
                    statusLabel.Text = $"업데이트를 다운로드하고 있습니다… {value}%";
                });
                string payload = await ToolUpdateService.DownloadAsync(availableUpdate, progress, CancellationToken.None);
                statusLabel.Text = "다운로드 완료. 툴을 다시 시작합니다…";
                ToolUpdateService.StartInstaller(payload);
                Application.Exit();
            }
            catch (Exception ex)
            {
                progressBar.Visible = false;
                checkButton.Enabled = true;
                updateButton.Enabled = true;
                statusLabel.ForeColor = UiTheme.LogError;
                statusLabel.Text = "업데이트를 설치하지 못했습니다.";
                MessageBox.Show(this, ex.Message, "업데이트 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void AddInfoRow(TableLayoutPanel table, int row, string label, string value) =>
            AddInfoRow(table, row, label, CreateValueLabel(value));

        private static void AddInfoRow(TableLayoutPanel table, int row, string label, Control value)
        {
            table.Controls.Add(new Label
            {
                AutoSize = true,
                Text = label,
                ForeColor = UiTheme.TextMuted,
                Anchor = AnchorStyles.Left,
                Margin = Padding.Empty
            }, 0, row);
            table.Controls.Add(value, 1, row);
        }

        private static Label CreateValueLabel(string text) => new Label
        {
            AutoSize = true,
            Text = text,
            Font = UiTheme.FontUiMedium,
            ForeColor = UiTheme.TextPrimary,
            Anchor = AnchorStyles.Left,
            Margin = Padding.Empty
        };

        private async Task FadeInAsync()
        {
            for (int i = 1; i <= 8 && !IsDisposed; i++)
            {
                Opacity = i / 8d;
                await Task.Delay(16);
            }
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "브라우저 열기 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}