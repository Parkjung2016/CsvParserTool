using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CSVParserTool.MiniGames
{
    [ExportMiniGame(
        "builtin.endless-stairs",
        "끝없는 계단",
        Description = "다음 계단 방향을 빠르게 선택해 계속 올라가세요. 방향을 틀리거나 시간이 끝나면 추락합니다.",
        Controls = "←/A 왼쪽 · →/D 오른쪽 · Space/Enter 재시작",
        Order = 20)]
    public sealed class EndlessStairsMiniGame : ExportMiniGame, IConfigurableMiniGameDifficulty
    {
        private const int VisibleColumns = 7;
        private const int InitialStepCount = 24;

        // 밸런스 조절은 이 프리셋만 수정하면 됩니다. 새 프리셋은 난이도 UI에 자동 표시됩니다.
        private static readonly StairDifficultySettings[] DifficultyPresets =
        {
            new StairDifficultySettings("easy", "쉬움", "방향을 판단할 시간이 넉넉합니다.", 1.65F, 0.78F, 0.012F),
            new StairDifficultySettings("normal", "보통", "점수가 오를수록 제한 시간이 점차 짧아집니다.", 1.25F, 0.62F, 0.014F),
            new StairDifficultySettings("hard", "어려움", "처음부터 빠르며 속도가 더 빠르게 증가합니다.", 0.92F, 0.48F, 0.016F)
        };

        private readonly List<StairStep> steps = new List<StairStep>();
        private StairDifficultySettings difficulty = DifficultyPresets[1];
        private Font scoreFont;
        private Font titleFont;
        private Font bodyFont;
        private int currentIndex;
        private float remainingTime;
        private float hop;
        private float fallOffset;
        private float fallVelocity;
        private float failureDelay;
        private bool falling;
        private bool gameOver;

        public IReadOnlyList<MiniGameDifficultyOption> DifficultyOptions =>
            DifficultyPresets.Select(item => item.Option).ToArray();

        public string CurrentDifficultyId => difficulty.Option.Id;

        public void SetDifficulty(string difficultyId)
        {
            StairDifficultySettings selected = DifficultyPresets.FirstOrDefault(
                item => string.Equals(item.Option.Id, difficultyId, StringComparison.OrdinalIgnoreCase));
            if (selected == null || ReferenceEquals(selected, difficulty))
                return;

            difficulty = selected;
            ResetGame();
        }

        protected override void OnCreate()
        {
            scoreFont = new Font("Segoe UI Semibold", 22F, FontStyle.Bold, GraphicsUnit.Point);
            titleFont = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point);
            bodyFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            ResetGame();
        }

        protected override void OnKeyDown(Keys key)
        {
            if (gameOver)
            {
                if (key == Keys.Space || key == Keys.Enter || IsDirectionKey(key))
                    ResetGame();
                return;
            }

            if (falling)
                return;

            if (key == Keys.Left || key == Keys.A)
                TryClimb(-1);
            else if (key == Keys.Right || key == Keys.D)
                TryClimb(1);
        }

        protected override void OnUpdate(float deltaTime)
        {
            hop = Math.Max(0F, hop - deltaTime * 4.8F);

            if (falling)
            {
                fallVelocity += 980F * deltaTime;
                fallOffset += fallVelocity * deltaTime;
                failureDelay -= deltaTime;
                if (failureDelay <= 0F)
                {
                    falling = false;
                    gameOver = true;
                }
                return;
            }

            if (gameOver || Context.ViewportSize.Width < 280 || Context.ViewportSize.Height < 170)
                return;

            remainingTime -= deltaTime;
            if (remainingTime <= 0F)
                BeginFall();
        }

        protected override void OnDraw(Graphics graphics, Rectangle viewport)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawBackground(graphics, viewport);

            if (viewport.Width < 280 || viewport.Height < 170)
            {
                DrawCenteredMessage(graphics, viewport, "창을 조금 더 크게 늘려주세요.", Context.Palette.MutedText);
                return;
            }

            StairMetrics metrics = StairMetrics.Create(viewport);
            DrawStairs(graphics, viewport, metrics);
            DrawPlayer(graphics, metrics);
            DrawHud(graphics, viewport);

            if (falling)
                DrawFailureOverlay(graphics, viewport, "발판을 놓쳤어요!");
            else if (gameOver)
                DrawFailureOverlay(graphics, viewport, "실패  ·  Space로 다시 시작");
        }

        protected override void OnDispose()
        {
            scoreFont?.Dispose();
            titleFont?.Dispose();
            bodyFont?.Dispose();
        }

        private void TryClimb(int direction)
        {
            EnsureStepsAhead();
            if (steps[currentIndex + 1].DirectionFromPrevious != direction)
            {
                BeginFall();
                return;
            }

            currentIndex++;
            Context.SetScore(currentIndex);
            remainingTime = GetStepTime();
            hop = 1F;
            EnsureStepsAhead();
        }

        private void BeginFall()
        {
            if (falling || gameOver)
                return;

            falling = true;
            remainingTime = 0F;
            fallOffset = 0F;
            fallVelocity = -105F;
            failureDelay = 0.9F;
        }

        private void ResetGame()
        {
            steps.Clear();
            currentIndex = 0;
            falling = false;
            gameOver = false;
            fallOffset = 0F;
            fallVelocity = 0F;
            failureDelay = 0F;
            hop = 0F;

            int startColumn = VisibleColumns / 2;
            steps.Add(new StairStep(0, startColumn, 0));
            while (steps.Count < InitialStepCount)
                AddNextStep();

            remainingTime = GetStepTime();
            Context?.SetScore(0);
        }

        private void EnsureStepsAhead()
        {
            while (steps.Count <= currentIndex + InitialStepCount)
                AddNextStep();

            // 긴 Export에서도 리스트가 무한히 커지지 않도록 이미 지난 계단은 정리할 수 있는 구조를 유지한다.
            // 현재 점수는 인덱스로 사용하므로 뼈대 단계에서는 보존하고, 월드 청크 방식으로 확장할 때 교체하면 된다.
        }

        private void AddNextStep()
        {
            StairStep previous = steps[steps.Count - 1];
            int direction;
            if (previous.Column <= 0)
                direction = 1;
            else if (previous.Column >= VisibleColumns - 1)
                direction = -1;
            else
                direction = Context.Random.Next(2) == 0 ? -1 : 1;

            steps.Add(new StairStep(previous.Index + 1, previous.Column + direction, direction));
        }

        private float GetStepTime()
        {
            return Math.Max(
                difficulty.MinimumStepSeconds,
                difficulty.StartStepSeconds - currentIndex * difficulty.SpeedUpPerStep);
        }

        private void DrawBackground(Graphics graphics, Rectangle viewport)
        {
            Color top = Blend(Context.Palette.Background, Context.Palette.Accent, 0.13F);
            Color bottom = Blend(Context.Palette.Background, Context.Palette.Surface, 0.34F);
            using (var brush = new LinearGradientBrush(viewport, top, bottom, LinearGradientMode.Vertical))
                graphics.FillRectangle(brush, viewport);

            using (var cloudBrush = new SolidBrush(Color.FromArgb(24, Context.Palette.Text)))
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = (viewport.Width * (i + 1) / 6F) - 42F;
                    float y = 62F + (i % 2) * 44F;
                    graphics.FillEllipse(cloudBrush, x, y, 84F, 22F);
                }
            }
        }

        private void DrawStairs(Graphics graphics, Rectangle viewport, StairMetrics metrics)
        {
            int first = Math.Max(0, currentIndex - 3);
            int last = Math.Min(steps.Count - 1, currentIndex + metrics.VisibleStepCount);
            for (int i = last; i >= first; i--)
            {
                StairStep step = steps[i];
                PointF center = GetStepCenter(step, metrics);
                if (center.Y < -metrics.StepHeight || center.Y > viewport.Height + metrics.StepHeight)
                    continue;

                bool active = i == currentIndex;
                Color topColor = active
                    ? Context.Palette.Accent
                    : Blend(Context.Palette.Surface, Context.Palette.Accent, 0.18F);
                Color sideColor = Blend(topColor, Color.Black, 0.24F);
                var top = new RectangleF(
                    center.X - metrics.StepWidth * 0.5F,
                    center.Y - metrics.StepHeight,
                    metrics.StepWidth,
                    metrics.StepThickness);
                PointF[] side =
                {
                    new PointF(top.Left, top.Bottom),
                    new PointF(top.Right, top.Bottom),
                    new PointF(top.Right - 7F, top.Bottom + metrics.StepThickness),
                    new PointF(top.Left + 7F, top.Bottom + metrics.StepThickness)
                };

                using (var sideBrush = new SolidBrush(sideColor))
                using (var topBrush = new SolidBrush(topColor))
                using (var outline = new Pen(Color.FromArgb(110, Context.Palette.Border), 1F))
                {
                    graphics.FillPolygon(sideBrush, side);
                    graphics.FillRectangle(topBrush, top);
                    graphics.DrawRectangle(outline, top.X, top.Y, top.Width, top.Height);
                }
            }
        }

        private void DrawPlayer(Graphics graphics, StairMetrics metrics)
        {
            StairStep step = steps[currentIndex];
            PointF center = GetStepCenter(step, metrics);
            float bounce = (float)Math.Sin(hop * Math.PI) * 15F;
            float x = center.X;
            float y = center.Y - metrics.StepHeight - 21F - bounce + fallOffset;

            using (var shadow = new SolidBrush(Color.FromArgb(42, Color.Black)))
                graphics.FillEllipse(shadow, center.X - 18F, center.Y - metrics.StepHeight - 4F, 36F, 9F);
            using (var body = new SolidBrush(Context.Palette.Accent))
            using (var outline = new Pen(Blend(Context.Palette.Accent, Color.Black, 0.35F), 2F))
            {
                graphics.FillEllipse(body, x - 20F, y - 20F, 40F, 40F);
                graphics.DrawEllipse(outline, x - 20F, y - 20F, 40F, 40F);
                graphics.DrawLine(outline, x - 10F, y - 16F, x - 14F, y - 27F);
                graphics.DrawLine(outline, x + 10F, y - 16F, x + 14F, y - 27F);
            }

            using (var eye = new SolidBrush(Color.White))
            using (var pupil = new SolidBrush(Color.FromArgb(35, 38, 48)))
            {
                graphics.FillEllipse(eye, x - 11F, y - 7F, 9F, 11F);
                graphics.FillEllipse(eye, x + 2F, y - 7F, 9F, 11F);
                graphics.FillEllipse(pupil, x - 7F, y - 3F, 4F, 5F);
                graphics.FillEllipse(pupil, x + 6F, y - 3F, 4F, 5F);
            }
        }

        private void DrawHud(Graphics graphics, Rectangle viewport)
        {
            float total = GetStepTime();
            float ratio = total <= 0F ? 0F : Math.Max(0F, Math.Min(1F, remainingTime / total));
            using (var textBrush = new SolidBrush(Context.Palette.Text))
            using (var mutedBrush = new SolidBrush(Context.Palette.MutedText))
            using (var barBack = new SolidBrush(Color.FromArgb(58, Context.Palette.Text)))
            using (var barFill = new SolidBrush(ratio < 0.28F ? Context.Palette.Danger : Context.Palette.Success))
            {
                graphics.DrawString($"{currentIndex}", scoreFont, textBrush, 18F, 12F);
                graphics.DrawString("STEPS", bodyFont, mutedBrush, 21F, 47F);
                graphics.DrawString($"BEST  {Context.HighScore}", bodyFont, mutedBrush, 21F, 65F);

                float barWidth = Math.Max(90F, Math.Min(220F, viewport.Width * 0.34F));
                float left = viewport.Width - barWidth - 20F;
                graphics.DrawString("NEXT STEP", bodyFont, mutedBrush, left, 18F);
                graphics.FillRectangle(barBack, left, 40F, barWidth, 8F);
                graphics.FillRectangle(barFill, left, 40F, barWidth * ratio, 8F);

                if (!falling && !gameOver && currentIndex + 1 < steps.Count)
                {
                    string arrow = steps[currentIndex + 1].DirectionFromPrevious < 0 ? "←" : "→";
                    SizeF arrowSize = graphics.MeasureString(arrow, titleFont);
                    graphics.DrawString(arrow, titleFont, textBrush, viewport.Width * 0.5F - arrowSize.Width * 0.5F, 16F);
                }
            }
        }

        private void DrawFailureOverlay(Graphics graphics, Rectangle viewport, string message)
        {
            using (var overlay = new SolidBrush(Color.FromArgb(gameOver ? 138 : 72, Context.Palette.Danger)))
                graphics.FillRectangle(overlay, viewport);
            DrawCenteredMessage(graphics, viewport, message, Color.White);
        }

        private void DrawCenteredMessage(Graphics graphics, Rectangle viewport, string message, Color color)
        {
            using (var brush = new SolidBrush(color))
            {
                SizeF size = graphics.MeasureString(message, titleFont);
                graphics.DrawString(message, titleFont, brush,
                    viewport.Left + (viewport.Width - size.Width) * 0.5F,
                    viewport.Top + (viewport.Height - size.Height) * 0.5F);
            }
        }

        private PointF GetStepCenter(StairStep step, StairMetrics metrics)
        {
            float x = metrics.CenterX + (step.Column - (VisibleColumns - 1) * 0.5F) * metrics.ColumnWidth;
            float y = metrics.BaseY - (step.Index - currentIndex) * metrics.StepRise;
            return new PointF(x, y);
        }

        private static bool IsDirectionKey(Keys key) =>
            key == Keys.Left || key == Keys.Right || key == Keys.A || key == Keys.D;

        private static Color Blend(Color first, Color second, float amount)
        {
            amount = Math.Max(0F, Math.Min(1F, amount));
            return Color.FromArgb(
                (int)(first.A + (second.A - first.A) * amount),
                (int)(first.R + (second.R - first.R) * amount),
                (int)(first.G + (second.G - first.G) * amount),
                (int)(first.B + (second.B - first.B) * amount));
        }

        private sealed class StairDifficultySettings
        {
            public StairDifficultySettings(
                string id,
                string displayName,
                string description,
                float startStepSeconds,
                float minimumStepSeconds,
                float speedUpPerStep)
            {
                Option = new MiniGameDifficultyOption(id, displayName, description);
                StartStepSeconds = startStepSeconds;
                MinimumStepSeconds = minimumStepSeconds;
                SpeedUpPerStep = speedUpPerStep;
            }

            public MiniGameDifficultyOption Option { get; }
            public float StartStepSeconds { get; }
            public float MinimumStepSeconds { get; }
            public float SpeedUpPerStep { get; }
        }

        private sealed class StairStep
        {
            public StairStep(int index, int column, int directionFromPrevious)
            {
                Index = index;
                Column = column;
                DirectionFromPrevious = directionFromPrevious;
            }

            public int Index { get; }
            public int Column { get; }
            public int DirectionFromPrevious { get; }
        }

        private sealed class StairMetrics
        {
            public float CenterX { get; private set; }
            public float BaseY { get; private set; }
            public float ColumnWidth { get; private set; }
            public float StepWidth { get; private set; }
            public float StepHeight { get; private set; }
            public float StepThickness { get; private set; }
            public float StepRise { get; private set; }
            public int VisibleStepCount { get; private set; }

            public static StairMetrics Create(Rectangle viewport)
            {
                float columnWidth = Math.Max(34F, Math.Min(68F, (viewport.Width - 48F) / VisibleColumns));
                float rise = Math.Max(27F, Math.Min(44F, viewport.Height * 0.095F));
                return new StairMetrics
                {
                    CenterX = viewport.Left + viewport.Width * 0.5F,
                    BaseY = viewport.Bottom - 30F,
                    ColumnWidth = columnWidth,
                    StepWidth = columnWidth * 0.92F,
                    StepHeight = Math.Max(22F, rise * 0.78F),
                    StepThickness = Math.Max(7F, rise * 0.22F),
                    StepRise = rise,
                    VisibleStepCount = Math.Max(5, (int)(viewport.Height / rise) + 2)
                };
            }
        }
    }
}
