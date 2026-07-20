using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

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
        private const string LatestReleaseFeed = "https://github.com/Parkjung2016/CsvParserTool/releases.atom";
        private const string PreferredAssetName = "Tool.zip";
        private static readonly TimeSpan CheckCacheDuration = TimeSpan.FromMinutes(15);
        private static readonly SemaphoreSlim CheckLock = new SemaphoreSlim(1, 1);
        private static ToolUpdateInfo cachedUpdate;
        private static DateTimeOffset cachedUpdateAt;

        public static async Task<ToolUpdateInfo> CheckAsync(
            CancellationToken cancellationToken,
            bool forceRefresh = false)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            if (!forceRefresh && IsFreshCache())
                return cachedUpdate;

            await CheckLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!forceRefresh && IsFreshCache())
                    return cachedUpdate;

                try
                {
                    using (var client = CreateHttpClient())
                    using (var response = await client.GetAsync(LatestReleaseFeed, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            ToolUpdateInfo update = ReadLatestReleaseFromAtom(stream);
                            cachedUpdate = update;
                            cachedUpdateAt = DateTimeOffset.UtcNow;
                            return update;
                        }
                    }
                }
                catch when (cachedUpdate != null)
                {
                    // 일시적인 네트워크 오류가 있어도 마지막으로 확인한 버전 정보는 계속 보여준다.
                    return cachedUpdate;
                }
            }
            finally
            {
                CheckLock.Release();
            }
        }

        private static bool IsFreshCache() =>
            cachedUpdate != null && DateTimeOffset.UtcNow - cachedUpdateAt < CheckCacheDuration;

        private static ToolUpdateInfo ReadLatestReleaseFromAtom(Stream stream)
        {
            var document = new XmlDocument { XmlResolver = null };
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                CloseInput = false
            }))
            {
                document.Load(reader);
            }

            var namespaces = new XmlNamespaceManager(document.NameTable);
            namespaces.AddNamespace("atom", "http://www.w3.org/2005/Atom");
            XmlNode entry = document.SelectSingleNode("/atom:feed/atom:entry", namespaces);
            if (entry == null)
                throw new InvalidOperationException("아직 배포된 업데이트가 없습니다. 현재 설치된 버전을 계속 사용할 수 있습니다.");

            var link = entry.SelectSingleNode("atom:link[@rel='alternate']", namespaces) as XmlElement;
            string notesUrl = link?.GetAttribute("href");
            if (!Uri.TryCreate(notesUrl, UriKind.Absolute, out Uri releaseUri)
                || !IsAllowedGitHubUrl(notesUrl))
                throw new InvalidDataException("GitHub Release 주소를 읽을 수 없습니다.");

            string tag = Uri.UnescapeDataString(releaseUri.Segments.LastOrDefault()?.Trim('/') ?? string.Empty);
            Version version = ToolVersionInfo.ParseVersion(tag);
            if (version == null)
                throw new InvalidDataException("GitHub Release의 버전 태그를 읽을 수 없습니다.");

            string updated = entry.SelectSingleNode("atom:updated", namespaces)?.InnerText;
            string downloadUrl = ToolVersionInfo.RepositoryUrl
                + "/releases/download/" + Uri.EscapeDataString(tag)
                + "/" + PreferredAssetName;

            return new ToolUpdateInfo
            {
                Version = version,
                VersionText = ToolVersionInfo.Format(version),
                PublishedAt = FormatPublishedAt(updated),
                NotesUrl = notesUrl,
                DownloadUrl = downloadUrl,
                AssetName = PreferredAssetName,
                IsNewer = version > ToolVersionInfo.Version
            };
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
            client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
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