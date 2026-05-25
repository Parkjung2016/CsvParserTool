using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSVParserTool;

public static class CsvClassGenerator
{
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

        return CsvTableParser.SanitizeIdentifier(body + "Data");
    }

    /// <summary>XLSX 첫 시트를 임시 CSV로 내보낸 뒤 <see cref="GenerateTableContainerFile(string,string)"/> 와 동일.</summary>
    public static string GenerateTableContainerFileFromXlsx(string xlsxPath)
    {
        string temp = Path.Combine(Path.GetTempPath(), "DataToolCsv_" + Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            XlsxToCsvConverter.ExportFirstWorksheetToCsv(xlsxPath, temp);
            string sheetName = Path.GetFileNameWithoutExtension(xlsxPath);
            return GenerateTableContainerFile(temp, string.IsNullOrWhiteSpace(sheetName) ? "Sheet" : sheetName);
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

    /// <summary>UI 미리보기 전용: XLSX 앞부분만 읽어 빠르게 코드 스케치를 생성합니다.</summary>
    public static string GeneratePreviewFromXlsxFast(string xlsxPath, int maxRows = 64)
    {
        string csv = XlsxToCsvConverter.ConvertXlsxToCsvString(xlsxPath, maxRows);
        string[] lines = csv
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        lines = lines
            .Where(line => !CsvTableParser.IsDescriptionRow(line.Split(',')))
            .ToArray();

        if (lines.Length < 2)
            throw new InvalidOperationException("Preview requires header and at least one data row.");

        string sheetName = Path.GetFileNameWithoutExtension(xlsxPath);
        string className = DataRecordClassNameFromFileBaseName(string.IsNullOrWhiteSpace(sheetName) ? "Sheet" : sheetName);

        var headers = lines[0].Split(',');
        var firstValues = lines[1].Split(',');
        var fieldTypes = new string[headers.Length];
        var enumMap = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var enumOrder = new List<string>();

        for (int i = 0; i < headers.Length; i++)
        {
            string header = headers[i];
            string value = i < firstValues.Length ? firstValues[i] : string.Empty;

            if (header.StartsWith("E", StringComparison.Ordinal))
            {
                fieldTypes[i] = header;
                if (!enumMap.ContainsKey(header))
                {
                    enumMap[header] = new HashSet<string>(StringComparer.Ordinal);
                    enumOrder.Add(header);
                }
                string id = CsvTableParser.SanitizeIdentifier(value.Trim());
                if (!string.IsNullOrEmpty(id) && id != "_")
                    enumMap[header].Add(id);
            }
            else if (value.IndexOf('|') >= 0)
            {
                string first = value.Split('|')[0];
                fieldTypes[i] = $"List<{CsvTableParser.InferPrimitiveType(first)}>";
            }
            else if (value.StartsWith("\"", StringComparison.Ordinal))
            {
                fieldTypes[i] = "string";
            }
            else
            {
                fieldTypes[i] = CsvTableParser.InferPrimitiveType(value);
            }
        }

        // Preview only: sample enum values from truncated rows.
        for (int row = 1; row < lines.Length; row++)
        {
            string[] values = lines[row].Split(',');
            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i];
                if (!header.StartsWith("E", StringComparison.Ordinal))
                    continue;
                string cell = i < values.Length ? values[i] : string.Empty;
                string id = CsvTableParser.SanitizeIdentifier(cell.Trim());
                if (!string.IsNullOrEmpty(id) && id != "_")
                    enumMap[header].Add(id);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("using MessagePack;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Cysharp.Threading.Tasks;");
        sb.AppendLine("using CsvHelper.Configuration.Attributes;");
        sb.AppendLine();
        sb.AppendLine("namespace PJDev.Data");
        sb.AppendLine("{");

        foreach (string enumName in enumOrder)
        {
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");
            foreach (string v in enumMap[enumName].OrderBy(x => x, StringComparer.Ordinal))
                sb.AppendLine($"        {v},");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("    [MessagePackObject]");
        sb.AppendLine("    [System.Serializable]");
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");
        for (int i = 0; i < headers.Length; i++)
        {
            string field = CsvTableParser.SanitizeIdentifier(headers[i]);
            string type = fieldTypes[i];
            sb.AppendLine($"        [Key({i})]");
            if (type.StartsWith("List<", StringComparison.Ordinal))
            {
                string element = type.Substring("List<".Length);
                element = element.Substring(0, element.Length - 1);
                sb.AppendLine($"        [TypeConverter(typeof(ListConverter<{element}>))]");
            }
            sb.AppendLine($"        public {type} {field} {{ get; set; }}");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public sealed class {className}Container : DataContainerBase<{className}>");
        sb.AppendLine("    {");
        sb.AppendLine("        public UniTask LoadAsync() => DataSheetLoader.LoadAsync(this);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GenerateTableContainerFile(string csvPath, string classNameOverride = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using MessagePack;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Cysharp.Threading.Tasks;");
        sb.AppendLine("using CsvHelper.Configuration.Attributes;");
        sb.AppendLine();
        sb.AppendLine("namespace PJDev.Data");
        sb.AppendLine("{");

        AppendRecordTypeDefinitions(sb, csvPath, classNameOverride, out string className);

        sb.AppendLine();
        sb.AppendLine($"    public sealed class {className}Container : DataContainerBase<{className}>");
        sb.AppendLine("    {");
        sb.AppendLine("        public UniTask LoadAsync() => DataSheetLoader.LoadAsync(this);");
        sb.AppendLine("    }");

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendRecordTypeDefinitions(StringBuilder sb, string csvPath, string classNameOverride, out string className)
    {
        CsvTableParseResult table = CsvTableParser.Parse(csvPath, classNameOverride);
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

            sb.AppendLine($"        [Key({i})]");

            if (type.StartsWith("List<", StringComparison.Ordinal))
            {
                string element = type.Substring("List<".Length);
                element = element.Substring(0, element.Length - 1);
                sb.AppendLine($"        [TypeConverter(typeof(ListConverter<{element}>))]");
            }

            sb.AppendLine($"        public {type} {field} {{ get; set; }}");
        }

        sb.AppendLine("    }");
    }
}
