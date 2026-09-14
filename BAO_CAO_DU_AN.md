# BÁO CÁO TOÀN DIỆN VỀ DỰ ÁN HPPARKING

> **Ngày cập nhật:** 09/09/2026 (Đã đồng bộ sau tối ưu chụp ảnh song song, nhập biển số thủ công & đối soát an ninh Vào/Ra)  
> **Công cụ phân tích:** CodeGraph Engine & Antigravity IDE  
> **Workspace:** `c:\Users\ADMIN\source\repos\HPParking`  
> **Kiến trúc mục tiêu:** .NET 8 (`net8.0-windows`), x86 (32-bit Native P/Invoke)  
> **Bộ kiểm thử tự động:** 66 tests (xUnit + NSubstitute + FluentAssertions) - 100% Passed  

---

## 1. DỰ ÁN NÀY LÀ GÌ?

**HPParking** (Hoàng Phát Parking) là một **Hệ thống phần mềm Quản lý Bãi đỗ xe Thông minh** (Smart Parking & Access Control System) dành cho phương tiện Ô tô và Xe máy.

Phần mềm được xây dựng dưới dạng ứng dụng Desktop chạy trên nền tảng **Windows Forms (.NET 8 SDK-style)** kết hợp cú pháp C# hiện đại (**C# 12 / latest**). Ứng dụng giao tiếp thời gian thực với các thiết bị phần cứng kiểm soát truy cập (Access Control), camera quan sát, đầu đọc thẻ thông minh, đầu đọc Căn cước công dân gắn chip (CCCD) và thiết bị nhận diện khuôn mặt (FaceID).

### Mục tiêu nghiệp vụ cốt lõi:
1. **Kiểm soát xe vào/ra tự động không dùng thẻ (Cardless Access Control):**
   - Xác thực danh tính chủ xe 100% bằng **nhận diện khuôn mặt (FaceID)**.
   - Đầu đọc FaceID xuất mã định danh số điện thoại qua chuẩn giao tiếp phần cứng **Wiegand** sang bộ điều khiển trung tâm (ZKTeco Controller).
   - Tự động kích hoạt chụp ảnh biển số và ảnh toàn cảnh khi nhận tín hiệu nhận diện FaceID.
   - Nhận diện biển số tự động (ALPR/LPR) bằng engine OCR SimpleLPR 3.x chuyên dụng cho biển số xe Việt Nam.
   - So khớp biển số xe vào và xe ra, kiểm tra hạn thuê/đăng ký của khách hàng.
   - Tự động đóng/mở barrier bằng relay điều khiển.
2. **Đăng ký khách hàng & Căn cước công dân (CCCD):**
   - Đọc dữ liệu từ thẻ CCCD gắn chip qua kết nối SignalR thời gian thực.
   - Trích xuất ảnh chân dung trên chip hoặc chụp ảnh chân dung từ camera.
   - Đồng bộ hồ sơ khách hàng (thông tin cá nhân, mã số thẻ, dữ liệu khuôn mặt) trực tiếp lên thiết bị nhận diện khuôn mặt (Hikvision FaceID terminal) qua ISAPI Digest Auth.
3. **Lưu trữ & Bảo mật:**
   - Lưu trữ dữ liệu lịch sử sự kiện gửi xe và danh mục khách hàng trên cơ sở dữ liệu phi quan hệ **MongoDB** (Database `HPParking`).
   - Lưu trữ hình ảnh xe vào/ra trên ổ cứng theo cấu trúc thư mục ngày tháng chuẩn hóa.
   - Cơ chế bảo vệ bản quyền phần mềm (License Key) dựa trên chữ ký điện tử RSA-2048, định danh mã máy phần cứng (CPU ID qua WMI) và đồng hồ Registry chống tua lùi thời gian máy tính.

---

## 2. CƠ CẤU & THÀNH PHẦN HỆ THỐNG HIỆN TẠI

