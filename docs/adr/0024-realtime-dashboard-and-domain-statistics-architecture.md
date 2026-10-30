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
   - Màn hình tổng quan (Dashboard Home) của người quản trị cần hiển thị đồng thời các thẻ KPI của Khách hàng, Phương tiện, Phiên đỗ xe, Doanh thu và Hạ tầng Làn xe. Việc gọi nhiều request riêng lẻ sẽ gây phân mảnh mạng.
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
     * Tiếp nhận các tham số truy vấn tùy chọn: `?fromDate=...&toDate=...` (chuỗi ISO Local Date-Time `...T00:00:00` và `...T23:59:59`).
     * **Ngữ Nghĩa Phân Tầng Chỉ Số (Dual Metric Scoping Semantics)**:
       1. *Nhóm Hiện Trạng Vận Hành & Danh Mục (Operational State & Master Data Snapshot - Toàn Cục / Tức Thời)*:
          - Khách hàng (`TotalClients`, `ActiveClients`, `ClientsByType`).
          - Sinh trắc học (`ClientsWithFaceId`, `FaceIdSyncRatePercentage`).
          - Phương tiện (`TotalVehicles`, `ActiveVehicles`, `VehiclesByType`).
          - Phiên đỗ xe hiện hành (`ActiveParkingSessions`).
          - Hạ tầng Cổng/Làn kiểm soát (`TotalGates`, `TotalLanes`, `ActiveLanes`). Trong đó, `TotalGates` được duy trì trong DTO backend để phục vụ tổng kiểm kê hạ tầng, còn trên giao diện Dashboard UI, thẻ KPI trực quan tập trung vào `ActiveLanes`/`TotalLanes` (Làn Vận Hành).
          Các chỉ số này phản ánh hiện trạng toàn cục tức thời của hệ thống tại thời điểm truy vấn, không bị phụ thuộc vào khoảng ngày lọc.
       2. *Nhóm Tài Chính & Doanh Thu (Financial & Revenue Metrics - Kỳ Lọc Thời Gian / Mặc Định Hôm Nay)*:
          - Doanh thu thực thu (`TotalRevenue`): Tổng tiền thực thu từ các phiên hoàn tất (`Status == Completed`, `IsFeeCollected == true`, `!IsDeleted`) có thời điểm xuất bãi `OutTime` nằm trong khoảng lọc.
          - Số lượt thu phí (`PaidParkingSessionsCount`): Số phiên hoàn tất đã thu tiền có `OutTime` nằm trong khoảng lọc.
       3. *Quy Tắc Mặc Định Khi Không Truyền Thời Gian (`fromDate == null && toDate == null`)*:
          - Hệ thống tự động kích hoạt `NormalizeDateRange(null, null)` và chuẩn hóa về **Hôm nay (Today: 00:00:00 -> 23:59:59 trong không gian ngày Việt Nam `UTC+7`)**.
          - Do đó, khi gọi `GET /api/v1/statistics/dashboard` không tham số, thẻ KPI Doanh thu thể hiện **Doanh thu trong ngày** chứ không phải doanh thu tích lũy toàn thời gian (All-time).
       4. *Chuẩn Hóa 5 Thẻ KPI Trên Giao Diện WebAdmin (`KpiCardGrid`)*:
          - Lưới thẻ KPI trên Dashboard Home được chuẩn hóa thành 5 thẻ trực quan:
            1. **Nhân Sự** (`TotalClients`, `ActiveClients`).
            2. **Phương Tiện** (`TotalVehicles`, `ActiveVehicles`).
            3. **Xe Đang Đỗ** (`ActiveParkingSessions` kèm cảnh báo dung lượng bãi khi $\ge 180$ xe).
            4. **Doanh Thu** (`TotalRevenue`, `PaidParkingSessionsCount` theo ngày/kỳ lọc) — *thay thế cho thẻ "Cổng Kiểm Soát" trước đây để ưu tiên giám sát dòng tiền*.
            5. **Làn Vận Hành** (`ActiveLanes` / `TotalLanes` kèm tỷ lệ trực tuyến).

> [!NOTE] **Ghi nhận Chuyển Dịch Ngữ Nghĩa Chỉ Số Doanh Thu (ADR-0024 Borderline Drift & Scoping Defense)**:  
> _Trước đây, tên trường `TotalRevenue` trên DTO có thể ngụ ý doanh thu tích lũy toàn thời gian (all-time) tương tự `TotalClients` hay `TotalVehicles`. Tuy nhiên, việc áp dụng phạm vi thời gian (mặc định hôm nay) được bảo vệ và giữ vững theo tôn chỉ ADR-0024 vì:_  
> 1. **Bảo vệ hiệu năng MongoDB & SLA phản hồi (< 15ms)**: Truy vấn quét toàn bộ lịch sử phiên đỗ xe (hàng triệu bản ghi theo năm tháng) mỗi lần tải trang chủ sẽ gây quá tải RAM index và vi phạm nghiêm trọng mục tiêu truy vấn thời gian thực của ADR-0024.  
> 2. **Giá trị vận hành thực tiễn**: Trong quản lý bãi xe hàng ngày, ban quản lý cần nắm bắt nhanh doanh thu và lượng thu tiền của ngày hôm nay, hoặc tùy chọn Tháng/Năm thông qua thanh công cụ `DashboardFilterBar`. Số liệu doanh thu tích lũy lịch sử toàn thời gian thuộc về phân hệ Báo cáo chuyên sâu (`/reports/revenue` theo ADR 0043) thay vì thẻ KPI giám sát tức thời của Dashboard.

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

