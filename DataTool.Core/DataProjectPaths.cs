using System.IO;

namespace CSVParserTool
{
    /// <summary>Project root = Unity repo root (do not include <c>Assets</c>). All outputs under <c>Assets\_Game\…</c>.</summary>
    public static class DataProjectPaths
    {
        public static string UnityAssetsDir(string projectRoot) =>
            Path.Combine(projectRoot, "Assets");

        /// <summary>게임 데이터 출력 루트: <c>Assets\_Game\04Datas</c> (CSV·NDB 상위).</summary>
        public static string GameDatasDir(string projectRoot) =>
            Path.Combine(UnityAssetsDir(projectRoot), "_Game", "04Datas");

        public static string DataCsvDir(string projectRoot) =>
            Path.Combine(GameDatasDir(projectRoot), "CSV");

        public static string DataNdbDir(string projectRoot) =>
            Path.Combine(GameDatasDir(projectRoot), "NDB");

        /// <summary>MessagePack 테이블 바이너리: <c>Assets\_Game\04Datas\Bytes</c> (<c>DT_Test.bytes</c>, CSV stem과 동일).</summary>
        public static string DataBytesDir(string projectRoot) =>
            Path.Combine(GameDatasDir(projectRoot), "Bytes");

        public static string ScriptsDataDir(string projectRoot) =>
            Path.Combine(UnityAssetsDir(projectRoot), "_Game", "03Scripts", "04Datas");
    }
}
