using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using System.Threading;
using CSVParserTool;
using CSVParserTool.Exporting;

internal static class Program
{
    private static int failures;

    private static int Main()
    {
        Console.WriteLine("DataTool automated performance checks");
        Run("CSV parse 25k x 8", 5000, CsvParse);
        Run("Key reference validation 50k", 5000, ReferenceValidation);
        Run("XLSX preview 10k x 8 -> 64 rows", 5000, XlsxPreview);
        Check("Duplicate Id is rejected", DuplicateIdRejected);
        Check("Missing keyref is rejected", MissingReferenceRejected);
        Check("Unchanged generated file keeps timestamp", UnchangedFileKeepsTimestamp);
        Check("Repeated XLSX preview uses cache", RepeatedPreviewUsesCache);
        Check("Unity and Unreal targets resolve stable layouts", EngineTargetLayouts);
        Check("Unreal row header maps common types", UnrealRowHeaderGeneration);
        Check("Unreal export writes C++ and import artifacts", UnrealExportArtifacts);
        Check("Unreal XLSX preview uses the export header generator", UnrealXlsxPreview);
        Check("Enum names and values are case-sensitive", EnumCatalogIsCaseSensitive);
        Console.WriteLine(failures == 0 ? "PASS" : $"FAIL ({failures})");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(string name, Action test)
    {
        try { test(); Console.WriteLine($"[OK] {name}"); }
        catch (Exception ex) { failures++; Console.WriteLine($"[ERROR] {name}: {ex.GetBaseException().Message}"); }
    }

    private static void EngineTargetLayouts()
    {
        string root = Path.Combine(Path.GetTempPath(), "DataToolTargets_" + Guid.NewGuid().ToString("N"));
        string unityRoot = Path.Combine(root, "UnityGame");
        string unrealRoot = Path.Combine(root, "UnrealGame");
        try
        {
            Directory.CreateDirectory(Path.Combine(unityRoot, "Assets"));
            Directory.CreateDirectory(Path.Combine(unityRoot, "ProjectSettings"));
            Directory.CreateDirectory(unrealRoot);
            File.WriteAllText(Path.Combine(unrealRoot, "ProjectR.uproject"), "{}");

            IEngineExportTarget unity = EngineExportTargetRegistry.Detect(unityRoot);
            IEngineExportTarget unreal = EngineExportTargetRegistry.Detect(unrealRoot);
            if (unity.Platform != ExportPlatform.Unity || unreal.Platform != ExportPlatform.Unreal)
                throw new InvalidOperationException("Engine target detection mismatch.");

            ExportTargetLayout unrealLayout = unreal.CreateLayout(unrealRoot);
            if (unrealLayout.ProjectName != "ProjectR" ||
                !unrealLayout.GeneratedCodeDirectory.EndsWith(
                    Path.Combine("Source", "ProjectR", "DataTables", "Generated"),
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unreal layout mismatch: " + unrealLayout.GeneratedCodeDirectory);
        }
        finally
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
        }
    }

    private static void UnrealExportArtifacts()
    {
        string root = Path.Combine(Path.GetTempPath(), "DataToolUnrealExport_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "ProjectR.uproject"), "{}");
            ExportTargetLayout layout = EngineExportTargetRegistry.Get(ExportPlatform.Unreal).CreateLayout(root);
            Directory.CreateDirectory(layout.StagingCsvDirectory);
            File.WriteAllLines(
                Path.Combine(layout.StagingCsvDirectory, "DT_Character.csv"),
                new[]
                {
                    "Id,Name,Speeds",
                    "1.0.0,1.0.0,1.0.0",
                    "int,string,float[]",
                    "1,Warrior,1.0|2.0"
                });

            DataExportResult result = DataExportService.RunExport(
                root,
                null,
                false,
                null,
                exportVersion: "1.0.0",
                exportPlatform: ExportPlatform.Unreal);
            if (!result.Ok)
                throw new InvalidOperationException(result.ErrorMessage);

            string header = Path.Combine(layout.GeneratedCodeDirectory, "CharacterRow.h");
            string enums = Path.Combine(layout.GeneratedCodeDirectory, "DataEnums.h");
            string manifest = Path.Combine(layout.RuntimeDataDirectory, "DataToolImportManifest.json");
            if (!File.Exists(header) || !File.Exists(enums) || !File.Exists(manifest))
                throw new InvalidOperationException("Unreal export artifact is missing.");
            if (Directory.GetFiles(layout.GeneratedCodeDirectory, "*Container.cs").Length != 0)
                throw new InvalidOperationException("Unity Container was generated for Unreal.");
            if (Directory.Exists(layout.RuntimeDataDirectory) && Directory.GetFiles(layout.RuntimeDataDirectory, "*.bytes").Length != 0)
                throw new InvalidOperationException("MessagePack bytes were generated for Unreal.");
        }
        finally
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
        }
    }
    private static void UnrealRowHeaderGeneration()
    {
        var enums = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["CharacterType"] = new[] { "Warrior", "Mage" }
        };
        var table = new CsvTableParseResult(
            "CharacterData",
            new[] { "Id", "Name", "Speeds", "Type" },
            new[] { "int", "string", "float[]", "CharacterType" },
            new[] { "CharacterType" },
            enums,
            Array.Empty<string[]>());

        string header = UnrealCodeGenerator.GenerateRowHeader(table, "ProjectR");
        string[] expected =
        {
            "struct PROJECTR_API FCharacterRow : public FTableRowBase",
            "int32 Id{};",
            "FString Name{};",
            "TArray<float> Speeds{};",
            "ECharacterType Type{};",
            "enum class ECharacterType : uint8"
        };
        foreach (string text in expected)
        {
            if (!header.Contains(text))
                throw new InvalidOperationException("Missing Unreal output: " + text);
        }
    }
    private static void UnchangedFileKeepsTimestamp()
    {
        string path = Path.Combine(Path.GetTempPath(), "DataToolWrite_" + Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            if (!GeneratedFileWriter.WriteAllTextIfChanged(path, "same", new System.Text.UTF8Encoding(false)))
                throw new InvalidOperationException("Initial write was skipped.");
            DateTime stamp = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, stamp);
            if (GeneratedFileWriter.WriteAllTextIfChanged(path, "same", new System.Text.UTF8Encoding(false)))
                throw new InvalidOperationException("Unchanged text was rewritten.");
            if (File.GetLastWriteTimeUtc(path) != stamp)
                throw new InvalidOperationException("Unchanged file timestamp changed.");
            if (!GeneratedFileWriter.WriteAllTextIfChanged(path, "changed", new System.Text.UTF8Encoding(false)))
                throw new InvalidOperationException("Changed text was not written.");
        }
        finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    }

    private static void RepeatedPreviewUsesCache()
    {
        string path = Path.Combine(Path.GetTempPath(), "DT_CachePerf_" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                for (int c = 1; c <= 8; c++) { sheet.Cell(1, c).Value = c == 1 ? "Id" : "C" + c; sheet.Cell(2, c).Value = "1.0.0"; sheet.Cell(3, c).Value = "int"; }
                for (int r = 0; r < 10000; r++) for (int c = 1; c <= 8; c++) sheet.Cell(r + 4, c).Value = r * 8 + c;
                workbook.SaveAs(path);
            }
            var first = Stopwatch.StartNew();
            string a = CsvClassGenerator.GeneratePreviewFromXlsxFast(path, 64, "1.0.0");
            first.Stop();
            var second = Stopwatch.StartNew();
            string b = CsvClassGenerator.GeneratePreviewFromXlsxFast(path, 64, "1.0.0");
            second.Stop();
            if (!string.Equals(a, b, StringComparison.Ordinal)) throw new InvalidOperationException("Cached preview output changed.");
            if (second.ElapsedMilliseconds >= first.ElapsedMilliseconds || second.ElapsedMilliseconds > 250)
                throw new InvalidOperationException($"Preview cache was not effective: first={first.ElapsedMilliseconds}ms second={second.ElapsedMilliseconds}ms");
            Console.WriteLine($"     preview cache: first={first.ElapsedMilliseconds} ms, second={second.ElapsedMilliseconds} ms");
        }
        finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    }
    private static void DuplicateIdRejected()
    {
        string[] lines = { "Id,Name", "1.0.0,1.0.0", "int,string", "1,A", "1,B" };
        try
        {
            CsvTableParser.ParseLines(lines, "DuplicateData", new CsvParseOptions { ExportVersion = "1.0.0" });
            throw new InvalidOperationException("Duplicate Id was accepted.");
        }
        catch (InvalidOperationException ex) when (ex.Message != "Duplicate Id was accepted.") { }
    }

    private static void MissingReferenceRejected()
    {
        var target = new CsvTableParseResult("StatDefinitionData", new[] { "Id", "StatId" }, new[] { "int", "string" }, null, Array.Empty<string>(), new Dictionary<string, IReadOnlyList<string>>(), new[] { new[] { "1", "Health" } });
        var source = new CsvTableParseResult("CharacterStatData", new[] { "Id", "StatId" }, new[] { "int", "string" }, new[] { null, new CsvColumnReference("StatDefinition", "StatId", false, true) }, Array.Empty<string>(), new Dictionary<string, IReadOnlyList<string>>(), new[] { new[] { "1", "Missing" } });
        try
        {
            CrossTableReferenceResolver.Resolve(new[] { target, source });
            throw new InvalidOperationException("Missing keyref was accepted.");
        }
        catch (InvalidOperationException ex) when (ex.Message != "Missing keyref was accepted.") { }
    }
    private static void Run(string name, long limitMs, Func<int> test)
    {
        try
        {
            test();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long before = GC.GetTotalMemory(true);
            var watch = Stopwatch.StartNew();
            int count = test();
            watch.Stop();
            long allocated = Math.Max(0, GC.GetTotalMemory(false) - before);
            bool ok = watch.ElapsedMilliseconds <= limitMs;
            if (!ok) failures++;
            Console.WriteLine($"[{(ok ? "OK" : "SLOW")}] {name}: {watch.ElapsedMilliseconds} ms, result={count:N0}, retainedΔ={allocated / 1024d / 1024d:F1} MB, limit={limitMs} ms");
        }
        catch (Exception ex)
        {
            failures++;
            Console.WriteLine($"[ERROR] {name}: {ex.GetBaseException().Message}");
        }
    }

    private static int CsvParse()
    {
        const int rows = 25000, columns = 8;
        var lines = new string[rows + 3];
        lines[0] = "Id,C1,C2,C3,C4,C5,C6,C7";
        lines[1] = "1.0.0,1.0.0,1.0.0,1.0.0,1.0.0,1.0.0,1.0.0,1.0.0";
        lines[2] = "int,int,int,int,int,int,int,string";
        for (int row = 0; row < rows; row++)
            lines[row + 3] = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},Value_{0}", row + 1, row, row + 1, row + 2, row + 3, row + 4, row + 5);
        CsvTableParseResult table = CsvTableParser.ParseLines(lines, "PerformanceData", new CsvParseOptions { ExportVersion = "1.0.0" });
        if (table.DataRows.Count != rows || table.Headers.Length != columns) throw new InvalidOperationException("CSV parse result mismatch.");
        return table.DataRows.Count;
    }

    private static int ReferenceValidation()
    {
        const int targetRows = 5000, sourceRows = 50000;
        var targets = new List<string[]>(targetRows);
        for (int i = 0; i < targetRows; i++) targets.Add(new[] { (i + 1).ToString(CultureInfo.InvariantCulture), "Key_" + i });
        var sources = new List<string[]>(sourceRows);
        for (int i = 0; i < sourceRows; i++) sources.Add(new[] { (i + 1).ToString(CultureInfo.InvariantCulture), "Key_" + (i % targetRows) });
        var target = new CsvTableParseResult("StatDefinitionData", new[] { "Id", "StatId" }, new[] { "int", "string" }, null, Array.Empty<string>(), new Dictionary<string, IReadOnlyList<string>>(), targets);
        var source = new CsvTableParseResult("CharacterStatData", new[] { "Id", "StatId" }, new[] { "int", "string" }, new[] { null, new CsvColumnReference("StatDefinition", "StatId", false, true) }, Array.Empty<string>(), new Dictionary<string, IReadOnlyList<string>>(), sources);
        CrossTableReferenceResolver.Resolve(new[] { target, source });
        if (source.DataRows.Count != sourceRows) throw new InvalidOperationException("Reference result mismatch.");
        return source.DataRows.Count;
    }

    private static int XlsxPreview()
    {
        string path = Path.Combine(Path.GetTempPath(), "DataToolPerf_" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                string[] headers = { "Id", "C1", "C2", "C3", "C4", "C5", "C6", "Name" };
                for (int c = 0; c < headers.Length; c++) { sheet.Cell(1, c + 1).Value = headers[c]; sheet.Cell(2, c + 1).Value = "1.0.0"; sheet.Cell(3, c + 1).Value = c == 7 ? "string" : "int"; }
                for (int r = 0; r < 10000; r++) for (int c = 0; c < 8; c++) sheet.Cell(r + 4, c + 1).Value = c == 7 ? "Name_" + r : (r * 8 + c);
                workbook.SaveAs(path);
            }
            string preview = CsvClassGenerator.GeneratePreviewFromXlsxFast(path, 64, "1.0.0");
            if (string.IsNullOrWhiteSpace(preview) || !preview.Contains("class DataToolPerf_")) throw new InvalidOperationException("Preview output mismatch.");
            return preview.Length;
        }
        finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    }
    private static void UnrealXlsxPreview()
    {
        string path = Path.Combine(Path.GetTempPath(), "DT_PreviewCharacter_" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                sheet.Cell(1, 1).Value = "Id";
                sheet.Cell(1, 2).Value = "Name";
                sheet.Cell(1, 3).Value = "Speeds";
                sheet.Cell(2, 1).Value = "1.0.0";
                sheet.Cell(2, 2).Value = "1.0.0";
                sheet.Cell(2, 3).Value = "1.0.0";
                sheet.Cell(3, 1).Value = "int";
                sheet.Cell(3, 2).Value = "string";
                sheet.Cell(3, 3).Value = "float[]";
                sheet.Cell(4, 1).Value = 1;
                sheet.Cell(4, 2).Value = "Warrior";
                sheet.Cell(4, 3).Value = "1.0|2.0";
                workbook.SaveAs(path);
            }

            string preview = CsvClassGenerator.GenerateValidatedPreviewFromXlsx(
                path,
                Path.GetDirectoryName(path),
                "1.0.0",
                CancellationToken.None,
                ExportPlatform.Unreal,
                "ProjectR");
            string exportedHeader = UnrealCodeGenerator.GenerateRowHeader(
                CsvClassGenerator.ParseTableFromXlsx(
                    path,
                    null,
                    new CsvParseOptions { ExportVersion = "1.0.0" }),
                "ProjectR");
            if (preview != exportedHeader
                || !preview.Contains("USTRUCT(BlueprintType)")
                || !preview.Contains("struct PROJECTR_API FPreviewCharacter_")
                || preview.Contains("using MessagePack"))
                throw new InvalidOperationException("Unreal Preview does not match the exported C++ header.");
        }
        finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    }

    private static void EnumCatalogIsCaseSensitive()
    {
        string path = Path.Combine(Path.GetTempPath(), "DT_Enums_" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Enums");
                sheet.Cell(1, 1).Value = "EnumName";
                sheet.Cell(1, 2).Value = "Value";
                sheet.Cell(2, 1).Value = "Test";
                sheet.Cell(2, 2).Value = "A";
                sheet.Cell(3, 1).Value = "tEST";
                sheet.Cell(3, 2).Value = "A";
                sheet.Cell(4, 1).Value = "test";
                sheet.Cell(4, 2).Value = "A";
                sheet.Cell(5, 1).Value = "Test";
                sheet.Cell(5, 2).Value = "Value";
                sheet.Cell(6, 1).Value = "Test";
                sheet.Cell(6, 2).Value = "value";
                sheet.Cell(7, 1).Value = "Test";
                sheet.Cell(7, 2).Value = "vALUE";
                workbook.SaveAs(path);
            }

            EnumCatalog catalog = EnumCatalogService.ParseXlsx(path);
            if (catalog.DeclarationOrder.Count != 3
                || !catalog.Members.ContainsKey("Test")
                || !catalog.Members.ContainsKey("tEST")
                || !catalog.Members.ContainsKey("test")
                || catalog.Members.ContainsKey("TEST")
                || !catalog.Members["Test"].SequenceEqual(new[] { "A", "Value", "value", "vALUE" }))
            {
                throw new InvalidOperationException("EnumName or Value casing was merged.");
            }

            string unrealEnums = UnrealCodeGenerator.GenerateEnumHeader(catalog);
            foreach (string expected in new[]
            {
                "enum class ETest", "enum class EtEST", "enum class Etest",
                "    Value,", "    value,", "    vALUE"
            })
            {
                if (!unrealEnums.Contains(expected))
                    throw new InvalidOperationException("Unreal Enum casing changed: " + expected);
            }

            var validTable = new CsvTableParseResult(
                "CaseData",
                new[] { "Id", "Mode" },
                new[] { "int", "Test" },
                Array.Empty<string>(),
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
                new[] { new[] { "1", "Value" }, new[] { "2", "vALUE" } });
            EnumCatalogService.ApplyToTables(catalog, new[] { validTable });

            var invalidTable = new CsvTableParseResult(
                "InvalidCaseData",
                new[] { "Id", "Mode" },
                new[] { "int", "Test" },
                Array.Empty<string>(),
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
                new[] { new[] { "1", "VALUE" } });
            bool rejected = false;
            try
            {
                EnumCatalogService.ApplyToTables(catalog, new[] { invalidTable });
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            if (!rejected)
                throw new InvalidOperationException("Wrong-case Enum value was accepted.");
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
