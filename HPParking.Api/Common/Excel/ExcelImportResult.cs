namespace HPParking.Api.Common.Excel
{
    /// <summary>
    /// Kết quả nhập liệu tệp Excel generic
    /// </summary>
    public class ExcelImportResult<T> where T : class
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public bool IsDryRun { get; set; }
        public List<ExcelRowError> Errors { get; set; } = new();
        public List<T> SuccessData { get; set; } = new();
    }
}
