# 0042: Cơ Chế Phê Duyệt Kích Hoạt Nhân Sự Qua Email Phòng Ban (Departmental Email Approval for Client Activation)

> **Trạng thái:** Chấp thuận (Accepted)  
> **Liên quan (Related):** [ADR 0030](0030-entity-relationship-and-referential-integrity-policy.md), [ADR 0037](0037-simplified-audit-log-immutable-ledger.md), [ADR 0041](0041-client-vehicle-workflow-handler-and-rich-domain-model.md)

Quyết định kiến trúc về việc chuyển đổi cơ chế kích hoạt hồ sơ nhân sự (`Client.IsActive`) từ thao tác bật/tắt thủ công trực tiếp trên giao diện sang quy trình phê duyệt bắt buộc thông qua liên kết xác nhận 1-click gửi đến email phòng ban phê duyệt (`Approving Department`).

---

## Ngữ cảnh

1. **Rủi ro kiểm soát ra vào khi kích hoạt thủ công (Manual Activation Risk)**:
   - Trước đây, thuộc tính `Client.IsActive` được thiết lập trực tiếp thông qua công tắc bật/tắt thủ công trên form đăng ký hoặc cập nhật hồ sơ bởi bất kỳ tài khoản quản trị nào có quyền ghi.
   - Thao tác này tiềm ẩn rủi ro nhân sự chưa được phòng ban chuyên trách (như Phòng Nhân sự hoặc Phòng Hành chính) thẩm định tính hợp lệ của hợp đồng, phương tiện nhưng đã có thể ra vào tự do qua hệ thống barie bãi xe.
2. **Thiếu dấu vết phê duyệt độc lập (Lack of Departmental Audit Trail)**:
   - Các quyết định khóa thẻ (vô hiệu hóa khi nghỉ việc/vi phạm) hoặc mở lại quyền gửi xe chỉ được ghi nhận dưới dạng cập nhật thông thường của tài khoản quản trị viên kỹ thuật, không thể hiện được trách nhiệm của phòng ban quản lý nhân sự trực tiếp.
3. **Cần trải nghiệm phê duyệt tinh gọn, tức thì (Frictionless Approval)**:
   - Nếu áp dụng quy trình đăng nhập phức tạp vào phần mềm quản trị bãi xe, cán bộ phòng nhân sự phải có tài khoản riêng và thao tác nhiều bước. Do đó, cần cơ chế phê duyệt 1-click trực tiếp từ email thông báo với độ bảo mật cao và thời hạn kiểm soát rõ ràng.

---

## Quyết định

### 1. Bỏ Kích Hoạt Thủ Công & Thiết Lập Phòng Ban Phê Duyệt
1. **Loại bỏ công tắc `IsActive` thủ công**:
   - Trong form Thêm mới và Cập nhật hồ sơ nhân sự, công tắc kích hoạt thủ công bị loại bỏ hoàn toàn.
   - Mọi hồ sơ nhân sự mới tạo lập luôn khởi tạo ở trạng thái Chưa kích hoạt (`IsActive = false`).
2. **Chọn Phòng ban phê duyệt (`Approving Department`)**:
   - Ở cuối giao diện Thêm mới nhân sự, bổ sung ô chọn phòng ban phê duyệt bắt buộc.
   - Hệ thống chỉ lọc danh sách các phòng ban đã cấu hình email hợp lệ và đang hoạt động.
   - Tự động nhận diện và gán mặc định phòng ban có tên chứa từ khóa `"nhân sự"` hoặc `"hr"` (không phân biệt hoa thường, không dấu).
   - Phòng ban phê duyệt nhận email này hoàn toàn độc lập với phòng ban công tác trực thuộc của nhân sự.

