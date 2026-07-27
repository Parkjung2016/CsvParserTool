using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CSVParserTool.Exporting
{
    internal sealed class UnrealEngineInstallation
    {
        public string RootDirectory { get; }
        public string EditorCommandPath { get; }
        public string BuildBatchPath { get; }

        public UnrealEngineInstallation(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            EditorCommandPath = Path.Combine(rootDirectory, "Engine", "Binaries", "Win64", "UnrealEditor-Cmd.exe");
            BuildBatchPath = Path.Combine(rootDirectory, "Engine", "Build", "BatchFiles", "Build.bat");
        }

        public bool IsValid => File.Exists(EditorCommandPath) && File.Exists(BuildBatchPath);
    }

    internal static class UnrealEngineLocator
    {
        private static readonly Regex EngineAssociationPattern = new Regex(
            "\\\"EngineAssociation\\\"\\s*:\\s*\\\"(?<value>[^\\\"]+)\\\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static UnrealEngineInstallation Find(string projectFile)
        {
            if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile))
                throw new FileNotFoundException("Unreal .uproject 파일을 찾지 못했습니다.", projectFile);

            string association = ReadEngineAssociation(projectFile);
            foreach (string candidate in EnumerateCandidates(association))
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                var installation = new UnrealEngineInstallation(Path.GetFullPath(candidate.Trim()));
                if (installation.IsValid)
                    return installation;
            }

            string associationText = string.IsNullOrWhiteSpace(association) ? "(지정되지 않음)" : association;
            throw new InvalidOperationException(
                $"Unreal Engine 설치 경로를 찾지 못했습니다. .uproject EngineAssociation: {associationText}. " +
                "Epic Games Launcher 설치 또는 Unreal 소스 빌드 등록 상태를 확인하세요.");
        }

        public static string FindProjectFile(string projectRoot)
        {
            string[] files = Directory.GetFiles(projectRoot, "*.uproject", SearchOption.TopDirectoryOnly);
            if (files.Length != 1)
                throw new InvalidOperationException("Unreal 프로젝트 루트에는 .uproject 파일이 정확히 하나 있어야 합니다.");
            return files[0];
        }

        public static string FindEditorTarget(string projectRoot, string projectName)
        {
            string sourceRoot = Path.Combine(projectRoot, "Source");
            if (Directory.Exists(sourceRoot))
            {
                string editorTarget = Directory.GetFiles(sourceRoot, "*Editor.Target.cs", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(editorTarget))
                {
                    const string suffix = ".Target.cs";
                    string fileName = Path.GetFileName(editorTarget);
                    return fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                        ? fileName.Substring(0, fileName.Length - suffix.Length)
                        : Path.GetFileNameWithoutExtension(editorTarget);
                }
            }
            return projectName + "Editor";
        }

        private static string ReadEngineAssociation(string projectFile)
        {
            Match match = EngineAssociationPattern.Match(File.ReadAllText(projectFile));
            return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
        }

        private static IEnumerable<string> EnumerateCandidates(string association)
        {
            if (!string.IsNullOrWhiteSpace(association) && Directory.Exists(association))
                yield return association;

            string registered = ReadRegistryValue(
                Registry.CurrentUser,
                @"Software\Epic Games\Unreal Engine\Builds",
                association);
            if (!string.IsNullOrWhiteSpace(registered))
                yield return registered;

            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                string installed = ReadInstalledDirectory(association, view);
                if (!string.IsNullOrWhiteSpace(installed))
                    yield return installed;
            }

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(association))
                yield return Path.Combine(programFiles, "Epic Games", "UE_" + association);

            string launcherRoot = Path.Combine(programFiles, "Epic Games");
            if (Directory.Exists(launcherRoot))
                foreach (string launcherInstallation in ReadEpicLauncherInstallations(association))
                    yield return launcherInstallation;

            {
                foreach (string directory in Directory.GetDirectories(launcherRoot, "UE_*", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    yield return directory;
                }
            }
        }

        private static IEnumerable<string> ReadEpicLauncherInstallations(string association)
        {
            string manifestRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Epic",
                "EpicGamesLauncher",
                "Data",
                "Manifests");
            if (!Directory.Exists(manifestRoot))
                yield break;

            string expectedAppName = "UE_" + association;
            foreach (string manifestPath in Directory.GetFiles(manifestRoot, "*.item", SearchOption.TopDirectoryOnly))
            {
                string text;
                try { text = File.ReadAllText(manifestPath); }
                catch { continue; }

                if (!text.Contains("Engine/Binaries/Win64/UnrealEditor.exe")
                    || (!string.IsNullOrWhiteSpace(association)
                        && text.IndexOf("\"AppName\": \"" + expectedAppName + "\"", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    continue;
                }

                Match location = Regex.Match(
                    text,
                    "\\\"InstallLocation\\\"\\s*:\\s*\\\"(?<path>(?:\\\\.|[^\\\"])*)\\\"");
                if (!location.Success)
                    continue;

                string path = Regex.Unescape(location.Groups["path"].Value);
                if (!string.IsNullOrWhiteSpace(path))
                    yield return path;
            }
        }
        private static string ReadRegistryValue(RegistryKey hive, string keyPath, string valueName)
        {
            if (string.IsNullOrWhiteSpace(valueName))
                return null;
            try
            {
                using (RegistryKey key = hive.OpenSubKey(keyPath))
                    return key?.GetValue(valueName) as string;
            }
            catch
            {
                return null;
            }
        }

        private static string ReadInstalledDirectory(string association, RegistryView view)
        {
            if (string.IsNullOrWhiteSpace(association))
                return null;
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (RegistryKey key = baseKey.OpenSubKey(@"SOFTWARE\EpicGames\Unreal Engine\" + association))
                    return key?.GetValue("InstalledDirectory") as string;
            }
            catch
            {
                return null;
            }
        }
    }
}
