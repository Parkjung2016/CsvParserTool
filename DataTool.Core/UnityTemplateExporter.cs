using System;
using System.IO;
using System.Reflection;
using System.Text;

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
            if (templateRoot == null && !HasEmbeddedTemplate("PJDev.Data.asmdef"))
            {
                throw new InvalidOperationException(
                    $"Unity template folder '{TemplateFolderName}' not found next to DataTool.Core.");
            }

            Directory.CreateDirectory(scriptsDir);
            string editorDir = Path.Combine(scriptsDir, "Editor");
            Directory.CreateDirectory(editorDir);

            CopyTemplate(templateRoot, "PJDev.Data.asmdef", Path.Combine(scriptsDir, "PJDev.Data.asmdef"), log);
            CopyTemplate(templateRoot, Path.Combine("Editor", "PJDev.Data.Editor.asmdef"),
                Path.Combine(editorDir, "PJDev.Data.Editor.asmdef"), log);
            CopyTemplate(templateRoot, Path.Combine("Editor", "Extender.cs"),
                Path.Combine(editorDir, "Extender.cs"), log);
        }

        private static void CopyTemplate(string templateRoot, string relativePath, string destPath, Action<string> log)
        {
            if (TryReadEmbeddedTemplate(relativePath, out string embedded))
            {
                GeneratedFileWriter.WriteAllTextIfChanged(destPath, embedded, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                log?.Invoke($"Deployed: {destPath}");
                return;
            }

            if (templateRoot == null)
                throw new FileNotFoundException("Unity template file not found.", relativePath);

            string sourcePath = Path.Combine(templateRoot, relativePath);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Unity template file not found.", sourcePath);

            File.Copy(sourcePath, destPath, overwrite: true);
            log?.Invoke($"Deployed: {destPath}");
        }

        private static bool HasEmbeddedTemplate(string relativePath) =>
            TryReadEmbeddedTemplate(relativePath, out _);

        private static bool TryReadEmbeddedTemplate(string relativePath, out string content)
        {
            content = null;
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            string needle = relativePath.Replace('\\', '.').Replace('/', '.');
            Assembly asm = typeof(UnityTemplateExporter).Assembly;
            foreach (string name in asm.GetManifestResourceNames())
            {
                if (!name.EndsWith(needle, StringComparison.OrdinalIgnoreCase))
                    continue;

                using (var stream = asm.GetManifestResourceStream(name))
                {
                    if (stream == null)
                        continue;

                    using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    {
                        content = reader.ReadToEnd();
                        return true;
                    }
                }
            }

            return false;
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
