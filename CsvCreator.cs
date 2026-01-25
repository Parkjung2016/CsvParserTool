using System.IO;
using System.Text;

public static class CsvCreator
{
    public static string CreateNew(string outputDir, string fileNameWithoutExt)
    {
        Directory.CreateDirectory(outputDir);

        string baseName = Path.GetFileNameWithoutExtension(fileNameWithoutExt);

        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "NewData";

        string path;
        int index = 0;

        do
        {
            string fileName = index == 0
                ? $"{baseName}.csv"
                : $"{baseName}_{index}.csv";

            path = Path.Combine(outputDir, fileName);
            index++;
        }
        while (File.Exists(path));

        var sb = new StringBuilder();
        sb.AppendLine("ID,Name,Value");
        sb.AppendLine("0,\"\"\"Sample\"\"\",0");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

        return path;
    }
}
