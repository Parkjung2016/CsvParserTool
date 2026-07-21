using System;
using System.Collections.Generic;

namespace CSVParserTool
{
    /// <summary>전체 테이블의 ref Table.Column 스키마를 검증하고 Id를 실제 값으로 치환한다.</summary>
    public static class CrossTableReferenceResolver
    {
        private sealed class Context
        {
            public readonly IReadOnlyList<CsvTableParseResult> Tables;
            public readonly Dictionary<string, CsvTableParseResult> TablesByClass;
            public readonly Dictionary<CsvTableParseResult, Dictionary<string, int>> IdIndexes = new();
            public readonly Dictionary<string, HashSet<string>> ColumnValueIndexes = new(StringComparer.OrdinalIgnoreCase);

            public Context(IReadOnlyList<CsvTableParseResult> tables)
            {
                Tables = tables;
                TablesByClass = new Dictionary<string, CsvTableParseResult>(StringComparer.OrdinalIgnoreCase);

                foreach (CsvTableParseResult table in tables)
                {
                    if (table == null)
                        continue;

                    if (TablesByClass.ContainsKey(table.ClassName))
                        throw new InvalidOperationException($"테이블 이름이 중복됩니다: {DisplayTable(table)}");

                    TablesByClass.Add(table.ClassName, table);
                }
            }
        }

        public static void Resolve(IReadOnlyList<CsvTableParseResult> tables)
        {
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));

