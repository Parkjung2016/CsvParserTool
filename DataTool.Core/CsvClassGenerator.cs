using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CSVParserTool;

public static class CsvClassGenerator
{
    private const string IfUniTask = "#if UNITASK_INSTALLED";
    private const string ElseNoUniTask = "#else";
    private const string EndIf = "#endif";
    public static string DataRecordClassNameFromFileBaseName(string fileNameWithoutExtension)
    {
        string raw = fileNameWithoutExtension?.Trim() ?? "Sheet";

        if (raw.StartsWith("DT_", StringComparison.OrdinalIgnoreCase))
        {
            raw = raw.Substring(3).Trim();
            if (string.IsNullOrEmpty(raw))
                raw = "Sheet";
        }

        string body = CsvTableParser.SanitizeIdentifier(raw);
        if (string.IsNullOrEmpty(body) || body == "_")
            body = "Sheet";

        // DT_CharacterStatData.xlsx → CharacterStatData (Data 접미사 중복 방지)
        if (body.EndsWith("Data", StringComparison.OrdinalIgnoreCase) && body.Length > 4)
            body = body.Substring(0, body.Length - 4);

        return CsvTableParser.SanitizeIdentifier(body + "Data");
    }

    /// <summary>XLSX 첫 시트 → <see cref="CsvTableParseResult"/> (버전·타입 행 포함 원본 기준).</summary>
    public static CsvTableParseResult ParseTableFromXlsx(
        string xlsxPath,
        string fileNameWithoutExtension = null,
        CsvParseOptions options = null)
    {
        if (string.IsNullOrWhiteSpace(xlsxPath) || !File.Exists(xlsxPath))
            throw new FileNotFoundException("XLSX not found.", xlsxPath);

        string temp = Path.Combine(Path.GetTempPath(), "DataToolCsv_" + Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            XlsxToCsvConverter.ExportFirstWorksheetToCsv(xlsxPath, temp);
            string stem = string.IsNullOrWhiteSpace(fileNameWithoutExtension)
                ? Path.GetFileNameWithoutExtension(xlsxPath)
                : fileNameWithoutExtension;
            string className = DataRecordClassNameFromFileBaseName(stem);
            return CsvTableParser.Parse(temp, className, options);
        }
        finally
        {
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch
            {
            }
        }
    }

    /// <summary>XLSX 첫 시트를 임시 CSV로 내보낸 뒤 <see cref="GenerateTableContainerFile(CsvTableParseResult)"/> 와 동일.</summary>
    public static string GenerateTableContainerFileFromXlsx(string xlsxPath)
    {
        string stem = Path.GetFileNameWithoutExtension(xlsxPath);
        return GenerateTableContainerFile(ParseTableFromXlsx(xlsxPath, stem));
    }

    /// <summary>UI 미리보기 전용: 선택 테이블만 빠르게 읽어 코드를 생성합니다.</summary>
    public static string GeneratePreviewFromXlsxFast(string xlsxPath, int maxRows = 64, string exportVersion = null)
    {
        var options = CreatePreviewParseOptions(exportVersion);
        return GenerateTableContainerFile(ParsePreviewTableFromXlsx(xlsxPath, maxRows, options));
    }

    /// <summary>선택 테이블과 참조되는 모든 XLSX를 읽고 Export와 동일한 참조 검사를 수행합니다.</summary>
    public static string GenerateValidatedPreviewFromXlsx(
        string xlsxPath,
        string xlsxFolder,
        string exportVersion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(xlsxPath) || !File.Exists(xlsxPath))
            throw new FileNotFoundException("XLSX not found.", xlsxPath);

        string folder = !string.IsNullOrWhiteSpace(xlsxFolder) && Directory.Exists(xlsxFolder)
            ? xlsxFolder
            : Path.GetDirectoryName(xlsxPath);
        var options = CreatePreviewParseOptions(exportVersion);
        var workbookByClass = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
        {
            foreach (string path in Directory.GetFiles(folder, "*.xlsx"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal)
                    || EnumCatalogService.IsCatalogPath(path))
                    continue;

                string className = DataRecordClassNameFromFileBaseName(Path.GetFileNameWithoutExtension(path));
                if (workbookByClass.TryGetValue(className, out string existing)
                    && !string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"같은 클래스 이름으로 생성되는 XLSX가 중복됩니다: {Path.GetFileName(existing)}, {Path.GetFileName(path)} ({className})");
                }

                workbookByClass[className] = path;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        CsvTableParseResult selected = ParsePreviewTableFromXlsx(xlsxPath, 64, options);
        if (!selected.ColumnReferences.Any(reference => reference != null))
            return GenerateTableContainerFile(selected);

