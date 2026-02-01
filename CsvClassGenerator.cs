using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public static class CsvClassGenerator
{
    private class EnumInfo
    {
        public string Name;
        public HashSet<string> Values = new HashSet<string>();
    }

    private static Dictionary<string, EnumInfo> enumMap = new();

    public static void GenerateAndSave(string csvPath, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        string className = Path.GetFileNameWithoutExtension(csvPath);

        string outputPath = Path.Combine(outputDir, className + ".cs");
        string generatedClass = GenerateClass(csvPath);

        File.WriteAllText(outputPath, generatedClass, Encoding.UTF8);
    }
    public static string GenerateClass(string csvPath)
    {
        enumMap.Clear();

        var lines = ReadAllLinesSafe(csvPath);
        if (lines.Length < 2)
            throw new Exception("CSV must have header and at least one data row.");

        string className = SanitizeIdentifier(
            Path.GetFileNameWithoutExtension(csvPath));

        string[] headers = lines[0].Split(',');
        string[] values = lines[1].Split(',');

        var sb = new StringBuilder();

        string[] fieldTypes = new string[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            fieldTypes[i] = InferType(values[i], headers[i]);

        sb.AppendLine("namespace Skddkkkk.Data");
        sb.AppendLine("{");

        foreach (var enumInfo in enumMap.Values)
        {
            sb.AppendLine($"    public enum {enumInfo.Name}");
            sb.AppendLine("    {");
            foreach (var v in enumInfo.Values)
                sb.AppendLine($"        {v},");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("    [System.Serializable]");
        sb.AppendLine($"    public class {className} : IDataRecord");
        sb.AppendLine("    {");
        for (int i = 0; i < headers.Length; i++)
            sb.AppendLine($"        public {fieldTypes[i]} {SanitizeIdentifier(headers[i])} {{ get; set; }}");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }
    private static string InferType(string value, string fieldName)
    {
        value = value.Trim();

        if (value.StartsWith("\""))
            return "string";

        if (fieldName.StartsWith("E") && char.IsUpper(fieldName[0]))
        {
            RegisterEnum(fieldName, value);
            return fieldName;
        }

        if (int.TryParse(value, out _)) return "int";
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return "float";
        if (bool.TryParse(value, out _)) return "bool";

        throw new Exception($"Invalid value: {value}");
    }

    private static void RegisterEnum(string enumName, string value)
    {
        if (!enumMap.TryGetValue(enumName, out var info))
            enumMap[enumName] = info = new EnumInfo { Name = enumName };

        info.Values.Add(SanitizeIdentifier(value));
    }

    private static string[] ReadAllLinesSafe(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs, Encoding.UTF8);

        var list = new List<string>();
        while (!sr.EndOfStream)
            list.Add(sr.ReadLine());

        return list.ToArray();
    }

    public static bool ValidateCsv(string csvPath, out string error)
    {
        error = "";

        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            error = "CSV 파일 경로가 유효하지 않습니다.";
            return false;
        }

        try
        {
            enumMap.Clear();

            var lines = ReadAllLinesSafe(csvPath);
            if (lines.Length < 2)
                throw new Exception("CSV는 헤더와 최소 1줄의 데이터가 필요합니다.");

            string[] headers = lines[0].Split(',');
            string[] values = lines[1].Split(',');

            if (headers.Length != values.Length)
                throw new Exception("헤더 개수와 데이터 개수가 일치하지 않습니다.");

            for (int i = 0; i < headers.Length; i++)
                InferType(values[i], headers[i]);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string SanitizeIdentifier(string s)
    {
        var sb = new StringBuilder();

        if (!char.IsLetter(s[0]) && s[0] != '_')
            sb.Append('_');

        foreach (var c in s)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');

        return sb.ToString();
    }
}
