# HPParking System Context

Hệ thống quản lý kiểm soát xe vào/ra bãi đỗ xe thông minh không dùng thẻ từ (Cardless Smart Parking & Access Control System) sử dụng công nghệ nhận diện khuôn mặt (FaceID), truyền định danh số điện thoại qua giao tiếp phần cứng Wiegand tới bộ điều khiển trung tâm, kết hợp nhận diện biển số tự động (LPR) và điều khiển barrier.

## Language

### Core Entities

**Client**:
Hồ sơ khách hàng hoặc nhân sự đã đăng ký phương tiện và khuôn mặt trong hệ thống để được cấp quyền ra vào bãi xe.
_Avoid_: User, Customer, Account, Driver

**ParkingSession**:
Bản ghi phiên gửi xe theo dõi toàn bộ chu trình từ thời điểm phương tiện vào bãi đến khi rời bãi (chuẩn hóa theo kiến trúc PhuXuanParkingSystem, trước đây là EventParking).
_Avoid_: EventParking, Ticket, Transaction, HistoryLog

**Gate**:
Cổng vật lý độc lập tại khuôn viên hoặc nhà máy (ví dụ: Cổng 1, Cổng 2, Cổng Phụ), được điều khiển bởi một trạm máy tính (`MachineCode`) và quản lý một nhóm gồm 4 làn xe chuẩn (1 Moto Vào, 1 Moto Ra, 1 Car Vào, 1 Car Ra) tuân thủ ADR 0007.
_Avoid_: Door, EntryPoint, Checkpoint, Portal

**Lane**:
Làn kiểm soát vật lý trực thuộc một Cổng (`Gate`) tại bãi xe, liên kết với đầu đọc FaceID, camera chụp biển số, camera toàn cảnh và bộ điều khiển barrier.
_Avoid_: BarrierLane, Channel

**Company**:
Thông tin đơn vị quản lý vận hành hệ thống bãi xe, lưu trữ cấu hình bản quyền phần mềm (License Key) và thư mục lưu trữ ảnh.
_Avoid_: Organization, Agency, Tenant

**Department**:
Phòng ban trực thuộc công ty hoặc cơ quan mà Client đang công tác.
_Avoid_: Division, Group, Team

**User**:
Tài khoản người dùng nội bộ (nhân viên vận hành, quản lý hoặc quản trị viên) đăng nhập vào hệ thống phần mềm HPParking để thao tác nghiệp vụ và quản trị.
_Avoid_: Client, Customer, Account, Driver

**Device**:
Thiết bị phần cứng ngoại vi (Camera chụp ảnh, Bộ điều khiển Barie, Máy nhận diện FaceID hoặc thiết bị phụ trợ) kết nối mạng LAN phục vụ kiểm soát bãi xe, được định danh qua mã duy nhất và địa chỉ mạng.
_Avoid_: Hardware, Peripheral, Machine, Equipment

**Contractor**:
Đơn vị nhà thầu, đối tác thi công hoặc nhà cung cấp dịch vụ bên ngoài hoạt động trong khuôn viên bãi xe, có nhân sự (`Client`) đăng ký phương tiện và quyền ra vào.
_Avoid_: Vendor, Partner, Supplier, Subcontractor, ConstructionUnit

### Domain Concepts & Identifiers (Mô hình Không Thẻ Từ)

**Cardless Access Control (Kiểm soát không dùng thẻ)**:
Mô hình vận hành loại bỏ hoàn toàn thẻ từ vật lý; việc xác thực danh tính tại làn xe được thực hiện 100% bằng nhận diện khuôn mặt qua FaceID Terminal.
_Avoid_: RFID System, SmartCard System, Quẹt thẻ vật lý

**PhoneNumber**:
Số điện thoại liên lạc của Client. Là **khóa định danh duy nhất (Single Source of Truth)** trong thực thể `Client`, bộ nhớ của đầu đọc FaceID và luồng xử lý gửi xe `ParkingWorkflowService`. Hệ thống đã loại bỏ hoàn toàn trường `Card_Code` trong `Client`; trong `ParkingSession`, trường `Card_Code` vẫn được duy trì đồng bộ với `PhoneNumber` nhằm tương thích ngược với dữ liệu lịch sử.
_Avoid_: Card_Code trong Client, RfidCode, CardNumber, MagneticCode

**Code**:
Số Căn cước công dân (CCCD) hoặc số định danh cá nhân của Client được đọc từ chip CCCD khi làm thủ tục đăng ký ban đầu.
_Avoid_: CitizenId, NationalId, IdentityCard, ID_Code

**Inbound Lane**:
Làn xe dành riêng cho phương tiện đi vào bãi (`Lane.Type % 2 != 0`), kích hoạt khi FaceID nhận diện khuôn mặt vào, chụp ảnh biển số vào và mở barrier vào.
_Avoid_: EntryGate, CheckInLane, InLane

**Outbound Lane**:
Làn xe dành riêng cho phương tiện rời khỏi bãi (`Lane.Type % 2 == 0`), kích hoạt khi FaceID nhận diện khuôn mặt ra, đối soát biển số vào/ra và mở barrier ra.
_Avoid_: ExitGate, CheckOutLane, OutLane

**Motorcycle Lane (Làn Xe Máy)**:
Làn kiểm soát vật lý dành cho xe máy, được định danh phần cứng qua cổng đọc số LẺ (`Lane.InputReader % 2 != 0`). Chuẩn trạm kiểm soát sử dụng Cổng 1 (Vào) và Cổng 3 (Ra) trên bộ điều khiển ZKTeco C3-400.
_Avoid_: MotoLane, BikeLane, Làn 2 bánh

