using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace CSVParserTool
{
    /// <summary>EXE 해시와 버전에 영향받지 않는 사용자 설정 저장소를 관리한다.</summary>
    internal static class UserSettingsMigration
    {
        private const string SettingsFolderName = "DataTool";
        private const string SettingsFileName = "settings.xml";
        private static bool initialized;

        private sealed class Snapshot
        {
            public string ProjectRootPath = string.Empty;
            public string ExcelSourceFolderPath = string.Empty;
            public bool DarkMode;
            public string ExportVersion = "1.0.0";
            public bool RemoveOrphanArtifactsOnExport = true;
            public int Quality;
            public DateTime LastWriteTimeUtc;
        }

        public static void InitializeAndRestore()
        {
            if (initialized)
                return;

            try
            {
                var settings = Properties.Settings.Default;
                Snapshot snapshot;
                if (!TryReadSnapshot(DurableSettingsPath, out snapshot))
                {
                    TryUpgradeFrameworkSettings(settings);
                    snapshot = SnapshotFromCurrentSettings(settings);

                    Snapshot legacy = FindBestLegacySnapshot();
                    if (legacy != null && legacy.Quality > snapshot.Quality)
                        snapshot = legacy;

                    WriteSnapshot(DurableSettingsPath, snapshot);
                }

                ApplySnapshot(settings, snapshot);
                settings.UpgradeRequired = false;
                settings.SettingsSaving += (_, __) =>
                {
                    try { WriteSnapshot(DurableSettingsPath, SnapshotFromCurrentSettings(settings)); }
                    catch (Exception ex) { Debug.WriteLine("고정 사용자 설정 저장 실패: " + ex.Message); }
                };
                initialized = true;
            }
            catch (Exception ex)
            {
                // 설정 복구 실패가 앱 실행을 막지는 않도록 기존 기본 설정으로 계속 실행한다.
                Debug.WriteLine("사용자 설정 초기화 실패: " + ex.Message);
            }
        }

        private static string DurableSettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PJDev",
            SettingsFolderName,
            SettingsFileName);

        private static void TryUpgradeFrameworkSettings(Properties.Settings settings)
        {
            try
            {
                if (settings.UpgradeRequired)
                    settings.Upgrade();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("기존 .NET 사용자 설정 마이그레이션 실패: " + ex.Message);
            }
        }

        private static Snapshot SnapshotFromCurrentSettings(Properties.Settings settings)
        {
            var snapshot = new Snapshot
            {
                ProjectRootPath = settings.ProjectRootPath ?? string.Empty,
                ExcelSourceFolderPath = settings.ExcelSourceFolderPath ?? string.Empty,
                DarkMode = settings.DarkMode,
                ExportVersion = string.IsNullOrWhiteSpace(settings.ExportVersion) ? "1.0.0" : settings.ExportVersion,
                RemoveOrphanArtifactsOnExport = settings.RemoveOrphanArtifactsOnExport
            };
            snapshot.Quality = CalculateQuality(snapshot);
            return snapshot;
        }

        private static void ApplySnapshot(Properties.Settings settings, Snapshot snapshot)
        {
            if (snapshot == null)
                return;

            settings.ProjectRootPath = snapshot.ProjectRootPath ?? string.Empty;
            settings.ExcelSourceFolderPath = snapshot.ExcelSourceFolderPath ?? string.Empty;
            settings.DarkMode = snapshot.DarkMode;
            settings.ExportVersion = string.IsNullOrWhiteSpace(snapshot.ExportVersion) ? "1.0.0" : snapshot.ExportVersion;
            settings.RemoveOrphanArtifactsOnExport = snapshot.RemoveOrphanArtifactsOnExport;
        }

        private static Snapshot FindBestLegacySnapshot()
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PJDev");
            if (!Directory.Exists(root))
                return null;

            var candidates = new List<Snapshot>();
            try
            {
                foreach (string path in Directory.GetFiles(root, "user.config", SearchOption.AllDirectories))
                {
                    if (path.IndexOf("PJDevDataToolUpdater.exe_", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (!TryReadSnapshot(path, out Snapshot snapshot))
                        continue;
                    snapshot.LastWriteTimeUtc = File.GetLastWriteTimeUtc(path);
                    candidates.Add(snapshot);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("이전 사용자 설정 검색 실패: " + ex.Message);
            }

            return candidates
                .OrderByDescending(candidate => candidate.Quality)
                .ThenByDescending(candidate => candidate.LastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static bool TryReadSnapshot(string path, out Snapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            try
            {
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

                bool durableFormat = document.DocumentElement?.Name == "DataToolSettings";
                string Read(string name)
                {
                    XmlNode node = durableFormat
                        ? document.DocumentElement.SelectSingleNode(name)
                        : document.SelectSingleNode("//setting[@name='" + name + "']/value");
                    return node?.InnerText;
                }

                string projectRoot = Read("ProjectRootPath");
                string excelSource = Read("ExcelSourceFolderPath");
                string darkMode = Read("DarkMode");
                string exportVersion = Read("ExportVersion");
                string removeOrphans = Read("RemoveOrphanArtifactsOnExport");
                if (projectRoot == null && excelSource == null && darkMode == null && exportVersion == null && removeOrphans == null)
                    return false;

                snapshot = new Snapshot
                {
                    ProjectRootPath = projectRoot ?? string.Empty,
                    ExcelSourceFolderPath = excelSource ?? string.Empty,
                    DarkMode = bool.TryParse(darkMode, out bool dark) && dark,
                    ExportVersion = string.IsNullOrWhiteSpace(exportVersion) ? "1.0.0" : exportVersion,
                    RemoveOrphanArtifactsOnExport = !bool.TryParse(removeOrphans, out bool remove) || remove
                };
                snapshot.Quality = CalculateQuality(snapshot);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("사용자 설정 읽기 실패 (" + path + "): " + ex.Message);
                return false;
            }
        }

        private static int CalculateQuality(Snapshot snapshot)
        {
            int quality = 0;
            if (!string.IsNullOrWhiteSpace(snapshot.ProjectRootPath)) quality += 4;
            if (!string.IsNullOrWhiteSpace(snapshot.ExcelSourceFolderPath)) quality += 4;
            if (snapshot.DarkMode) quality += 1;
            if (!string.IsNullOrWhiteSpace(snapshot.ExportVersion)) quality += 1;
            return quality;
        }

        private static void WriteSnapshot(string path, Snapshot snapshot)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string temporaryPath = path + ".tmp";

            var document = new XmlDocument { XmlResolver = null };
            XmlElement root = document.CreateElement("DataToolSettings");
            root.SetAttribute("version", "1");
            document.AppendChild(root);
            Append(document, root, "ProjectRootPath", snapshot.ProjectRootPath);
            Append(document, root, "ExcelSourceFolderPath", snapshot.ExcelSourceFolderPath);
            Append(document, root, "DarkMode", snapshot.DarkMode.ToString());
            Append(document, root, "ExportVersion", snapshot.ExportVersion);
            Append(document, root, "RemoveOrphanArtifactsOnExport", snapshot.RemoveOrphanArtifactsOnExport.ToString());

            using (XmlWriter writer = XmlWriter.Create(temporaryPath, new XmlWriterSettings
            {
                Encoding = new System.Text.UTF8Encoding(false),
                Indent = true,
                NewLineChars = Environment.NewLine
            }))
            {
                document.Save(writer);
            }
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }

        private static void Append(XmlDocument document, XmlElement root, string name, string value)
        {
            XmlElement element = document.CreateElement(name);
            element.InnerText = value ?? string.Empty;
            root.AppendChild(element);
        }
    }
}
