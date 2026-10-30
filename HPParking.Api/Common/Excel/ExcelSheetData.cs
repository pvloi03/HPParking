namespace HPParking.Api.Common.Excel
{
    /// <summary>
    /// DTO mô tả dữ liệu cấu hình cho một trang tính trong tài liệu Excel (ADR 0023, ADR 0043)
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu dòng</typeparam>
    public record ExcelSheetData<T>(
        string SheetName,
        IEnumerable<T> Data,
        ExcelProfile<T> Profile,
        string? ReportTitle = null) where T : class;
}