**Car Lane (Làn Ô Tô)**:
Làn kiểm soát vật lý dành cho ô tô, được định danh phần cứng qua cổng đọc số CHẴN (`Lane.InputReader % 2 == 0`). Chuẩn trạm kiểm soát sử dụng Cổng 2 (Vào) và Cổng 4 (Ra) trên bộ điều khiển ZKTeco C3-400.
_Avoid_: AutoLane, OtoLane, Làn 4 bánh

### Workflow States & Business Rules

**Parking In Progress**:
Trạng thái phương tiện đang đỗ trong bãi (bản ghi `ParkingSession` có `Status = "IN"` và `StatusInOut = false`).
_Avoid_: ActiveParking, CurrentSession

**Already In Parking**:
Tình huống vi phạm nghiệp vụ khi phương tiện đang có một phiên gửi xe chưa hoàn thành trong bãi nhưng khuôn mặt lại được quét đòi vào tiếp.
_Avoid_: DuplicateEntry, DoubleCheckIn

**Plate Mismatch**:
Tình huống cảnh báo an ninh khi biển số xe camera nhận diện được tại làn ra không trùng khớp với biển số xe đã ghi nhận lúc vào.
_Avoid_: WrongPlate, InvalidPlateMatch

**Manual Barrier Release**:
Quy trình xử lý ngoại lệ khi phần mềm không thể tự động kích mở thanh chắn Barrier qua bộ điều khiển: cho phép nhân viên vận hành thử lại (Retry) hoặc dùng remote cơ/nút bấm tay để mở barrier, đồng thời xác nhận phương tiện đã qua cổng để hệ thống lưu đầy đủ bản ghi `ParkingSession` vào MongoDB phục vụ đối soát.
_Avoid_: ForceOpen, BypassBarrier, OverrideGate

**Anti-tamper Clock**:
Cơ chế bảo vệ bản quyền: định kỳ mỗi 60 giây cập nhật mốc thời gian chạy mới nhất (`LastRunTime`) vào Windows Registry để ngăn chặn hành vi lùi giờ hệ thống nhằm gian lận thời hạn phần mềm.
_Avoid_: TimeWatcher, ClockGuard

**MachineCode**:
Chuỗi mã băm định danh phần cứng máy tính (lấy từ CPU ID qua WMI) được ký kèm trong License Key để khóa ứng dụng với máy trạm được cấp phép.
_Avoid_: HardwareId, MachineFingerprint

**Zero-Touch Hardware Binding**:
Cơ chế tự động định danh và liên kết trạm máy tính với Cổng tương ứng (`Gate`) lúc khởi động ứng dụng thông qua mã phần cứng `HardwareFingerprint.GetMachineCode()`, tự động nạp và chỉ điều khiển các làn thuộc cổng đó mà không cần cấu hình thủ công mỗi lần mở máy.
_Avoid_: ManualStationSelect, StationConfigIni

**Cross-Gate Vehicle Flow (Lưu thông liên cổng)**:
Quy trình nghiệp vụ cho phép phương tiện đi vào tại một Cổng (Gate A) và đi ra tại một Cổng khác (Gate B). Dữ liệu hình ảnh chụp xe lúc vào được lưu trữ tại thư mục mạng dùng chung (`\\SERVER_IP\HPParkingImages` hoặc NAS), giúp trạm tại Cổng B truy xuất hình ảnh phục vụ đối soát an ninh trực quan theo ADR 0011.
_Avoid_: SingleGateOnly, LocalStorageOnly

**Password Policy**:
Quy chuẩn xác thực tài khoản; mật khẩu hệ thống có độ dài tối thiểu 6 ký tự, không bắt buộc độ phức tạp về chữ hoa hay số để thuận tiện cho thao tác vận hành tại bãi xe.
_Avoid_: ComplexPasswordPolicy, PasswordRegex

**Admin Password Override**:
Đặc quyền của vai trò Admin cho phép cập nhật mật khẩu trực tiếp mà không cần cung cấp mật khẩu cũ hiện tại.
_Avoid_: BypassPassword, ForceReset

### Hardware, Protocol & Peripherals

**FaceID Terminal**:
Thiết bị nhận diện khuôn mặt chuyên dụng (Hikvision) lắp tại làn xe. Khi nhận diện thành công khuôn mặt của chủ xe, thiết bị tự động xuất chuỗi số điện thoại qua chuẩn Wiegand sang bộ điều khiển ZKTeco.
_Avoid_: FaceScanner, BiometricDevice, Đầu đọc thẻ

**Wiegand Interface**:
Giao thức truyền thông phần cứng dạng xung nhị phân kết nối trực tiếp từ đầu đọc FaceID sang bộ điều khiển ZKTeco, dùng để truyền số điện thoại (được lưu ở trường CardNo) ngay khi nhận diện khuôn mặt thành công.
_Avoid_: Serial, RS485, USB (đối với luồng tín hiệu kích hoạt sự kiện)

**Barrier**:
Thanh chắn kiểm soát phương tiện tự động đóng/mở thông qua relay của bộ điều khiển ZKTeco khi nhận được tín hiệu hợp lệ.
_Avoid_: Gate, BoomBarrier, Door