            var context = new Context(tables);
            var resolvingTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolvedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CsvTableParseResult table in tables)
            {
                for (int column = 0; column < table.Headers.Length; column++)
                    ResolveColumnType(context, table, column, resolvingTypes, resolvedTypes);
            }

            foreach (CsvTableParseResult table in tables)
            {
                for (int row = 0; row < table.DataRows.Count; row++)
                {
                    for (int column = 0; column < table.Headers.Length; column++)
                    {
                        if (GetReference(table, column) == null)
                            continue;

                        table.DataRows[row][column] = ResolveCellValue(
                            context,
                            table,
                            row,
                            column,
                            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    }
                }
            }
        }

        private static string ResolveColumnType(
            Context context,
            CsvTableParseResult table,
            int column,
            HashSet<string> resolving,
            HashSet<string> resolved)
        {
            string key = table.ClassName + ":" + column;
            if (resolved.Contains(key))
                return table.ColumnTypes[column];

            CsvColumnReference reference = GetReference(table, column);
            if (reference == null)
            {
                resolved.Add(key);
                return table.ColumnTypes[column];
            }

            if (column == CsvTableParser.FindIdColumnIndex(table.Headers))
                throw new InvalidOperationException($"{DisplayTable(table)}.Id는 참조 컬럼으로 지정할 수 없습니다.");

            if (!resolving.Add(key))
                throw new InvalidOperationException($"순환 참조가 있습니다: {DisplayTable(table)}.{table.Headers[column]}");

            CsvTableParseResult targetTable = FindTable(context, table, column, reference);
            int targetColumn = FindTargetColumn(targetTable, reference.ColumnName, table, column);
            reference = new CsvColumnReference(
                targetTable.ClassName,
                targetTable.Headers[targetColumn],
                reference.IsArray,
                reference.IsValidationOnly);
            table.ColumnReferences[column] = reference;
            string targetType = ResolveColumnType(context, targetTable, targetColumn, resolving, resolved);
            if (reference.IsArray)
            {
                if (CsvColumnTypes.TryGetArrayElementType(targetType, out _))
                    throw new InvalidOperationException(
                        $"{DisplayTable(table)}.{table.Headers[column]}: 배열 참조의 대상 컬럼은 배열일 수 없습니다.");
                targetType += "[]";
            }

            table.ColumnTypes[column] = targetType;
            RegisterReferencedEnum(context, table, targetType);
            resolving.Remove(key);
            resolved.Add(key);
            return targetType;
        }

        private static string ResolveCellValue(
            Context context,
            CsvTableParseResult sourceTable,
            int sourceRow,
            int sourceColumn,
            HashSet<string> resolving)
        {
            CsvColumnReference reference = GetReference(sourceTable, sourceColumn);
            if (reference == null)
                return Cell(sourceTable, sourceRow, sourceColumn);

            string cellKey = sourceTable.ClassName + ":" + sourceRow + ":" + sourceColumn;
            if (!resolving.Add(cellKey))
                throw new InvalidOperationException($"순환 참조가 있습니다: {SourceLocation(sourceTable, sourceRow, sourceColumn)}");

            CsvTableParseResult targetTable = FindTable(context, sourceTable, sourceColumn, reference);
            int targetColumn = FindTargetColumn(targetTable, reference.ColumnName, sourceTable, sourceColumn);
            string sourceValue = Cell(sourceTable, sourceRow, sourceColumn);
            if (reference.IsValidationOnly)
            {
                ValidateReferenceValues(
                    context,
                    sourceTable,
                    sourceRow,
                    sourceColumn,
                    targetTable,
                    targetColumn,
                    reference,
                    sourceValue);
                resolving.Remove(cellKey);
                return sourceValue;
            }

            if (reference.IsArray)
            {
                string[] referenceIds = CsvColumnTypes.SplitArrayCell(sourceValue);
                var values = new List<string>(referenceIds.Length);
                foreach (string referenceId in referenceIds)
                {
                    values.Add(ResolveReferenceId(
                        context,
                        sourceTable,
                        sourceRow,
                        sourceColumn,
                        targetTable,
                        targetColumn,
                        NormalizeKey(referenceId),
                        resolving));
                }

                resolving.Remove(cellKey);
                return string.Join("|", values);
            }

            string value = ResolveReferenceId(
                context,
                sourceTable,
                sourceRow,
                sourceColumn,
                targetTable,
                targetColumn,
                NormalizeKey(sourceValue),
                resolving);
            resolving.Remove(cellKey);
            return value;
        }

        private static string ResolveReferenceId(
            Context context,
            CsvTableParseResult sourceTable,
            int sourceRow,
            int sourceColumn,
            CsvTableParseResult targetTable,
            int targetColumn,
            string referenceId,
            HashSet<string> resolving)
        {
            if (string.IsNullOrEmpty(referenceId))
            {
                throw new InvalidOperationException(
                    $"{SourceLocation(sourceTable, sourceRow, sourceColumn)}: 참조 Id가 비어 있습니다. ({DisplayTable(targetTable)}.{targetTable.Headers[targetColumn]})");
            }

            Dictionary<string, int> targetIds = GetIdIndex(context, targetTable);
            if (!targetIds.TryGetValue(referenceId, out int targetRow))
            {
                throw new InvalidOperationException(
                    $"{SourceLocation(sourceTable, sourceRow, sourceColumn)}: {DisplayTable(targetTable)}에서 Id '{referenceId}'을(를) 찾을 수 없습니다. ({targetTable.Headers[targetColumn]} 참조)");
            }

            return ResolveCellValue(context, targetTable, targetRow, targetColumn, resolving);
        }

        private static void ValidateReferenceValues(
            Context context,
            CsvTableParseResult sourceTable,
            int sourceRow,
            int sourceColumn,
            CsvTableParseResult targetTable,
            int targetColumn,
            CsvColumnReference reference,
            string sourceValue)
        {
            string[] values = reference.IsArray
                ? CsvColumnTypes.SplitArrayCell(sourceValue)
                : new[] { NormalizeKey(sourceValue) };

            foreach (string rawValue in values)
            {
                string value = NormalizeKey(rawValue);
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException(
                        $"{SourceLocation(sourceTable, sourceRow, sourceColumn)}: 검증할 참조 값이 비어 있습니다. " +
                        $"({DisplayTable(targetTable)}.{targetTable.Headers[targetColumn]} 존재 검증)");
                }

                if (!ContainsTargetValue(context, targetTable, targetColumn, value))
                {
                    throw new InvalidOperationException(
                        $"{SourceLocation(sourceTable, sourceRow, sourceColumn)}: " +
                        $"{DisplayTable(targetTable)}.{targetTable.Headers[targetColumn]}에서 값 '{value}'을(를) 찾을 수 없습니다. " +
                        "(keyref 존재 검증)");
                }
            }
        }

        private static bool ContainsTargetValue(
            Context context,
            CsvTableParseResult targetTable,
            int targetColumn,
            string value)
        {
            int idColumn = CsvTableParser.FindIdColumnIndex(targetTable.Headers);
            if (targetColumn == idColumn)
                return GetIdIndex(context, targetTable).ContainsKey(value);

            string cacheKey = targetTable.ClassName + ":" + targetColumn;
            if (!context.ColumnValueIndexes.TryGetValue(cacheKey, out HashSet<string> values))
            {
                values = new HashSet<string>(StringComparer.Ordinal);
                for (int row = 0; row < targetTable.DataRows.Count; row++)
                {
                    string candidate = NormalizeKey(ResolveCellValue(
                        context,
                        targetTable,
                        row,
                        targetColumn,
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
                    if (!string.IsNullOrEmpty(candidate))
                        values.Add(candidate);
                }
                context.ColumnValueIndexes.Add(cacheKey, values);
            }

            return values.Contains(value);
        }

        private static CsvTableParseResult FindTable(
            Context context,
            CsvTableParseResult sourceTable,
            int sourceColumn,
            CsvColumnReference reference)
        {
            string className = CsvClassGenerator.DataRecordClassNameFromFileBaseName(reference.TableName);
            if (context.TablesByClass.TryGetValue(className, out CsvTableParseResult table))
                return table;

            throw new InvalidOperationException(
                $"{DisplayTable(sourceTable)}.{sourceTable.Headers[sourceColumn]}: 참조 테이블 '{reference.TableName}'을(를) 찾을 수 없습니다.");
        }

        private static int FindTargetColumn(
            CsvTableParseResult targetTable,
            string targetColumnName,
            CsvTableParseResult sourceTable,
            int sourceColumn)
        {
            int found = -1;
            for (int i = 0; i < targetTable.Headers.Length; i++)
            {
                if (!string.Equals(targetTable.Headers[i]?.Trim(), targetColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (found >= 0)
                {
                    throw new InvalidOperationException(
                        $"{DisplayTable(targetTable)}에 '{targetColumnName}' 컬럼이 중복됩니다.");
                }

                found = i;
            }

            if (found >= 0)
                return found;

            throw new InvalidOperationException(
                $"{DisplayTable(sourceTable)}.{sourceTable.Headers[sourceColumn]}: {DisplayTable(targetTable)}에 '{targetColumnName}' 컬럼이 없습니다.");
        }

        private static Dictionary<string, int> GetIdIndex(Context context, CsvTableParseResult table)
        {
            if (context.IdIndexes.TryGetValue(table, out Dictionary<string, int> cached))
                return cached;

            int idColumn = CsvTableParser.FindIdColumnIndex(table.Headers);
            if (idColumn < 0)
                throw new InvalidOperationException($"{DisplayTable(table)}에 Id 컬럼이 없습니다.");
            if (GetReference(table, idColumn) != null)
                throw new InvalidOperationException($"{DisplayTable(table)}.Id는 참조 컬럼으로 지정할 수 없습니다.");

            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int row = 0; row < table.DataRows.Count; row++)
            {
                string id = NormalizeKey(Cell(table, row, idColumn));
                if (string.IsNullOrEmpty(id))
                    continue;

                if (result.ContainsKey(id))
                    throw new InvalidOperationException($"{DisplayTable(table)}에 Id '{id}'이(가) 중복됩니다.");

                result.Add(id, row);
            }

            context.IdIndexes.Add(table, result);
            return result;
        }

        private static void RegisterReferencedEnum(Context context, CsvTableParseResult table, string columnType)
        {
            if (CsvColumnTypes.TryGetArrayElementType(columnType, out string elementType))
                columnType = elementType;

            foreach (CsvTableParseResult candidate in context.Tables)
            {
                if (candidate.EnumMembers.TryGetValue(columnType, out IReadOnlyList<string> members))
                {
                    table.RegisterReferencedEnum(columnType, members);
                    return;
                }
            }
        }

        private static CsvColumnReference GetReference(CsvTableParseResult table, int column) =>
            table.ColumnReferences != null && column >= 0 && column < table.ColumnReferences.Length
                ? table.ColumnReferences[column]
                : null;

        private static string Cell(CsvTableParseResult table, int row, int column)
        {
            if (row < 0 || row >= table.DataRows.Count)
                return string.Empty;

            string[] cells = table.DataRows[row];
            return cells != null && column >= 0 && column < cells.Length ? cells[column] ?? string.Empty : string.Empty;
        }

        private static string NormalizeKey(string value)
        {
            value = value?.Trim() ?? string.Empty;
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                value = value.Substring(1, value.Length - 2).Replace(new string('"', 2), new string('"', 1));
            return value;
        }

        private static string SourceLocation(CsvTableParseResult table, int row, int column)
        {
            int idColumn = CsvTableParser.FindIdColumnIndex(table.Headers);
            string id = idColumn >= 0 ? NormalizeKey(Cell(table, row, idColumn)) : (row + 1).ToString();
            return $"{DisplayTable(table)}[Id={id}].{table.Headers[column]}";
        }

        private static string DisplayTable(CsvTableParseResult table)
        {
            string name = table?.ClassName ?? "UnknownData";
            if (name.EndsWith("Data", StringComparison.OrdinalIgnoreCase) && name.Length > 4)
                name = name.Substring(0, name.Length - 4);
            return "DT_" + name;
        }
    }
}
