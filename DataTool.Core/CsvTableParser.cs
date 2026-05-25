using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CSVParserTool
{
    /// <summary>CSV 한 테이블 파싱 결과 — 코드 생성·MessagePack·NDB보내기 공통.</summary>
    public sealed class CsvTableParseResult
    {
        public string ClassName { get; }
        public string[] Headers { get; }
        public string[] ColumnTypes { get; }
        public IReadOnlyList<string> EnumDeclarationOrder { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<string>> EnumMembers { get; }
        public IReadOnlyList<string[]> DataRows { get; }

        public CsvTableParseResult(
            string className,
            string[] headers,
            string[] columnTypes,
            IReadOnlyList<string> enumDeclarationOrder,
            IReadOnlyDictionary<string, IReadOnlyList<string>> enumMembers,
            IReadOnlyList<string[]> dataRows)
        {
            ClassName = className;
            Headers = headers;
            ColumnTypes = columnTypes;
            EnumDeclarationOrder = enumDeclarationOrder;
            EnumMembers = enumMembers;
            DataRows = dataRows;
        }
    }

    public static class CsvTableParser
    {
        /// <summary>
        /// 헤더가 <c>#</c> 로 시작하면 그 열 전체를 메모 열로 본다 (<c>#</c>, <c>#설명</c> 등 모두 동일).
        /// 아래 셀에 <c>#</c> 없이 <c>플레이어</c>만 있어도 CSV·NDB·bytes보내기에서 제외한다. 엑셀 원본은 유지.
        /// </summary>
        public static bool IsNoteColumnHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
                return false;

            string s = header.TrimStart();
            return s.Length > 0 && s[0] == '#';
        }

        public static int FindIdColumnIndex(IReadOnlyList<string> headers)
        {
            if (headers == null)
                return -1;

            for (int i = 0; i < headers.Count; i++)
            {
                if (string.Equals(headers[i]?.Trim(), "Id", StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        /// <summary>보내기용: <c>#</c> 헤더 열은 false.</summary>
        public static bool[] BuildExportColumnKeepMask(IReadOnlyList<string> headerCells)
        {
            if (headerCells == null || headerCells.Count == 0)
                return Array.Empty<bool>();

            var mask = new bool[headerCells.Count];
            for (int i = 0; i < headerCells.Count; i++)
                mask[i] = !IsNoteColumnHeader(headerCells[i]);

            return mask;
        }

        /// <summary>보내기용: <c>#</c> 헤더 열만 제거(셀 값은 그대로).</summary>
        public static string[] FilterExportColumns(IReadOnlyList<string> cells, bool[] exportColumnKeepMask)
        {
            if (exportColumnKeepMask == null || exportColumnKeepMask.Length == 0)
                return Array.Empty<string>();

            var result = new List<string>();
            int cellCount = cells?.Count ?? 0;

            for (int i = 0; i < exportColumnKeepMask.Length; i++)
            {
                if (!exportColumnKeepMask[i])
                    continue;

                result.Add(i < cellCount ? cells[i] ?? string.Empty : string.Empty);
            }

            return result.ToArray();
        }

        private sealed class EnumAccumulator
        {
            public readonly List<string> DeclarationOrder = new();
            public readonly Dictionary<string, List<string>> Members = new(StringComparer.Ordinal);
        }

        public static CsvTableParseResult Parse(string csvPath, string classNameOverride = null)
        {
            if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
                throw new FileNotFoundException("CSV not found.", csvPath);

            var lines = ReadAllLinesSafe(csvPath);
            var tableLineIndexes = CollectTableLineIndexes(lines);
            if (tableLineIndexes.Count < 2)
                throw new InvalidOperationException("CSV must have header and at least one data row.");

            string rawName = string.IsNullOrWhiteSpace(classNameOverride)
                ? Path.GetFileNameWithoutExtension(csvPath)
                : classNameOverride;

            string className = CsvClassGenerator.DataRecordClassNameFromFileBaseName(rawName);

            string[] rawHeader = SplitCsvLine(lines[tableLineIndexes[0]]);
            bool[] keepMask = BuildExportColumnKeepMask(rawHeader);
            var headers = FilterExportColumns(rawHeader, keepMask);
            var enumAcc = new EnumAccumulator();

            var columnTypes = new string[headers.Length];
            var firstValues = FilterExportColumns(SplitCsvLine(lines[tableLineIndexes[1]]), keepMask);
            for (int i = 0; i < headers.Length; i++)
                columnTypes[i] = InferType(firstValues[i], headers[i], enumAcc);

            for (int t = 1; t < tableLineIndexes.Count; t++)
            {
                var values = FilterExportColumns(SplitCsvLine(lines[tableLineIndexes[t]]), keepMask);
                for (int i = 0; i < headers.Length; i++)
                {
                    if (headers[i].StartsWith("E", StringComparison.Ordinal))
                    {
                        string cell = i < values.Length ? values[i] : string.Empty;
                        RegisterEnum(enumAcc, headers[i], cell);
                    }
                }
            }

            var dataRows = new List<string[]>();
            for (int t = 1; t < tableLineIndexes.Count; t++)
                dataRows.Add(FilterExportColumns(SplitCsvLine(lines[tableLineIndexes[t]]), keepMask));

            var enumRead = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (var kv in enumAcc.Members)
                enumRead[kv.Key] = kv.Value;

            return new CsvTableParseResult(
                className,
                headers,
                columnTypes,
                enumAcc.DeclarationOrder,
                enumRead,
                dataRows);
        }

        public static string SanitizeIdentifier(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "_";

            var sb = new StringBuilder();
            if (!char.IsLetter(s[0]) && s[0] != '_')
                sb.Append('_');

            foreach (var c in s)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');

            return sb.ToString();
        }

        internal static string InferPrimitiveType(string value)
        {
            value = value.Trim();
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return "int";
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "float";
            if (bool.TryParse(value, out _))
                return "bool";
            return "string";
        }

        private static List<int> CollectTableLineIndexes(string[] lines)
        {
            int headerLine = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                if (FindIdColumnIndex(SplitCsvLine(lines[i])) >= 0)
                {
                    headerLine = i;
                    break;
                }
            }

            if (headerLine < 0)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    headerLine = i;
                    break;
                }
            }

            if (headerLine < 0)
                return new List<int>();

            var indexes = new List<int> { headerLine };

            for (int i = headerLine + 1; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    indexes.Add(i);
            }

            return indexes;
        }

        private static string[] SplitCsvLine(string line) => line.Split(',');

        private static string[] ReadAllLinesSafe(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            var list = new List<string>();
            while (!sr.EndOfStream)
                list.Add(sr.ReadLine());
            return list.ToArray();
        }

        private static string InferType(string value, string fieldName, EnumAccumulator acc)
        {
            value = value.Trim();

            if (value.StartsWith("\"", StringComparison.Ordinal))
                return "string";

            if (value.IndexOf('|') >= 0)
            {
                string first = value.Split('|')[0];
                return $"List<{InferPrimitiveType(first)}>";
            }

            if (fieldName.StartsWith("E", StringComparison.Ordinal))
            {
                RegisterEnum(acc, fieldName, value);
                return fieldName;
            }

            return InferPrimitiveType(value);
        }

        private static void RegisterEnum(EnumAccumulator acc, string enumName, string value)
        {
            if (!acc.Members.TryGetValue(enumName, out var list))
            {
                list = new List<string>();
                acc.Members[enumName] = list;
                acc.DeclarationOrder.Add(enumName);
            }

            string id = SanitizeIdentifier(value.Trim());
            if (string.IsNullOrEmpty(id) || id == "_")
                return;

            if (!list.Contains(id))
                list.Add(id);
        }
    }
}
