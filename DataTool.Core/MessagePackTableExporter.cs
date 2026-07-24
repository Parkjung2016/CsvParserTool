using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MessagePack;

namespace CSVParserTool
{
    /// <summary>테이블 CSV → Unity에서 역직렬화 가능한 MessagePack 바이너리.</summary>
    public static class MessagePackTableExporter
    {
        private sealed class GrowingBufferWriter : IBufferWriter<byte>
        {
            private byte[] _data = new byte[65536];
            private int _index;

            public void Advance(int count) => _index += count;

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                int need = _index + Math.Max(sizeHint, 256);
                if (need > _data.Length)
                {
                    int newLen = _data.Length;
                    while (newLen < need)
                        newLen *= 2;
                    Array.Resize(ref _data, newLen);
                }

                return new Memory<byte>(_data, _index, _data.Length - _index);
            }

            public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

            public byte[] ToArray()
            {
                var copy = new byte[_index];
                Buffer.BlockCopy(_data, 0, copy, 0, _index);
                return copy;
            }
        }

        public static void ExportToFile(string csvPath, string outputPath, string classNameOverride = null) =>
            ExportToFile(CsvTableParser.Parse(csvPath, classNameOverride), outputPath);

        public static void ExportToFile(CsvTableParseResult table, string outputPath)
        {
            byte[] bytes = BuildBytes(table);
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            GeneratedFileWriter.WriteAllBytesIfChanged(outputPath, bytes);
        }

        public static byte[] BuildBytes(CsvTableParseResult table)
        {
            var buffer = new GrowingBufferWriter();
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(table.DataRows.Count);

            foreach (string[] row in table.DataRows)
            {
                writer.WriteArrayHeader(table.Headers.Length);
                for (int c = 0; c < table.Headers.Length; c++)
                {
                    string cell = c < row.Length ? row[c] : string.Empty;
                    WriteColumn(ref writer, table.ColumnTypes[c], cell, table.EnumMembers);
                }
            }

            writer.Flush();
            return buffer.ToArray();
        }

        private static void WriteColumn(
            ref MessagePackWriter writer,
            string columnType,
            string cell,
            IReadOnlyDictionary<string, IReadOnlyList<string>> enums)
        {
            if (CsvColumnTypes.TryGetArrayElementType(columnType, out string elementType))
            {
                string[] items = CsvColumnTypes.SplitArrayCell(cell);
                writer.WriteArrayHeader(items.Length);
                foreach (string item in items)
                    WriteColumn(ref writer, elementType, item, enums);
                return;
            }

            if (enums != null && enums.TryGetValue(columnType, out IReadOnlyList<string> members))
            {
                string id = CsvTableParser.SanitizeIdentifier(cell.Trim());
                if (string.IsNullOrEmpty(id) || id == "_")
                {
                    writer.Write(0);
                    return;
                }

                int idx = -1;
                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i] == id)
                    {
                        idx = i;
                        break;
                    }
                }

                if (idx < 0)
                    throw new InvalidOperationException($"Enum '{columnType}' has no member for value '{cell}'.");

                writer.Write(idx);
                return;
            }

            cell = cell.Trim();
            switch (columnType)
            {
                case "int":
                    if (!int.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
                        iv = 0;
                    writer.Write(iv);
                    break;
                case "uint":
                    if (!uint.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uv))
                        uv = 0;
                    writer.Write(uv);
                    break;
                case "float":
                    if (!float.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out float fv))
                        fv = 0f;
                    writer.Write(fv);
                    break;
                case "double":
                    if (!double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv))
                        dv = 0d;
                    writer.Write(dv);
                    break;
                case "bool":
                    if (!bool.TryParse(cell, out bool bv))
                        bv = false;
                    writer.Write(bv);
                    break;
                default:
                    writer.Write(NormalizeStringCell(cell));
                    break;
            }
        }

        private static string NormalizeStringCell(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                return s.Substring(1, s.Length - 2).Replace("\"\"", "\"");
            return s;
        }
    }
}
