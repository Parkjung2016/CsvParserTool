using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CSVParserTool.Exporting
{
    internal static class UnrealDataTableImporter
    {
        private const int BuildTimeoutMilliseconds = 30 * 60 * 1000;
        private const int ImportTimeoutMilliseconds = 15 * 60 * 1000;

        public static int CompileAndImport(
            ExportTargetLayout layout,
            IReadOnlyList<CsvTableParseResult> tables,
            Action<string> log)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));

            string projectFile = UnrealEngineLocator.FindProjectFile(layout.ProjectRoot);
            UnrealEngineInstallation engine = UnrealEngineLocator.Find(projectFile);
            string editorTarget = UnrealEngineLocator.FindEditorTarget(layout.ProjectRoot, layout.ProjectName);

            log?.Invoke($"Unreal Engine: {engine.RootDirectory}");
            log?.Invoke($"Unreal Editor 타깃 컴파일: {editorTarget}");
            RunBuild(engine, editorTarget, projectFile, layout.ProjectRoot, log);

            string temporaryRoot = Path.Combine(
                Path.GetTempPath(),
                "PJDevDataTool",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryRoot);
            string scriptPath = Path.Combine(temporaryRoot, "ImportDataTables.py");
            string resultPath = Path.Combine(temporaryRoot, "ImportResult.json");

            try
            {
                GeneratedFileWriter.WriteAllTextIfChanged(
                    scriptPath,
                    BuildPythonScript(layout, tables, resultPath),
                    new UTF8Encoding(false));

                log?.Invoke("Unreal Commandlet: 메모리 데이터로 UDataTable 생성 및 갱신 시작");
                RunEditorCommandlet(engine, projectFile, scriptPath, layout.ProjectRoot, log);
                int importedCount = ReadImportResult(resultPath);
                log?.Invoke($"Unreal UDataTable Import 완료: {importedCount}개");
                return importedCount;
            }
            finally
            {
                TryDeleteDirectory(temporaryRoot);
            }
        }

        private static void RunBuild(
            UnrealEngineInstallation engine,
            string editorTarget,
            string projectFile,
            string workingDirectory,
            Action<string> log)
        {
            string command = Quote(engine.BuildBatchPath) + " "
                + Quote(editorTarget) + " Win64 Development "
                + Quote(projectFile)
                + " -WaitMutex -NoHotReloadFromIDE";
            string commandInterpreter = Environment.GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrWhiteSpace(commandInterpreter))
                commandInterpreter = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

            RunProcess(
                commandInterpreter,
                "/d /s /c \"" + command + "\"",
                workingDirectory,
                BuildTimeoutMilliseconds,
                "Unreal Build",
                log);
        }

        private static void RunEditorCommandlet(
            UnrealEngineInstallation engine,
            string projectFile,
            string scriptPath,
            string workingDirectory,
            Action<string> log)
        {
            string arguments = Quote(projectFile)
                + " -run=pythonscript"
                + " -script=" + Quote(scriptPath)
                + " -unattended -nop4 -nosplash -nullrhi -NoShaderCompile"
                + " -EnablePlugins=PythonScriptPlugin,EditorScriptingUtilities";
            RunProcess(
                engine.EditorCommandPath,
                arguments,
                workingDirectory,
                ImportTimeoutMilliseconds,
                "Unreal Import",
                log,
                throwOnNonZeroExit: false);
        }

        private static int RunProcess(
            string fileName,
            string arguments,
            string workingDirectory,
            int timeoutMilliseconds,
            string label,
            Action<string> log,
            bool throwOnNonZeroExit = true)
        {
            var output = new ConcurrentQueue<string>();
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                process.OutputDataReceived += (_, e) => CaptureLine(e.Data, label, output, log);
                process.ErrorDataReceived += (_, e) => CaptureLine(e.Data, label, output, log);

                try
                {
                    if (!process.Start())
                        throw new InvalidOperationException(label + " 프로세스를 시작하지 못했습니다.");
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (!process.WaitForExit(timeoutMilliseconds))
                    {
                        try { process.Kill(); } catch { }
                        throw new TimeoutException($"{label} 제한 시간({timeoutMilliseconds / 60000}분)을 초과했습니다.");
                    }
                    process.WaitForExit();
                }
                catch (Exception ex) when (!(ex is TimeoutException))
                {
                    throw new InvalidOperationException($"{label} 실행 실패: {ex.Message}", ex);
                }

                if (process.ExitCode != 0 && throwOnNonZeroExit)
                {
                    string tail = string.Join(Environment.NewLine, output.ToArray());
                    if (tail.Length > 6000)
                        tail = tail.Substring(tail.Length - 6000);
                    throw new InvalidOperationException($"{label} 실패 (ExitCode {process.ExitCode}).\r\n{tail}");
                }

                if (process.ExitCode != 0)
                    log?.Invoke($"[{label}] 프로젝트의 다른 Editor 오류로 ExitCode {process.ExitCode}이 반환되었습니다. DataTable Import 결과를 별도로 확인합니다.");

                return process.ExitCode;
            }
        }

        private static void CaptureLine(
            string line,
            string label,
            ConcurrentQueue<string> output,
            Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;
            output.Enqueue(line);
            while (output.Count > 120 && output.TryDequeue(out _)) { }
            log?.Invoke($"[{label}] {line}");
        }

        private static int ReadImportResult(string resultPath)
        {
            if (!File.Exists(resultPath))
                throw new InvalidOperationException("Unreal Import 결과 파일이 생성되지 않았습니다.");

            string json = File.ReadAllText(resultPath);
            if (!Regex.IsMatch(json, "\\\"success\\\"\\s*:\\s*true", RegexOptions.IgnoreCase))
            {
                Match error = Regex.Match(json, "\\\"error\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"");
                string message = error.Success ? error.Groups["value"].Value : json;
                throw new InvalidOperationException("Unreal UDataTable Import 실패: " + message);
            }

            Match imported = Regex.Match(json, "\\\"imported\\\"\\s*:\\s*(?<value>\\d+)");
            return imported.Success
                ? int.Parse(imported.Groups["value"].Value, CultureInfo.InvariantCulture)
                : 0;
        }

        private static string BuildPythonScript(
            ExportTargetLayout layout,
            IReadOnlyList<CsvTableParseResult> tables,
            string resultPath)
        {
            var payload = new StringBuilder();
            payload.AppendLine("TABLES = [");
            foreach (CsvTableParseResult table in tables)
            {
                string stem = table.ClassName.EndsWith("Data", StringComparison.Ordinal)
                    ? table.ClassName.Substring(0, table.ClassName.Length - 4)
                    : table.ClassName;
                string csv = CsvTableParser.BuildDeployedCsv(
                    table,
                    (column, value) => UnrealCodeGenerator.ToUnrealCsvValue(
                        table.ColumnTypes[column], value, table.EnumMembers));
                string csvBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv));
                payload.Append("    {'asset_name': '").Append(PythonString("DT_" + stem))
                    .Append("', 'row_header': '").Append(PythonString(UnrealCodeGenerator.GetRowHeaderFileName(table)))
                    .Append("', 'csv_base64': '").Append(csvBase64).AppendLine("'},");
            }
            payload.AppendLine("]");

            return
