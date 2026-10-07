# HPParking System Context

Hệ thống quản lý bãi đỗ xe thông minh và kiểm soát ra vào đa phương thức (thẻ định danh, nhận diện khuôn mặt, biển số và điều vận xe công vụ) cho nhà máy và tòa nhà văn phòng.

## Language

### Core Entities

**Client**:
Hồ sơ nhân sự (cán bộ công nhân viên, nhà thầu hoặc khách đến thăm) được đăng ký thông tin nhận diện và phương tiện để cấp quyền ra vào bãi xe. Khóa nghiệp vụ nhận diện là Mã định danh (`Code`), bắt buộc duy nhất và tự động chuẩn hóa viết hoa không phân biệt hoa thường.
_Avoid_: Khách hàng, Customer, User, Account, Driver

**Vehicle**:
Phương tiện giao thông cơ giới đã đăng ký trong hệ thống, thuộc quyền sở hữu của nhân sự hoặc là xe công vụ nội bộ dùng chung.
_Avoid_: Car, Bike, Motorbike, TransportUnit

**Card**:
Thẻ định danh vật lý chuẩn hóa 10 ký tự số, dùng để quẹt thẻ tại các đầu đọc kiểm soát cổng.
_Avoid_: RfidChip, MagneticCard, BadCode

**ParkingSession**:
Bản ghi phiên gửi xe theo dõi chu trình lưu thông của phương tiện hoặc người từ lúc vào đến khi rời khỏi bãi xe.
_Avoid_: EventParking, Ticket, Transaction, HistoryLog

**AuditLog**:
Bản ghi sổ cái kiểm toán ghi nhận dấu vết các thao tác quản trị và thay đổi cấu hình trên hệ thống.
_Avoid_: SystemLog, DebugLog, AccessLog, ActivityHistory

**Gate**:
Cổng kiểm soát vật lý độc lập tại khuôn viên hoặc nhà máy, quản lý một hoặc nhiều làn kiểm soát.
_Avoid_: Door, EntryPoint, Checkpoint, Portal

**Lane**:
Làn kiểm soát vật lý trực thuộc cổng, xác định luồng di chuyển của phương tiện hoặc người đi bộ theo một chiều vào hoặc ra.
_Avoid_: BarrierLane, Channel

**Company**:
Đơn vị quản lý vận hành hoặc doanh nghiệp sở hữu khuôn viên bãi đỗ xe.
_Avoid_: Organization, Agency, Tenant

**Department**:
Phòng ban trực thuộc công ty nơi nhân sự công tác.
_Avoid_: Division, Group, Team

**Contractor**:
Đơn vị nhà thầu hoặc đối tác bên ngoài có nhân sự và phương tiện đăng ký hoạt động trong khuôn viên.
_Avoid_: Vendor, Partner, Supplier, Subcontractor

**User**:
Tài khoản người dùng nội bộ đăng nhập vào phần mềm để thao tác quản trị và vận hành bãi xe.
_Avoid_: Client, Customer, Account, Driver

**Device**:
Thiết bị phần cứng ngoại vi (camera, barie, đầu đọc thẻ, máy FaceID) kết nối và phối hợp hoạt động tại các làn kiểm soát.
_Avoid_: Hardware, Peripheral, Machine, Equipment

**GateRouteConfig**:
Cấu hình lộ trình di chuyển cố định giữa các cổng kèm giới hạn thời gian cho xe công vụ điều vận.
_Avoid_: TripRoute, FactoryPath, Itinerary

**VehicleDispatchTrip**:
Bản ghi hành trình điều vận liên cổng của xe công vụ nhằm giám sát tiến độ di chuyển và thời gian dừng đỗ thực tế.
_Avoid_: TripRecord, DispatchLog, TransportSession

### Access Control & Verification

**Inbound Lane**:
Làn kiểm soát dành cho phương tiện hoặc người đi vào khuôn viên bãi xe.
_Avoid_: EntryGate, CheckInLane, InLane

**Outbound Lane**:
Làn kiểm soát dành cho phương tiện hoặc người rời khỏi khuôn viên bãi xe.
_Avoid_: ExitGate, CheckOutLane, OutLane

**Vehicle Lane**:
Làn kiểm soát dành riêng cho phương tiện giao thông cơ giới, tích hợp camera biển số, camera toàn cảnh và barie.
_Avoid_: MotoOnlyLane, CarOnlyLane, FixedParityLane

**Pedestrian Lane**:
Làn kiểm soát dành cho người đi bộ, tích hợp cổng quay turnstile và thiết bị nhận diện khuôn mặt hoặc quẹt thẻ.
_Avoid_: PeopleLane, WalkGate, TurnstileOnly

**Trigger Source**:
Tín hiệu kích hoạt quy trình kiểm soát tại làn phát sinh từ quẹt thẻ, nhận diện khuôn mặt, cảm biến radar hoặc nút bấm tay.
_Avoid_: EventOrigin, TriggerType

