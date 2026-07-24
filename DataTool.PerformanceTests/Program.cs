using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using CSVParserTool;

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
        Console.WriteLine(failures == 0 ? "PASS" : $"FAIL ({failures})");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(string name, Action test)
    {
        try { test(); Console.WriteLine($"[OK] {name}"); }
        catch (Exception ex) { failures++; Console.WriteLine($"[ERROR] {name}: {ex.GetBaseException().Message}"); }
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
}