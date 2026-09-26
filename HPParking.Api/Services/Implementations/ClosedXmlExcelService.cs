using ClosedXML.Excel;
using HPParking.Api.Common.Excel;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Enums;
using System.Globalization;

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
            string? reportTitle = null,
            CancellationToken cancellationToken = default) where T : class
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);

            var columns = profile.Columns;
            int totalCols = Math.Max(columns.Count, 1);

            var effectiveTitle = reportTitle;
            if (string.IsNullOrWhiteSpace(effectiveTitle) && sheetName is not "Data" and not "DataSheet" and not "Template")
            {
                effectiveTitle = sheetName.Replace('_', ' ');
            }

            int headerRowIndex = 1;
            int dataStartRowIndex = 2;

            // Khối Report Title Banner (nếu có tiêu đề báo cáo)
            if (!string.IsNullOrWhiteSpace(effectiveTitle))
            {
                // 1. Dòng Tên Đơn vị / Hệ thống (Row 1)
                ws.Range(1, 1, 1, totalCols).Merge();
                var orgCell = ws.Cell(1, 1);
                orgCell.Value = "HỆ THỐNG QUẢN LÝ BÃI ĐỖ XE THÔNG MINH HPPARKING";
                orgCell.Style.Font.Bold = true;
                orgCell.Style.Font.FontSize = 10;
                orgCell.Style.Font.FontColor = XLColor.FromHtml("#5F6368");
                orgCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                orgCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(1).Height = 22;

                // 2. Dòng Tiêu đề Báo cáo nổi bật (Row 2)
                ws.Range(2, 1, 2, totalCols).Merge();
                var titleCell = ws.Cell(2, 1);
                titleCell.Value = effectiveTitle.Trim().ToUpperInvariant();
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 15;
                titleCell.Style.Font.FontColor = XLColor.FromHtml("#1A73E8");
                titleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F0FE");
                titleCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                titleCell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1A73E8");
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(2).Height = 36;

                // 3. Dòng Thời gian kết xuất & Metadata (Row 3)
                ws.Range(3, 1, 3, totalCols).Merge();
                var metaCell = ws.Cell(3, 1);
                metaCell.Value = $"Thời gian kết xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Hệ thống trích xuất tự động";
                metaCell.Style.Font.Italic = true;
                metaCell.Style.Font.FontSize = 9.5;
                metaCell.Style.Font.FontColor = XLColor.FromHtml("#5F6368");
                metaCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                metaCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(3).Height = 20;

                // 4. Dòng đệm phân cách (Row 4)
                ws.Row(4).Height = 10;

                headerRowIndex = 5;
                dataStartRowIndex = 6;
            }

            // Định dạng dòng Tiêu đề Bảng dữ liệu (Table Header Row)
            var headerRow = ws.Row(headerRowIndex);
            headerRow.Height = 28;

            for (int i = 0; i < columns.Count; i++)
            {
                int colIdx = i + 1;
                var col = columns[i];
                var cell = ws.Cell(headerRowIndex, colIdx);

                cell.Value = col.ColumnName;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10.5;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A73E8");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1557B0");
            }

            // Dữ liệu bảng (Data Rows)
            int rowIdx = dataStartRowIndex;
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
                ws.Row(rowIdx).Height = 22;
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
            int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            int maxSearchRow = Math.Min(15, lastRowUsed.RowNumber());
            int bestHeaderRowIndex = 1;
            int bestMatchCount = 0;
            var bestHeaderMap = new Dictionary<int, ExcelColumnDefinition>();

            // Quét tìm dòng tiêu đề phù hợp nhất từ dòng 1 tới dòng 15 (tương thích cả có/không Report Title Banner)
            for (int r = 1; r <= maxSearchRow; r++)
            {
                var row = ws.Row(r);
                var currentMap = new Dictionary<int, ExcelColumnDefinition>();
                for (int colIdx = 1; colIdx <= lastCol; colIdx++)
                {
                    var headerText = row.Cell(colIdx).GetString().Trim();
                    if (string.IsNullOrEmpty(headerText)) continue;

                    var matchingCol = columns.FirstOrDefault(c =>
                        string.Equals(c.ColumnName.Trim(), headerText, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.Property.Name, headerText, StringComparison.OrdinalIgnoreCase));

                    if (matchingCol != null && !currentMap.ContainsKey(colIdx))
                    {
                        currentMap[colIdx] = matchingCol;
                    }
                }

                if (currentMap.Count > bestMatchCount)
                {
                    bestMatchCount = currentMap.Count;
                    bestHeaderRowIndex = r;
                    bestHeaderMap = currentMap;
                }
            }

            var headerMap = bestHeaderMap;
            int endRow = lastRowUsed.RowNumber();
            int startRow = bestHeaderRowIndex + 1;
            result.TotalRows = Math.Max(0, endRow - bestHeaderRowIndex);

            for (int rowNumber = startRow; rowNumber <= endRow; rowNumber++)
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
                var colName = col.ColumnName.ToLowerInvariant();
                var propName = col.Property.Name.ToLowerInvariant();

                if (colName.Contains("kết quả") || propName.Contains("success"))
                {
                    cell.Value = b ? "Thành công" : "Thất bại";
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = b ? XLColor.FromHtml("#137333") : XLColor.FromHtml("#C5221F");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    return;
                }

                if (colName.Contains("bãi") || colName.Contains("parking"))
                {
                    cell.Value = b ? "Đang trong bãi" : "Không";
                    cell.Style.Font.Bold = b;
                    cell.Style.Font.FontColor = b ? XLColor.FromHtml("#137333") : XLColor.FromHtml("#5F6368");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    return;
                }

                // Cột trạng thái Active / Hoạt động
                cell.Value = b ? "Đang hoạt động" : "Không hoạt động";
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = b ? XLColor.FromHtml("#137333") : XLColor.FromHtml("#C5221F");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                return;
            }

            if (val is ParkingSessionStatus status)
            {
                switch (status)
                {
                    case ParkingSessionStatus.Active:
                        cell.Value = "Đang hoạt động";
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.FromHtml("#137333");
                        break;
                    case ParkingSessionStatus.Completed:
                        cell.Value = "Đã hoàn thành";
                        cell.Style.Font.FontColor = XLColor.FromHtml("#3C4043");
                        break;
                    case ParkingSessionStatus.Cancelled:
                        cell.Value = "Đã hủy";
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.FromHtml("#C5221F");
                        break;
                    default:
                        cell.Value = status.ToString();
                        break;
                }
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                return;
            }

            if (val is VehicleType vt)
            {
                cell.Value = vt switch
                {
                    VehicleType.Car => "Ô tô",
                    VehicleType.Motorbike => "Xe máy",
                    VehicleType.Bicycle => "Xe đạp",
                    _ => "Khác"
                };
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
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

                if (targetEnum == typeof(VehicleType))
                {
                    var cleanVt = str.Trim().ToLowerInvariant();
                    if (cleanVt is "ô tô" or "oto" or "car") return VehicleType.Car;
                    if (cleanVt is "xe máy" or "xemay" or "xe may" or "motorbike") return VehicleType.Motorbike;
                    if (cleanVt is "xe đạp" or "xedap" or "xe dap" or "bicycle") return VehicleType.Bicycle;
                    if (cleanVt is "khác" or "khac" or "other") return VehicleType.Other;
                }

                if (targetEnum == typeof(ParkingSessionStatus))
                {
                    var cleanStatus = str.Trim().ToLowerInvariant();
                    if (cleanStatus is "đang hoạt động" or "hoạt động" or "active") return ParkingSessionStatus.Active;
                    if (cleanStatus is "đã hoàn thành" or "hoàn thành" or "completed") return ParkingSessionStatus.Completed;
                    if (cleanStatus is "đã hủy" or "hủy" or "cancelled") return ParkingSessionStatus.Cancelled;
                }

                if (Enum.TryParse(targetEnum, str, true, out var parsedEnum))
                {
                    return parsedEnum;
                }
                throw new FormatException($"Giá trị '{str}' không khớp với danh sách lựa chọn của {targetEnum.Name}.");
            }

            if (underlyingType == typeof(DateTime))
            {
                var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "yyyy/MM/dd", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss" };
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
                var clean = str.Trim().ToLowerInvariant();
                if (clean is "có" or "true" or "1" or "yes" or "hoạt động" or "active" or "đang hoạt động" or "thành công" or "đang trong bãi") return true;
                if (clean is "không" or "false" or "0" or "no" or "tạm khóa" or "inactive" or "không hoạt động" or "thất bại") return false;
                return bool.Parse(str);
            }

            return Convert.ChangeType(str, underlyingType, CultureInfo.InvariantCulture);
        }
    }
}
