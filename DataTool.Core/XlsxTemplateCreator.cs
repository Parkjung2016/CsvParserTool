using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace CSVParserTool
{
    /// <summary>데이터 파이프라인용 빈 XLSX 템플릿 (헤더 · 버전 · 타입 · Excel 표).</summary>
    public static class XlsxTemplateCreator
    {
        private const int NoteCol = 1;
        private const int FirstExportCol = 2;
        private const int LastCol = 4;
        private const int HeaderRow = 1;
        private const int VersionRow = 2;
        private const int TypeRow = 3;
        private const int FirstDataRow = 4;
        private const int InitialDataRows = 2;

        private const string TableName = "DataTable";

        private static readonly XLColor ValidTextColor = XLColor.Black;
        private static readonly XLColor InvalidFillColor = XLColor.FromHtml("#FF9999");

        public static void CreateNew(string outputPath, string tableBaseName)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path is empty.", nameof(outputPath));

            string sheetName = DeriveSheetName(tableBaseName);

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add(sheetName);
                WriteSchema(ws);
                WriteSampleData(ws);
                IXLTable table = CreateSheetTable(ws);
                ApplyLayout(ws);
                ApplySchemaStyles(ws);
                ApplyNoteColumnStyles(ws);
                ApplyValidationRules(ws, table);

                string dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                wb.SaveAs(outputPath);
                RepairExpressionConditionalFormats(outputPath, TableName);
            }
        }

        private static void WriteSchema(IXLWorksheet ws)
        {
            ws.Cell(HeaderRow, NoteCol).Value = "#설명";
            ws.Cell(HeaderRow, FirstExportCol).Value = "Id";
            ws.Cell(HeaderRow, FirstExportCol + 1).Value = "Name";
            ws.Cell(HeaderRow, LastCol).Value = "Value";

            ws.Cell(VersionRow, NoteCol).Value = "버전";
            ws.Cell(VersionRow, FirstExportCol).Value = "1.0.0";
            ws.Cell(VersionRow, FirstExportCol + 1).Value = "1.0.0";
            ws.Cell(VersionRow, LastCol).Value = "1.0.0";

            ws.Cell(TypeRow, NoteCol).Value = "타입";
            ws.Cell(TypeRow, FirstExportCol).Value = "int";
            ws.Cell(TypeRow, FirstExportCol + 1).Value = "string";
            ws.Cell(TypeRow, LastCol).Value = "int";
        }

        private static void WriteSampleData(IXLWorksheet ws)
        {
            ws.Cell(FirstDataRow, NoteCol).Value = "플레이어";
            ws.Cell(FirstDataRow, FirstExportCol).Value = 0;
            ws.Cell(FirstDataRow, FirstExportCol + 1).Value = "Sample";
            ws.Cell(FirstDataRow, LastCol).Value = 0;
        }

        private static void ApplyLayout(IXLWorksheet ws)
        {
            ws.Column(NoteCol).Width = 14;
            ws.Column(FirstExportCol).Width = 10;
            ws.Column(FirstExportCol + 1).Width = 18;
            ws.Column(LastCol).Width = 10;

            ws.Cell(FirstDataRow, FirstExportCol).Style.NumberFormat.Format = "0";
            ws.Cell(FirstDataRow, LastCol).Style.NumberFormat.Format = "0";

            ws.SheetView.Freeze(FirstDataRow - 1, NoteCol);
        }

        private static void ApplySchemaStyles(IXLWorksheet ws)
        {
            var schema = ws.Range(HeaderRow, NoteCol, TypeRow, LastCol);
            schema.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var noteHeader = ws.Cell(HeaderRow, NoteCol);
            noteHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7E6E6");
            noteHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var exportHeaders = ws.Range(HeaderRow, FirstExportCol, HeaderRow, LastCol);
            exportHeaders.Style.Font.Bold = true;
            exportHeaders.Style.Font.FontColor = XLColor.White;
            exportHeaders.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            exportHeaders.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var noteLabels = ws.Range(VersionRow, NoteCol, TypeRow, NoteCol);
            noteLabels.Style.Font.Bold = true;
            noteLabels.Style.Font.FontColor = ValidTextColor;
            noteLabels.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            noteLabels.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var versionCells = ws.Range(VersionRow, FirstExportCol, VersionRow, LastCol);
            versionCells.Style.Font.FontColor = ValidTextColor;
            versionCells.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
            versionCells.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var typeCells = ws.Range(TypeRow, FirstExportCol, TypeRow, LastCol);
            typeCells.Style.Font.FontName = "Consolas";
            typeCells.Style.Font.FontColor = ValidTextColor;
            typeCells.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDEDED");
            typeCells.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void ApplyValidationRules(IXLWorksheet ws, IXLTable table)
        {
            // ClosedXML은 CF sqref를 표 전체로 안정적으로 쓰지 못하는 경우가 있어 save 후 XML에서 범위를 넓힘.
            IXLRangeAddress addr = table.RangeAddress;
            var tableRange = ws.Range(
                addr.FirstAddress.RowNumber,
                addr.FirstAddress.ColumnNumber,
                addr.LastAddress.RowNumber,
                addr.LastAddress.ColumnNumber);
            tableRange.AddConditionalFormat()
                .WhenIsTrue("=" + BuildVersionInvalidTableFormula())
                .Fill.SetBackgroundColor(InvalidFillColor);
            tableRange.AddConditionalFormat()
                .WhenIsTrue("=" + BuildTypeInvalidTableFormula())
                .Fill.SetBackgroundColor(InvalidFillColor);
        }

        private const string CurrentCellRef = "INDIRECT(\"RC\",FALSE)";
        private const string HeaderCellRefExpr = "INDIRECT(\"R1C\",FALSE)";

        private static string BuildVersionInvalidTableFormula()
        {
            string header = HeaderCellRefExpr;
            string trimmedHeader = $"TRIM({header})";
            return "AND(" +
                   $"ROW()={VersionRow}," +
                   $"COLUMN()>{NoteCol}," +
                   $"LEN({trimmedHeader})>0," +
                   $"LEFT({trimmedHeader},1)<>\"#\"," +
                   BuildVersionInvalidFormula(CurrentCellRef) +
                   ")";
        }

        private static string BuildTypeInvalidTableFormula()
        {
            string header = HeaderCellRefExpr;
            string trimmedHeader = $"TRIM({header})";
            return "AND(" +
                   $"ROW()={TypeRow}," +
                   $"COLUMN()>{NoteCol}," +
                   $"LEN({trimmedHeader})>0," +
                   BuildTypeInvalidFormula(CurrentCellRef, header) +
                   ")";
        }

        /// <summary>expression CF XML 보정 — operator 제거, sqref를 버전·타입 행 전체 열로 확장.</summary>
        private static void RepairExpressionConditionalFormats(string xlsxPath, string tableName)
        {
            string cfSqref = BuildValidationConditionalFormatSqref(xlsxPath, tableName);

            byte[] originalBytes = File.ReadAllBytes(xlsxPath);
            using (var input = new MemoryStream(originalBytes))
            using (var output = new MemoryStream())
            {
                using (var read = new ZipArchive(input, ZipArchiveMode.Read))
                using (var write = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (ZipArchiveEntry entry in read.Entries)
                    {
                        ZipArchiveEntry destEntry = write.CreateEntry(
                            entry.FullName,
                            CompressionLevel.Optimal);

                        using (Stream entryStream = entry.Open())
                        using (Stream destStream = destEntry.Open())
                        {
                            if (IsWorksheetXml(entry.FullName))
                            {
                                string xml = new StreamReader(entryStream, Encoding.UTF8).ReadToEnd();
                                xml = Regex.Replace(
                                    xml,
                                    "type=\"expression\" dxfId=\"(\\d+)\" priority=\"(\\d+)\" operator=\"equal\"",
                                    "type=\"expression\" dxfId=\"$1\" priority=\"$2\"",
                                    RegexOptions.CultureInvariant);
                                xml = Regex.Replace(
                                    xml,
                                    "(<(?:x:)?conditionalFormatting[^>]*\\ssqref=\")[^\"]+(\")",
                                    m => m.Groups[1].Value + cfSqref + m.Groups[2].Value,
                                    RegexOptions.CultureInvariant);
                                byte[] utf8 = Encoding.UTF8.GetBytes(xml);
                                destStream.Write(utf8, 0, utf8.Length);
                            }
                            else
                            {
                                entryStream.CopyTo(destStream);
                            }
                        }
                    }
                }

                File.WriteAllBytes(xlsxPath, output.ToArray());
            }
        }

        /// <summary>표 ref 기준 CF 적용 범위 — 표 전체 행 × export 열 방향 여유.</summary>
        private static string BuildValidationConditionalFormatSqref(string xlsxPath, string tableName)
        {
            string tableRef = TryReadTableRef(xlsxPath, tableName);
            if (string.IsNullOrEmpty(tableRef))
                return "$A$1:$ZZ$500";

            if (!TryParseCellRange(tableRef, out int r1, out int c1, out int r2, out int c2))
                return "$A$1:$ZZ$500";

            const int extraExportCols = 50;
            int lastCol = Math.Max(c2, c1 + extraExportCols);
            int lastRow = Math.Max(r2, r1 + 500);
            return "$" + ColumnLetter(c1) + "$" + r1 + ":$" + ColumnLetter(lastCol) + "$" + lastRow;
        }

        private static string TryReadTableRef(string xlsxPath, string tableName)
        {
            using (var archive = ZipFile.OpenRead(xlsxPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!entry.FullName.StartsWith("xl/tables/table", StringComparison.OrdinalIgnoreCase)
                        || !entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string xml;
                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        xml = reader.ReadToEnd();

                    var nameMatch = Regex.Match(
                        xml,
                        "name=\"" + Regex.Escape(tableName) + "\"",
                        RegexOptions.CultureInvariant);
                    if (!nameMatch.Success)
                        continue;

                    var refMatch = Regex.Match(xml, "\\sref=\"([^\"]+)\"", RegexOptions.CultureInvariant);
                    if (refMatch.Success)
                        return refMatch.Groups[1].Value;
                }
            }

            return null;
        }

        private static bool TryParseCellRange(string rangeRef, out int r1, out int c1, out int r2, out int c2)
        {
            r1 = c1 = r2 = c2 = 0;
            var match = Regex.Match(
                rangeRef,
                "^([A-Z]+)(\\d+):([A-Z]+)(\\d+)$",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (!match.Success)
                return false;

            c1 = ColumnIndex(match.Groups[1].Value);
            r1 = int.Parse(match.Groups[2].Value);
            c2 = ColumnIndex(match.Groups[3].Value);
            r2 = int.Parse(match.Groups[4].Value);
            return c1 > 0 && c2 > 0;
        }

        private static int ColumnIndex(string letters)
        {
            int index = 0;
            foreach (char ch in letters.ToUpperInvariant())
            {
                if (ch < 'A' || ch > 'Z')
                    return 0;
                index = index * 26 + (ch - 'A' + 1);
            }
            return index;
        }

        private static string ColumnLetter(int index)
        {
            if (index <= 0)
                return "A";

            var chars = new char[4];
            int pos = chars.Length;
            while (index > 0)
            {
                index--;
                chars[--pos] = (char)('A' + index % 26);
                index /= 26;
            }

            return new string(chars, pos, chars.Length - pos);
        }

        private static bool IsWorksheetXml(string entryFullName) =>
            entryFullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
            && entryFullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);

        /// <summary>표 헤더 테마가 A1 글자색을 흰색으로 덮어쓰므로 마지막에 재적용.</summary>
        private static void ApplyNoteColumnStyles(IXLWorksheet ws)
        {
            var noteHeader = ws.Cell(HeaderRow, NoteCol);
            noteHeader.Style.Font.Bold = true;
            noteHeader.Style.Font.Italic = true;
            noteHeader.Style.Font.FontColor = ValidTextColor;
            noteHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7E6E6");
            noteHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var noteLabels = ws.Range(VersionRow, NoteCol, TypeRow, NoteCol);
            noteLabels.Style.Font.Bold = true;
            noteLabels.Style.Font.FontColor = ValidTextColor;

            ws.Column(NoteCol).Style.Font.FontColor = ValidTextColor;
        }

        /// <summary>버전 미입력·형식 오류(숫자·점만, 1~3 segment).</summary>
        private static string BuildVersionInvalidFormula(string cellRef)
        {
            string trimmed = $"TRIM({cellRef})";
            return "OR(" +
                   $"{trimmed}=\"\"," +
                   $"LEFT({trimmed},1)=\".\"," +
                   $"RIGHT({trimmed},1)=\".\"," +
                   $"ISNUMBER(SEARCH(\"..\",{cellRef}))," +
                   $"LEN({cellRef})-LEN(SUBSTITUTE({cellRef},\".\",\"\"))>2," +
                   $"{HasNonDigitOrDotCharacters(cellRef)}" +
                   ")";
        }

        /// <summary>타입 미입력·헤더 기준 허용 타입 불일치.</summary>
        private static string BuildTypeInvalidFormula(string typeCellRef, string headerCellRef)
        {
            string trimmedType = $"TRIM({typeCellRef})";
            string primitiveMismatch =
                "AND(" +
                $"LOWER({trimmedType})<>\"bool\"," +
                $"LOWER({trimmedType})<>\"uint\"," +
                $"LOWER({trimmedType})<>\"int\"," +
                $"LOWER({trimmedType})<>\"float\"," +
                $"LOWER({trimmedType})<>\"double\"," +
                $"LOWER({trimmedType})<>\"string\"," +
                $"LOWER({trimmedType})<>\"str\"" +
                ")";

            return "IF(LEFT(" + headerCellRef + ",1)=\"#\",FALSE," +
                   "OR(" +
                   "ISBLANK(" + typeCellRef + ")," +
                   "LEN(TRIM(" + typeCellRef + "))=0," +
                   "IF(LEFT(" + headerCellRef + ",1)=\"E\"," +
                   "AND(LOWER(" + trimmedType + ")<>LOWER(" + headerCellRef + "),LOWER(" + trimmedType + ")<>\"enum\")," +
                   primitiveMismatch +
                   ")" +
                   "))";
        }

        private static string HasNonDigitOrDotCharacters(string cellRef)
        {
            string expr = cellRef;
            foreach (char ch in "0123456789.")
                expr = "SUBSTITUTE(" + expr + ",\"" + ch + "\",\"\")";

            return "LEN(" + expr + ")>0";
        }

        private static IXLTable CreateSheetTable(IXLWorksheet ws)
        {
            int lastRow = FirstDataRow + InitialDataRows - 1;
            var tableRange = ws.Range(HeaderRow, NoteCol, lastRow, LastCol);
            IXLTable table = tableRange.CreateTable(TableName);
            table.ShowHeaderRow = true;
            table.ShowAutoFilter = false;
            table.Theme = XLTableTheme.TableStyleMedium2;
            return table;
        }

        private static string DeriveSheetName(string tableBaseName)
        {
            string name = string.IsNullOrWhiteSpace(tableBaseName) ? "Data" : tableBaseName.Trim();
            if (name.StartsWith("DT_", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(3);

            foreach (char c in new[] { ':', '\\', '/', '?', '*', '[', ']', '\'', '"' })
                name = name.Replace(c.ToString(), string.Empty);

            name = name.Trim();
            if (name.Length > 31)
                name = name.Substring(0, 31);

            return string.IsNullOrEmpty(name) ? "Data" : name;
        }
    }
}
