using System;
using System.Collections.Generic;
using System.IO;
using CSVParserTool;
using CSVParserTool.Exporting;

namespace DataTool.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            NetFxAssemblyLoadFix.Register();

            if (args == null || args.Length == 0)
            {
                PrintHelp();
                return 0;
            }

            if (IsHelp(args[0]))
            {
                PrintHelp();
                return 0;
            }

            string verb = args[0].ToLowerInvariant();
            if (verb == "export")
                return RunExport(ParseArgs(args, 1));

            Console.Error.WriteLine($"Unknown command: {args[0]}");
            PrintHelp();
            return 1;
        }

        private static bool IsHelp(string a) =>
            string.Equals(a, "help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase);

        private static void PrintHelp()
        {
            Console.WriteLine("DataTool CLI — GUI와 동일한 검증·Export 파이프라인");
            Console.WriteLine();
            Console.WriteLine("사용법:");
            Console.WriteLine("  DataTool.exe export --project <프로젝트 루트> [옵션]");
            Console.WriteLine();
            Console.WriteLine("옵션:");
            Console.WriteLine("  --project <경로>         Unity 또는 Unreal 프로젝트 루트 (필수)");
            Console.WriteLine("  --engine unity|unreal    Export 엔진 (기본값: unity)");
            Console.WriteLine("  --excel <경로>           DT_*.xlsx 원본 폴더");
            Console.WriteLine("  --refresh-xlsx           XLSX 원본 검사 및 원본 없는 산출물 정리 실행");
            Console.WriteLine("  --version <버전>         이 버전 이하의 컬럼만 포함 (예: 1.0.0, 생략 시 전체)");
            Console.WriteLine("  --no-orphan-cleanup      원본 XLSX가 없는 기존 산출물 유지");
            Console.WriteLine("  --no-unreal-import       Unreal C++만 생성하고 컴파일·UDataTable Import 생략");
            Console.WriteLine("  -h, --help               도움말 표시");
            Console.WriteLine();
            Console.WriteLine("Unity 예시:");
            Console.WriteLine("  DataTool.exe export --engine unity --project C:\\Game\\MyUnityProject --excel C:\\Data\\Xlsx --refresh-xlsx --version 1.0.0");
            Console.WriteLine();
            Console.WriteLine("Unreal 예시:");
            Console.WriteLine("  DataTool.exe export --engine unreal --project C:\\Game\\MyUnrealProject --excel C:\\Data\\Xlsx --refresh-xlsx --version 1.0.0");
            Console.WriteLine();
            Console.WriteLine("종료 코드: 0=성공, 1=옵션·검증·Export 실패");
        }
        private static Dictionary<string, string> ParseArgs(string[] args, int start)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = start; i < args.Length; i++)
            {
                string a = args[i];
                if (!a.StartsWith("--", StringComparison.Ordinal))
                    continue;
                string key = a.Substring(2);
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    d[key] = "true";
                else
                {
                    d[key] = args[i + 1].Trim();
                    i++;
                }
            }

            return d;
        }

        private static int RunExport(Dictionary<string, string> opt)
        {
            if (!opt.TryGetValue("project", out string project) || string.IsNullOrWhiteSpace(project))
            {
                Console.Error.WriteLine("Missing --project");
                return 1;
            }

            opt.TryGetValue("excel", out string excel);
            opt.TryGetValue("version", out string exportVersion);
            bool refresh = opt.ContainsKey("refresh-xlsx");
            bool removeOrphanArtifacts = !opt.ContainsKey("no-orphan-cleanup");
            bool autoImportUnrealDataTables = !opt.ContainsKey("no-unreal-import");
            opt.TryGetValue("engine", out string engine);
            ExportPlatform platform;
            if (string.IsNullOrWhiteSpace(engine)
                || string.Equals(engine, "unity", StringComparison.OrdinalIgnoreCase))
            {
                platform = ExportPlatform.Unity;
            }
            else if (string.Equals(engine, "unreal", StringComparison.OrdinalIgnoreCase))
            {
                platform = ExportPlatform.Unreal;
            }
            else
            {
                Console.Error.WriteLine("Invalid --engine value. Use unity or unreal: " + engine);
                return 1;
            }

            var result = DataExportService.RunExport(
                project,
                excel,
                refresh,
                line => Console.WriteLine(line),
                exportVersion: exportVersion,
                removeOrphanArtifacts: removeOrphanArtifacts,
                exportPlatform: platform,
                autoImportUnrealDataTables: autoImportUnrealDataTables);

            if (!result.Ok)
            {
                Console.Error.WriteLine(result.ErrorMessage);
                return 1;
            }

            Console.WriteLine(result.SummaryLines);
            return 0;
        }
    }
}
