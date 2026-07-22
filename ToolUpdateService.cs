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
        public static Version Version
        {
            get
            {
                Assembly assembly = typeof(ToolVersionInfo).Assembly;
                Version informationalVersion = ParseVersion(assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion);
                return informationalVersion ?? Normalize(assembly.GetName().Version);
            }
        }

        public static string VersionText => Format(Version);

        public static string UpdateDate =>
            typeof(ToolVersionInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(x => string.Equals(x.Key, "UpdateDate", StringComparison.OrdinalIgnoreCase))?.Value
            ?? "?ïÏù∏?????ÜÏùå";

        public static Version ParseVersion(string value)
        {
            string text = (value ?? string.Empty).Trim().TrimStart('v', 'V');
            int suffix = text.IndexOfAny(new[] { '-', '+' });
            if (suffix >= 0)
                text = text.Substring(0, suffix);

            return Version.TryParse(text, out Version parsed) ? Normalize(parsed) : null;
        }

        public static string Format(Version version) =>
            version == null ? "?????ÜÏùå" : $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";

        public static bool IsNewerThanInstalled(Version candidate) =>
            candidate != null && candidate.CompareTo(Version) > 0;

        private static Version Normalize(Version version) =>
            version == null
                ? new Version(0, 0, 0)
                : new Version(version.Major, version.Minor, Math.Max(0, version.Build));
    }

    internal static class ToolUpdateService
    {
        private const string LatestReleaseFeed = "https://github.com/Parkjung2016/CsvParserTool/releases.atom";
        private const string PreferredAssetName = "Tool.zip";
        private const string UpdateCacheFileName = "DataTool.update-cache.xml";
        private static readonly TimeSpan CheckCacheDuration = TimeSpan.FromMinutes(15);
        private static readonly SemaphoreSlim CheckLock = new SemaphoreSlim(1, 1);
        private static ToolUpdateInfo cachedUpdate;
        private static DateTimeOffset cachedUpdateAt;
        private static bool persistentCacheLoaded;

        public static async Task<ToolUpdateInfo> CheckAsync(
            CancellationToken cancellationToken,
            bool forceRefresh = false)
        {
            if (!ToolRuntimeEnvironment.UpdatesAllowed)
            {
                return new ToolUpdateInfo
                {
                    Version = ToolVersionInfo.Version,
                    VersionText = ToolVersionInfo.VersionText,
                    PublishedAt = "Í∞úÎ∞ú ?§Ìñâ",
                    NotesUrl = ToolVersionInfo.RepositoryUrl,
                    AssetName = string.Empty,
                    IsNewer = false
                };
            }

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            EnsurePersistentCacheLoaded();
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
                            TryWritePersistentCache(update, cachedUpdateAt);
                            return update;
                        }
                    }
                }
                catch when (cachedUpdate != null)
                {
                    // ?ºÏãú?ÅÏù∏ ?§Ìä∏?åÌÅ¨ ?§Î•òÍ∞Ä ?àÏñ¥??ÎßàÏ?ÎßâÏúºÎ°??ïÏù∏??Î≤ÑÏ†Ñ ?ïÎ≥¥??Í≥ÑÏÜç Î≥¥Ïó¨Ï§Ä??
                    return cachedUpdate;
                }
            }
            finally
            {
                CheckLock.Release();
            }
        }

        private static string UpdateCacheFilePath => Path.Combine(
            Path.GetDirectoryName(typeof(ToolUpdateService).Assembly.Location),
            UpdateCacheFileName);

        private static void EnsurePersistentCacheLoaded()
        {
            if (persistentCacheLoaded)
                return;
            persistentCacheLoaded = true;

            try
            {
                string path = UpdateCacheFilePath;
                if (!File.Exists(path))
                    return;

                var document = new XmlDocument { XmlResolver = null };
                using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    IgnoreComments = true,
                    IgnoreWhitespace = true
                }))
                {
                    document.Load(reader);
                }

                XmlElement root = document.DocumentElement;
                Version version = ToolVersionInfo.ParseVersion(root?.SelectSingleNode("Version")?.InnerText);
                string checkedAtText = root?.SelectSingleNode("CheckedAtUtc")?.InnerText;
                string notesUrl = root?.SelectSingleNode("NotesUrl")?.InnerText;
                if (version == null
                    || !DateTimeOffset.TryParse(checkedAtText, out DateTimeOffset checkedAt)
                    || !IsAllowedGitHubUrl(notesUrl))
                    return;

                var notesUri = new Uri(notesUrl);
                string tag = Uri.UnescapeDataString(notesUri.Segments.LastOrDefault()?.Trim('/') ?? string.Empty);
                if (string.IsNullOrWhiteSpace(tag))
                    return;

                cachedUpdate = new ToolUpdateInfo
                {
                    Version = version,
                    VersionText = ToolVersionInfo.Format(version),
                    PublishedAt = root.SelectSingleNode("PublishedAt")?.InnerText ?? "?ïÏù∏?????ÜÏùå",
                    NotesUrl = notesUrl,
                    DownloadUrl = ToolVersionInfo.RepositoryUrl
                        + "/releases/download/" + Uri.EscapeDataString(tag)
                        + "/" + PreferredAssetName,
                    AssetName = PreferredAssetName,
                    IsNewer = ToolVersionInfo.IsNewerThanInstalled(version)
                };
                cachedUpdateAt = checkedAt.ToUniversalTime();
            }
            catch
            {
                // Ï∫êÏãú???†ÌÉù ?¨Ìï≠?¥Î?Î°??êÏÉÅ?òÏóàÍ±∞ÎÇò ?ΩÏùÑ ???ÜÏúºÎ©??®Îùº???ïÏù∏?ºÎ°ú ÏßÑÌñâ?úÎã§.
            }
        }

        private static void TryWritePersistentCache(ToolUpdateInfo update, DateTimeOffset checkedAt)
        {
            if (update == null)
                return;

            try
            {
                string path = UpdateCacheFilePath;
                string temporaryPath = path + ".tmp";
                var document = new XmlDocument { XmlResolver = null };
                XmlElement root = document.CreateElement("DataToolUpdateCache");
                root.SetAttribute("version", "1");
                document.AppendChild(root);
                AppendCacheValue(document, root, "CheckedAtUtc", checkedAt.UtcDateTime.ToString("O"));
                AppendCacheValue(document, root, "Version", update.VersionText);
                AppendCacheValue(document, root, "PublishedAt", update.PublishedAt);
                AppendCacheValue(document, root, "NotesUrl", update.NotesUrl);

                using (XmlWriter writer = XmlWriter.Create(temporaryPath, new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false),
                    Indent = true,
                    NewLineChars = Environment.NewLine
                }))
                {
                    document.Save(writer);
                }
                File.Copy(temporaryPath, path, true);
                File.Delete(temporaryPath);
            }
            catch
            {
                // ?∞Í∏∞ Í∂åÌïú???ÜÏñ¥???ÑÏû¨ ?§Ìñâ Ï§ëÏùò Î©îÎ™®Î¶?Ï∫êÏãú??Í≥ÑÏÜç ?¨Ïö©?úÎã§.
            }
        }

        private static void AppendCacheValue(
            XmlDocument document,
            XmlElement root,
            string name,
            string value)
        {
            XmlElement element = document.CreateElement(name);
            element.InnerText = value ?? string.Empty;
            root.AppendChild(element);
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
                throw new InvalidOperationException("?ÑÏßÅ Î∞∞Ìè¨???ÖÎç∞?¥Ìä∏Í∞Ä ?ÜÏäµ?àÎã§. ?ÑÏû¨ ?§Ïπò??Î≤ÑÏ†Ñ??Í≥ÑÏÜç ?¨Ïö©?????àÏäµ?àÎã§.");

            var link = entry.SelectSingleNode("atom:link[@rel='alternate']", namespaces) as XmlElement;
            string notesUrl = link?.GetAttribute("href");
            if (!Uri.TryCreate(notesUrl, UriKind.Absolute, out Uri releaseUri)
                || !IsAllowedGitHubUrl(notesUrl))
                throw new InvalidDataException("GitHub Release Ï£ºÏÜåÎ•??ΩÏùÑ ???ÜÏäµ?àÎã§.");

            string tag = Uri.UnescapeDataString(releaseUri.Segments.LastOrDefault()?.Trim('/') ?? string.Empty);
            Version version = ToolVersionInfo.ParseVersion(tag);
            if (version == null)
                throw new InvalidDataException("GitHub Release??Î≤ÑÏ†Ñ ?úÍ∑∏Î•??ΩÏùÑ ???ÜÏäµ?àÎã§.");

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
                IsNewer = ToolVersionInfo.IsNewerThanInstalled(version)
            };
        }
        public static async Task<string> DownloadAsync(
            ToolUpdateInfo update,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            EnsureUpdatesAllowed();

            if (update == null || !update.IsNewer)
                throw new InvalidOperationException("?§Ïπò????Î≤ÑÏ†Ñ???ÜÏäµ?àÎã§.");
            if (!IsAllowedGitHubUrl(update.DownloadUrl))
                throw new InvalidOperationException("??Release???ÖÎç∞?¥Ìä∏ ZIP???ÜÏäµ?àÎã§.");

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
                string payloadExe = Path.Combine(payloadPath, Path.GetFileName(Application.ExecutablePath));
                if (!File.Exists(payloadExe))
                    throw new InvalidDataException("?ÖÎç∞?¥Ìä∏ ZIP??DataToolGUI.exeÍ∞Ä ?ÜÏäµ?àÎã§.");

                Version payloadVersion = ToolVersionInfo.ParseVersion(
                    AssemblyName.GetAssemblyName(payloadExe).Version.ToString());
                if (payloadVersion == null || payloadVersion != update.Version)
                {
                    throw new InvalidDataException(
                        $"?ÖÎç∞?¥Ìä∏ ZIP???§Ìñâ ?åÏùº Î≤ÑÏ†Ñ???¨Î∞îÎ•¥Ï? ?äÏäµ?àÎã§. ?îÏ≤≠: v{update.VersionText}, ?åÏùº: v{ToolVersionInfo.Format(payloadVersion)}");
                }

                progress?.Report(100);
                return payloadPath;
            }
            catch
            {
                TryDeleteDirectory(root);
                throw;
            }
        }

        public static void StartInstaller(string payloadPath, string expectedVersion)
        {
            EnsureUpdatesAllowed();

            string sourceExe = Path.GetFullPath(Application.ExecutablePath);
            string executableName = Path.GetFileName(sourceExe);
            string downloadedExe = Path.Combine(payloadPath, executableName);
            if (!File.Exists(downloadedExe))
                throw new FileNotFoundException("?§Ïö¥Î°úÎìú???ÖÎç∞?¥Ìä∏ ?§Ìñâ ?åÏùº??Ï∞æÏùÑ ???ÜÏäµ?àÎã§.", downloadedExe);
            string updaterExe = Path.Combine(Path.GetDirectoryName(payloadPath), "PJDevDataToolUpdater.exe");
            File.Copy(downloadedExe, updaterExe, true);

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
                    Quote(Path.GetDirectoryName(sourceExe)),
                    Quote(executableName),
                    Quote(expectedVersion)
                })
            };
            Process.Start(start);
        }

        private static void EnsureUpdatesAllowed()
        {
            if (!ToolRuntimeEnvironment.UpdatesAllowed)
                throw new InvalidOperationException("Í∞úÎ∞ú ?§Ìñâ?êÏÑú???ÖÎç∞?¥Ìä∏Î•??§Ïö¥Î°úÎìú?òÍ±∞???§Ïπò?????ÜÏäµ?àÎã§.");
        }
        public static bool TryRunInstallerMode(string[] args)
        {
            if (args == null || args.Length < 6 || !string.Equals(args[0], "--apply-update", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                if (int.TryParse(args[1], out int processId))
                {
                    try
                    {
                        using (Process runningTool = Process.GetProcessById(processId))
                        {
                            if (!runningTool.WaitForExit(60000))
                                throw new IOException("Í∏∞Ï°¥ Data Tool??Ï¢ÖÎ£å?òÏ? ?äÏïÑ ?ÖÎç∞?¥Ìä∏Î•??ÅÏö©?????ÜÏäµ?àÎã§.");
                        }
                    }
                    catch (ArgumentException) { }
                }

                string payloadPath = Path.GetFullPath(args[2]);
                string installPath = Path.GetFullPath(args[3]);
                string executableName = Path.GetFileName(args[4]);
                Version expectedVersion = ToolVersionInfo.ParseVersion(args[5]);
                if (!Directory.Exists(payloadPath) || string.IsNullOrWhiteSpace(executableName) || expectedVersion == null)
                    throw new InvalidDataException("?ÖÎç∞?¥Ìä∏ ?åÏùº ?ÑÏπò ?êÎäî Î≤ÑÏ†Ñ???¨Î∞îÎ•¥Ï? ?äÏäµ?àÎã§.");

                CopyDirectoryWithRetry(payloadPath, installPath);
                string installedExe = Path.Combine(installPath, executableName);
                Version installedVersion = ToolVersionInfo.ParseVersion(
                    AssemblyName.GetAssemblyName(installedExe).Version.ToString());
                if (installedVersion == null || installedVersion != expectedVersion)
                {
                    throw new InvalidDataException(
                        $"?ÖÎç∞?¥Ìä∏ ?ÅÏö© ??Î≤ÑÏ†Ñ Í≤ÄÏ¶ùÏóê ?§Ìå®?àÏäµ?àÎã§. ?àÏÉÅ: v{ToolVersionInfo.Format(expectedVersion)}, ?§Ïπò: v{ToolVersionInfo.Format(installedVersion)}");
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = installedExe,
                    WorkingDirectory = installPath,
                    UseShellExecute = true
                });
                TryDeleteDirectory(Path.GetDirectoryName(payloadPath));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "?ÖÎç∞?¥Ìä∏Î•??ÅÏö©?òÏ? Î™ªÌñà?µÎãà??\r\n\r\n" + ex.Message,
                    "PJDev Data Tool ?ÖÎç∞?¥Ìä∏",
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
                        throw new InvalidDataException("?ÖÎç∞?¥Ìä∏ ZIP???àÏ†Ñ?òÏ? ?äÏ? Í≤ΩÎ°úÍ∞Ä ?àÏäµ?àÎã§.");
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

        private static void CopyDirectoryWithRetry(string source, string destination)
        {
            Exception lastError = null;
            for (int attempt = 1; attempt <= 20; attempt++)
            {
                try
                {
                    CopyDirectory(source, destination);
                    return;
                }
                catch (IOException ex)
                {
                    lastError = ex;
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastError = ex;
                }

                Thread.Sleep(500);
            }

            throw new IOException(
                "?ÖÎç∞?¥Ìä∏ ?åÏùº???§Ïπò ?¥Îçî??Î≥µÏÇ¨?òÏ? Î™ªÌñà?µÎãà?? ?§Ìñâ Ï§ëÏù∏ Data Tool??Î™®Îëê ?´Í≥† ?§Ïãú ?úÎèÑ?òÏÑ∏??",
                lastError);
        }
        private static void CopyDirectory(string source, string destination)
        {
            string sourceRoot = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string full = Path.GetFullPath(file);
                if (!full.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("?ÖÎç∞?¥Ìä∏ ?åÏùº Í≤ΩÎ°úÍ∞Ä ?¨Î∞îÎ•¥Ï? ?äÏäµ?àÎã§.");
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
                : "?ïÏù∏?????ÜÏùå";

        private static string Quote(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";

        private static void TryDeleteDirectory(string path)
        {
            try { if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) Directory.Delete(path, true); }
            catch { }
        }
    }
}