**ZKTeco Controller**:
Bộ điều khiển trung tâm 4 cổng (ZKTeco C3-400) kết nối qua giao thức TCP/IP, tiếp nhận tín hiệu số điện thoại qua chuẩn Wiegand từ đầu đọc FaceID và kích hoạt relay mở Barrier. Chuẩn hóa quy hoạch 4 cổng: Cổng 1 (Xe máy Vào), Cổng 2 (Ô tô Vào), Cổng 3 (Xe máy Ra), Cổng 4 (Ô tô Ra).
_Avoid_: CentralBox, GateController, IOBoard, C3-200

**LPR (License Plate Recognition)**:
Quy trình nhận diện chuỗi ký tự biển số xe tự động từ ảnh chụp camera độ phân giải cao thông qua engine OCR SimpleLPR3 với trọng số ưu tiên biển số Việt Nam.
_Avoid_: ANPR, OCR Reader, PlateScanner

**CCCD Reader**:
Đầu đọc thẻ Căn cước công dân gắn chip kết nối thời gian thực qua SignalR Hub service chạy nền ở cổng local 5000, chỉ dùng tại bàn đăng ký cư dân ban đầu ([FrmRegisterClient.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Forms/FrmRegisterClient.cs)).
_Avoid_: ChipReader, CardScanner

**Device Orchestrator**:
Hệ thống điều phối tập trung quản lý vòng đời, kết nối đồng thời và lắng nghe sự kiện TCP/IP từ bộ điều khiển ZKTeco và các camera.
_Avoid_: DeviceManager, HardwareController

**LiveView**:
Luồng xuất hình ảnh trực tiếp từ camera biển số (hàng giữa) và camera toàn cảnh (hàng trên) lên các khung PictureBox giám sát trên giao diện chính. Luồng hiển thị được liên kết theo cơ chế ánh xạ tường minh (Explicit Semantic Mapping) bám sát đúng vị trí làn xe và loại camera, loại bỏ hoàn toàn sự phụ thuộc vào thứ tự dữ liệu trong MongoDB.
_Avoid_: VideoStream, CameraFeed

**Device Adapter (`IDeviceAdapter`)**:
Giao diện adapter chuẩn hóa vòng đời kết nối (`IsConnected`), nhận luồng dữ liệu (`IsStreaming`), ngắt kết nối và kiểm tra cổng socket (`PingAsync`) áp dụng thống nhất cho mọi loại thiết bị ngoại vi (Camera, Controller, Barrier).
_Avoid_: HardwareWrapper, DeviceDriver, DeviceBridge

**Active Socket Ping**:
Cơ chế kiểm tra sức khỏe thiết bị phần cứng trong mạng LAN bằng cách mở kết nối TCP Socket trực tiếp tới IP:Port qua `TcpClient` với timeout ngắn. Cơ chế này được giới hạn duy nhất trong phạm vi ứng dụng máy trạm WinForms (`HPParking`) tại cổng bãi xe; hoàn toàn không áp dụng trên tầng REST API (`HPParking.Api`) để đảm bảo khả năng triển khai độc lập trên Điện toán đám mây theo ADR 0032.
_Avoid_: IcmpPing, CommandPing, SDKHeartbeat, ApiSocketPing

**Stale Buffer Draining**:
Cơ chế xả bỏ toàn bộ dữ liệu log đọc được trong 1.5 giây đầu tiên sau khi kết nối lại Controller ZKTeco, nhằm loại bỏ triệt để các sự kiện cũ còn tồn đọng trong bộ đệm thiết bị từ phiên trước.
_Avoid_: BufferFlush, ClearLog, DropCache

**Parallel Camera Capture**:
Cơ chế chụp ảnh đồng thời cả Camera Biển Số và Camera Toàn Cảnh bằng `Task.WhenAll` với giới hạn thời gian cứng 1000ms. Cho phép tiếp tục luồng xe nếu Camera Toàn Cảnh bị timeout/lỗi và tự động giải phóng tài nguyên ảnh trễ để chống rò rỉ GDI+ handles.
_Avoid_: SequentialCapture, SyncSnap

**Manual Plate Fallback**:
Quy trình dự phòng cho phép nhân viên bảo vệ gõ biển số xe trực tiếp trên hộp thoại phần mềm khi Camera Biển Số lỗi/timeout hoặc khi engine OCR LPR không nhận diện được ký tự. Khi LPR thất bại nhưng camera chụp được, ảnh biển số vẫn được lưu trữ nguyên vẹn vào đĩa để phục vụ kiểm toán đối soát.
_Avoid_: ForcePlate, ManualOverride, Bỏ qua biển số

**Strict Exit Lockout (Chặn Cứng Khi Ra)**:
Chính sách an ninh nghiêm ngặt tại cổng ra: nếu biển số xe ra không trùng khớp với biển số xe lúc vào (`LicensePlateOut != LicensePlateIn`) hoặc thông tin gửi xe không tồn tại trong bãi, hệ thống lập tức chặn cứng 100%, tuyệt đối không mở barrier tự động và hiển thị cảnh báo đỏ trên màn hình để nhân viên giữ xe lại làm việc trực tiếp.
_Avoid_: SoftMismatch, AutoBypassExit

**Plate & Avatar Dual-Display (Cặp Ô Biển Số & Avatar)**:
Bố cục hiển thị ảnh gồm 2 ô trong cụm thông tin xe của mỗi làn: ô trái hiển thị luân phiên ảnh chụp biển số xe (khi vào hiện biển số vào, khi ra hiện biển số ra) và ô phải hiển thị ảnh đại diện khuôn mặt (Avatar) của khách hàng từ hồ sơ hệ thống nếu có.
_Avoid_: InOutDualPlate, Hai ô biển số song song

### REST API & Remote Management Concepts