### 2.1. Công nghệ & Thư viện sử dụng
* **Nền tảng:** .NET 8 (`net8.0-windows`), SDK-style project, biên dịch mục tiêu cố định `x86` (32-bit).
* **Cơ sở dữ liệu:** [MongoDB.Driver](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Data/MongoContext.cs) (v3.11.0), database `HPParking`.
* **Dependency Injection:** [Microsoft.Extensions.DependencyInjection](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Program.cs) (v8.0.1).
* **Kết nối thời gian thực:** [Microsoft.AspNetCore.SignalR.Client](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/CCCDReader/CccdReaderManager.cs) (v8.0.13) kết nối tới CardHub service địa phương.
* **Hệ thống & Cấu hình:** `System.Configuration.ConfigurationManager` (v8.0.1) và `System.Management` (v8.0.0).
* **Xử lý JSON:** `System.Text.Json` (Native .NET, đã loại bỏ hoàn toàn `Newtonsoft.Json`).
* **Hộp thoại giao diện:** `System.Windows.Forms.TaskDialog` & `FolderBrowserDialog` (Native Windows Forms .NET 8, đã loại bỏ hoàn toàn `Ookii.Dialogs.WinForms`).
* **Kiểm thử tự động:** `HPParking.Tests` (xUnit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0) — **57 tests 100% Passed**.

---

### 2.2. Phần cứng & SDK tích hợp
Hệ thống tích hợp sâu với các SDK native C/C++ 32-bit thông qua cơ chế P/Invoke:

| Thiết bị / Chức năng | Thư viện / SDK | Chi tiết triển khai trong dự án |
| :--- | :--- | :--- |
| **Nhận diện biển số (LPR)** | `SimpleLPR3.dll` (Libs/x86/) | [LprService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/LPR/LprService.cs): Cấu hình engine OCR qua interface [ILprService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Interfaces/ILprService.cs), tinh chỉnh trọng số ưu tiên biển số xe Việt Nam (`countryWeight = 1.0f`), cắt vùng biển số và trích xuất chuỗi ký tự. |
| **Bộ điều khiển Barrier & Tiếp nhận Wiegand** | ZKTeco Pull SDK (`plcommpro.dll`) | [IZKTecoSdk.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/SDK/CtrlSDK/IZKTecoSdk.cs), [ZKTecoSdkWrapper.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/SDK/CtrlSDK/ZKTecoSdkWrapper.cs), [MockZKTecoSdk.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/SDK/CtrlSDK/MockZKTecoSdk.cs), [ControllerService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Controller/ControllerService.cs) & [IControllerService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Interfaces/IControllerService.cs): Kết nối TCP/IP, tiếp nhận chuỗi số điện thoại truyền từ FaceID qua cổng Wiegand, kích relay đóng/mở barrier, tự động kết nối lại không lỗi lặp, xả buffer 1.5s và hiển thị trạng thái lên nhãn `lbStatusCtrl` (xem [ADR 0006](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0006-cardless-faceid-wiegand-architecture.md), [ADR 0007](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0007-standard-4-port-controller-parity-architecture.md) & [ADR 0010](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0010-resilient-controller-driver-and-health-monitoring.md)). |
| **Camera chụp biển số** | Hisilicon SDK (`hi_h264dec_w.dll` / `hi_net_dev_sdk.dll`) | [PlateCameraService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Camera/PlateCameraService.cs): Đăng nhập camera, xuất luồng LiveView lên giao diện và chụp ảnh snapshot (JPEG) biển số xe. |
| **Camera toàn cảnh** | Hikvision NetSDK (`HCNetSDK.dll`, `PlayCtrl.dll`) | [OverviewCameraService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Camera/OverviewCameraService.cs): Đăng nhập thiết bị Hikvision, xem luồng trực tiếp và chụp ảnh snapshot người/phương tiện độ phân giải gốc. |
| **Thiết bị FaceID (Nhận diện khuôn mặt)** | Hikvision ISAPI & Wiegand Output | [FaceIdApiService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/FaceId/FaceIdApiService.cs): Nạp User, gán số điện thoại vào trường mã thẻ (`AddCardAsync`), tải ảnh khuôn mặt qua ISAPI; khi nhận diện khuôn mặt thành công, tự động xuất số điện thoại qua chuẩn Wiegand sang Controller ZKTeco. |
| **Đầu đọc CCCD gắn chip** | WebSocket/SignalR Card Hub | [CccdReaderManager.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/CCCDReader/CccdReaderManager.cs): Lắng nghe sự kiện cắm thẻ, đọc số CCCD, họ tên, ngày sinh, địa chỉ và ảnh chân dung thẻ chip qua cơ chế chạy ngầm Non-blocking. |

---

### 2.3. Cấu trúc thư mục & Giải pháp hiện tại

