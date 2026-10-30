# 0023: Kiến Trúc Generic Excel Engine Dựa Trên Cấu Hình Fluent, ClosedXML, Nhập Liệu Từng Phần Và Xuất Dữ Liệu An Toàn

Quyết định thiết kế và chuẩn hóa giải pháp Nhập (Import) và Xuất (Export) dữ liệu qua file Excel (.xlsx) cho `HPParking.Api`: áp dụng kiến trúc Core Generic Engine kết hợp cấu hình Fluent Profile (`ExcelProfile<T>`) dùng chung cho toàn bộ thực thể trong hệ thống, sử dụng thư viện ClosedXML (MIT License) trên nền tảng .NET 8 AnyCPU, hỗ trợ chiến lược nhập liệu thành công từng phần (Partial Success) kèm chế độ chạy thử nghiệm (dry-run preview), cơ chế giải quyết xung đột dữ liệu linh hoạt (`duplicateAction`), xuất file trực tiếp (HTTP File Streaming) theo bộ lọc với giới hạn an toàn 10,000 dòng/file, tự động sinh file template mẫu động từ schema profile, và tách biệt hoàn toàn luồng nhập liệu khỏi phần cứng FaceID để tối ưu tốc độ phản hồi và độ ổn định hệ thống.

## Ngữ cảnh

Trong các nghiệp vụ quản lý bãi đỗ xe thông minh, hoạt động nhập danh sách hàng loạt người dùng/nhân viên và xuất báo cáo dữ liệu định kỳ là nhu cầu thiết yếu:
1. **Tránh lặp lại mã nguồn (Code Duplication):**
   - Nếu mỗi thực thể (`Client`, `Vehicle`, sau này là `ParkingSession`, `AuditLog`, `Department`...) tự viết code đọc/ghi file Excel riêng lẻ, mã nguồn sẽ bị phân mảnh, trùng lặp hàng trăm dòng code xử lý OpenXML, parse kiểu dữ liệu, bắt lỗi và tạo định dạng bảng tính.
   - Do đó, cần xây dựng một **Generic Excel Engine** dùng chung, tách biệt hoàn toàn logic kỹ thuật đọc/ghi Excel ra khỏi mô hình dữ liệu nghiệp vụ.
2. **Lựa chọn thư viện xử lý Excel trên .NET 8 AnyCPU:**
   - Cân nhắc giữa:
     * *EPPlus*: Bản 5+ yêu cầu giấy phép thương mại (Polyform Noncommercial), có rủi ro pháp lý cho triển khai thương mại.
     * *MiniExcel*: Siêu nhẹ và nhanh nhưng hạn chế về tính năng định dạng bảng tính, màu sắc, dropdown validation và tooltip chú thích.
     * *ClosedXML (MIT License)*: Hoàn toàn miễn phí bản quyền thương mại, hỗ trợ đầy đủ các tính năng nâng cao: định dạng header chuyên nghiệp, autofit cột, dropdown data validation (cho các trường Enum), định dạng ngày tháng/số, và tạo cell comments trực quan.
3. **Chiến lược cấu hình dữ liệu (Configuration Pattern):**
   - Thay vì dùng Attribute làm biến dạng các DTO nghiệp vụ, sử dụng **Fluent Configuration** (`ExcelProfile<T>`) giúp phân tách triệt để trách nhiệm (Separation of Concerns), hỗ trợ định nghĩa thứ tự cột, tên hiển thị tiếng Việt, quy tắc bắt buộc, format hiển thị, và parser/converter tùy biến.
4. **Chiến lược xử lý lỗi khi nhập liệu hàng loạt:**
   - Trong tệp danh sách hàng trăm dòng, nếu chỉ 1 dòng vi phạm mà hủy toàn bộ (All-or-Nothing) sẽ gây ức chế và giảm hiệu suất vận hành của người dùng. Áp dụng cơ chế **Partial Success**: lưu thành công các dòng hợp lệ, đồng thời thu thập danh sách chi tiết các dòng lỗi kèm tọa độ ô (Row, Column, Value, ErrorMessage).
   - Hỗ trợ tham số `?dryRun=true` để kiểm tra tính hợp lệ của toàn bộ tệp mà không ghi dữ liệu vào database.
5. **Xử lý trùng lặp dữ liệu (Duplicate Resolution):**
   - Cung cấp tham số `duplicateAction` với 3 chế độ: `Skip` (bỏ qua bản ghi trùng - mặc định), `Update` (cập nhật thông tin mới vào bản ghi hiện có), hoặc `Error` (ghi nhận như một lỗi dòng).
6. **Tách biệt phần cứng FaceID:**
   - Dữ liệu khuôn mặt sinh trắc học và việc nạp vào thiết bị phần cứng qua camera Hikvision ISAPI là tác vụ tốn thời gian và phụ thuộc trạng thái mạng LAN. Việc nhập Excel tập trung 100% vào nghiệp vụ cơ sở dữ liệu để phản hồi nhanh chóng (< 1s cho 1,000 dòng). Việc gán ảnh và nạp FaceID được thực hiện qua các endpoint chuyên biệt riêng.