**HPParking.Api**:
Dịch vụ ASP.NET Core REST API (.NET 8 AnyCPU) độc lập trong cùng Solution, triển khai trên máy chủ trung tâm kết nối trực tiếp vào MongoDB và tái sử dụng 100% tầng dữ liệu `HPParking.Core` phục vụ Web Admin, Mobile App và bên thứ ba.
_Avoid_: EmbeddedApi, WinFormsApi, LocalStationApi

**Hybrid Authentication (Xác thực Kép)**:
Cơ chế bảo mật kết hợp giữa JWT Bearer Token (dành cho người dùng Web Admin / Mobile dựa trên thực thể `User` và `UserRole` trong `HPParking.Core`) và API Key qua header `X-API-KEY` (dành cho hệ thống bên thứ ba).
_Avoid_: BasicAuth, OpenApiNoAuth, HardcodedSecret

**Explicit FaceID Sync (Đồng bộ FaceID Tách Biệt)**:
Quy trình nạp khuôn mặt và số điện thoại lên thiết bị FaceID được tách biệt thành endpoint chuyên trách (`POST /api/clients/{id}/sync-faceid`), độc lập với các thao tác CRUD Client nhằm đảm bảo thời gian phản hồi API dưới 100ms và không bị ảnh hưởng bởi độ trễ kết nối thiết bị mạng LAN.
_Avoid_: SyncOnSave, BlockingDeviceUpload, DirectHardwareCRUD

**Client-Vehicle Ownership (Quan hệ Sở hữu Xe)**:
Mô hình quan hệ cho phép một Client sở hữu nhiều phương tiện (`1-N`) hoặc không sở hữu phương tiện nào (đối với khách VIP), trong đó biển số xe được kiểm tra tính duy nhất trên toàn hệ thống giữa các phương tiện đang kích hoạt (`IsActive && !IsDeleted`).
_Avoid_: SingleVehicleOnly, DuplicatePlateActive

**PolicyScheme Forwarding (Bộ Điều Tuyến Scheme Xác Thực)**:
Cơ chế tự động phân giải và chuyển tiếp yêu cầu xác thực trong ASP.NET Core: kiểm tra sự hiện diện của header `X-API-KEY` để chọn scheme `ApiKey`, hoặc mặc định chuyển sang `JwtBearerDefaults`, cho phép áp dụng duy nhất thuộc tính `[Authorize]` trên toàn bộ API.
_Avoid_: ManualMiddlewareAuth, CustomAuthorizeAttribute

**Fixed-Window Rate Limiting (Giới Hạn Tần Suất Cửa Sổ Cố Định)**:
Chính sách phòng vệ tại tầng biên nhằm ngăn chặn tấn công dò quét mật khẩu (brute-force) bằng cách giới hạn tối đa 5 yêu cầu đăng nhập trong mỗi khung thời gian 60 giây cho mỗi địa chỉ IP, tự động từ chối với mã phản hồi HTTP 429 Too Many Requests.
_Avoid_: InfiniteRetry, CustomSleepDelay

**URL-Segment API Versioning (Quản Lý Phiên Bản API Qua URL)**:
Chiến lược định tuyến phiên bản API chuẩn mực dựa trên gói `Asp.Versioning.Mvc`, nhúng mã phiên bản trực tiếp vào đường dẫn URL (`/api/v{version:apiVersion}/...`), cho phép duy trì song song nhiều phiên bản mà không gây phá vỡ tính tương thích ngược cho hệ thống tích hợp cũ.
_Avoid_: QueryStringVersioning, HeaderVersioningOnly

**Dual-Scheme Swagger Authorization (Ủy Quyền Kép Trên Swagger)**:
Cấu hình giao diện SwaggerGen hỗ trợ đồng thời hai định nghĩa bảo mật: HTTP Bearer JWT và API Key header `X-API-KEY`, cho phép kỹ sư và đối tác dễ dàng kiểm thử trực tiếp mọi endpoint theo đúng vai trò và phương thức mong muốn.
_Avoid_: SingleSchemeSwagger, PostmanOnlyTesting

**Namespace-Filtered Test Execution (Chạy Kiểm Thử Theo Namespace)**:
Quy ước dòng lệnh cho phép tách bạch chu kỳ kiểm thử của phân hệ REST API khỏi bộ test WinForms: sử dụng `dotnet test --filter "FullyQualifiedName~HPParking.Tests.Api"` để đạt tốc độ phản hồi sub-second trong quá trình lập trình.
_Avoid_: FullSuiteOnlyTesting, ManualTestRuns

**Cascade Soft-Delete Assertion (Kiểm Chứng Xóa Mềm Phân Tầng)**:
Quy tắc kiểm thử đảm bảo khi thực hiện xóa mềm một Client (`hardDelete = false`), toàn bộ các Vehicle liên kết phải được kiểm chứng tự động chuyển trạng thái `IsDeleted = true` và `DeletedAt` khác null; đồng thời **bảo lưu 100% dữ liệu FaceID trên thiết bị phần cứng (tuyệt đối không phát lệnh xóa)**. Chỉ khi thực hiện xóa cứng (`hardDelete = true`) mới kích hoạt lệnh thu hồi quyền FaceID (`DeleteCard` & `DeleteUser`) trên các thiết bị đầu đọc active.
_Avoid_: OrphanVehicles, DeleteFaceIdOnSoftDelete

