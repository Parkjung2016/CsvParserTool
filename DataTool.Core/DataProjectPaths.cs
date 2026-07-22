using System.IO;

namespace CSVParserTool
{
    /// <summary>Project root = Unity repo root (do not include <c>Assets</c>). 출력: <c>DataTables/Scripts</c>, <c>DataTables/Content/…</c>.</summary>
    public static class DataProjectPaths
    {
        public const string DataTablesFolderName = "DataTables";

        /// <summary>CSV·Bytes 등 런타임 에셋 묶음 폴더명 (<c>DataTables/Datas</c> 대신).</summary>
        public const string DataTablesContentFolderName = "Content";

        public static string UnityAssetsDir(string projectRoot) =>
            Path.Combine(projectRoot, "Assets");

        /// <summary>데이터 테이블 루트: <c>Assets\_Game\DataTables</c>.</summary>
        public static string GameDatasDir(string projectRoot) =>
            Path.Combine(UnityAssetsDir(projectRoot), "_Game", DataTablesFolderName);

        /// <summary><c>DataTables\Content</c> — CSV·Bytes.</summary>
        public static string DataTablesContentDir(string projectRoot) =>
            Path.Combine(GameDatasDir(projectRoot), DataTablesContentFolderName);

        public static string DataCsvDir(string projectRoot) =>
            Path.Combine(DataTablesContentDir(projectRoot), "CSV");

        public static string DataBytesDir(string projectRoot) =>
            Path.Combine(DataTablesContentDir(projectRoot), "Bytes");

        /// <summary>생성 C# (Container·ToolGenerated): <c>DataTables\Scripts</c>.</summary>
        public static string ScriptsDataDir(string projectRoot) =>
            Path.Combine(GameDatasDir(projectRoot), "Scripts");

        /// <summary>Unity Editor 전용: <c>DataTables\Scripts\Editor</c>.</summary>
        public static string ScriptsEditorDir(string projectRoot) =>
            Path.Combine(ScriptsDataDir(projectRoot), "Editor");
    }
}
