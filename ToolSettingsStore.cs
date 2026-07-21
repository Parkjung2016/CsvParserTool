using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace CSVParserTool
{
    /// <summary>실행 파일 옆의 고정 XML에서 사용자 설정을 직접 읽고 저장한다.</summary>
    internal static class ToolSettingsStore
    {
        private const string SettingsFileName = "DataTool.settings.xml";
        private static readonly object Sync = new object();
        private static Snapshot current = new Snapshot();
        private static bool loaded;

        public static bool IsFirstRun { get; private set; }

        private sealed class Snapshot
        {
            public string ProjectRootPath = string.Empty;
            public string ExcelSourceFolderPath = string.Empty;
            public bool DarkMode;
            public string ThemeName = "Default";
            public string ExportVersion = "1.0.0";
            public bool RemoveOrphanArtifactsOnExport = true;
            public int Quality;
            public DateTime LastWriteTimeUtc;
        }

        public static string SettingsFilePath => Path.Combine(
            Path.GetDirectoryName(typeof(ToolSettingsStore).Assembly.Location),
            SettingsFileName);

        public static string ProjectRootPath
        {
            get { EnsureLoaded(); return current.ProjectRootPath; }
            set { EnsureLoaded(); current.ProjectRootPath = value ?? string.Empty; }
        }

        public static string ExcelSourceFolderPath
        {
            get { EnsureLoaded(); return current.ExcelSourceFolderPath; }
            set { EnsureLoaded(); current.ExcelSourceFolderPath = value ?? string.Empty; }
        }

        public static bool DarkMode
        {
            get { EnsureLoaded(); return current.DarkMode; }
            set { EnsureLoaded(); current.DarkMode = value; }
        }
        public static string ThemeName
        {
            get { EnsureLoaded(); return current.ThemeName; }
            set { EnsureLoaded(); current.ThemeName = string.IsNullOrWhiteSpace(value) ? "Default" : value; }
        }

        public static string ExportVersion
        {
            get { EnsureLoaded(); return current.ExportVersion; }
            set { EnsureLoaded(); current.ExportVersion = string.IsNullOrWhiteSpace(value) ? "1.0.0" : value; }
        }

        public static bool RemoveOrphanArtifactsOnExport
        {
            get { EnsureLoaded(); return current.RemoveOrphanArtifactsOnExport; }
            set { EnsureLoaded(); current.RemoveOrphanArtifactsOnExport = value; }
        }

        public static void Load()
        {
            lock (Sync)
            {
                if (loaded)
                    return;

                try
                {
                    IsFirstRun = !File.Exists(SettingsFilePath);
                    if (!TryReadSnapshot(SettingsFilePath, out Snapshot snapshot))
                    {
                        snapshot = FindBestLegacySnapshot() ?? new Snapshot();
                        WriteSnapshot(SettingsFilePath, snapshot);
                    }
                    current = snapshot;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("사용자 설정 불러오기 실패: " + ex.Message);
                    current = new Snapshot();
                }
                finally
                {
                    loaded = true;
                }
            }
        }

        public static void Save()
        {
            EnsureLoaded();
            lock (Sync)
            {
                try
                {
                    current.Quality = CalculateQuality(current);
                    WriteSnapshot(SettingsFilePath, current);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("사용자 설정 저장 실패: " + ex.Message);
                }
            }
        }

        private static void EnsureLoaded()
        {
            if (!loaded)
                Load();
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

                bool fixedFormat = document.DocumentElement?.Name == "DataToolSettings";
                string Read(string name)
                {
                    XmlNode node = fixedFormat
                        ? document.DocumentElement.SelectSingleNode(name)
                        : document.SelectSingleNode("//setting[@name='" + name + "']/value");
                    return node?.InnerText;
                }

                string projectRoot = Read("ProjectRootPath");
                string excelSource = Read("ExcelSourceFolderPath");
                string darkMode = Read("DarkMode");
                string themeName = Read("ThemeName");
                string exportVersion = Read("ExportVersion");
                string removeOrphans = Read("RemoveOrphanArtifactsOnExport");
                if (projectRoot == null && excelSource == null && darkMode == null && themeName == null && exportVersion == null && removeOrphans == null)
                    return false;

                snapshot = new Snapshot
                {
                    ProjectRootPath = projectRoot ?? string.Empty,
                    ExcelSourceFolderPath = excelSource ?? string.Empty,
                    DarkMode = bool.TryParse(darkMode, out bool dark) && dark,
                    ThemeName = string.IsNullOrWhiteSpace(themeName) ? "Default" : themeName,
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
            string temporaryPath = path + ".tmp";
            var document = new XmlDocument { XmlResolver = null };
            XmlElement root = document.CreateElement("DataToolSettings");
            root.SetAttribute("version", "1");
            document.AppendChild(root);
            Append(document, root, "ProjectRootPath", snapshot.ProjectRootPath);
            Append(document, root, "ExcelSourceFolderPath", snapshot.ExcelSourceFolderPath);
            Append(document, root, "DarkMode", snapshot.DarkMode.ToString());
            Append(document, root, "ThemeName", snapshot.ThemeName);
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