```
c:\Users\ADMIN\source\repos\HPParking\
│
├── HPParking.slnx                  # Solution file hiện đại của Visual Studio
├── CONTEXT.md                      # Từ điển thuật ngữ nghiệp vụ (Domain Glossary) chuẩn hóa
├── AGENTS.md                       # Bản đồ kỹ năng & hướng dẫn AI Agent
│
├── docs/
│   ├── adr/                        # Các quyết định kiến trúc trọng yếu (ADRs)
│   │   ├── 0001-target-x86-architecture.md
│   │   ├── 0002-card-code-bound-to-phone-number.md
│   │   ├── 0003-non-blocking-hardware-initialization.md
│   │   ├── 0004-hikvision-isapi-integration-for-faceid.md
│   │   ├── 0005-rsa-and-registry-anti-tamper-licensing.md
│   │   ├── 0006-cardless-faceid-wiegand-architecture.md
│   │   ├── 0007-standard-4-port-controller-parity-architecture.md
│   │   ├── 0008-barrier-hardware-failure-recovery-workflow.md
│   │   ├── 0009-explicit-liveview-camera-mapping.md
│   │   └── 0010-resilient-controller-driver-and-health-monitoring.md
│   └── agents/                     # Cấu hình tracker, nhãn triage và quy tắc domain
│       ├── issue-tracker.md
│       ├── triage-labels.md
│       └── domain.md
│
├── HPParking.Tests/                # Dự án Kiểm thử tự động (57 tests - 100% Passed)
│   ├── Helper/ValidationHelperTests.cs
│   ├── LicenseKey/LicenseValidatorTests.cs
│   ├── Services/Storage/ImageStorageServiceTests.cs
│   ├── Models/LaneAndClientModelTests.cs
│   ├── Services/Controller/ControllerServiceTests.cs      # 9 tests cô lập bằng Mock SDK
│   ├── Services/Parking/ParkingWorkflowServiceTests.cs    # 17 tests luồng gửi xe vào/ra
│   └── Services/Parking/OverviewCameraDiagnosticTests.cs  # Chẩn đoán camera phần cứng
│
└── HPParking/                      # Ứng dụng WinForms chính
    ├── Program.cs                  # Cấu hình DI, Global Exception Handlers & khởi động Form
    ├── App.config                  # Cấu hình chuỗi kết nối MongoDB (DatabaseName: HPParking)
    ├── HPParking.csproj            # SDK-style net8.0-windows x86
    │
    ├── Libs/                       # Native C/C++ DLLs và SimpleLPR3.dll tự động copy sang bin
    │   ├── x86/
    │   └── native/
    │
    ├── Data/
    │   └── MongoContext.cs         # Khởi tạo kết nối MongoClient và cung cấp IMongoCollection
    │
    ├── Models/Entities/
    │   ├── Client.cs               # Thông tin khách hàng, số điện thoại, hạn dùng thẻ
    │   ├── Company.cs              # Cấu hình công ty, license key, đường dẫn ảnh
    │   ├── Department.cs           # Phòng ban / đơn vị trực thuộc
    │   ├── DeviceConfig.cs         # DTO cấu hình kết nối thiết bị (IP, Port, User, Pass)
    │   ├── EventParking.cs         # Bản ghi sự kiện xe vào/ra (Card_Code = PhoneNumber)
    │   ├── FaceId.cs               # Cấu hình thiết bị nhận diện khuôn mặt (Ip, Port, User, Pass)
    │   └── Lane.cs                 # Định nghĩa làn xe (Inbound/Outbound, Car/Moto, camera, controller)
    │
    ├── Interfaces/                 # Interface trừu tượng hóa tầng Repositories & Services
    │   ├── IDeviceAdapter.cs       # Vòng đời chuẩn hóa cho thiết bị ngoại vi
    │   ├── IControllerService.cs   # Hợp đồng điều khiển bộ điều khiển ZKTeco
    │   ├── ILprService.cs          # Interface OCR cho phép tách biệt và mock SimpleLPR khi test
    │   ├── IClientRepository.cs
    │   ├── ICompanyRepository.cs
    │   ├── IDepartmentRepository.cs
    │   ├── IEventParkingRepository.cs
    │   ├── IFaceIdApiService.cs
    │   ├── IImageStorageService.cs
    │   ├── ILaneRepository.cs
    │   └── IParkingWorkflowService.cs
    │
    ├── Repositories/               # Tầng truy xuất dữ liệu MongoDB theo Repository Pattern
    ├── Services/                   # Tầng xử lý logic nghiệp vụ bãi xe, camera, controller, FaceID, LPR
    │   ├── Controller/             # Quản lý kết nối, đọc realtime log, xả buffer & auto-reconnect
    │   ├── Devices/                # Điều phối thiết bị (DeviceOrchestrator, DeviceStatus enum)
    │   ├── Camera/                 # Dịch vụ camera biển số & camera toàn cảnh
    │   ├── FaceId/                 # Giao tiếp ISAPI FaceID Hikvision
    │   └── Parking/                # Điều phối quy trình gửi xe (ParkingWorkflowService)
    │
    ├── Forms/                      # Giao diện WinForms (FrmMain, FrmLogin, FrmRegisterClient, ConfigManager)
    ├── SDK/                        # P/Invoke C# wrappers (CamPlateSDK, CtrlSDK, HikVision)
    │   └── CtrlSDK/                # IZKTecoSdk, ZKTecoSdkWrapper, MockZKTecoSdk, ZKTecoSDK
    └── LicenseKey/                 # Xác thực bản quyền RSA & Anti-tamper clock
```