**Dual-Layer Integration Testing (Kiểm Thử Tích Hợp Phân Lớp Kép)**:
Mô hình kiểm thử phân tách rõ ràng thành 2 tầng độc lập: Tầng tự động hóa (In-Memory WebApplicationFactory + CSDL test `hpparking_integration_test` + Mock FaceID) phục vụ CI/CD ổn định 100%, và Tầng chẩn đoán phần cứng (Live Hardware Diagnostics) kết nối trực tiếp thiết bị FaceID thật trong mạng LAN phục vụ đối soát thiết bị thực tế.
_Avoid_: HardwareDependentCI, PureMockOnly

**In-Memory Test Pipeline (Đường Ống Kiểm Thử Trong Bộ Nhớ)**:
Cơ chế kiểm thử tích hợp sử dụng `WebApplicationFactory<Program>` để khởi chạy TestServer giả lập trong bộ nhớ, cho phép gửi các yêu cầu HTTP thực thụ qua toàn bộ chuỗi Middleware, Auth Handler, Model Validation và Controller mà không cần chiếm dụng cổng mạng vật lý của máy tính.
_Avoid_: LoopbackPortTesting, ExternalServerTesting

**Lazy On-Demand Log Creation (Khởi Tạo File Log Theo Yêu Cầu)**:
Quy tắc quản lý tệp tin nhật ký của Serilog: chỉ khởi tạo tệp tin `logs/hpparking-api-yyyyMMdd.log` tại đúng thời điểm dòng log đầu tiên phát sinh trong ngày; ngày nào không có hoạt động hoặc sự cố đạt ngưỡng thì tuyệt đối không tạo tệp tin 0 byte rỗng trên đĩa.
_Avoid_: EmptyLogSpam, PreallocatedLogFiles

**Environment-Aware Log Sinks (Bộ Xuất Log Tương Thích Môi Trường)**:
Chiến lược phân bổ nơi xuất log linh hoạt: kích hoạt Console Sink trong môi trường Development phục vụ lập trình viên quan sát thời gian thực, và tắt hoàn toàn Console Sink trên máy chủ Production để loại bỏ thao tác I/O blocking, chỉ ghi log vào File cuộn xoay vòng 30 ngày.
_Avoid_: AlwaysConsoleProduction, BlockingStdoutServer

**Semantic HTTP Status Codes (Mã Trạng Thái HTTP Chuẩn Ngữ Nghĩa)**:
Hệ thống mã trạng thái giao vận tuân thủ nghiêm ngặt RFC 9110 phản ánh chính xác kết quả xử lý: 200 OK (thao tác thành công kể cả xóa kèm body thông báo), 201 Created (tạo tài nguyên mới), 400 Bad Request (lỗi validation), 401 Unauthorized (chưa xác thực), 403 Forbidden (sai quyền vai trò), 404 Not Found (không tìm thấy), 409 Conflict (trùng khóa duy nhất), 429 Too Many Requests (vượt ngưỡng tần suất), và 500 Server Error (lỗi ngoại lệ hệ thống).
_Avoid_: All200OkPattern, Generic400Only

**Unique Constraint Conflict (409 Conflict)**:
Quy chuẩn trả về mã lỗi HTTP 409 Conflict chuyên biệt khi phát sinh xung đột trạng thái dữ liệu trong MongoDB (số điện thoại Client đã tồn tại hoặc biển số xe đang kích hoạt bị trùng), tách bạch rõ ràng khỏi các lỗi kiểm tra định dạng đầu vào (400 Bad Request).
_Avoid_: 400ForDuplicateData, DuplicateIgnore

**Deterministic 10-Step Pipeline (Chuỗi Pipeline 10 Bước Định Sẵn)**:
Thứ tự thực thi bất biến của chuỗi middleware trong ASP.NET Core: 1. Exception -> 2. TraceId -> 3. SecurityHeaders -> 4. SerilogRequestLogging -> 5. StaticFiles -> 6. Routing -> 7. CORS -> 8. Authentication -> 9. RateLimiter -> 10. Authorization -> MapControllers, tối ưu hóa triệt để để không sinh lỗi CORS giả và cho phép Rate Limiter phân bổ hạn ngạch chính xác theo User ID.
_Avoid_: RandomMiddlewareOrder, CorsAfterAuth

**W3C Traceparent Context (Ngữ Cảnh Truy Vết W3C)**:
Chuẩn quốc tế truy vết phân tán RFC W3C sử dụng định dạng `00-{traceId}-{spanId}-01`, tích hợp native qua `System.Diagnostics.Activity.Current` trong .NET 8, đồng bộ mã `traceId` xuyên suốt từ HTTP headers, Serilog logs đến trường lỗi của `ApiResponse`.
_Avoid_: CustomOnlyTraceId, MissingCorrelationHeader

**Offset-Based 1-Indexed Pagination (Phân Trang Offset Chuẩn 1-Indexed)**:
Cơ chế phân trang dữ liệu dựa trên chỉ số trang con người (`pageIndex = 1` là trang đầu tiên) và số bản ghi trên trang (`pageSize`, mặc định 20), ánh xạ sang MongoDB qua công thức `skip = (pageIndex - 1) * pageSize` và `limit = pageSize`, tương thích hoàn hảo với các UI Table quản trị.
_Avoid_: ZeroBasedPageParam, KeysetOnlyPagination

**Safe MaxPageSize Clamping (Kẹp Trần Kích Thước Trang An Toàn)**:
Chính sách bảo vệ tài nguyên máy chủ bằng cách tự động giới hạn `pageSize` tối đa không vượt quá 100 bản ghi/trang (`Math.Clamp(pageSize, 1, 100)`), ngăn chặn tuyệt đối các cuộc tấn công quét dữ liệu gây cạn kiệt RAM và quá tải MongoDB.
_Avoid_: UnboundedPageSize, ArbitraryLimit

