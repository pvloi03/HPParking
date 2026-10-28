# 0013: Kiến Trúc Dự Án HPParking.Api Độc Lập, Xác Thực Hybrid JWT/API Key Và Phạm Vi Module Client & Vehicle Giai Đoạn 1

Hệ thống mở rộng kiến trúc kiểm soát bãi xe HPParking thông qua việc bổ sung dự án `HPParking.Api` (ASP.NET Core Web API .NET 8.0 AnyCPU) độc lập trong cùng Solution, tái sử dụng toàn bộ tầng dữ liệu `HPParking.Core`, triển khai cơ chế xác thực kép (JWT Bearer cho người dùng quản trị và API Key cho tích hợp bên thứ ba), tập trung hoàn thiện phân hệ Quản lý Khách hàng & Phương tiện (Client & Vehicle) trong giai đoạn 1, và tách biệt quy trình đồng bộ FaceID thành endpoint chuyên trách để đảm bảo hiệu năng cao cho các thao tác CRUD.

## Ngữ cảnh

Hệ thống HPParking hiện đang hoạt động ổn định với ứng dụng WinForms x86 trên máy trạm tại các cổng vật lý, kết nối trực tiếp với các thiết bị phần cứng (đầu đọc FaceID Hikvision, camera LPR, bộ điều khiển ZKTeco C3-400) và lưu trữ dữ liệu tập trung trên MongoDB. Để đáp ứng nhu cầu quản trị từ xa qua Web Admin / Mobile Dashboard và mở rộng khả năng kết nối với hệ sinh thái bên thứ ba (hệ thống nhân sự HRM/ERP, hệ sinh thái cư dân đô thị), hệ thống cần một giao diện REST API chuẩn hóa.

Các bài toán và đánh đổi kỹ thuật được đặt ra:
1. **Mô hình triển khai dịch vụ (Service Hosting Model):**
   - *Lựa chọn A (Nhúng Kestrel vào WinForms):* Chạy Kestrel Web API trực tiếp bên trong ứng dụng WinForms của máy trạm. Nhược điểm: Phụ thuộc vào vòng đời bật/tắt của máy trạm cổng, rủi ro sập API khi máy trạm gặp sự cố, khó cân bằng tải và bảo mật.
   - *Lựa chọn B (Dự án ASP.NET Core Web API độc lập):* Tạo project `HPParking.Api` độc lập trong Solution, tham chiếu trực tiếp `HPParking.Core`. Dịch vụ có thể triển khai trên Máy chủ trung tâm, Docker hoặc Windows Service chạy 24/7 độc lập với máy trạm cổng.
2. **Cơ chế đồng bộ phần cứng FaceID khi thao tác dữ liệu qua API:**
   - Khi Admin hoặc hệ thống ERP thêm/sửa Client qua REST API, việc đồng bộ khuôn mặt lên các đầu đọc FaceID (Hikvision ISAPI) qua mạng LAN có thể mất từ 2-5 giây/thiết bị hoặc thất bại nếu thiết bị đang offline.
   - Nếu thực hiện đồng bộ đồng bộ (synchronous) ngay trong request CRUD, API sẽ bị nghẽn (blocking), giảm thông lượng và dễ gây timeout cho client gọi API.
3. **Mô hình quan hệ Client và Vehicle:**
   - Trong `HPParking.Core`, thực thể `Vehicle` liên kết với `Client` qua trường `OwnerClientId`. Một Client có thể sở hữu nhiều phương tiện (1-N, ví dụ 1 ô tô + 1 xe máy) hoặc 0 phương tiện (khách VIP). Biển số xe cần được đảm bảo tính duy nhất trên toàn hệ thống giữa các phương tiện đang hoạt động (`IsActive && !IsDeleted`).
4. **Cơ chế xác thực & Phân quyền (Authentication & Authorization):**
   - Cần hỗ trợ cả hai đối tượng tiêu thụ: người dùng tương tác qua giao diện Web/Mobile (cần phiên làm việc, phân quyền theo vai trò `UserRole`: Admin, Manager, Viewer) và các ứng dụng tự động bên thứ ba (cần cơ chế API Key an toàn, không phụ thuộc vào chu kỳ hết hạn ngắn của Access Token).

## Quyết định

1. **Khởi tạo Project `HPParking.Api` độc lập (.NET 8.0 AnyCPU):**
   - Nằm trong cùng Solution `HPParking.sln`, tham chiếu trực tiếp `HPParking.Core`.
   - Triển khai độc lập với máy trạm WinForms, kết nối trực tiếp vào MongoDB trung tâm thông qua `MongoDbContext` và `IRepository<T>`.
   - Cấu hình Swagger/OpenAPI UI phục vụ kiểm thử và tài liệu hóa trực quan.

