# 0012: Kiến Trúc Phân Hệ Nhiều Cổng Nhà Máy, Tự Động Định Danh Trạm Qua Hardware Fingerprint & Đồng Nhất Kho Lưu Trữ Generic IRepository

Hệ thống nâng cấp kiến trúc vận hành cho nhà máy công nghiệp nhiều cổng (Multi-Gate Architecture): tự động định danh cổng làm việc của trạm máy tính qua mã phần cứng (`HardwareFingerprint.GetMachineCode()`), phân vùng tải và cấu hình làn xe theo cổng (`Gate_Code`), chia sẻ kho lưu trữ hình ảnh qua đường dẫn mạng LAN dùng chung (UNC / NAS) phục vụ đối soát liên cổng (Cross-Gate), và đồng nhất tầng dữ liệu sử dụng generic `IRepository<T>` cho toàn bộ các thực thể bao gồm cả `LicenseInfo`.

## Ngữ cảnh

Trong các nhà máy công nghiệp hoặc khu liên hợp quy mô lớn, hạ tầng kiểm soát phương tiện thường được bố trí tại nhiều cổng vật lý khác nhau (ví dụ: Cổng 1, Cổng 2, Cổng Phụ, Cổng Xuất Nhập Hàng) cách xa nhau hàng trăm mét hoặc vài kilômét. Tuy nhiên:

1. **Xung đột nạp và cấu hình làn giữa các trạm:**
   - Phiên bản trước đây của `FrmMain` nạp tất cả các làn có trong cơ sở dữ liệu MongoDB mà không phân biệt trạm đang chạy ở cổng nào.
   - Khi kỹ thuật viên mở `FrmConfigManager` để cấu hình camera hoặc controller, phần mềm nạp lại toàn bộ cấu hình các làn của hệ thống, dẫn đến rủi ro cấu hình đè hoặc xung đột thiết bị phần cứng giữa các trạm ở cổng khác nhau.
2. **Luồng xe liên cổng (Cross-Gate Traffic Flow):**
   - Xe của công nhân viên hoặc khách có thể đi vào ở Cổng A nhưng đi ra ở Cổng B.
   - Nếu trạm Cổng B không truy cập được hình ảnh chụp xe lúc vào tại Cổng A (do lưu cục bộ trên ổ đĩa trạm Cổng A), bảo vệ tại Cổng B không thể đối soát biển số và hình ảnh thực tế, phá vỡ chính sách an ninh đối soát vào/ra theo ADR 0011.
3. **Cân nhắc phạm vi phân quyền quản trị (YAGNI):**
   - Việc xây dựng hệ thống phân quyền phức tạp (Role-Based Access Control - RBAC) và chia nhỏ nhiều bộ cài đặt ứng dụng WinForms khác nhau cho từng cổng là giải pháp cồng kềnh, khó bảo trì và over-engineering.
   - Cần một giải pháp tinh gọn: Duy nhất một bộ cài đặt WinForms đồng nhất; trạm tự động nhận biết cổng vật lý tương ứng dựa vào phần cứng máy tính và chỉ hiển thị/cho phép thao tác trên các làn thuộc cổng đó.
4. **Bất đồng nhất ở tầng lưu trữ dữ liệu (Repository Layer):**
   - Toàn bộ các thực thể mới (`Company`, `Department`, `Contractor`, `Gate`) đều kế thừa `BaseEntity` và vận hành qua `IRepository<T>` / `MongoRepository<T>`.
   - Riêng `LicenseRepository` trước đây lại là một class độc lập kế thừa trực tiếp từ interface riêng `ILicenseRepository`, gây phân mảnh mã nguồn và khó khăn khi cấu hình Dependency Injection cũng như viết Unit Test.

## Quyết định

1. **Bổ sung Thực thể Cổng `Gate` & Cơ chế Zero-Touch Hardware Binding:**
   - Tạo thực thể `Gate` kế thừa `BaseEntity` với các trường: `Code` (mã cổng), `Name` (tên cổng), `Description`, `MachineCode` (mã phần cứng máy trạm điều khiển), `IsActive`.
   - Khi trạm khởi động (`FrmMain_Load`), hệ thống tự động đọc mã máy thông qua `HardwareFingerprint.GetMachineCode()` và tra cứu trong cơ sở dữ liệu qua `IRepository<Gate>`.
   - Nếu tìm thấy cổng có `MachineCode` tương ứng và đang kích hoạt (`IsActive == true`), hệ thống xác định cổng hiện tại (`_currentGate`). Nếu chưa cấu hình, trạm hiển thị cảnh báo hướng dẫn liên kết máy với cổng.

