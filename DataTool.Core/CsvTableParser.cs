using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CSVParserTool
{
    /// <summary>CSV 한 테이블 파싱 결과 — 코드 생성·MessagePack 내보내기 공통.</summary>
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
        /// <summary>첫 열 값이 <c>#</c> 로 시작하면 설명 행(데이터 제외).</summary>
        public static bool IsDescriptionRow(string firstColumnValue)
        {
            if (string.IsNullOrEmpty(firstColumnValue))
                return false;

            string s = firstColumnValue.TrimStart();
            return s.Length > 0 && s[0] == '#';
        }

        public static bool IsDescriptionRow(IReadOnlyList<string> cells)
        {
            if (cells == null || cells.Count == 0)
                return false;

            return IsDescriptionRow(cells[0]);
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

            int headerLine = tableLineIndexes[0];
            var headers = SplitCsvLine(lines[headerLine]);
            var enumAcc = new EnumAccumulator();

            var columnTypes = new string[headers.Length];
            var firstValues = SplitCsvLine(lines[tableLineIndexes[1]]);
            for (int i = 0; i < headers.Length; i++)
                columnTypes[i] = InferType(firstValues[i], headers[i], enumAcc);

            for (int t = 1; t < tableLineIndexes.Count; t++)
            {
                var values = SplitCsvLine(lines[tableLineIndexes[t]]);
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
                dataRows.Add(SplitCsvLine(lines[tableLineIndexes[t]]));

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
            var indexes = new List<int>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                if (!IsDescriptionRow(SplitCsvLine(lines[i])))
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