**Strict Exit Lockout**:
Chính sách an ninh chặn barie tại cổng ra khi biển số xe lúc ra không trùng khớp với biển số xe ghi nhận lúc vào.
_Avoid_: SoftMismatch, AutoBypassExit

**VIP Seamless Gate Access**:
Chính sách mở barie ngay khi xác thực thẻ hợp lệ cho nhân sự được miễn đối soát biển số, không chặn barie lúc ra khi có phiên hợp lệ.
_Avoid_: EnforcedPlateOnVip, ConcatenatedPlateSession, DummyPlateAssignment

**Cross-Gate Flow**:
Quy trình lưu thông cho phép phương tiện đi vào tại một cổng và rời đi tại một cổng khác trong cùng hệ thống.
_Avoid_: SingleGateOnly, LocalStorageOnly

**Active Parking**:
Trạng thái phương tiện hoặc người đang có phiên gửi xe lưu lại trong bãi chưa hoàn tất chu trình ra.
_Avoid_: CurrentSession, UnfinishedPark

**Duplicate Entry**:
Tình huống vi phạm khi một đối tượng đang có phiên gửi xe chưa hoàn tất trong bãi nhưng lại tiếp tục yêu cầu vào thêm lần nữa.
_Avoid_: DoubleCheckIn, ReEntryViolation

**Plate Mismatch**:
Tình huống cảnh báo an ninh khi biển số xe nhận diện được không trùng khớp với biển số đăng ký hoặc biển số lúc vào.
_Avoid_: WrongPlate, InvalidPlateMatch

**Lane FaceID Verification**:
Quy tắc kiểm soát khuôn mặt tại làn xe: chỉ kích hoạt chụp và nhận diện FaceID (`needFace: true`) khi làn được trang bị phần cứng FaceID (`Lane.HasFaceDevice()`) VÀ hồ sơ Client có đăng ký phương thức xác thực `FaceId` (`client.UsesFaceAuth()`). Với Client chỉ dùng thẻ hoặc làn không có FaceID, và đối với toàn bộ luồng xe công vụ / dùng chung (`SharedVehicle`), FaceID luôn ở trạng thái tắt (`needFace: false`).
_Avoid_: ForceLaneFace, AlwaysFaceCapture, SharedVehicleFaceScan

### Hardware & Peripherals

**Barrier**:
Thanh chắn tự động kiểm soát lối lưu thông của phương tiện tại làn xe.
_Avoid_: Gate, BoomBarrier, Door

**Turnstile**:
Cổng quay tự động kiểm soát lối di chuyển của người đi bộ tại làn người.
_Avoid_: Tripod, FlapGate, PedestrianDoor

**FaceID Terminal**:
Thiết bị nhận diện khuôn mặt chuyên dụng lắp đặt tại làn để xác thực sinh trắc học và kích hoạt mở cổng.
_Avoid_: FaceScanner, BiometricDevice

**ZKTeco Controller**:
Bộ điều khiển trung tâm tiếp nhận tín hiệu từ các đầu đọc, cảm biến và kích hoạt đóng mở rơ-le barie hoặc turnstile.
_Avoid_: CentralBox, GateController, IOBoard

**Radar Sensor**:
Cảm biến sóng phát hiện phương tiện tiếp cận làn kiểm soát để kích hoạt chụp ảnh hoặc mở cổng tự do.
_Avoid_: MotionSensor, AuxDetector

**LPR (License Plate Recognition)**:
Cơ chế tự động trích xuất chuỗi ký tự biển số xe từ hình ảnh chụp của camera làn xe.
_Avoid_: ANPR, OCR Reader, PlateScanner

**CCCD Reader**:
Đầu đọc thẻ căn cước công dân gắn chip phục vụ nạp nhanh thông tin định danh khi làm thủ tục đăng ký nhân sự ban đầu.
_Avoid_: ChipReader, CardScanner

### System & Identification Terms

**CardNumber**:
Mã thẻ định danh chuẩn hóa đủ 10 chữ số của nhân sự hoặc phương tiện trong toàn bộ hệ thống.
_Avoid_: RawCardCode, UnpaddedCard

**MachineCode**:
Chuỗi mã băm định danh phần cứng máy tính dùng để khóa bản quyền và tự động nhận diện trạm kiểm soát cổng.
_Avoid_: HardwareId, MachineFingerprint

**License Key**:
Chuỗi khóa bản quyền phần mềm xác thực quyền sử dụng của trạm máy tính theo thông tin đăng ký doanh nghiệp.
_Avoid_: ProductKey, ActivationToken

**Parking Duration**:
Khoảng thời gian lưu lại trong bãi xe tính từ thời điểm vào đến thời điểm ra hoặc thời điểm hiện tại.
_Avoid_: StaticDuration, StoredParkingTime
