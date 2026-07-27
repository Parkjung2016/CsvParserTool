using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSVParserTool.Exporting
{
    public enum ExportPlatform
    {
        Unity,
        Unreal
    }

    [Flags]
    public enum ExportTargetCapabilities
    {
        None = 0,
        GeneratesSourceCode = 1,
        GeneratesBinaryData = 2,
        SupportsEditorImport = 4,
        SupportsRuntimeLoading = 8
    }

    public sealed class ExportTargetLayout
    {
        public string ProjectRoot { get; }
        public string ProjectName { get; }
        public string ModuleName { get; }
        public string StagingCsvDirectory { get; }
        public string RuntimeDataDirectory { get; }
        public string GeneratedCodeDirectory { get; }
        public string GeneratedEditorCodeDirectory { get; }

        public ExportTargetLayout(
            string projectRoot,
            string projectName,
            string stagingCsvDirectory,
            string runtimeDataDirectory,
            string generatedCodeDirectory,
            string generatedEditorCodeDirectory)
            : this(
                projectRoot,
                projectName,
                projectName,
                stagingCsvDirectory,
                runtimeDataDirectory,
                generatedCodeDirectory,
                generatedEditorCodeDirectory)
        {
        }

        public ExportTargetLayout(
            string projectRoot,
            string projectName,
            string moduleName,
            string stagingCsvDirectory,
            string runtimeDataDirectory,
            string generatedCodeDirectory,
            string generatedEditorCodeDirectory)
        {
            ProjectRoot = projectRoot ?? throw new ArgumentNullException(nameof(projectRoot));
            ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
            ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
            StagingCsvDirectory = stagingCsvDirectory ?? throw new ArgumentNullException(nameof(stagingCsvDirectory));
            RuntimeDataDirectory = runtimeDataDirectory ?? throw new ArgumentNullException(nameof(runtimeDataDirectory));
            GeneratedCodeDirectory = generatedCodeDirectory ?? throw new ArgumentNullException(nameof(generatedCodeDirectory));
            GeneratedEditorCodeDirectory = generatedEditorCodeDirectory ?? throw new ArgumentNullException(nameof(generatedEditorCodeDirectory));
        }
    }

    public interface IEngineExportTarget
    {
        ExportPlatform Platform { get; }
        string DisplayName { get; }
        ExportTargetCapabilities Capabilities { get; }
        bool CanOpenProject(string projectRoot);
        void ValidateProject(string projectRoot);
        ExportTargetLayout CreateLayout(string projectRoot);
    }

    public static class EngineExportTargetRegistry
    {
        private static readonly IReadOnlyDictionary<ExportPlatform, IEngineExportTarget> Targets =
            new Dictionary<ExportPlatform, IEngineExportTarget>
            {
                [ExportPlatform.Unity] = new UnityEngineExportTarget(),
                [ExportPlatform.Unreal] = new UnrealEngineExportTarget()
            };

        public static IReadOnlyCollection<IEngineExportTarget> All => Targets.Values.ToArray();

        public static IEngineExportTarget Get(ExportPlatform platform)
        {
            if (!Targets.TryGetValue(platform, out IEngineExportTarget target))
                throw new NotSupportedException($"지원하지 않는 Export 타깃입니다: {platform}");
            return target;
        }

        public static IEngineExportTarget Detect(string projectRoot)
        {
            IEngineExportTarget[] matches = Targets.Values
                .Where(target => target.CanOpenProject(projectRoot))
                .ToArray();
            if (matches.Length == 1)
                return matches[0];
            if (matches.Length == 0)
                throw new InvalidOperationException("Unity 또는 Unreal 프로젝트 루트를 찾지 못했습니다.");
            throw new InvalidOperationException("Unity와 Unreal 프로젝트 표식이 모두 있습니다. Export 타깃을 직접 선택하세요.");
        }
    }

    internal sealed class UnityEngineExportTarget : IEngineExportTarget
    {
        public ExportPlatform Platform => ExportPlatform.Unity;
        public string DisplayName => "Unity";
        public ExportTargetCapabilities Capabilities =>
            ExportTargetCapabilities.GeneratesSourceCode |
            ExportTargetCapabilities.GeneratesBinaryData |
            ExportTargetCapabilities.SupportsEditorImport |
            ExportTargetCapabilities.SupportsRuntimeLoading;

        public bool CanOpenProject(string projectRoot) =>
            Directory.Exists(projectRoot) &&
            Directory.Exists(Path.Combine(projectRoot, "Assets")) &&
            Directory.Exists(Path.Combine(projectRoot, "ProjectSettings"));

        public void ValidateProject(string projectRoot)
        {
            if (!CanOpenProject(projectRoot))
                throw new InvalidOperationException("Unity 프로젝트 루트를 선택하세요. Assets 폴더 자체가 아니라 그 상위 폴더입니다.");
        }

        public ExportTargetLayout CreateLayout(string projectRoot)
        {
            ValidateProject(projectRoot);
            string normalizedRoot = Path.GetFullPath(projectRoot);
            return new ExportTargetLayout(
                normalizedRoot,
                new DirectoryInfo(normalizedRoot).Name,
                DataProjectPaths.DataCsvDir(normalizedRoot),
                DataProjectPaths.DataBytesDir(normalizedRoot),
                DataProjectPaths.ScriptsDataDir(normalizedRoot),
                DataProjectPaths.ScriptsEditorDir(normalizedRoot));
        }
    }

    internal sealed class UnrealEngineExportTarget : IEngineExportTarget
    {
        public ExportPlatform Platform => ExportPlatform.Unreal;
        public string DisplayName => "Unreal Engine";
        public ExportTargetCapabilities Capabilities =>
            ExportTargetCapabilities.GeneratesSourceCode |
            ExportTargetCapabilities.SupportsEditorImport |
            ExportTargetCapabilities.SupportsRuntimeLoading;

        public bool CanOpenProject(string projectRoot) => FindProjectFile(projectRoot) != null;

        public void ValidateProject(string projectRoot)
        {
            if (!Directory.Exists(projectRoot))
                throw new InvalidOperationException("Unreal 프로젝트 루트가 존재하지 않습니다.");

            string[] projectFiles = Directory.GetFiles(projectRoot, "*.uproject", SearchOption.TopDirectoryOnly);
            if (projectFiles.Length == 0)
                throw new InvalidOperationException("선택한 폴더에 .uproject 파일이 없습니다.");
            if (projectFiles.Length > 1)
                throw new InvalidOperationException(".uproject 파일이 여러 개입니다. 하나의 Unreal 프로젝트 루트를 선택하세요.");
        }

        public ExportTargetLayout CreateLayout(string projectRoot)
        {
            ValidateProject(projectRoot);
            string normalizedRoot = Path.GetFullPath(projectRoot);
            string projectFile = FindProjectFile(normalizedRoot);
            UnrealProjectModule module = UnrealProjectModuleResolver.Resolve(normalizedRoot, projectFile);
            // Keep CSV/JSON outside Content so Unreal does not prompt to import generated sources.
            // Only finalized UDataTable assets belong under Content.
            string sourceDataRoot = Path.Combine(normalizedRoot, "Saved", "PJDevDataTool", "Source");
            return new ExportTargetLayout(
                normalizedRoot,
                module.ProjectName,
                module.ModuleName,
                Path.Combine(sourceDataRoot, "CSV"),
                sourceDataRoot,
                UnrealGeneratedSourceLayout.GetUnifiedDirectory(module.ModuleRoot),
                UnrealGeneratedSourceLayout.GetUnifiedDirectory(module.ModuleRoot));
        }

        private static string FindProjectFile(string projectRoot)
        {
            if (!Directory.Exists(projectRoot))
                return null;
            string[] files = Directory.GetFiles(projectRoot, "*.uproject", SearchOption.TopDirectoryOnly);
            return files.Length == 1 ? files[0] : null;
        }
    }
}
