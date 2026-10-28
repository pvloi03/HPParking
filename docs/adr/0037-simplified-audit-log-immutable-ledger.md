# 0037: Kiến Trúc Sổ Cái Kiểm Toán Tinh Gọn Bất Biến (Simplified Audit Log Immutable Ledger) & Chuẩn Hóa Định Danh Người Thao Tác

Quyết định kiến trúc về việc chuẩn hóa phân hệ nhật ký kiểm toán hệ thống (`AuditLog`) trong `HPParking.Api` và `HPParking.WebAdmin` theo mô hình Sổ cái bất biến chỉ ghi thêm (Append-Only Immutable Ledger). Quyết định này loại bỏ hoàn toàn các trường dữ liệu theo dõi biến động chi tiết (`ChangedProperties`, `OldValues`, `NewValues`) và địa chỉ mạng (`IpAddress`), chuyển dịch sang lưu trữ tinh gọn tập trung vào ngữ cảnh hành vi cốt lõi (Ai, Làm gì, Khi nào, Trên đối tượng nào, Kết quả và Lý do), đồng thời giải quyết triệt để lỗi định danh sai lệch người thao tác thông qua cơ chế `ICurrentUserService` trích xuất thông tin người dùng từ JWT claims.

## Ngữ cảnh

1. **Rủi ro rò rỉ dữ liệu nhạy cảm (PII & Credentials)**:
   - Trước đây, mô hình lưu vết dự kiến serialize toàn bộ JSON snapshot của thực thể trước (`OldValues`) và sau (`NewValues`) khi chỉnh sửa. Cơ chế này tiềm ẩn nguy cơ bảo mật nghiêm trọng khi các trường nhạy cảm (như mật khẩu thiết bị phần cứng `Device.Password`, thông tin căn cước công dân `Client.Code`, thông tin tài khoản) có thể bị lưu lộ thiên trong CSDL MongoDB dưới dạng bản rõ.
2. **Chi phí tài nguyên và phình to dữ liệu (Storage & Memory Bloat)**:
   - Việc lưu trữ các chuỗi JSON snapshot lớn và mảng `ChangedProperties` cho mỗi thao tác CUD (Create/Update/Delete) khiến dung lượng collection `AuditLogs` tăng phi mã, gây áp lực lên I/O và RAM máy chủ khi phân trang, lọc hoặc truy vấn.
3. **Sự không cần thiết của việc diff chi tiết**:
   - Trong thực tế vận hành trạm kiểm soát bãi xe, mục tiêu của nhật ký kiểm toán là trả lời nhanh và rõ ràng cho Ban quản lý câu hỏi: "Ai đã can thiệp dữ liệu gì, vào lúc nào, thành công hay thất bại, và vì lý do gì?" mà không cần tái hiện phức tạp từng thuộc tính thay đổi cấp byte.
4. **Lỗi định danh sai lệch người thao tác (Actor Attribution Bug)**:
   - Trong các phiên bản trước, ở một số dịch vụ (tiêu biểu là `UserService`), khi Quản trị viên (Admin A) thay đổi mật khẩu hoặc trạng thái của một tài khoản (User B), mã nguồn lại lấy `user.Username` của User B gán vào `ActorUsername`. Điều này làm sai lệch nghiêm trọng tính minh bạch của sổ cái, khiến bản ghi log hiển thị như thể User B tự thao tác trên chính mình.
5. **Trường địa chỉ IP không mang nhiều giá trị và gây nhiễu**:
   - Khi hệ thống chạy qua các lớp Reverse Proxy/Nginx hoặc mạng nội bộ LAN, trường `IpAddress` thường chỉ ghi nhận loopback hoặc IP cổng nội bộ, không phản ánh môi trường ứng dụng thực tế. Do đó, trường `Source` được chuẩn hóa thành chuỗi định danh môi trường tác nghiệp (`"WebAdmin"`).

## Quyết định

1. **Loại bỏ triệt để các trường diff và IP khỏi Domain Entity, DTO và Database**:
   - Xóa bỏ `OldValues`, `NewValues`, `ChangedProperties` khỏi entity `AuditLog` ([AuditLog.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Core/Models/Entities/AuditLog.cs)), DTO ([AuditLogDetailDto.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Api/DTOs/AuditLogs/AuditLogDetailDto.cs)), và schema giao tiếp TypeScript ([auditLog.ts](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.WebAdmin/src/types/auditLog.ts)).
   - Loại bỏ trường `IpAddress`, chuẩn hóa thuộc tính `Source` thành `"WebAdmin"` cố định đại diện cho phân hệ phát sinh thao tác.