**Generic Excel Engine (Bộ Xử Lý Excel Dùng Chung)**:
Kiến trúc lõi đọc, ghi và phát sinh bảng tính Excel (.xlsx) dựa trên ClosedXML (MIT License) và Generic Programming, tập trung toàn bộ kỹ thuật xử lý bảng tính tại một điểm duy nhất, tách bạch hoàn toàn logic OpenXML khỏi các thực thể nghiệp vụ.
_Avoid_: EntitySpecificExcelCode, ScatteredOpenXml

**Fluent Excel Profile (Hồ Sơ Cấu Hình Bảng Tính Fluent)**:
Mô hình cấu hình hướng đối tượng kế thừa từ `ExcelProfile<T>` cho phép định nghĩa thứ tự cột, tên tiêu đề tiếng Việt, quy tắc bắt buộc, định dạng ngày/tháng, chú thích ô (cell tooltip), và danh sách thả xuống (dropdown data validation) hoàn toàn tách biệt khỏi DTO.
_Avoid_: AttributeOnlyMapping, HardcodedColumns

**Partial Success Import (Nhập Liệu Thành Công Từng Phần)**:
Chiến lược xử lý tệp Excel hàng loạt: lưu trữ thành công các dòng dữ liệu hợp lệ vào CSDL và đồng thời tổng hợp báo cáo chi tiết cho toàn bộ các dòng vi phạm (kèm tọa độ dòng, tên cột, giá trị nhập vào và thông báo lỗi rõ ràng) để người dùng dễ dàng khắc phục.
_Avoid_: AllOrNothingFailure, SilentRowDrop

**Dry-Run Import Preview (Chạy Thử Nghiệm Nhập Liệu Giả Lập)**:
Tham số truy vấn `?dryRun=true` cho phép kiểm tra tính hợp lệ về định dạng và tính toàn vẹn trùng lặp của toàn bộ tệp Excel mà không thực hiện ghi bất kỳ dữ liệu nào vào cơ sở dữ liệu MongoDB.
_Avoid_: BlindImport, TestRecordPollution

**Duplicate Resolution Action (Chế Độ Xử Lý Bản Ghi Trùng Lặp)**:
Tham số điều khiển hành vi khi nhập liệu gặp bản ghi đã tồn tại mã định danh (Code hoặc Biển số xe): `Skip` (bỏ qua bản ghi trùng - mặc định), `Update` (cập nhật thông tin mới), hoặc `Error` (ghi nhận như lỗi dòng).
_Avoid_: ForceOverwrite, SilentOverwrite

**Safe Stream Export (Xuất Dữ Liệu Luồng An Toàn)**:
Cơ chế xuất tệp Excel trực tiếp qua HTTP File Stream theo đúng bộ lọc tìm kiếm hiện hành, được kiểm soát bởi ngưỡng kẹp trần tối đa 10,000 bản ghi/tệp nhằm triệt tiêu nguy cơ cạn kiệt bộ nhớ RAM máy chủ.
_Avoid_: UnboundedExport, MemoryLeakDump

**Real-Time Facet Aggregation (Gom Nhóm Đa Chiều Thời Gian Thực)**:
Kỹ thuật truy vấn cơ sở dữ liệu MongoDB sử dụng stage `$facet` để thực thi song song các luồng tính toán số lượng (tổng số, phân loại theo trạng thái, nhóm theo loại khách/loại xe, kiểm tra ảnh FaceID) trong một round-trip mạng duy nhất, loại bỏ hoàn toàn tầng cache trung gian để bảo đảm số liệu luôn chính xác tuyệt đối theo từng mili-giây.
_Avoid_: SequentialCountQueries, StaleCacheAggregation

**Dual-Mode Authentication (Xác Thực Song Song)**:
Cơ chế phát hành thông tin xác thực đồng thời qua hai kênh khi đăng nhập: vừa trả về JSON payload chứa `accessToken` cho các ứng dụng native (Mobile, WinForms, Swagger, Postman), vừa đính kèm `Set-Cookie` an toàn chứa JWT token cho trình duyệt Web Admin.
_Avoid_: SplitLoginEndpoints, CookieOnlyAuth

**HttpOnly Cookie Authentication (Xác Thực Cookie An Toàn Cho Web Admin)**:
Cơ chế lưu trữ và truyền nhận JWT token qua Cookie có gắn cờ `HttpOnly = true`, `SameSite = Lax` và `Secure = Request.IsHttps` với tên cookie `hpparking_access_token`. Đảm bảo JavaScript không thể can thiệp đọc token (triệt tiêu nguy cơ XSS) và trình duyệt tự động gửi cookie trong mọi request khi bật cờ credentials.
_Avoid_: LocalStorageToken, InsecureCookie

**Safe Logout & Audit (Đăng Xuất An Toàn Và Ghi Vết)**:
Quy trình hủy phiên làm việc tại endpoint `POST /api/v1/auth/logout`: tự động xóa sạch Cookie `hpparking_access_token` trên trình duyệt và ghi vết sự kiện Logout của người dùng vào collection `AuditLogs` trong MongoDB phục vụ đối soát an toàn thông tin.
_Avoid_: ClientSideOnlyLogout, UnloggedLogout

