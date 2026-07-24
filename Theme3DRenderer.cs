using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal static class Theme3DRenderer
    {
        private sealed class ButtonState
        {
            public bool Primary;
            public bool Hovered;
            public bool Pressed;
            public float HoverProgress;
        }

        private sealed class PanelState
        {
            public bool Accent;
        }

        private static readonly Dictionary<Button, ButtonState> Buttons = new Dictionary<Button, ButtonState>();
        private static readonly Dictionary<Panel, PanelState> Panels = new Dictionary<Panel, PanelState>();
        private static readonly PropertyInfo DoubleBufferedProperty = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        private static Timer buttonAnimationTimer;

        public static void AttachButton(Button button, bool primary)
        {
            if (button == null)
                return;

            if (!Buttons.TryGetValue(button, out ButtonState state))
            {
                state = new ButtonState();
                Buttons.Add(button, state);
                EnableDoubleBuffering(button);
                button.UseVisualStyleBackColor = false;
                button.Paint += (_, e) => DrawButton(button, state, e);
                button.MouseEnter += (_, __) => { state.Hovered = true; StartButtonAnimation(); };
                button.MouseLeave += (_, __) => { state.Hovered = false; state.Pressed = false; StartButtonAnimation(); };
                button.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { state.Pressed = true; button.Invalidate(); } };
                button.MouseUp += (_, __) => { state.Pressed = false; button.Invalidate(); };
                button.Resize += (_, __) => UpdateButtonShape(button);
                button.EnabledChanged += (_, __) => button.Invalidate();
                button.Disposed += (_, __) => Buttons.Remove(button);
            }

            state.Primary = primary;
            UpdateButtonShape(button);
            button.Invalidate();
        }

        public static void AttachPanel(Panel panel, bool accent)
        {
            if (panel == null)
                return;

            EnableDoubleBuffering(panel);
            if (!Panels.TryGetValue(panel, out PanelState state))
            {
                state = new PanelState();
                Panels.Add(panel, state);
                panel.Paint += (_, e) => DrawPanel(panel, state, e);
                panel.Resize += (_, __) => panel.Invalidate();
                panel.Disposed += (_, __) => Panels.Remove(panel);
            }
            state.Accent = accent;
            panel.Invalidate();
        }

        private static void StartButtonAnimation()
        {
            if (!SystemInformation.IsMenuAnimationEnabled)
            {
                foreach (KeyValuePair<Button, ButtonState> item in Buttons)
                {
                    item.Value.HoverProgress = item.Value.Hovered ? 1F : 0F;
                    item.Key.Invalidate();
                }
                return;
            }

            if (buttonAnimationTimer == null)
            {
                buttonAnimationTimer = new Timer { Interval = 16 };
                buttonAnimationTimer.Tick += (_, __) => AdvanceButtonAnimations();
            }
            buttonAnimationTimer.Start();
        }

        private static void AdvanceButtonAnimations()
        {
            bool active = false;
            foreach (KeyValuePair<Button, ButtonState> item in new List<KeyValuePair<Button, ButtonState>>(Buttons))
            {
                Button button = item.Key;
                ButtonState state = item.Value;
                float target = state.Hovered ? 1F : 0F;
                float difference = target - state.HoverProgress;
                if (Math.Abs(difference) > 0.02F)
                {
                    state.HoverProgress += Math.Sign(difference) * Math.Min(Math.Abs(difference), 0.22F);
                    active = true;
                }
                else
                {
                    state.HoverProgress = target;
                }

                if (!button.IsDisposed)
                    button.Invalidate();
            }

            if (!active)
                buttonAnimationTimer.Stop();
        }
        private static void EnableDoubleBuffering(Control control)
        {
            try
            {
                DoubleBufferedProperty?.SetValue(control, true, null);
            }
            catch
            {
                // Painting remains functional on hosts that reject reflective style changes.
            }
        }

        private static void DrawButton(Button button, ButtonState state, PaintEventArgs e)
        {
            if (button.Width < 8 || button.Height < 12)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(button.Parent?.BackColor ?? UITheme.AppBackground);

            if (UITheme.CurrentTheme == AppTheme.Default)
            {
                DrawModernButton(button, state, e.Graphics);
                return;
            }

            int offset = state.Pressed ? 4 : state.Hovered ? 0 : 1;
            Rectangle softShadowRect = new Rectangle(3, 6, button.Width - 6, button.Height - 7);
            Rectangle sideRect = new Rectangle(1, 5, button.Width - 3, button.Height - 7);
            Rectangle faceRect = new Rectangle(1, 1 + offset, button.Width - 3, button.Height - 8);

            using (GraphicsPath softShadowPath = RoundedRectangle(softShadowRect, 8))
            using (var softShadow = new SolidBrush(Color.FromArgb(UITheme.IsDarkMode ? 155 : 85, UITheme.DepthShadow)))
                e.Graphics.FillPath(softShadow, softShadowPath);
            using (GraphicsPath sidePath = RoundedRectangle(sideRect, 8))
            using (var sideBrush = new LinearGradientBrush(sideRect, UITheme.BorderStrong, UITheme.DepthShadow, 90F))
                e.Graphics.FillPath(sideBrush, sidePath);

            Color baseColor = state.Primary
                ? state.Pressed ? UITheme.AccentPressed : state.Hovered ? UITheme.AccentHover : UITheme.Accent
                : state.Pressed ? Blend(UITheme.SurfaceMuted, Color.Black, 0.16F)
                : state.Hovered ? Blend(UITheme.Surface, Color.White, UITheme.IsDarkMode ? 0.08F : 0.22F) : UITheme.Surface;
            if (!button.Enabled)
                baseColor = Blend(baseColor, UITheme.AppBackground, 0.48F);

            Color topColor = Blend(baseColor, Color.White, UITheme.IsDarkMode ? 0.23F : 0.46F);
            Color bottomColor = Blend(baseColor, Color.Black, UITheme.IsDarkMode ? 0.18F : 0.14F);
            using (GraphicsPath facePath = RoundedRectangle(faceRect, 8))
            using (var faceBrush = new LinearGradientBrush(faceRect, topColor, bottomColor, 90F))
            using (var borderPen = new Pen(state.Primary ? UITheme.AccentPressed : Blend(UITheme.BorderStrong, Color.Black, 0.16F), 1.25F))
            {
                e.Graphics.FillPath(faceBrush, facePath);
                e.Graphics.DrawPath(borderPen, facePath);
            }

            using (var shine = new Pen(Color.FromArgb(UITheme.IsDarkMode ? 105 : 210, Color.White), 1.5F))
            {
                e.Graphics.DrawLine(shine, faceRect.Left + 8, faceRect.Top + 2, faceRect.Right - 8, faceRect.Top + 2);
                e.Graphics.DrawLine(shine, faceRect.Left + 2, faceRect.Top + 8, faceRect.Left + 2, faceRect.Bottom - 7);
            }

            Rectangle textRect = faceRect;
            textRect.Offset(0, state.Pressed ? 1 : -1);
            TextRenderer.DrawText(
                e.Graphics,
                button.Text,
                button.Font,
                textRect,
                !button.Enabled ? UITheme.TextMuted : state.Primary ? UITheme.TextOnAccent : UITheme.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            if (button.Focused)
            {
                Rectangle focus = Rectangle.Inflate(faceRect, -5, -5);
                ControlPaint.DrawFocusRectangle(e.Graphics, focus, state.Primary ? UITheme.TextOnAccent : UITheme.TextPrimary, Color.Transparent);
            }
        }

        private static void DrawPanel(Panel panel, PanelState state, PaintEventArgs e)
        {
            if (panel.Width < 16 || panel.Height < 18)
                return;
            RenderPanel(panel, state, e.Graphics);
        }

        private static void RenderPanel(Panel panel, PanelState state, Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(UITheme.AppBackground);

            if (UITheme.CurrentTheme == AppTheme.Default)
            {
                RenderModernPanel(panel, state, graphics);
                return;
            }

            Rectangle farShadowRect = new Rectangle(5, 7, panel.Width - 8, panel.Height - 10);
            Rectangle nearShadowRect = new Rectangle(3, 5, panel.Width - 7, panel.Height - 9);
            Rectangle sideRect = new Rectangle(1, 5, panel.Width - 7, panel.Height - 9);
            Rectangle faceRect = new Rectangle(1, 1, panel.Width - 8, panel.Height - 9);

            using (GraphicsPath farPath = RoundedRectangle(farShadowRect, 11))
            using (var farShadow = new SolidBrush(Color.FromArgb(UITheme.IsDarkMode ? 95 : 50, UITheme.DepthShadow)))
                graphics.FillPath(farShadow, farPath);
            using (GraphicsPath nearPath = RoundedRectangle(nearShadowRect, 11))
            using (var nearShadow = new SolidBrush(Color.FromArgb(UITheme.IsDarkMode ? 185 : 105, UITheme.DepthShadow)))
                graphics.FillPath(nearShadow, nearPath);
            using (GraphicsPath sidePath = RoundedRectangle(sideRect, 10))
            using (var sideBrush = new LinearGradientBrush(sideRect, UITheme.BorderStrong, UITheme.DepthShadow, 90F))
                graphics.FillPath(sideBrush, sidePath);

            Color baseColor = panel.BackColor;
            Color topColor = Blend(baseColor, Color.White, UITheme.IsDarkMode ? 0.16F : 0.34F);
            Color bottomColor = Blend(baseColor, Color.Black, UITheme.IsDarkMode ? 0.13F : 0.10F);
            using (GraphicsPath facePath = RoundedRectangle(faceRect, 10))
            using (var faceBrush = new LinearGradientBrush(faceRect, topColor, bottomColor, 90F))
            using (var border = new Pen(Blend(UITheme.BorderStrong, Color.Black, 0.10F), 1.4F))
            {
                graphics.FillPath(faceBrush, facePath);
                graphics.DrawPath(border, facePath);
            }

            if (state.Accent)
            {
                Rectangle accentStrip = new Rectangle(faceRect.Left + 10, faceRect.Top + 3, Math.Max(30, faceRect.Width - 20), 4);
                using (GraphicsPath stripPath = RoundedRectangle(accentStrip, 2))
                using (var strip = new LinearGradientBrush(accentStrip, Blend(UITheme.Accent, Color.White, 0.34F), UITheme.AccentPressed, 0F))
                    graphics.FillPath(strip, stripPath);
            }

            using (var topHighlight = new Pen(Color.FromArgb(UITheme.IsDarkMode ? 120 : 225, UITheme.DepthHighlight), 1.5F))
            using (var edgeShadow = new Pen(Color.FromArgb(UITheme.IsDarkMode ? 170 : 105, UITheme.DepthShadow), 1.4F))
            {
                graphics.DrawLine(topHighlight, faceRect.Left + 11, faceRect.Top + 2, faceRect.Right - 11, faceRect.Top + 2);
                graphics.DrawLine(topHighlight, faceRect.Left + 2, faceRect.Top + 10, faceRect.Left + 2, faceRect.Bottom - 9);
                graphics.DrawLine(edgeShadow, faceRect.Left + 10, faceRect.Bottom, faceRect.Right - 8, faceRect.Bottom);
                graphics.DrawLine(edgeShadow, faceRect.Right, faceRect.Top + 10, faceRect.Right, faceRect.Bottom - 7);
            }
        }
        private static void UpdateButtonShape(Button button)
        {
            Region previous = button.Region;
            if (button.Width < 4 || button.Height < 4)
            {
                button.Region = null;
                previous?.Dispose();
                return;
            }

            using (GraphicsPath path = RoundedRectangle(new Rectangle(0, 0, button.Width, button.Height), 9))
                button.Region = new Region(path);
            previous?.Dispose();
        }

        private static void DrawModernButton(Button button, ButtonState state, Graphics graphics)
        {
            Rectangle bounds = new Rectangle(0, 0, button.Width - 1, button.Height - 1);
            Color fill = state.Primary
                ? state.Pressed ? UITheme.AccentPressed : state.Hovered ? UITheme.AccentHover : UITheme.Accent
                : state.Pressed ? UITheme.SurfaceMuted : Blend(UITheme.Surface, UITheme.Accent, 0.06F * state.HoverProgress);
            if (!button.Enabled)
                fill = Blend(fill, UITheme.AppBackground, 0.55F);

            using (GraphicsPath path = RoundedRectangle(bounds, 8))
            using (var brush = new SolidBrush(fill))
            using (var border = new Pen(state.Primary ? fill : Blend(UITheme.BorderStrong, UITheme.Accent, state.HoverProgress)))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(border, path);
            }

            TextRenderer.DrawText(
                graphics,
                button.Text,
                button.Font,
                bounds,
                !button.Enabled ? UITheme.TextMuted : state.Primary ? UITheme.TextOnAccent : UITheme.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            if (button.Focused)
            {
                Rectangle focus = Rectangle.Inflate(bounds, -4, -4);
                ControlPaint.DrawFocusRectangle(graphics, focus);
            }
        }

        private static void RenderModernPanel(Panel panel, PanelState state, Graphics graphics)
        {
            Rectangle shadowRect = new Rectangle(2, 3, panel.Width - 5, panel.Height - 5);
            Rectangle faceRect = new Rectangle(1, 1, panel.Width - 4, panel.Height - 4);
            using (GraphicsPath shadowPath = RoundedRectangle(shadowRect, 10))
            using (var shadow = new SolidBrush(Color.FromArgb(UITheme.IsDarkMode ? 55 : 22, UITheme.DepthShadow)))
                graphics.FillPath(shadow, shadowPath);

            using (GraphicsPath facePath = RoundedRectangle(faceRect, 10))
            using (var fill = new SolidBrush(panel.BackColor))
            using (var border = new Pen(UITheme.Border))
            {
                graphics.FillPath(fill, facePath);
                graphics.DrawPath(border, facePath);
            }

            if (state.Accent)
            {
                Rectangle accentLine = new Rectangle(faceRect.Left + 14, faceRect.Bottom - 2, Math.Max(24, faceRect.Width - 28), 2);
                using (GraphicsPath linePath = RoundedRectangle(accentLine, 1))
                using (var accent = new SolidBrush(Color.FromArgb(150, UITheme.Accent)))
                    graphics.FillPath(accent, linePath);
            }
        }
        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = Math.Min(Math.Min(bounds.Width, bounds.Height), radius * 2);
            var path = new GraphicsPath();
            if (diameter <= 2)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Blend(Color source, Color target, float amount)
        {
            amount = Math.Max(0F, Math.Min(1F, amount));
            return Color.FromArgb(
                source.A,
                (int)(source.R + (target.R - source.R) * amount),
                (int)(source.G + (target.G - source.G) * amount),
                (int)(source.B + (target.B - source.B) * amount));
        }
    }
}