# 0014: Thiết Lập Dự Án HPParking.Api, Kiến Trúc Controller-Service-DTO, Cách Ly Tuyệt Đối WinForms & Chuẩn Hóa Công Nghệ

Quyết định chuẩn hóa kiến trúc mã nguồn và thiết lập dự án `HPParking.Api`: áp dụng mô hình phân tầng Controller - Service - DTO tinh gọn, cách ly hoàn toàn không di dời hay phụ thuộc mã nguồn từ dự án WinForms `HPParking`, tái sử dụng tầng dữ liệu `HPParking.Core`, quản trị cấu hình qua Environment Variables kết hợp appsettings.json, sử dụng Mapster cho chuyển đổi đối tượng, FluentValidation cho kiểm tra dữ liệu, Global Exception Middleware cho xử lý lỗi toàn cục, tự động Seed tài khoản Admin ban đầu, chạy trên cổng mặc định 5050 và tích hợp kiểm thử tập trung vào `HPParking.Tests/Api/`.

## Ngữ cảnh

Sau khi hoàn thành xác định yêu cầu và phạm vi nghiệp vụ cho REST API ở ADR 0013, dự án cần một thiết kế kiến trúc mã nguồn (Code Architecture) rõ ràng, tường minh và dễ kiểm thử cho `HPParking.Api`.

Các bài toán kỹ thuật đặt ra:
1. **Mức độ liên kết và cách ly với WinForms:**
   - Dự án WinForms `HPParking` đang chạy phục vụ máy trạm cổng.
   - Nguyên tắc cốt lõi: **Tuyệt đối không lấy code, không di chuyển file và không làm ảnh hưởng đến mã nguồn WinForms hiện tại.**
   - `HPParking.Api` chỉ tham chiếu duy nhất tới thư viện dùng chung `HPParking.Core` (chứa các Entities, MongoDbContext, Generic MongoRepository). Toàn bộ các dịch vụ cần thiết (ISAPI FaceID Client, Auth Service, File Storage, Client/Vehicle Business Services) sẽ được lập trình mới hoàn toàn bên trong `HPParking.Api`.
2. **Mô hình kiến trúc nội bộ (Internal Layering Pattern):**
   - Cần một mô hình đủ đơn giản, dễ đọc, dễ kiểm thử và tuân thủ nguyên lý Single Responsibility Principle (SRP).
   - Lựa chọn mô hình Controller - Service - DTO 3 lớp:
     * `Controllers/`: Chỉ tiếp nhận HTTP request, phân quyền, gọi Service tương ứng và trả về `ApiResponse<T>`.
     * `Services/`: Chứa toàn bộ nghiệp vụ, tương tác với `IRepository<T>`, gọi FaceID client, upload ảnh.
     * `DTOs/`: Tách rời hoàn toàn khỏi `Entities` trong `HPParking.Core` để kiểm soát dữ liệu vào/ra an toàn.
3. **Thư viện công nghệ (Tech Stack Choices):**
   - Chuyển đổi dữ liệu (Object Mapping): Chọn **Mapster** (hiệu năng cao, dựa trên Expression Trees, cấu hình nhanh chóng).
   - Kiểm tra dữ liệu (Validation): Chọn **FluentValidation** (tách biệt các rule validation thành các lớp riêng, tự động format lỗi).
   - Bảo mật mật khẩu: Sử dụng **BCrypt.Net-Next** (thuật toán băm mật khẩu chuẩn bảo mật cao).
   - Xử lý lỗi: Sử dụng **Global Exception Middleware** bắt mọi ngoại lệ và format ra JSON chuẩn `ApiResponse<T>`.
4. **Cổng mạng và Môi trường chạy:**
   - Cổng HTTP mặc định là **5050** (tránh xung đột cổng 5000 đang được sử dụng bởi SignalR Hub của đầu đọc CCCD HN212).
   - Hỗ trợ ghi đè linh hoạt qua biến môi trường `ASPNETCORE_URLS` và `MONGODB_CONNECTION_STRING` để tối ưu cho việc đóng gói Docker container.
5. **Cơ chế khởi tạo tài khoản quản trị (Initial Admin Seeding):**
   - Khi chạy API lần đầu, hệ thống tự động kiểm tra bảng `Users` trong MongoDB. Nếu chưa có tài khoản nào thuộc vai trò `Admin`, ứng dụng tự động seed tài khoản admin với mật khẩu được cấu hình từ appsettings hoặc biến môi trường `DEFAULT_ADMIN_PASSWORD`.
