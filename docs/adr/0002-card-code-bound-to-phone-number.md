# 0002: Đồng nhất Card_Code trong EventParking bằng PhoneNumber của Client

> **Trạng thái:** Bị thay thế (Superseded) bởi [ADR 0038](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0038-unified-card-and-multi-auth-verification-architecture.md) và [ADR 0040](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0040-multi-target-lane-architecture-and-tuple-pattern-matching.md).  
> **Lý do thay thế:** Hệ thống đã tách biệt mã thẻ thành thực thể `Card` 10 chữ số thống nhất (hỗ trợ cả thẻ RFID vật lý và thẻ ảo FaceID) và không còn ép buộc gán `Card_Code` bằng `PhoneNumber`.

Trong sự kiện gửi xe (`EventParking`), trường `Card_Code` trước đây được gán bằng số điện thoại của khách hàng (`client.PhoneNumber`) theo chủ đích thiết kế nghiệp vụ ban đầu.

## Ngữ cảnh

Trong một hệ thống bãi đỗ xe thông thường, lập trình viên có thể kỳ vọng trường `EventParking.Card_Code` sẽ lưu trữ mã thẻ từ RFID vật lý (`client.Card_Code`). Tuy nhiên, trong mô hình vận hành của dự án này, khách hàng đăng ký định danh gắn liền với số điện thoại liên lạc. Số điện thoại được sử dụng làm khóa truy vấn và đồng bộ dữ liệu giao dịch xuyên suốt qua các hệ thống con và cổng thông tin.

## Quyết định

Trong `ParkingWorkflowService.ProcessEntryAsync` và `ProcessExitAsync`:
- Luôn gán: `Card_Code = client.PhoneNumber`.
- Tuyệt đối không thay thế bằng `client.Card_Code` trong các lần refactor mã nguồn.

## Hệ quả

- Bảo toàn tính tương thích với cơ sở dữ liệu MongoDB hiện có.
- Tránh việc các công cụ AI hoặc lập trình viên mới ngộ nhận đây là một lỗi chính tả (bug/typo) và tự ý sửa đổi gây sai lệch dữ liệu lịch sử.
