using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace CSVParserTool
{
    /// <summary>Exports the first worksheet of each workbook to UTF-8 CSV for the data pipeline.</summary>
    public static class XlsxToCsvConverter
    {
        public static int ConvertAllXlsxInFolder(
            string xlsxFolder,
            string csvOutputFolder,
            Action<string> log,
            ICollection<string> includedStems = null)
        {
            if (string.IsNullOrWhiteSpace(xlsxFolder) || !Directory.Exists(xlsxFolder))
                throw new ArgumentException("XLSX folder is missing or invalid.", nameof(xlsxFolder));

            Directory.CreateDirectory(csvOutputFolder);

            string[] paths = Directory.GetFiles(xlsxFolder, "*.xlsx")
                .Where(path => !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal))
                .Where(path => !EnumCatalogService.IsCatalogPath(path))
                .Where(path =>
                    includedStems == null
                    || includedStems.Contains(
                        Path.GetFileNameWithoutExtension(path),
                        StringComparer.OrdinalIgnoreCase))
                .ToArray();

            int converted = 0;
            object logSync = new object();
            void LogLine(string message)
            {
                if (log == null)
                    return;

                lock (logSync)
                    log(message);
            }

            ParallelBatchRunner.ForEach(
                paths,
                path =>
                {
                    string fileName = Path.GetFileName(path);
                    try
                    {
                        ExportFirstWorksheetToCsv(
                            path,
                            Path.Combine(csvOutputFolder, Path.GetFileNameWithoutExtension(path) + ".csv"));
                        Interlocked.Increment(ref converted);
                        LogLine($"Excel→CSV: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        LogLine($"Excel→CSV 실패 ({fileName}): {ex.Message}");
                    }
                },
                batchLog: paths.Length > ParallelBatchRunner.DefaultBatchSize ? LogLine : null);

            return converted;
        }

        public static string GetPrimaryCsvPathForWorkbook(string xlsxFullPath, string dataCsvDir)
        {
            if (string.IsNullOrWhiteSpace(xlsxFullPath))
                throw new ArgumentException("XLSX path is empty.", nameof(xlsxFullPath));

            string name = Path.GetFileNameWithoutExtension(xlsxFullPath);
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid XLSX file name.", nameof(xlsxFullPath));

            return Path.Combine(dataCsvDir, name + ".csv");
        }

        public static void ExportFirstWorksheetToCsv(string xlsxPath, string csvPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath) || !File.Exists(xlsxPath))
                throw new FileNotFoundException("XLSX not found.", xlsxPath);

            // Excel 등에서 연 채로 있어도 읽을 수 있게 공유 읽기 (기본 XLWorkbook(path) 는 배타 잠금에 걸림)
            using (var fs = OpenXlsxReadStreamShared(xlsxPath))
            using (var wb = new XLWorkbook(fs))
            {
                IXLWorksheet ws = wb.Worksheets.FirstOrDefault();
                if (ws == null)
                    throw new InvalidOperationException("Workbook has no worksheets.");

                var used = ws.RangeUsed();
                if (used == null)
                {
                    GeneratedFileWriter.WriteAllTextIfChanged(csvPath, string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    return;
                }

                string csv = BuildExportCsvFromWorksheet(ws, used);
                GeneratedFileWriter.WriteAllTextIfChanged(csvPath, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }

        private static FileStream OpenXlsxReadStreamShared(string xlsxPath)
        {
            const int maxAttempts = 6;
            const int retryDelayMs = 120;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return new FileStream(
                        xlsxPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete,
                        bufferSize: 4096,
                        FileOptions.SequentialScan);
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    Thread.Sleep(retryDelayMs);
                }
                catch (UnauthorizedAccessException) when (attempt < maxAttempts)
                {
                    Thread.Sleep(retryDelayMs);
                }
            }

            throw new IOException($"Failed to open XLSX with shared read access: {xlsxPath}");
        }

        private static string CellToCsvField(IXLCell cell)
        {
            if (cell.IsEmpty())
                return string.Empty;

            XLCellValue v = cell.Value;

            switch (v.Type)
            {
                case XLDataType.Number:
                {
                    double d = v.GetNumber();
                    if (!double.IsNaN(d) && !double.IsInfinity(d) && Math.Abs(d - Math.Truncate(d)) < 1e-9)
                        return ((long)Math.Truncate(d)).ToString(CultureInfo.InvariantCulture);
                    return d.ToString(CultureInfo.InvariantCulture);
                }
                case XLDataType.Boolean:
                    return v.GetBoolean() ? "True" : "False";
                case XLDataType.Text:
                    return v.GetText();
                case XLDataType.Blank:
                    return string.Empty;
                default:
                    return v.ToString();
            }
        }

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            if (s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return s;

            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>CSV Export용. 헤더가 <c>#</c> 로 시작하는 열은 통째로 제외(엑셀 원본 시트는 변경하지 않음).</summary>
        private static string BuildExportCsvFromWorksheet(IXLWorksheet ws, IXLRange used)
        {
            int c0 = used.FirstColumn().ColumnNumber();
            int c1 = used.LastColumn().ColumnNumber();
            int r0 = used.FirstRow().RowNumber();
            int r1 = used.LastRow().RowNumber();

            int headerRow = TryFindHeaderRow(ws, r0, r1, c0, c1);
            bool[] keepMask = headerRow >= 0
                ? CsvTableParser.BuildExportColumnKeepMask(ReadWorksheetRowCells(ws, headerRow, c0, c1))
                : CreateKeepAllMask(c1 - c0 + 1);

            var sb = new StringBuilder();
            for (int r = r0; r <= r1; r++)
            {
                string[] cells = ReadWorksheetRowCells(ws, r, c0, c1);
                AppendExportRowCsv(sb, CsvTableParser.FilterExportColumns(cells, keepMask));
            }

            return sb.ToString();
        }

        private static int TryFindHeaderRow(IXLWorksheet ws, int r0, int r1, int c0, int c1)
        {
            for (int r = r0; r <= r1; r++)
            {
                if (CsvTableParser.FindIdColumnIndex(ReadWorksheetRowCells(ws, r, c0, c1)) >= 0)
                    return r;
            }

            return r0;
        }

        private static bool[] CreateKeepAllMask(int columnCount)
        {
            var mask = new bool[columnCount];
            for (int i = 0; i < columnCount; i++)
                mask[i] = true;
            return mask;
        }

        private static string[] ReadWorksheetRowCells(IXLWorksheet ws, int row, int c0, int c1)
        {
            int len = c1 - c0 + 1;
            var cells = new string[len];
            for (int i = 0; i < len; i++)
                cells[i] = CellToCsvField(ws.Cell(row, c0 + i));
            return cells;
        }

        private static void AppendExportRowCsv(StringBuilder sb, string[] cells)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append(EscapeCsv(cells[i] ?? string.Empty));
            }

            sb.AppendLine();
        }


        public static string ConvertXlsxToCsvString(string xlsxPath, int? maxRows = null)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath) || !File.Exists(xlsxPath))
                throw new FileNotFoundException("XLSX not found.", xlsxPath);

            using (var fs = OpenXlsxReadStreamShared(xlsxPath))
            using (var wb = new XLWorkbook(fs))
            {
                var ws = wb.Worksheets.FirstOrDefault();
                if (ws == null)
                    throw new InvalidOperationException("Workbook has no worksheets.");

                var used = ws.RangeUsed();
                if (used == null)
                    return string.Empty;

                int c0 = used.FirstColumn().ColumnNumber();
                int c1 = used.LastColumn().ColumnNumber();
                int r0 = used.FirstRow().RowNumber();
                int r1 = used.LastRow().RowNumber();
                if (maxRows.HasValue && maxRows.Value > 0)
                    r1 = Math.Min(r1, r0 + maxRows.Value - 1);

                var subset = ws.Range(r0, c0, r1, c1);
                return BuildExportCsvFromWorksheet(ws, subset);
            }
        }
    }
}
