using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.Common.Excel;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ Core Generic Excel Engine xử lý đọc/ghi và sinh template bảng tính (ADR 0023)
    /// </summary>
    public interface IExcelService
    {
        /// <summary>
        /// Đọc và phân tích dữ liệu từ luồng tệp Excel thành danh sách đối tượng Generic
        /// </summary>
        Task<ExcelImportResult<T>> ReadAsync<T>(
            Stream stream,
            ExcelProfile<T> profile,
            ExcelImportOptions? options = null,
            CancellationToken cancellationToken = default) where T : class, new();

        /// <summary>
        /// Xuất danh sách đối tượng Generic thành mảng byte bảng tính Excel (.xlsx)
        /// </summary>
        Task<byte[]> WriteAsync<T>(
            IEnumerable<T> data,
            ExcelProfile<T> profile,
            string sheetName = "Data",
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Tự động sinh tệp Excel template chuẩn từ Fluent Profile (kèm header, styling, cell comments và dropdown validation)
        /// </summary>
        Task<byte[]> GenerateTemplateAsync<T>(
            ExcelProfile<T> profile,
            T? sampleData = null,
            string sheetName = "Template",
            CancellationToken cancellationToken = default) where T : class;
    }
}