2. **Phạm vi Nghiệp vụ Giai đoạn 1 (Phase 1 / MVP):**
   - Tập trung duy nhất vào **Phân hệ Quản lý Khách hàng & Phương tiện (Clients & Vehicles)** và **Phân hệ Xác thực (Authentication)**.
   - Các phân hệ: Tra cứu lượt gửi xe (`ParkingSession`), Dashboard thống kê báo cáo, Cấu hình Cổng/Làn (`Gate`/`Lane`), và Thu phí/Vé gửi xe được phân định rõ ràng thuộc Giai đoạn 2.

3. **Cơ chế Xác thực Kép (Hybrid Authentication):**
   - **JWT Bearer Authentication:** Phục vụ Web Admin / Mobile App qua endpoint `POST /api/auth/login`. Xác thực với thực thể `User` trong MongoDB, phân quyền truy cập thông qua `UserRole` (`Admin`, `Manager`, `Viewer`).
   - **API Key Authentication:** Phục vụ hệ thống bên thứ ba tích hợp thông qua HTTP Header `X-API-KEY`.

4. **Tách biệt Đồng bộ FaceID (Explicit Asynchronous FaceID Sync Endpoint):**
   - Các endpoint CRUD Client (`POST /api/clients`, `PUT /api/clients/{id}`) chỉ ghi nhận dữ liệu vào MongoDB và lưu trữ ảnh, phản hồi nhanh chóng (`< 100ms`).
   - Cung cấp endpoint chuyên biệt `POST /api/clients/{id}/sync-faceid` để kích hoạt đồng bộ thông tin thẻ/SĐT và ảnh khuôn mặt lên toàn bộ đầu đọc FaceID của các làn xe đang kích hoạt.
   - Tái sử dụng logic lọc FaceID theo IP duy nhất (`ResolveUniqueLaneFaceIdServicesAsync`) đã được chuẩn hóa trong `FrmRegisterClient.cs`.

5. **Chính sách Lưu trữ Ảnh Avatar:**
   - Nhận ảnh qua `multipart/form-data` (file định dạng JPG/PNG/WEBP).
   - Lưu trữ file vật lý trên thư mục đĩa cục bộ hoặc thư mục mạng chia sẻ UNC/NAS tập trung theo cấu trúc đường dẫn rõ ràng.
   - Cung cấp đường dẫn tĩnh (Static File Middleware, ví dụ: `/uploads/avatars/{filename}`) để các ứng dụng Web/Mobile hiển thị trực tiếp.

6. **Chính sách Xóa Mềm (Soft Delete) & Thu hồi Quyền Phần cứng:**
   - Endpoint `DELETE /api/clients/{id}` thực hiện xóa mềm `Client` (`IsDeleted = true`, `DeletedAt = DateTime.UtcNow`).
   - Tự động xóa mềm toàn bộ các `Vehicle` liên kết với `Client` này.
   - Tự động phát lệnh thu hồi/xóa thẻ và khuôn mặt của khách hàng trên tất cả các đầu đọc FaceID đang hoạt động để đảm bảo an ninh bãi xe ngay lập tức.

7. **Chuẩn hóa Định dạng Dữ liệu (Standardized API Envelope):**
   - Mọi response từ API tuân theo cấu trúc bao gói thống nhất:
     ```json
     {
       "success": true,
       "data": { ... },
       "message": "Thông điệp kết quả",
       "errors": []
     }
     ```
   - Dữ liệu danh sách hỗ trợ phân trang chuẩn `PagedResult<T>` (`pageIndex`, `pageSize`, `totalCount`, `totalPages`).

## Hệ quả

- **Tách bạch mối quan tâm (Separation of Concerns):** Máy trạm WinForms tập trung xử lý luồng xe thời gian thực ở cổng mà không bị quá tải bởi các yêu cầu truy vấn quản trị từ xa.
- **Tính khả dụng và Hiệu năng cao:** Các thao tác nghiệp vụ thêm/sửa khách hàng trên Web Admin phản hồi tức thì, không bị phụ thuộc vào tình trạng kết nối của thiết bị phần cứng mạng LAN.
- **Tương thích toàn diện Domain Model:** Tận dụng 100% các thực thể và generic repository có sẵn trong `HPParking.Core`, đảm bảo tính toàn vẹn dữ liệu giữa ứng dụng WinForms và REST API.
- **Bảo mật phân lớp:** Kết hợp chặt chẽ giữa JWT role-based claims và API Key giúp quản soát an ninh chặt chẽ cho cả người dùng lẫn máy chủ đối tác.
