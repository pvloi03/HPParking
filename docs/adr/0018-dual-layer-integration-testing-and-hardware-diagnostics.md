# 0018: Chiến Lược Kiểm Thử Tích Hợp Phân Lớp Kép (Dual-Layer Integration Testing) Qua WebApplicationFactory Và Chẩn Đoán Phần Cứng FaceID Thực Tế

Quyết định chuẩn hóa chiến lược kiểm thử tích hợp (Integration Testing) cho dịch vụ `HPParking.Api`: áp dụng mô hình phân lớp kép (Dual-Layer) kết hợp giữa bộ kiểm thử tự động hóa In-Memory TestServer thông qua `WebApplicationFactory<Program>` (sử dụng MongoDB CSDL test riêng `hpparking_integration_test` và Mock FaceID) và bộ kiểm thử chẩn đoán phần cứng trực tiếp (Live Hardware Diagnostics) kết nối thiết bị FaceID Hikvision thật trong mạng LAN, bao phủ toàn bộ 5 luồng nghiệp vụ End-to-End cốt lõi từ xác thực, giới hạn tốc độ, vòng đời khách hàng/xe, xóa mềm cascade đến giao tiếp bên thứ ba qua API Key.

## Ngữ cảnh

Sau khi chuẩn hóa kiểm thử Unit Test ở ADR 0017, hệ thống cần kiểm chứng sự phối hợp đồng bộ giữa tất cả các thành phần trong toàn bộ chu trình xử lý của một yêu cầu HTTP thực tế:
1. **Kiểm tra tính toàn vẹn của HTTP Pipeline:**
   - Cần xác nhận các middleware (Routing, ExceptionMiddleware, Rate Limiter, ApiKeyAuthenticationHandler, JwtBearerHandler) phối hợp nhịp nhàng, mô hình phân giải Scheme hoạt động chính xác và các mã phản hồi HTTP (200, 201, 400, 401, 403, 429) được trả về đúng chuẩn `ApiResponse<T>`.
2. **Kiểm tra tính toàn vẹn với CSDL MongoDB thật:**
   - Khác với Unit Test (mock repository), Integration Test cần tương tác trực tiếp với máy chủ MongoDB cục bộ để xác nhận:
     * Các filter truy vấn BSON, toán tử điều kiện và logic Soft Delete hoạt động chính xác.
     * Dữ liệu được ghi nhận vào đúng Collection (`Clients`, `Vehicles`, `Users`, `AuditLogs`).
     * Cơ chế phân trang `PagedResult<T>` tính toán chính xác trên dữ liệu CSDL thực.
3. **Bài toán kiểm thử phần cứng ngoại vi (FaceID Terminals):**
   - Hệ thống hiện tại đang có sẵn thiết bị phần cứng FaceID Hikvision kết nối mạng LAN tại công trường (IP `192.168.1.205`).
   - Nếu bộ test tự động bắt buộc phải có thiết bị thật online mới chạy được, pipeline CI/CD hoặc môi trường máy phát triển khác sẽ bị fail.
   - Ngược lại, nếu chỉ dùng Mock hoàn toàn, kỹ sư không thể kiểm chứng tính tương thích thực tế của giao thức ISAPI Digest Auth với thiết bị phần cứng có sẵn.
   - Do đó, cần một giải pháp **kết hợp phân lớp kép (Dual-Layer)**:
     * *Lớp 1 (Automated In-Memory Pipeline)*: Sử dụng `WebApplicationFactory` với CSDL test thật nhưng mock tầng ngoại vi FaceID, đảm bảo 100% tự động, chạy ổn định, không phụ thuộc kết nối vật lý.
     * *Lớp 2 (Live Hardware Diagnostics)*: Bộ test độc lập kết nối trực tiếp thiết bị FaceID thật, phục vụ chẩn đoán và nghiệm thu phần cứng khi có kết nối mạng LAN.

## Quyết định

