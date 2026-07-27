using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CSVParserTool.Exporting
{
    internal sealed class UnrealProjectModule
    {
        public string ProjectName { get; }
        public string ModuleName { get; }
        public string ModuleRoot { get; }

        public UnrealProjectModule(string projectName, string moduleName, string moduleRoot)
        {
            ProjectName = projectName;
            ModuleName = moduleName;
            ModuleRoot = moduleRoot;
        }
    }

    internal static class UnrealProjectModuleResolver
    {
        private const string BuildFileSuffix = ".Build.cs";
        private static readonly Regex DescriptorNamePattern = new Regex(
            "\\\"Name\\\"\\s*:\\s*\\\"(?<name>[A-Za-z_][A-Za-z0-9_]*)\\\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static UnrealProjectModule Resolve(string projectRoot, string projectFile)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Unreal 프로젝트 루트가 필요합니다.", nameof(projectRoot));
            if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile))
                throw new FileNotFoundException("Unreal 프로젝트의 .uproject 파일을 찾지 못했습니다.", projectFile);

            string sourceRoot = Path.Combine(projectRoot, "Source");
            if (!Directory.Exists(sourceRoot))
            {
                throw new InvalidOperationException(
                    "Unreal 프로젝트에 Source 폴더가 없습니다. C++ 클래스를 하나 추가해 런타임 모듈을 만든 뒤 Export하세요.");
            }

            var moduleRoots = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string buildFile in Directory.GetFiles(sourceRoot, "*.Build.cs", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(buildFile);
                if (!fileName.EndsWith(BuildFileSuffix, StringComparison.Ordinal))
                    continue;

                string moduleName = fileName.Substring(0, fileName.Length - BuildFileSuffix.Length);
                string moduleRoot = Path.GetDirectoryName(buildFile);
                if (moduleRoots.TryGetValue(moduleName, out string existing)
                    && !string.Equals(existing, moduleRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("같은 이름의 Unreal 모듈 Build.cs가 여러 개입니다: " + moduleName);
                }
                moduleRoots[moduleName] = moduleRoot;
            }

            if (moduleRoots.Count == 0)
            {
                throw new InvalidOperationException(
                    "Unreal C++ 모듈(*.Build.cs)을 찾지 못했습니다. C++ 클래스를 하나 추가한 뒤 Export하세요.");
            }

            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            string descriptor = File.ReadAllText(projectFile);
            foreach (Match match in DescriptorNamePattern.Matches(descriptor))
            {
                string declaredName = match.Groups["name"].Value;
                if (moduleRoots.TryGetValue(declaredName, out string declaredRoot))
                    return new UnrealProjectModule(projectName, declaredName, declaredRoot);
            }

            if (moduleRoots.TryGetValue(projectName, out string primaryRoot))
                return new UnrealProjectModule(projectName, projectName, primaryRoot);

            if (moduleRoots.Count == 1)
            {
                KeyValuePair<string, string> only = moduleRoots.Single();
                return new UnrealProjectModule(projectName, only.Key, only.Value);
            }

            throw new InvalidOperationException(
                ".uproject의 Modules 목록에서 기본 Unreal 런타임 모듈을 판별하지 못했습니다. 발견된 모듈: "
                + string.Join(", ", moduleRoots.Keys.OrderBy(name => name, StringComparer.Ordinal)));
        }
    }
}
