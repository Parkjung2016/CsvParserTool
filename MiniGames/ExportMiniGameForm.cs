using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CSVParserTool.MiniGames
{
    internal sealed class ExportMiniGameForm : Form
    {
        private readonly ComboBox gameSelector = new ComboBox();
        private readonly ComboBox difficultySelector = new ComboBox();
        private readonly Button restartButton = new Button();

        private readonly Label descriptionLabel = new Label();
        private readonly Label exportStatusLabel = new Label();
        private readonly Label scoreLabel = new Label();
        private readonly MiniGameCanvas canvas = new MiniGameCanvas();
        private readonly MiniGameDescriptor[] games;
        private readonly Random random = new Random();
        private bool updatingDifficulty;
        private bool exportRunning;
        private int currentScore;
        private int currentHighScore;

        public ExportMiniGameForm(Action<string> warning)
        {
            Text = "Export Mini Game";
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(500, 400);
            ClientSize = new Size(660, 520);
            KeyPreview = false;

            games = MiniGameCatalog.Discover(warning).ToArray();
            BuildLayout();
            ApplyTheme();

            gameSelector.Items.AddRange(games.Cast<object>().ToArray());
            if (gameSelector.Items.Count > 0)
                gameSelector.SelectedIndex = 0;
            else
                descriptionLabel.Text = "등록된 미니게임이 없습니다. 게임 소스를 추가한 뒤 다시 빌드하세요.";
        }

        public void ShowCentered(Form owner)
        {
            if (owner == null)
            {
                Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
                Location = CenterInside(workingArea, workingArea);
                Show();
                return;
            }

            Rectangle ownerBounds = owner.WindowState == FormWindowState.Minimized
                ? owner.RestoreBounds
                : owner.Bounds;
            Rectangle workingAreaForOwner = Screen.FromRectangle(ownerBounds).WorkingArea;
            Location = CenterInside(ownerBounds, workingAreaForOwner);
            Show(owner);
        }

        private Point CenterInside(Rectangle ownerBounds, Rectangle workingArea)
        {
            int desiredX = ownerBounds.Left + (ownerBounds.Width - Width) / 2;
            int desiredY = ownerBounds.Top + (ownerBounds.Height - Height) / 2;
            int x = Width <= workingArea.Width
                ? Math.Max(workingArea.Left, Math.Min(workingArea.Right - Width, desiredX))
                : workingArea.Left;
            int y = Height <= workingArea.Height
                ? Math.Max(workingArea.Top, Math.Min(workingArea.Bottom - Height, desiredY))
                : workingArea.Top;
            return new Point(x, y);
        }
        public void StartExport()
        {
            SelectRandomGame();
            exportRunning = true;
            exportStatusLabel.Text = "Export 진행 중 · 게임은 Export 속도에 영향을 주지 않습니다.";
            exportStatusLabel.ForeColor = UiTheme.StatusRunning;
            canvas.SetExportRunning(true);
        }

        public void UpdateExportProgress(string message)
        {
            if (exportRunning && !string.IsNullOrWhiteSpace(message))
                exportStatusLabel.Text = message;
        }

        public void CompleteExport(bool success, string message)
        {
            exportRunning = false;
            exportStatusLabel.Text = string.IsNullOrWhiteSpace(message)
                ? success ? "Export 완료" : "Export 실패"
                : message;
            exportStatusLabel.ForeColor = success ? UiTheme.LogSuccess : UiTheme.LogError;
            canvas.SetExportRunning(false);
        }

        public void ApplyTheme()
        {
            BackColor = UiTheme.AppBackground;
            ForeColor = UiTheme.TextPrimary;
            gameSelector.BackColor = UiTheme.Surface;
            gameSelector.ForeColor = UiTheme.TextPrimary;
            descriptionLabel.ForeColor = UiTheme.TextSecondary;
            scoreLabel.ForeColor = UiTheme.TextPrimary;
            UiTheme.StyleCombo(gameSelector);
            UiTheme.StyleCombo(difficultySelector);
            UiTheme.StyleSecondaryButton(restartButton);
            canvas.SetPalette(CreatePalette());
            Invalidate(true);
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(16)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 4,
                Margin = new Padding(0, 0, 0, 8)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            gameSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            gameSelector.Dock = DockStyle.Fill;
            gameSelector.SelectedIndexChanged += (_, __) => LoadSelectedGame();
            difficultySelector.DropDownStyle = ComboBoxStyle.DropDownList;
            difficultySelector.AutoSize = true;
            difficultySelector.Visible = false;
            difficultySelector.AccessibleName = "게임 난이도";
            difficultySelector.SelectedIndexChanged += (_, __) => ApplySelectedDifficulty();
            restartButton.Text = "다시 시작";
            restartButton.AutoSize = true;
            restartButton.Click += (_, __) => LoadSelectedGame(preserveDifficulty: true);

            scoreLabel.Text = "Score 0  ·  Best 0";
            scoreLabel.AutoSize = true;
            scoreLabel.Anchor = AnchorStyles.None;
            scoreLabel.Padding = new Padding(10, 7, 10, 0);
            canvas.ScoreChanged += score =>
            {
                currentScore = score;
                UpdateScoreLabel();
            };
            canvas.HighScoreChanged += score =>
            {
                currentHighScore = score;
                UpdateScoreLabel();
            };

            toolbar.Controls.Add(gameSelector, 0, 0);
            toolbar.Controls.Add(difficultySelector, 1, 0);
            toolbar.Controls.Add(scoreLabel, 2, 0);
            toolbar.Controls.Add(restartButton, 3, 0);

            descriptionLabel.Dock = DockStyle.Fill;
            descriptionLabel.AutoSize = false;
            descriptionLabel.AutoEllipsis = true;
            descriptionLabel.TextAlign = ContentAlignment.MiddleLeft;
            descriptionLabel.Padding = new Padding(2, 0, 2, 6);
            canvas.Dock = DockStyle.Fill;
            canvas.MinimumSize = new Size(280, 170);
            canvas.Margin = Padding.Empty;
            exportStatusLabel.Dock = DockStyle.Fill;
            exportStatusLabel.AutoSize = true;
            exportStatusLabel.Padding = new Padding(2, 10, 2, 0);

            root.Controls.Add(toolbar, 0, 0);
            root.Controls.Add(descriptionLabel, 0, 1);
            root.Controls.Add(canvas, 0, 2);
            root.Controls.Add(exportStatusLabel, 0, 3);
            Controls.Add(root);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // The canvas already renders the score. Hide the duplicate toolbar score
            // before it can squeeze the game selector on a narrow window.
            scoreLabel.Visible = ClientSize.Width >= 600;
        }

        private void SelectRandomGame()
        {
            if (games.Length == 0)
                return;

            int randomIndex = random.Next(games.Length);
            if (gameSelector.SelectedIndex == randomIndex)
                LoadSelectedGame();
            else
                gameSelector.SelectedIndex = randomIndex;
        }

        private void LoadSelectedGame(bool preserveDifficulty = false)
        {
            if (!(gameSelector.SelectedItem is MiniGameDescriptor selected))
                return;

            string previousDifficulty = preserveDifficulty ? canvas.CurrentDifficultyId : null;
            descriptionLabel.Text = selected.Metadata.Description +
                (string.IsNullOrWhiteSpace(selected.Metadata.Controls) ? "" : $"  ·  조작: {selected.Metadata.Controls}");
            canvas.LoadGame(selected.GameType, CreatePalette(), exportRunning);
            if (!string.IsNullOrWhiteSpace(previousDifficulty))
                canvas.SetDifficulty(previousDifficulty);
            ConfigureDifficultySelector();
            if (!string.IsNullOrWhiteSpace(canvas.LastError))
                exportStatusLabel.Text = "게임을 불러오지 못했습니다: " + canvas.LastError;
        }

        private void ConfigureDifficultySelector()
        {
            updatingDifficulty = true;
            try
            {
                difficultySelector.Items.Clear();
                foreach (MiniGameDifficultyOption option in canvas.DifficultyOptions)
                    difficultySelector.Items.Add(option);

                difficultySelector.Visible = difficultySelector.Items.Count > 0;
                if (!difficultySelector.Visible)
                    return;

                int selectedIndex = 0;
                for (int i = 0; i < difficultySelector.Items.Count; i++)
                {
                    if (difficultySelector.Items[i] is MiniGameDifficultyOption option
                        && string.Equals(option.Id, canvas.CurrentDifficultyId, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
                difficultySelector.SelectedIndex = selectedIndex;
            }
            finally
            {
                updatingDifficulty = false;
            }
        }

        private void ApplySelectedDifficulty()
        {
            if (updatingDifficulty || !(difficultySelector.SelectedItem is MiniGameDifficultyOption option))
                return;
            canvas.SetDifficulty(option.Id);
        }

        private void UpdateScoreLabel()
        {
            scoreLabel.Text = $"Score {currentScore}  ·  Best {currentHighScore}";
        }

        private static MiniGamePalette CreatePalette() => new MiniGamePalette
        {
            Background = UiTheme.PreviewBackground,
            Surface = UiTheme.Surface,
            Border = UiTheme.Border,
            Text = UiTheme.TextPrimary,
            MutedText = UiTheme.TextMuted,
            Accent = UiTheme.Accent,
            Success = UiTheme.LogSuccess,
            Danger = UiTheme.LogError
        };

    }
}
