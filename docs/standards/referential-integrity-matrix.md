# Referential Integrity & Restrict Deletion Matrix

Tài liệu chuẩn mực kỹ thuật định nghĩa toàn diện ma trận quan hệ cha - con (Parent-Child Relationships), chính sách Chặn xóa toàn diện (Universal Restrict Deletion — 409 Conflict), bảo vệ trạng thái hoạt động (Active State Protection — 400 Bad Request), quy tắc khôi phục dữ liệu (Parent-First Restore — 400 Bad Request) và tính bất biến của dữ liệu sổ cái (Immutable Ledgers) áp dụng thống nhất cho toàn bộ 11 thực thể do phân hệ Web API (`HPParking.Api` kết nối MongoDB) quản lý theo [ADR 0030](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0030-entity-relationship-and-referential-integrity-policy.md), [ADR 0031](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0031-recycle-bin-restore-and-universal-restrict-deletion.md), [ADR 0033](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0033-contractor-management-and-referential-integrity.md), [ADR 0034](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0034-gate-and-lane-management-with-hardware-binding.md) và [ADR 0043](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0043-parking-fee-calculation-and-tariff.md).

> [!NOTE]
> **Phạm vi Bản quyền (`LicenseInfo`)**:
> Thực thể `LicenseInfo` thuộc cơ chế cấp phép phần mềm ngoại tuyến (Offline Node-Locking qua Windows Registry / RSA theo ADR 0005) được xử lý độc lập tại máy trạm WinForms (`HPParking`). Phân hệ Web API **hoàn toàn không quản lý `LicenseInfo`** và không có API can thiệp vào vòng đời bản quyền. Trường `Gate.MachineCode` trên API chỉ lưu chuỗi định danh trạm máy tính phục vụ cơ chế tự động nhận diện Zero-Touch Hardware Binding (ADR 0012).

---

## 1. Ma Trận Quan Hệ Toàn Bộ 11 Thực Thể Trên Tầng API

### 1.1. Nhóm Thực Thể Tổ Chức & Hạ Tầng (Organization & Infrastructure)

