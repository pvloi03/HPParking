# BÁO CÁO TOÀN DIỆN VỀ DỰ ÁN HPPARKING

> **Ngày cập nhật:** 01/10/2026 (Đã đồng bộ sau tái cấu trúc làn đa đối tượng, bộ điều phối khớp mẫu tuple, CHCNetSDK native preview & điều vận xe công vụ)  
> **Workspace:** `c:\Users\ADMIN\source\repos\HPParking`  
> **Kiến trúc mục tiêu:** .NET 8 Solution đa phân hệ (WinForms x86, REST API AnyCPU, React Web Admin)  
> **Bộ kiểm thử tự động:** **90 tests** (xUnit + NSubstitute + FluentAssertions) - **100% Passed**  

---

## 1. DỰ ÁN NÀY LÀ GÌ?

**HPParking** (Hoàng Phát Parking) là **Hệ thống Phần mềm Quản lý Kiểm soát Ra vào & Bãi đỗ xe Thông minh Đa phương thức** (Multi-Authentication Smart Parking & Access Control System) phục vụ các khu công nghiệp, nhà máy, cơ quan và tòa nhà.

Hệ thống cung cấp giải pháp toàn diện từ trạm bốt bảo vệ tại cổng đến cổng thông tin quản trị tập trung từ xa:
1. **Trạm kiểm soát cổng (Desktop WinForms - .NET 8 x86)**: Giao tiếp thời gian thực với các thiết bị phần cứng kiểm soát truy cập (ZKTeco C3 Controller), camera quan sát (Hisilicon, Hikvision), đầu đọc Căn cước công dân gắn chip (CCCD), đầu đọc FaceID (Hikvision `CHCNetSDK`), cảm biến Radar sóng milimet và thanh chắn Barrier / Turnstile.
2. **Máy chủ dịch vụ trung tâm (`HPParking.Api` - .NET 8 AnyCPU)**: Tầng REST API bảo mật chuẩn RFC 9110, xác thực kép (JWT Cookie + API Key), xử lý nhập/xuất bảng tính ClosedXML Excel, thống kê đa chiều MongoDB `$facet` và dịch vụ nền quét vi phạm SLA điều vận xe công vụ.
3. **Cổng web quản trị (`HPParking.WebAdmin` - React + Vite + TypeScript + shadcn/ui)**: Giao diện quản trị hiện đại, responsive đa thiết bị, hỗ trợ giám sát bãi xe thời gian thực, quản lý danh mục, thẻ, xe, khách hàng và điều vận xe công vụ liên nhà máy.

### Mục tiêu nghiệp vụ cốt lõi:

1. **Xác thực đa phương thức thống nhất (Unified Multi-Authentication - ADR 0038, ADR 0040):**
   - Hỗ trợ kết hợp đồng thời: **Thẻ từ RFID/Wiegand chuẩn 10 chữ số**, **Nhận diện khuôn mặt (FaceID)**, **Cảm biến Radar sóng milimet** ngõ AUX IN ZKTeco cho làn tự do, và **Nút bấm cưỡng bức bảo vệ**.
   - Tách biệt hoàn toàn xác thực người (`AuthMethods`) và kiểm tra xe (`VerifyVehiclePlate`).
2. **Kiến trúc làn đa đối tượng & Cấu hình động 1-4 làn (ADR 0040):**
   - Hỗ trợ cấu hình linh hoạt từ 1 đến 4 làn tại mỗi Cổng, phân định rõ **Làn phương tiện** (`LaneTargetType.Vehicle`) và **Làn người đi bộ** (`LaneTargetType.Pedestrian` qua cổng quay turnstile).
   - Tái cấu trúc luồng kiểm soát bằng cú pháp so khớp mẫu tuple (`Pattern Matching Switch Dispatcher`) với 6 khối helper dùng chung.
   - Bố cục giao diện động Option B trên WinForms: Gom hàng ngang camera 280px khi $\le 3$ làn, chia 4 cột khi có 4 làn.
3. **Hiển thị video trực tiếp & Chụp ảnh tối ưu bộ nhớ (`CHCNetSDK`):**
   - Truyền trực tiếp handle đồ họa `PictureBox.Handle` vào SDK Hikvision ở cổng 8000, kiểm soát RAM tiến trình x86 luôn **< 250MB**.
