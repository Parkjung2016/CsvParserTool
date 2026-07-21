using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CSVParserTool.MiniGames
{
    [ExportMiniGame(
        "builtin.core-click",
        "코어 클릭",
        Description = "움직이는 코어를 눌러 점수를 올리는 가벼운 게임입니다.",
        Controls = "마우스 클릭",
        Order = 0)]
    public sealed class CoreClickMiniGame : ExportMiniGame
    {
        private PointF target;
        private float targetRadius = 30F;
        private float moveTimer;
        private float gameTimer = 7F;
        private float pulse;
        private bool isGameEnd;
        private Bitmap gameEndBackdrop;

        protected override void OnCreate()
        {
            Context.SetScore(0);
            MoveTarget();
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (isGameEnd)
                return;

            moveTimer += deltaTime;
            gameTimer -= deltaTime;
            pulse += deltaTime * 4F;
            if (moveTimer >= 1.25F)
                MoveTarget();

            if (gameTimer <= 0F)
                GameEnd();
        }

        protected override void OnResize(Size size)
        {
            KeepTargetInside(size);
            DisposeGameEndBackdrop();
        }

        protected override void OnMouseDown(MouseButtons button, Point position)
        {
            if (button != MouseButtons.Left)
                return;

            if (isGameEnd)
            {
                RestartGame();
                return;
            }

            float dx = position.X - target.X;
            float dy = position.Y - target.Y;
            if ((dx * dx) + (dy * dy) <= targetRadius * targetRadius)
            {
                Context.AddScore();
                MoveTarget();
            }
        }

        protected override void OnDraw(Graphics graphics, Rectangle viewport)
        {
            if (isGameEnd)
            {
                DrawBlurredGameEnd(graphics, viewport);
                return;
            }

            DrawScene(graphics, viewport);
        }

        protected override void OnDispose() => DisposeGameEndBackdrop();

        private void DrawScene(Graphics graphics, Rectangle viewport)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawGrid(graphics, viewport);

            float radius = targetRadius + (float)Math.Sin(pulse) * 3F;
            var glow = new RectangleF(target.X - radius - 8F, target.Y - radius - 8F, (radius + 8F) * 2F, (radius + 8F) * 2F);
            var core = new RectangleF(target.X - radius, target.Y - radius, radius * 2F, radius * 2F);
            using (var glowBrush = new SolidBrush(Color.FromArgb(45, Context.Palette.Accent)))
            using (var coreBrush = new SolidBrush(Context.Palette.Accent))
            using (var highlightBrush = new SolidBrush(Color.FromArgb(150, Color.White)))
            {
                graphics.FillEllipse(glowBrush, glow);
                graphics.FillEllipse(coreBrush, core);
                graphics.FillEllipse(highlightBrush, target.X - radius * 0.42F, target.Y - radius * 0.48F, radius * 0.38F, radius * 0.32F);
            }

            using (var titleFont = new Font("Segoe UI Semibold", 14F))
            using (var bodyFont = new Font("Segoe UI", 9F))
            using (var textBrush = new SolidBrush(Context.Palette.Text))
            using (var mutedBrush = new SolidBrush(Context.Palette.MutedText))
            {
                graphics.DrawString($"Game Timer  {Math.Round(Math.Max(0F, gameTimer), 2)}", titleFont, textBrush, 18F, 16F);
                graphics.DrawString($"SCORE  {Context.Score}    BEST  {Context.HighScore}", titleFont, textBrush, 18F, 35F);
                graphics.DrawString(
                    Context.IsExportRunning ? "Export 중 · 코어를 클릭하세요" : "Export 완료 · 계속 플레이할 수 있어요",
                    bodyFont,
                    mutedBrush,
                    20F,
                    66F);
            }

            if (isGameEnd)
            {

            }
        }

        private void DrawBlurredGameEnd(Graphics graphics, Rectangle viewport)
        {
            if (viewport.Width <= 0 || viewport.Height <= 0)
                return;

            if (gameEndBackdrop == null)
                gameEndBackdrop = CreateBlurredBackdrop(viewport.Size);

            InterpolationMode previousInterpolation = graphics.InterpolationMode;
            PixelOffsetMode previousPixelOffset = graphics.PixelOffsetMode;
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(gameEndBackdrop, viewport);
            graphics.InterpolationMode = previousInterpolation;
            graphics.PixelOffsetMode = previousPixelOffset;

            using (var shade = new SolidBrush(Color.FromArgb(105, Context.Palette.Background)))
                graphics.FillRectangle(shade, viewport);

            DrawGameEndText(graphics, viewport);
        }

        private Bitmap CreateBlurredBackdrop(Size viewport)
        {
            int width = Math.Max(1, viewport.Width / 12);
            int height = Math.Max(1, viewport.Height / 12);
            var reduced = new Bitmap(width, height);
            using (Graphics buffer = Graphics.FromImage(reduced))
            {
                buffer.Clear(Context.Palette.Background);
                buffer.ScaleTransform(width / (float)viewport.Width, height / (float)viewport.Height);
                DrawScene(buffer, new Rectangle(Point.Empty, viewport));
            }
            return reduced;
        }

        private void DrawGameEndText(Graphics graphics, Rectangle viewport)
        {
            using (var titleFont = new Font("Segoe UI Semibold", 24F, FontStyle.Bold))
            using (var scoreFont = new Font("Segoe UI Semibold", 13F))
            using (var bodyFont = new Font("Segoe UI", 9F))
            using (var textBrush = new SolidBrush(Context.Palette.Text))
            using (var mutedBrush = new SolidBrush(Context.Palette.MutedText))
            {
                DrawCentered(graphics, viewport, "TIME UP", titleFont, textBrush, -35F);
                DrawCentered(graphics, viewport, $"SCORE {Context.Score}   ·   BEST {Context.HighScore}", scoreFont, textBrush, 5F);
                DrawCentered(graphics, viewport, "클릭해서 다시 시작", bodyFont, mutedBrush, 37F);
            }
        }

        private static void DrawCentered(Graphics graphics, Rectangle viewport, string text, Font font, Brush brush, float offsetY)
        {
            SizeF size = graphics.MeasureString(text, font);
            graphics.DrawString(text, font, brush,
                viewport.Left + (viewport.Width - size.Width) * 0.5F,
                viewport.Top + (viewport.Height - size.Height) * 0.5F + offsetY);
        }

        private void DrawGrid(Graphics graphics, Rectangle viewport)
        {
            using (var pen = new Pen(Color.FromArgb(25, Context.Palette.Border)))
            {
                for (int x = 0; x < viewport.Width; x += 28)
                    graphics.DrawLine(pen, x, 0, x, viewport.Height);
                for (int y = 0; y < viewport.Height; y += 28)
                    graphics.DrawLine(pen, 0, y, viewport.Width, y);
            }
        }

        private void MoveTarget()
        {
            moveTimer = 0F;
            Size size = Context.ViewportSize;
            int minX = 55;
            int minY = 90;
            int maxX = Math.Max(minX + 1, size.Width - 55);
            int maxY = Math.Max(minY + 1, size.Height - 55);
            target = new PointF(Context.Random.Next(minX, maxX), Context.Random.Next(minY, maxY));
        }

        private void KeepTargetInside(Size size)
        {
            target.X = Math.Max(45F, Math.Min(Math.Max(45F, size.Width - 45F), target.X));
            target.Y = Math.Max(80F, Math.Min(Math.Max(80F, size.Height - 45F), target.Y));
        }

        private void GameEnd()
        {
            isGameEnd = true;
            gameTimer = 0F;
            DisposeGameEndBackdrop();
        }

        private void RestartGame()
        {
            Context.SetScore(0);
            gameTimer = 7F;
            isGameEnd = false;
            pulse = 0F;
            DisposeGameEndBackdrop();
            MoveTarget();
        }

        private void DisposeGameEndBackdrop()
        {
            gameEndBackdrop?.Dispose();
            gameEndBackdrop = null;
        }
    }
}
