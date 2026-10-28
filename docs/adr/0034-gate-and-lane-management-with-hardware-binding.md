# 0034: Quản Lý Cổng & Làn Xe Kiểm Soát Bãi Đỗ Kèm Ràng Buộc Phần Cứng (Gate & Lane Management with Hardware Binding)

Quyết định thiết kế về việc chuẩn hóa cụm REST API quản lý Cổng kiểm soát (`Gate`) và Làn xe (`Lane`) trong `HPParking.Api`, thực thi chính sách Chặn xóa toàn diện (Universal Restrict Deletion — 409 Conflict), Bảo vệ trạng thái hoạt động (Active State Protection — 400 Bad Request), Khôi phục cha trước (Parent-First Restore — 400 Bad Request) và cơ chế làm giàu dữ liệu chi tiết cấu hình ngoại vi (`GET /api/v1/lanes/{id}/detail`), tuân thủ [ADR 0012](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0012-multi-gate-architecture-and-station-hardware-binding.md), [ADR 0030](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0030-entity-relationship-and-referential-integrity-policy.md) và [ADR 0031](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0031-recycle-bin-restore-and-universal-restrict-deletion.md).

## Ngữ cảnh

1. **Nhu cầu quản trị tập trung Cổng & Làn qua REST API**:
   - Trước đây, `Gate` và `Lane` đã được thiết kế mô hình thực thể tại `HPParking.Core` phục vụ WinForms theo ADR 0012, nhưng chưa có cụm REST API hoàn chỉnh để cấu hình từ Web Portal hoặc hệ thống quản trị trung tâm.
2. **Rủi ro đứt gãy toàn vẹn liên kết và thiết bị mồ côi**:
   - Một cổng kiểm soát vật lý chứa nhiều làn xe. Nếu xóa Cổng mà không kiểm tra, toàn bộ các Làn xe sẽ trở thành bản ghi mồ côi.
   - Khi vô hiệu hóa Cổng (`IsActive = false`), nếu các làn xe thuộc cổng đó vẫn đang hoạt động, hệ thống WinForms trạm bốt bảo vệ sẽ gặp xung đột trạng thái vận hành.
   - Làn xe phụ thuộc trực tiếp vào 4 thiết bị vật lý: Camera biển số (`PlateCameraDeviceId`), Camera toàn cảnh (`OverviewCameraDeviceId`), Bộ điều khiển Barie (`ControllerDeviceId`) và Đầu đọc FaceID (`FaceDeviceId`). Nếu các thiết bị này đã bị xóa mềm hoặc đang bị vô hiệu hóa, việc gắn vào làn sẽ khiến bốt bảo vệ lỗi kết nối ngoại vi.
3. **Nhu cầu xem cấu hình thiết bị tóm tắt**:
   - Kỹ thuật viên vận hành cần xem nhanh chi tiết làn kèm thông số IP, trạng thái, chủng loại của 4 thiết bị ngoại vi và thông tin cổng liên kết trong một lượt gọi API duy nhất thay vì phải gửi 5-6 request riêng lẻ.

## Quyết định

1. **Chuẩn Hóa Cụm REST API Cổng (`/api/v1/gates`) & Làn Xe (`/api/v1/lanes`)**:
   - Cung cấp đầy đủ các thao tác CRUD, tìm kiếm phân trang có lọc trạng thái/từ khóa, hỗ trợ tham số `onlyDeleted=true` cho thùng rác và endpoint `/restore`.
   - Phân quyền: `Viewer,Manager,Admin` cho các tác vụ tra cứu (GET); `Manager,Admin` cho các tác vụ ghi (POST, PUT, DELETE, RESTORE).

2. **Chính Sách Chặn Xóa Tuyệt Đối (Universal Restrict Deletion Policy — 409 Conflict)**:
   - **Xóa Cổng**: Bắt buộc kiểm tra collection `Lane`. Nếu tồn tại bất kỳ làn nào chưa bị xóa (`l.GateId == id && !l.IsDeleted`), lập tức ném `ConflictException` (`409 Conflict` - `GATE_HAS_LANES`).
   - **Xóa Thiết bị**: Tiếp tục duy trì kiểm tra `DEVICE_IN_USE_BY_LANE` theo ADR 0030 khi có làn đang sử dụng bất kỳ cổng kết nối nào của thiết bị đó.

