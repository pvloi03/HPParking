using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HPParking.Api.Common.Excel;
using HPParking.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Triển khai Core Generic Excel Engine sử dụng thư viện ClosedXML (ADR 0023)
    /// </summary>
    public class ClosedXmlExcelService : IExcelService
    {
        private readonly ILogger<ClosedXmlExcelService> _logger;

        public ClosedXmlExcelService(ILogger<ClosedXmlExcelService> logger)
        {
            _logger = logger;
        }

        public Task<byte[]> GenerateTemplateAsync<T>(
            ExcelProfile<T> profile,
            T? sampleData = null,
            string sheetName = "Template",
            CancellationToken cancellationToken = default) where T : class
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);

            var columns = profile.Columns;

            // 1. Định dạng dòng Tiêu đề (Header Row 1)
            var headerRow = ws.Row(1);
            headerRow.Height = 28;

            for (int i = 0; i < columns.Count; i++)
            {
                int colIdx = i + 1;
                var col = columns[i];
                var cell = ws.Cell(1, colIdx);

                cell.Value = col.ColumnName;

                // Styling ô tiêu đề
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.FromHtml("#1A73E8");
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F0FE");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#BDC1C6");

                // Thêm tooltip / comment nếu có
                if (!string.IsNullOrWhiteSpace(col.Comment))
                {
                    cell.GetComment().AddText(col.Comment);
                }

                // Thiết lập Dropdown Data Validation cho Enum hoặc danh sách lựa chọn
                if (col.DropdownOptions != null && col.DropdownOptions.Length > 0)
                {
                    var validationRange = ws.Range(2, colIdx, 1000, colIdx);
                    var validation = validationRange.CreateDataValidation();
                    validation.List(string.Join(",", col.DropdownOptions), true);
                    validation.InCellDropdown = true;
                    validation.InputTitle = col.ColumnName;
                    validation.InputMessage = $"Vui lòng chọn: {string.Join(", ", col.DropdownOptions)}";
                }

                // Điền dữ liệu dòng mẫu (Sample Row 2) nếu được cung cấp
                if (sampleData != null)
                {
                    var val = col.Property.GetValue(sampleData);
                    var sampleCell = ws.Cell(2, colIdx);
                    SetCellValue(sampleCell, val, col);
                    sampleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    sampleCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    sampleCell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E0E0E0");
                }
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return Task.FromResult(ms.ToArray());
        }

        public Task<byte[]> WriteAsync<T>(
            IEnumerable<T> data,
            ExcelProfile<T> profile,
            string sheetName = "Data",
            CancellationToken cancellationToken = default) where T : class
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);

            var columns = profile.Columns;

            // 1. Header Row
            var headerRow = ws.Row(1);
            headerRow.Height = 26;

            for (int i = 0; i < columns.Count; i++)
            {
                int colIdx = i + 1;
                var col = columns[i];
                var cell = ws.Cell(1, colIdx);

                cell.Value = col.ColumnName;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A73E8");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1557B0");
            }

            // 2. Data Rows
            int rowIdx = 2;
            foreach (var item in data)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    int colIdx = i + 1;
                    var col = columns[i];
                    var cell = ws.Cell(rowIdx, colIdx);
                    var rawVal = col.Property.GetValue(item);

                    SetCellValue(cell, rawVal, col);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#EEEEEE");
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                rowIdx++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return Task.FromResult(ms.ToArray());
        }

        public Task<ExcelImportResult<T>> ReadAsync<T>(
            Stream stream,
            ExcelProfile<T> profile,
            ExcelImportOptions? options = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            options ??= new ExcelImportOptions();
            var result = new ExcelImportResult<T>
            {
                IsDryRun = options.IsDryRun
            };

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                return Task.FromResult(result);
            }

            var lastRowUsed = ws.LastRowUsed();
            if (lastRowUsed == null || lastRowUsed.RowNumber() < 2)
            {
                return Task.FromResult(result);
            }

            var columns = profile.Columns;

            // Ánh xạ tiêu đề cột từ Row 1
            var headerMap = new Dictionary<int, ExcelColumnDefinition>();
            var headerRow = ws.Row(1);
            int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (int colIdx = 1; colIdx <= lastCol; colIdx++)
            {
                var headerText = headerRow.Cell(colIdx).GetString().Trim();
                if (string.IsNullOrEmpty(headerText)) continue;

                var matchingCol = columns.FirstOrDefault(c =>
                    string.Equals(c.ColumnName.Trim(), headerText, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Property.Name, headerText, StringComparison.OrdinalIgnoreCase));

                if (matchingCol != null)
                {
                    headerMap[colIdx] = matchingCol;
                }
            }

            int endRow = lastRowUsed.RowNumber();
            result.TotalRows = endRow - 1;

            for (int rowNumber = 2; rowNumber <= endRow; rowNumber++)
            {
                var row = ws.Row(rowNumber);
                if (row.IsEmpty())
                {
                    continue;
                }

                var item = new T();
                bool rowHasError = false;

                foreach (var col in columns)
                {
                    // Tìm ô tương ứng qua headerMap
                    var colEntry = headerMap.FirstOrDefault(kvp => kvp.Value == col);
                    IXLCell? cell = colEntry.Key > 0 ? row.Cell(colEntry.Key) : null;
                    string cellValue = cell?.GetString()?.Trim() ?? string.Empty;

                    // 1. Kiểm tra trường bắt buộc
                    if (col.IsRequired && string.IsNullOrWhiteSpace(cellValue))
                    {
                        result.Errors.Add(new ExcelRowError
                        {
                            Row = rowNumber,
                            Column = col.ColumnName,
                            Value = cellValue,
                            ErrorMessage = $"Cột '{col.ColumnName}' là bắt buộc, không được để trống."
                        });
                        rowHasError = true;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        continue;
                    }

                    // 2. Phân tích kiểu dữ liệu
                    try
                    {
                        if (col.CustomParser != null)
                        {
                            var parsedObj = col.CustomParser(cellValue);
                            col.Property.SetValue(item, parsedObj);
                        }
                        else
                        {
                            var parsedVal = ParseValue(cellValue, col.Property.PropertyType, col.EnumType);
                            col.Property.SetValue(item, parsedVal);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ExcelRowError
                        {
                            Row = rowNumber,
                            Column = col.ColumnName,
                            Value = cellValue,
                            ErrorMessage = $"Giá trị '{cellValue}' không đúng định dạng: {ex.Message}"
                        });
                        rowHasError = true;
                    }
                }

                if (rowHasError)
                {
                    result.FailedCount++;
                }
                else
                {
                    result.SuccessCount++;
                    result.SuccessData.Add(item);
                }
            }

            _logger.LogInformation("Hoàn tất phân tích tệp Excel: {Total} dòng, {Success} hợp lệ, {Failed} lỗi.",
                result.TotalRows, result.SuccessCount, result.FailedCount);

            return Task.FromResult(result);
        }

        private static void SetCellValue(IXLCell cell, object? val, ExcelColumnDefinition col)
        {
            if (val == null)
            {
                cell.Value = string.Empty;
                return;
            }

            if (col.CustomFormatter != null)
            {
                cell.Value = col.CustomFormatter(val);
                return;
            }

            if (val is DateTime dt)
            {
                cell.Value = dt;
                cell.Style.NumberFormat.Format = col.Format ?? "dd/MM/yyyy";
                return;
            }

            if (val is bool b)
            {
                cell.Value = b ? "Có" : "Không";
                return;
            }

            if (val is Enum enumVal)
            {
                cell.Value = enumVal.ToString();
                return;
            }

            if (val is int or long or double or decimal or float)
            {
                if (!string.IsNullOrEmpty(col.Format))
                {
                    cell.Style.NumberFormat.Format = col.Format;
                }
                cell.Value = Convert.ToDouble(val);
                return;
            }

            cell.Value = val.ToString();
        }

        private static object? ParseValue(string str, Type targetType, Type? enumType = null)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (enumType != null || underlyingType.IsEnum)
            {
                var targetEnum = enumType ?? underlyingType;
                if (Enum.TryParse(targetEnum, str, true, out var parsedEnum))
                {
                    return parsedEnum;
                }
                throw new FormatException($"Giá trị '{str}' không khớp với danh sách lựa chọn của {targetEnum.Name}.");
            }

            if (underlyingType == typeof(DateTime))
            {
                var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "yyyy/MM/dd" };
                if (DateTime.TryParseExact(str, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt;
                }
                if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDt))
                {
                    return parsedDt;
                }
                throw new FormatException("Định dạng ngày tháng không hợp lệ (chuẩn dd/MM/yyyy).");
            }

            if (underlyingType == typeof(int))
            {
                return int.Parse(str, CultureInfo.InvariantCulture);
            }

            if (underlyingType == typeof(long))
            {
                return long.Parse(str, CultureInfo.InvariantCulture);
            }

            if (underlyingType == typeof(double))
            {
                return double.Parse(str, CultureInfo.InvariantCulture);
            }

            if (underlyingType == typeof(decimal))
            {
                return decimal.Parse(str, CultureInfo.InvariantCulture);
            }

            if (underlyingType == typeof(bool))
            {
                var clean = str.Trim().ToLower();
                if (clean is "có" or "true" or "1" or "yes" or "hoạt động" or "active") return true;
                if (clean is "không" or "false" or "0" or "no" or "tạm khóa" or "inactive") return false;
                return bool.Parse(str);
            }

            return Convert.ChangeType(str, underlyingType, CultureInfo.InvariantCulture);
        }
    }
}
