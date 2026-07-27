using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CSVParserTool.Exporting;

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
            IReadOnlyCollection<string> selectedTableStems = null,
            ExportPlatform exportPlatform = ExportPlatform.Unity,
            bool autoImportUnrealDataTables = true)
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

            IEngineExportTarget exportTarget = EngineExportTargetRegistry.Get(exportPlatform);
            ExportTargetLayout targetLayout;
            try
            {
                targetLayout = exportTarget.CreateLayout(projectRoot);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }

            if (exportTarget.Platform == ExportPlatform.Unreal)
            {
                IReadOnlyList<UnrealEditorProcessInfo> runningEditors =
                    UnrealEditorProcessGuard.FindRunningEditors(targetLayout.ProjectName);
                if (runningEditors.Count > 0)
                    return Fail("USTRUCT/UENUM 헤더를 안전하게 생성하려면 Unreal Editor를 종료해야 합니다. 실행 중: "
                        + UnrealEditorProcessGuard.Describe(runningEditors));
            }

            string trimmedExportVersion = exportVersion?.Trim();
            if (!string.IsNullOrEmpty(trimmedExportVersion) && !DataVersion.TryParse(trimmedExportVersion, out _))
                return Fail($"Export 버전 '{trimmedExportVersion}' 형식이 올바르지 않습니다. (예: 1.0.0)");

            if (refreshAllXlsxFromExcelFolder)
            {
                if (string.IsNullOrWhiteSpace(excelSourceFolder) || !Directory.Exists(excelSourceFolder))
                    return Fail("XLSX source folder is missing; cannot refresh CSV from Excel.");

                try
                {
                    Report(DataExportProgressKind.PhaseChanged, phaseLabel: "XLSX 원본 검사", phaseIndex: 0);
                    string csvDir = targetLayout.StagingCsvDirectory;
                    if (exportTarget.Platform == ExportPlatform.Unity)
                        Directory.CreateDirectory(csvDir);
                    int sourceCount = selectedOnly
                        ? xlsxDtStems.Count(stem => selectedStems.Contains(stem))
                        : xlsxDtStems.Count;
                    log?.Invoke(exportTarget.Platform == ExportPlatform.Unreal
                        ? $"XLSX 원본 {sourceCount}개 검사 준비. 검증 완료 후 메모리에서 UDataTable을 생성합니다."
                        : $"XLSX 원본 {sourceCount}개 검사 준비. 검증 완료 후 런타임 CSV를 생성합니다.");

                    if (removeOrphanArtifacts && xlsxDtStems != null && xlsxDtStems.Count > 0)
                    {
                        int removed = exportTarget.Platform == ExportPlatform.Unity
                            ? DataExportSourceFilter.RemoveOrphanArtifacts(projectRoot, xlsxDtStems, log)
                            : RemoveUnrealOrphanArtifacts(targetLayout, xlsxDtStems, log);
                        if (removed > 0)
                            log?.Invoke($"원본 XLSX가 없는 테이블 산출물 {removed}개 삭제.");
                    }
                    else if (!removeOrphanArtifacts && xlsxDtStems != null && xlsxDtStems.Count > 0)
                    {
                        log?.Invoke("원본 XLSX가 없는 테이블 산출물 삭제: OFF (기존 파일 유지).");
                    }
                }
                catch (Exception ex)
                {
                    return Fail(ex.Message);
                }
            }

            string dataCsv = targetLayout.StagingCsvDirectory;

            try
            {
                string scriptsDir = targetLayout.GeneratedCodeDirectory;
                string bytesDir = targetLayout.RuntimeDataDirectory;
                string enumWorkbookPath = EnumCatalogService.FindWorkbook(excelSourceFolder);
                EnumCatalog enumCatalog = enumWorkbookPath == null
                    ? null
                    : EnumCatalogService.ParseXlsx(enumWorkbookPath);
                string enumCatalogStem = enumWorkbookPath == null
                    ? null
                    : Path.GetFileNameWithoutExtension(enumWorkbookPath);

                Directory.CreateDirectory(scriptsDir);
                if (exportTarget.Platform == ExportPlatform.Unity)
                {
                    Directory.CreateDirectory(dataCsv);
                    Directory.CreateDirectory(bytesDir);
                }

                // XLSX가 있으면 런타임 폴더에 중간 CSV를 만들지 않고 원본을 직접 검증한다.
                // 모든 검사가 끝난 뒤 ExportSingleTable이 최종 헤더·데이터 행만 CSV로 기록한다.
                string[] existingCsvFiles = Directory.Exists(dataCsv)
                    ? Directory.GetFiles(dataCsv, "*.csv")
                        .Where(p => Path.GetFileName(p).StartsWith("DT_", StringComparison.OrdinalIgnoreCase))
                        .ToArray()
                    : Array.Empty<string>();
                string[] csvFiles;
                if (hasXlsxSource)
                {
                    csvFiles = xlsxDtStems
                        .Select(stem => Path.Combine(dataCsv, stem + ".csv"))
                        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    int skipped = existingCsvFiles.Count(path =>
                        !xlsxDtStems.Contains(Path.GetFileNameWithoutExtension(path)));
                    if (skipped > 0)
                        log?.Invoke($"XLSX에 없는 CSV {skipped}개는 Export에서 제외합니다.");
                }
                else
                {
                    csvFiles = existingCsvFiles
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
                if (exportTarget.Platform == ExportPlatform.Unreal)
                    UnrealCodeGenerator.ValidateForExport(enumCatalog, parseResults);
                IReadOnlyDictionary<string, string> unrealEnumTypeNames =
                    exportTarget.Platform == ExportPlatform.Unreal
                        ? UnrealCodeGenerator.CreateEnumTypeNameMap(
                            (enumCatalog?.DeclarationOrder ?? Array.Empty<string>())
                                .Concat(parseResults.SelectMany(result => result.EnumMembers.Keys)))
                        : null;

                ParallelBatchRunner.ForEach(
                    csvFiles,
                    csvPath =>
                    {
                        TableExportOutcome outcome = ExportSingleTable(
                            csvPath,
                            parsedTables[csvPath],
                            scriptsDir,
                            bytesDir,
                            exportTarget.Platform,
                            targetLayout.ModuleName,
                            unrealEnumTypeNames);
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
                                ? exportTarget.Platform == ExportPlatform.Unity ? "CSV · Script · Bytes" : "CSV · Unreal Header"
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
                            || TargetGeneratedTableExists(scriptsDir, table, exportTarget.Platform))
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
                    string enumPath = WriteTargetEnums(
                        exportTarget.Platform,
                        scriptsDir,
                        enumCatalog,
                        log);
                    string enumSummary =
                        $"{exportTarget.DisplayName} Enum Export 완료\r\n" +
                        $"· {enumPath}";
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

                WriteTargetEnums(exportTarget.Platform, scriptsDir, enumCatalog, log);
                Report(
                    DataExportProgressKind.PhaseChanged,
                    phaseLabel: exportTarget.DisplayName + " 런타임 산출물 생성",
                    phaseIndex: 2);

                if (exportTarget.Platform == ExportPlatform.Unity)
                {
                    UnityDataRuntimeGenerator.Write(
                        scriptsDir,
                        toolTableClassNames,
                        runtimeTables.OrderBy(t => t.ClassName, StringComparer.OrdinalIgnoreCase).ToList(),
                        log);
                }
                else
                {
                    RemoveUnrealIntermediateArtifacts(targetLayout, log);
                    UnrealRuntimeStorageGenerator.Write(targetLayout, runtimeTables, log);
                    if (autoImportUnrealDataTables)
                    {
                        Report(
                            DataExportProgressKind.PhaseChanged,
                            phaseLabel: "Unreal C++ 컴파일 및 UDataTable Import",
                            phaseIndex: 2);
                        UnrealDataTableImporter.CompileAndImport(targetLayout, runtimeTables, log);
                    }
                    else
                    {
                        log?.Invoke("Unreal UDataTable 자동 Import 생략.");
                    }
                }

                string summary = exportTarget.Platform == ExportPlatform.Unity
                    ? $"{(selectedOnly ? "선택 Export" : "전체 Export")} 완료 ({ok}개 테이블)\r\n" +
                      $"· Scripts: {scriptsDir}\r\n" +
                      $"· CSV/Bytes: {targetLayout.StagingCsvDirectory} · {targetLayout.RuntimeDataDirectory}\r\n" +
                      "· Unity: Container + InfoStorage + MessagePack 런타임 생성"
                    : $"{(selectedOnly ? "선택 Export" : "전체 Export")} 완료 ({ok}개 테이블)\r\n" +
                      $"· C++ Headers (IDE / C++ Classes): {scriptsDir}\r\n" +
                      "· Content Browser 표시 위치: /Game/PJDevData/DataTables\r\n" +
                      "· C++ 구조: UGlobalDataStorage 원본 + IInfoStorage 가공 Registry\r\n" +
                      (autoImportUnrealDataTables
                          ? "· 중간 CSV/JSON 없이 C++ 컴파일 및 UDataTable 자동 생성·갱신 완료"
                          : "· UDataTable 자동 Import 생략");
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

        private static int RemoveUnrealOrphanArtifacts(
            ExportTargetLayout layout,
            IReadOnlyCollection<string> sourceStems,
            Action<string> log)
        {
            var expected = new HashSet<string>(sourceStems ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            int removed = 0;
            if (Directory.Exists(layout.StagingCsvDirectory))
            {
                foreach (string csvPath in Directory.GetFiles(layout.StagingCsvDirectory, "DT_*.csv"))
                {
                    if (expected.Contains(Path.GetFileNameWithoutExtension(csvPath)))
                        continue;
                    File.Delete(csvPath);
                    removed++;
                    log?.Invoke("Unreal 원본에서 제외된 CSV 삭제: " + csvPath);
                }
            }

            if (Directory.Exists(layout.GeneratedCodeDirectory))
            {
                var expectedHeaders = new HashSet<string>(
                    expected.Select(stem =>
                    {
                        string name = stem.StartsWith("DT_", StringComparison.OrdinalIgnoreCase)
                            ? stem.Substring(3)
                            : stem;
                        return name + "Row.h";
                    }),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string headerPath in Directory.GetFiles(layout.GeneratedCodeDirectory, "*Row.h"))
                {
                    if (expectedHeaders.Contains(Path.GetFileName(headerPath)))
                        continue;
                    File.Delete(headerPath);
                    removed++;
                    log?.Invoke("Unreal 원본에서 제외된 Row 헤더 삭제: " + headerPath);
                }
            }
            return removed;
        }

        private static void RemoveUnrealIntermediateArtifacts(
            ExportTargetLayout layout,
            Action<string> log)
        {
            string legacyRoot = Path.Combine(
                layout.ProjectRoot,
                "Content",
                "PJDevData",
                "DataTables",
                "Source");
            string legacyManifest = Path.Combine(legacyRoot, "DataToolImportManifest.json");
            if (File.Exists(legacyManifest))
            {
                Directory.Delete(legacyRoot, true);
                log?.Invoke("Removed legacy Unreal import sources from Content: " + legacyRoot);
            }

            string savedRoot = Path.Combine(layout.ProjectRoot, "Saved", "PJDevDataTool");
            if (Directory.Exists(savedRoot))
            {
                Directory.Delete(savedRoot, true);
                log?.Invoke("Removed obsolete Unreal intermediate files: " + savedRoot);
            }
        }

        private static bool TargetGeneratedTableExists(
            string scriptsDir,
            CsvTableParseResult table,
            ExportPlatform platform)
        {
            string fileName = platform == ExportPlatform.Unity
                ? table.ClassName + "Container.cs"
                : UnrealCodeGenerator.GetRowHeaderFileName(table);
            return File.Exists(Path.Combine(scriptsDir, fileName));
        }

        private static string WriteTargetEnums(
            ExportPlatform platform,
            string scriptsDir,
            EnumCatalog catalog,
            Action<string> log)
        {
            Directory.CreateDirectory(scriptsDir);
            if (platform == ExportPlatform.Unity)
            {
                if (catalog != null)
                    return EnumCatalogService.WriteGeneratedFile(scriptsDir, catalog, log);
                return null;
            }

            string unrealPath = Path.Combine(scriptsDir, "DataEnums.h");
            GeneratedFileWriter.WriteAllTextIfChanged(
                unrealPath,
                UnrealCodeGenerator.GenerateEnumHeader(catalog),
                new UTF8Encoding(false));
            log?.Invoke("Unreal Enum Header: " + unrealPath);
            return unrealPath;
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
            string bytesDir,
            ExportPlatform exportPlatform,
            string projectName,
            IReadOnlyDictionary<string, string> unrealEnumTypeNames)
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

                if (exportPlatform == ExportPlatform.Unity)
                {
                    CsvTableParser.WriteDeployedCsv(csvPath, table);
                    outcome.LogLines.Add($"CSV: {csvPath}");

                    string csPath = Path.Combine(scriptsDir, classFileName + "Container.cs");
                    GeneratedFileWriter.WriteAllTextIfChanged(csPath, CsvClassGenerator.GenerateTableContainerFile(table), Encoding.UTF8);
                    outcome.LogLines.Add($"Script: {csPath}");

                    string bytePath = Path.Combine(bytesDir, fileName + ".bytes");
                    MessagePackTableExporter.ExportToFile(table, bytePath);
                    outcome.LogLines.Add($"Bytes: {bytePath}");
                }
                else
                {
                    string headerPath = Path.Combine(scriptsDir, UnrealCodeGenerator.GetRowHeaderFileName(table));
                    GeneratedFileWriter.WriteAllTextIfChanged(
                        headerPath,
                        UnrealCodeGenerator.GenerateRowHeader(table, projectName, unrealEnumTypeNames),
                        new UTF8Encoding(false));
                    outcome.LogLines.Add($"Unreal Header: {headerPath}");
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
