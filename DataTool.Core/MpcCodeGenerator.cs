using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CSVParserTool
{
    /// <summary>MessagePack mpc — Unity <c>DataTables/Scripts</c>용 직렬화 코드 생성(IL2CPP·로드 성능).</summary>
    public static class MpcCodeGenerator
    {
        public const string GeneratedFileName = "MessagePackGenerated.cs";
        public const string ResolverTypeName = "PJDevDataGeneratedResolver";

        public static void Generate(string scriptsDir, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(scriptsDir))
                return;

            Directory.CreateDirectory(scriptsDir);
            string outputPath = Path.Combine(scriptsDir, GeneratedFileName);

            if (!TryRunMpc(scriptsDir, outputPath, log))
            {
                WriteFallbackResolver(outputPath);
                log?.Invoke(
                    "MessagePack: fallback resolver (Standard). 성능용 mpc 실패 — 솔루션 루트에서 dotnet tool restore 후 export 다시 실행.");
            }
        }

        private static bool TryRunMpc(string scriptsDir, string outputPath, Action<string> log)
        {
            string repoRoot = FindToolManifestRoot();
            if (repoRoot == null)
            {
                log?.Invoke("MessagePack mpc: dotnet-tools.json 을 찾지 못했습니다.");
                return false;
            }

            string tempOut = Path.Combine(Path.GetTempPath(), "DataToolMpc_" + Guid.NewGuid().ToString("N") + ".cs");
            string mpcArgs =
                "-i \"" + scriptsDir + "\" " +
                "-o \"" + tempOut + "\" " +
                "-n PJDev.Data " +
                "--resolvername " + ResolverTypeName;

            try
            {
                if (!TryDotnetToolRestore(repoRoot, log))
                    return false;

                string dotnetArgs = "tool run mpc -- " + mpcArgs;
                if (!TryStartProcess(repoRoot, "dotnet", dotnetArgs, out string stderr, timeOutMs: 120_000))
                {
                    string localMpc = Path.Combine(repoRoot, ".tools", "mpc.exe");
                    if (!File.Exists(localMpc) ||
                        !TryStartProcess(repoRoot, localMpc, mpcArgs, out stderr, timeOutMs: 120_000))
                    {
                        if (!string.IsNullOrWhiteSpace(stderr))
                            log?.Invoke("mpc: " + stderr.Trim());
                        return false;
                    }
                }

                if (!File.Exists(tempOut) || new FileInfo(tempOut).Length == 0)
                {
                    log?.Invoke("MessagePack mpc: 출력 파일이 비어 있습니다.");
                    return false;
                }

                File.Copy(tempOut, outputPath, overwrite: true);
                log?.Invoke($"MessagePack mpc: {outputPath}");
                return true;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempOut))
                        File.Delete(tempOut);
                }
                catch
                {
                }
            }
        }

        private static bool TryDotnetToolRestore(string repoRoot, Action<string> log)
        {
            if (TryStartProcess(repoRoot, "dotnet", "tool restore", out string stderr, timeOutMs: 180_000))
                return true;

            if (!string.IsNullOrWhiteSpace(stderr))
                log?.Invoke("dotnet tool restore: " + stderr.Trim());
            return false;
        }

        private static bool TryStartProcess(string workingDir, string fileName, string arguments, out string stderr, int timeOutMs)
        {
            stderr = string.Empty;
            try
            {
                using var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        WorkingDirectory = workingDir,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };

                p.Start();
                string err = p.StandardError.ReadToEnd();
                p.StandardOutput.ReadToEnd();
                if (!p.WaitForExit(timeOutMs))
                {
                    try
                    {
                        p.Kill();
                    }
                    catch
                    {
                    }

                    stderr = "timeout";
                    return false;
                }

                stderr = err;
                return p.ExitCode == 0;
            }
            catch (Exception ex)
            {
                stderr = ex.Message;
                return false;
            }
        }

        private static string FindToolManifestRoot()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
            {
                if (File.Exists(Path.Combine(dir, "dotnet-tools.json")))
                    return dir;

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }

        private static void WriteFallbackResolver(string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated — mpc 실패 시 fallback. DataTool export를 다시 실행하세요.>");
            sb.AppendLine("using MessagePack;");
            sb.AppendLine("using MessagePack.Resolvers;");
            sb.AppendLine();
            sb.AppendLine("namespace PJDev.Data.Resolvers");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {ResolverTypeName}");
            sb.AppendLine("    {");
            sb.AppendLine("        public static readonly IFormatterResolver Instance = StandardResolver.Instance;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
