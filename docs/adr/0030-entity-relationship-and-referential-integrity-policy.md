# 0030: Chính Sách Quản Lý Khóa Quan Hệ Và Ràng Buộc Toàn Vẹn Tham Chiếu

Quyết định thiết kế về việc quản lý các khóa ngoại và đảm bảo tính toàn vẹn tham chiếu (Referential Integrity) giữa các thực thể trong `HPParking.Api` (MongoDB): áp dụng chính sách chặn xóa nghiêm ngặt (Restrict Deletion Policy: 409 Conflict khi còn thực thể phụ thuộc), xác thực kiểu thiết bị chặt chẽ khi gán làn (Strict Lane Device Type-Safety), cho phép tái sử dụng thiết bị giữa các làn (Cross-Lane Hardware Sharing), phân tách mô hình DTO danh sách và DTO chi tiết (Enriched Detail DTO), và cảnh báo an toàn khi vô hiệu hóa phần cứng đang vận hành.

## Ngữ cảnh

Trong hệ thống cơ sở dữ liệu MongoDB:
1. **Không có Foreign Key Constraints ở tầng Database Engine**: Khác với SQL RDBMS, MongoDB không tự động kích hoạt foreign key cascade hoặc restrict khi các thực thể liên quan bị thêm, sửa, xóa. Nếu tầng Service không kiểm tra nghiêm ngặt, hệ thống sẽ sinh ra các "bản ghi mồ côi" (Orphan Records) — ví dụ một Làn xe trỏ tới một Camera đã bị xóa vĩnh viễn, khiến worker luồng kiểm soát xe bị crash do NullReferenceException.
2. **Đặc thù phần cứng kiểm soát bãi đỗ**:
   - Cổng (`Gate`) quản lý Làn xe (`Lane`).
   - Làn xe liên kết cùng lúc 4 vị trí thiết bị: Camera toàn cảnh, Camera biển số, Bộ điều khiển Barrier, và Đầu đọc FaceID.
   - Một số thiết bị như Bộ điều khiển ZKTeco C3-400 có 4 cổng vật lý, thực tế phục vụ đồng thời nhiều làn khác nhau (ADR 0040). Camera toàn cảnh góc rộng cũng có thể bao quát 2 làn cạnh nhau.
   - Do đó, cần quy định rõ ràng: thiết bị nào được dùng chung giữa các làn, và điều gì xảy ra khi người dùng cố tình xóa hoặc tắt một thiết bị đang được các làn xe khác sử dụng.

## Quyết định

1. **Chính sách Chặn Xóa Tham Chiếu (Restrict Deletion Policy — 409 Conflict)**:
   - Áp dụng cơ chế **Restrict (Chặn xóa)** tại tầng Service cho toàn bộ hạ tầng:
     * **Xóa Công ty (`Company`)**: Bắt buộc kiểm tra nếu còn Phòng ban (`Department`) hoặc Cổng (`Gate`) đang trực thuộc (`!IsDeleted`) -> Chặn xóa và trả về `409 Conflict` kèm thông điệp rõ ràng.
     * **Xóa Cổng (`Gate`)**: Bắt buộc kiểm tra nếu còn Làn xe (`Lane`) đang trực thuộc (`!IsDeleted`) -> Chặn xóa và trả về `409 Conflict`.
     * **Xóa Thiết bị (`Device`)**: Bắt buộc kiểm tra nếu thiết bị đó đang được sử dụng ở bất kỳ vị trí nào trong các Làn xe (`OverviewCameraDeviceId`, `PlateCameraDeviceId`, `ControllerDeviceId`, `FaceDeviceId` của các làn chưa bị xóa) -> Chặn xóa và trả về `409 Conflict`.
   - *Ngoại lệ duy nhất*: Quan hệ Khách hàng (`Client`) -> Phương tiện (`Vehicle`) tiếp tục duy trì cơ chế Cascade Soft Delete theo ADR 0013 / ADR 0029 (xóa Client thì tự động xóa mềm các xe thuộc sở hữu).