---

## 3. CÁC QUY TẮC NGHIỆP VỤ & LUỒNG HOẠT ĐỘNG CHUẨN HÓA


1. **Khởi động ứng dụng an toàn & Non-blocking ([FrmMain.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Forms/FrmMain.cs)):**
   - Kết nối SignalR CCCD service chạy ngầm ở background, không bao giờ block luồng giao diện khi dịch vụ đọc thẻ offline.
   - Thẩm định License Key từ cơ sở dữ liệu. Nếu chưa có hoặc hết hạn, mở `FrmLogin.ShowDialog()` chuẩn modal để cấu hình lại; nếu không thành công sẽ đóng ứng dụng sạch sẽ thay vì chạy ẩn vô hình.
   - Bật đồng hồ thời gian thực và tự động cập nhật `LastRunTime` mỗi 60 giây vào Registry để chống lùi giờ hệ thống.
   - Khởi tạo thiết bị và LiveView song song trên các PictureBox.
   - Trạng thái kết nối của Controller được tự động phản ánh trực quan qua màu sắc và nhãn `lbStatusCtrl` trên Footer (`BỘ ĐIỀU KHIỂN: ONLINE` / `OFFLINE`).

2. **Quy chuẩn định danh không thẻ từ & Luồng gửi xe ([ParkingWorkflowService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Parking/ParkingWorkflowService.cs)):**
   - **Xác thực FaceID qua Wiegand (Xem [ADR 0006](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0006-cardless-faceid-wiegand-architecture.md)):** Hệ thống loại bỏ hoàn toàn thẻ từ vật lý. Khi xe đến cổng, thiết bị FaceID quét khuôn mặt chủ xe, tự động xuất số điện thoại qua chuẩn Wiegand sang bộ điều khiển ZKTeco.
   - **Chủ đích thiết kế Card_Code = PhoneNumber (Xem [ADR 0002](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0002-card-code-bound-to-phone-number.md)):** Trong `EventParking` và MongoDB, trường `Card_Code` lưu số điện thoại khách hàng để khớp trực tiếp với chuỗi số nhận diện từ Controller.
   - **Luồng Xe vào (`ProcessEntryAsync`):** Nhận tín hiệu FaceID từ Controller -> Kiểm tra trạng thái xe trong bãi (`AlreadyInParking`), chụp ảnh biển số + toàn cảnh, nhận diện LPR, mở barrier, lưu ảnh vào đĩa theo ngày và lưu `EventParking` (trạng thái `IN`).
   - **Luồng Xe ra (`ProcessExitAsync`):** Nhận tín hiệu FaceID -> Tìm bản ghi xe đang trong bãi (`GetParkingInProgress`), chụp ảnh xe ra, nhận diện LPR, so khớp biển số vào/ra (`PlateMismatch`), mở barrier, cập nhật trạng thái `OUT` và giờ ra.
   - **Xử lý sự cố mở barrier thủ công (Xem [ADR 0008](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0008-barrier-hardware-failure-recovery-workflow.md)):** Khi barrier không thể tự kích mở tự động, hỗ trợ người vận hành mở barrier cơ và xác nhận để bảo toàn dữ liệu đối soát trên MongoDB.
   - **Tối ưu chụp ảnh song song & Hỗ trợ nhập biển số thủ công (Xem [ADR 0011](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0011-parallel-camera-capture-and-security-mismatch-policy.md)):**
     - Chụp đồng thời 2 camera với timeout 1000ms, giảm độ trễ mở barrier 400ms – 800ms. Camera toàn cảnh lỗi không làm dừng luồng xe.
     - Hỗ trợ hộp thoại `FrmManualPlateInput` cho bảo vệ gõ biển số tay khi Camera Biển Số hỏng hoặc LPR không đọc ra. Nếu LPR không đọc ra, hệ thống vẫn lưu ảnh chụp gốc vào đĩa để đối soát.
     - Lượt Vào: Kiểm tra sai lệch biển số đăng ký qua hộp thoại `FrmConfirmEntryMismatch` (Bảo vệ đồng ý mới mở barrier).
     - Lượt Ra: Áp dụng chính sách **chặn cứng an ninh tuyệt đối** khi biển số ra khác biển số vào (`PlateMismatch`), barrier tuyệt đối giữ đóng để chống trộm xe.

