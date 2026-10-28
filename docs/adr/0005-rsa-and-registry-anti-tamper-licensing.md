# 0005: Cơ chế bảo vệ bản quyền Offline kết hợp Chữ ký số RSA và Registry Anti-Tamper Clock

Bảo vệ bản quyền phần mềm trạm đỗ xe bằng chữ ký số bất đối xứng RSA-2048 gắn liền với định danh phần cứng máy tính (WMI CPU ID) và cơ chế chống lùi thời gian máy tính (Anti-tamper Clock) ghi nhận vào Windows Registry.

## Ngữ cảnh

Hệ thống bãi đỗ xe vận hành tại các trạm kiểm soát độc lập, nhiều nơi mạng nội bộ bị cách ly (air-gapped) hoặc không có đường truyền Internet 24/7 để kết nối License Server trên đám mây. Nếu chỉ lưu trữ ngày hết hạn thông thường hoặc dùng thuật toán băm đơn giản, khách hàng có thể dễ dàng can thiệp bằng cách chỉnh lùi giờ hệ điều hành Windows để dùng phần mềm vĩnh viễn mà không trả phí gia hạn.

## Quyết định

Triển khai [LicenseValidator.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/LicenseKey/LicenseValidator.cs):
1. Chuỗi License Key gồm 2 phần định dạng Base64: `payload.signature`.
   - `payload`: Chứa `FixedCode`, `ExpiryDate` và `MachineCode` (lấy từ CPU ID qua `win32_processor`).
   - `signature`: Chữ ký điện tử được ký bằng Private Key RSA-2048 từ phía nhà cung cấp phần mềm, được ứng dụng thẩm định bằng Public Key RSA nhúng sẵn.
2. Kiểm tra MachineCode: Phải trùng khớp với CPU ID của máy trạm thực tế.
3. Chống tua lùi thời gian (Anti-tamper):
   - Mốc thời gian thực thi mới nhất (`LastRunTime`) được ghi nhận liên tục mỗi 60 giây vào Registry `HKCU\Software\HPParking`.
   - Khi khởi động, nếu `DateTime.Now < LastRunTime`, ứng dụng phát hiện hành vi can thiệp lùi giờ và lập tức khóa hoạt động.

## Hệ quả

- Đảm bảo an toàn bản quyền tối đa cho phần mềm thương mại khi triển khai offline.
- Cần công cụ cấp phát License nội bộ riêng để sinh License Key theo từng CPU ID của khách hàng.