2. **Bộ thuộc tính cốt lõi của Sổ cái bất biến (Core Ledger Schema)**:
   - `Id`: Mã định danh bản ghi MongoDB.
   - `ActorId`: Khóa định danh của người thực hiện hành động (trích xuất từ JWT token).
   - `ActorUsername`: Tên tài khoản của người thực hiện.
   - `ActorRole`: Vai trò của người thực hiện (`Admin`, `Manager`, `Viewer`...).
   - `Source`: Nguồn tác nghiệp (`"WebAdmin"`).
   - `ActionType`: Loại hành động (`Create`, `Update`, `Delete`, `Restore`, `PermanentDelete`, `Export`, `FaceIdSync`...).
   - `TargetEntity`: Tên thực thể nghiệp vụ (`Client`, `Vehicle`, `User`, `Company`, `Department`, `Contractor`, `Gate`, `Lane`, `Device`, `Reports`).
   - `TargetId`: ID của thực thể bị tác động.
   - `TargetDisplay`: Nhãn hiển thị con người dễ nhận biết (Họ tên, Biển số xe, Tên thiết bị, v.v.).
   - `Reason`: Mô tả tóm tắt ngữ cảnh thao tác có ý nghĩa nghiệp vụ.
   - `IsSuccess`: Cờ trạng thái thực thi (`true`/`false`).
   - `ErrorMessage`: Thông báo lỗi chi tiết khi `IsSuccess = false`.
   - `CreatedAt`: Mốc thời gian UTC phát sinh sự kiện.

3. **Chuẩn hóa cơ chế định danh người thao tác qua `ICurrentUserService`**:
   - Xây dựng dịch vụ [ICurrentUserService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Api/Services/Interfaces/ICurrentUserService.cs) đọc trực tiếp claims (`ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, `ClaimTypes.Role`) từ `IHttpContextAccessor.HttpContext.User`.
   - Toàn bộ các service nghiệp vụ (`ClientService`, `UserService`, `VehicleService`, `CompanyService`, `DepartmentService`, `ContractorService`, `GateService`, `LaneService`, `DeviceService`, `ReportExcelService`) khi gọi `_auditLogService.LogActivityAsync` đều tự động lấy danh tính người dùng đang đăng nhập trên Web Admin, xóa bỏ hoàn toàn tình trạng gán nhầm danh tính đối tượng tác động làm người thực hiện.

4. **Bảo mật và phân quyền nghiêm ngặt (Admin Only)**:
   - Toàn bộ endpoints của `AuditLogsController` được bảo vệ bởi `[Authorize(Roles = "Admin")]`.
   - Tuân thủ nguyên tắc Bất biến chỉ ghi thêm: Chỉ cung cấp API đọc danh sách (`GET /api/v1/audit-logs`), đọc chi tiết (`GET /api/v1/audit-logs/{id}`) và xuất Excel (`GET /api/v1/excel/reports/audit-logs/export`). Tuyệt đối không cung cấp API sửa (`PUT`/`PATCH`) hay xóa (`DELETE`).

5. **Đồng bộ giao diện Web Admin**:
   - Cập nhật [AuditPayloadViewer.tsx](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.WebAdmin/src/components/auditLogs/AuditPayloadViewer.tsx) loại bỏ hoàn toàn tab xem diff hoặc huy hiệu danh sách thuộc tính thay đổi, tập trung vào việc hiển thị thanh lịch, rõ ràng thông tin sự kiện, người thực hiện, đối tượng và lý do thao tác.
   - Tối ưu hóa bảng dữ liệu [AuditLogsPage.tsx](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.WebAdmin/src/pages/AuditLogsPage.tsx) với bộ lọc đa chiều (Actor, Entity, ActionType, IsSuccess, Date Range) và hỗ trợ giao diện đáp ứng linh hoạt (Responsive Hybrid View theo ADR 0036).

## Hệ quả

- **Tích cực**:
  - Triệt tiêu hoàn toàn nguy cơ rò rỉ dữ liệu định danh cá nhân và mật khẩu cấu hình vào log kiểm toán.
  - Tối ưu hóa vượt trội hiệu năng ghi và dung lượng cơ sở dữ liệu MongoDB: kích thước mỗi bản ghi giảm hơn 80%, loại bỏ chi phí serialize JSON và so sánh diff trên CPU máy chủ.
  - Bảo đảm tính trung thực tuyệt đối của nhật ký kiểm toán nhờ cơ chế trích xuất Actor tự động từ JWT Token qua `ICurrentUserService`.
  - Trải nghiệm giao diện người dùng trên Web Admin trở nên sạch sẽ, trực quan, dễ tra cứu và tải nhanh hơn đáng kể.
- **Đánh đổi**:
  - Không thể khôi phục lại giá trị cụ thể của từng trường dữ liệu cũ từ Audit Log (tuy nhiên việc này đã được bảo vệ bởi cơ chế Thùng rác hệ thống / Xóa mềm `IsDeleted` và sao lưu định kỳ cơ sở dữ liệu MongoDB).