4. **Điều vận xe công vụ liên nhà máy & Giám sát SLA (Fleet Dispatch - ADR 0039):**
   - Quản lý xe dùng chung (`Vehicle.IsShared`), điểm xuất phát động (`OriginGateId`), tuyến cố định và tuyến tự do mặc định.
   - Dịch vụ nền quét vi phạm SLA thời gian thực (`VehicleTransitWatcherService`), tự động gửi Email cảnh báo đính kèm ảnh bằng chứng tài xế qua MailKit.
5. **Quản trị tập trung & Nhập/Xuất Excel (Generic Excel Engine - ADR 0023):**
   - Quản lý toàn bộ 8 danh mục tổ chức và hạ tầng trên Web Admin.
   - Tải file mẫu có dropdown validation, import từng phần (partial-success) kèm báo cáo lỗi chi tiết, export luồng an toàn clamp 10,000 dòng.

---

## 2. CƠ CẤU & THÀNH PHẦN GIẢI PHÁP (SOLUTION STRUCTURE)

```
c:\Users\ADMIN\source\repos\HPParking\
│
├── HPParking.slnx                  # Solution file Visual Studio 2022+
├── CONTEXT.md                      # Từ điển thuật ngữ nghiệp vụ (Domain Glossary) chuẩn hóa
├── AGENTS.md                       # Bản đồ kỹ năng & hướng dẫn AI Agent
│
├── docs/
│   ├── adr/                        # 40 bản ghi quyết định kiến trúc (ADR 0001 -> ADR 0040)
│   ├── agents/                     # Cấu hình domain, issue tracker, triage labels
│   ├── specs/                      # Đặc tả kỹ thuật chi tiết các phân hệ & tái cấu trúc làn
│   └── standards/                  # Chuẩn lập kế hoạch & ma trận ràng buộc tham chiếu
│
├── [1] HPParking.Core/             # Tầng Dữ Liệu & Thực Thể Dùng Chung (.NET 8 AnyCPU)
│   ├── Data/                       # MongoDbContext, MongoRepository<T>, IRepository<T>
│   ├── Models/
│   │   ├── Entities/               # Client, Vehicle, Card, Gate, Lane, Device, ParkingSession,
│   │   │                           # VehicleDispatchTrip, GateRouteConfig, Company, Department...
│   │   ├── Enums/                  # LaneTargetType, TriggerSource, ClientType, VehicleType,
│   │   │                           # ParkingSessionStatus, DeviceType, UserRole...
│   │   └── Common/                 # BaseEntity, AuditableEntity, WorkflowTriggerEvent
│   └── Helpers/                    # PlateHelper, CardHelper, ValidationHelper
│
├── [2] HPParking.Api/              # Tầng Dịch Vụ REST API Trung Tâm (.NET 8 AnyCPU)
│   ├── Controllers/                # Auth, Clients, Vehicles, Cards, Gates, Lanes, Devices,
│   │                               # ParkingSessions, FleetDispatch, Statistics, AuditLogs...
│   ├── Services/                   # Nghiệp vụ CRUD, FaceID sync, Excel Engine, Dashboard `$facet`,
│   │                               # EmailSender, Background SLA Watcher
│   └── Middleware/                 # Deterministic 10-step pipeline, W3C Tracing, Rate Limiting
│
├── [3] HPParking.WebAdmin/         # Phân Hệ Web Quản Trị (Vite + React 18 + TypeScript)
│   ├── src/pages/                  # Dashboard, Clients, Vehicles, Cards, Gates, Lanes, Devices,
│   │                               # ParkingHistory, FleetDispatch, AuditLogs, Settings...
│   ├── src/components/             # UI components chuẩn shadcn/ui, Data Tables, Drawers, Dialogs
│   └── src/services/               # TanStack Query hooks, Axios API client, Zustand stores
│
├── [4] HPParking/                  # Ứng Dụng Máy Trạm WinForms Trạm Cổng (.NET 8 x86)
│   ├── Forms/                      # FrmMain (Option B adaptive UI), FrmLogin, FrmConfigManager...
│   ├── Services/                   # ParkingWorkflowService (Pattern Matching Switch Dispatcher),
│   │                               # DeviceOrchestrator, ControllerService, LprService...
│   └── SDK/                        # Native P/Invoke SDKs (CHCNetSDK, ZKTeco plcommpro, SimpleLPR3)
│
└── [5] HPParking.Tests/            # Bộ Kiểm Thử Tự Động Toàn Diện (90 tests - 100% Passed)
    ├── Api/                        # Unit tests các Controller, Services, Mappers của REST API
    ├── Services/                   # Unit tests ControllerService, ParkingWorkflowService, Storage
    └── Helper/                     # Unit tests PlateHelper, CardHelper, ValidationHelper
```

