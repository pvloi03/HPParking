# 0032: Loại Bỏ Cơ Chế Active TCP Socket Ping Trực Tiếp Khỏi HPParking.Api Để Tối Ưu Triển Khai Cloud

Quyết định thiết kế về việc loại bỏ hoàn toàn cơ chế kiểm tra kết nối TCP Socket trực tiếp (Active TCP Socket Ping: `/ping`, `/ping-all`) khỏi phân hệ `HPParking.Api`: giữ cho dịch vụ REST API hoàn toàn phi trạng thái (Stateless), trung lập về hạ tầng mạng (Network-Agnostic) và sẵn sàng triển khai trên môi trường Điện toán đám mây (Cloud / Docker / Kubernetes) mà không bị phụ thuộc vào định tuyến mạng nội bộ (LAN / Private Subnet).

## Ngữ cảnh

Trong hệ thống kiểm soát bãi đỗ xe HPParking:
1. **Hạ tầng mạng phần cứng bãi đỗ (Edge LAN)**:
   - Các thiết bị ngoại vi như Camera IP (Hikvision, Dahua), Bộ điều khiển Barie (ZKTeco C3-400) và Đầu đọc FaceID luôn được cấu hình địa chỉ IP nội bộ (Private IPv4: `192.168.x.x` hoặc `10.x.x.x`) nằm phía sau Router/NAT Firewall của trạm kiểm soát.
2. **Kịch bản triển khai REST API trên Cloud (Cloud-Native Hosting)**:
   - Khi `HPParking.Api` được đóng gói Docker và triển khai trên các nền tảng đám mây công cộng (AWS, Azure, DigitalOcean, VPS), máy chủ API không nằm trong cùng mạng vật lý với thiết bị bãi xe.
   - Nếu `HPParking.Api` mở kết nối TCP Socket trực tiếp tới `192.168.1.100:8000`:
     * Yêu cầu sẽ bị timeout hoặc drop hoàn toàn tại gateway đám mây.
     * API trả về trạng thái Offline sai lệch (False Negative), gây hiểu lầm cho người quản trị rằng phần cứng hỏng trong khi thực tế thiết bị tại bãi xe vẫn đang vận hành bình thường.
     * Để khắc phục qua đường socket trực tiếp, doanh nghiệp bắt buộc phải duy trì hạ tầng mạng phức tạp: thiết lập Site-to-Site VPN (IPsec/WireGuard) hoặc mở cổng NAT công khai (Port Forwarding / DDNS) — một rủi ro an ninh mạng nghiêm trọng đối với thiết bị camera và barrier công nghiệp.
3. **Phân định trách nhiệm kiến trúc (Separation of Concerns & YAGNI)**:
   - Trách nhiệm của `HPParking.Api`: Cung cấp giao diện CRUD quản lý cấu hình danh mục, thực thi toàn vẹn tham chiếu dữ liệu (ADR 0030, ADR 0031), kiểm soát vòng đời và cung cấp dữ liệu phục vụ Web Admin / Mobile / bên thứ ba.
   - Trách nhiệm kiểm tra trạng thái sống/chết (Liveness / Heartbeat) của phần cứng cục bộ thuộc về:
     * Ứng dụng máy trạm cổng **WinForms Desktop `HPParking`** (kết nối trực tiếp cùng switch mạng LAN với thiết bị theo ADR 0010 và ADR 0012).
     * Hoặc một Edge Agent dịch vụ chạy nền tại chỗ.

## Quyết định

1. **Loại Bỏ Hoàn Toàn Tính Năng Direct TCP Socket Ping Khỏi `HPParking.Api`**:
   - Không bổ sung endpoint `POST /api/v1/devices/{id}/ping` và `POST /api/v1/devices/ping-all`.
   - Không đăng ký service `ITcpSocketPingService` / `TcpSocketPingService` trong tầng API.
   - Tránh phát sinh mã nguồn thừa và các phụ thuộc socket mạng không khả thi trên môi trường Cloud (tuân thủ nguyên tắc YAGNI).

2. **Giữ Trọn Vẹn Phạm Vi CRUD Thiết Bị Ngoại Vi Chuẩn Mực**:
   - `HPParking.Api` tập trung tuyệt đối vào:
     * Quản lý thông tin cấu hình phần cứng: `Code`, `Name`, `Type` (`Camera`, `Controller`, `FaceId`, `Other`), `IpAddress`, `Port`, `UserName`.
     * Bảo mật thông tin mạng: Trường `Password` được mã hóa/lưu trữ DB và không bao giờ xuất hiện trong `DeviceDto` (chỉ cung cấp cờ `HasPassword`).
     * Xác thực hợp lệ dữ liệu đầu vào: Địa chỉ IPv4 chuẩn, dải Port 1-65535, chống trùng `Code` và cặp `(IpAddress, Port)` giữa các thiết bị đang hoạt động (`409 Conflict`).
     * Toàn vẹn tham chiếu theo ADR 0030 & ADR 0031: Chặn xóa (`409 Conflict`) khi thiết bị đang được sử dụng bởi bất kỳ Làn xe nào chưa xóa; Chặn tắt (`400 Bad Request`) khi Làn xe đang active; Hỗ trợ xem thùng rác (`?onlyDeleted=true`) và khôi phục (`POST /restore`).

3. **Định Hướng Kiến Trúc Giám Sát Phần Cứng Từ Xa (Future Telemetry Strategy)**:
   - Nếu trong tương lai có yêu cầu hiển thị trạng thái Online/Offline của thiết bị trên Web Admin Cloud:
     * Hệ thống sẽ áp dụng mô hình **Báo Cáo Nhịp Tim Đẩy Ngược (Inverted Heartbeat Telemetry / Edge Ingestion)**: Máy trạm WinForms hoặc Edge Agent tại bãi xe thực hiện ping nội bộ trong LAN, sau đó định kỳ đẩy bản tin trạng thái lên API thông qua endpoint `POST /api/v1/devices/telemetry` hoặc qua kênh WebSocket/SignalR/MQTT.
     * Máy chủ Cloud chỉ tiếp nhận và lưu trữ trạng thái mà không bao giờ chủ động mở kết nối TCP ngược vào mạng riêng của bãi xe.

## Hệ quả

- **Tích cực**:
  - `HPParking.Api` hoàn toàn độc lập với hạ tầng mạng LAN cục bộ, triển khai được ngay trên bất kỳ nền tảng Cloud, Container, hay Serverless nào mà không yêu cầu cấu hình VPN hay mở port mạng.
  - Loại bỏ hoàn toàn các lỗi false-offline hoặc request treo do timeout kết nối socket khi test trên các môi trường staging/cloud.
  - Mã nguồn API tinh gọn, tuân thủ nguyên tắc Single Responsibility Principle (SRP) và YAGNI.
- **Đánh đổi**:
  - Giao diện Web Admin tạm thời chưa xem được trạng thái Online/Offline tức thời của thiết bị bằng nút bấm trực tiếp từ Cloud cho đến khi phân hệ báo cáo nhịp tim từ máy trạm (Edge Telemetry) được triển khai ở giai đoạn tiếp theo. Đánh đổi này là hoàn toàn hợp lý và đúng đắn về mặt kỹ thuật.