| Thực thể (Entity) | Vai trò trong Quan hệ | Thực thể Liên quan / Phụ thuộc | Khóa Ngoại / Liên kết | Mã Lỗi Chặn Xóa (`409 Conflict`) | Mã Lỗi Chặn Tắt Hoạt Động (`400 Bad Request`) | Điều kiện Khôi phục (`Restore`) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`Company`** (Công ty) | Thực thể Cha Gốc (Root) | **`Department`**<br>**`Gate`**<br>**`Client`** | `Department.CompanyId`<br>`Gate.CompanyId`<br>`Client.CompanyId` | `COMPANY_HAS_DEPARTMENTS`<br>`COMPANY_HAS_GATES`<br>`COMPANY_HAS_CLIENTS` | `COMPANY_ACTIVE_DEPENDENCY_EXISTS`<br>(khi còn phòng ban, cổng hoặc khách hàng active) | Tái xác thực duy nhất `Code`. |
| **`Department`** (Phòng ban) | Con của `Company`, Cha của `Client` | **`Company`** (Cha)<br>**`Client`** (Con) | `Department.CompanyId`<br>`Client.DepartmentId` | `DEPARTMENT_HAS_CLIENTS` | `DEPARTMENT_ACTIVE_CLIENTS_EXIST`<br>(khi còn khách hàng active) | `PARENT_IS_DELETED` nếu Company cha bị xóa.<br>Tái xác thực duy nhất `(Code, CompanyId)`. |
| **`Contractor`** (Nhà thầu) | Thực thể Độc lập, Cha của `Client` | **`Client`** (Con - nhân sự nhà thầu) | `Client.ContractorId` | `CONTRACTOR_HAS_CLIENTS` | `CONTRACTOR_ACTIVE_CLIENTS_EXIST`<br>(khi còn nhân sự active) | Tái xác thực duy nhất `Code` và `TaxCode`. |
| **`Gate`** (Cổng kiểm soát) | Con của `Company`, Cha của `Lane` | **`Company`** (Cha)<br>**`Lane`** (Con) | `Gate.CompanyId`<br>`Lane.GateId` | `GATE_HAS_LANES` | `INFRA_ACTIVE_DEPENDENCY_EXISTS`<br>(khi còn làn xe active) | `PARENT_IS_DELETED` nếu Company cha bị xóa.<br>Tái xác thực duy nhất `Code`, `MachineCode`. |
| **`Lane`** (Làn xe) | Con của `Gate`, Liên kết 4 `Device` | **`Gate`** (Cha)<br>**`Device`** (Thiết bị liên kết) | `Lane.GateId`<br>`Lane.OverviewCameraDeviceId`<br>`Lane.PlateCameraDeviceId`<br>`Lane.ControllerDeviceId`<br>`Lane.FaceDeviceId` | `LANE_IN_ACTIVE_OPERATION`<br>(khi bốt trạm đang trực tiếp mở ca) | Vô hiệu hóa làn sẽ tạm dừng điều khiển barrier và nhận diện camera tại làn đó. | `PARENT_IS_DELETED` nếu Gate cha bị xóa.<br>`DEVICE_IS_DELETED` nếu 1 trong 4 thiết bị bị xóa.<br>Tái xác thực duy nhất `Code` trong Gate. |
| **`Device`** (Thiết bị) | Thiết bị gán vào `Lane` | **`Lane`** (Làn xe sử dụng) | Được trỏ bởi 4 trường DeviceId trong `Lane` | `DEVICE_IN_USE_BY_LANE` | `INFRA_ACTIVE_DEPENDENCY_EXISTS`<br>(khi thiết bị đang gắn vào làn xe active) | Tái xác thực duy nhất `Code` và cặp `(IpAddress, Port)`. |

---

### 1.2. Nhóm Thực Thể Định Danh & Phương Tiện (Identity & Vehicles)

