using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ClosedXML.Excel;

namespace CSVParserTool
{
    /// <summary>Exports the first worksheet of each workbook to UTF-8 CSV for the data pipeline.</summary>
    public static class XlsxToCsvConverter
    {
        public static int ConvertAllXlsxInFolder(string xlsxFolder, string csvOutputFolder, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(xlsxFolder) || !Directory.Exists(xlsxFolder))
                throw new ArgumentException("XLSX folder is missing or invalid.", nameof(xlsxFolder));

            Directory.CreateDirectory(csvOutputFolder);

            int n = 0;
            foreach (string path in Directory.GetFiles(xlsxFolder, "*.xlsx"))
            {
                string fileName = Path.GetFileName(path);
                if (fileName.StartsWith("~$", StringComparison.Ordinal))
                    continue;

                try
                {
                    ExportFirstWorksheetToCsv(path, Path.Combine(csvOutputFolder, Path.GetFileNameWithoutExtension(path) + ".csv"));
                    log?.Invoke($"Excel→CSV: {fileName}");
                    n++;
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Excel→CSV 실패 ({fileName}): {ex.Message}");
                }
            }

            return n;
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
                    File.WriteAllText(csvPath, string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    return;
                }

                int c0 = used.FirstColumn().ColumnNumber();
                int c1 = used.LastColumn().ColumnNumber();
                int r0 = used.FirstRow().RowNumber();
                int r1 = used.LastRow().RowNumber();

                var sb = new StringBuilder();
                for (int r = r0; r <= r1; r++)
                {
                    if (CsvTableParser.IsDescriptionRow(CellToCsvField(ws.Cell(r, c0))))
                        continue;

                    for (int c = c0; c <= c1; c++)
                    {
                        if (c > c0)
                            sb.Append(',');
                        sb.Append(EscapeCsv(CellToCsvField(ws.Cell(r, c))));
                    }

                    sb.AppendLine();
                }

                File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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

                var sb = new StringBuilder();

                for (int r = r0; r <= r1; r++)
                {
                    if (CsvTableParser.IsDescriptionRow(CellToCsvField(ws.Cell(r, c0))))
                        continue;

                    for (int c = c0; c <= c1; c++)
                    {
                        if (c > c0)
                            sb.Append(',');

                        sb.Append(EscapeCsv(CellToCsvField(ws.Cell(r, c))));
                    }

                    sb.AppendLine();
                }

                return sb.ToString(); 
            }
        }
    }
}