5. **Bất Biến Thời Gian CSDL (True-UTC Instant Invariant cho `InTime` / `OutTime`):**
   - **Quy tắc bắt buộc:** Mọi trường mốc thời gian nghiệp vụ của phiên đỗ xe (`ParkingSession.InTime`, `ParkingSession.OutTime`) được lưu trong CSDL **LUÔN là mốc UTC thực (true-UTC instant)**. Đây là nền tảng để toàn bộ phía đọc (bộ lọc `[fromUtc, toUtc]` trong `DateRangeHelper.NormalizeDateRange`, pipeline `$facet`/`$dateToString "+07:00"` gom nhóm theo ngày Việt Nam, đường repo DB-side) hoạt động đúng trong không gian ngày Việt Nam (ADR 0024 §1.1).
   - **Nguồn đầu vào là wall-clock địa phương (naive-VN):** Thiết bị điều khiển (ZKTeco controller qua `RealtimeLog.Time = DateTime.Now`) và trạm WinForms truyền `TriggerTime` dạng wall-clock không gắn Kind (naive). **Khi ingest (ghi CSDL), mọi phía ghi bắt buộc quy đổi qua `VietnamTime.ToUtcInstant`:**
     - `Kind == Utc` → giữ nguyên;
     - `Kind == Local` → `ToUniversalTime()` (giữ instant);
     - `Kind == Unspecified` → **giả định là wall-clock giờ Việt Nam (UTC+7)**, trừ 7 giờ và gán `Kind = Utc`.
     Các điểm ghi áp dụng: workflow xe người (entry/exit), xe vãng lai, xe khách, xe radar, người đi bộ trong `ParkingWorkflowService` và các handler tương ứng; đường API force-close vốn dùng `DateTime.UtcNow` (đã đúng bất biến, không cần đổi).
   - **Lý do:** Nếu giá trị naive-VN được lưu nguyên khai (không quy đổi), sự kiện ra bãi buổi tối Việt Nam (17:00–23:59) sẽ rơi ra ngoài cửa sổ UTC của ngày và bị **bỏ sót / xếp sai ngày** trong mọi báo cáo doanh thu và thống kê. Quy đổi tại ngòi ghi (normalize at ingest) giữ nguyên hiệu thời gian (duration) giữa `InTime` và `OutTime` (cả hai cùng được quy đổi), nên tính phí không bị ảnh hưởng.
   - **Ngoại lệ (scope hiện tại):** Chỉ áp dụng cho các dòng ghi mới sau khi triển khai. Các dòng `InTime`/`OutTime` đã lưu trước đó với giá trị naive chưa được tự phát hiện trong CSDL; việc di chuyển (migration) dữ liệu lịch sử là một bước riêng, ngoài phạm vi bất biến này.

## Hệ quả

- **Tích cực:**
  - **Toàn nhất thời gian (Temporal Consistency):** Mọi dòng `InTime`/`OutTime` mới nhất đều là true-UTC instant nhờ quy đổi bắt buộc tại ngòi ghi; phía đọc (dashboard, báo cáo doanh thu, xuất Excel) không cần xử lý nhiều trường hợp Kind hỗn hợp, giảm một lớp lỗi tiềm ẩn theo thời gian (ADR 0024 §5).
  - Bức tranh Dashboard toàn diện: Người quản trị nắm bắt đồng thời các trụ cột trọng yếu: **Nhân sự - Phương tiện - Vận hành bãi đỗ (Xe đang đỗ & Làn xe) - Tài chính (Doanh thu thực thu)**.
  - Báo cáo phân bổ phản ánh chính xác quy mô của từng Công ty thành viên (bao gồm số nhân viên, số xe đăng ký và số cổng/làn xe được giao quản lý).
  - Tốc độ phản hồi cực nhanh (< 15ms) nhờ thực thi song song bất đồng bộ trên MongoDB.
- **Tiêu cực / Rủi ro:**
  - Cần đảm bảo index `IsDeleted`, `IsActive`, `CompanyId` trên các collection `Gates` và `Lanes` để câu truy vấn đếm luôn nằm trọn trong RAM.