| Thực thể (Entity) | Vai trò trong Quan hệ | Thực thể Liên quan / Phụ thuộc | Khóa Ngoại / Liên kết | Mã Lỗi Chặn Xóa (`409 Conflict`) | Mã Lỗi Chặn Tắt Hoạt Động (`400 Bad Request`) | Điều kiện Khôi phục (`Restore`) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`Client`** (Khách hàng / Nhân sự) | Con của `Company`/`Dept`/`Contractor`, Cha của `Vehicle` & `ParkingSession`, Liên kết `Card` | **`Vehicle`** (Phương tiện)<br>**`ParkingSession`** (Phiên xe)<br>**`Card`** (Thẻ định danh) | `Vehicle.OwnerClientId`<br>`ParkingSession.PersonId`<br>`Card.ClientId`<br>`Client.CardCode` | `CLIENT_HAS_VEHICLES`<br>`CLIENT_HAS_ACTIVE_PARKING_SESSION`<br>(khi phương tiện của khách đang đỗ trong bãi)<br>`CLIENT_HAS_PENDING_APPROVAL_REQUEST`<br>*(Lưu ý Thẻ: Tự động gỡ liên kết thẻ về kho `Available`, xóa `CardCode` và thu hồi FaceID)* | `CLIENT_HAS_ACTIVE_PARKING_SESSION`<br>*(Vô hiệu hóa khách hàng sẽ tự động chặn quyền vào/ra của các xe liên kết)* | `PARENT_IS_DELETED` nếu Công ty/Phòng ban/Nhà thầu cha bị xóa.<br>Tái xác thực duy nhất `PhoneNumber` và `Code` (CCCD). |
| **`Card`** (Thẻ định danh RFID) | Con của `Tariff` (nếu tính phí), Liên kết `Client` (`Monthly`, `VIP`) hoặc `Vehicle` (nội bộ), Liên kết `ParkingSession` (khi đang trong bãi), Đồng bộ Thiết bị `FaceID` | **`Client`** (Chủ thẻ tháng / VIP)<br>**`Vehicle`** (Xe nội bộ gán thẻ)<br>**`Tariff`** (Biểu phí áp dụng)<br>**`ParkingSession`** (Phiên xe đang gửi)<br>**`Device`** (Máy FaceID vật lý) | `Card.ClientId`<br>`Card.VehicleId`<br>`Card.TariffId`<br>`Client.CardCode`<br>`ParkingSession.CardId` | `CARD_HAS_ACTIVE_PARKING_SESSION`<br>(khi thẻ đang có phiên đỗ xe `Active` trong bãi — chặn xóa, đổi gán, sửa biểu phí/thu phí hoặc về `Available`)<br>`CARD_ALREADY_ASSIGNED_TO_OTHER_CLIENT`<br>(Chặn chuyển chủ thẻ trực tiếp giữa 2 nhân sự)<br>*(Lưu ý Xóa: Tự động gỡ liên kết khỏi Client/Vehicle và thu hồi FaceID)* | `CARD_HAS_ACTIVE_PARKING_SESSION`<br>(Chỉ chặn chuyển về `Available` hoặc đổi biểu phí khi đang có xe trong bãi. **Cho phép** đổi sang `Locked`/`Lost` để kịp thời khóa xe bảo vệ an ninh).<br>Khi chuyển `Locked`/`Lost`: tự động đẩy `userType = "blackList"` lên FaceID.<br>Khi chuyển `InUse`: đẩy `normal`. | Tái xác thực duy nhất `CardNumber` (chuẩn hóa 10 chữ số). |
| **`Tariff`** (Biểu phí đỗ xe) | Thực thể Độc lập, Cha của `Card` | **`Card`** (Thẻ áp dụng biểu phí) | `Card.TariffId` | `TARIFF_IN_USE_BY_CARDS`<br>(khi còn thẻ liên kết biểu phí này — `409 Conflict`) | `TARIFF_ACTIVE_CARDS_EXIST`<br>(khi còn thẻ đang hoạt động liên kết biểu phí này — `400 Bad Request`) | Tái xác thực duy nhất `Code`. |
| **`Vehicle`** (Phương tiện) | Con của `Client`, Liên kết `ParkingSession` | **`Client`** (Chủ sở hữu)<br>**`ParkingSession`** (Phiên xe) | `Vehicle.OwnerClientId`<br>`ParkingSession.PlateNumber` | `VEHICLE_HAS_ACTIVE_PARKING_SESSION`<br>(khi xe đang có phiên gửi trong bãi `Active`) | `VEHICLE_HAS_ACTIVE_PARKING_SESSION`<br>(không cho tắt xe khi đang đỗ trong bãi) | `PARENT_IS_DELETED` nếu Client chủ sở hữu bị xóa.<br>Tái xác thực duy nhất `PlateNumber` chuẩn hóa. |
| **`User`** (Tài khoản nội bộ) | Thực thể Quản trị & Xác thực | **`AuditLog`** (Vết kiểm toán thao tác) | `AuditLog.ActorId`<br>`BaseEntity.CreatedBy`/`UpdatedBy` | `USER_CANNOT_DELETE_SELF`<br>`CANNOT_DELETE_LAST_ADMIN`<br>`USER_HAS_AUDIT_LOGS` (chặn xóa cứng khi đã có log) | `CANNOT_DEACTIVATE_LAST_ADMIN`<br>`USER_CANNOT_DEACTIVATE_SELF` | Tái xác thực duy nhất `Username` và `Email`. |

---

### 1.3. Nhóm Thực Thể Sổ Cái Bất Biến (Append-Only Immutable Ledgers)

