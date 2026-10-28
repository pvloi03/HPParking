# 0016: Chiến Lược Tài Liệu Hóa API Chuyên Nghiệp Qua Swagger UI, Quản Lý Đa Phiên Bản Bằng Asp.Versioning Và Xuất Bản Postman Collection

Quyết định chuẩn hóa chiến lược tài liệu hóa API cho `HPParking.Api`: tích hợp Swagger UI (Swashbuckle) với cơ chế ủy quyền kép (JWT Bearer & X-API-KEY), quản lý đa phiên bản API chuyên sâu bằng bộ thư viện chính thức `Asp.Versioning.Mvc` và `Asp.Versioning.Mvc.ApiExplorer` (chuẩn route `/api/v{version:apiVersion}/[controller]`), trích xuất tài liệu XML Comments chi tiết kèm ví dụ dữ liệu mẫu (Example Payloads), điều khiển bật/tắt Swagger linh hoạt qua cấu hình `SwaggerSettings:Enabled`, và xuất bản đồng bộ tệp OpenAPI Specification cùng bộ Postman Collection hoàn chỉnh phục vụ đối tác tích hợp bên thứ ba.

## Ngữ cảnh

Một hệ thống REST API phục vụ cho cả quản trị nội bộ (Web Admin / Mobile App) và tích hợp bên thứ ba (HRM, ERP, hệ sinh thái cư dân) đòi hỏi tài liệu giao tiếp phải đáp ứng các tiêu chuẩn khắt khe:
1. **Khả năng tương tác & kiểm thử trực quan:**
   - Kỹ sư frontend và đối tác tích hợp cần một giao diện web trực quan để đọc đặc tả, xem mô tả từng trường dữ liệu và thực hiện gửi request thử nghiệm ("Try it out") ngay trên trình duyệt mà không cần cài thêm phần mềm ngoài.
   - Giao diện tài liệu phải hỗ trợ xác thực cả hai cơ chế bảo mật đã chọn ở ADR 0015: JWT Bearer Token và HTTP Header `X-API-KEY`.
2. **Quản lý phiên bản API (API Versioning):**
   - Các hệ thống bãi xe phát triển dài hạn thường xuyên có sự nâng cấp cấu trúc dữ liệu giữa các phiên bản. Nếu không có cơ chế versioning bài bản ngay từ đầu, việc thay đổi model sẽ gây phá vỡ tính tương thích ngược (breaking changes) cho các đối tác đã tích hợp v1.
   - Cần một giải pháp quản trị phiên bản chuyên sâu, chuẩn mực của hệ sinh thái .NET: Sử dụng bộ thư viện `Asp.Versioning.Mvc` kết hợp `Asp.Versioning.Mvc.ApiExplorer` để tạo các document độc lập (v1, v2...) và hiển thị bộ chọn phiên bản trên Swagger UI.
3. **Mức độ chi tiết của tài liệu hóa:**
   - Tài liệu chỉ hiển thị tên thuộc tính mà thiếu giải thích ý nghĩa, định dạng và dữ liệu mẫu sẽ gây khó khăn, nhầm lẫn cho bên tích hợp.
   - Cần kích hoạt xuất XML Comments từ mã nguồn C# và định nghĩa các Schema Filter / Examples cho toàn bộ DTOs và responses.
4. **Môi trường hiển thị:**
   - Thay vì chỉ hardcode bật trong `Development`, cần cho phép cấu hình bật/tắt qua `SwaggerSettings:Enabled` trong `appsettings.json` để có thể mở Swagger trên môi trường máy chủ On-Premise của khách hàng khi đội ngũ triển khai kỹ thuật cần nghiệm thu và tích hợp.
5. **Chia sẻ và bàn giao tài liệu ngoại tuyến:**
   - Đối tác tích hợp doanh nghiệp thường yêu cầu bàn giao tài liệu dưới dạng OpenAPI JSON và bộ sưu tập Postman Collection kèm file môi trường (Environment) mẫu để import trực tiếp vào công cụ làm việc của họ.

