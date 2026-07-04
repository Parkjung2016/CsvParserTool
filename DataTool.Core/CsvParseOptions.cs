namespace CSVParserTool
{
    /// <summary>CSV 파싱·Export 옵션.</summary>
    public sealed class CsvParseOptions
    {
        /// <summary>Export 대상 버전. 컬럼 버전 행이 있으면 이 값 이하(포함)인 컬럼만 Export.</summary>
        public string ExportVersion { get; set; }
    }
}