| Thực thể (Entity) | Vai trò trong Quan hệ | Thực thể Tham Chiếu Tới | Đặc Điểm Bất Biến & Chính Sách Xóa | Quyền Hạn Truy Cập |
| :--- | :--- | :--- | :--- | :--- |
| **`ParkingSession`** (Phiên đỗ xe) | Sổ cái Giao dịch Lượt xe Vào/Ra bãi | **`Client`** (qua `PersonId`)<br>**`Vehicle`** (qua `PlateNumber`)<br>**`Lane`** (qua `InLaneName`/`OutLaneName`) | **SỔ CÁI GIAO DỊCH — CẤM DELETE/PUT TỰ DO**.<br>Bản ghi được tạo và cập nhật tự động bởi trạm kiểm soát bốt bảo vệ (`ParkingWorkflowService`). Dữ liệu mang giá trị pháp lý, tài chính và đối soát an ninh.<br>**Ngoại lệ Quản trị Duy nhất (`Administrative Force-Close Exception`)**: Cho phép duy nhất endpoint `POST /api/v1/parking-sessions/{id}/force-close` cho vai trò `Manager` và `Admin` để giải phóng phiên bị treo khi mất thẻ (`LostCard`) hoặc xử lý sự cố (`AdminForceClose`). Mọi can thiệp tài chính (`OverrideFeeAmount`) bắt buộc lưu vết kiểm toán bất biến sang `AuditLog`. | `Viewer` (Chỉ đọc: Read-Only);<br>`Manager`, `Admin` (Chỉ đọc + Quyền duy nhất gọi `force-close`);<br>`X-API-KEY` (Chỉ đọc) |
| **`AuditLog`** (Nhật ký kiểm toán) | Sổ cái Nhật ký An ninh Hệ thống | **`User`** (qua `ActorId`)<br>**Mọi Entity** (qua `TargetEntity`, `TargetId`) | **TUYỆT ĐỐI BẤT BIẾN (Append-Only Immutable)**.<br>Không có cơ chế Xóa mềm hay Xóa cứng qua API (`AUDIT_LOG_IMMUTABLE`). Không có cơ chế cập nhật hay khôi phục. Dữ liệu lưu trữ vĩnh viễn phục vụ điều tra an toàn thông tin và kiểm toán. | **CHỈ DUY NHẤT `Admin`** (hoặc `X-API-KEY` hệ thống). Chặn `403 Forbidden` với `Manager`/`Viewer`.<br>(Chỉ đọc: Read-Only) |

---

## 2. Bảy Quy Tắc Toàn Vẹn Tham Chiếu Bắt Buộc (Mandatory Integrity Rules)

### Quy tắc 1: Chặn Xóa Tuyệt Đối (Universal Restrict Deletion — 100% No Cascade)
- **Tuyệt đối không sử dụng Cascade Delete** cho bất kỳ quan hệ nào trong hệ thống.
- Khi nhận yêu cầu xóa (dù là Xóa mềm hay Xóa cứng), tầng Service của thực thể cha bắt buộc phải truy vấn toàn bộ các collection con có liên kết (với điều kiện `!IsDeleted`).
- Nếu số lượng bản ghi con `> 0`, lập tức ném `ConflictException` (HTTP 409 Conflict) kèm thông điệp hướng dẫn rõ ràng cho người dùng.
- **Ngoại lệ liên kết Thẻ (`Card`) khi xóa Nhân sự (`Client`)**: Khi xóa nhân sự, thẻ định danh không bị xóa mà được tự động gỡ liên kết hoàn trả về kho khả dụng (`CardStatus.Available`, `ClientId = null`), làm rỗng `client.CardCode` để tránh xung đột mã thẻ trong thùng rác, và gửi lệnh thu hồi quyền FaceID trên các thiết bị vật lý. Các ràng buộc phương tiện (`CLIENT_HAS_VEHICLES`), phiên đỗ xe (`CLIENT_HAS_ACTIVE_PARKING_SESSION`) và phê duyệt email (`CLIENT_HAS_PENDING_APPROVAL_REQUEST`) vẫn áp dụng chính sách chặn xóa tuyệt đối (409 Conflict).