$@"import base64
import json
import os
import traceback
import unreal

MODULE_NAME = '{PythonString(layout.ModuleName)}'
DESTINATION = '/Game/PJDevData/DataTables'
RESULT_PATH = '{PythonString(resultPath)}'
{payload}


def write_result(result):
    with open(RESULT_PATH, 'w', encoding='utf-8') as output:
        json.dump(result, output, ensure_ascii=False, indent=2)


def load_row_struct(row_header):
    struct_name = os.path.splitext(os.path.basename(row_header))[0]
    object_path = '/Script/{{}}.{{}}'.format(MODULE_NAME, struct_name)
    row_struct = unreal.load_object(None, object_path)
    if row_struct is None:
        raise RuntimeError('RowStruct를 찾지 못했습니다: ' + object_path)
    return row_struct


def import_table(asset_tools, entry):
    asset_name = entry['asset_name']
    asset_path = DESTINATION + '/' + asset_name
    row_struct = load_row_struct(entry['row_header'])
    data_table = (unreal.EditorAssetLibrary.load_asset(asset_path)
                  if unreal.EditorAssetLibrary.does_asset_exist(asset_path) else None)
    if data_table is not None and not isinstance(data_table, unreal.DataTable):
        raise RuntimeError('같은 경로에 DataTable이 아닌 에셋이 있습니다: ' + asset_path)

    if data_table is None:
        factory = unreal.DataTableFactory()
        factory.set_editor_property('struct', row_struct)
        data_table = asset_tools.create_asset(asset_name, DESTINATION, unreal.DataTable, factory)
        if data_table is None:
            raise RuntimeError('DataTable 에셋 생성 실패: ' + asset_path)

    csv_text = base64.b64decode(entry['csv_base64']).decode('utf-8-sig')
    data_table.set_editor_property('import_key_field', 'Id')
    if not data_table.fill_from_csv_string(csv_text, row_struct):
        raise RuntimeError('메모리 CSV DataTable 변환 실패: ' + asset_name)
    if not unreal.EditorAssetLibrary.save_loaded_asset(data_table, only_if_is_dirty=False):
        raise RuntimeError('DataTable 저장 실패: ' + asset_path)

    row_count = len(unreal.DataTableFunctionLibrary.get_data_table_row_names(data_table))
    unreal.log('PJDevDataTool imported {{}} ({{}} rows)'.format(asset_path, row_count))
    return {{'asset': asset_path, 'rows': row_count}}


try:
    tools = unreal.AssetToolsHelpers.get_asset_tools()
    imported = [import_table(tools, entry) for entry in TABLES]
    write_result({{'success': True, 'imported': len(imported), 'tables': imported}})
except Exception as exc:
    details = traceback.format_exc()
    unreal.log_error('PJDevDataTool import failed: ' + details)
    write_result({{'success': False, 'imported': 0, 'error': str(exc), 'traceback': details}})
    raise
";
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);

                string parent = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parent)
                    && string.Equals(Path.GetFileName(parent), "PJDevDataTool", StringComparison.OrdinalIgnoreCase)
                    && Directory.Exists(parent)
                    && Directory.GetFileSystemEntries(parent).Length == 0)
                {
                    Directory.Delete(parent);
                }
            }
            catch
            {
                // Temporary diagnostics are best-effort cleanup only.
            }
        }

        private static string PythonString(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");

        private static string Quote(string value) =>
            "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
    }
}