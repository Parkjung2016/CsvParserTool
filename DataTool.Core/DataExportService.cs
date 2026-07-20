using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CSVParserTool
{
    public enum DataExportProgressKind
    {
        PhaseChanged,
        TablesStarted,
        TableCompleted,
        Finished
    }

    public sealed class DataExportProgressInfo
    {
        public DataExportProgressKind Kind;
        public string PhaseLabel;
        /// <summary>0=Excel→CSV, 1=테이블, 2=Unity (없으면 -1)</summary>
        public int PhaseIndex = -1;
        public string ItemName;
        public int CompletedCount;
        public int TotalCount;
        public bool Success;
        public string Message;
        public IReadOnlyList<string> PendingItemNames;
    }

    public sealed class DataExportTableResult
    {
        public string SourceFileName;
        public string ClassFileName;
        public bool Success;
        public string ErrorMessage;
    }

    public sealed class DataExportResult
    {
        public bool Ok;
        public string ErrorMessage;
        public string SummaryLines;
        public IReadOnlyList<DataExportTableResult> TableResults = Array.Empty<DataExportTableResult>();
        public int SucceededCount;
        public int FailedCount;
    }

    /// <summary>Shared by GUI and CLI: refresh XLSX→CSV and export *Container.cs / deploy CSV / MessagePack .bytes.</summary>
    public static class DataExportService
    {
        public static DataExportResult RunExport(
            string projectRoot,
            string excelSourceFolder,
            bool refreshAllXlsxFromExcelFolder,
            Action<string> log,
            Action<DataExportProgressInfo> progress = null,
            string exportVersion = null,
            bool removeOrphanArtifacts = true,
            IReadOnlyCollection<string> selectedTableStems = null)
        {
            void Report(
                DataExportProgressKind kind,
                string phaseLabel = null,
                int phaseIndex = -1,
                string itemName = null,
                int completed = 0,
                int total = 0,
                bool success = false,
                string message = null,
                IReadOnlyList<string> pendingItems = null)
            {
                progress?.Invoke(new DataExportProgressInfo
                {
                    Kind = kind,
                    PhaseLabel = phaseLabel,
                    PhaseIndex = phaseIndex,
                    ItemName = itemName,
                    CompletedCount = completed,
                    TotalCount = total,
                    Success = success,
                    Message = message,
                    PendingItemNames = pendingItems
                });
            }

            bool hasXlsxSource = !string.IsNullOrWhiteSpace(excelSourceFolder) && Directory.Exists(excelSourceFolder);
            HashSet<string> selectedStems = selectedTableStems == null
                ? null
                : new HashSet<string>(
                    selectedTableStems
                        .Where(stem => !string.IsNullOrWhiteSpace(stem))
                        .Select(stem => Path.GetFileNameWithoutExtension(stem.Trim())),
                    StringComparer.OrdinalIgnoreCase);
            bool selectedOnly = selectedStems != null;
            if (selectedOnly && selectedStems.Count == 0)
                return Fail("선택 Export할 테이블이 없습니다.");

            HashSet<string> xlsxDtStems = hasXlsxSource
                ? DataExportSourceFilter.CollectDtStemsFromXlsxFolder(excelSourceFolder)
                : null;

            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
                return Fail("Project root is missing or does not exist.");

            string trimmedExportVersion = exportVersion?.Trim();
            if (!string.IsNullOrEmpty(trimmedExportVersion) && !DataVersion.TryParse(trimmedExportVersion, out _))
                return Fail($"Export 버전 '{trimmedExportVersion}' 형식이 올바르지 않습니다. (예: 1.0.0)");

            if (refreshAllXlsxFromExcelFolder)
            {
                if (string.IsNullOrWhiteSpace(excelSourceFolder) || !Directory.Exists(excelSourceFolder))
                    return Fail("XLSX source folder is missing; cannot refresh CSV from Excel.");

                try
                {
                    Report(DataExportProgressKind.PhaseChanged, phaseLabel: "Excel → CSV 변환", phaseIndex: 0);
                    string csvDir = DataProjectPaths.DataCsvDir(projectRoot);
                    Directory.CreateDirectory(csvDir);
                    int n = XlsxToCsvConverter.ConvertAllXlsxInFolder(
                        excelSourceFolder,
                        csvDir,
                        log,
                        selectedOnly ? selectedStems : null);
                    log?.Invoke($"Excel → CSV refresh ({n} file(s)).");

                    if (removeOrphanArtifacts && xlsxDtStems != null && xlsxDtStems.Count > 0)
                    {
                        int removed = DataExportSourceFilter.RemoveOrphanArtifacts(projectRoot, xlsxDtStems, log);
                        if (removed > 0)
                            log?.Invoke($"원본 XLSX가 없는 이전 출력 파일 {removed}개 삭제.");
                    }
                    else if (!removeOrphanArtifacts && xlsxDtStems != null && xlsxDtStems.Count > 0)
                    {
                        log?.Invoke("원본 XLSX가 없는 이전 출력 파일 삭제: OFF (기존 파일 유지).");
                    }
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
                string enumWorkbookPath = EnumCatalogService.FindWorkbook(excelSourceFolder);
                EnumCatalog enumCatalog = enumWorkbookPath == null
                    ? null
                    : EnumCatalogService.ParseXlsx(enumWorkbookPath);
                string enumCatalogStem = enumWorkbookPath == null
                    ? null
                    : Path.GetFileNameWithoutExtension(enumWorkbookPath);

                Directory.CreateDirectory(scriptsDir);
                Directory.CreateDirectory(dataCsv);
                Directory.CreateDirectory(bytesDir);

                // Export 대상: XLSX 원본 폴더가 있으면 그 목록과 일치하는 DT_*.csv만.
                string[] allCsvFiles = Directory.GetFiles(dataCsv, "*.csv")
                    .Where(p => Path.GetFileName(p).StartsWith("DT_", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                string[] csvFiles;
                if (hasXlsxSource)
                {
                    csvFiles = DataExportSourceFilter.FilterCsvPathsByXlsxStems(allCsvFiles, xlsxDtStems);
                    if (csvFiles.Length == 0 && xlsxDtStems.Count > 0)
                    {
                        return Fail("XLSX 원본 폴더에 테이블은 있지만 CSV가 없습니다. Export 전 Excel→CSV 변환을 실행하세요.");
                    }

                    if (csvFiles.Length < allCsvFiles.Length)
                    {
                        int skipped = allCsvFiles.Length - csvFiles.Length;
                        log?.Invoke($"XLSX에 없는 CSV {skipped}개는 Export에서 제외합니다.");
                    }
                }
                else
                {
                    csvFiles = allCsvFiles
                        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }

                string[] validationCsvFiles = selectedOnly && hasXlsxSource
                    ? xlsxDtStems
                        .Select(stem => Path.Combine(dataCsv, stem + ".csv"))
                        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                    : csvFiles;
                if (selectedOnly)
                {
                    csvFiles = csvFiles
                        .Where(path => selectedStems.Contains(Path.GetFileNameWithoutExtension(path)))
                        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    bool enumCatalogSelected = enumCatalog != null
                        && selectedStems.Contains(enumCatalogStem);
                    if (csvFiles.Length == 0 && !enumCatalogSelected)
                        return Fail("체크한 테이블의 CSV를 찾을 수 없습니다. XLSX 파일 이름과 프로젝트 경로를 확인하세요.");

                    log?.Invoke($"선택 Export: 테이블 {csvFiles.Length}개{(enumCatalogSelected ? " · Enum 관리 포함" : string.Empty)}");
                }

                if (csvFiles.Length == 0 && enumCatalog == null)
                    return Fail("Export할 DT_*.csv가 없습니다. XLSX 원본 폴더를 지정하세요.");

                var pendingNames = csvFiles.Select(Path.GetFileName).ToArray();
                Report(
                    DataExportProgressKind.TablesStarted,
                    phaseLabel: "테이블 Export",
                    phaseIndex: 1,
                    total: csvFiles.Length,
                    pendingItems: pendingNames);

                var outcomes = new ConcurrentBag<TableExportOutcome>();
                object logSync = new object();
                int completedCount = 0;
                void LogLine(string message)
                {
                    if (log == null)
                        return;

                    lock (logSync)
                        log(message);
                }

                var parseOptions = new CsvParseOptions { ExportVersion = trimmedExportVersion };
                var parsedTables = new ConcurrentDictionary<string, CsvTableParseResult>(
                    StringComparer.OrdinalIgnoreCase);
                var parseErrors = new ConcurrentBag<string>();

                ParallelBatchRunner.ForEach(
                    validationCsvFiles,
                    csvPath =>
                    {
                        try
                        {
                            string fileName = Path.GetFileNameWithoutExtension(csvPath);
                            parsedTables[csvPath] = TryParseTableForExport(
                                csvPath,
                                excelSourceFolder,
                                fileName,
                                parseOptions);
                        }
                        catch (Exception ex)
                        {
                            parseErrors.Add($"{Path.GetFileName(csvPath)}: {ex.Message}");
                        }
                    });

                if (!parseErrors.IsEmpty)
                    throw new InvalidOperationException(parseErrors.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).First());

                var parseResults = parsedTables.Values
                    .OrderBy(t => t.ClassName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                // 선택하지 않은 테이블도 읽어 참조 대상과 Id가 올바른지 함께 검증한다.
                CrossTableReferenceResolver.Resolve(parseResults);
                EnumCatalogService.ApplyToTables(enumCatalog, parseResults);
                ParallelBatchRunner.ForEach(
                    csvFiles,
                    csvPath =>
                    {
                        TableExportOutcome outcome = ExportSingleTable(
                            csvPath,
                            parsedTables[csvPath],
                            scriptsDir,
                            dataCsv,
                            bytesDir,
                            legacyNdbDir);
                        outcomes.Add(outcome);

                        if (outcome.LogLines != null)
                        {
                            foreach (string line in outcome.LogLines)
                                LogLine(line);
                        }

                        int done = Interlocked.Increment(ref completedCount);
                        Report(
                            DataExportProgressKind.TableCompleted,
                            phaseLabel: "테이블 Export",
                            phaseIndex: 1,
                            itemName: outcome.SourceFileName,
                            completed: done,
                            total: csvFiles.Length,
                            success: outcome.Success,
                            message: outcome.Success
                                ? "CSV · Script · Bytes"
                                : outcome.ErrorMessage ?? "알 수 없는 오류");
                    },
                    batchLog: csvFiles.Length > ParallelBatchRunner.DefaultBatchSize
                        ? msg => LogLine(msg)
                        : null);

                var tableResults = outcomes
                    .Select(o => new DataExportTableResult
                    {
                        SourceFileName = o.SourceFileName,
                        ClassFileName = o.ClassFileName,
                        Success = o.Success,
                        ErrorMessage = o.ErrorMessage
                    })
                    .OrderBy(o => o.SourceFileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var succeeded = outcomes
                    .Where(o => o.Success)
                    .OrderBy(o => o.SourceFileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                int ok = succeeded.Count;
                int failed = outcomes.Count - ok;
                List<CsvTableParseResult> runtimeTables;
                if (selectedOnly)
                {
                    var selectedClassNames = new HashSet<string>(
                        succeeded.Select(o => o.ClassFileName),
                        StringComparer.OrdinalIgnoreCase);
                    runtimeTables = parseResults
                        .Where(table =>
                            selectedClassNames.Contains(table.ClassName)
                            || File.Exists(Path.Combine(scriptsDir, table.ClassName + "Container.cs")))
                        .ToList();
                }
                else
                {
                    runtimeTables = parseResults;
                }

                var toolTableClassNames = runtimeTables
                    .Select(table => table.ClassName)
                    .ToList();

                if (failed > 0)
                {
                    string errorMessage = failed == csvFiles.Length
                        ? "Every CSV failed to export."
                        : $"{failed} table(s) failed to export. Export aborted.";

                    log?.Invoke(errorMessage);
                    Report(
                        DataExportProgressKind.Finished,
                        phaseLabel: "Export 실패",
                        phaseIndex: 1,
                        completed: ok + failed,
                        total: csvFiles.Length,
                        success: false,
                        message: errorMessage);
                    return new DataExportResult
                    {
                        Ok = false,
                        ErrorMessage = errorMessage,
                        TableResults = tableResults,
                        SucceededCount = ok,
                        FailedCount = failed
                    };
                }

                if (ok == 0 && enumCatalog != null)
                {
                    EnumCatalogService.WriteGeneratedFile(scriptsDir, enumCatalog, log);
                    const string enumSummary =
                        "Enum Export 완료\r\n" +
                        "· Scripts: Assets\\_Game\\DataTables\\Scripts\\DataEnums.ToolGenerated.g.cs";
                    Report(
                        DataExportProgressKind.Finished,
                        phaseLabel: "Enum Export 완료",
                        phaseIndex: 2,
                        completed: 1,
                        total: 1,
                        success: true);
                    return new DataExportResult
                    {
                        Ok = true,
                        SummaryLines = enumSummary,
                        TableResults = tableResults,
                        SucceededCount = 0,
                        FailedCount = 0
                    };
                }

                if (ok == 0)
                {
                    Report(
                        DataExportProgressKind.Finished,
                        phaseLabel: "Export 실패",
                        phaseIndex: 1,
                        completed: 0,
                        total: csvFiles.Length,
                        success: false,
                        message: "No CSV exported.");
                    return new DataExportResult
                    {
                        Ok = false,
                        ErrorMessage = "No CSV exported.",
                        TableResults = tableResults,
                        SucceededCount = ok,
                        FailedCount = failed
                    };
                }

                string generatedEnumPath = Path.Combine(scriptsDir, EnumCatalogService.GeneratedFileName);
                if (enumCatalog != null)
                {
                    EnumCatalogService.WriteGeneratedFile(scriptsDir, enumCatalog, log);
                }
                else if (removeOrphanArtifacts && File.Exists(generatedEnumPath))
                {
                    File.Delete(generatedEnumPath);
                    if (File.Exists(generatedEnumPath + ".meta"))
                        File.Delete(generatedEnumPath + ".meta");
                    log?.Invoke($"Enum 관리 XLSX가 없어 이전 enum 생성 파일을 삭제했습니다: {EnumCatalogService.GeneratedFileName}");
                }

                Report(DataExportProgressKind.PhaseChanged, phaseLabel: "Unity 스크립트 · MessagePack 생성", phaseIndex: 2);
                UnityDataRuntimeGenerator.Write(
                    scriptsDir,
                    toolTableClassNames,
                    runtimeTables.OrderBy(t => t.ClassName, StringComparer.OrdinalIgnoreCase).ToList(),
                    log);

                string summary =
                    $"{(selectedOnly ? "선택 Export" : "전체 Export")} 완료 ({ok}개 테이블)\r\n" +
                    "· Scripts: Assets\\_Game\\DataTables\\Scripts\r\n" +
                    "· Content: Assets\\_Game\\DataTables\\Content\\CSV · Bytes (DT_*)\r\n" +
                    "· Unity: PJDev.Data.asmdef + ToolGenerated.g.cs + *Container.cs + InfoStorage + MessagePackGenerated.cs";

                Report(
                    DataExportProgressKind.Finished,
                    phaseLabel: "Export 완료",
                    phaseIndex: 2,
                    completed: ok,
                    total: csvFiles.Length,
                    success: true);

                return new DataExportResult
                {
                    Ok = true,
                    SummaryLines = summary,
                    TableResults = tableResults,
                    SucceededCount = ok,
                    FailedCount = 0
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
            public CsvTableParseResult ParseResult;
            public List<string> LogLines = new List<string>();
            public string ErrorMessage;
        }

        private static TableExportOutcome ExportSingleTable(
            string csvPath,
            CsvTableParseResult table,
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
                outcome.ParseResult = table;

                string legacyRecordCs = Path.Combine(scriptsDir, classFileName + ".cs");
                if (File.Exists(legacyRecordCs))
                {
                    File.Delete(legacyRecordCs);
                    outcome.LogLines.Add($"Removed legacy script (merged into Container): {legacyRecordCs}");
                }

                string csPath = Path.Combine(scriptsDir, classFileName + "Container.cs");
                File.WriteAllText(csPath, CsvClassGenerator.GenerateTableContainerFile(table), Encoding.UTF8);
                outcome.LogLines.Add($"Script: {csPath}");

                CsvTableParser.WriteDeployedCsv(csvPath, table);
                outcome.LogLines.Add($"CSV: {csvPath}");

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
                MessagePackTableExporter.ExportToFile(table, bytePath);
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

        private static CsvTableParseResult TryParseTableForExport(
            string csvPath,
            string excelSourceFolder,
            string fileName,
            CsvParseOptions parseOptions)
        {
            if (!string.IsNullOrWhiteSpace(excelSourceFolder) && Directory.Exists(excelSourceFolder))
            {
                string xlsxPath = Path.Combine(excelSourceFolder, fileName + ".xlsx");
                if (File.Exists(xlsxPath))
                    return CsvClassGenerator.ParseTableFromXlsx(xlsxPath, fileName, parseOptions);
            }

            return CsvTableParser.Parse(csvPath, classNameOverride: null, parseOptions);
        }

        private static DataExportResult Fail(string msg) =>
            new DataExportResult { Ok = false, ErrorMessage = msg };

    }
}
