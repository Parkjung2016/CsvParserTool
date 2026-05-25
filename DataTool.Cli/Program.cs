using System;
using System.Collections.Generic;
using System.IO;
using CSVParserTool;

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
            Console.WriteLine("DataTool — CLI (same pipeline as DataToolGUI)");
            Console.WriteLine();
            Console.WriteLine("  DataTool export --project <dir> [--excel <xlsxSourceDir>] [--refresh-xlsx]");
            Console.WriteLine();
            Console.WriteLine("  --project      Unity project root (do NOT select the Assets folder).");
            Console.WriteLine("  --excel        XLSX source folder (optional; used with --refresh-xlsx).");
            Console.WriteLine("  --refresh-xlsx Run Excel→CSV for all .xlsx in --excel before export.");
            Console.WriteLine("  Exports DT_* → DataTables\\Content\\CSV|Bytes, DataTables\\Scripts.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  DataTool export --project C:\\Game --excel C:\\tables --refresh-xlsx");
            Console.WriteLine("  DataTool export --project C:\\Game");
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
            bool refresh = opt.ContainsKey("refresh-xlsx");

            var result = DataExportService.RunExport(
                project,
                excel,
                refresh,
                line => Console.WriteLine(line));

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
