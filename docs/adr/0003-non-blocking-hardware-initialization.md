# 0003: Khởi tạo thiết bị ngoại vi bất đồng bộ và Non-blocking cho luồng UI

Toàn bộ các tác vụ kết nối thiết bị phần cứng (Service đọc CCCD SignalR, Camera Hikvision, Controller ZKTeco) phải được khởi tạo bất đồng bộ và chạy ngầm (Non-blocking), không bao giờ được phép chặn luồng khởi động Form chính.

## Ngữ cảnh

Trong môi trường thực tế hoặc phòng phát triển (dev environment), các thiết bị ngoại vi (đầu đọc CCCD qua Service local port 5000, camera mạng IP, bộ điều khiển barrier) thường xuyên ở trạng thái chưa bật nguồn, chưa cắm dây mạng hoặc dịch vụ nền chưa chạy. Nếu hàm khởi tạo `Form_Load` chờ đồng bộ (`await`) một vòng lặp kết nối hoặc thiết bị có timeout lâu, giao diện ứng dụng sẽ bị treo (hang) vô thời hạn hoặc form biến mất.

## Quyết định

1. `CccdReaderManager.StartAsync()` thử kết nối ban đầu một lần duy nhất; nếu không thành công, lập tức chuyển cơ chế retry sang một background task (`Task.Run`) độc lập.
2. `DeviceOrchestrator` bọc toàn bộ tác vụ kết nối camera và controller trong khối try-catch an toàn, kích hoạt sự kiện cập nhật trạng thái UI thay vì ném exception làm sập ứng dụng.
3. Form chính `FrmMain` hiển thị trạng thái thiết bị ngoại vi trực quan qua màu sắc và nhãn văn bản (Online/Offline), cho phép người vận hành vẫn tương tác được với hệ thống khi có một vài thiết bị mất kết nối.

## Hệ quả

- Ứng dụng khởi động ngay lập tức (< 1 giây) trong mọi điều kiện mạng và phần cứng.
- Giao diện người dùng phản hồi mượt mà, tăng tính sẵn sàng cao (high availability) cho trạm kiểm soát bãi đỗ xe.
