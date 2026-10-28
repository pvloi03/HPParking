# 0021: Chuẩn Hóa Chuỗi Middleware Pipeline 10 Bước, Tích Hợp Truy Vết W3C Trace Context Và Vị Trí Tối Ưu Cho Rate Limiter

Quyết định chuẩn hóa toàn diện chuỗi Middleware Pipeline 10 bước cho `HPParking.Api`: thiết lập thứ tự thực thi chuẩn mực (ExceptionMiddleware -> TraceIdMiddleware -> UseSerilogRequestLogging -> UseStaticFiles -> UseRouting -> UseCors -> UseAuthentication -> UseRateLimiter -> UseAuthorization -> MapControllers), áp dụng chuẩn quốc tế W3C Trace Context (`traceparent` và `X-Trace-ID`), gia cố tầng biên với Security Headers (`nosniff`, `DENY`), và tối ưu vị trí đặt `UseRateLimiter` ngay sau `UseAuthentication` để phân bổ hạn ngạch chính xác theo từng User ID (chống khóa chùm NAT IP) và API Key của đối tác tích hợp.

## Ngữ cảnh

Thứ tự sắp xếp các Middleware trong ASP.NET Core là yếu tố quyết định sự ổn định, hiệu năng và tính bảo mật của toàn bộ dịch vụ REST API:
1. **Xử lý Ngoại lệ và Truy vết lỗi (Exception & Trace Context):**
   - Nếu middleware bắt lỗi không nằm ở vị trí đầu tiên, bất kỳ sự cố nào xảy ra trong các middleware tiếp theo sẽ không thể chuyển đổi thành response JSON chuẩn `ApiResponse<T>`, dẫn đến việc rò rỉ mã lỗi 500 HTML thô ra ngoài.
   - Để mọi dòng log của Serilog và mọi response lỗi đều có mã truy vết xuyên suốt, `TraceIdMiddleware` phải chạy ngay sau `ExceptionMiddleware`. Áp dụng chuẩn W3C Trace Context (`traceparent`) giúp hệ thống tích hợp tự nhiên với `System.Diagnostics.Activity` trong .NET 8 và các công cụ APM/OpenTelemetry hiện đại.
2. **Hiệu năng phục vụ Ảnh Tĩnh (Static Files):**
   - Khách hàng xem ảnh phương tiện hoặc ảnh avatar thông qua các thẻ `<img>` với URL `/uploads/avatars/...`. Nếu request đọc file tĩnh phải đi qua chuỗi xử lý CORS, Authentication, Authorization thì CPU sẽ bị lãng phí đáng kể. Đặt `UseStaticFiles` sớm giúp trả file đĩa ngay lập tức, đạt tốc độ phản hồi cực nhanh.
3. **Bài toán CORS kết hợp với Auth:**
   - Trong ASP.NET Core, `UseCors` bắt buộc phải đặt sau `UseRouting` và trước các middleware bảo vệ (`UseAuthentication`, `UseAuthorization`, `UseRateLimiter`). Nếu đặt sai vị trí, khi một request bị từ chối 401 Unauthorized hoặc 429 Too Many Requests, trình duyệt Web Admin sẽ không nhận được header CORS, dẫn đến việc nuốt mất thông điệp lỗi thật và báo lỗi giả lập "CORS error".
4. **Vị trí tối ưu của Rate Limiter kết hợp Authentication:**
   - Nếu đặt `UseRateLimiter` trước `UseAuthentication`, hệ thống chỉ có thể phân chia hạn mức dựa trên địa chỉ IP client. Khi đó, toàn bộ nhân viên trong tòa nhà sử dụng chung 1 địa chỉ IP mạng nội bộ (NAT) sẽ bị dùng chung một hạn ngạch, dẫn đến rủi ro "khóa chùm" (một người thao tác nhiều khiến cả văn phòng bị chặn 429).
   - Bằng cách đặt `UseAuthentication` ngay trước `UseRateLimiter`, thông tin định danh `User.Id` (đối với người dùng đăng nhập JWT) và `X-API-KEY` (đối với hệ thống ERP) đã được phân giải xong. Rate Limiter có thể áp dụng chính sách phân tầng đa chiều: cấp hạn mức 120 req/phút cho từng User ID riêng biệt, 600 req/phút cho đối tác, và chỉ giới hạn theo IP cho các request chưa đăng nhập.

