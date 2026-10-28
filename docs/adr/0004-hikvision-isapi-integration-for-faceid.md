# 0004: Tích hợp thiết bị FaceID qua giao thức Hikvision ISAPI thay vì Native SDK

Hệ thống sử dụng giao thức chuẩn HTTP REST ISAPI (Digest Authentication) để đồng bộ dữ liệu người dùng, mã thẻ và ảnh khuôn mặt lên thiết bị FaceID Hikvision thay vì dùng thư viện P/Invoke C/C++ Native SDK.

## Ngữ cảnh

Trong quy trình đăng ký khách hàng ([FrmRegisterClient.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Forms/FrmRegisterClient.cs)), nhân viên trạm cần đẩy dữ liệu thẻ CCCD gắn chip và ảnh chân dung thẻ trực tiếp lên các đầu đọc nhận diện khuôn mặt Hikvision lắp tại cổng. Nếu sử dụng `HCNetSDK.dll` (P/Invoke C/C++), việc quản lý bộ nhớ đệm (unmanaged memory), đồng bộ trạng thái mạng và xử lý lỗi tải ảnh đa phần (multipart image) rất phức tạp và dễ phát sinh rò rỉ bộ nhớ hoặc xung đột con trỏ.

## Quyết định

Triển khai [FaceIdApiService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/FaceId/FaceIdApiService.cs) giao tiếp qua Hikvision ISAPI:
1. Xác thực bằng HTTP Digest Authentication (`HttpClientHandler`).
2. Giao tiếp qua các endpoint RESTful chuẩn: tạo User (`/ISAPI/AccessControl/UserInfo/Record`), gán thẻ (`/ISAPI/AccessControl/CardInfo/Record`) và nạp ảnh khuôn mặt (`/ISAPI/Intelligent/FDLib/FaceDataRecord`).
3. Cơ chế Rollback tự động: Nếu bước tải ảnh khuôn mặt thất bại, hệ thống tự động phát lệnh xóa User và Card vừa tạo để tránh rác dữ liệu trên thiết bị.

## Hệ quả

- Độc lập hoàn toàn với kiến trúc bộ nhớ 32-bit/64-bit cho module FaceID.
- Dễ dàng kiểm thử, mock và mở rộng kết nối tới nhiều thiết bị FaceID trong mạng LAN mà không lo cạn kiệt tài nguyên SDK native.
