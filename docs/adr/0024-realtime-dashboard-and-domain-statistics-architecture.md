# 0024: Kiến Trúc API Thống Kê Thời Gian Thực Dùng MongoDB Facet Pipeline Và Mô Hình Định Tuyến Lai

Quyết định chuẩn hóa giải pháp Cung Cấp Số Liệu Thống Kê & Báo Cáo Tổng Quan (Dashboard & Statistics) cho `HPParking.Api`: áp dụng mô hình định tuyến lai (Hybrid Routing) kết hợp giữa endpoint tập trung `GET /api/v1/dashboard/summary` phục vụ trang chủ quản trị và các endpoint phân tán (`/clients/statistics`, `/vehicles/statistics`) phục vụ từng phân hệ nghiệp vụ, sử dụng kỹ thuật truy vấn gom nhóm đa chiều song song MongoDB `$facet` Aggregation Pipeline trong một round-trip mạng duy nhất, vận hành theo cơ chế tính toán thời gian thực 100% (Real-time, No-Cache) để đảm bảo độ chính xác tuyệt đối, hỗ trợ bộ lọc đa chiều linh hoạt theo đơn vị và khoảng thời gian, và mở quyền truy cập cho toàn bộ các vai trò đã xác thực (`Viewer`, `Manager`, `Admin`) phục vụ công tác giám sát vận hành bãi xe.

## Ngữ cảnh

Trong hệ thống quản lý bãi đỗ xe thông minh HPParking:
1. **Trách nhiệm tính toán (Backend vs Client):**
   - Nếu để máy khách (Frontend Web/App) tự kéo toàn bộ dữ liệu về rồi chạy vòng lặp tính toán:
     * Gây bùng nổ băng thông mạng (kéo hàng nghìn bản ghi JSON về máy khách).
     * Xung đột trực tiếp với cơ chế phân trang an toàn (ADR 0022 giới hạn `pageSize <= 100`), khiến máy khách không thể biết được số liệu toàn cục nếu không phá vỡ kẹp trần.
     * Trình duyệt/thiết bị di động bị đơ, giật lag, ngốn RAM và pin.
     * Nguy cơ lệch số liệu giữa các nền tảng (Web Admin, App bảo vệ, trạm WinForms) do cài đặt logic đếm khác nhau.
     * Rò rỉ thông tin cá nhân khi phải tải về dữ liệu nhạy cảm của toàn bộ khách hàng chỉ để đếm số lượng.
   - Do đó, **100% logic thống kê phải do Backend API xử lý** tại nơi lưu trữ dữ liệu (Database level).
2. **Lựa chọn mô hình định tuyến (Centralized vs Domain-specific):**
   - Màn hình tổng quan (Dashboard Home) của người quản trị cần hiển thị đồng thời các thẻ KPI của Khách hàng, Phương tiện (và sau này là Lượt gửi xe `ParkingSessions`). Việc gọi 3-4 request riêng lẻ sẽ gây phân mảnh mạng.
   - Tuy nhiên, khi nhân sự làm việc tại màn hình "Quản lý Khách hàng" hoặc "Quản lý Xe", họ cần số liệu thống kê chi tiết của riêng màn hình đó mà không muốn tải thừa dữ liệu của màn hình khác.
   - Giải pháp: **Mô hình Lai (Hybrid)** cung cấp cả endpoint tổng hợp `GET /api/v1/dashboard/summary` và các endpoint chuyên sâu cho từng resource.
3. **Hiệu năng truy vấn dữ liệu lớn trong MongoDB:**
   - Thay vì gửi hàng loạt lệnh `CountDocumentsAsync` tuần tự gây nhiều round-trip mạng, kỹ thuật **MongoDB `$facet` Aggregation Pipeline** cho phép định nghĩa nhiều luồng tính toán song song (đếm tổng số, đếm theo trạng thái kích hoạt, gom nhóm theo loại khách/loại xe, kiểm tra ảnh FaceID) trong **1 câu lệnh duy nhất**.
4. **Chiến lược Lưu bộ nhớ đệm (Caching Policy):**
   - Ở quy mô bãi xe hiện tại (vài nghìn khách hàng và phương tiện), MongoDB `$facet` thực thi trực tiếp trên RAM index chỉ mất 2-5ms.
   - Việc áp dụng Cache phân tán hoặc In-Memory Cache sẽ phát sinh rủi ro số liệu bị cũ (stale data) hoặc phức tạp trong việc xóa cache khi có thêm/sửa/xóa khách hàng. Lựa chọn **Thời gian thực 100% (No Cache)** là phương án tinh gọn (YAGNI), đơn giản và chính xác tuyệt đối.

## Quyết định

1. **Mô Hình Định Tuyến Báo Cáo Thống Kê (Statistics Endpoint Routing):**
   - **Dashboard Tổng Quan**: `GET /api/v1/statistics/dashboard`
     * Trả về toàn bộ các thẻ KPI tổng hợp về Khách hàng (`TotalClients`, `ActiveClients`, `ClientsByType`), Sinh trắc học (`ClientsWithFaceId`, `FaceIdSyncRatePercentage`), Phương tiện (`TotalVehicles`, `ActiveVehicles`, `VehiclesByType`), Phiên đỗ xe hiện hành (`ActiveParkingSessions`), và **Hạ tầng Cổng/Làn kiểm soát** (`TotalGates`, `TotalLanes`, `ActiveLanes`).

2. **Kỹ Thuật Truy Vấn MongoDB `$facet` Aggregation Pipeline Song Song:**
   - Triển khai trong tầng Service qua MongoDB Driver, thực thi song song các luồng aggregation và đếm qua `Task.WhenAll`:
     * Pipeline `$facet` trên collection `Clients` (đếm tổng, active, nhóm theo loại khách, kiểm tra FaceID avatar).
     * Pipeline `$facet` trên collection `Vehicles` (đếm tổng, active, nhóm theo loại xe).
     * Đếm phiên đỗ xe đang gửi trong bãi `ParkingSessions` (`Status == Active && !IsDeleted`).
     * Pipeline đếm hạ tầng `Gates` và `Lanes` (`TotalGates`, `TotalLanes`, `ActiveLanes`).
   - Toàn bộ phép tính hoàn tất trong **1 round-trip mạng song song** từ API Server tới MongoDB.

3. **Chính Sách Tính Toán Thời Gian Thực (Real-time Policy):**
   - 100% truy vấn trực tiếp vào MongoDB, không áp dụng caching layer để bảo đảm số liệu luôn tươi mới tức thời theo từng mili-giây.

4. **Chính Sách Phân Quyền (Authorization Policy):**
   - Cho phép tất cả các tài khoản hợp lệ (`Viewer`, `Manager`, `Admin`) và hệ thống tích hợp qua `X-API-KEY` (tương đương quyền Admin theo ADR 0015).

## Hệ quả

- **Tích cực:**
  - Bức tranh Dashboard toàn diện: Người quản trị nắm bắt đồng thời cả 3 trụ cột: **Nhân sự - Phương tiện - Hạ tầng Cổng/Làn**.
  - Báo cáo phân bổ phản ánh chính xác quy mô của từng Công ty thành viên (bao gồm số nhân viên, số xe đăng ký và số cổng/làn xe được giao quản lý).
  - Tốc độ phản hồi cực nhanh (< 15ms) nhờ thực thi song song bất đồng bộ trên MongoDB.
- **Tiêu cực / Rủi ro:**
  - Cần đảm bảo index `IsDeleted`, `IsActive`, `CompanyId` trên các collection `Gates` và `Lanes` để câu truy vấn đếm luôn nằm trọn trong RAM.
