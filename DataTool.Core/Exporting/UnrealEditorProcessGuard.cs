using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSVParserTool.Exporting
{
    public sealed class UnrealEditorProcessInfo
    {
        public int ProcessId { get; }
        public string ProcessName { get; }
        public string WindowTitle { get; }

        public UnrealEditorProcessInfo(int processId, string processName, string windowTitle)
        {
            ProcessId = processId;
            ProcessName = processName ?? string.Empty;
            WindowTitle = windowTitle ?? string.Empty;
        }

        public override string ToString() => string.IsNullOrWhiteSpace(WindowTitle)
            ? $"{ProcessName} (PID {ProcessId})"
            : $"{WindowTitle} (PID {ProcessId})";
    }

    public static class UnrealEditorProcessGuard
    {
        public static IReadOnlyList<UnrealEditorProcessInfo> FindRunningEditors(string projectName)
        {
            var result = new List<UnrealEditorProcessInfo>();
            foreach (Process process in Process.GetProcesses())
            {
                using (process)
                {
                    try
                    {
                        string processName = process.ProcessName;
                        if (!IsEditorProcessName(processName))
                            continue;

                        string title = process.MainWindowTitle ?? string.Empty;
                        if (!MatchesProject(title, projectName))
                            continue;

                        result.Add(new UnrealEditorProcessInfo(process.Id, processName, title));
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                    }
                }
            }

            return result.OrderBy(item => item.ProcessId).ToArray();
        }

        public static string Describe(IReadOnlyList<UnrealEditorProcessInfo> editors) =>
            editors == null || editors.Count == 0
                ? string.Empty
                : string.Join(", ", editors.Select(editor => editor.ToString()));

        private static bool IsEditorProcessName(string processName) =>
            string.Equals(processName, "UnrealEditor", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processName, "UE4Editor", StringComparison.OrdinalIgnoreCase)
            || processName.StartsWith("UnrealEditor-", StringComparison.OrdinalIgnoreCase)
            || processName.StartsWith("UE4Editor-", StringComparison.OrdinalIgnoreCase);

        private static bool MatchesProject(string windowTitle, string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(windowTitle))
                return true;

            return windowTitle.IndexOf(projectName, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
