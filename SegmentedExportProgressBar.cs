using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal enum SegmentedPhaseState
    {
        Pending,
        Running,
        Done,
        Skipped,
        Failed
    }

    /// <summary>단일 바 + 구분선으로 Export 3단계 진행을 표시.</summary>
    internal sealed class SegmentedExportProgressBar : Control
    {
        private const int PhaseCount = 3;
        private const int BarHeight = 18;
        private const int LabelGap = 5;
        private const int LabelHeight = 16;

        private readonly SegmentedPhaseState[] _states = new SegmentedPhaseState[PhaseCount];
        private readonly float[] _progress = new float[PhaseCount];
        private readonly float[] _displayProgress = new float[PhaseCount];
        private readonly string[] _captions =
        {
            "Excel → CSV",
            "테이블 Export",
            "Unity · MessagePack"
        };

        private readonly Timer _marqueeTimer;
        private int _marqueeOffset;

        public SegmentedExportProgressBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            DoubleBuffered = true;
            Height = BarHeight + LabelGap + LabelHeight + 2;
            MinimumSize = new Size(240, Height);
            TabStop = false;

            _marqueeTimer = new Timer { Interval = 35 };
            _marqueeTimer.Tick += MarqueeTimer_Tick;
            Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < PhaseCount; i++)
            {
                _states[i] = SegmentedPhaseState.Pending;
                _progress[i] = 0f;
                _displayProgress[i] = 0f;
            }

            _captions[0] = "Excel → CSV";
            _captions[1] = "테이블 Export";
            _captions[2] = "Unity · MessagePack";
            UpdateMarqueeTimer();
            Invalidate();
        }

        public void SetPhaseState(int phaseIndex, SegmentedPhaseState state, string caption = null)
        {
            if (phaseIndex < 0 || phaseIndex >= PhaseCount)
                return;

            _states[phaseIndex] = state;
            if (!string.IsNullOrEmpty(caption))
                _captions[phaseIndex] = caption;

            switch (state)
            {
                case SegmentedPhaseState.Done:
                case SegmentedPhaseState.Skipped:
                    _progress[phaseIndex] = 1f;
                    break;
                case SegmentedPhaseState.Pending:
                    _progress[phaseIndex] = 0f;
                    break;
                case SegmentedPhaseState.Failed:
                    if (phaseIndex != 1)
                        _progress[phaseIndex] = 0f;
                    break;
            }

            UpdateMarqueeTimer();
            Invalidate();
        }

        public void SetTableProgress(int completed, int total, bool hasFailure = false)
        {
            _progress[1] = total <= 0
                ? 0f
                : Math.Max(0f, Math.Min(1f, (float)completed / total));

            if (hasFailure)
                _states[1] = SegmentedPhaseState.Failed;
            else if (completed >= total && total > 0)
                _states[1] = SegmentedPhaseState.Done;
            else
                _states[1] = SegmentedPhaseState.Running;

            UpdateMarqueeTimer();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int barTop = 0;
            int barWidth = Math.Max(PhaseCount * 40, ClientSize.Width);
            var barRect = new Rectangle(0, barTop, barWidth, BarHeight);

            Color trackColor = UiTheme.IsDarkMode
                ? Color.FromArgb(55, 55, 60)
                : Color.FromArgb(229, 231, 235);

            using (var trackBrush = new SolidBrush(trackColor))
            using (var path = CreateRoundedRect(barRect, 4))
            {
                g.FillPath(trackBrush, path);
                using (var borderPen = new Pen(UiTheme.Border, 1f))
                    g.DrawPath(borderPen, path);
            }

            int segmentWidth = barWidth / PhaseCount;
            for (int i = 0; i < PhaseCount; i++)
            {
                var segmentRect = new Rectangle(i * segmentWidth, barTop, segmentWidth, BarHeight);
                if (i == PhaseCount - 1)
                    segmentRect.Width = barWidth - segmentRect.X;

                DrawSegmentFill(g, segmentRect, i);
            }

            using (var dividerPen = new Pen(UiTheme.BorderStrong, 1f))
            {
                for (int i = 1; i < PhaseCount; i++)
                {
                    int x = i * segmentWidth;
                    g.DrawLine(dividerPen, x, barTop + 2, x, barTop + BarHeight - 2);
                }
            }

            int labelTop = barTop + BarHeight + LabelGap;
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                for (int i = 0; i < PhaseCount; i++)
                {
                    var labelRect = new Rectangle(i * segmentWidth, labelTop, segmentWidth, LabelHeight);
                    if (i == PhaseCount - 1)
                        labelRect.Width = barWidth - labelRect.X;

                    Color labelColor = LabelColorForState(_states[i]);
                    using (var brush = new SolidBrush(labelColor))
                        g.DrawString(_captions[i], UiTheme.FontUi, brush, labelRect, format);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _marqueeTimer?.Dispose();
            base.Dispose(disposing);
        }

        private void MarqueeTimer_Tick(object sender, EventArgs e)
        {
            _marqueeOffset = (_marqueeOffset + 7) % 400;
            bool progressChanged = false;
            for (int i = 0; i < PhaseCount; i++)
            {
                float delta = _progress[i] - _displayProgress[i];
                if (Math.Abs(delta) <= 0.002f)
                {
                    _displayProgress[i] = _progress[i];
                    continue;
                }

                _displayProgress[i] += delta * 0.24f;
                progressChanged = true;
            }

            Invalidate();
            if (!progressChanged && !HasRunningPhase())
                _marqueeTimer.Stop();
        }

        private void UpdateMarqueeTimer()
        {
            if (!SystemInformation.IsMenuAnimationEnabled || UiTheme.CurrentTheme != AppTheme.Default)
            {
                for (int i = 0; i < PhaseCount; i++)
                    _displayProgress[i] = _progress[i];
                _marqueeTimer.Stop();
                return;
            }

            bool progressPending = false;
            for (int i = 0; i < PhaseCount; i++)
            {
                if (Math.Abs(_progress[i] - _displayProgress[i]) > 0.002f)
                {
                    progressPending = true;
                    break;
                }
            }

            _marqueeTimer.Enabled = HasRunningPhase() || progressPending;
        }

        private bool HasRunningPhase()
        {
            for (int i = 0; i < PhaseCount; i++)
            {
                if (_states[i] == SegmentedPhaseState.Running)
                    return true;
            }
            return false;
        }
        private void DrawSegmentFill(Graphics g, Rectangle segmentRect, int phaseIndex)
        {
            SegmentedPhaseState state = _states[phaseIndex];
            float progress = _displayProgress[phaseIndex];
            Rectangle inner = Rectangle.Inflate(segmentRect, -1, -1);
            if (inner.Width <= 0 || inner.Height <= 0)
                return;

            switch (state)
            {
                case SegmentedPhaseState.Running:
                    if (phaseIndex == 1)
                    {
                        int fillWidth = (int)Math.Round(inner.Width * progress);
                        if (fillWidth > 0)
                        {
                            var fillRect = new Rectangle(inner.X, inner.Y, fillWidth, inner.Height);
                            using (var brush = new SolidBrush(UiTheme.Accent))
                                g.FillRectangle(brush, fillRect);
                            DrawProgressShimmer(g, fillRect);
                        }
                    }
                    else
                    {
                        DrawMarqueeFill(g, inner);
                    }
                    break;

                case SegmentedPhaseState.Done:
                    using (var brush = new SolidBrush(UiTheme.LogSuccess))
                        g.FillRectangle(brush, inner);
                    break;

                case SegmentedPhaseState.Skipped:
                    Color skipped = UiTheme.IsDarkMode
                        ? Color.FromArgb(90, 90, 96)
                        : Color.FromArgb(209, 213, 219);
                    using (var brush = new SolidBrush(skipped))
                        g.FillRectangle(brush, inner);
                    break;

                case SegmentedPhaseState.Failed:
                    {
                        float fillRatio = progress > 0f ? progress : 1f;
                        int fillWidth = Math.Max(1, (int)Math.Round(inner.Width * fillRatio));
                        fillWidth = Math.Min(fillWidth, inner.Width);
                        using (var brush = new SolidBrush(UiTheme.LogError))
                            g.FillRectangle(brush, new Rectangle(inner.X, inner.Y, fillWidth, inner.Height));
                    }
                    break;
            }
        }

        private void DrawMarqueeFill(Graphics g, Rectangle inner)
        {
            int blockWidth = Math.Max(24, inner.Width / 3);
            int travel = inner.Width + blockWidth;
            int start = inner.X + (int)((long)_marqueeOffset * travel / 400) - blockWidth;

            var blockRect = new Rectangle(start, inner.Y, blockWidth, inner.Height);
            blockRect.Intersect(inner);
            if (blockRect.Width <= 0)
                return;

            using (var brush = new LinearGradientBrush(
                blockRect,
                Color.FromArgb(40, UiTheme.Accent),
                UiTheme.Accent,
                LinearGradientMode.Horizontal))
            {
                g.FillRectangle(brush, blockRect);
            }
        }

        private void DrawProgressShimmer(Graphics g, Rectangle fillRect)
        {
            if (!SystemInformation.IsMenuAnimationEnabled || fillRect.Width < 10)
                return;

            int shimmerWidth = Math.Max(22, Math.Min(72, fillRect.Width / 3));
            int travel = fillRect.Width + shimmerWidth;
            int x = fillRect.X + (int)((long)_marqueeOffset * travel / 400) - shimmerWidth;
            var shimmerRect = new Rectangle(x, fillRect.Y, shimmerWidth, fillRect.Height);
            GraphicsState state = g.Save();
            try
            {
                g.SetClip(fillRect);
                using (var brush = new LinearGradientBrush(
                    shimmerRect,
                    Color.FromArgb(0, Color.White),
                    Color.FromArgb(0, Color.White),
                    LinearGradientMode.Horizontal))
                {
                    brush.InterpolationColors = new ColorBlend
                    {
                        Colors = new[]
                        {
                            Color.FromArgb(0, Color.White),
                            Color.FromArgb(95, Color.White),
                            Color.FromArgb(0, Color.White)
                        },
                        Positions = new[] { 0f, 0.5f, 1f }
                    };
                    g.FillRectangle(brush, shimmerRect);
                }
            }
            finally
            {
                g.Restore(state);
            }
        }
        private static Color LabelColorForState(SegmentedPhaseState state)
        {
            switch (state)
            {
                case SegmentedPhaseState.Running:
                    return UiTheme.StatusRunning;
                case SegmentedPhaseState.Done:
                    return UiTheme.LogSuccess;
                case SegmentedPhaseState.Failed:
                    return UiTheme.LogError;
                case SegmentedPhaseState.Skipped:
                    return UiTheme.TextMuted;
                default:
                    return UiTheme.TextMuted;
            }
        }

        private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
