using System;
using System.IO;

namespace CSVParserTool
{
    /// <summary><c>DataTool.Core/UnityTemplates</c> → Unity <c>DataTables/Scripts</c> 복사.</summary>
    public static class UnityTemplateExporter
    {
        public const string TemplateFolderName = "UnityTemplates";

        public static void Deploy(string scriptsDir, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(scriptsDir))
                throw new ArgumentException("Invalid scripts directory.", nameof(scriptsDir));

            string templateRoot = ResolveTemplateRoot();
            if (templateRoot == null)
                throw new InvalidOperationException(
                    $"Unity template folder '{TemplateFolderName}' not found next to DataTool.Core.");

            Directory.CreateDirectory(scriptsDir);
            string editorDir = Path.Combine(scriptsDir, "Editor");
            Directory.CreateDirectory(editorDir);

            CopyTemplate(templateRoot, "PJDev.Data.asmdef", Path.Combine(scriptsDir, "PJDev.Data.asmdef"), log);
            CopyTemplate(templateRoot, Path.Combine("Editor", "PJDev.Data.Editor.asmdef"),
                Path.Combine(editorDir, "PJDev.Data.Editor.asmdef"), log);
            CopyTemplate(templateRoot, Path.Combine("Editor", "Extender.cs"),
                Path.Combine(editorDir, "Extender.cs"), log);

            RemoveLegacyGeneratedExtender(editorDir, log);
        }

        private static void RemoveLegacyGeneratedExtender(string editorDir, Action<string> log)
        {
            string legacy = Path.Combine(editorDir, "Extender.ToolGenerated.cs");
            if (!File.Exists(legacy))
                return;

            File.Delete(legacy);
            log?.Invoke($"Removed legacy generated extender: {legacy}");
        }

        private static void CopyTemplate(string templateRoot, string relativePath, string destPath, Action<string> log)
        {
            string sourcePath = Path.Combine(templateRoot, relativePath);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Unity template file not found.", sourcePath);

            File.Copy(sourcePath, destPath, overwrite: true);
            log?.Invoke($"Deployed: {destPath}");
        }

        private static string ResolveTemplateRoot()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
            {
                string candidate = Path.Combine(dir, TemplateFolderName);
                if (Directory.Exists(candidate)
                    && File.Exists(Path.Combine(candidate, "PJDev.Data.asmdef")))
                {
                    return candidate;
                }

                string devCandidate = Path.Combine(dir, "DataTool.Core", TemplateFolderName);
                if (Directory.Exists(devCandidate)
                    && File.Exists(Path.Combine(devCandidate, "PJDev.Data.asmdef")))
                {
                    return devCandidate;
                }

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }
    }
}