### Quy tắc 2: Bảo Vệ Trạng Thái Hoạt Động (Active State Protection)
- Khi cập nhật `IsActive = false` cho bất kỳ thực thể cha nào:
  - Tầng Service phải kiểm tra nếu còn bất kỳ thực thể con nào đang hoạt động (`IsActive == true && !IsDeleted`).
  - Nếu còn, từ chối cập nhật và ném `BadRequestException` (HTTP 400 Bad Request) để ngăn chặn việc ngắt kết nối phần cứng hoặc đình chỉ công ty trong khi các làn xe/nhân sự vẫn đang vận hành.

### Quy tắc 3: Khôi Phục Cha Trước - Con Sau (Strict Parent-First Restore)
- Khi gọi `POST /restore` cho một thực thể con (ví dụ: `Department`, `Client`, `Vehicle`, `Lane`):
  - Bắt buộc kiểm tra thực thể cha tương ứng: Thực thể cha phải tồn tại trong cơ sở dữ liệu và đang ở trạng thái hoạt động bình thường (`!IsDeleted`).
  - Nếu thực thể cha đang nằm trong thùng rác hoặc không tồn tại, từ chối khôi phục và trả về `400 Bad Request` (`PARENT_IS_DELETED`).

### Quy tắc 4: Tái Xác Thực Khóa Duy Nhất Khi Khôi Phục (Re-validation on Restore)
- Trước khi khôi phục bản ghi về `IsDeleted = false`, bắt buộc kiểm tra các trường khóa duy nhất (`Code`, `PhoneNumber`, `PlateNumber`, `IpAddress:Port`, `Username`, `TaxCode`) trong số các bản ghi đang hoạt động.
- Nếu bị trùng lặp với bản ghi đã đăng ký mới trong thời gian bản ghi cũ nằm trong thùng rác, trả về `409 Conflict`.

### Quy tắc 5: Tính Bất Biến Của Sổ Cái Giao Dịch & Kiểm Toán (Immutable Ledgers Policy)
- Hai thực thể `AuditLog` và `ParkingSession` là dữ liệu bất biến và sổ cái tài chính:
  - **`AuditLog`**: Tuyệt đối bất biến 100% (Append-Only), không có bất kỳ ngoại lệ nào. Không cung cấp API thêm, sửa, xóa thủ công.
  - **`ParkingSession`**: Không cho phép tạo, sửa, xóa tự do (`POST`, `PUT`, `DELETE` CRUD thông thường bị cấm tuyệt đối). Mọi bản ghi thông thường đều do máy trạm bốt bảo vệ (`ParkingWorkflowService`) quản lý vòng đời tự động.
  - **Ngoại lệ Quản trị Sự cố Duy nhất (`Administrative Force-Close Exception`)**:
    - Để giải phóng các phiên bị treo khi khách làm mất thẻ (`LostCard`) hoặc gặp sự cố kỹ thuật tại cổng (`AdminForceClose`) — tránh gây khóa vĩnh viễn thẻ và phương tiện theo Quy tắc 7 & 9 — hệ thống cho phép duy nhất thao tác: `POST /api/v1/parking-sessions/{id}/force-close`.
    - **Ràng buộc phân quyền RBAC**: Chỉ tài khoản có vai trò `Manager` hoặc `Admin` mới có quyền thực thi (`[Authorize(Roles = "Manager,Admin")]`).
    - **Cơ chế kiểm soát bù đắp an ninh (Compensating Controls)**:
      1. **Bảo toàn tính nguyên tử**: Sử dụng bộ lọc điều kiện `Status == Active && !IsDeleted` để ngăn chặn cập nhật xung đột (Race Condition) với làn ra.
      2. **Dấu vết kiểm toán bất biến**: Bắt buộc ghi nhận sự kiện vào `AuditLog` với đầy đủ thông tin: Người thực hiện (`ClosedBy`), Lý do (`CloseReason`), Phí hệ thống tính toán ban đầu, Phí ghi đè thực tế (`OverrideFeeAmount` nếu có), Cờ thu tiền (`IsFeeCollected`) và Ghi chú giải trình. Mọi hành vi can thiệp số tiền phí thủ công đều được lưu vết vĩnh viễn phục vụ đối soát, kiểm toán doanh thu.

