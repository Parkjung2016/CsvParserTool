using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

                int ok = 0;
                int failed = 0;
                var toolTableClassNames = new List<string>();
                foreach (string csvPath in csvFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(csvPath);
                    if (string.IsNullOrEmpty(fileName))
                        continue;

                    string classFileName = CsvClassGenerator.DataRecordClassNameFromFileBaseName(fileName);

                    try
                    {
                        string legacyRecordCs = Path.Combine(scriptsDir, classFileName + ".cs");
                        if (File.Exists(legacyRecordCs))
                        {
                            File.Delete(legacyRecordCs);
                            log?.Invoke($"Removed legacy script (merged into Container): {legacyRecordCs}");
                        }

                        string csPath = Path.Combine(scriptsDir, classFileName + "Container.cs");
                        File.WriteAllText(csPath, CsvClassGenerator.GenerateTableContainerFile(csvPath), Encoding.UTF8);
                        log?.Invoke($"Script: {csPath}");

                        string legacyCopiedCsv = Path.Combine(dataCsv, classFileName + ".csv");
                        if (File.Exists(legacyCopiedCsv))
                        {
                            File.Delete(legacyCopiedCsv);
                            log?.Invoke($"Removed legacy copied CSV: {legacyCopiedCsv}");
                        }

                        string legacyClassBytePath = Path.Combine(bytesDir, classFileName + ".byte");
                        if (File.Exists(legacyClassBytePath))
                        {
                            File.Delete(legacyClassBytePath);
                            log?.Invoke($"Removed legacy byte file (use DT_ stem): {legacyClassBytePath}");
                        }

                        string legacyStemBytePath = Path.Combine(bytesDir, fileName + ".byte");
                        if (File.Exists(legacyStemBytePath))
                        {
                            File.Delete(legacyStemBytePath);
                            log?.Invoke($"Removed legacy byte extension file (use .bytes): {legacyStemBytePath}");
                        }

                        string bytePath = Path.Combine(bytesDir, fileName + ".bytes");
                        MessagePackTableExporter.ExportToFile(csvPath, bytePath, classNameOverride: null);
                        log?.Invoke($"Bytes: {bytePath}");

                        string legacyNdbPath = Path.Combine(legacyNdbDir, classFileName + ".ndb");
                        if (File.Exists(legacyNdbPath))
                        {
                            File.Delete(legacyNdbPath);
                            log?.Invoke($"Removed legacy NDB: {legacyNdbPath}");
                        }

                        toolTableClassNames.Add(classFileName);
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        log?.Invoke($"Skip {Path.GetFileName(csvPath)}: {ex.Message}");
                    }
                }

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

        private static DataExportResult Fail(string msg) =>
            new DataExportResult { Ok = false, ErrorMessage = msg };

    }
}
