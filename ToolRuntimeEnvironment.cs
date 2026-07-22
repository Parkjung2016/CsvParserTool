using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CSVParserTool
{
    internal static class ToolRuntimeEnvironment
    {
#if DEBUG
        private const bool IsDebugBuild = true;
#else
        private const bool IsDebugBuild = false;
#endif

        public static bool IsDevelopmentRun { get; } = DetectDevelopmentRun();
        public static bool UpdatesAllowed => !IsDevelopmentRun;

        private static bool DetectDevelopmentRun()
        {
            if (IsDebugBuild || Debugger.IsAttached)
                return true;

            string executablePath = typeof(ToolRuntimeEnvironment).Assembly.Location;
            if (string.IsNullOrWhiteSpace(executablePath))
                return true;

            try
            {
                executablePath = Path.GetFullPath(executablePath);
                if (File.Exists(Path.ChangeExtension(executablePath, ".pdb")))
                    return true;

                string directory = Path.GetDirectoryName(executablePath);
                if (IsVisualStudioOutputDirectory(directory) || IsInsideSourceProject(directory))
                    return true;
            }
            catch
            {
                // 판별할 수 없으면 배포 실행으로 취급한다. 업데이트 서비스 자체 검증은 그대로 적용된다.
            }

            return false;
        }

        private static bool IsVisualStudioOutputDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                return false;

            string normalized = directory.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string[] segments = normalized.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                if (string.Equals(segments[i], "obj", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (!string.Equals(segments[i], "bin", StringComparison.OrdinalIgnoreCase) || i + 1 >= segments.Length)
                    continue;

                string configuration = segments[i + 1];
                if (string.Equals(configuration, "Debug", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(configuration, "Release", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsInsideSourceProject(string directory)
        {
            for (int depth = 0; depth < 6 && !string.IsNullOrWhiteSpace(directory); depth++)
            {
                if (File.Exists(Path.Combine(directory, "CSVParserTool.csproj")))
                    return true;
                directory = Path.GetDirectoryName(directory);
            }

            return false;
        }
    }
}