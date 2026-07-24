using System;
using System.IO;
using System.Text;

namespace CSVParserTool
{
    /// <summary>Writes generated artifacts only when their content changed, preserving timestamps and avoiding Unity reimports.</summary>
    public static class GeneratedFileWriter
    {
        public static bool WriteAllTextIfChanged(string path, string content, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Output path is empty.", nameof(path));
            content = content ?? string.Empty;
            encoding = encoding ?? new UTF8Encoding(false);
            if (File.Exists(path))
            {
                try
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan))
                    using (var reader = new StreamReader(stream, encoding, true))
                    {
                        if (string.Equals(reader.ReadToEnd(), content, StringComparison.Ordinal)) return false;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, content, encoding);
            return true;
        }

        public static bool WriteAllBytesIfChanged(string path, byte[] content)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Output path is empty.", nameof(path));
            content = content ?? Array.Empty<byte>();
            if (File.Exists(path))
            {
                try
                {
                    var info = new FileInfo(path);
                    if (info.Length == content.Length && BytesEqual(path, content)) return false;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, content);
            return true;
        }

        private static bool BytesEqual(string path, byte[] expected)
        {
            var buffer = new byte[81920];
            int offset = 0;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, buffer.Length, FileOptions.SequentialScan))
            {
                while (offset < expected.Length)
                {
                    int read = stream.Read(buffer, 0, Math.Min(buffer.Length, expected.Length - offset));
                    if (read <= 0) return false;
                    for (int i = 0; i < read; i++) if (buffer[i] != expected[offset + i]) return false;
                    offset += read;
                }
                return stream.ReadByte() < 0;
            }
        }
    }
}