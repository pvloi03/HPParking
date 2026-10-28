# 0028: Chuẩn Hóa Generic Base DTOs Và Kiến Trúc Mapster IRegister Không Wrapper

Quyết định thiết kế về việc chuẩn hóa tầng truyền tải dữ liệu (DTO) và cơ chế ánh xạ đối tượng (Object Mapping) trong `HPParking.Api`: xây dựng hệ thống Generic Base DTOs (`BaseDto<TKey>`, `AuditableDto<TKey>`) kế thừa đồng nhất, loại bỏ hoàn toàn các lớp mở rộng wrapper trung gian (`MappingExtensions`), chuyển sang sử dụng trực tiếp thư viện Mapster thông qua interface `IRegister` theo từng phân hệ và biên dịch cấu hình (`config.Compile()`) khi ứng dụng khởi động.

## Ngữ cảnh

Trong các phiên bản khởi tạo ban đầu:
1. **Trùng lặp mã nguồn trong các DTO**: Các thực thể như `User`, `Client`, `Vehicle`, `Gate`, `Lane` đều sở hữu các trường định danh (`Id`) và dấu vết kiểm toán (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`). Việc khai báo lặp đi lặp lại các thuộc tính này trong từng DTO gây dư thừa mã nguồn và thiếu tính nhất quán về kiểu dữ liệu khóa chính giữa các phân hệ.
2. **Lớp wrapper ánh xạ trung gian cồng kềnh**: Ban đầu, các phương thức mở rộng như `ToDto()`, `ToEntity()`, `ToPagedResultDto()` được định nghĩa thủ công trong một lớp wrapper trung gian (`MappingExtensions.cs`). Khi số lượng thực thể và DTO tăng lên, lớp này trở thành một "God Class" chứa hàng chục phương thức mở rộng lặp lại, khó bảo trì và cản trở việc tối ưu hóa hiệu năng của thư viện Mapster.
3. **Hiệu năng ánh xạ đối tượng**: Mapster là một trong những thư viện Object Mapping nhanh nhất trong hệ sinh thái .NET nhờ cơ chế phát sinh mã IL động (Dynamic IL Generation). Việc gọi Mapster thông qua một wrapper trung gian viết tay làm giảm khả năng cấu hình sâu (custom projection, nested mapping, ignoring properties) và không tận dụng được phương thức `TypeAdapterConfig.Compile()`.

## Quyết định

1. **Hệ thống Generic Base DTOs**:
   - Khởi tạo tại `HPParking.Api/DTOs/Common/BaseDto.cs` với cấu trúc phân cấp:
     * `BaseDto<TKey>`: Chứa trường định danh duy nhất `Id` có kiểu dữ liệu `TKey`.
     * `BaseDto`: Kế thừa `BaseDto<string>` (chuẩn hóa khóa chính dạng chuỗi tương thích MongoDB ObjectId).
     * `AuditableDto<TKey>`: Kế thừa `BaseDto<TKey>`, bổ sung các trường dấu vết kiểm toán: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`.
     * `AuditableDto`: Kế thừa `AuditableDto<string>` làm lớp cơ sở mặc định cho toàn bộ DTO phản hồi của các thực thể nghiệp vụ (`ClientDto`, `VehicleDto`, `GateDto`, `LaneDto`, `DeviceDto`).
   - Đảm bảo tất cả API response DTO đều có chung một cấu trúc chuẩn, hỗ trợ tự động hóa serialization và kiểm thử.

2. **Áp dụng Mapster `IRegister` theo từng phân hệ (Modular Mapping)**:
   - Thay vì tập trung mapping vào một tệp trung gian duy nhất, mỗi phân hệ nghiệp vụ tự quản lý cấu hình ánh xạ của mình bằng cách hiện thực interface `IRegister` của Mapster:
     * `ClientMappingConfig : IRegister`: Cấu hình ánh xạ `Client` <-> `ClientDto`, `Client` <-> `ClientDetailDto`.
     * `VehicleMappingConfig : IRegister`: Cấu hình ánh xạ `Vehicle` <-> `VehicleDto`.
     * `InfrastructureMappingConfig : IRegister`: Cấu hình ánh xạ `Gate`, `Lane`, `Device` (tự động bỏ qua trường mật khẩu thiết bị).
   - Tách biệt rõ ràng ranh giới phân hệ, tuân thủ nguyên lý Single Responsibility.

3. **Tự động quét và Biên dịch trước (Scan & Compile-time Mapping)**:
   - Tại `HPParking.Api/Configuration/MapsterConfig.cs`:
     - Gọi `TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly())` để tự động phát hiện và đăng ký mọi cấu hình `IRegister` trong assembly mà không cần đăng ký thủ công từng lớp.
     - Thực thi `TypeAdapterConfig.GlobalSettings.Compile()` ngay lúc khởi động ứng dụng (`Program.cs`) để tiền biên dịch toàn bộ cây ánh xạ đối tượng sang biểu thức lambda IL.
     - Xóa bỏ vĩnh viễn tệp `MappingExtensions.cs`.

4. **Cách thức sử dụng trực tiếp trong Service Layer**:
   - Trong các Service nghiệp vụ (`ClientService`, `VehicleService`...):
     - Chuyển đổi một đối tượng: `entity.Adapt<ClientDto>()`.
     - Chuyển đổi danh sách đối tượng: `entities.Adapt<List<ClientDto>>()`.
     - Chuyển đổi trang dữ liệu: `pagedEntities.Items.Adapt<List<ClientDto>>()`.
     - Hoàn toàn trong sáng, gọn gàng và không phụ thuộc lớp wrapper trung gian.

## Hệ quả

- **Tích cực**:
  - Giảm thiểu hơn 70% mã nguồn trùng lặp trong khai báo DTO nhờ kế thừa `AuditableDto`.
  - Loại bỏ hoàn toàn lớp wrapper trung gian `MappingExtensions`, mã nguồn tầng Service sạch sẽ và đúng chuẩn hiện đại của .NET.
  - Tối đa hóa hiệu năng runtime của Mapster nhờ cơ chế tiền biên dịch `Compile()`.
  - Dễ dàng mở rộng cho các thực thể mới (Gate, Lane, Device, Company) chỉ bằng cách tạo file `*MappingConfig.cs` kế thừa `IRegister`.
- **Đánh đổi**:
  - Các kỹ sư mới cần nắm rõ cách hoạt động của Mapster `IRegister` thay vì viết các phương thức ánh xạ thủ công kiểu truyền thống.
