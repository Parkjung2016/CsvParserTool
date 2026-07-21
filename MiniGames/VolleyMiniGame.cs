using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CSVParserTool.MiniGames
{
    [ExportMiniGame(
        "builtin.volley",
        "배구",
        Description = "AI와 겨루는 2D 아케이드 배구입니다. 7점을 먼저 얻으면 승리합니다.",
        Controls = "← → 또는 A D 이동 · ↑/W/Space 점프",
        Order = 10)]
    public sealed class VolleyMiniGame : ExportMiniGame, IConfigurableMiniGameDifficulty
    {
        private const float Gravity = 900F;
        private const float BallGravity = 650F;
        private const float PlayerMoveSpeed = 235F;
        private const float PlayerJumpSpeed = 435F;
        private const float PlayerDashSpeed = 250F;
        private const float ActorRadius = 27F;
        private const float BallRadius = 15F;
        private const float NetWidth = 10F;
        private const float MinimumPlayableHeight = 170F;
        private const int WinningScore = 7;

        // 난이도 조정은 이 프리셋만 수정하면 된다. 이후 프리셋을 추가해도 UI에 자동 표시된다.
        private static readonly VolleyDifficultySettings[] DifficultyPresets =
        {
            new VolleyDifficultySettings(
                "easy", "쉬움", "AI가 늦게 반응하고 예측 오차가 큽니다.",
                aiMoveSpeed: 178F, reactionSeconds: 0.22F, predictionError: 62F,
                jumpDistance: 68F, jumpChance: 0.42F, hitBoost: 25F),
            new VolleyDifficultySettings(
                "normal", "보통", "AI가 공의 낙하지점을 적당히 예측합니다.",
                aiMoveSpeed: 215F, reactionSeconds: 0.12F, predictionError: 32F,
                jumpDistance: 86F, jumpChance: 0.72F, hitBoost: 48F),
            new VolleyDifficultySettings(
                "hard", "어려움", "AI가 빠르게 예측하고 적극적으로 점프합니다.",
                aiMoveSpeed: 252F, reactionSeconds: 0.055F, predictionError: 12F,
                jumpDistance: 108F, jumpChance: 0.94F, hitBoost: 72F)
        };

        private readonly Actor player = new Actor();
        private readonly Actor ai = new Actor();
        private VolleyDifficultySettings difficulty = DifficultyPresets[2];
        private Font scoreFont;
        private Font titleFont;
        private Font bodyFont;
        private float ballX;
        private float ballY;
        private float ballVelocityX;
        private float ballVelocityY;
        private float groundY;
        private float netX;
        private float netHeight = 118F;
        private float serveCountdown;
        private float matchResetCountdown;
        private float aiReactionCountdown;
        private float aiTargetX;
        private bool ballActive;
        private bool playerServes = true;
        private bool playerJumpQueued;
        private bool playerDashQueued;
        private int playerScore;
        private int aiScore;
        private string banner = "";

        public IReadOnlyList<MiniGameDifficultyOption> DifficultyOptions =>
            DifficultyPresets.Select(preset => preset.Option).ToArray();

        public string CurrentDifficultyId => difficulty.Option.Id;

        public void SetDifficulty(string difficultyId)
        {
            VolleyDifficultySettings selected = DifficultyPresets.FirstOrDefault(
                preset => string.Equals(preset.Option.Id, difficultyId, StringComparison.OrdinalIgnoreCase));
            if (selected == null || ReferenceEquals(selected, difficulty))
                return;

            difficulty = selected;
            ResetMatch();
        }

        protected override void OnCreate()
        {
            scoreFont = new Font("Segoe UI Semibold", 22F, FontStyle.Bold, GraphicsUnit.Point);
            titleFont = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point);
            bodyFont = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            ResetMatch();
        }

        protected override void OnResize(Size size)
        {
            UpdateCourt(size);
            if (size.Width > 0 && size.Height > 0)
            {
                ClampActor(player, leftSide: true);
                ClampActor(ai, leftSide: false);
                ClampBallToCourt(size);
            }
        }

        protected override void OnKeyDown(Keys key)
        {
            if (key == Keys.Up || key == Keys.W || key == Keys.Space)
                playerJumpQueued = true;

            if (key == Keys.Shift)
                playerDashQueued = true;

        }

        protected override void OnUpdate(float deltaTime)
        {
            Size viewport = Context.ViewportSize;
            if (viewport.Width < 280 || viewport.Height < MinimumPlayableHeight)
                return;

            UpdateCourt(viewport);
            int steps = Math.Max(1, (int)Math.Ceiling(deltaTime / (1F / 120F)));
            float step = deltaTime / steps;
            for (int i = 0; i < steps; i++)
                Simulate(step);
        }

        protected override void OnDraw(Graphics graphics, Rectangle viewport)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawCourt(graphics, viewport);
            DrawActor(graphics, player, Context.Palette.Accent, "YOU", facingLeft: false);
            DrawActor(graphics, ai, Context.Palette.Danger, "AI", facingLeft: true);
            DrawBall(graphics);
            DrawHud(graphics, viewport);
        }

        protected override void OnDispose()
        {
            scoreFont?.Dispose();
            titleFont?.Dispose();
            bodyFont?.Dispose();
        }

        private void Simulate(float deltaTime)
        {
            if (matchResetCountdown > 0F)
            {
                matchResetCountdown -= deltaTime;
                if (matchResetCountdown <= 0F)
                    ResetMatch();
                return;
            }

            UpdatePlayer(deltaTime);
            UpdateAi(deltaTime);
            UpdateActorPhysics(player, deltaTime, leftSide: true);
            UpdateActorPhysics(ai, deltaTime, leftSide: false);

            if (!ballActive)
            {
                Actor server = playerServes ? player : ai;
                ballX = server.X + (playerServes ? 18F : -18F);
                ballY = server.Y - ActorRadius - BallRadius - 8F;
                serveCountdown -= deltaTime;
                if (serveCountdown <= 0F)
                {
                    ballActive = true;
                    ballVelocityX = playerServes ? 125F : -125F;
                    ballVelocityY = -315F;
                    banner = "";
                }
                return;
            }

            ballVelocityY += BallGravity * deltaTime;
            ballX += ballVelocityX * deltaTime;
            ballY += ballVelocityY * deltaTime;

            ResolveWallCollision();
            ResolveNetCollision();
            ResolveActorCollision(player, extraBoost: 25F);
            ResolveActorCollision(ai, extraBoost: difficulty.HitBoost);

            if (ballY + BallRadius >= groundY)
            {
                bool landedOnPlayerSide = ballX < netX;
                AwardPoint(playerWon: !landedOnPlayerSide);
            }
        }

        private void UpdatePlayer(float deltaTime)
        {
            int direction = 0;
            if (Context.Input.IsKeyDown(Keys.Left) || Context.Input.IsKeyDown(Keys.A)) direction--;
            if (Context.Input.IsKeyDown(Keys.Right) || Context.Input.IsKeyDown(Keys.D)) direction++;
            float targetVelocity = direction * PlayerMoveSpeed;
            player.VelocityX = MoveTowards(player.VelocityX, targetVelocity, 1100F * deltaTime);

            if (playerJumpQueued)
            {
                if (player.Grounded)
                {
                    player.VelocityY = -PlayerJumpSpeed;
                    player.Grounded = false;
                }
                playerJumpQueued = false;
            }
            if (playerDashQueued)
            {
                player.VelocityX = PlayerDashSpeed;
                playerDashQueued = false;
            }
        }

        private void UpdateAi(float deltaTime)
        {
            aiReactionCountdown -= deltaTime;
            if (aiReactionCountdown <= 0F)
            {
                aiReactionCountdown = difficulty.ReactionSeconds;
                float defensivePosition = Context.ViewportSize.Width * 0.76F;
                bool threatening = ballActive && (ballX >= netX || ballVelocityX > 0F);
                aiTargetX = threatening ? PredictLandingX() : defensivePosition;
                aiTargetX += RandomRange(-difficulty.PredictionError, difficulty.PredictionError);

                float horizontalDistance = Math.Abs(ballX - ai.X);
                bool ballReachable = ballActive
                    && ballX > netX - 25F
                    && horizontalDistance <= difficulty.JumpDistance
                    && ballY < ai.Y + 4F
                    && ballY > ai.Y - 155F;
                if (ai.Grounded && ballReachable && Context.Random.NextDouble() <= difficulty.JumpChance)
                {
                    ai.VelocityY = -PlayerJumpSpeed * 0.98F;
                    ai.Grounded = false;
                }
            }

            float difference = aiTargetX - ai.X;
            float direction = Math.Abs(difference) < 7F ? 0F : Math.Sign(difference);
            float targetVelocity = direction * difficulty.AiMoveSpeed;
            ai.VelocityX = MoveTowards(ai.VelocityX, targetVelocity, 1250F * deltaTime);
        }

        private void UpdateActorPhysics(Actor actor, float deltaTime, bool leftSide)
        {
            actor.VelocityY += Gravity * deltaTime;
            actor.X += actor.VelocityX * deltaTime;
            actor.Y += actor.VelocityY * deltaTime;
            ClampActor(actor, leftSide);

            float standingY = groundY - ActorRadius;
            if (actor.Y >= standingY)
            {
                actor.Y = standingY;
                actor.VelocityY = 0F;
                actor.Grounded = true;
            }
        }

        private void ClampActor(Actor actor, bool leftSide)
        {
            float min = leftSide ? ActorRadius + 4F : netX + NetWidth * 0.5F + ActorRadius + 3F;
            float max = leftSide ? netX - NetWidth * 0.5F - ActorRadius - 3F : Context.ViewportSize.Width - ActorRadius - 4F;
            if (max < min) max = min;
            actor.X = Math.Max(min, Math.Min(max, actor.X));
            if ((actor.X <= min && actor.VelocityX < 0F) || (actor.X >= max && actor.VelocityX > 0F))
                actor.VelocityX = 0F;
        }

        private float PredictLandingX()
        {
            if (!ballActive)
                return Context.ViewportSize.Width * 0.76F;

            float targetY = groundY - ActorRadius - BallRadius;
            float a = BallGravity * 0.5F;
            float b = ballVelocityY;
            float c = ballY - targetY;
            float discriminant = b * b - 4F * a * c;
            float time = 0.25F;
            if (discriminant >= 0F)
            {
                float root = (float)Math.Sqrt(discriminant);
                float first = (-b + root) / (2F * a);
                float second = (-b - root) / (2F * a);
                time = Math.Max(first, second);
                if (time < 0F) time = 0.25F;
            }

            float predicted = ballX + ballVelocityX * Math.Min(1.8F, time);
            float min = netX + NetWidth * 0.5F + ActorRadius;
            float max = Context.ViewportSize.Width - ActorRadius;
            while (predicted > max || predicted < min)
            {
                if (predicted > max) predicted = max - (predicted - max);
                if (predicted < min) predicted = min + (min - predicted);
            }
            return predicted;
        }

        private void ResolveWallCollision()
        {
            float min = BallRadius;
            float max = Context.ViewportSize.Width - BallRadius;
            if (ballX < min)
            {
                ballX = min;
                ballVelocityX = Math.Abs(ballVelocityX) * 0.94F;
            }
            else if (ballX > max)
            {
                ballX = max;
                ballVelocityX = -Math.Abs(ballVelocityX) * 0.94F;
            }

            if (ballY < BallRadius)
            {
                ballY = BallRadius;
                ballVelocityY = Math.Abs(ballVelocityY) * 0.9F;
            }
        }

        private void ResolveNetCollision()
        {
            float netTop = groundY - netHeight;
            float halfWidth = NetWidth * 0.5F;
            bool overlapsX = ballX + BallRadius > netX - halfWidth && ballX - BallRadius < netX + halfWidth;
            bool overlapsY = ballY + BallRadius > netTop && ballY - BallRadius < groundY;
            if (!overlapsX || !overlapsY)
                return;

            if (ballY < netTop && ballVelocityY > 0F)
            {
                ballY = netTop - BallRadius;
                ballVelocityY = -Math.Abs(ballVelocityY) * 0.82F;
                ballVelocityX += ballX < netX ? -24F : 24F;
            }
            else if (ballX < netX)
            {
                ballX = netX - halfWidth - BallRadius;
                ballVelocityX = -Math.Abs(ballVelocityX) * 0.9F;
            }
            else
            {
                ballX = netX + halfWidth + BallRadius;
                ballVelocityX = Math.Abs(ballVelocityX) * 0.9F;
            }
        }

        private void ResolveActorCollision(Actor actor, float extraBoost)
        {
            float dx = ballX - actor.X;
            float dy = ballY - actor.Y;
            float minimumDistance = BallRadius + ActorRadius;
            float distanceSquared = dx * dx + dy * dy;
            if (distanceSquared >= minimumDistance * minimumDistance)
                return;

            float distance = (float)Math.Sqrt(Math.Max(0.001F, distanceSquared));
            float normalX = dx / distance;
            float normalY = dy / distance;
            float overlap = minimumDistance - distance;
            ballX += normalX * overlap;
            ballY += normalY * overlap;

            float relativeX = ballVelocityX - actor.VelocityX;
            float relativeY = ballVelocityY - actor.VelocityY;
            float closingSpeed = relativeX * normalX + relativeY * normalY;
            if (closingSpeed < 0F)
            {
                float impulse = -closingSpeed * 1.72F + 42F;
                ballVelocityX += normalX * impulse + actor.VelocityX * 0.22F;
                ballVelocityY += normalY * impulse + Math.Min(0F, actor.VelocityY) * 0.28F;
            }

            ballVelocityY -= extraBoost;
            LimitBallSpeed();
        }

        private void LimitBallSpeed()
        {
            float speed = (float)Math.Sqrt(ballVelocityX * ballVelocityX + ballVelocityY * ballVelocityY);
            const float maximum = 610F;
            if (speed <= maximum || speed <= 0.001F)
                return;
            float scale = maximum / speed;
            ballVelocityX *= scale;
            ballVelocityY *= scale;
        }

        private void AwardPoint(bool playerWon)
        {
            ballActive = false;
            if (playerWon)
            {
                playerScore++;
                Context.SetScore(playerScore);
                banner = "POINT!";
            }
            else
            {
                aiScore++;
                banner = "AI POINT";
            }

            if (playerScore >= WinningScore || aiScore >= WinningScore)
            {
                banner = playerScore >= WinningScore ? "YOU WIN!" : "AI WINS";
                matchResetCountdown = 2.4F;
                return;
            }

            playerServes = !playerWon;
            PrepareRound();
        }

        private void ResetMatch()
        {
            playerScore = 0;
            aiScore = 0;
            Context?.SetScore(0);
            playerServes = Context == null || Context.Random.Next(2) == 0;
            matchResetCountdown = 0F;
            banner = playerServes ? "YOUR SERVE" : "AI SERVE";
            PrepareRound();
        }

        private void PrepareRound()
        {
            UpdateCourt(Context?.ViewportSize ?? Size.Empty);
            player.X = Math.Max(ActorRadius + 4F, netX * 0.48F);
            ai.X = Math.Min(Math.Max(netX + ActorRadius + 10F, Context.ViewportSize.Width * 0.76F), Context.ViewportSize.Width - ActorRadius - 4F);
            player.Y = ai.Y = groundY - ActorRadius;
            player.VelocityX = player.VelocityY = 0F;
            ai.VelocityX = ai.VelocityY = 0F;
            player.Grounded = ai.Grounded = true;
            aiTargetX = ai.X;
            aiReactionCountdown = 0F;
            serveCountdown = 0.85F;
            ballActive = false;
            playerJumpQueued = false;
        }

        private void UpdateCourt(Size viewport)
        {
            netX = viewport.Width * 0.5F;
            if (viewport.Height <= 0)
            {
                groundY = 0F;
                netHeight = 54F;
                return;
            }

            float bottomMargin = Math.Max(24F, Math.Min(44F, viewport.Height * 0.12F));
            groundY = Math.Max(ActorRadius * 2F + 48F, viewport.Height - bottomMargin);
            groundY = Math.Min(viewport.Height - 4F, groundY);
            netHeight = Math.Max(48F, Math.Min(118F, groundY - 62F));
        }

        private void ClampBallToCourt(Size viewport)
        {
            if (viewport.Width <= 0 || viewport.Height <= 0)
                return;

            float maxX = Math.Max(BallRadius, viewport.Width - BallRadius);
            float maxY = Math.Max(BallRadius, groundY - BallRadius);
            ballX = Math.Max(BallRadius, Math.Min(maxX, ballX));
            ballY = Math.Max(BallRadius, Math.Min(maxY, ballY));
        }

        private void DrawCourt(Graphics graphics, Rectangle viewport)
        {
            Color top = Blend(Context.Palette.Background, Context.Palette.Accent, 0.10F);
            Color bottom = Blend(Context.Palette.Background, Context.Palette.Surface, 0.28F);
            using (var background = new LinearGradientBrush(viewport, top, bottom, LinearGradientMode.Vertical))
                graphics.FillRectangle(background, viewport);

            using (var linePen = new Pen(Color.FromArgb(48, Context.Palette.Border), 1F))
            {
                for (int y = 72; y < groundY; y += 34)
                    graphics.DrawLine(linePen, 0, y, viewport.Width, y);
            }

            using (var floorBrush = new SolidBrush(Blend(Context.Palette.Surface, Context.Palette.Accent, 0.14F)))
                graphics.FillRectangle(floorBrush, 0, groundY, viewport.Width, Math.Max(0, viewport.Height - groundY));
            using (var floorPen = new Pen(Context.Palette.Border, 2F))
                graphics.DrawLine(floorPen, 0, groundY, viewport.Width, groundY);

            float netTop = groundY - netHeight;
            using (var netBrush = new SolidBrush(Context.Palette.Text))
                graphics.FillRectangle(netBrush, netX - NetWidth * 0.5F, netTop, NetWidth, netHeight);
            using (var capBrush = new SolidBrush(Context.Palette.Accent))
                graphics.FillEllipse(capBrush, netX - 9F, netTop - 6F, 18F, 12F);
            using (var meshPen = new Pen(Color.FromArgb(100, Context.Palette.MutedText), 1F))
            {
                for (float y = netTop + 12F; y < groundY; y += 14F)
                    graphics.DrawLine(meshPen, netX - 18F, y, netX + 18F, y);
                graphics.DrawLine(meshPen, netX - 18F, netTop, netX - 18F, groundY);
                graphics.DrawLine(meshPen, netX + 18F, netTop, netX + 18F, groundY);
            }
        }

        private void DrawActor(Graphics graphics, Actor actor, Color color, string label, bool facingLeft)
        {
            float left = actor.X - ActorRadius;
            float top = actor.Y - ActorRadius;
            using (var shadow = new SolidBrush(Color.FromArgb(45, Color.Black)))
                graphics.FillEllipse(shadow, actor.X - 29F, groundY - 9F, 58F, 13F);
            using (var body = new SolidBrush(color))
            using (var outline = new Pen(Blend(color, Color.Black, 0.30F), 2F))
            {
                graphics.FillEllipse(body, left, top, ActorRadius * 2F, ActorRadius * 2F);
                graphics.DrawEllipse(outline, left, top, ActorRadius * 2F, ActorRadius * 2F);
                graphics.DrawLine(outline, actor.X - 12F, top + 5F, actor.X - 17F, top - 9F);
                graphics.DrawLine(outline, actor.X + 12F, top + 5F, actor.X + 17F, top - 9F);
            }

            float eyeOffset = facingLeft ? -6F : 6F;
            using (var eye = new SolidBrush(Color.White))
            using (var pupil = new SolidBrush(Color.FromArgb(30, 35, 45)))
            {
                graphics.FillEllipse(eye, actor.X + eyeOffset - 7F, actor.Y - 9F, 10F, 12F);
                graphics.FillEllipse(eye, actor.X + eyeOffset + 4F, actor.Y - 9F, 10F, 12F);
                graphics.FillEllipse(pupil, actor.X + eyeOffset - 3F, actor.Y - 5F, 4F, 5F);
                graphics.FillEllipse(pupil, actor.X + eyeOffset + 8F, actor.Y - 5F, 4F, 5F);
            }

            using (var labelBrush = new SolidBrush(Context.Palette.Text))
            {
                SizeF size = graphics.MeasureString(label, bodyFont);
                graphics.DrawString(label, bodyFont, labelBrush, actor.X - size.Width * 0.5F, actor.Y + ActorRadius + 3F);
            }
        }

        private void DrawBall(Graphics graphics)
        {
            using (var shadow = new SolidBrush(Color.FromArgb(38, Color.Black)))
                graphics.FillEllipse(shadow, ballX - 13F, groundY - 6F, 26F, 8F);
            using (var ballBrush = new SolidBrush(Color.FromArgb(255, 208, 72)))
            using (var outline = new Pen(Color.FromArgb(170, 115, 28), 2F))
            {
                graphics.FillEllipse(ballBrush, ballX - BallRadius, ballY - BallRadius, BallRadius * 2F, BallRadius * 2F);
                graphics.DrawEllipse(outline, ballX - BallRadius, ballY - BallRadius, BallRadius * 2F, BallRadius * 2F);
                graphics.DrawArc(outline, ballX - 10F, ballY - 13F, 20F, 26F, -70F, 140F);
                graphics.DrawArc(outline, ballX - 13F, ballY - 7F, 26F, 14F, 20F, 140F);
            }
        }

        private void DrawHud(Graphics graphics, Rectangle viewport)
        {
            string score = $"{playerScore}   :   {aiScore}";
            SizeF scoreSize = graphics.MeasureString(score, scoreFont);
            using (var scoreBrush = new SolidBrush(Context.Palette.Text))
                graphics.DrawString(score, scoreFont, scoreBrush, (viewport.Width - scoreSize.Width) * 0.5F, 11F);

            string difficultyText = "난이도  " + difficulty.Option.DisplayName;
            using (var muted = new SolidBrush(Context.Palette.MutedText))
            {
                graphics.DrawString(difficultyText, bodyFont, muted, 14F, 14F);
                graphics.DrawString($"BEST  {Context.HighScore}", bodyFont, muted, 14F, 31F);
            }

            if (!string.IsNullOrWhiteSpace(banner))
            {
                SizeF bannerSize = graphics.MeasureString(banner, titleFont);
                var box = new RectangleF((viewport.Width - bannerSize.Width) * 0.5F - 14F, 53F, bannerSize.Width + 28F, 30F);
                using (var boxBrush = new SolidBrush(Color.FromArgb(205, Context.Palette.Surface)))
                using (var border = new Pen(Context.Palette.Border))
                using (var textBrush = new SolidBrush(Context.Palette.Text))
                {
                    graphics.FillRectangle(boxBrush, box);
                    graphics.DrawRectangle(border, box.X, box.Y, box.Width, box.Height);
                    graphics.DrawString(banner, titleFont, textBrush, box.X + 14F, box.Y + 6F);
                }
            }

            string exportState = Context.IsExportRunning ? "Export 진행 중" : "Export 완료";
            using (var muted = new SolidBrush(Context.Palette.MutedText))
            {
                SizeF size = graphics.MeasureString(exportState, bodyFont);
                graphics.DrawString(exportState, bodyFont, muted, viewport.Width - size.Width - 12F, 14F);
            }
        }

        private float RandomRange(float minimum, float maximum) =>
            minimum + (float)Context.Random.NextDouble() * (maximum - minimum);

        private static float MoveTowards(float current, float target, float maximumDelta)
        {
            if (Math.Abs(target - current) <= maximumDelta)
                return target;
            return current + Math.Sign(target - current) * maximumDelta;
        }

        private static Color Blend(Color first, Color second, float amount)
        {
            amount = Math.Max(0F, Math.Min(1F, amount));
            return Color.FromArgb(
                (int)(first.A + (second.A - first.A) * amount),
                (int)(first.R + (second.R - first.R) * amount),
                (int)(first.G + (second.G - first.G) * amount),
                (int)(first.B + (second.B - first.B) * amount));
        }

        private sealed class Actor
        {
            public float X;
            public float Y;
            public float VelocityX;
            public float VelocityY;
            public bool Grounded;
        }

        private sealed class VolleyDifficultySettings
        {
            public VolleyDifficultySettings(
                string id,
                string displayName,
                string description,
                float aiMoveSpeed,
                float reactionSeconds,
                float predictionError,
                float jumpDistance,
                float jumpChance,
                float hitBoost)
            {
                Option = new MiniGameDifficultyOption(id, displayName, description);
                AiMoveSpeed = aiMoveSpeed;
                ReactionSeconds = reactionSeconds;
                PredictionError = predictionError;
                JumpDistance = jumpDistance;
                JumpChance = jumpChance;
                HitBoost = hitBoost;
            }

            public MiniGameDifficultyOption Option { get; }
            public float AiMoveSpeed { get; }
            public float ReactionSeconds { get; }
            public float PredictionError { get; }
            public float JumpDistance { get; }
            public float JumpChance { get; }
            public float HitBoost { get; }
        }
    }
}