2. **Ràng buộc Loại Thiết Bị Nghiêm Ngặt khi Gán Làn (Strict Lane Device Type-Safety)**:
   - Khi tạo mới hoặc cập nhật Làn xe (`Lane`):
     * `OverviewCameraDeviceId` và `PlateCameraDeviceId`: Nếu có truyền ID, bắt buộc thiết bị tham chiếu phải có `Type == DeviceType.Camera`.
     * `ControllerDeviceId`: Nếu có truyền ID, bắt buộc thiết bị tham chiếu phải có `Type == DeviceType.Controller`.
     * `FaceDeviceId`: Nếu có truyền ID, bắt buộc thiết bị tham chiếu phải có `Type == DeviceType.FaceId`.
     * Nếu truyền ID của thiết bị sai loại, ném `BadRequestException` (`400 Bad Request`) với thông báo lỗi chỉ rõ vị trí và loại thiết bị kỳ vọng.

3. **Chính sách Dùng Chung Thiết Bị Giữa Các Làn (Cross-Lane Hardware Sharing)**:
   - Hệ thống **cho phép một Thiết bị (`Device`) được gán vào nhiều Làn xe (`Lane`)** khác nhau:
     * Cho phép Bộ điều khiển ZKTeco C3-400 được khai báo trên 4 làn xe vào/ra cùng cổng.
     * Cho phép Camera toàn cảnh hoặc FaceID dùng chung giữa các làn (ví dụ làn 2 chiều).
   - Không áp đặt unique constraint trên các trường khóa ngoại thiết bị trong collection `Lanes`.

4. **Phân Tách DTO Danh Sách Và DTO Chi Tiết (Enriched Detail DTO Pattern)**:
   - **DTO Danh sách (`DepartmentDto`, `GateDto`, `LaneDto`)**:
     * Kế thừa `AuditableDto`.
     * Nhúng thêm các trường tên tóm tắt phẳng (`CompanyName`, `GateName`) được bổ sung qua query lookup, giúp giao diện bảng biểu (Table UI) hiển thị ngay mà không cần gọi API phụ.
   - **DTO Chi tiết (`LaneDetailDto`)**:
     * Kế thừa `LaneDto`.
     * Nạp đầy đủ các đối tượng tóm tắt của Cổng (`GateSummaryDto`) và 4 thiết bị ngoại vi (`DeviceSummaryDto` chứa `Id`, `Code`, `Name`, `Type`, `IpAddress`, `Port`, `IsActive`), tối ưu trải nghiệm người dùng trên màn hình sơ đồ cấu hình làn.

5. **Chính sách Cảnh Báo Khi Vô Hiệu Hóa (Active State Protection — 400 Bad Request)**:
   - Khi cập nhật `IsActive = false` cho một Cổng hoặc Thiết bị:
     * Kiểm tra nếu có bất kỳ Làn xe nào đang hoạt động (`IsActive == true && !IsDeleted`) phụ thuộc vào Cổng hoặc Thiết bị này:
       - Từ chối cập nhật với mã lỗi `400 Bad Request` (`INFRA_ACTIVE_DEPENDENCY_EXISTS`).
       - Yêu cầu người dùng phải gỡ thiết bị hoặc tắt Làn xe liên quan trước khi tắt thiết bị phần cứng, ngăn ngừa rủi ro mất đồng bộ tại các bốt trực bảo vệ.

## Hệ quả

- **Tích cực**:
  - Triệt tiêu 100% rủi ro sinh dữ liệu mồ côi (orphaned data) trong MongoDB.
  - Đảm bảo an toàn vật lý: không thể vô tình xóa hoặc tắt phần cứng đang điều khiển làn xe thực tế.
  - Mô hình DTO được tối ưu hóa: bảng danh sách tải nhanh, trang chi tiết cung cấp trọn vẹn thông tin phần cứng mà không cần Frontend gọi nhiều HTTP requests.
  - Linh hoạt thích ứng với các sơ đồ bãi đỗ thực tế (bộ điều khiển 4 cổng, camera góc rộng dùng chung).
- **Đánh đổi**:
  - Tầng Service phải thực hiện thêm các truy vấn kiểm tra tham chiếu trước khi thực hiện các lệnh Xóa hoặc Cập nhật `IsActive`. Chi phí này là hoàn toàn xứng đáng để đổi lấy tính toàn vẹn hệ thống.