### Quy tắc 6: Bảo Vệ Tài Khoản Người Dùng & Quản Trị Viên (Admin & Self-Protection)
- Không cho phép người dùng tự xóa tài khoản của chính mình (`USER_CANNOT_DELETE_SELF`).
- Không cho phép xóa hoặc vô hiệu hóa tài khoản `Admin` duy nhất còn lại trong hệ thống (`CANNOT_DELETE_LAST_ADMIN`, `CANNOT_DEACTIVATE_LAST_ADMIN`).
- Người dùng đã từng có phát sinh hành động trong `AuditLog` chỉ được phép Xóa mềm, tuyệt đối không được Xóa cứng nhằm bảo toàn dấu vết kiểm toán.

### Quy tắc 7: Ràng Buộc Khóa Khi Phương Tiện Đang Gửi (Active Parking Session Lockout)
- Không cho phép xóa hoặc vô hiệu hóa `Client` hoặc `Vehicle` nếu phương tiện đang có một phiên gửi xe chưa kết thúc (`ParkingSession.Status == ParkingSessionStatus.Active && !ParkingSession.IsDeleted`).
- Tránh tình trạng xe đang đỗ trong bãi nhưng hồ sơ chủ xe/biển số bị xóa, dẫn đến sự cố chặn cứng tại làn ra mà không đối soát được dữ liệu vào.

### Quy tắc 8: Bất Biến Sở Hữu Thẻ & Đồng Bộ Phần Cứng FaceID (Strict Card Ownership & Hardware Sync)
- **Chặn chuyển thẻ trực tiếp giữa 2 nhân sự**: Một thẻ đang gán cho Client A tuyệt đối không được chuyển trực tiếp sang Client B (`CARD_ALREADY_ASSIGNED_TO_OTHER_CLIENT` — 409 Conflict). Thẻ bắt buộc phải được thu hồi về kho (`ClientId = null`, `Status = Available`) trước khi gán cho nhân sự khác.
- **Tự động thu hồi thẻ cũ của nhân sự**: Khi một thẻ từ kho được gán cho Client B, nếu Client B đã có một thẻ cũ trước đó, thẻ cũ sẽ tự động được thu hồi về kho khả dụng (`Available`).
- **Đồng bộ phần cứng FaceID hai chiều**:
  - Khi gán thẻ mới cho Client có phương thức `FaceId`: Tự động gọi `CleanAndAssignCardAsync` quét sạch mã thẻ cũ và nạp thẻ mới lên toàn bộ các thiết bị FaceID active.
  - Khi thẻ đang gán bị đổi trạng thái sang `Locked` (Tạm khóa) hoặc `Lost` (Báo mất): Tự động gọi ISAPI `PUT /ISAPI/AccessControl/UserInfo/Modify` đổi `userType = "blackList"` trên các máy FaceID active nhằm lập tức khóa quyền ra vào và kích hoạt chuông báo động tại cổng.
  - Khi thẻ được mở lại `InUse`: Tự động gọi ISAPI khôi phục `userType = "normal"`.
  - Khi gỡ thẻ khỏi Client (`ClientId = null`) hoặc xóa thẻ: Tự động gọi `DeleteUserAsync` thu hồi quyền vật lý trên máy FaceID do thiết bị FaceID bắt buộc phải có mã thẻ định danh.

