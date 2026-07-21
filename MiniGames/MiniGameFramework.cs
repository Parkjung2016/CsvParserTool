using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace CSVParserTool.MiniGames
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ExportMiniGameAttribute : Attribute
    {
        public ExportMiniGameAttribute(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Game id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
            Id = id.Trim();
            DisplayName = displayName.Trim();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; set; } = "";
        public string Controls { get; set; } = "";
        public int Order { get; set; }
    }

    public sealed class MiniGamePalette
    {
        public Color Background { get; internal set; }
        public Color Surface { get; internal set; }
        public Color Border { get; internal set; }
        public Color Text { get; internal set; }
        public Color MutedText { get; internal set; }
        public Color Accent { get; internal set; }
        public Color Success { get; internal set; }
        public Color Danger { get; internal set; }
    }

    public sealed class MiniGameDifficultyOption
    {
        public MiniGameDifficultyOption(string id, string displayName, string description = "")
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Description = description ?? "";
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public override string ToString() => DisplayName;
    }

    public interface IConfigurableMiniGameDifficulty
    {
        IReadOnlyList<MiniGameDifficultyOption> DifficultyOptions { get; }
        string CurrentDifficultyId { get; }
        void SetDifficulty(string difficultyId);
    }

    public sealed class MiniGameInput
    {
        private readonly HashSet<Keys> keys = new HashSet<Keys>();
        private readonly HashSet<MouseButtons> mouseButtons = new HashSet<MouseButtons>();

        public Point MousePosition { get; internal set; }
        public bool IsKeyDown(Keys key) => keys.Contains(key);
        public bool IsMouseDown(MouseButtons button) => mouseButtons.Contains(button);

        internal void KeyDown(Keys key) => keys.Add(key);
        internal void KeyUp(Keys key) => keys.Remove(key);
        internal void MouseDown(MouseButtons button) => mouseButtons.Add(button);
        internal void MouseUp(MouseButtons button) => mouseButtons.Remove(button);
        internal void Clear()
        {
            keys.Clear();
            mouseButtons.Clear();
        }
    }

    public sealed class MiniGameContext
    {
        private readonly Action repaint;
        private readonly Action<int> scoreChanged;

        internal MiniGameContext(Action repaint, Action<int> scoreChanged)
        {
            this.repaint = repaint;
            this.scoreChanged = scoreChanged;
            Random = new Random();
            Input = new MiniGameInput();
        }

        public Random Random { get; }
        public MiniGameInput Input { get; }
        public MiniGamePalette Palette { get; internal set; }
        public Size ViewportSize { get; internal set; }
        public bool IsExportRunning { get; internal set; }
        public int Score { get; private set; }
        public int HighScore { get; internal set; }

        public void SetScore(int value)
        {
            Score = value;
            scoreChanged?.Invoke(value);
        }

        public void AddScore(int amount = 1) => SetScore(Score + amount);
        public void RequestRepaint() => repaint?.Invoke();
    }

    /// <summary>
    /// Export 미니게임의 기본 클래스. Unity와 비슷한 생명주기 메서드만 재정의하면 된다.
    /// 모든 콜백은 UI 스레드에서 실행되며 OnUpdate의 deltaTime은 초 단위다.
    /// </summary>
    public abstract class ExportMiniGame : IDisposable
    {
        protected MiniGameContext Context { get; private set; }

        protected virtual void OnCreate() { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected abstract void OnDraw(Graphics graphics, Rectangle viewport);
        protected virtual void OnResize(Size size) { }
        protected virtual void OnKeyDown(Keys key) { }
        protected virtual void OnKeyUp(Keys key) { }
        protected virtual void OnMouseMove(Point position) { }
        protected virtual void OnMouseDown(MouseButtons button, Point position) { }
        protected virtual void OnMouseUp(MouseButtons button, Point position) { }
        protected virtual void OnExportStateChanged(bool isRunning) { }
        protected virtual void OnStop() { }
        protected virtual void OnDispose() { }

        internal void Attach(MiniGameContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            OnCreate();
            OnStart();
        }

        internal void Update(float deltaTime) => OnUpdate(deltaTime);
        internal void Draw(Graphics graphics, Rectangle viewport) => OnDraw(graphics, viewport);
        internal void Resize(Size size) => OnResize(size);
        internal void KeyDown(Keys key) => OnKeyDown(key);
        internal void KeyUp(Keys key) => OnKeyUp(key);
        internal void MouseMove(Point position) => OnMouseMove(position);
        internal void MouseDown(MouseButtons button, Point position) => OnMouseDown(button, position);
        internal void MouseUp(MouseButtons button, Point position) => OnMouseUp(button, position);
        internal void ExportStateChanged(bool running) => OnExportStateChanged(running);
        internal void Stop() => OnStop();

        public void Dispose()
        {
            OnDispose();
            Context = null;
        }
    }

    internal sealed class MiniGameDescriptor
    {
        public ExportMiniGameAttribute Metadata { get; set; }
        public Type GameType { get; set; }
        public override string ToString() => Metadata.DisplayName;
    }

    internal static class MiniGameCatalog
    {
        public static IReadOnlyList<MiniGameDescriptor> Discover(Action<string> warning)
        {
            Assembly assembly = typeof(MiniGameCatalog).Assembly;
            var games = new List<MiniGameDescriptor>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Type type in GetLoadableTypes(assembly, warning))
            {
                if (type == null || type.IsAbstract || !typeof(ExportMiniGame).IsAssignableFrom(type))
                    continue;

                var metadata = type.GetCustomAttribute<ExportMiniGameAttribute>();
                if (metadata == null || type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                if (!ids.Add(metadata.Id))
                {
                    warning?.Invoke($"중복 게임 ID '{metadata.Id}'는 건너뜁니다: {type.FullName}");
                    continue;
                }

                games.Add(new MiniGameDescriptor { Metadata = metadata, GameType = type });
            }

            return games
                .OrderBy(game => game.Metadata.Order)
                .ThenBy(game => game.Metadata.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly, Action<string> warning)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (Exception loaderException in ex.LoaderExceptions ?? Array.Empty<Exception>())
                    warning?.Invoke($"{assembly.GetName().Name}: {loaderException.Message}");
                return ex.Types.Where(type => type != null);
            }
            catch (Exception ex)
            {
                warning?.Invoke($"{assembly.GetName().Name}: {ex.Message}");
                return Array.Empty<Type>();
            }
        }
    }

    internal sealed class MiniGameCanvas : Control
    {
        private readonly Timer timer;
        private readonly Timer highScoreSaveTimer;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly MiniGameContext context;
        private ExportMiniGame game;
        private long previousTicks;
        private string errorMessage;
        private string currentGameId = "unknown";
        private int highScore;
        private bool highScoreDirty;

        public MiniGameCanvas()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);
            TabStop = true;
            context = new MiniGameContext(Invalidate, HandleScoreChanged);
            timer = new Timer { Interval = 16 };
            timer.Tick += (_, __) => TickGame();
            highScoreSaveTimer = new Timer { Interval = 900 };
            highScoreSaveTimer.Tick += (_, __) => FlushHighScore();
        }

        public event Action<int> ScoreChanged;
        public event Action<int> HighScoreChanged;
        public string LastError => errorMessage;
        public IReadOnlyList<MiniGameDifficultyOption> DifficultyOptions =>
            (game as IConfigurableMiniGameDifficulty)?.DifficultyOptions
            ?? Array.Empty<MiniGameDifficultyOption>();
        public string CurrentDifficultyId =>
            (game as IConfigurableMiniGameDifficulty)?.CurrentDifficultyId;

        public void SetDifficulty(string difficultyId)
        {
            if (game is IConfigurableMiniGameDifficulty configurable)
            {
                configurable.SetDifficulty(difficultyId);
                RefreshHighScore();
                Focus();
                Invalidate();
            }
        }

        public void LoadGame(Type gameType, MiniGamePalette palette, bool exportRunning)
        {
            UnloadGame();
            errorMessage = null;
            context.Palette = palette;
            context.ViewportSize = ClientSize;
            context.IsExportRunning = exportRunning;
            currentGameId = gameType.GetCustomAttribute<ExportMiniGameAttribute>()?.Id
                ?? gameType.FullName
                ?? "unknown";
            highScore = 0;
            context.HighScore = 0;

            try
            {
                game = (ExportMiniGame)Activator.CreateInstance(gameType);
                RefreshHighScore();
                context.SetScore(0);
                game.Attach(context);
                game.Resize(ClientSize);
                game.ExportStateChanged(exportRunning);
                clock.Restart();
                previousTicks = 0;
                timer.Start();
                Focus();
            }
            catch (Exception ex)
            {
                errorMessage = ex.GetBaseException().Message;
                UnloadGame();
            }

            Invalidate();
        }

        public void SetPalette(MiniGamePalette palette)
        {
            context.Palette = palette;
            Invalidate();
        }

        public void SetExportRunning(bool running)
        {
            context.IsExportRunning = running;
            game?.ExportStateChanged(running);
        }

        private void TickGame()
        {
            if (game == null || !Visible)
                return;

            long now = clock.ElapsedTicks;
            float deltaTime = previousTicks == 0
                ? 1F / 60F
                : Math.Min(0.05F, (now - previousTicks) / (float)Stopwatch.Frequency);
            previousTicks = now;

            try
            {
                game.Update(deltaTime);
            }
            catch (Exception ex)
            {
                errorMessage = ex.GetBaseException().Message;
                timer.Stop();
            }

            Invalidate();
        }

        private void HandleScoreChanged(int score)
        {
            ScoreChanged?.Invoke(score);
            if (score <= highScore)
                return;

            string difficultyId = (game as IConfigurableMiniGameDifficulty)?.CurrentDifficultyId;
            if (!ToolSettingsStore.TryUpdateMiniGameHighScore(currentGameId, difficultyId, score))
            {
                RefreshHighScore();
                return;
            }

            highScore = score;
            context.HighScore = score;
            highScoreDirty = true;
            HighScoreChanged?.Invoke(score);
            highScoreSaveTimer.Stop();
            highScoreSaveTimer.Start();
        }

        private void RefreshHighScore()
        {
            string difficultyId = (game as IConfigurableMiniGameDifficulty)?.CurrentDifficultyId;
            highScore = ToolSettingsStore.GetMiniGameHighScore(currentGameId, difficultyId);
            context.HighScore = highScore;
            HighScoreChanged?.Invoke(highScore);
        }

        private void FlushHighScore()
        {
            highScoreSaveTimer.Stop();
            if (!highScoreDirty)
                return;
            highScoreDirty = false;
            ToolSettingsStore.Save();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(context.Palette?.Background ?? Color.FromArgb(24, 24, 27));
            if (game != null && errorMessage == null)
            {
                try
                {
                    game.Draw(e.Graphics, ClientRectangle);
                    return;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.GetBaseException().Message;
                    timer.Stop();
                }
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                using (var brush = new SolidBrush(context.Palette?.Danger ?? Color.IndianRed))
                    e.Graphics.DrawString("게임 실행 오류\n" + errorMessage, Font, brush, ClientRectangle);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            context.ViewportSize = ClientSize;
            game?.Resize(ClientSize);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down
                || key == Keys.A || key == Keys.D || key == Keys.W || key == Keys.S
                || key == Keys.Space)
                return true;
            return base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            context.Input.KeyDown(e.KeyCode);
            game?.KeyDown(e.KeyCode);
            e.Handled = true;
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            context.Input.KeyUp(e.KeyCode);
            game?.KeyUp(e.KeyCode);
            e.Handled = true;
            base.OnKeyUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            context.Input.MousePosition = e.Location;
            game?.MouseMove(e.Location);
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            context.Input.MousePosition = e.Location;
            context.Input.MouseDown(e.Button);
            game?.MouseDown(e.Button, e.Location);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            context.Input.MousePosition = e.Location;
            context.Input.MouseUp(e.Button);
            game?.MouseUp(e.Button, e.Location);
            base.OnMouseUp(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            context.Input.Clear();
            base.OnLostFocus(e);
        }

        private void UnloadGame()
        {
            FlushHighScore();
            timer.Stop();
            clock.Reset();
            context.Input.Clear();
            if (game == null)
                return;
            try { game.Stop(); } catch { }
            try { game.Dispose(); } catch { }
            game = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnloadGame();
                timer.Dispose();
                highScoreSaveTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