## Quyết định

1. **Chuỗi Middleware Pipeline Chuẩn Hóa:**
   ```csharp
   // 1. Gắn W3C Trace Context (traceparent) ngay tại cửa ngõ đầu tiên để toàn bộ pipeline và Serilog dùng chung TraceId
   app.UseMiddleware<TraceIdMiddleware>();

   // 2. Tự động ghi log tóm tắt HTTP request qua Serilog (đo toàn bộ vòng đời, bắt mã 4xx/5xx không gây nhiễu log)
   app.UseSerilogRequestLogging(options =>
   {
       options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} phản hồi {StatusCode} trong {Elapsed:0.0000} ms";
       options.GetLevel = (httpContext, elapsed, ex) =>
       {
           if (ex != null || httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
           if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
           return LogEventLevel.Verbose; // Bỏ qua request thành công (< 400)
       };
   });

   // 3. Bắt lỗi toàn cục, đóng gói ApiResponse JSON chuẩn, phân tách rạch ròi 4xx Warning vs 5xx Error
   app.UseMiddleware<ExceptionMiddleware>();

   // 4. Gia cố an ninh tầng biên HTTP (X-Content-Type-Options, X-Frame-Options, CSP)
   app.UseMiddleware<SecurityHeadersMiddleware>();

   // 5. Phục vụ file ảnh avatar tĩnh từ đĩa ngay lập tức, không tốn CPU cho Auth
   app.UseStaticFiles();

   // 6. Phân tích định tuyến URL để xác định Controller/Action
   app.UseRouting();

   // 7. Xử lý chính sách CORS trước khi chạm vào các rào chắn bảo vệ
   app.UseCors("DefaultCorsPolicy");

   // 8. Xác thực danh tính: Phân giải JWT Bearer hoặc Header X-API-KEY thành ClaimsPrincipal
   app.UseAuthentication();

   // 9. Giới hạn tần suất phân tầng đa chiều: Theo User ID (chống khóa chùm NAT) hoặc API Key
   app.UseRateLimiter();

   // 10. Kiểm tra quyền hạn vai trò (Role: Admin, Manager, Viewer)
   app.UseAuthorization();

   // 11. Thực thi Controller Action cuối cùng
   app.MapControllers();
   ```

2. **Tiêu Chuẩn Truy Vết W3C Trace Context:**
   - Đọc header `traceparent` nếu client gửi lên; nếu không có, tự động sinh mã TraceId 32 ký tự hex qua `System.Diagnostics.Activity`.
   - Luôn trả về 2 headers trong mọi HTTP response: `traceparent` (chuẩn W3C) và `X-Trace-ID` (tương thích ngược).
   - Tự động cấu hình CORS `ExposeHeaders("traceparent", "X-Trace-ID")` để trình duyệt Web Admin có thể đọc và hiển thị cho người dùng khi báo lỗi.

## Hệ quả

- **Vận hành an toàn & Không lỗi CORS giả:** Mọi kịch bản từ chối truy cập (401, 403, 429) đều trả về đầy đủ header CORS và cấu trúc JSON nhất quán.
- **Log sạch sẽ & Không rác Stack Trace (Production-Ready):** Việc đặt `UseSerilogRequestLogging` bọc bên ngoài `ExceptionMiddleware` giúp `ExceptionMiddleware` xử lý gọn gàng các ngoại lệ nghiệp vụ (400, 404, 409 trùng CCCD) thành HTTP 4xx, tránh việc Serilog tưởng nhầm server bị crash và xả 30 dòng Stack Trace rác ra file log. Trong khi đó, lỗi 500 nghiêm trọng vẫn giữ nguyên 100% chi tiết Stack Trace từ `ExceptionMiddleware`.
- **Bảo vệ chống khóa chùm NAT IP:** Nhân viên bãi xe và ban quản lý làm việc trên cùng mạng LAN nội bộ không bao giờ bị ảnh hưởng hạn ngạch của nhau.
- **Tối ưu hóa hiệu năng tối đa:** Ảnh phương tiện và avatar được phục vụ trực tiếp với độ trễ thấp nhất mà không tiêu tốn tài nguyên xác thực.
- **Dễ dàng giám sát & Điều tra sự cố:** Mã truy vết W3C đồng bộ xuyên suốt từ giao diện frontend, response headers đến từng dòng log trong Serilog.