### Quy tắc 9: Ràng Buộc Toàn Vẹn Biểu Phí & Thẻ Trong Phiên Đỗ (Tariff & Active Card Session Integrity)
- **Chặn xóa biểu phí (`Tariff`)**: Khi nhận yêu cầu xóa biểu phí (soft delete), tầng Service bắt buộc kiểm tra xem có thẻ nào đang liên kết (`Card.TariffId == id && !Card.IsDeleted`). Nếu có, ném `ConflictException` (`TARIFF_IN_USE_BY_CARDS` — 409 Conflict).
- **Chặn ngừng kích hoạt biểu phí (`IsActive = false`)**: Không cho phép tắt kích hoạt biểu phí nếu còn bất kỳ thẻ nào đang hoạt động liên kết với nó (`Card.TariffId == id && Card.IsChargeable && !Card.IsDeleted`), ném `BadRequestException` (`TARIFF_ACTIVE_CARDS_EXIST` — 400 Bad Request, áp dụng chuẩn mã HTTP 400 cho bảo vệ trạng thái hoạt động theo Quy tắc 2).
- **Chặn xóa hoặc sửa thông tin thẻ (`Card`) có phiên đỗ Active**: Khi thẻ đang có phiên gửi xe trong bãi (`ParkingSession.CardId == card.Id && ParkingSession.Status == ParkingSessionStatus.Active`), cấm xóa thẻ, cấm thay đổi đối tượng gán (`ClientId`, `VehicleId`), loại thẻ (`TargetType`), loại xe (`VehicleType`), cấu hình biểu phí (`TariffId`, `IsChargeable`), hoặc chuyển trạng thái về `Available` (`CARD_HAS_ACTIVE_PARKING_SESSION` — 409 Conflict). Ràng buộc này được thực thi đồng nhất trên cả `CardService` và `CardExcelService`.
  - **Ngoại lệ an ninh bắt buộc**: Cho phép chuyển trạng thái thẻ sang `Locked` (Tạm khóa) hoặc `Lost` (Báo mất) ngay cả khi xe đang trong bãi để người quản trị/bảo vệ kịp thời khóa xe không cho xuất bãi trái phép (hệ thống tự động kích hoạt đồng bộ `userType = "blackList"` lên các thiết bị FaceID).
  - **Ngoại lệ gia hạn thẻ tháng**: Cho phép gia hạn ngày kết thúc hiệu lực (`ValidTo`) cho thẻ tháng (chặn rút ngắn ngày hoặc sửa đổi ngày bắt đầu `ValidFrom`).

---

## 3. Quy Trình Bắt Buộc Khi Lập Trình & Review (Inverted FK Scan)

Mỗi khi AI Agent hoặc lập trình viên thực hiện viết mới hoặc chỉnh sửa hàm `Delete{Entity}Async` hoặc `Update{Entity}Async(IsActive = false)` cho bất kỳ thực thể nào trong 12 bảng:

1. **Bước 1 (Grep ngược khóa ngoại)**:
   Chạy lệnh tìm kiếm trên toàn bộ thư mục `HPParking.Core/Models/Entities/`:
   `grep -E "public string\??\s+([A-Za-z0-9_]*{Entity}Id)" HPParking.Core/Models/Entities/`
2. **Bước 2 (Đối soát với Ma trận 12 bảng)**:
   Tra cứu đối chiếu với Mục 1 của tài liệu này để kiểm tra xem thực thể đang xử lý có phụ thuộc vào bảng nào khác hay không (bao gồm cả quan hệ nghiệp vụ với `ParkingSession` hay `AuditLog`).
3. **Bước 3 (Kiểm chứng mã nguồn Service & Test)**:
   Kiểm tra xem hàm `Delete{Entity}Async` đã gọi kiểm tra trên **từng thực thể con phụ thuộc** hay chưa. Thiếu bất kỳ thực thể nào đều bị tính là **Review Fail**.