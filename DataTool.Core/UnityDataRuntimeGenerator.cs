using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSVParserTool
{
    public static partial class UnityDataRuntimeGenerator
    {
        public const string GlobalDataContainerFileName = "GlobalDataContainer.ToolGenerated.g.cs";
        public const string DataContainerBaseFileName = "DataContainerBase.ToolGenerated.g.cs";
        public const string InfoStorageFileName = "InfoStorage.ToolGenerated.g.cs";
        public const string DataSheetLoaderFileName = "DataSheetLoader.ToolGenerated.g.cs";

        private const string IfUniTask = "#if UNITASK_INSTALLED";
        private const string ElseNoUniTask = "#else";
        private const string EndIf = "#endif";

        public static void Write(
            string scriptsDir,
            IReadOnlyList<string> dataClassNames,
            IReadOnlyList<CsvTableParseResult> tables,
            Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(scriptsDir))
                throw new ArgumentException("Invalid scripts directory.", nameof(scriptsDir));

            Directory.CreateDirectory(scriptsDir);

            var unique = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var name in dataClassNames ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                    continue;

                unique.Add(name);
            }

            string basePath = Path.Combine(scriptsDir, DataContainerBaseFileName);
            GeneratedFileWriter.WriteAllTextIfChanged(basePath, BuildDataContainerBase(), new UTF8Encoding(false));
            log?.Invoke($"Generated: {basePath}");

            string infoStoragePath = Path.Combine(scriptsDir, InfoStorageFileName);
            GeneratedFileWriter.WriteAllTextIfChanged(infoStoragePath, BuildInfoStorageTypes(), new UTF8Encoding(false));
            log?.Invoke($"Generated: {infoStoragePath}");

            string loaderPath = Path.Combine(scriptsDir, DataSheetLoaderFileName);
            GeneratedFileWriter.WriteAllTextIfChanged(loaderPath, BuildDataSheetLoader(), new UTF8Encoding(false));
            log?.Invoke($"Generated: {loaderPath}");

            string editorDir = Path.Combine(scriptsDir, "Editor");
            Directory.CreateDirectory(editorDir);

            UnityTemplateExporter.Deploy(scriptsDir, log);

            string loaderEditorPath = Path.Combine(editorDir, "DataSheetLoaderEditor.ToolGenerated.cs");
            GeneratedFileWriter.WriteAllTextIfChanged(loaderEditorPath, BuildDataSheetLoaderEditor(), new UTF8Encoding(false));
            log?.Invoke($"Generated: {loaderEditorPath}");

            string globalPath = Path.Combine(scriptsDir, GlobalDataContainerFileName);
            GeneratedFileWriter.WriteAllTextIfChanged(globalPath, BuildGlobalDataContainer(unique), new UTF8Encoding(false));
            log?.Invoke($"Generated: {globalPath}");

            foreach (var name in unique)
            {
                string containerPath = Path.Combine(scriptsDir, $"{name}Container.cs");
                if (File.Exists(containerPath))
                    continue;

                GeneratedFileWriter.WriteAllTextIfChanged(containerPath, BuildContainerStub(name), new UTF8Encoding(false));
                log?.Invoke($"Generated: {containerPath}");
            }

            MpcCodeGenerator.Generate(scriptsDir, tables, log);
        }

        // =========================
        // GlobalDataContainer ??밴쉐
        // =========================

    }
}