## Quyết định

1. **Giao diện Tài liệu Swagger UI & Cấu hình Ủy quyền Kép:**
   - Sử dụng `Swashbuckle.AspNetCore`.
   - Cấu hình SwaggerGen với 2 Security Definitions:
     * `Bearer`: HTTP Authorization header sử dụng Bearer scheme.
     * `ApiKey`: Header `X-API-KEY`.
   - Swagger hiển thị hộp thoại "Authorize" cho phép kiểm thử API với cả 2 hình thức xác thực.

2. **Quản lý Đa Phiên Bản Bằng `Asp.Versioning`:**
   - Cài đặt các gói NuGet: `Asp.Versioning.Mvc` và `Asp.Versioning.Mvc.ApiExplorer`.
   - Định dạng phiên bản chuẩn URL: `/api/v{version:apiVersion}/[controller]` (ví dụ: `/api/v1/clients`, `/api/v1/auth/login`).
   - Cấu hình `DefaultApiVersion = new ApiVersion(1, 0)`, `AssumeDefaultVersionWhenUnspecified = true`.
   - Swagger tự động tạo tài liệu theo từng phiên bản (`/swagger/v1/swagger.json`), có menu dropdown chuyển đổi giữa các phiên bản trên thanh công cụ.

3. **Tài Liệu Hóa XML Comments & Example Payloads:**
   - Bật `<GenerateDocumentationFile>true</GenerateDocumentationFile>` và `<NoWarn>$(NoWarn);1591</NoWarn>` trong `.csproj`.
   - Nạp tệp XML documentation vào `SwaggerGenOptions.IncludeXmlComments()`.
   - Khai báo đầy đủ các thuộc tính `[ProducesResponseType]`, thẻ `<summary>`, `<remarks>`, `<param>` trên Controllers và thẻ `<example>` trên DTOs.

4. **Điều Khiển Bật/Tắt Swagger Linh Hoạt:**
   - Thêm phần cấu hình `SwaggerSettings` trong `appsettings.json`:
     ```json
     "SwaggerSettings": {
       "Enabled": true,
       "RoutePrefix": "swagger",
       "Title": "HPParking Management & Integration API",
       "Description": "REST API phục vụ Web Admin, Mobile App và tích hợp hệ thống bên thứ ba cho hệ thống kiểm soát xe thông minh HPParking."
     }
     ```
   - Kích hoạt Swagger UI khi `app.Environment.IsDevelopment() || configuration.GetValue<bool>("SwaggerSettings:Enabled")`.

5. **Xuất Bản Bộ Tài Liệu Bàn Giao (Export Artifacts):**
   - Duy trì endpoint động: `GET /swagger/v1/swagger.json`.
   - Xuất bản tệp tĩnh OpenAPI Specification tại `docs/api/openapi/hpparking-api-v1.json`.
   - Xuất bản bộ Postman Collection hoàn chỉnh kèm biến môi trường tại:
     * `docs/api/postman/HPParking_API_v1.postman_collection.json`
     * `docs/api/postman/HPParking_Local.postman_environment.json`

## Hệ quả

- **Tính chuyên nghiệp & Trải nghiệm tích hợp xuất sắc:** Đối tác bên thứ ba có thể dễ dàng hiểu rõ cấu trúc dữ liệu, chạy thử API ngay trên Swagger hoặc import bộ Postman Collection chỉ trong 1 phút.
- **Sẵn sàng mở rộng lâu dài:** Cơ chế `Asp.Versioning` đảm bảo việc nâng cấp API lên phiên bản v2 trong tương lai sẽ diễn ra mượt mà, không phá vỡ bất kỳ client nào đang sử dụng v1.
- **Kiểm soát linh hoạt:** Quản trị viên hệ thống có thể bật hoặc tắt giao diện tài liệu Swagger trên các máy chủ thực tế chỉ bằng một cờ cấu hình trong `appsettings.json` hoặc biến môi trường `SwaggerSettings__Enabled`.