### 2. Thực Thể Phê Duyệt Chuyên Trách `ClientApprovalRequest`
Tạo thực thể mới kế thừa `BaseEntity` lưu trữ trong collection `client_approval_requests` để theo dõi vòng đời phê duyệt:
- `ClientId`: Mã định danh nhân sự mục tiêu.
- `ApprovingDepartmentId` & `ApprovingDepartmentEmail`: Phòng ban và email tiếp nhận.
- `ActionType`: Loại hành động (`Activate` - Kích hoạt hoặc `Deactivate` - Khóa).
- `Token`: Chuỗi token ngẫu nhiên mã hóa an toàn (Crypto Random Token), duy nhất và được đánh chỉ mục (Index).
- `ExpiresAt`: Thời điểm hết hạn token, cố định **24 giờ** tính từ thời điểm khởi tạo.
- `IsProcessed` & `ProcessedAt`: Cờ và thời điểm ghi nhận lượt xác nhận thành công.
- `RequestedByUserId`: Người gửi yêu cầu.

### 3. Cơ Chế Xác Nhận 1-Click Qua WebAdmin
1. **Liên kết 1-Click trong Email**:
   - Email gửi tới phòng ban chứa thông tin chi tiết của nhân sự và nút bấm 1-click trỏ tới tuyến đường công khai trên WebAdmin: `/confirm-client-status?token=...`.
2. **Xử lý Tự động & Không Cần Đăng Nhập**:
   - Khi người duyệt bấm link, giao diện WebAdmin tự động gửi token đến endpoint API công khai `POST /api/v1/clients/confirm-status`.
   - Backend xác thực tính hợp lệ của token (chưa hết hạn 24h, chưa qua xử lý), tự động cập nhật `Client.IsActive` tương ứng với `ActionType`, đánh dấu `IsProcessed = true` và ghi nhận vết kiểm toán `AuditLog`.
   - WebAdmin phản hồi giao diện kết quả trang trọng (Thành công / Hết hạn / Đã xử lý).

### 4. Quy Trình Khóa và Kích Hoạt Lại Nhân Sự Hiện Có
- Đối với nhân sự đã tồn tại, việc chuyển đổi trạng thái giữa Hoạt động và Vô hiệu hóa được thực hiện thông qua nút thao tác "Yêu cầu khóa qua email" hoặc "Yêu cầu kích hoạt lại qua email" trong màn hình Sửa / Chi tiết, tạo ra bản ghi `ClientApprovalRequest` tương tự.
- Cho phép quản trị viên gửi lại email xác nhận (`Resend Approval Email`) nếu email bị thất lạc hoặc token đã hết hạn 24 giờ.

---

## Hệ quả & Đánh đổi (Consequences)

### Tích cực
- **Minh bạch trách nhiệm nghiệp vụ**: Quyền ra vào bãi xe của nhân sự được phân quyền phê duyệt rõ ràng cho phòng ban chuyên trách qua email.
- **Dấu vết kiểm toán toàn diện**: Bản ghi `ClientApprovalRequest` lưu giữ đầy đủ lịch sử các lần yêu cầu, thời gian click và địa chỉ email phê duyệt.
- **Trải nghiệm mượt mà**: Người phê duyệt chỉ cần 1 cú click từ email trên máy tính hoặc điện thoại thông minh để kích hoạt hoặc khóa hồ sơ mà không cần đăng nhập vào hệ thống quản trị.

### Đánh đổi
- **Thời gian trễ kích hoạt (Asynchronous Latency)**: Nhân sự mới không thể qua cổng ngay lập tức sau khi admin tạo hồ sơ mà phải đợi phòng ban click xác nhận trong vòng 24 giờ.
- **Phụ thuộc hạ tầng gửi Mail (SMTP)**: Quá trình tạo mới hồ sơ phụ thuộc vào việc cấu hình SMTP hoạt động ổn định; hệ thống áp dụng cơ chế tự bảo vệ: nếu gửi mail thất bại, hồ sơ vẫn được lưu trữ và cho phép bấm gửi lại sau mà không làm mất dữ liệu đã nhập.
