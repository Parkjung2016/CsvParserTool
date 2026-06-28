using System;
using System.Collections.Generic;

namespace CSVParserTool
{
    /// <summary>Unity <c>DataTables/Scripts/MessagePackGenerated.cs</c> — 오프라인 내장 생성기로 대체.</summary>
    public static class MpcCodeGenerator
    {
        public const string GeneratedFileName = MessagePackUnityResolverGenerator.GeneratedFileName;
        public const string ResolverTypeName = MessagePackUnityResolverGenerator.ResolverTypeName;

        public static void Generate(
            string scriptsDir,
            IReadOnlyList<CsvTableParseResult> tables,
            Action<string> log)
        {
            MessagePackUnityResolverGenerator.Generate(scriptsDir, tables, log);
        }
    }
}
