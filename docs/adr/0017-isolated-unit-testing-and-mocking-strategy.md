# 0017: Chiến Lược Kiểm Thử Unit Test Độc Lập Qua NSubstitute, Bao Phủ Toàn Bộ Ca Biên Và Lọc Chạy Theo Namespace

Quyết định chuẩn hóa chiến lược kiểm thử Unit Test cho `HPParking.Api`: tích hợp bộ test vào namespace `HPParking.Tests.Api`, áp dụng mô hình kiểm thử cô lập (Isolated Unit Testing) sử dụng NSubstitute và FluentAssertions, mock 100% tầng dữ liệu `IRepository<T>` không phụ thuộc CSDL MongoDB, bao phủ toàn diện các ca kiểm thử biên (Edge Cases: trùng SĐT, trùng biển số active, khách VIP không biển số, xóa mềm cascade và lọc trùng FaceID IP), hỗ trợ chạy kiểm thử độc lập siêu tốc thông qua bộ lọc namespace `dotnet test --filter "FullyQualifiedName~HPParking.Tests.Api"`.

## Ngữ cảnh

Khi phát triển dịch vụ REST API `HPParking.Api`, việc đảm bảo tính đúng đắn của logic nghiệp vụ, tính an toàn của các ràng buộc dữ liệu và độ tin cậy của phân quyền là yêu cầu tối thượng.

Các bài toán kỹ thuật đặt ra:
1. **Lựa chọn mô hình kiểm thử:**
   - Cân nhắc giữa:
     * *Integration Tests (End-to-End)*: Khởi động WebApplicationFactory và database MongoDB thật. Nhược điểm: Chạy chậm, dễ bị ảnh hưởng bởi môi trường mạng/cổng port, khó thiết lập giả lập lỗi phần cứng mạng FaceID.
     * *Isolated Unit Tests*: Sử dụng NSubstitute để mock toàn bộ các interface phụ thuộc (`IRepository<T>`, `IFaceIdService`, `IFileStorageService`). Ưu điểm: Tốc độ thực thi tính bằng miligiây, hoàn toàn độc lập, dễ dàng kiểm thử mọi kịch bản ngoại lệ và lỗi mạng.
2. **Chiến lược giả lập tầng dữ liệu (Database Mocking):**
   - Thay vì dựng MongoDB in-memory hoặc Mongo2Go, việc mock generic `IRepository<T>` bằng NSubstitute cho phép thiết lập trạng thái dữ liệu giả định và kiểm tra số lần gọi hàm (`Received()`, `DidNotReceive()`) một cách trực quan, chính xác.
3. **Bao phủ các ca kiểm thử biên trọng yếu (Edge Cases):**
   - Kiểm tra chặt chẽ các quy tắc nghiệp vụ cốt lõi:
     * Khóa định danh `PhoneNumber`: Bị từ chối khi trùng lặp với Client khác; được phép cập nhật nếu giữ nguyên SĐT của chính mình.
     * Ràng buộc biển số xe: Chuẩn hóa ký tự, ngăn chặn đăng ký biển số đã tồn tại trên một xe đang kích hoạt (`IsActive && !IsDeleted`), nhưng cho phép đăng ký lại biển số của xe đã bị xóa mềm trước đó.
     * Khách hàng VIP: Cho phép tạo và cập nhật hồ sơ với danh sách xe rỗng (`0` vehicles).
     * Xóa mềm cascade (Cascade Soft-Delete): Khi xóa Client, toàn bộ các Vehicle liên kết phải được đánh dấu xóa mềm, đồng thời phát lệnh thu hồi quyền FaceID trên các thiết bị active.
     * Đồng bộ FaceID: Lọc trùng lặp IP thiết bị bằng `HashSet<string>`, đảm bảo các thiết bị khác vẫn được nạp thành công ngay cả khi có 1 thiết bị mạng bị lỗi.
4. **Quy trình chạy kiểm thử độc lập (Test Execution Flow):**
   - Để tối ưu hóa chu kỳ phát triển (Feedback Loop) của lập trình viên, cần cho phép chạy riêng lẻ bộ test của API bằng câu lệnh `dotnet test --filter "FullyQualifiedName~HPParking.Tests.Api"` mà không cần chờ chạy lại 141 tests hiện có của ứng dụng WinForms, giúp tiết kiệm thời gian mà vẫn đảm bảo độ tin cậy cao.

## Quyết định

1. **Cấu trúc Tổ chức Thư mục Kiểm thử:**
   - Đặt toàn bộ mã nguồn kiểm thử API vào thư mục `HPParking.Tests/Api/` thuộc namespace `HPParking.Tests.Api`:
     * `Services/`: `AuthServiceTests.cs`, `ClientServiceTests.cs`, `VehicleServiceTests.cs`, `FileStorageServiceTests.cs`
     * `Controllers/`: `AuthControllerTests.cs`, `ClientsControllerTests.cs`, `VehiclesControllerTests.cs`
     * `Validators/`: `LoginRequestValidatorTests.cs`, `ClientValidatorTests.cs`, `VehicleValidatorTests.cs`
     * `Middlewares/`: `ExceptionMiddlewareTests.cs`, `ApiKeyAuthenticationHandlerTests.cs`

2. **Khung Công Cụ & Thư Viện:**
   - Framework kiểm thử: **xUnit** (2.9.x)
   - Thư viện Mocking: **NSubstitute** (5.3.x)
   - Thư viện Assertion: **FluentAssertions** (7.0.x)
   - Quy ước viết test: Cấu trúc **Arrange - Act - Assert (AAA)**, đặt tên phương thức theo mẫu: `MethodName_StateUnderTest_ExpectedBehavior`.

3. **Mocking 100% Tầng Phụ Thuộc:**
   - Mọi tương tác CSDL được thực hiện qua mock `IRepository<Client>`, `IRepository<Vehicle>`, `IRepository<User>`, `IRepository<AuditLog>`.
   - Giao tiếp phần cứng FaceID được mock qua `IFaceIdService`.
   - Lưu trữ file được mock qua `IFileStorageService`.

4. **Lệnh Chạy Kiểm Thử Độc Lập:**
   - Lập trình viên kiểm tra riêng phân hệ API qua lệnh:
     ```bash
     dotnet test HPParking.Tests/HPParking.Tests.csproj --filter "FullyQualifiedName~HPParking.Tests.Api"
     ```
   - Chạy toàn bộ giải pháp khi release/commit:
     ```bash
     dotnet test HPParking.Tests/HPParking.Tests.csproj
     ```

## Hệ quả

- **Vòng phản hồi cực nhanh (Sub-second Feedback Loop):** Toàn bộ test suite của API thực thi xong trong vòng 1-2 giây, hỗ trợ tối đa quy trình lập trình TDD.
- **Bảo vệ toàn diện trước lỗi hồi quy (Regression Shield):** Đảm bảo mọi thay đổi trong mã nguồn API không thể vô tình phá vỡ các quy tắc nghiệp vụ hoặc an ninh hệ thống.
- **Tương thích 100% với giải pháp hiện hữu:** Tận dụng trọn vẹn hạ tầng xUnit/NSubstitute sẵn có trong `HPParking.Tests` mà không cần cấu hình thêm bất kỳ công cụ phụ trợ nào.