3. **Bảo Vệ Trạng Thái Hoạt Động (Active State Protection — 400 Bad Request)**:
   - **Tắt Cổng**: Khi cập nhật Cổng từ hoạt động sang ngưng hoạt động (`IsActive = false`), kiểm tra nếu còn bất kỳ làn nào đang active (`l.GateId == id && l.IsActive && !l.IsDeleted`) -> Từ chối và ném `BadRequestException` (`400 Bad Request` - `INFRA_ACTIVE_DEPENDENCY_EXISTS`).
   - **Tắt Thiết bị**: Tiếp tục duy trì kiểm tra theo ADR 0030, chặn tắt nếu còn làn xe đang hoạt động sử dụng thiết bị.

4. **Khôi Phục Cha Trước - Con Sau (Strict Parent-First Restore — 400 Bad Request)**:
   - **Khôi phục Cổng**: Kiểm tra Công ty cha (`CompanyId`). Nếu công ty cha không tồn tại hoặc đã bị xóa mềm (`company.IsDeleted == true`) -> Chặn khôi phục và trả về `400 Bad Request` (`PARENT_IS_DELETED`). Tái kiểm tra trùng mã `Code` trong các cổng đang hoạt động (`409 Conflict`).
   - **Khôi phục Làn**: Kiểm tra Cổng cha (`GateId`). Nếu cổng cha đã bị xóa mềm -> Chặn khôi phục với mã lỗi `PARENT_IS_DELETED`. Đồng thời kiểm tra 4 thiết bị ngoại vi liên kết không được ở trạng thái đã xóa. Tái xác thực trùng mã `Code` của làn (`409 Conflict` - `LANE_CODE_DUPLICATE`).

5. **Xác Thực Ràng Buộc Phần Cứng (Hardware Binding Validation)**:
   - Khi tạo hoặc cập nhật Làn xe:
     * Cổng cha `GateId` phải tồn tại, `!gate.IsDeleted` và `gate.IsActive == true`.
     * Đối với từng thiết bị trong 4 thiết bị (`PlateCameraDeviceId`, `OverviewCameraDeviceId`, `ControllerDeviceId`, `FaceDeviceId`), nếu có truyền ID, hệ thống bắt buộc kiểm tra thiết bị tồn tại trong collection `Device`, `!device.IsDeleted` và `device.IsActive == true`. Nếu vi phạm, từ chối với `400 Bad Request`.
     * Chỉ số Relay và Reader: `OutputRelay >= 0`, `InputReader >= 0`.
     * Hướng di chuyển: Tuân thủ enum `LaneDirection` (`In = 1, Out = 2, Bidirectional = 3`).

6. **Endpoint Chi Tiết Làm Giàu Dữ Liệu (`GET /api/v1/lanes/{id}/detail`)**:
   - Trả về `LaneDetailDto` kế thừa `LaneDto`.
   - Nạp thông tin tóm tắt của Cổng (`GateSummaryDto`: `Id`, `Code`, `Name`, `CompanyId`, `MachineCode`, `IsActive`).
   - Gom toàn bộ ID của 4 thiết bị ngoại vi để truy vấn in-batch `Builders<Device>.Filter.In(d => d.Id, deviceIds)` trong một lượt truy vấn DB, trả về thông tin tóm tắt `DeviceSummaryDto` (`Id`, `Code`, `Name`, `Type`, `IpAddress`, `Port`, `IsActive`).

## Hệ quả

- **Tích cực**:
  - Đảm bảo tính nhất quán 100% với hệ thống quy chuẩn toàn vẹn tham chiếu của dự án (ADR 0030, 0031, 0033).
  - Ngăn chặn hoàn toàn việc cấu hình các thiết bị phần cứng không hợp lệ hoặc đã hỏng/xóa vào làn kiểm soát xe.
  - Tối ưu hóa hiệu năng nạp cấu hình trạm nhờ batch query trong endpoint `/detail`.
- **Đánh đổi**:
  - Khi xóa hoặc ngừng hoạt động một Cổng/Thiết bị, quản trị viên phải xử lý các Làn phụ thuộc trước (chuẩn hóa quy trình vận hành, loại trừ sai sót vô tình xóa nhầm cấu hình đang chạy).