6. **Chiến lược kiểm thử (Testing Strategy):**
   - Tích hợp bộ kiểm thử API vào thư mục `Api/` trong dự án `HPParking.Tests` hiện tại, đảm bảo toàn bộ hệ thống (WinForms + API) được kiểm thử tập trung bằng 1 lệnh `dotnet test`.

## Quyết định

1. **Khởi tạo dự án `HPParking.Api` (.NET 8.0 Web API, AnyCPU):**
   - Thêm vào Solution `HPParking.sln`.
   - Tham chiếu duy nhất tới `HPParking.Core`.
   - Không tham chiếu và không can thiệp vào dự án `HPParking` (WinForms).

2. **Cấu trúc thư mục dự án `HPParking.Api`:**
   ```
   HPParking.Api/
   ├── Controllers/
   │   ├── AuthController.cs
   │   ├── ClientsController.cs
   │   └── VehiclesController.cs
   ├── Services/
   │   ├── Interfaces/
   │   │   ├── IAuthService.cs
   │   │   ├── IClientService.cs
   │   │   ├── IVehicleService.cs
   │   │   ├── IFileStorageService.cs
   │   │   └── IFaceIdService.cs
   │   └── Implementations/
   │       ├── AuthService.cs
   │       ├── ClientService.cs
   │       ├── VehicleService.cs
   │       ├── LocalFileStorageService.cs
   │       └── HikvisionFaceIdService.cs
   ├── DTOs/
   │   ├── Common/ (ApiResponse.cs, PagedResult.cs, PaginationQuery.cs)
   │   ├── Auth/ (LoginRequest.cs, LoginResponse.cs, UserInfoDto.cs)
   │   ├── Clients/ (ClientDto.cs, CreateClientRequest.cs, UpdateClientRequest.cs, ClientFilterQuery.cs)
   │   ├── Vehicles/ (VehicleDto.cs, CreateVehicleRequest.cs, UpdateVehicleRequest.cs)
   │   └── FaceId/ (FaceIdSyncResultDto.cs, DeviceSyncStatusDto.cs)
   ├── Validators/ (CreateClientRequestValidator.cs, CreateVehicleRequestValidator.cs, LoginRequestValidator.cs)
   ├── Mappings/ (MappingConfig.cs)
   ├── Middlewares/ (ExceptionMiddleware.cs, ApiKeyMiddleware.cs)
   ├── Configuration/ (JwtSettings.cs, ApiKeySettings.cs, StorageSettings.cs)
   ├── Data/ (DbSeeder.cs)
   ├── Program.cs
   ├── appsettings.json
   └── appsettings.Development.json
   ```

3. **Cấu hình Dependencies (NuGet Packages):**
   - `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.x)
   - `Swashbuckle.AspNetCore` (6.6.x)
   - `FluentValidation.AspNetCore` (11.x)
   - `Mapster` (7.x)
   - `Mapster.DependencyInjection` (1.x)
   - `BCrypt.Net-Next` (4.x)

4. **Tích hợp Kiểm thử Tự động:**
   - Tạo thư mục `HPParking.Tests/Api/` bao gồm:
     * `Controllers/`: Kiểm thử Mock Controller responses.
     * `Services/`: Kiểm thử nghiệp vụ ClientService, VehicleService, AuthService.
     * `Validators/`: Kiểm thử các quy tắc FluentValidation.

## Hệ quả

- **Tính độc lập & an toàn tuyệt đối:** Dự án WinForms `HPParking` không bị tác động, không có nguy cơ hồi quy (regression) mã nguồn máy trạm.
- **Kiến trúc rõ ràng, hiện đại:** Chuẩn phân tầng 3 lớp kết hợp Mapster và FluentValidation giúp mã nguồn trong sáng, dễ đọc, dễ viết test và bảo trì lâu dài.
- **Sẵn sàng cho môi trường Container & Cloud:** Cấu hình thông qua Environment Variables cho phép đóng gói Docker và triển khai dễ dàng lên môi trường Production/Staging.
- **Quy trình kiểm thử hợp nhất:** Toàn bộ test suite được chạy đồng bộ qua `dotnet test`, đảm bảo độ tin cậy của toàn bộ giải pháp phần mềm.
