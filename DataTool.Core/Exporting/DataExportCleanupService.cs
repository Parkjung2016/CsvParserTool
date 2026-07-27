using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CSVParserTool.Exporting
{
    public sealed class DataExportCleanupPlan
    {
        internal DataExportCleanupPlan(
            ExportPlatform platform,
            string projectRoot,
            string projectName,
            IEnumerable<string> directoryPaths,
            IEnumerable<string> filePaths)
        {
            Platform = platform;
            ProjectRoot = Path.GetFullPath(projectRoot);
            ProjectName = projectName ?? string.Empty;
            DirectoryPaths = new ReadOnlyCollection<string>(
                (directoryPaths ?? Array.Empty<string>()).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            FilePaths = new ReadOnlyCollection<string>(
                (filePaths ?? Array.Empty<string>()).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        public ExportPlatform Platform { get; }
        public string ProjectRoot { get; }
        public string ProjectName { get; }
        public IReadOnlyList<string> DirectoryPaths { get; }
        public IReadOnlyList<string> FilePaths { get; }

        public IReadOnlyList<string> ExistingPaths => DirectoryPaths
            .Where(Directory.Exists)
            .Concat(FilePaths.Where(File.Exists))
            .ToArray();

        public bool HasArtifacts => ExistingPaths.Count > 0;
    }

    public sealed class DataExportCleanupResult
    {
        internal DataExportCleanupResult(int removedDirectories, int removedFiles)
        {
            RemovedDirectoryCount = removedDirectories;
            RemovedFileCount = removedFiles;
        }

        public int RemovedDirectoryCount { get; }
        public int RemovedFileCount { get; }
    }

    public static class DataExportCleanupService
    {
        public static DataExportCleanupPlan CreatePlan(string projectRoot, ExportPlatform platform)
        {
            IEngineExportTarget target = EngineExportTargetRegistry.Get(platform);
            ExportTargetLayout layout = target.CreateLayout(projectRoot);
            if (platform == ExportPlatform.Unity)
            {
                string dataTablesRoot = DataProjectPaths.GameDatasDir(layout.ProjectRoot);
                return new DataExportCleanupPlan(
                    platform,
                    layout.ProjectRoot,
                    layout.ProjectName,
                    new[] { dataTablesRoot },
                    new[] { dataTablesRoot + ".meta" });
            }

            var unrealDirectories = new List<string>
            {
                layout.GeneratedCodeDirectory,
                Path.Combine(layout.ProjectRoot, "Content", "PJDevData"),
                Path.Combine(layout.ProjectRoot, "Saved", "PJDevDataTool")
            };
            return new DataExportCleanupPlan(
                platform,
                layout.ProjectRoot,
                layout.ProjectName,
                unrealDirectories,
                Array.Empty<string>());
        }

        public static DataExportCleanupResult Execute(DataExportCleanupPlan plan, Action<string> log = null)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (plan.Platform == ExportPlatform.Unreal)
            {
                IReadOnlyList<UnrealEditorProcessInfo> editors =
                    UnrealEditorProcessGuard.FindRunningEditors(plan.ProjectName);
                if (editors.Count > 0)
                {
                    throw new InvalidOperationException(
                        "생성된 C++ 코드와 UDataTable을 안전하게 정리하려면 Unreal Editor를 종료해야 합니다. 실행 중: "
                        + UnrealEditorProcessGuard.Describe(editors));
                }
            }

            int removedDirectories = 0;
            int removedFiles = 0;
            foreach (string directory in plan.DirectoryPaths)
            {
                EnsureSafeTarget(plan.ProjectRoot, directory);
                if (!Directory.Exists(directory))
                    continue;

                MakeFilesWritable(directory);
                Directory.Delete(directory, true);
                removedDirectories++;
                log?.Invoke("정리된 폴더: " + directory);
            }

            foreach (string file in plan.FilePaths)
            {
                EnsureSafeTarget(plan.ProjectRoot, file);
                if (!File.Exists(file))
                    continue;

                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
                removedFiles++;
                log?.Invoke("정리된 파일: " + file);
            }

            return new DataExportCleanupResult(removedDirectories, removedFiles);
        }

        private static void EnsureSafeTarget(string projectRoot, string targetPath)
        {
            string root = AppendSeparator(Path.GetFullPath(projectRoot));
            string target = Path.GetFullPath(targetPath);
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                || string.Equals(target.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("프로젝트 밖의 경로는 정리할 수 없습니다: " + target);
            }
        }

        private static string AppendSeparator(string path) =>
            path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;

        private static void MakeFilesWritable(string directory)
        {
            foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
        }
    }
}