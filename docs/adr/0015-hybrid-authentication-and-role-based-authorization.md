# 0015: Kiến Trúc Xác Thực Kép Hybrid JWT/API Key, Phân Quyền Role-Based, Chống Brute-Force Và Ghi Vết AuditLog

Quyết định thiết kế chi tiết phân hệ Xác thực (Authentication) và Phân quyền (Authorization) cho dịch vụ `HPParking.Api`: áp dụng kiến trúc Hybrid Authentication dựa trên chuẩn ASP.NET Core PolicyScheme kết hợp Custom `ApiKeyAuthenticationHandler` và `JwtBearer`, vòng đời Access Token theo ca làm việc (8-12 tiếng) hoàn toàn stateless, ma trận phân quyền 3 cấp (`Admin`, `Manager`, `Viewer`), chống tấn công dò quét mật khẩu qua .NET 8 Rate Limiting, hỗ trợ đổi mật khẩu với BCrypt và tự động ghi vết sự kiện bảo mật vào CSDL qua thực thể `AuditLog`.

## Ngữ cảnh

Hệ thống `HPParking.Api` cần phục vụ hai nhóm đối tượng tiêu thụ khác nhau:
1. **Người dùng tương tác qua giao diện (Web Admin / Mobile App):** Cần cơ chế đăng nhập bằng tài khoản/mật khẩu, nhận diện danh tính và phân quyền vai trò (`Admin`, `Manager`, `Viewer`).
2. **Hệ thống phần mềm bên thứ ba (ERP, HRM, hệ thống cư dân):** Cần cơ chế máy chủ giao tiếp máy chủ (Server-to-Server) bằng API Key tĩnh truyền qua HTTP Header `X-API-KEY`.

Các bài toán kỹ thuật cần giải quyết:
- **Xử lý Hybrid Authentication mượt mà trong ASP.NET Core:**
  - Nếu dùng Middleware độc lập sẽ can thiệp thô bạo vào `HttpContext.User`, gây khó khăn cho Swagger UI và phá vỡ cơ chế kiểm thử tích hợp.
  - Cần một giải pháp chính thống (idiomatic): Đăng ký `AuthenticationHandler<ApiKeyAuthenticationOptions>` kết hợp `PolicyScheme` ("JWT_OR_APIKEY"). Pipeline sẽ tự động chuyển tiếp sang Scheme `ApiKey` nếu request có Header `X-API-KEY`, ngược lại chuyển tiếp sang `JwtBearer`. Nhờ đó, lập trình viên chỉ cần sử dụng thuộc tính `[Authorize(Roles = "...")]` tự nhiên trên mọi Controller/Action.
- **Chiến lược Token Lifecycle:**
  - Cân nhắc giữa Access Token hạn ngắn kèm Refresh Token và Access Token theo ca làm việc (8-12 tiếng).
  - Đối với nghiệp vụ bãi xe, nhân viên vận hành và quản lý làm việc theo ca cố định. Việc duy trì Refresh Token đòi hỏi tạo thêm bảng quản lý token trong CSDL và cơ chế thu hồi phức tạp. Lựa chọn Access Token có thời hạn tương đương một ca làm việc (8-12 tiếng) giúp API giữ được tính chất Stateless hoàn toàn, đơn giản hóa tối đa kiến trúc mà vẫn đảm bảo an toàn.
- **Ma trận Phân quyền & Giới hạn quyền Xóa:**
  - Hành động Xóa khách hàng và phương tiện là thao tác nhạy cảm, ảnh hưởng trực tiếp đến dữ liệu lịch sử và quyền ra vào bãi xe. Quyền Xóa (`DELETE`) được giới hạn duy nhất cho vai trò `Admin`. Vai trò `Manager` được cấp quyền Thêm, Sửa, Upload ảnh và kích hoạt đồng bộ FaceID. Vai trò `Viewer` chỉ có quyền xem dữ liệu.