        cancellationToken.ThrowIfCancellationRequested();
        selected = ParsePreviewTableFromXlsx(xlsxPath, int.MaxValue, options);
        var tablesByClass = new Dictionary<string, CsvTableParseResult>(StringComparer.OrdinalIgnoreCase)
        {
            [selected.ClassName] = selected
        };
        var pending = new Queue<CsvTableParseResult>();
        pending.Enqueue(selected);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CsvTableParseResult table = pending.Dequeue();
            foreach (CsvColumnReference reference in table.ColumnReferences ?? Array.Empty<CsvColumnReference>())
            {
                if (reference == null)
                    continue;

                string targetClass = DataRecordClassNameFromFileBaseName(reference.TableName);
                if (tablesByClass.ContainsKey(targetClass)
                    || !workbookByClass.TryGetValue(targetClass, out string targetPath))
                    continue;

                CsvTableParseResult target = ParsePreviewTableFromXlsx(targetPath, int.MaxValue, options);
                tablesByClass.Add(target.ClassName, target);
                pending.Enqueue(target);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        List<CsvTableParseResult> tables = tablesByClass.Values.ToList();
        CrossTableReferenceResolver.Resolve(tables);

        string enumWorkbook = EnumCatalogService.FindWorkbook(folder);
        if (!string.IsNullOrEmpty(enumWorkbook))
            EnumCatalogService.ApplyToTables(EnumCatalogService.ParseXlsx(enumWorkbook), tables);

        cancellationToken.ThrowIfCancellationRequested();
        return GenerateTableContainerFile(selected);
    }

    private static CsvParseOptions CreatePreviewParseOptions(string exportVersion) =>
        string.IsNullOrWhiteSpace(exportVersion)
            ? null
            : new CsvParseOptions { ExportVersion = exportVersion.Trim() };

    private static CsvTableParseResult ParsePreviewTableFromXlsx(
        string xlsxPath,
        int maxRows,
        CsvParseOptions options)
    {
        string csv = XlsxPreviewReader.ReadFirstWorksheetAsCsv(xlsxPath, maxRows);
        string[] lines = csv
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
            throw new InvalidOperationException("Preview requires at least a header row.");

        string sheetName = Path.GetFileNameWithoutExtension(xlsxPath);
        string className = DataRecordClassNameFromFileBaseName(
            string.IsNullOrWhiteSpace(sheetName) ? "Sheet" : sheetName);
        return CsvTableParser.ParseLines(lines, className, options);
    }

    public static string GenerateTableContainerFile(string csvPath, string classNameOverride = null, CsvParseOptions options = null) =>
        GenerateTableContainerFile(CsvTableParser.Parse(csvPath, classNameOverride, options));

    public static string GenerateTableContainerFile(CsvTableParseResult table)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("using MessagePack;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine(IfUniTask);
        sb.AppendLine("using Cysharp.Threading.Tasks;");
        sb.AppendLine(EndIf);
        sb.AppendLine("using CsvHelper.Configuration.Attributes;");
        sb.AppendLine();
        sb.AppendLine("namespace PJDev.Data");
        sb.AppendLine("{");

        AppendRecordTypeDefinitions(sb, table, out string className);

        sb.AppendLine();
        sb.AppendLine($"    public sealed class {className}Container : DataContainerBase<{className}>");
        sb.AppendLine("    {");
        sb.AppendLine(IfUniTask);
        sb.AppendLine("        public UniTask LoadAsync() => DataSheetLoader.LoadAsync(this);");
        sb.AppendLine(ElseNoUniTask);
        sb.AppendLine("        public void Load() => DataSheetLoader.Load(this);");
        sb.AppendLine(EndIf);
        sb.AppendLine("    }");

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendRecordTypeDefinitions(StringBuilder sb, string csvPath, string classNameOverride, out string className)
    {
        CsvTableParseResult table = CsvTableParser.Parse(csvPath, classNameOverride);
        AppendRecordTypeDefinitions(sb, table, out className);
    }

    private static void AppendRecordTypeDefinitions(StringBuilder sb, CsvTableParseResult table, out string className)
    {
        className = table.ClassName;

        foreach (string enumName in table.EnumDeclarationOrder)
        {
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");
            foreach (string v in table.EnumMembers[enumName])
                sb.AppendLine($"        {v},");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("    [MessagePackObject]");
        sb.AppendLine("    [System.Serializable]");
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");

        for (int i = 0; i < table.Headers.Length; i++)
        {
            string field = CsvTableParser.SanitizeIdentifier(table.Headers[i]);
            string type = table.ColumnTypes[i];

            CsvColumnReference reference = table.ColumnReferences[i];
            if (reference != null)
            {
                string referenceSummary = reference.IsValidationOnly
                    ? reference.IsArray ? "여러 값 존재 검증" : "값 존재 검증"
                    : reference.IsArray ? "여러 값 참조" : "참조";
                string referenceClassName = DataRecordClassNameFromFileBaseName(reference.TableName);
                sb.AppendLine($"        /// <summary>{referenceClassName}.{reference.ColumnName} {referenceSummary}</summary>");
            }

            sb.AppendLine($"        [Key({i})]");
            if (CsvColumnTypes.TryGetArrayElementType(type, out string elementType))
            {
                sb.AppendLine($"        [TypeConverter(typeof(PipeSeparatedArrayConverter<{elementType}>))]");
                sb.AppendLine($"        public {type} {field} {{ get; set; }} = System.Array.Empty<{elementType}>();");
            }
            else
            {
                sb.AppendLine($"        public {type} {field} {{ get; set; }}");
            }
        }

        sb.AppendLine("    }");
    }
}
