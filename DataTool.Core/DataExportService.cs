using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CSVParserTool
{
    public sealed class DataExportResult
    {
        public bool Ok;
        public string ErrorMessage;
        public string SummaryLines;
    }

    /// <summary>Shared by GUI and CLI: refresh XLSX→CSV and export *Container.cs / deploy CSV / MessagePack .bytes.</summary>
    public static class DataExportService
    {
        public static DataExportResult RunExport(
            string projectRoot,
            string excelSourceFolder,
            bool refreshAllXlsxFromExcelFolder,
            Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
                return Fail("Project root is missing or does not exist.");

            if (refreshAllXlsxFromExcelFolder)
            {
                if (string.IsNullOrWhiteSpace(excelSourceFolder) || !Directory.Exists(excelSourceFolder))
                    return Fail("XLSX source folder is missing; cannot refresh CSV from Excel.");

                try
                {
                    string csvDir = DataProjectPaths.DataCsvDir(projectRoot);
                    Directory.CreateDirectory(csvDir);
                    int n = XlsxToCsvConverter.ConvertAllXlsxInFolder(excelSourceFolder, csvDir, log);
                    log?.Invoke($"Excel → CSV refresh ({n} file(s)).");
                }
                catch (Exception ex)
                {
                    return Fail(ex.Message);
                }
            }

            string dataCsv = DataProjectPaths.DataCsvDir(projectRoot);
            Directory.CreateDirectory(dataCsv);

            try
            {
                string scriptsDir = DataProjectPaths.ScriptsDataDir(projectRoot);
                string bytesDir = DataProjectPaths.DataBytesDir(projectRoot);
                string legacyNdbDir = DataProjectPaths.DataNdbDir(projectRoot);

                Directory.CreateDirectory(scriptsDir);
                Directory.CreateDirectory(dataCsv);
                Directory.CreateDirectory(bytesDir);

                // 소스/실사용 CSV는 DT_*.csv만 유지합니다.
                string[] csvFiles = Directory.GetFiles(dataCsv, "*.csv")
                    .Where(p => Path.GetFileName(p).StartsWith("DT_", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (csvFiles.Length == 0)
                    return Fail("No DT_*.csv files in the work folder.");

                var outcomes = new ConcurrentBag<TableExportOutcome>();
                object logSync = new object();
                void LogLine(string message)
                {
                    if (log == null)
                        return;

                    lock (logSync)
                        log(message);
                }

                ParallelBatchRunner.ForEach(
                    csvFiles,
                    csvPath =>
                    {
                        TableExportOutcome outcome = ExportSingleTable(
                            csvPath,
                            scriptsDir,
                            dataCsv,
                            bytesDir,
                            legacyNdbDir);
                        outcomes.Add(outcome);

                        if (outcome.LogLines == null)
                            return;

                        foreach (string line in outcome.LogLines)
                            LogLine(line);
                    },
                    batchLog: csvFiles.Length > ParallelBatchRunner.DefaultBatchSize
                        ? msg => LogLine(msg)
                        : null);

                var succeeded = outcomes
                    .Where(o => o.Success)
                    .OrderBy(o => o.SourceFileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                int ok = succeeded.Count;
                int failed = outcomes.Count - ok;
                var toolTableClassNames = succeeded.Select(o => o.ClassFileName).ToList();

                if (ok == 0)
                    return Fail(failed > 0 ? "Every CSV failed to export." : "No CSV exported.");

                UnityDataRuntimeGenerator.Write(scriptsDir, toolTableClassNames, log);

                return new DataExportResult
                {
                    Ok = true,
                    SummaryLines =
                        $"Done. ({ok} table(s)" + (failed > 0 ? $", {failed} skipped" : "") + ")\r\n" +
                        "· Scripts: Assets\\_Game\\DataTables\\Scripts\r\n" +
                        "· Content: Assets\\_Game\\DataTables\\Content\\CSV · Bytes (DT_*)\r\n" +
                        "· Unity: PJDev.Data.asmdef + ToolGenerated.g.cs + *Container.cs + MessagePackGenerated.cs (mpc)\r\n" +
                        "  (mpc 실패 시 fallback — 솔루션 루트에서 dotnet tool restore)"
                };
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        private sealed class TableExportOutcome
        {
            public bool Success;
            public string ClassFileName;
            public string SourceFileName;
            public List<string> LogLines = new List<string>();
            public string ErrorMessage;
        }

        private static TableExportOutcome ExportSingleTable(
            string csvPath,
            string scriptsDir,
            string dataCsv,
            string bytesDir,
            string legacyNdbDir)
        {
            var outcome = new TableExportOutcome
            {
                SourceFileName = Path.GetFileName(csvPath)
            };

            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            if (string.IsNullOrEmpty(fileName))
                return outcome;

            string classFileName = CsvClassGenerator.DataRecordClassNameFromFileBaseName(fileName);

            try
            {
                string legacyRecordCs = Path.Combine(scriptsDir, classFileName + ".cs");
                if (File.Exists(legacyRecordCs))
                {
                    File.Delete(legacyRecordCs);
                    outcome.LogLines.Add($"Removed legacy script (merged into Container): {legacyRecordCs}");
                }

                string csPath = Path.Combine(scriptsDir, classFileName + "Container.cs");
                File.WriteAllText(csPath, CsvClassGenerator.GenerateTableContainerFile(csvPath), Encoding.UTF8);
                outcome.LogLines.Add($"Script: {csPath}");

                string legacyCopiedCsv = Path.Combine(dataCsv, classFileName + ".csv");
                if (File.Exists(legacyCopiedCsv))
                {
                    File.Delete(legacyCopiedCsv);
                    outcome.LogLines.Add($"Removed legacy copied CSV: {legacyCopiedCsv}");
                }

                string legacyClassBytePath = Path.Combine(bytesDir, classFileName + ".byte");
                if (File.Exists(legacyClassBytePath))
                {
                    File.Delete(legacyClassBytePath);
                    outcome.LogLines.Add($"Removed legacy byte file (use DT_ stem): {legacyClassBytePath}");
                }

                string legacyStemBytePath = Path.Combine(bytesDir, fileName + ".byte");
                if (File.Exists(legacyStemBytePath))
                {
                    File.Delete(legacyStemBytePath);
                    outcome.LogLines.Add($"Removed legacy byte extension file (use .bytes): {legacyStemBytePath}");
                }

                string bytePath = Path.Combine(bytesDir, fileName + ".bytes");
                MessagePackTableExporter.ExportToFile(csvPath, bytePath, classNameOverride: null);
                outcome.LogLines.Add($"Bytes: {bytePath}");

                string legacyNdbPath = Path.Combine(legacyNdbDir, classFileName + ".ndb");
                if (File.Exists(legacyNdbPath))
                {
                    File.Delete(legacyNdbPath);
                    outcome.LogLines.Add($"Removed legacy NDB: {legacyNdbPath}");
                }

                outcome.ClassFileName = classFileName;
                outcome.Success = true;
            }
            catch (Exception ex)
            {
                outcome.ErrorMessage = ex.Message;
                outcome.LogLines.Add($"Skip {Path.GetFileName(csvPath)}: {ex.Message}");
            }

            return outcome;
        }

        private static DataExportResult Fail(string msg) =>
            new DataExportResult { Ok = false, ErrorMessage = msg };

    }
}