---

## 3. PHẦN CỨNG & GIAO THỨC TÍCH HỢP

| Thiết bị / Chức năng | Thư viện / SDK / Giao thức | Chi tiết triển khai trong dự án |
| :--- | :--- | :--- |
| **Đầu đọc FaceID Hikvision** | `CHCNetSDK.dll` (cổng 8000) & ISAPI | Xem trực tiếp video LiveView truyền thẳng window handle, chụp ảnh khuôn mặt thời gian thực độ phân giải cao; nạp User & Card 10 số qua ISAPI. RAM < 250MB. |
| **Nhận diện biển số (LPR)** | `SimpleLPR3.dll` (Libs/x86/) | Engine OCR SimpleLPR 3.x với trọng số ưu tiên biển số Việt Nam (`countryWeight = 1.0f`), nhận diện ký tự chính xác. |
| **Bộ điều khiển Barrier / Turnstile** | ZKTeco Pull SDK (`plcommpro.dll`) | Điều khiển relay barie/cổng quay, tiếp nhận mã thẻ Wiegand, bắt sự kiện cảm biến Radar ngõ AUX IN (Event 220/100). |
| **Camera chụp biển số** | Hisilicon SDK & RTSP Stream | Xem LiveView và chụp ảnh snapshot JPEG biển số xe. |
| **Camera toàn cảnh** | Hikvision NetSDK & RTSP Stream | Đăng nhập thiết bị Hikvision, xem luồng trực tiếp và chụp ảnh snapshot toàn cảnh người/phương tiện. |
| **Cảm biến Radar sóng milimet** | Ngõ AUX IN / Sensor Controller | Kích hoạt sự kiện lưu thông tự do cho làn không barie hoặc phát hiện xe tiếp cận. |
| **Đầu đọc CCCD gắn chip** | WebSocket/SignalR Card Hub | Kết nối thời gian thực cổng local 5000, trích xuất số CCCD, họ tên, ngày sinh và ảnh chân dung thẻ chip. |

---

## 4. TỔNG KẾT & ĐÁNH GIÁ CHẤT LƯỢNG

| Tiêu chí | Trạng thái | Đánh giá chi tiết |
| :--- | :--- :--- | :--- |
| **Kiến trúc Solution** | ✅ Chuẩn mực hiện đại | Tách bạch 4 phân hệ rõ ràng: Core, API trung tâm, Web Admin SPA và WinForms máy trạm trạm cổng. |
| **Độ tin cậy & Kiểm thử** | ✅ Tuyệt đối (90/90 Passed) | **90 unit tests** bao phủ toàn bộ API, Services, Helpers, Controller Mock và Workflow Switch Dispatcher, chạy hoàn tất trong ~4 giây. |
| **Tính linh hoạt triển khai** | ✅ Đa đối tượng & Động 1-4 làn | Hỗ trợ mọi quy mô cổng từ 1 làn đến 4 làn, phục vụ đồng thời cả ô tô, xe máy và người đi bộ qua turnstile ([ADR 0040](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0040-multi-target-lane-architecture-and-tuple-pattern-matching.md)). |
| **Hiệu năng & Tiêu thụ RAM** | ✅ < 250MB RAM ổn định | Luồng video native Direct Preview qua `CHCNetSDK` giải phóng bộ nhớ managed .NET, triệt tiêu nguy cơ sập ứng dụng x86 32-bit. |
| **Tính toàn vẹn dữ liệu** | ✅ Referential Integrity | Áp dụng chính sách Chặn Xóa Toàn Diện (`409 Conflict`), bảo vệ dữ liệu cha-con và nhật ký kiểm toán bất biến `AuditLog`. |
| **Tài liệu & Mô hình nghiệp vụ** | ✅ Đồng bộ 100% | Từ điển thuật ngữ [CONTEXT.md](file:///c:/Users/ADMIN/source/repos/HPParking/CONTEXT.md), hệ thống 40 quyết định kiến trúc [docs/adr/](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/) và tài liệu kỹ thuật phản ánh chuẩn xác 100% nghiệp vụ và mã nguồn thực tế. |
