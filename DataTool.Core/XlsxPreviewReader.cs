using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace CSVParserTool
{
    /// <summary>UI Preview용 XLSX 경량 리더. 첫 워크시트 앞부분만 ZIP/XML로 읽는다.</summary>
    internal static class XlsxPreviewReader
    {
        private const string SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string OfficeRelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static string ReadFirstWorksheetAsCsv(string xlsxPath, int maxRows)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath) || !File.Exists(xlsxPath))
                throw new FileNotFoundException("XLSX not found.", xlsxPath);
            if (maxRows <= 0)
                maxRows = 64;

            using (var stream = new FileStream(
                xlsxPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                4096,
                FileOptions.SequentialScan))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false))
            {
                List<string> sharedStrings = ReadSharedStrings(archive);
                string relationshipId = ReadFirstSheetRelationshipId(archive);
                string worksheetPath = ResolveWorksheetPath(archive, relationshipId);
                ZipArchiveEntry worksheet = FindEntry(archive, worksheetPath)
                    ?? throw new InvalidDataException("XLSX first worksheet XML was not found.");
                List<List<string>> rows = ReadRows(worksheet, sharedStrings, maxRows);
                return BuildCsv(rows);
            }
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var result = new List<string>();
            ZipArchiveEntry entry = FindEntry(archive, "xl/sharedStrings.xml");
            if (entry == null)
                return result;

            using (Stream stream = entry.Open())
            using (XmlReader reader = CreateReader(stream))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "si")
                        continue;

                    var text = new StringBuilder();
                    using (XmlReader item = reader.ReadSubtree())
                    {
                        while (item.Read())
                        {
                            if (item.NodeType == XmlNodeType.Element && item.LocalName == "t")
                                text.Append(item.ReadElementContentAsString());
                        }
                    }
                    result.Add(text.ToString());
                }
            }
            return result;
        }

        private static string ReadFirstSheetRelationshipId(ZipArchive archive)
        {
            ZipArchiveEntry entry = FindEntry(archive, "xl/workbook.xml");
            if (entry == null)
                return null;

            using (Stream stream = entry.Open())
            using (XmlReader reader = CreateReader(stream))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "sheet")
                        return reader.GetAttribute("id", OfficeRelationshipNs);
                }
            }
            return null;
        }

        private static string ResolveWorksheetPath(ZipArchive archive, string relationshipId)
        {
            if (string.IsNullOrEmpty(relationshipId))
                return "xl/worksheets/sheet1.xml";

            ZipArchiveEntry entry = FindEntry(archive, "xl/_rels/workbook.xml.rels");
            if (entry == null)
                return "xl/worksheets/sheet1.xml";

            using (Stream stream = entry.Open())
            using (XmlReader reader = CreateReader(stream))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "Relationship")
                        continue;
                    if (!string.Equals(reader.GetAttribute("Id"), relationshipId, StringComparison.Ordinal))
                        continue;

                    string target = reader.GetAttribute("Target");
                    if (string.IsNullOrWhiteSpace(target))
                        break;
                    var baseUri = new Uri("http://xlsx.local/xl/workbook.xml");
                    return new Uri(baseUri, target).AbsolutePath.TrimStart('/');
                }
            }
            return "xl/worksheets/sheet1.xml";
        }

        private static List<List<string>> ReadRows(
            ZipArchiveEntry worksheet,
            IReadOnlyList<string> sharedStrings,
            int maxRows)
        {
            var rows = new List<List<string>>(Math.Min(maxRows, 64));
            using (Stream stream = worksheet.Open())
            using (XmlReader reader = CreateReader(stream))
            {
                while (reader.Read() && rows.Count < maxRows)
                {
                    if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "row")
                        continue;

                    var cells = new List<string>();
                    using (XmlReader row = reader.ReadSubtree())
                    {
                        while (row.Read())
                        {
                            if (row.NodeType != XmlNodeType.Element || row.LocalName != "c")
                                continue;

                            string reference = row.GetAttribute("r");
                            string cellType = row.GetAttribute("t");
                            int columnIndex = ColumnIndex(reference);
                            if (columnIndex < 0)
                                columnIndex = cells.Count;
                            while (cells.Count <= columnIndex)
                                cells.Add(string.Empty);

                            string rawValue = string.Empty;
                            var inlineText = new StringBuilder();
                            using (XmlReader cell = row.ReadSubtree())
                            {
                                while (cell.Read())
                                {
                                    if (cell.NodeType != XmlNodeType.Element)
                                        continue;
                                    if (cell.LocalName == "v")
                                        rawValue = cell.ReadElementContentAsString();
                                    else if (cell.LocalName == "t")
                                        inlineText.Append(cell.ReadElementContentAsString());
                                }
                            }
                            cells[columnIndex] = DecodeCell(cellType, rawValue, inlineText.ToString(), sharedStrings);
                        }
                    }
                    rows.Add(cells);
                }
            }
            return rows;
        }

        private static string DecodeCell(
            string cellType,
            string rawValue,
            string inlineText,
            IReadOnlyList<string> sharedStrings)
        {
            if (string.Equals(cellType, "inlineStr", StringComparison.Ordinal))
                return inlineText;
            if (string.Equals(cellType, "s", StringComparison.Ordinal)
                && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                && index >= 0 && index < sharedStrings.Count)
                return sharedStrings[index];
            if (string.Equals(cellType, "b", StringComparison.Ordinal))
                return rawValue == "1" ? "True" : "False";
            return rawValue ?? string.Empty;
        }

        private static int ColumnIndex(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return -1;
            int index = 0;
            int letters = 0;
            while (letters < reference.Length && char.IsLetter(reference[letters]))
            {
                index = index * 26 + (char.ToUpperInvariant(reference[letters]) - 'A' + 1);
                letters++;
            }
            return letters == 0 ? -1 : index - 1;
        }

        private static string BuildCsv(IReadOnlyList<List<string>> rows)
        {
            int columnCount = rows.Count == 0 ? 0 : rows.Max(row => row.Count);
            var result = new StringBuilder();
            foreach (List<string> row in rows)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    if (column > 0)
                        result.Append(',');
                    AppendCsvCell(result, column < row.Count ? row[column] : string.Empty);
                }
                result.AppendLine();
            }
            return result.ToString();
        }

        private static void AppendCsvCell(StringBuilder result, string value)
        {
            value = value ?? string.Empty;
            bool quote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!quote)
            {
                result.Append(value);
                return;
            }
            result.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
        }

        private static ZipArchiveEntry FindEntry(ZipArchive archive, string path) =>
            archive.Entries.FirstOrDefault(entry =>
                string.Equals(entry.FullName, path, StringComparison.OrdinalIgnoreCase));

        private static XmlReader CreateReader(Stream stream) => XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = true,
            IgnoreWhitespace = true,
            CloseInput = false
        });
    }
}