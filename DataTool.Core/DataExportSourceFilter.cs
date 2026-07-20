using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSVParserTool
{
    /// <summary>XLSX 원본 폴더 기준 Export 대상·고아 파일 정리.</summary>
    internal static class DataExportSourceFilter
    {
        public static HashSet<string> CollectDtStemsFromXlsxFolder(string xlsxFolder)
        {
            var stems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(xlsxFolder) || !Directory.Exists(xlsxFolder))
                return stems;

            foreach (string path in Directory.GetFiles(xlsxFolder, "*.xlsx"))
            {
                string fileName = Path.GetFileName(path);
                if (fileName.StartsWith("~$", StringComparison.Ordinal))
                    continue;
                if (EnumCatalogService.IsCatalogPath(path))
                    continue;

                string stem = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrWhiteSpace(stem))
                    stems.Add(stem);
            }

            return stems;
        }

        public static string[] FilterCsvPathsByXlsxStems(string[] csvPaths, ICollection<string> allowedDtStems)
        {
            if (allowedDtStems == null || allowedDtStems.Count == 0)
                return Array.Empty<string>();

            return csvPaths
                .Where(p =>
                {
                    string stem = Path.GetFileNameWithoutExtension(p);
                    return !string.IsNullOrEmpty(stem) && allowedDtStems.Contains(stem);
                })
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static int RemoveOrphanArtifacts(
            string projectRoot,
            ICollection<string> allowedDtStems,
            Action<string> log)
        {
            if (allowedDtStems == null || allowedDtStems.Count == 0)
                return 0;

            var orphanStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int removed = 0;

            removed += RemoveOrphanFilesInDir(
                DataProjectPaths.DataCsvDir(projectRoot),
                "*.csv",
                allowedDtStems,
                orphanStems,
                log,
                label: "CSV");

            removed += RemoveOrphanFilesInDir(
                DataProjectPaths.DataBytesDir(projectRoot),
                "*.bytes",
                allowedDtStems,
                orphanStems,
                log,
                label: "Bytes");

            string scriptsDir = DataProjectPaths.ScriptsDataDir(projectRoot);
            foreach (string stem in orphanStems)
                removed += TryDeleteScriptArtifacts(scriptsDir, stem, log);

            return removed;
        }

        private static int RemoveOrphanFilesInDir(
            string dir,
            string pattern,
            ICollection<string> allowedDtStems,
            ISet<string> orphanStems,
            Action<string> log,
            string label)
        {
            if (!Directory.Exists(dir))
                return 0;

            int removed = 0;
            foreach (string path in Directory.GetFiles(dir, pattern))
            {
                string stem = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(stem) || allowedDtStems.Contains(stem))
                    continue;

                orphanStems.Add(stem);
                removed += TryDeleteFileWithMeta(path, log, $"orphan {label} (XLSX 없음)");
            }

            return removed;
        }

        private static int TryDeleteScriptArtifacts(string scriptsDir, string dtStem, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(scriptsDir) || !Directory.Exists(scriptsDir))
                return 0;

            string className = CsvClassGenerator.DataRecordClassNameFromFileBaseName(dtStem);
            int removed = 0;

            string[] targets =
            {
                Path.Combine(scriptsDir, className + ".cs"),
                Path.Combine(scriptsDir, className + "Container.cs"),
            };

            foreach (string path in targets)
            {
                if (!File.Exists(path))
                    continue;

                removed += TryDeleteFileWithMeta(path, log, "orphan script (XLSX 없음)");
            }

            return removed;
        }

        private static int TryDeleteFileWithMeta(string path, Action<string> log, string label)
        {
            int removed = 0;
            if (string.IsNullOrWhiteSpace(path))
                return removed;

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    removed++;
                    log?.Invoke($"Removed {label}: {Path.GetFileName(path)}");
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Skip delete {Path.GetFileName(path)}: {ex.Message}");
                    return removed;
                }
            }

            string metaPath = path + ".meta";
            if (!File.Exists(metaPath))
                return removed;

            try
            {
                File.Delete(metaPath);
                removed++;
                log?.Invoke($"Removed {label} meta: {Path.GetFileName(metaPath)}");
            }
            catch (Exception ex)
            {
                log?.Invoke($"Skip delete {Path.GetFileName(metaPath)}: {ex.Message}");
            }

            return removed;
        }
    }
}