7. **Kiểm soát tài nguyên khi Xuất file (Export Safeguards):**
   - Xuất file trực tiếp qua HTTP File Stream theo đúng bộ lọc tìm kiếm hiện tại, có giới hạn kẹp trần an toàn tối đa 10,000 dòng/file để bảo vệ RAM server khỏi nguy cơ OutOfMemory.

## Quyết định

1. **Kiến Trúc Core Generic Excel Engine:**
   - Định nghĩa giao diện trung tâm `IExcelService`:
     ```csharp
     public interface IExcelService
     {
         Task<ExcelImportResult<T>> ReadAsync<T>(Stream stream, ExcelImportOptions options, CancellationToken ct = default) where T : class, new();
         Task<byte[]> WriteAsync<T>(IEnumerable<T> data, string sheetName = "Data", CancellationToken ct = default) where T : class;
         Task<byte[]> WriteTwoSheetsAsync<T1, T2>(ExcelSheetData<T1> sheet1, ExcelSheetData<T2> sheet2, CancellationToken ct = default) where T1 : class where T2 : class;
         Task<byte[]> GenerateTemplateAsync<T>(string sheetName = "Template", CancellationToken ct = default) where T : class;
     }
     ```
   - Thư viện nền tảng: **ClosedXML** (Package: `ClosedXML` v0.102.x+ trên .NET 8.0 AnyCPU).

2. **Cơ Chế Cấu Hình Fluent (`ExcelProfile<T>`):**
   - Mỗi DTO dữ liệu Excel có một Profile cấu hình kế thừa từ `ExcelProfile<T>`:
     ```csharp
     public class ClientExcelProfile : ExcelProfile<ClientExcelDto>
     {
         public ClientExcelProfile()
         {
             Map(x => x.Code).ColumnName("Mã khách hàng").Order(1).Required().WithComment("Mã nhân viên hoặc CCCD");
             Map(x => x.Name).ColumnName("Họ và tên").Order(2).Required();
             Map(x => x.PhoneNumber).ColumnName("Số điện thoại").Order(3).Required();
             Map(x => x.BirthDay).ColumnName("Ngày sinh").Order(4).Format("dd/MM/yyyy");
             Map(x => x.Address).ColumnName("Địa chỉ").Order(5);
             Map(x => x.PlateNumber).ColumnName("Biển số xe").Order(6);
             Map(x => x.VehicleType).ColumnName("Loại xe").Order(7).EnumDropdown<VehicleType>();
         }
     }
     ```

3. **Chiến Lược Nhập Liệu Thành Công Từng Phần (Partial Success & Dry-Run):**
   - Endpoint: `POST /api/v1/{entity}/import?dryRun={bool}&duplicateAction={Skip|Update|Error}`.
   - Kết quả phản hồi bọc trong `ApiResponse<ExcelImportResultDto>`:
     ```json
     {
       "success": true,
       "data": {
         "totalRows": 150,
         "successCount": 147,
         "failedCount": 3,
         "skippedCount": 0,
         "isDryRun": false,
         "errors": [
           { "row": 12, "column": "Số điện thoại", "value": "123", "errorMessage": "Số điện thoại không hợp lệ" },
           { "row": 45, "column": "Biển số xe", "value": "30A-99999", "errorMessage": "Biển số xe đã được gán cho khách hàng khác" }
         ]
       },
       "message": "Xử lý tệp hoàn tất: 147 thành công, 3 thất bại",
       "errors": []
     }
     ```