**Cross-Domain Authentication Strategy (Chiến Lược Xác Thực Đa Tên Miền)**:
Quy tắc triển khai phân hệ Web Admin tùy theo hạ tầng tên miền:
- *Cùng hệ sinh thái* (cùng domain cha như `admin.hpparking.vn` và `api.hpparking.vn`): Sử dụng **HttpOnly Cookie** (`hpparking_access_token` và `hpparking_refresh_token`) là chuẩn an toàn nhất để chống XSS.
- *Khác hoàn toàn domain* (cross-site như `quanlybaixe.com` và `api-hpparking.vn`): Sử dụng cơ chế truyền qua **Header** (`Authorization: Bearer <accessToken>` và `X-Refresh-Token: <refreshToken>`) mà API đã tích hợp sẵn trong mô hình Dual-Mode để đảm bảo 100% không bị trình duyệt chặn Third-Party Cookie.
_Avoid_: CookieOnlyAcrossDifferentDomains, HardcodedAuthChannel

**Stateless Refresh Token & Token Rotation (Xoay Vòng Token Phi Trạng Thái)**:
Cơ chế duy trì phiên làm việc không phụ thuộc bảng lưu trữ trong cơ sở dữ liệu: mã hóa thông tin người dùng vào chuỗi JWT Refresh Token độc lập với khóa bí mật riêng (`RefreshTokenSecretKey`) có thời hạn 7 ngày. Mỗi lần làm mới phiên tại `/refresh-token`, hệ thống bắt buộc phát hành cặp Access Token và Refresh Token hoàn toàn mới (Token Rotation), đồng thời nạp lại thông tin vai trò mới nhất từ MongoDB.
_Avoid_: StatefulRefreshToken, DatabaseTokenStore, InfiniteTokenReuse

**LastLogoutAt Revocation (Thu Hồi Phiên Theo Thời Điểm Đăng Xuất)**:
Cơ chế vô hiệu hóa toàn bộ Refresh Token không trạng thái khi người dùng đăng xuất hoặc đổi mật khẩu: ghi nhận mốc thời gian `user.LastLogoutAt = DateTime.UtcNow` trong MongoDB. Mọi Refresh Token có thời điểm phát hành (`iat`) trước hoặc bằng mốc này sẽ bị từ chối với mã lỗi `401 Unauthorized`.
_Avoid_: BlacklistDatabaseTable, ZombieSessionToken