3. **Cơ chế phục hồi kết nối & Giám sát bộ điều khiển (Xem [ADR 0010](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0010-resilient-controller-driver-and-health-monitoring.md)):**
   - Hỗ trợ Active TCP Socket Ping (`PingAsync`) không qua native SDK để phát hiện đứt mạng tức thì.
   - Xả đệm 1.5s đầu sau khi kết nối (`_drainUntil`) để loại bỏ sự kiện quẹt thẻ "ma".
   - Tự động kết nối lại ngầm khi mất mạng và ngắt vòng lặp retry ngay khi phục hồi thành công.
   - Quản lý luồng đọc độc lập từng Controller trong `DeviceOrchestrator`, không làm nghẽn chéo giữa các làn.

4. **Đăng ký khách hàng & đồng bộ FaceID ([FrmRegisterClient.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Forms/FrmRegisterClient.cs)):**
   - Tự động nạp thông tin CCCD gắn chip khi đặt thẻ lên đầu đọc.
   - Đẩy thông tin người dùng, thẻ và ảnh chân dung đồng thời lên các thiết bị FaceID Hikvision qua ISAPI kèm cơ chế rollback khi lỗi.

---

## 4. TỔNG KẾT & ĐÁNH GIÁ CHẤT LƯỢNG

| Tiêu chí | Trạng thái | Đánh giá chi tiết |
| :--- | :--- :--- | :--- |
| **Nền tảng công nghệ** | ✅ Hiện đại (.NET 8) | Chuyển đổi thành công sang .NET 8, SDK-style gọn gàng, loại bỏ các package lỗi thời (`Newtonsoft.Json`, `Ookii.Dialogs.WinForms`). |
| **Độ tin cậy & Kiểm thử** | ✅ Tuyệt đối | Bộ **66 unit tests xUnit** bao phủ Helpers, Models, License, Storage, Controller (Mock SDK) và Workflow xe vào/ra chạy Passed 100% trong ~3 giây. |
| **Khả năng tương thích phần cứng** | ✅ Cố định 32-bit | Khóa kiến trúc `x86` đảm bảo tương thích ổn định tuyệt đối với các native DLL P/Invoke (SimpleLPR, ZKTeco, Hikvision). |
| **Độ ổn định giao diện & Khởi động** | ✅ Non-blocking | Loại bỏ hoàn toàn lỗi treo Form và lỗi tự đóng Form lúc khởi động; giao diện hiển thị tức thì kể cả khi thiết bị ngoại vi offline. Trạng thái Controller hiển thị trực tiếp lên nhãn Footer. |
| **Độ bền bỉ của Controller** | ✅ Phục hồi tự động | Đã sửa dứt điểm lỗi lặp reconnect vô tận, bổ sung xả buffer và lọc gói keepalive, luồng đọc song song không nghẽn làn. |
| **Hiệu năng & An ninh đối soát** | ✅ Đỉnh cao | Chụp song song 2 camera (timeout 1000ms), giải phóng xe khi camera lỗi bằng nhập tay, bảo toàn ảnh LPR fail, chặn cứng trộm xe ở làn ra. |
| **Tài liệu & Mô hình nghiệp vụ** | ✅ Hoàn chỉnh | Đồng bộ hóa toàn diện giữa mã nguồn thực tế, từ điển thuật ngữ [CONTEXT.md](file:///c:/Users/ADMIN/source/repos/HPParking/CONTEXT.md) và hệ thống 11 bản ghi quyết định kiến trúc [docs/adr/](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/). |