- **An toàn Tài khoản & Chống Brute-Force:**
  - Cần kiểm tra trạng thái kích hoạt `user.IsActive == true` trước khi cấp phép đăng nhập.
  - Cung cấp endpoint đổi mật khẩu `POST /api/auth/change-password` để quản trị viên đổi mật khẩu ban đầu sau khi seed.
  - Sử dụng .NET 8 Rate Limiting (`FixedWindowRateLimiter`: tối đa 5 lần thử/phút/IP trên endpoint login) để ngăn chặn tấn công dò mật khẩu tự động.
- **Lưu vết Kiểm toán An ninh (Audit Logging):**
  - Tận dụng thực thể `AuditLog` có sẵn trong `HPParking.Core` để ghi nhận các sự kiện đăng nhập (thành công/thất bại) và đổi mật khẩu vào MongoDB.

## Quyết định

1. **Cấu hình Hybrid Authentication PolicyScheme:**
   - Đăng ký Default Scheme `"JWT_OR_APIKEY"`.
   - `ForwardDefaultSelector`: Nếu request chứa header `X-API-KEY`, chọn Scheme `"ApiKey"`; ngược lại chọn `JwtBearerDefaults.AuthenticationScheme`.
   - Cấu hình Swagger UI hỗ trợ đồng thời hai Security Definitions (`Bearer` và `ApiKey`), hiển thị 2 nút ủy quyền trực quan.

2. **Cấu trúc Claims trong JWT:**
   - `sub`: `User.Id` (ObjectId)
   - `name`: `User.Username`
   - `role`: `User.Role.ToString()` (`Admin`, `Manager`, `Viewer`)
   - `fullName`: `User.FullName`
   - `email`: `User.Email`

3. **Ma trận Phân quyền (Authorization Matrix):**
   | Endpoint | Thao tác | Quyền hạn |
   |---|---|---|
   | `/api/auth/login` | Đăng nhập | Public (Rate limited) |
   | `/api/auth/me` | Lấy thông tin cá nhân | Authenticated |
   | `/api/auth/change-password` | Đổi mật khẩu | Authenticated |
   | `GET /api/clients/**` | Xem danh sách/chi tiết | Viewer, Manager, Admin, APIKey |
   | `POST/PUT /api/clients/**` | Thêm, sửa, nạp FaceID | Manager, Admin, APIKey |
   | `DELETE /api/clients/**` | Xóa mềm khách hàng | Admin, APIKey |
   | `PUT/DELETE /api/vehicles/**` | Cập nhật/Xóa xe | Manager (sửa), Admin (xóa), APIKey |

4. **Quản lý API Key:**
   - Lưu trữ danh sách khóa hợp lệ trong cấu hình `ApiKeySettings:ApiKeys` (hỗ trợ ghi đè qua biến môi trường `ApiKeySettings__ApiKeys__0`).
   - Khi request gửi header `X-API-KEY` hợp lệ, hệ thống tạo `ClaimsPrincipal` định danh với vai trò `Admin`.

5. **Bảo vệ Endpoint Đăng nhập & Audit Trail:**
   - Kích hoạt .NET 8 `RateLimiter` trên `POST /api/auth/login` với chính sách Fixed Window (5 requests/phút/IP).
   - Khi đăng nhập thành công: Cập nhật `user.LastLoginAt = DateTime.UtcNow` và ghi `AuditLog` với `ActionType = AuditActionType.Login`, `IsSuccess = true`.
   - Khi đăng nhập thất bại: Ghi `AuditLog` với `ActionType = AuditActionType.Login`, `IsSuccess = false`, `ErrorMessage = "Sai thông tin đăng nhập hoặc tài khoản bị khóa"`.

## Hệ quả

- **Trải nghiệm lập trình đồng nhất:** Không cần viết các custom filter phức tạp, toàn bộ Controller sử dụng chuẩn ASP.NET Core `[Authorize]`.
- **Bảo mật phân tầng vững chắc:** Ngăn chặn tấn công brute-force từ mạng ngoài, bảo vệ endpoint nhạy cảm DELETE chỉ cho Admin, kiểm soát chặt chẽ trạng thái kích hoạt tài khoản.
- **Kiểm toán an ninh minh bạch:** Mọi hoạt động truy cập và thay đổi mật khẩu đều được lưu vết đầy đủ trong MongoDB phục vụ đối soát và tuân thủ an toàn thông tin.
