using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class Program
{
    static int Main(string[] args)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        string src = Path.Combine(root, "assets", "pjdev-icon-source.png");
        string dest = Path.Combine(root, "Properties", "AppIcon.ico");

        if (!File.Exists(src))
        {
            Console.Error.WriteLine("Missing source: " + src);
            return 1;
        }

        using (var probe = new Bitmap(src))
            Console.WriteLine($"Source: {probe.Width}x{probe.Height}");

        string squaredPath = Path.Combine(root, "assets", "pjdev-icon-source-square.png");
        using (var sourceForSquare = new Bitmap(src))
        using (var squaredSource = CropCenterSquare(sourceForSquare))
        {
            squaredSource.Save(squaredPath, ImageFormat.Png);
            Console.WriteLine($"Saved square source: {squaredSource.Width}x{squaredSource.Height}");
        }

        string iconSource = File.Exists(squaredPath) ? squaredPath : src;

        int[] sizes = { 16, 32, 48, 256 };
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)sizes.Length);

        var images = new byte[sizes.Length][];
        for (int i = 0; i < sizes.Length; i++)
        {
            using var source = new Bitmap(iconSource);
            using var squared = CropCenterSquare(source);
            using var resized = new Bitmap(sizes[i], sizes[i], PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(resized))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(squared, 0, 0, sizes[i], sizes[i]);
            }

            images[i] = GetDibBytes(resized);
        }

        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            byte w = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
            byte h = w;
            writer.Write(w);
            writer.Write(h);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)images[i].Length);
            writer.Write((uint)offset);
            offset += images[i].Length;
        }

        for (int i = 0; i < sizes.Length; i++)
            writer.Write(images[i]);

        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllBytes(dest, stream.ToArray());
        Console.WriteLine("Created " + dest);
        return 0;
    }

    static Bitmap CropCenterSquare(Bitmap source)
    {
        int dim = Math.Min(source.Width, source.Height);
        int x = (source.Width - dim) / 2;
        int y = (source.Height - dim) / 2;

        var squared = new Bitmap(dim, dim, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(squared))
        {
            g.Clear(Color.Transparent);
            g.DrawImage(source, new Rectangle(0, 0, dim, dim), new Rectangle(x, y, dim, dim), GraphicsUnit.Pixel);
        }

        return squared;
    }

    static byte[] GetDibBytes(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        int xorStride = width * 4;
        int xorSize = xorStride * height;
        int maskRowBytes = ((width + 31) / 32) * 4;
        int maskSize = maskRowBytes * height;
        int headerSize = 40;
        byte[] result = new byte[headerSize + xorSize + maskSize];

        using (var ms = new MemoryStream(result))
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(40);
            bw.Write(width);
            bw.Write(height * 2);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
        }

        var rect = new Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var row = new byte[xorStride];
            var maskRow = new byte[maskRowBytes];

            for (int y = 0; y < height; y++)
            {
                IntPtr src = data.Scan0 + y * data.Stride;
                System.Runtime.InteropServices.Marshal.Copy(src, row, 0, xorStride);

                int xorDst = headerSize + (height - 1 - y) * xorStride;
                Buffer.BlockCopy(row, 0, result, xorDst, xorStride);

                Array.Clear(maskRow, 0, maskRow.Length);
                for (int x = 0; x < width; x++)
                {
                    byte alpha = row[(x * 4) + 3];
                    if (alpha < 128)
                    {
                        int byteIndex = x / 8;
                        int bitIndex = 7 - (x % 8);
                        maskRow[byteIndex] |= (byte)(1 << bitIndex);
                    }
                }

                int maskDst = headerSize + xorSize + (height - 1 - y) * maskRowBytes;
                Buffer.BlockCopy(maskRow, 0, result, maskDst, maskRowBytes);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return result;
    }
}
