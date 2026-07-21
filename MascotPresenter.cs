using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal enum MascotPose
    {
        Hello,
        Point,
        Celebrate,
        Read
    }

    internal sealed class MascotPresenter : PictureBox
    {
        private readonly Dictionary<MascotPose, Bitmap> images = new Dictionary<MascotPose, Bitmap>();
        private readonly Timer timer;
        private MascotPose[] sequence = { MascotPose.Hello };
        private int frameIndex;

        public MascotPresenter()
        {
            SizeMode = PictureBoxSizeMode.Zoom;
            BackColor = Color.Transparent;
            TabStop = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            foreach (MascotPose pose in Enum.GetValues(typeof(MascotPose)))
                images[pose] = LoadPose(pose);

            timer = new Timer { Interval = 950 };
            timer.Tick += (_, __) => AdvanceFrame();
            ShowPose(MascotPose.Hello);
        }

        public void SetSequence(params MascotPose[] poses)
        {
            sequence = poses == null || poses.Length == 0
                ? new[] { MascotPose.Hello }
                : poses.Distinct().ToArray();
            frameIndex = 0;
            ShowPose(sequence[0]);
            UpdateTimer();
        }

        public void ShowPose(MascotPose pose)
        {
            if (images.TryGetValue(pose, out Bitmap image))
                Image = image;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateTimer();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer?.Stop();
                timer?.Dispose();
                Image = null;
                foreach (Bitmap bitmap in images.Values)
                    bitmap?.Dispose();
                images.Clear();
            }
            base.Dispose(disposing);
        }

        private void AdvanceFrame()
        {
            if (sequence.Length < 2)
                return;
            frameIndex = (frameIndex + 1) % sequence.Length;
            ShowPose(sequence[frameIndex]);
        }

        private void UpdateTimer()
        {
            timer.Enabled = Visible && sequence.Length > 1;
        }

        private static Bitmap LoadPose(MascotPose pose)
        {
            string suffix = "mascot-" + pose.ToString().ToLowerInvariant() + ".png";
            Assembly assembly = typeof(MascotPresenter).Assembly;
            string resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(resourceName))
                return new Bitmap(1, 1);

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (var source = new Bitmap(stream))
                return new Bitmap(source);
        }
    }
}