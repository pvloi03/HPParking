# 0019: Chiến Lược Ghi Log Serilog Có Cấu Trúc, Cơ Chế Tạo File Theo Yêu Cầu (Lazy On-Demand) Và Bảo Mật Che Giấu Dữ Liệu Nhạy Cảm

Quyết định chuẩn hóa phân hệ ghi log (Logging) cho `HPParking.Api`: tích hợp thư viện Serilog (`Serilog.AspNetCore`) hỗ trợ Structured Logging, phân định rõ ràng giữa Application Log (Serilog File) và Business Audit Log (MongoDB `AuditLog`), áp dụng cơ chế khởi tạo file log theo yêu cầu (Lazy On-Demand: chỉ tạo file `logs/hpparking-api-yyyyMMdd.log` khi có dòng log đầu tiên phát sinh trong ngày, không tạo file 0 byte nếu ngày đó không có sự kiện), cấu hình Console Sink linh hoạt (chỉ bật ở môi trường Development, tắt trên Production để tối ưu hiệu năng CPU), che giấu dữ liệu nhạy cảm (Mật khẩu, API Key) và lưu giữ tự động 30 ngày (giới hạn 50MB/file).

## Ngữ cảnh

Trong một hệ thống dịch vụ REST API kiểm soát bãi xe thông minh hoạt động 24/7, việc giám sát, chẩn đoán sự cố và kiểm toán an ninh đòi hỏi một chiến lược ghi log chuyên nghiệp:
1. **Hạn chế của cách ghi log cũ:**
   - Ứng dụng WinForms hiện tại chỉ sử dụng `Debug.WriteLine`, thông tin bị biến mất khi đóng cửa sổ Output và không có tệp lưu trữ trên đĩa để tra cứu sự cố sau này.
2. **Yêu cầu không sinh file rác 0 byte (Lazy Log Creation):**
   - Rất nhiều thư viện log mặc định tự động tạo trước một file log rỗng lúc 00:00:00 của mỗi ngày. Nếu ngày hôm đó hệ thống không có cảnh báo hay lỗi, ổ đĩa sẽ tích tụ hàng loạt file rỗng 0 byte, gây rác đĩa và khó khăn cho kỹ thuật viên khi tìm kiếm ngày xảy ra sự cố thật sự.
   - Cần một cơ chế khởi tạo file log theo yêu cầu thực tế (**Lazy On-Demand File Creation**): Chỉ khi có dòng log đầu tiên đủ điều kiện được ghi nhận, tệp tin `logs/hpparking-api-yyyyMMdd.log` mới được hệ thống tạo ra trên đĩa.
3. **Phân định rõ ràng mục đích của Log:**
   - **Technical/Operational Log (Serilog)**: Phục vụ kỹ sư phần mềm và quản trị hạ tầng (theo dõi thời gian phản hồi HTTP request, lỗi ngoại lệ 500, lỗi kết nối mạng LAN tới FaceID).
   - **Business Audit Trail (MongoDB `AuditLog`)**: Phục vụ ban quản lý bãi xe (theo dõi lịch sử đăng nhập, thay đổi thông tin cư dân, xóa xe để đối soát trên giao diện Web Admin).
4. **Tối ưu hóa hiệu năng máy chủ (Console Sink vs File Sink):**
   - Thao tác in log ra Console màn hình đen là thao tác đồng bộ (blocking I/O) có thể làm sụt giảm thông lượng của API từ 5% - 15% khi có nhiều xe ra vào đồng thời.
   - Giải pháp: Chỉ bật Console Sink trong môi trường phát triển cục bộ (`Development`), tắt hoàn toàn Console Sink trên môi trường máy chủ chạy thật (`Production`), tập trung 100% vào File Sink.
5. **Bảo vệ an toàn thông tin nhạy cảm (PII & Credentials):**
   - Tuyệt đối không được ghi mật khẩu tài khoản người dùng (`password`, `oldPassword`, `newPassword`) hay khóa bí mật `X-API-KEY` dưới dạng văn bản thô (plain-text) vào file log.

## Quyết định

1. **Khung Ghi Log Serilog (`Serilog.AspNetCore`):**
   - Sử dụng Serilog với định dạng văn bản giàu ngữ cảnh (Human-readable):
     `[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{TraceId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}`
   - Tích hợp `app.UseSerilogRequestLogging()` tự động ghi vết thông tin HTTP request: Method, Path, StatusCode, ElapsedMs.

2. **Cơ Chế Tạo File Theo Yêu Cầu (Lazy On-Demand File Creation):**
   - Đường dẫn: `logs/hpparking-api-yyyyMMdd.log`.
   - File chỉ được mở và khởi tạo trên ổ đĩa khi có dòng log đầu tiên phát sinh trong ngày.
   - Nếu trong một ngày không có sự kiện log nào được ghi, hệ thống tuyệt đối không tạo file 0 byte.

3. **Chính Sách Lưu Giữ & Dọn Dẹp (Retention Policy):**
   - Cuộn theo ngày (`rollingInterval: RollingInterval.Day`).
   - Tự động dọn dẹp các file cũ hơn 30 ngày (`retainedFileCountLimit: 30`).
   - Giới hạn kích thước tối đa mỗi file là 50MB (`fileSizeLimitBytes: 52428800`), tự động cắt sang file phụ (`_001.log`) nếu vượt quá (`rollOnFileSizeLimit: true`).
   - Bật cờ `shared: true` cho phép mở file đọc log mà không bị khóa (non-blocking file lock).

4. **Quản Lý Console Sink Theo Môi Trường:**
   - Trong `appsettings.Development.json`: Kích hoạt Console Sink (kèm màu sắc trực quan) giúp lập trình viên theo dõi thời gian thực.
   - Trong `appsettings.json` (Production / Staging): Tắt Console Sink, chỉ ghi ra File Sink nhằm tối ưu tối đa hiệu năng CPU.

5. **Bộ Lọc Cấp Độ (Log Filtering) & Che Giấu Dữ Liệu:**
   - Mặc định hệ thống: `Information`.
   - Riêng `HPParking`: `Information` (hoặc `Debug` khi cần tra cứu sâu).
   - `Microsoft` & `System`: Giảm xuống `Warning` để loại bỏ 90% log rác của framework.
   - Tự động che giấu (mask) các trường nhạy cảm (`Password`, `ApiKey`) bằng chuỗi `***MASKED***`.

## Hệ quả

- **Ổ đĩa sạch sẽ, không file rác:** Chỉ những ngày thực sự có hoạt động hoặc sự cố mới xuất hiện file log trên đĩa.
- **Hiệu năng đạt mức tối đa:** Loại bỏ gánh nặng blocking I/O của console trên môi trường máy chủ sản xuất.
- **Dễ dàng điều tra sự cố:** Mỗi dòng log đều có `TraceId` và thời gian chính xác tới phần nghìn giây, giúp kỹ sư liên kết chuỗi sự kiện tức thì.
- **Tuân thủ an toàn thông tin:** Ngăn chặn hoàn toàn nguy cơ rò rỉ mật khẩu người dùng vào tệp nhật ký hệ thống.
