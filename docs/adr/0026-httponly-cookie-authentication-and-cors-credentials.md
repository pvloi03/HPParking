# 0026: Xác Thực HttpOnly Cookie Cho Web Admin, CORS Credentials Và Endpoint Đăng Xuất

Quyết định thiết kế về việc hỗ trợ xác thực bằng Cookie an toàn (HttpOnly Cookie) cho phân hệ quản trị Web Admin trong `HPParking.Api`: áp dụng cơ chế phát hành token song song (Dual Mode: vừa trả về JSON payload vừa đính kèm Cookie `hpparking_access_token`), tự động trích xuất token từ Cookie trong chuỗi pipeline JWT Bearer khi header `Authorization` không hiện diện, tích hợp endpoint đăng xuất `POST /api/v1/auth/logout` xóa Cookie và ghi nhận AuditLog, đồng thời cấu hình CORS cho phép truyền thông tin xác thực (`AllowCredentials`) dựa trên danh sách nguồn tin cậy (`CorsSettings:AllowedOrigins`) trong `appsettings.json`.

## Ngữ cảnh

Trong kiến trúc ban đầu (ADR 0015), `HPParking.Api` chủ yếu thiết kế cơ chế xác thực JWT thông qua HTTP Header `Authorization: Bearer <token>` và API Key qua Header `X-API-KEY`.

Tuy nhiên, đối với ứng dụng Web Admin tương tác trên trình duyệt:
1. **Rủi ro rò rỉ Token qua XSS**: Nếu lưu trữ Access Token trong `localStorage` hoặc `sessionStorage`, mã độc JavaScript (tấn công XSS) có thể đọc và đánh cắp token. Sử dụng `HttpOnly Cookie` là tiêu chuẩn bảo mật cao nhất hiện nay vì ngăn cản hoàn toàn quyền truy cập của JavaScript.
2. **Trải nghiệm lập trình Client**: Khi sử dụng Cookie, trình duyệt tự động đính kèm thông tin xác thực trong mọi request mà không yêu cầu Frontend Developer phải viết mã Interceptor thủ công.
3. **Tính tương thích đa nền tảng**: Hệ thống vẫn cần phục vụ song song các client phi-web (WinForms Desktop, Mobile App, Swagger UI, Postman) vốn quen thuộc với việc truyền JWT qua Header `Authorization: Bearer`.
4. **Yêu cầu CORS khắt khe**: Chuẩn W3C Fetch/XHR cấm sử dụng `Access-Control-Allow-Origin: *` khi bật cờ `withCredentials: true`. Do đó, CORS bắt buộc phải xác định danh sách Origin tin cậy thông qua file cấu hình.

## Quyết định

1. **Cơ chế phát hành Token song song (Dual Mode)**:
   - Tại endpoint `POST /api/v1/auth/login`, khi xác thực thông tin đăng nhập thành công:
     - Tiếp tục trả về dữ liệu `ApiResponse<LoginResponse>` chứa `accessToken` và `user` trong JSON body (phục vụ Swagger, Mobile, Desktop, Postman).
     - Đồng thời ghi nhận Cookie an toàn qua `Response.Cookies.Append`:
       * **Tên Cookie**: `hpparking_access_token`.
       * **Cờ HttpOnly**: `true` (ngăn chặn JavaScript đọc token, triệt tiêu nguy cơ XSS).
       * **Cờ Secure**: Thiết lập động theo giao thức request `Request.IsHttps` (`false` khi chạy Localhost HTTP, `true` khi chạy trên môi trường HTTPS/Production).
       * **Cờ SameSite**: `SameSiteMode.Lax` (đảm bảo truyền tải Cookie mượt mà giữa các cổng cục bộ như Web Admin port 3000/5173 và API port 5050).
       * **Thời hạn sống**: Khớp với thời hạn token theo ca làm việc `_jwtOptions.Value.ExpiryMinutes` (mặc định 480 phút / 8 tiếng).
       * **Path**: `/`.

2. **Cơ chế trích xuất Token tự động trong Pipeline (JWT Bearer Event)**:
   - Trong `Program.cs`, cấu hình sự kiện `JwtBearerEvents.OnMessageReceived`:
     - Nếu `context.Token` chưa được gán từ header `Authorization: Bearer <token>`:
     - Kiểm tra và trích xuất token từ `context.Request.Cookies["hpparking_access_token"]`.
     - Nhờ đó, pipeline xác thực chấp nhận linh hoạt cả 3 phương thức: Header `X-API-KEY`, Header `Authorization: Bearer`, hoặc Cookie `hpparking_access_token`.