4. **Sinh File Template Tự Động Từ Schema:**
   - Endpoint: `GET /api/v1/{entity}/import-template`.
   - Core Engine tự động sinh tệp `.xlsx` có:
     * Header in đậm, nền màu thương hiệu nhẹ (#E8F0FE), căn giữa.
     * Tự động bật Data Validation (Dropdown List) cho các cột ánh xạ từ Enum (VD: Loại xe: "Car", "Motorbike", "Bicycle").
     * Tự động bổ sung Cell Comment/Tooltip hướng dẫn định dạng trên từng tiêu đề cột.
     * Chèn sẵn 1 dòng dữ liệu mẫu (Sample Row) để người dùng dễ hình dung.
     * Tự động Auto-fit độ rộng cột (Column Width).

5. **Chiến Lược Xuất Dữ Liệu An Toàn (Safe Export):**
   - Endpoint: `GET /api/v1/{entity}/export?...bộ lọc tìm kiếm...`.
   - Phản hồi: Stream tệp nhị phân MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.
   - Tên tệp xuất: `{entity}_{yyyyMMdd_HHmmss}.xlsx` (VD: `clients_20260917_143000.xlsx`), timestamp là `DateTime.UtcNow` (compact `yyyyMMdd_HHmmss`).
   - **Ngoại lệ — báo cáo theo phạm vi thời gian (Date-Ranged Reports):** các báo cáo có tham số khoảng ngày (VD `Bao_Cao_Doanh_Thu`) đặt tên theo `{entity}_{fromDate}_{toDate}.xlsx` (compact `yyyyMMdd`, VD `Bao_Cao_Doanh_Thu_20261001_20261031.xlsx`) thay vì timestamp, vì khoảng ngày định danh đúng phạm vi dữ liệu và tệp chỉ được stream tải về (không lưu phía server nên không cần chống trùng tên). Tên tệp do backend sinh (ghi vào audit log) và do frontend dùng cho `downloadBlob` phải **giống hệt** để tránh lệch giữa tên người dùng thấy và tên trong nhật ký.
   - Giới hạn kẹp trần tối đa: **10,000 bản ghi/lần xuất**. Nếu truy vấn có `totalCount > 10000`, hệ thống ưu tiên xuất 10,000 bản ghi đầu theo bộ lọc và thêm cảnh báo vào header `X-Export-Truncated: true`.

6. **Phân Quyền & Bảo Mật File Upload:**
   - Phân quyền:
     * `Import`: Yêu cầu quyền tối thiểu `Manager` hoặc `Admin`.
     * `Export`: Cho phép `Viewer`, `Manager`, và `Admin` (riêng `AuditLogs` yêu cầu nghiêm ngặt vai trò `Admin`).
     * `Template`: Cho phép mọi người dùng đã xác thực (`Viewer`, `Manager`, `Admin`).
   - Bảo mật tệp upload:
     * Giới hạn kích thước tệp tối đa: **10 MB**.
     * Kiểm tra phần mở rộng: Duy nhất `.xlsx`.
     * Kiểm tra Content-Type: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.
     * Kiểm tra Magic Bytes đầu tệp: Ký tự Zip `PK` (`0x50, 0x4B, 0x03, 0x04`) để ngăn chặn tệp mã độc mạo danh phần mở rộng.

7. **Quy Hoạch Danh Mục & Phân Hệ Nhập/Xuất Toàn Diện (Comprehensive Scope Mapping):**
   - **Nhóm 1: Hồ sơ Đối tượng Kiểm soát (Core Domain - Template, Import & Export)**:
     * `Clients & Vehicles`: Quản lý hồ sơ nhân sự, số điện thoại, CCCD, ngày sinh, địa chỉ, gán phương tiện (biển số, loại xe) và đơn vị trực thuộc.
   - **Nhóm 2: Danh mục Tổ chức & Hạ tầng (Master Data - Template, Import & Export)**:
     * `Companies`: Quản lý danh mục đơn vị thành viên, mã công ty, địa chỉ, số điện thoại.
     * `Departments`: Quản lý danh mục phòng ban, liên kết mã/tên Công ty.
     * `Contractors`: Quản lý danh mục đối tác/nhà thầu, thời hạn hoạt động.
     * `Gates`: Quản lý danh mục Cổng kiểm soát, liên kết trạm `MachineCode` và Công ty.
     * `Lanes`: Quản lý danh mục Làn xe, Dropdown `LaneType` (Moto In, Moto Out, Car In, Car Out), liên kết Cổng.
     * `Devices`: Quản lý danh mục Thiết bị ngoại vi, Dropdown `DeviceType` (Camera, Controller, FaceId, Other), IP:Port, che giấu mật khẩu.
   - **Nhóm 3: Sổ Cái Giao Dịch & Báo Cáo Giám Sát (Immutable Ledgers & Reports - Read-Only Export)**:
     * `ParkingSessions`: Xuất lịch sử xe vào/ra, làn xe, 4 mốc ảnh bằng chứng và thời lượng đỗ xe động. **Nguyên tắc Sổ cái bất biến: Không cho phép Import.**
     * `AuditLogs`: Xuất nhật ký kiểm toán hệ thống (Chỉ dành cho Admin). **Nguyên tắc Sổ cái bất biến: Không cho phép Import.**
     * `DistributionStatistics`: Xuất ma trận phân bổ Nhân sự & Hạ tầng (Khách hàng, Xe, Cổng, Làn) theo từng Công ty và Phòng ban.
     * `RevenueReport`: Xuất báo cáo Doanh thu đa trang tính (Multi-sheet: Sheet 1 Tổng hợp theo ngày, Sheet 2 Chi tiết giao dịch theo ADR 0043). **Nguyên tắc Sổ cái bất biến: Không cho phép Import.**

## Hệ quả

- **Tích cực:**
  - Tiết kiệm 80% thời gian triển khai Import/Export cho các thực thể hiện tại và tương lai thông qua Generic Engine.
  - Trải nghiệm người dùng vượt trội: file mẫu có sẵn dropdown, chú thích ô, báo cáo lỗi chi tiết từng dòng.
  - Bảo vệ an toàn tài nguyên máy chủ nhờ giới hạn dung lượng file upload (10MB) và giới hạn dòng xuất (10,000).
  - Không vi phạm bản quyền phần mềm nhờ sử dụng ClosedXML (MIT).
  - Độc lập hoàn toàn với WinForms và không làm chậm hệ thống phần cứng camera FaceID.
- **Tiêu cực / Rủi ro:**
  - ClosedXML sử dụng mô hình DOM trong bộ nhớ, do đó cần duy trì ngưỡng kẹp trần 10,000 dòng để tránh tăng đột biến dung lượng RAM trong quá trình render bảng tính.
