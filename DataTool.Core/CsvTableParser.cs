using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CSVParserTool
{
    public sealed class CsvColumnReference
    {
        public string TableName { get; }
        public string ColumnName { get; }
        public bool IsArray { get; }

        public CsvColumnReference(string tableName, string columnName, bool isArray = false)
        {
            TableName = tableName;
            ColumnName = columnName;
            IsArray = isArray;
        }
    }

    /// <summary>CSV 한 테이블 파싱 결과 — 코드 생성·MessagePack·NDB보내기 공통.</summary>
    public sealed class CsvTableParseResult
    {
        public string ClassName { get; }
        public string[] Headers { get; }
        public string[] ColumnTypes { get; }
        public CsvColumnReference[] ColumnReferences { get; }
        private readonly List<string> enumDeclarationOrder;
        public IReadOnlyList<string> EnumDeclarationOrder => enumDeclarationOrder;
        private readonly Dictionary<string, IReadOnlyList<string>> enumMembers;
        public IReadOnlyDictionary<string, IReadOnlyList<string>> EnumMembers => enumMembers;
        public IReadOnlyList<string[]> DataRows { get; }

        public CsvTableParseResult(
            string className,
            string[] headers,
            string[] columnTypes,
            IReadOnlyList<string> enumDeclarationOrder,
            IReadOnlyDictionary<string, IReadOnlyList<string>> enumMembers,
            IReadOnlyList<string[]> dataRows)
            : this(className, headers, columnTypes, null, enumDeclarationOrder, enumMembers, dataRows)
        {
        }

        public CsvTableParseResult(
            string className,
            string[] headers,
            string[] columnTypes,
            CsvColumnReference[] columnReferences,
            IReadOnlyList<string> enumDeclarationOrder,
            IReadOnlyDictionary<string, IReadOnlyList<string>> enumMembers,
            IReadOnlyList<string[]> dataRows)
        {
            ClassName = className;
            Headers = headers;
            ColumnTypes = columnTypes;
            ColumnReferences = columnReferences ?? new CsvColumnReference[headers?.Length ?? 0];
            this.enumDeclarationOrder = enumDeclarationOrder == null
                ? new List<string>()
                : new List<string>(enumDeclarationOrder);
            this.enumMembers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            if (enumMembers != null)
            {
                foreach (var pair in enumMembers)
                    this.enumMembers[pair.Key] = pair.Value;
            }
            DataRows = dataRows;
        }

        internal void RegisterReferencedEnum(string enumName, IReadOnlyList<string> members)
        {
            if (!string.IsNullOrEmpty(enumName) && members != null && !enumMembers.ContainsKey(enumName))
                enumMembers[enumName] = members;
        }

        internal void UseCatalogEnum(string enumName, IReadOnlyList<string> members)
        {
            if (string.IsNullOrWhiteSpace(enumName) || members == null)
                return;

            enumMembers[enumName] = members;
            for (int i = enumDeclarationOrder.Count - 1; i >= 0; i--)
            {
                if (string.Equals(enumDeclarationOrder[i], enumName, StringComparison.OrdinalIgnoreCase))
                    enumDeclarationOrder.RemoveAt(i);
            }
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

        public static CsvTableParseResult Parse(string csvPath, string classNameOverride = null, CsvParseOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
                throw new FileNotFoundException("CSV not found.", csvPath);

            string rawName = string.IsNullOrWhiteSpace(classNameOverride)
                ? Path.GetFileNameWithoutExtension(csvPath)
                : classNameOverride;

            string className = CsvClassGenerator.DataRecordClassNameFromFileBaseName(rawName);
            return ParseLines(ReadAllLinesSafe(csvPath), className, options);
        }

        public static CsvTableParseResult ParseLines(string[] lines, string className, CsvParseOptions options = null)
        {
            if (lines == null || lines.Length == 0)
                throw new InvalidOperationException("CSV is empty.");

            var tableLineIndexes = CollectTableLineIndexes(lines);
            if (tableLineIndexes.Count < 1)
                throw new InvalidOperationException("CSV must have a header row.");

            string[] rawHeader = SplitCsvLine(lines[tableLineIndexes[0]]);
            bool[] noteKeepMask = BuildExportColumnKeepMask(rawHeader);
            string[] headers = FilterExportColumns(rawHeader, noteKeepMask);

            if (tableLineIndexes.Count < 2)
            {
                throw new InvalidOperationException(
                    "헤더 아래 2행에 버전 행이 필요합니다. (예: 1.0.0)");
            }

            if (tableLineIndexes.Count < 3)
            {
                throw new InvalidOperationException(
                    "버전 행(2행) 다음 3행에 타입 행이 필요합니다.");
            }

            string[] rowAfterHeader = FilterExportColumns(SplitCsvLine(lines[tableLineIndexes[1]]), noteKeepMask);
            if (!CsvColumnTypes.LooksLikeVersionRow(rowAfterHeader, headers))
            {
                if (CsvColumnTypes.LooksLikeTypeRow(rowAfterHeader, headers))
                {
                    throw new InvalidOperationException(
                        "2행에 버전 행이 필요합니다. 타입은 3행에 지정하세요. (2행 예: 1.0.0 · 3행 예: int, float)");
                }

                throw new InvalidOperationException(
                    "2행이 버전 행이 아닙니다. 각 Export 컬럼에 1.0.0 형식 버전을 지정하세요.");
            }

            string[] versionCells = rowAfterHeader;
            string[] typeProbe = FilterExportColumns(SplitCsvLine(lines[tableLineIndexes[2]]), noteKeepMask);
            EnsureCompleteTypeRow(typeProbe, headers);
            if (!CsvColumnTypes.LooksLikeTypeRow(typeProbe, headers))
            {
                throw new InvalidOperationException(
                    "3행이 타입 행이 아닙니다. 각 열에 int, string, enum:타입명, int[], ref 테이블.컬럼 등을 지정하세요.");
            }

            const int typeLineTableIndex = 2;
            const int firstDataTableIndex = 3;

            string[] explicitTypeCells = FilterExportColumns(
                SplitCsvLine(lines[tableLineIndexes[typeLineTableIndex]]),
                noteKeepMask);

            var columnTypes = new string[headers.Length];
            var columnReferences = new CsvColumnReference[headers.Length];
            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i];
                string typeCell = i < explicitTypeCells.Length ? explicitTypeCells[i] : string.Empty;
                if (TryParseReferenceType(typeCell, out CsvColumnReference reference))
                {
                    columnReferences[i] = reference;
                    columnTypes[i] = "string";
                }
                else
                {
                    columnTypes[i] = ResolveExplicitColumnType(typeCell, header);
                }
            }

            bool[] versionKeepMask = BuildVersionColumnKeepMask(headers, versionCells, options?.ExportVersion);
            headers = FilterColumnsByMask(headers, versionKeepMask);
            columnTypes = FilterColumnsByMask(columnTypes, versionKeepMask);
            columnReferences = FilterColumnsByMask(columnReferences, versionKeepMask);

            if (headers.Length == 0)
                throw new InvalidOperationException("Export 버전에 포함되는 컬럼이 없습니다. Id 열과 버전을 확인하세요.");

            if (FindIdColumnIndex(headers) < 0)
                throw new InvalidOperationException("Export 결과에 Id 컬럼이 필요합니다.");

            var dataRows = new List<string[]>();
            for (int t = firstDataTableIndex; t < tableLineIndexes.Count; t++)
            {
                string[] row = FilterExportColumns(SplitCsvLine(lines[tableLineIndexes[t]]), noteKeepMask);
                dataRows.Add(FilterColumnsByMask(row, versionKeepMask));
            }

            return new CsvTableParseResult(
                className,
                headers,
                columnTypes,
                columnReferences,
                Array.Empty<string>(),
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
                dataRows);
        }

        /// <summary>배포용 CSV — 헤더·데이터 행만 (버전·타입 행·# 열 제외, Export 버전 필터 반영).</summary>
        public static void WriteDeployedCsv(string csvPath, CsvTableParseResult table)
        {
            if (string.IsNullOrWhiteSpace(csvPath))
                throw new ArgumentException("CSV path is empty.", nameof(csvPath));
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var sb = new StringBuilder();
            AppendCsvRow(sb, table.Headers);
            foreach (string[] row in table.DataRows)
                AppendCsvRow(sb, row);

            string dir = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static void AppendCsvRow(StringBuilder sb, string[] cells)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append(EscapeCsvField(cells[i] ?? string.Empty));
            }

            sb.AppendLine();
        }

        private static string EscapeCsvField(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            if (s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return s;

            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static bool[] BuildVersionColumnKeepMask(
            string[] headers,
            string[] versionCells,
            string exportVersion)
        {
            var mask = new bool[headers.Length];
            if (versionCells == null || versionCells.Length == 0)
            {
                throw new InvalidOperationException(
                    "버전 행이 없습니다. 2행에 각 Export 컬럼 버전(예: 1.0.0)을 지정하세요.");
            }

            if (string.IsNullOrWhiteSpace(exportVersion))
            {
                throw new InvalidOperationException(
                    "버전 행이 있는 테이블은 Export 버전이 필요합니다. (예: 1.0.0)");
            }

            if (!DataVersion.TryParse(exportVersion.Trim(), out DataVersion target))
            {
                throw new InvalidOperationException(
                    $"Export 버전 '{exportVersion.Trim()}' 형식이 올바르지 않습니다. (예: 1.0.0)");
            }

            for (int i = 0; i < headers.Length; i++)
            {
                if (string.Equals(headers[i]?.Trim(), "Id", StringComparison.OrdinalIgnoreCase))
                {
                    mask[i] = true;
                    continue;
                }

                string versionCell = i < versionCells.Length ? versionCells[i]?.Trim() ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(versionCell))
                {
                    mask[i] = true;
                    continue;
                }

                if (!DataVersion.TryParse(versionCell, out DataVersion columnVersion))
                {
                    throw new InvalidOperationException(
                        $"열 '{headers[i]}': 버전 '{versionCell}' 형식이 올바르지 않습니다. (예: 1.0.0)");
                }

                mask[i] = columnVersion <= target;
            }

            return mask;
        }

        private static string[] FilterColumnsByMask(string[] cells, bool[] keepMask)
        {
            if (cells == null || keepMask == null || keepMask.Length == 0)
                return Array.Empty<string>();

            var result = new List<string>();
            int count = Math.Min(cells.Length, keepMask.Length);
            for (int i = 0; i < count; i++)
            {
                if (keepMask[i])
                    result.Add(cells[i] ?? string.Empty);
            }

            return result.ToArray();
        }

        private static CsvColumnReference[] FilterColumnsByMask(CsvColumnReference[] cells, bool[] keepMask)
        {
            if (cells == null || keepMask == null || keepMask.Length == 0)
                return Array.Empty<CsvColumnReference>();

            var result = new List<CsvColumnReference>();
            int count = Math.Min(cells.Length, keepMask.Length);
            for (int i = 0; i < count; i++)
            {
                if (keepMask[i])
                    result.Add(cells[i]);
            }

            return result.ToArray();
        }

        private static void EnsureCompleteTypeRow(string[] typeCells, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                string cell = i < typeCells.Length ? typeCells[i]?.Trim() ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(cell))
                {
                    throw new InvalidOperationException(
                        $"3행 타입이 비어 있습니다. 열 '{headers[i]}'에 int, string, enum:타입명, int[] 등 타입을 지정하세요.");
                }
            }
        }

        private static string ResolveExplicitColumnType(string explicitTypeCell, string header)
        {
            if (string.IsNullOrWhiteSpace(explicitTypeCell))
            {
                throw new InvalidOperationException(
                    $"열 '{header}': 타입이 비어 있습니다. 3행에 타입을 지정하세요.");
            }

            if (!CsvColumnTypes.TryNormalizeExplicit(explicitTypeCell.Trim(), header, out string explicitType))
            {
                throw new InvalidOperationException(
                    $"열 '{header}': 인식할 수 없는 타입 '{explicitTypeCell.Trim()}'. " +
                    "int, string, enum:타입명, int[], enum:타입명[], ref 테이블.컬럼을 사용할 수 있습니다.");
            }

            return explicitType;
        }

        private static bool TryParseReferenceType(string raw, out CsvColumnReference reference)
        {
            reference = null;
            raw = raw?.Trim() ?? string.Empty;
            bool isArray = false;
            if (CsvColumnTypes.TryGetArrayElementType(raw, out string arrayElement))
            {
                raw = arrayElement;
                isArray = true;
            }

            if (!CsvColumnTypes.IsReferenceTypeToken(raw))
                return false;

            string target = raw.Substring(3).Trim();
            int dot = target.LastIndexOf('.');
            if (dot <= 0 || dot >= target.Length - 1)
            {
                throw new InvalidOperationException(
                    $"참조 타입 '{raw}' 형식이 올바르지 않습니다. (예: ref CharacterStat.Speed)");
            }

            string tableName = target.Substring(0, dot).Trim();
            string columnName = target.Substring(dot + 1).Trim();
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
            {
                throw new InvalidOperationException(
                    $"참조 타입 '{raw}' 형식이 올바르지 않습니다. (예: ref CharacterStat.Speed)");
            }

            reference = new CsvColumnReference(tableName, columnName, isArray);
            return true;
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

        internal static string InferPrimitiveType(string value) =>
            CsvColumnTypes.InferPrimitiveFromValue(value);

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

    }
}
