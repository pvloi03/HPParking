# Referential Integrity & Restrict Deletion Matrix

Tài liệu chuẩn mực kỹ thuật định nghĩa toàn bộ ma trận quan hệ cha - con (Parent-Child Relationships), chính sách Chặn xóa toàn diện (Universal Restrict Deletion — 409 Conflict), bảo vệ trạng thái hoạt động (Active State Protection — 400 Bad Request) và quy tắc khôi phục dữ liệu (Parent-First Restore — 400 Bad Request) áp dụng thống nhất cho toàn bộ hệ thống `HPParking.Api` (MongoDB) theo [ADR 0030](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0030-entity-relationship-and-referential-integrity-policy.md), [ADR 0031](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0031-recycle-bin-restore-and-universal-restrict-deletion.md) và [ADR 0033](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0033-contractor-management-and-referential-integrity.md).

---

## 1. Ma Trận Quan Hệ Toàn Hệ Thống

| Thực thể Cha (Parent Entity) | Thực thể Con Phụ Thuộc (Child Entity) | Khóa Ngoại Trỏ Tới Cha | Mã Lỗi Chặn Xóa (`409 Conflict`) | Mã Lỗi Chặn Tắt Hoạt Động (`400 Bad Request`) |
| :--- | :--- | :--- | :--- | :--- |
| **`Company`** (Công ty) | **`Department`** (Phòng ban)<br>**`Gate`** (Cổng)<br>**`Client`** (Nhân sự/Khách hàng) | `Department.CompanyId`<br>`Gate.CompanyId`<br>`Client.CompanyId` | `COMPANY_HAS_DEPARTMENTS`<br>`COMPANY_HAS_GATES`<br>`COMPANY_HAS_CLIENTS` | `COMPANY_ACTIVE_DEPENDENCY_EXISTS`<br>(hoặc thông báo chi tiết khi còn phòng ban, cổng hoặc khách hàng active) |
| **`Department`** (Phòng ban) | **`Client`** (Nhân sự/Khách hàng) | `Client.DepartmentId` | `DEPARTMENT_HAS_CLIENTS` | `DEPARTMENT_ACTIVE_CLIENTS_EXIST` |
| **`Contractor`** (Nhà thầu) | **`Client`** (Nhân sự nhà thầu) | `Client.ContractorId` | `CONTRACTOR_HAS_CLIENTS` | `CONTRACTOR_ACTIVE_CLIENTS_EXIST` |
| **`Client`** (Khách hàng) | **`Vehicle`** (Phương tiện) | `Vehicle.OwnerClientId` | `CLIENT_HAS_VEHICLES` | *(Vô hiệu hóa khách hàng sẽ tự động chặn quyền vào/ra của xe)* |
| **`Device`** (Thiết bị) | **`Lane`** (Làn xe) | `Lane.OverviewCameraDeviceId`<br>`Lane.PlateCameraDeviceId`<br>`Lane.ControllerDeviceId`<br>`Lane.FaceDeviceId` | `DEVICE_IN_USE_BY_LANE` | `INFRA_ACTIVE_DEPENDENCY_EXISTS` |
| **`Gate`** (Cổng) | **`Lane`** (Làn xe) | `Lane.GateId` | `GATE_HAS_LANES` | `INFRA_ACTIVE_DEPENDENCY_EXISTS` |
| **`Vehicle`** (Phương tiện) | *(Thực thể lá)* | — | — | — |

---

## 2. Các Quy Tắc Bắt Buộc (Mandatory Rules)

### Quy tắc 1: Chặn Xóa Tuyệt Đối (Universal Restrict Deletion — 100% No Cascade)
- **Tuyệt đối không sử dụng Cascade Delete** cho bất kỳ quan hệ nào trong hệ thống.
- Khi nhận yêu cầu xóa (dù là Xóa mềm hay Xóa cứng), tầng Service của thực thể cha bắt buộc phải truy vấn toàn bộ các collection con có liên kết (với điều kiện `!IsDeleted`).
- Nếu số lượng bản ghi con `> 0`, lập tức ném `ConflictException` (HTTP 409 Conflict) kèm thông điệp hướng dẫn rõ ràng cho người dùng.

### Quy tắc 2: Bảo Vệ Trạng Thái Hoạt Động (Active State Protection)
- Khi cập nhật `IsActive = false` cho bất kỳ thực thể cha nào:
  - Tầng Service phải kiểm tra nếu còn bất kỳ thực thể con nào đang hoạt động (`IsActive == true && !IsDeleted`).
  - Nếu còn, từ chối cập nhật và ném `BadRequestException` (HTTP 400 Bad Request) để ngăn chặn việc ngắt kết nối phần cứng hoặc đình chỉ công ty trong khi các làn xe/nhân sự vẫn đang vận hành.

### Quy tắc 3: Khôi Phục Cha Trước - Con Sau (Strict Parent-First Restore)
- Khi gọi `POST /restore` cho một thực thể con (ví dụ: `Department`, `Client`, `Vehicle`, `Lane`):
  - Bắt buộc kiểm tra thực thể cha tương ứng: Thực thể cha phải tồn tại trong cơ sở dữ liệu và đang ở trạng thái hoạt động bình thường (`!IsDeleted`).
  - Nếu thực thể cha đang nằm trong thùng rác hoặc không tồn tại, từ chối khôi phục và trả về `400 Bad Request` (`PARENT_IS_DELETED`).

### Quy tắc 4: Tái Xác Thực Khóa Duy Nhất Khi Khôi Phục (Re-validation on Restore)
- Trước khi khôi phục bản ghi về `IsDeleted = false`, bắt buộc kiểm tra các trường khóa duy nhất (`Code`, `PhoneNumber`, `PlateNumber`, `IpAddress:Port`) trong số các bản ghi đang hoạt động.
- Nếu bị trùng lặp với bản ghi đã đăng ký mới trong thời gian bản ghi cũ nằm trong thùng rác, trả về `409 Conflict`.

---

## 3. Quy Trình Bắt Buộc Khi Lập Trình & Review (Inverted FK Scan)

Mỗi khi AI Agent hoặc lập trình viên thực hiện viết mới hoặc chỉnh sửa hàm `Delete{Entity}Async` hoặc `Update{Entity}Async(IsActive = false)`:

1. **Bước 1 (Grep ngược khóa ngoại)**:
   Chạy lệnh tìm kiếm trên toàn bộ thư mục `HPParking.Core/Models/Entities/`:
   `grep -E "public string\??\s+([A-Za-z0-9_]*{Entity}Id)" HPParking.Core/Models/Entities/`
2. **Bước 2 (Lập danh sách thực thể con)**:
   Ghi nhận tất cả các class và property có chứa khóa ngoại trỏ tới thực thể đang xử lý.
3. **Bước 3 (Đối soát mã nguồn Service)**:
   Kiểm tra xem hàm `Delete{Entity}Async` đã gọi `CountAsync` hoặc `FindAsync` trên **từng thực thể con** trong danh sách hay chưa. Thiếu bất kỳ thực thể nào đều bị tính là **Review Fail**.