2. **Phân vùng nạp và cấu hình Làn theo Cổng (`GateId`):**
   - Mỗi cổng quản lý các làn kiểm soát vật lý trực thuộc, hỗ trợ cấu hình động từ 1 đến 4 làn cho cả phương tiện cơ giới lẫn người đi bộ theo [ADR 0040](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0040-multi-target-lane-architecture-and-tuple-pattern-matching.md).
   - Thực thể `Gate` lưu giữ cả `Id` (ObjectId), `Code` (mã viết tắt/mã nghiệp vụ ngắn như `GATE_01`, `C1`) và `Name` (tên hiển thị).
   - Thực thể `Lane` liên kết với `Gate` thông qua khóa ngoại bất biến **`GateId`** trỏ tới `Gate.Id`. Cơ chế này đảm bảo khi người dùng đổi mã `Code` hoặc tên `Name` của Cổng thì toàn bộ các làn xe vẫn giữ nguyên liên kết toàn vẹn mà không cần cập nhật lại bảng `Lane`.
   - Sử dụng generic `IRepository<Lane>` (`MongoRepository<Lane>`) với `FindAsync(x => x.GateId == gateId && x.IsActive)`. Khi `FrmMain` khởi tạo, phần mềm chỉ nạp 4 làn thuộc `_currentGate.Id` để điều khiển và hiển thị luồng xe.
   - Khi kỹ thuật viên đăng nhập F1 vào `FrmConfigManager`, định danh `gateId` được truyền xuyên suốt qua `UcLanMotoManager` và `UcLanCarManager`.
   - Các UserControl chỉ nạp cấu hình và lưu lại cấu hình với `GateId` cố định của cổng đó, ngăn chặn hoàn toàn rủi ro sửa nhầm hoặc ghi đè làn của cổng khác.

3. **Lưu trữ hình ảnh dùng chung qua LAN UNC Path / NAS:**
   - Đường dẫn lưu trữ ảnh chụp phương tiện (`PathImage`) trong `AppConfig` được trỏ tới thư mục mạng chia sẻ tập trung trong mạng nội bộ (ví dụ: `\\192.168.1.100\HPParkingImages` hoặc NAS máy chủ).
   - Khi xe vào tại Cổng A, ảnh chụp biển số và toàn cảnh được lưu trực tiếp vào ổ đĩa mạng dùng chung.
   - Khi xe ra tại Cổng B, `ParkingWorkflowService` nạp bản ghi lượt vào từ MongoDB và đọc ảnh vào từ đường dẫn mạng chung, hiển thị trực quan lên giao diện Cổng B phục vụ đối soát an ninh.

4. **Đồng nhất hóa Tầng Dữ liệu qua Generic `IRepository<T>`:**
   - Loại bỏ hoàn toàn `ILicenseRepository`, `LicenseRepository`, `ILaneRepository`, `LaneRepository`.
   - Thay thế bằng generic `IRepository<T>` (`MongoRepository<T>`) cho toàn bộ các thực thể `Lane`, `Device`, `Gate`, `LicenseInfo`, `Client`, `Company`, `Contractor`, `Department`. Kế thừa mọi ưu điểm về CRUD bất đồng bộ, tự động xử lý soft-delete, tracking thời gian sửa đổi của `BaseEntity`.
   - Refactor `LicenseManager`, `FrmMain`, `UcLanCarManager`, `UcLanMotoManager`, `FrmRegisterClient` nhận generic `IRepository<T>` qua DI.

## Hệ quả

- **Zero-Touch Provisioning:** Trạm máy tính tại mỗi cổng tự động kích hoạt đúng làn và thiết bị phần cứng của mình mà không cần người dùng chọn thủ công ở mỗi phiên làm việc.
- **Cách ly an toàn cấu hình phần cứng:** Nhân viên bảo vệ hoặc kỹ thuật viên tại một cổng không thể can thiệp hoặc gây ảnh hưởng đến phần cứng của các cổng còn lại.
- **Hỗ trợ luồng xe liên cổng hoàn hảo:** Kiểm soát an ninh đối soát vào/ra chặt chẽ (ADR 0011) hoạt động xuyên suốt giữa các cổng nhờ hạ tầng ảnh mạng chia sẻ chung.
- **Kiến trúc mã nguồn đồng nhất & dễ bảo trì:** Tầng Repository loại bỏ code trùng lặp, 100% các thực thể tuân theo chuẩn Generic Repository Pattern, tối ưu hóa viết unit test và mocking.