**Generic Base DTO Pattern (Mẫu Kế Thừa DTO Phân Cấp)**:
Cấu trúc phân cấp DTO dùng chung (`BaseDto<TKey>`, `BaseDto`, `AuditableDto<TKey>`, `AuditableDto`) chuẩn hóa trường khóa chính (`Id`) và dấu vết kiểm toán (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`) cho toàn bộ tài nguyên trong hệ thống API, loại bỏ triệt để mã nguồn khai báo trùng lặp.
_Avoid_: RedundantDtoProperties, UntypedIdDto

**Direct Mapster Configuration (Cấu Hình Mapster IRegister Không Wrapper)**:
Mô hình ánh xạ đối tượng phân tán: từng phân hệ tự định nghĩa cấu hình ánh xạ qua interface `IRegister` (`ClientMappingConfig`, `VehicleMappingConfig`), được tự động quét và tiền biên dịch (`config.Compile()`) khi khởi động ứng dụng. Loại bỏ hoàn toàn lớp mở rộng wrapper trung gian nhằm tối ưu hóa hiệu năng chuyển đổi đối tượng và tính trong sáng của mã nguồn.
_Avoid_: MappingExtensionsWrapper, AutoMapperProfile, ManualDtoCopy

**Flat Identifier-Based Avatar Storage (Lưu Trữ Ảnh Phẳng Theo Định Danh)**:
Chiến lược lưu trữ tệp tin ảnh chân dung (Avatar) trực tiếp trên đĩa theo tên phẳng gắn chặt với định danh khách hàng (`{Code}.jpg` hoặc `{PhoneNumber}.jpg`), cấu hình qua `StorageSettings`. Không phân mảnh thư mục theo ngày tháng, tự động ghi đè tệp tin khi khách hàng cập nhật ảnh mới nhằm chống sinh tệp tin mồ côi (orphaned files).
_Avoid_: DateFolderSharding, RandomGuidFileName, DatabaseBlobStorage

**License Plate Normalization (Chuẩn Hóa Biển Số Xe)**:
Quy chuẩn làm sạch chuỗi ký tự biển số xe (`PlateHelper`): tự động loại bỏ toàn bộ khoảng trắng thừa, dấu chấm, dấu gạch ngang và chuyển thành ký tự in hoa (ví dụ: `30-A1 123.45` -> `30A112345`), đảm bảo tính nhất quán tuyệt đối khi tìm kiếm và kiểm tra duy nhất giữa các phương tiện đang kích hoạt.
_Avoid_: RawPlateStorage, UncleanedPlateQuery

**Hardware FaceID Lifecycle Policy (Chính Sách Vòng Đời FaceID Phần Cứng)**:
Quy tắc phân định vòng đời dữ liệu sinh trắc học trên đầu đọc nhận diện khuôn mặt:
- *Xóa mềm (`hardDelete = false`)*: Đánh dấu xóa trong CSDL nhưng **bảo lưu 100% dữ liệu FaceID trên thiết bị phần cứng**, tránh mất mẫu đăng ký của khách hàng.
- *Xóa cứng (`hardDelete = true`)*: Xóa vĩnh viễn dữ liệu MongoDB, xóa tệp ảnh Avatar trên đĩa, và đồng thời phát lệnh thu hồi thẻ SĐT (`DeleteCardAsync`) và xóa người dùng (`DeleteUserAsync`) trên toàn bộ thiết bị FaceID của các làn xe đang kích hoạt.
_Avoid_: RevokeFaceOnSoftDelete, RetainHardwareOnHardDelete

**Universal Restrict Deletion Policy (Chính Sách Chặn Xóa Toàn Diện)**:
Quy tắc bảo vệ tính toàn vẹn tham chiếu 100% không ngoại lệ trong toàn bộ hệ thống API: ngăn chặn hoàn toàn việc xóa thực thể cha nếu còn bất kỳ thực thể con nào đang tham chiếu tới (ví dụ: không thể xóa Company nếu còn Department/Gate, không thể xóa Department nếu còn Client, không thể xóa Client nếu còn Vehicle, không thể xóa Gate nếu còn Lane, không thể xóa Device nếu đang gán vào Lane). Khi vi phạm, API lập tức từ chối và trả về mã lỗi `409 Conflict`.
_Avoid_: CascadeOrphanRecords, SilentForeignDelete, ClientVehicleCascadeDelete

**Strict Lane Device Type-Safety (An Toàn Kiểu Thiết Bị Cho Làn Xe)**:
Quy chuẩn kiểm tra tính tương thích phần cứng khi liên kết Làn xe (`Lane`): `OverviewCameraDeviceId` và `PlateCameraDeviceId` bắt buộc có `Type == DeviceType.Camera`, `ControllerDeviceId` bắt buộc `DeviceType.Controller`, và `FaceDeviceId` bắt buộc `DeviceType.FaceId`. Gán sai loại thiết bị sẽ bị từ chối với mã lỗi `400 Bad Request`.
_Avoid_: UntypedLaneHardware, LooseDeviceBinding

**Cross-Lane Hardware Sharing (Tái Sử Dụng Thiết Bị Đa Làn)**:
Mô hình cấu hình cho phép một thiết bị vật lý được gán vào nhiều Làn xe khác nhau: hỗ trợ chuẩn bộ điều khiển 4 cổng ZKTeco C3-400 kết nối đồng thời 4 làn (vào/ra xe máy và ô tô), hoặc camera góc rộng/đầu đọc FaceID phục vụ làn 2 chiều (TwoWay).
_Avoid_: SingleLaneLockedDevice, RedundantHardwareEnrollment

**Enriched Detail DTO Pattern (Mẫu DTO Chi Tiết Bổ Sung)**:
Mô hình tách biệt cấu trúc dữ liệu trả về giữa DTO danh sách và DTO chi tiết: DTO danh sách (`LaneDto`, `DepartmentDto`) nhúng các trường tên tóm tắt phẳng (`CompanyName`, `GateName`) phục vụ render nhanh bảng biểu UI; DTO chi tiết (`LaneDetailDto`) nạp đầy đủ thông tin tóm tắt của Cổng và toàn bộ thiết bị ngoại vi liên quan, triệt tiêu nhu cầu gọi nhiều API phụ từ Frontend.
_Avoid_: MultipleGetRequestsForDetail, EmptyForeignKeyDto

**Active State Protection (Bảo Vệ Trạng Thái Kích Hoạt Phần Cứng)**:
Cơ chế kiểm soát an toàn vận hành: không cho phép chuyển trạng thái `IsActive = false` của một Cổng hoặc Thiết bị nếu đang được gắn vào một Làn xe đang kích hoạt (`IsActive == true`). Yêu cầu quản trị viên phải gỡ thiết bị hoặc tắt Làn xe trước khi vô hiệu hóa phần cứng, tránh sự cố gián đoạn tại bốt kiểm soát.
_Avoid_: SilentHardwareDeactivation, DisruptedLaneWorker

**Recycle Bin Query Pattern (Mẫu Truy Vấn Thùng Rác Qua Tham Số)**:
Chuẩn mực tra cứu các bản ghi đã xóa mềm trong cơ sở dữ liệu: tích hợp tham số truy vấn `?onlyDeleted=true` vào tất cả các API danh sách (`GET /api/v1/{resources}`), tái sử dụng toàn bộ bộ lọc tìm kiếm và cấu trúc phân trang chuẩn `PagedResult<T>`, loại bỏ nhu cầu sinh thêm endpoint rác.
_Avoid_: SplitTrashControllers, UnpagedRecycleBin

**Strict Parent-First Restore Workflow (Quy Trình Khôi Phục Cha Trước - Con Sau)**:
Nguyên tắc phục hồi dữ liệu phân cấp: bắt buộc thực thể cha phải đang hoạt động (`!IsDeleted`) thì thực thể con mới được phép phục hồi. Nếu cha đang nằm trong thùng rác, API khôi phục con lập tức từ chối (`400 Bad Request`) và yêu cầu phục hồi cha trước, triệt tiêu 100% nguy cơ phát sinh bản ghi mồ côi.
_Avoid_: OrphanChildRestore, ReverseCascadePollution

**Re-validation on Restore (Tái Xác Thực Khóa Duy Nhất Khi Khôi Phục)**:
Quy tắc bảo vệ toàn vẹn dữ liệu lúc phục hồi: trước khi chuyển `IsDeleted = false`, tầng Service bắt buộc kiểm tra lại toàn bộ các trường định danh duy nhất (Biển số xe, SĐT, Mã Code, Cặp IP:Port) so với tập các bản ghi đang hoạt động. Nếu đã bị bản ghi mới chiếm dụng trong thời gian nằm trong thùng rác, API lập tức từ chối và trả về mã lỗi `409 Conflict`.
_Avoid_: BlindRestoreDuplicate, SilentKeyCollision