3. **Endpoint Đăng Xuất An Toàn (`POST /api/v1/auth/logout`)**:
   - Yêu cầu xác thực `[Authorize]`.
   - Tiến hành xóa Cookie trên trình duyệt thông qua `Response.Cookies.Delete("hpparking_access_token")` với cùng thuộc tính bảo mật (`HttpOnly`, `SameSite = Lax`, `Secure = Request.IsHttps`).
   - Tự động ghi nhận sự kiện Đăng xuất vào collection `AuditLogs` trong MongoDB thông qua `AuditLog.CreateAuthLog` (`ActionType = AuditActionType.Logout`, `IsSuccess = true`).
   - Trả về phản hồi chuẩn: `ApiResponse<string>` với thông điệp: `"Đăng xuất thành công."`.

4. **Cấu hình CORS Origin từ File Config (`CorsSettings`)**:
   - Khai báo cấu hình trong `appsettings.json`:
     ```json
     "CorsSettings": {
       "AllowedOrigins": [
         "http://localhost:3000",
         "http://localhost:5173"
       ]
     }
     ```
   - Trong `Program.cs`, CORS Policy đọc danh sách `AllowedOrigins` từ cấu hình, áp dụng `.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()`. Nếu cấu hình để trống hoặc không tồn tại, fallback sang chế độ an toàn cho localhost.

5. **Chiến Lược Triển Khai Tên Miền (Domain Deployment Strategy)**:
   - **Môi trường Cùng Hệ Sinh Thái (Same Parent Domain / Subdomain)**:
     - *Ví dụ*: Web Admin chạy tại `https://admin.hpparking.vn` và API chạy tại `https://api.hpparking.vn`.
     - *Khuyến nghị*: **Sử dụng HttpOnly Cookie là chuẩn an toàn nhất**. Trình duyệt coi đây là Same-Site (chung domain gốc `hpparking.vn`), cookie được tự động lưu trữ và gửi kèm mà không bị chặn, triệt tiêu 100% rủi ro tấn công XSS.
   - **Môi trường Khác Hoàn Toàn Tên Miền (Cross-Site / Different Top-Level Domains)**:
     - *Ví dụ*: Web Admin chạy tại `https://quanlybaixe.com` và API chạy tại `https://api-hpparking.vn`.
     - *Thực tế trình duyệt*: Các trình duyệt hiện đại (Safari ITP, Firefox ETP, Chrome) xếp trường hợp này vào nhóm Third-Party Cookie và chặn mặc định.
     - *Khuyến nghị*: **Chuyển sang sử dụng cơ chế truyền qua Header (`Authorization: Bearer <accessToken>` và `X-Refresh-Token: <refreshToken>`)** mà hệ thống `HPParking.Api` đã tích hợp sẵn trong mô hình Dual-Mode. Phương thức Header không phụ thuộc vào chính sách Cookie của trình duyệt, đảm bảo hoạt động ổn định 100% trên mọi tên miền độc lập.

## Hệ quả

- **Tích cực**:
  - Bảo vệ tối đa tài khoản quản trị trên Web Admin trước các cuộc tấn công Cross-Site Scripting (XSS) khi chạy cùng hệ sinh thái.
  - Linh hoạt thích ứng với mọi mô hình triển khai hạ tầng: dễ dàng chuyển đổi giữa Cookie (Same-Site) và Header (Cross-Site) mà không cần viết lại mã nguồn backend.
  - Web Admin không cần lưu trữ token thủ công khi dùng Cookie, chỉ cần bật `withCredentials: true`.
  - Giữ vững 100% tính tương thích ngược với Mobile App, WinForms, Swagger UI và các hệ thống bên thứ ba dùng API Key.
  - Lưu vết kiểm toán đăng xuất đầy đủ phục vụ an ninh thông tin.
- **Đánh đổi**:
  - Khi triển khai môi trường mới, DevOps cần khai báo chính xác địa chỉ Web Admin vào `CorsSettings:AllowedOrigins` để trình duyệt không chặn Cookie khi gọi chéo tên miền.
