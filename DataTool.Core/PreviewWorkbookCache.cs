using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace CSVParserTool
{
    /// <summary>Caches immutable worksheet lines; parsed table objects remain request-local because reference resolution mutates them.</summary>
    internal static class PreviewWorkbookCache
    {
        private const int MaxEntries = 96;
        private static readonly ConcurrentDictionary<string, Lazy<string[]>> LinesByFileVersion =
            new ConcurrentDictionary<string, Lazy<string[]>>(StringComparer.OrdinalIgnoreCase);

        public static string[] GetLines(string xlsxPath, int maxRows)
        {
            string key = BuildKey(xlsxPath, maxRows);
            TrimIfNeeded();
            Lazy<string[]> lazy = LinesByFileVersion.GetOrAdd(key, _ =>
                new Lazy<string[]>(
                    () => ReadLines(xlsxPath, maxRows),
                    LazyThreadSafetyMode.ExecutionAndPublication));
            try
            {
                return lazy.Value;
            }
            catch
            {
                LinesByFileVersion.TryRemove(key, out _);
                throw;
            }
        }

        private static string BuildKey(string xlsxPath, int maxRows)
        {
            var info = new FileInfo(xlsxPath);
            return string.Concat(
                info.FullName, "|", info.LastWriteTimeUtc.Ticks.ToString(), "|",
                info.Length.ToString(), "|", maxRows.ToString());
        }

        private static string[] ReadLines(string xlsxPath, int maxRows)
        {
            string csv = XlsxPreviewReader.ReadFirstWorksheetAsCsv(xlsxPath, maxRows);
            return csv.Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void TrimIfNeeded()
        {
            if (LinesByFileVersion.Count >= MaxEntries)
                LinesByFileVersion.Clear();
        }
    }
}