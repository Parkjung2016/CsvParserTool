using System.IO;

namespace CSVParserTool.Exporting
{
    internal static class UnrealGeneratedSourceLayout
    {
        public static string GetUnifiedDirectory(string moduleRoot) =>
            Path.Combine(moduleRoot, "DataTables", "Generated");
    }
}