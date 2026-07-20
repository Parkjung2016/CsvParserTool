using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal sealed class ToolUpdateInfo
    {
        public Version Version { get; set; }
        public string VersionText { get; set; }
        public string PublishedAt { get; set; }
        public string NotesUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string AssetName { get; set; }
        public bool IsNewer { get; set; }
    }

    internal static class ToolVersionInfo
    {
        public const string RepositoryUrl = "https://github.com/Parkjung2016/CsvParserTool";
        public const string ReleasesUrl = RepositoryUrl + "/releases";

        public static string ProductName =>
            typeof(ToolVersionInfo).Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? "PJDev Data Tool";

        public static Version Version => Normalize(typeof(ToolVersionInfo).Assembly.GetName().Version);
        public static string VersionText => Format(Version);

        public static string UpdateDate =>
            typeof(ToolVersionInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(x => string.Equals(x.Key, "UpdateDate", StringComparison.OrdinalIgnoreCase))?.Value
            ?? "확인할 수 없음";

        public static Version ParseVersion(string value)
        {
            string text = (value ?? string.Empty).Trim().TrimStart('v', 'V');
            int suffix = text.IndexOfAny(new[] { '-', '+' });
            if (suffix >= 0)
                text = text.Substring(0, suffix);

            return Version.TryParse(text, out Version parsed) ? Normalize(parsed) : null;
        }

        public static string Format(Version version) =>
            version == null ? "알 수 없음" : $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";

        private static Version Normalize(Version version) =>
            version == null
                ? new Version(0, 0, 0)
                : new Version(version.Major, version.Minor, Math.Max(0, version.Build));
    }

    internal static class ToolUpdateService
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/Parkjung2016/CsvParserTool/releases/latest";
        private const string PreferredAssetName = "Tool.zip";

        [DataContract]
        private sealed class GitHubRelease
        {
            [DataMember(Name = "tag_name")] public string TagName { get; set; }
            [DataMember(Name = "html_url")] public string HtmlUrl { get; set; }
            [DataMember(Name = "published_at")] public string PublishedAt { get; set; }
            [DataMember(Name = "assets")] public List<GitHubAsset> Assets { get; set; }
        }

        [DataContract]
        private sealed class GitHubAsset
        {
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "browser_download_url")] public string DownloadUrl { get; set; }
        }

        public static async Task<ToolUpdateInfo> CheckAsync(CancellationToken cancellationToken)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (var client = CreateHttpClient())
            using (var response = await client.GetAsync(LatestReleaseApi, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    throw new InvalidOperationException("아직 배포된 업데이트가 없습니다. 현재 설치된 버전을 계속 사용할 수 있습니다.");

                response.EnsureSuccessStatusCode();
                using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                    var release = (GitHubRelease)serializer.ReadObject(stream);
                    Version version = ToolVersionInfo.ParseVersion(release?.TagName);
                    if (version == null)
                        throw new InvalidOperationException("GitHub Release의 버전 태그를 읽을 수 없습니다.");

                    GitHubAsset asset = release.Assets?.FirstOrDefault(x =>
                        string.Equals(x.Name, PreferredAssetName, StringComparison.OrdinalIgnoreCase));

                    return new ToolUpdateInfo
                    {
                        Version = version,
                        VersionText = ToolVersionInfo.Format(version),
                        PublishedAt = FormatPublishedAt(release.PublishedAt),
                        NotesUrl = IsAllowedGitHubUrl(release.HtmlUrl) ? release.HtmlUrl : ToolVersionInfo.ReleasesUrl,
                        DownloadUrl = IsAllowedGitHubUrl(asset?.DownloadUrl) ? asset.DownloadUrl : null,
                        AssetName = asset?.Name,
                        IsNewer = version > ToolVersionInfo.Version
                    };
                }
            }
        }

        public static async Task<string> DownloadAsync(
            ToolUpdateInfo update,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            if (update == null || !update.IsNewer)
                throw new InvalidOperationException("설치할 새 버전이 없습니다.");
            if (!IsAllowedGitHubUrl(update.DownloadUrl))
                throw new InvalidOperationException("이 Release에 업데이트 ZIP이 없습니다.");

            string root = Path.Combine(Path.GetTempPath(), "PJDevDataToolUpdate", Guid.NewGuid().ToString("N"));
            string zipPath = Path.Combine(root, "update.zip");
            string payloadPath = Path.Combine(root, "payload");
            Directory.CreateDirectory(payloadPath);

            try
            {
                using (var client = CreateHttpClient())
                using (var response = await client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    long total = response.Content.Headers.ContentLength ?? -1;
                    using (Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var target = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        long received = 0;
                        int read;
                        while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            await target.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                            received += read;
                            if (total > 0)
                                progress?.Report((int)Math.Min(100, received * 100L / total));
                        }
                    }
                }

                ExtractSecurely(zipPath, payloadPath);
                if (!File.Exists(Path.Combine(payloadPath, Path.GetFileName(Application.ExecutablePath))))
                    throw new InvalidDataException("업데이트 ZIP에 DataToolGUI.exe가 없습니다.");
                progress?.Report(100);
                return payloadPath;
            }
            catch
            {
                TryDeleteDirectory(root);
                throw;
            }
        }

        public static void StartInstaller(string payloadPath)
        {
            string sourceExe = Application.ExecutablePath;
            string updaterExe = Path.Combine(Path.GetDirectoryName(payloadPath), "PJDevDataToolUpdater.exe");
            File.Copy(sourceExe, updaterExe, true);

            string config = sourceExe + ".config";
            if (File.Exists(config))
                File.Copy(config, updaterExe + ".config", true);

            var start = new ProcessStartInfo
            {
                FileName = updaterExe,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(updaterExe),
                Arguments = string.Join(" ", new[]
                {
                    "--apply-update",
                    Process.GetCurrentProcess().Id.ToString(),
                    Quote(payloadPath),
                    Quote(AppDomain.CurrentDomain.BaseDirectory),
                    Quote(Path.GetFileName(sourceExe))
                })
            };
            Process.Start(start);
        }

        public static bool TryRunInstallerMode(string[] args)
        {
            if (args == null || args.Length < 5 || !string.Equals(args[0], "--apply-update", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                if (int.TryParse(args[1], out int processId))
                {
                    try { Process.GetProcessById(processId).WaitForExit(30000); }
                    catch (ArgumentException) { }
                }

                string payloadPath = Path.GetFullPath(args[2]);
                string installPath = Path.GetFullPath(args[3]);
                string executableName = Path.GetFileName(args[4]);
                if (!Directory.Exists(payloadPath) || string.IsNullOrWhiteSpace(executableName))
                    throw new InvalidDataException("업데이트 파일 위치가 올바르지 않습니다.");

                CopyDirectory(payloadPath, installPath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(installPath, executableName),
                    WorkingDirectory = installPath,
                    UseShellExecute = true
                });
                TryDeleteDirectory(Path.GetDirectoryName(payloadPath));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "업데이트를 적용하지 못했습니다.\r\n\r\n" + ex.Message,
                    "PJDev Data Tool 업데이트",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            return true;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PJDev-Data-Tool/" + ToolVersionInfo.VersionText);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private static void ExtractSecurely(string zipPath, string destination)
        {
            string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                    if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("업데이트 ZIP에 안전하지 않은 경로가 있습니다.");
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    entry.ExtractToFile(target, true);
                }
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            string sourceRoot = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string full = Path.GetFullPath(file);
                if (!full.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("업데이트 파일 경로가 올바르지 않습니다.");
                string relative = full.Substring(sourceRoot.Length);
                string target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(full, target, true);
            }
        }

        private static bool IsAllowedGitHubUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps)
                return false;
            return uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatPublishedAt(string value) =>
            DateTimeOffset.TryParse(value, out DateTimeOffset parsed)
                ? parsed.ToLocalTime().ToString("yyyy-MM-dd")
                : "확인할 수 없음";

        private static string Quote(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";

        private static void TryDeleteDirectory(string path)
        {
            try { if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) Directory.Delete(path, true); }
            catch { }
        }
    }
}