1. **Khởi tạo `CustomWebApplicationFactory`:**
   - Kế thừa `WebApplicationFactory<Program>` từ gói `Microsoft.AspNetCore.Mvc.Testing`.
   - Ghi đè cấu hình `MongoDb:DatabaseName` trỏ tới cơ sở dữ liệu test riêng biệt: `hpparking_integration_test`.
   - Tự động làm sạch các Collection (`Clients`, `Vehicles`, `Users`, `AuditLogs`) trước mỗi Test Class thông qua `IClassFixture<CustomWebApplicationFactory>`.
   - Seed sẵn tài khoản `admin` mẫu và cấu hình API Key mẫu để phục vụ xác thực.

2. **Mô Hình Phân Lớp Kép (Dual-Layer Testing Strategy):**
   - **Lớp 1: Automated Integration Tests (`HPParking.Tests.Api.Integration`)**:
     * Thay thế `IFaceIdService` bằng Mock trong DI container của TestServer.
     * Chạy tự động trong mọi chu trình build và CI/CD.
   - **Lớp 2: Live Hardware Tests (`HPParking.Tests.Api.Integration.LiveHardware`)**:
     * Kết nối trực tiếp IP thiết bị FaceID thật (mặc định: `192.168.1.205`).
     * Được đánh dấu riêng bằng `[Trait("Category", "HardwareLive")]` hoặc `[Fact(Skip = "...")]` tương tự như `OverviewCameraDiagnosticTests` hiện hữu.
     * Cho phép kỹ sư chạy kiểm tra thực tế bằng lệnh chuyên biệt khi cắm mạng LAN với đầu đọc FaceID.

3. **Bao Phủ 5 Luồng Nghiệp Vụ End-to-End Cốt Lõi:**
   - *Flow 1 (Auth & Rate Limiting)*: Login thành công nhận JWT, truy cập `/me`, chặn khi sai mật khẩu/tài khoản khóa, kích hoạt HTTP 429 sau 5 lần thử sai trong 1 phút, đổi mật khẩu thành công.
   - *Flow 2 (Client & Vehicle Lifecycle)*: Tạo Client kèm xe, truy vấn phân trang, chặn trùng SĐT, thêm xe thứ 2, chặn trùng biển số active, upload ảnh avatar thật (`multipart/form-data`) và đọc ảnh tĩnh qua URL.
   - *Flow 3 (Soft-Delete & Cascade Revocation)*: Từ chối quyền xóa của Manager (403), chấp thuận quyền xóa của Admin (200), kiểm tra CSDL xác nhận cả Client và tất cả xe liên kết chuyển `IsDeleted = true`.
   - *Flow 4 (Third-Party API Key)*: Truy cập API thành công chỉ với header `X-API-KEY`, từ chối khi sai key (401).
   - *Flow 5 (Live Hardware Verification)*: Nạp thẻ/khuôn mặt mẫu lên FaceID thật, truy vấn kiểm tra và xóa dọn dẹp trên thiết bị.

4. **Lệnh Thực Thi Kiểm Thử Độc Lập:**
   - Chạy bộ Integration Test tự động:
     ```bash
     dotnet test HPParking.Tests/HPParking.Tests.csproj --filter "FullyQualifiedName~HPParking.Tests.Api.Integration&Category!=HardwareLive"
     ```
   - Chạy bộ kiểm thử chẩn đoán phần cứng FaceID thật:
     ```bash
     dotnet test HPParking.Tests/HPParking.Tests.csproj --filter "FullyQualifiedName~HPParking.Tests.Api.Integration.LiveHardware"
     ```

## Hệ quả

- **Bảo chứng chất lượng toàn diện:** Đảm bảo toàn bộ chu trình xử lý từ tiếp nhận HTTP request, phân giải bảo mật, kiểm tra dữ liệu, ghi nhận CSDL MongoDB đến sinh response đều hoạt động hoàn hảo.
- **Tận dụng tối đa phần cứng có sẵn:** Kỹ sư có công cụ kiểm thử tự động trực tiếp trên thiết bị FaceID Hikvision mà không cần thao tác bấm tay trên giao diện.
- **An toàn tuyệt đối cho CI/CD:** Bộ test tự động không bao giờ bị phụ thuộc hay treo do thiết bị ngoại vi offline.
