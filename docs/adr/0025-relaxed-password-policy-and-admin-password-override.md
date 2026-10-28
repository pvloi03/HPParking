# 0025: Chính Sách Mật Khẩu Tối Giản Và Đặc Quyền Đổi Mật Khẩu Của Quản Trị Viên

Quyết định thiết kế về quy chuẩn mật khẩu và cơ chế đổi mật khẩu trong phân hệ `HPParking.Api`: áp dụng chính sách mật khẩu tối giản (chỉ yêu cầu độ dài tối thiểu 6 ký tự, loại bỏ bắt buộc chữ hoa, chữ thường hay số) nhằm tối ưu hóa sự thuận tiện cho nhân viên vận hành tại các trạm kiểm soát bãi xe; đồng thời thiết lập cơ chế ủy quyền linh hoạt cho phép vai trò `Admin` đổi mật khẩu trực tiếp mà không cần cung cấp mật khẩu cũ hiện tại, trong khi các vai trò người dùng thông thường (`Manager`, `Viewer`) vẫn bắt buộc phải xác thực mật khẩu cũ.

## Ngữ cảnh

Trong tài liệu đặc tả ban đầu (ADR 0015), quy chuẩn mật khẩu áp dụng các ràng buộc độ phức tạp khắt khe (chữ hoa, chữ thường, số, ký tự đặc biệt) và bắt buộc nhập mật khẩu cũ (`oldPassword`) trong mọi trường hợp gọi endpoint đổi mật khẩu `POST /api/v1/auth/change-password`.

Tuy nhiên, trong thực tế triển khai hệ thống quản lý bãi xe:
1. **Đặc thù môi trường vận hành bãi xe**: Nhân viên trực ca và quản lý thường xuyên phải thao tác trên màn hình cảm ứng hoặc bàn phím cơ bản tại bốt kiểm soát. Yêu cầu mật khẩu quá phức tạp gây chậm trễ khi đăng nhập đổi ca và thường dẫn đến việc nhân viên ghi mật khẩu ra giấy dán tại bàn làm việc, gây mất an toàn hơn.
2. **Khôi phục quyền truy cập ban đầu**: Sau khi hệ thống khởi tạo tài khoản quản trị ban đầu từ seeder, hoặc khi quản trị viên cần khôi phục/đặt lại mật khẩu nhanh cho tài khoản quản trị mà không muốn bị chặn bởi mật khẩu cũ.
3. **Phân quyền vai trò**: Cần sự phân tách rành mạch: tài khoản thông thường (non-Admin) bắt buộc phải kiểm tra mật khẩu cũ để đảm bảo người đang thao tác là chủ tài khoản; riêng tài khoản `Admin` có toàn quyền quản trị cao nhất nên được phép bỏ qua bước nhập mật khẩu cũ.

## Quyết định

1. **Chính sách mật khẩu tối giản (Password Policy)**:
   - Mọi mật khẩu mới (`newPassword`) chỉ cần đạt độ dài **tối thiểu 6 ký tự** (`MinimumLength = 6`).
   - Không áp dụng regex kiểm tra chữ hoa, chữ thường, số hay ký tự đặc biệt.
   - Trường `confirmNewPassword` bắt buộc phải trùng khớp hoàn toàn với `newPassword`.

2. **Cơ chế đổi mật khẩu theo vai trò (Role-based Password Change)**:
   - **Đối với vai trò `Admin`**:
     - Trường `oldPassword` là tùy chọn (optional/nullable).
     - Hệ thống tự động bỏ qua kiểm tra `BCrypt.Verify(oldPassword, user.PasswordHash)` khi token thuộc vai trò `Admin`.
     - Cho phép Admin cập nhật mật khẩu trực tiếp.
   - **Đối với vai trò thông thường (`Manager`, `Viewer`)**:
     - Trường `oldPassword` là bắt buộc (`NotEmpty`).
     - Bắt buộc kiểm tra khớp mật khẩu cũ qua `BCrypt.Verify(oldPassword, user.PasswordHash)`. Nếu sai, trả về HTTP 400 Bad Request: `"Mật khẩu hiện tại không đúng."`.
   - Cả hai trường hợp đều ghi nhận sự kiện vào `AuditLog` với `ActionType = AuditActionType.Update`, `TargetEntity = "User"`.

## Hệ quả

- **Tích cực**:
  - Giảm thiểu ma sát và sai sót khi nhân viên thao tác đổi ca hoặc cập nhật mật khẩu tại trạm bãi xe.
  - Quản trị viên hệ thống có thể linh hoạt khôi phục hoặc cập nhật mật khẩu tức thì mà không bị gián đoạn.
- **Đánh đổi**:
  - Mật khẩu tối thiểu 6 ký tự ít phức tạp hơn về mặt lý thuyết, nhưng được bảo vệ bù đắp bởi cơ chế Rate Limiting (5 lần/phút/IP) chống tấn công vét cạn (brute-force) và thuật toán băm chậm BCrypt.
