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
            public Dictionary<string, int> MiniGameHighScores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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

        public static int GetMiniGameHighScore(string gameId, string difficultyId = null)
        {
            EnsureLoaded();
            string key = BuildMiniGameScoreKey(gameId, difficultyId);
            lock (Sync)
                return current.MiniGameHighScores.TryGetValue(key, out int score) ? score : 0;
        }

        /// <summary>메모리의 최고 기록만 갱신한다. 호출부에서 여러 갱신을 묶어 Save할 수 있다.</summary>
        public static bool TryUpdateMiniGameHighScore(string gameId, string difficultyId, int score)
        {
            if (score < 0)
                return false;

            EnsureLoaded();
            string key = BuildMiniGameScoreKey(gameId, difficultyId);
            lock (Sync)
            {
                if (current.MiniGameHighScores.TryGetValue(key, out int previous) && previous >= score)
                    return false;
                current.MiniGameHighScores[key] = score;
                return true;
            }
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
                        snapshot = new Snapshot();
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

                if (document.DocumentElement?.Name != "DataToolSettings")
                    return false;

                string Read(string name) => document.DocumentElement.SelectSingleNode(name)?.InnerText;
                string projectRoot = Read("ProjectRootPath");
                string excelSource = Read("ExcelSourceFolderPath");
                string darkMode = Read("DarkMode");
                string themeName = Read("ThemeName");
                string exportVersion = Read("ExportVersion");
                string removeOrphans = Read("RemoveOrphanArtifactsOnExport");
                if (projectRoot == null && excelSource == null && darkMode == null && themeName == null && exportVersion == null && removeOrphans == null)
                    return false;

                var miniGameHighScores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (XmlNode scoreNode in document.DocumentElement.SelectNodes("MiniGameHighScores/Score"))
                {
                    if (!(scoreNode is XmlElement scoreElement))
                        continue;
                    string key = scoreElement.GetAttribute("key");
                    if (!string.IsNullOrWhiteSpace(key)
                        && int.TryParse(scoreElement.GetAttribute("value"), out int value)
                        && value >= 0)
                        miniGameHighScores[key] = value;
                }

                snapshot = new Snapshot
                {
                    ProjectRootPath = projectRoot ?? string.Empty,
                    ExcelSourceFolderPath = excelSource ?? string.Empty,
                    DarkMode = bool.TryParse(darkMode, out bool dark) && dark,
                    ThemeName = string.IsNullOrWhiteSpace(themeName) ? "Default" : themeName,
                    ExportVersion = string.IsNullOrWhiteSpace(exportVersion) ? "1.0.0" : exportVersion,
                    RemoveOrphanArtifactsOnExport = !bool.TryParse(removeOrphans, out bool remove) || remove,
                    MiniGameHighScores = miniGameHighScores
                };
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("사용자 설정 읽기 실패 (" + path + "): " + ex.Message);
                return false;
            }
        }

        private static void WriteSnapshot(string path, Snapshot snapshot)
        {
            string temporaryPath = path + ".tmp";
            var document = new XmlDocument { XmlResolver = null };
            XmlElement root = document.CreateElement("DataToolSettings");
            root.SetAttribute("version", "2");
            document.AppendChild(root);
            Append(document, root, "ProjectRootPath", snapshot.ProjectRootPath);
            Append(document, root, "ExcelSourceFolderPath", snapshot.ExcelSourceFolderPath);
            Append(document, root, "DarkMode", snapshot.DarkMode.ToString());
            Append(document, root, "ThemeName", snapshot.ThemeName);
            Append(document, root, "ExportVersion", snapshot.ExportVersion);
            Append(document, root, "RemoveOrphanArtifactsOnExport", snapshot.RemoveOrphanArtifactsOnExport.ToString());

            XmlElement highScores = document.CreateElement("MiniGameHighScores");
            foreach (KeyValuePair<string, int> item in snapshot.MiniGameHighScores.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                XmlElement score = document.CreateElement("Score");
                score.SetAttribute("key", item.Key);
                score.SetAttribute("value", item.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                highScores.AppendChild(score);
            }
            root.AppendChild(highScores);

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

        private static string BuildMiniGameScoreKey(string gameId, string difficultyId)
        {
            string game = string.IsNullOrWhiteSpace(gameId) ? "unknown" : gameId.Trim().ToLowerInvariant();
            string difficulty = string.IsNullOrWhiteSpace(difficultyId) ? "default" : difficultyId.Trim().ToLowerInvariant();
            return game + "|" + difficulty;
        }

        private static void Append(XmlDocument document, XmlElement root, string name, string value)
        {
            XmlElement element = document.CreateElement(name);
            element.InnerText = value ?? string.Empty;
            root.AppendChild(element);
        }
    }
}
