# 0038: Quản Lý Thẻ Thống Nhất, Chế Độ Xác Thực Đa Tầng & Chuẩn Hóa Đối Tượng

Quyết định kiến trúc về việc chuyển đổi hệ thống HPParking từ mô hình Cardless thuần túy (dùng FaceID gán SĐT) sang mô hình nhận diện đa phương thức toàn diện: Quản lý Thẻ (Card) 10 chữ số thống nhất, Tách biệt cấu hình xác thực Người (`AuthMethods`) và Xe (`VerifyVehiclePlate`), Xóa bỏ hardcode biển số VIP/Khách thăm, Chuẩn hóa phân loại `ClientType.Guest`, và Tích hợp Radar sóng milimet ngõ AUX IN ZKTeco cho làn tự do.

## Ngữ cảnh

1. **Bất cập của mô hình Cardless thuần túy**:
   - Trước đây, hệ thống map số điện thoại cá nhân vào trường mã thẻ Wiegand (`CardNo`). Khi triển khai thực tế tại các nhà máy khu công nghiệp, công nhân và nhà thầu thường dùng thẻ từ RFID Wiegand 26-bit/34-bit vật lý. Độ dài mã thẻ từ đầu đọc quẹt thực tế có thể từ 4 đến 10 chữ số (ví dụ: `12345`), gây lệch mã khi so sánh với thẻ 10 số trên đầu đọc FaceID Hikvision ISAPI.
2. **Hardcode nghiệp vụ bỏ qua kiểm tra biển số cho VIP**:
   - Trong mã nguồn cũ, logic bỏ qua kiểm tra biển số được hardcode cứng: `bool isVipWithoutVehicle = (client.Type == ClientType.VIP && clientVehicles.Count == 0);`. Điều này vi phạm tính linh hoạt khi Ban quản lý muốn miễn kiểm tra biển số cho một số đối tượng khác (ví dụ: đối tác chiến lược, xe chuyên gia đột xuất) hoặc ngược lại, bắt buộc VIP phải đi đúng xe đã đăng ký.
3. **Phân loại đối tượng thiếu nhất quán**:
   - `ClientType` tồn tại giá trị `Other = 4` mơ hồ, không rõ nghĩa nghiệp vụ và không hỗ trợ khách đến thăm nhà máy liên kết với nhà thầu.
4. **Yêu cầu kiểm soát làn tự do không barie**:
   - Tại các cổng nội bộ giữa các phân xưởng, việc lắp barie gây ùn ứ xe máy và xe tải. Doanh nghiệp cần cơ chế kiểm soát tự do (`None` - không barie) nhưng vẫn tự động kích hoạt chụp ảnh và đối soát khi xe đi qua thông qua cảm biến Radar sóng milimet đấu vào ngõ AUX IN/Sensor của ZKTeco C3 Controller.

## Quyết định

1. **Chuẩn hóa mã thẻ 10 chữ số thống nhất (Unified Card 10-digit)**:
   - Toàn bộ mã thẻ nhập vào hệ thống hoặc nhận diện từ đầu đọc Wiegand/Controller/FaceID đều được chuẩn hóa qua `CardHelper.NormalizeCardCode(cardCode)`: chỉ giữ lại các chữ số và tự động thêm các số 0 ở đầu (`PadLeft(10, '0')`).
   - Xây dựng thực thể [Card.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Core/Models/Entities/Card.cs) và dịch vụ quản lý kho thẻ [CardService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Api/Services/CardService.cs) phân loại rõ Thẻ người (`TargetType = Person`) và Thẻ cắm trên xe dùng chung (`TargetType = Vehicle`).
     > [!IMPORTANT]
     > **Cập nhật kiến trúc (ADR 0043)**: Loại thẻ nhân sự (`CardTargetType.Person`) ban đầu đã được **xóa bỏ hoàn toàn khỏi toàn bộ hệ thống** (loại bỏ khỏi enum `CardTargetType`, backend API và giao diện WebAdmin). Do hệ thống triển khai mới không có dữ liệu cũ cần tương thích ngược, các luồng gán người dùng chuyển dịch triệt để sang 2 phân loại vận hành chuẩn mực: Thẻ tháng (`Monthly`) và Thẻ VIP (`VIP`).

2. **Tách biệt cấu hình Xác thực Người, Xác thực Xe và Tự động mở Barrier**:
   - **Khối 1: Phương thức xác thực người (`AuthMethods`)**: Mảng chuỗi chứa các phương thức hợp lệ gồm `Card`, `FaceId`, `None`. Phương thức `None` mang tính loại trừ tuyệt đối (nếu chọn `None` thì không được kết hợp `Card` hay `FaceId`).
   - **Khối 2: Cờ kiểm tra đối chiếu biển số (`VerifyVehiclePlate`)**: Thuộc tính boolean độc lập (ẩn và ép `false` khi danh sách `AuthMethods` chứa `None`).
     - `true`: Bắt buộc xe phải có biển số đăng ký hợp lệ, trạm cổng bật camera chụp biển số và kích hoạt OCR LPR đối soát khớp 100%.
     - `false`: Miễn kiểm tra biển số, không chặn khi LPR không đọc được (áp dụng linh hoạt cho bất kỳ ai không cần kiểm tra xe).
   - **Khối 3: Chính sách tự động mở thanh chắn theo chiều (`AutoOpenBarrier { OnEntry, OnExit }`)**: Value object độc lập trên `Client` (mặc định `OnEntry = true`, `OnExit = true`; ẩn trên UI khi `includes('None')` và backend tự động ép về `true/true` khi `authMethods.Contains(None)`).
     - Cho phép cấu hình riêng từng chiều vào/ra xem sau khi xác thực thành công (thẻ/FaceID + đối soát biển số đạt) trạm WinForms có tự động gửi lệnh kích rơ-le mở barrier/turnstile hay không.
     - Khi tắt ở một chiều (`false`), luồng xác thực vẫn hoàn tất bình thường (`ProcessStatus.Success`, ghi nhận `ParkingSession` và lưu ảnh), chỉ bỏ qua lệnh kích rơ-le `TryOpenBarrier` để bảo vệ kiểm tra thực tế và chủ động mở bằng phím tắt phần cứng `F1–F4`.
   - **Xóa bỏ hoàn toàn hardcode `isVipWithoutVehicle`**: Trạm cổng WinForms ([ParkingWorkflowService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Parking/ParkingWorkflowService.cs) và [ClientVehicleWorkflowHandler.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Parking/Handlers/ClientVehicleWorkflowHandler.cs)) đọc trực tiếp `client.RequiresPlateVerification()` và `client.ShouldAutoOpenBarrier(direction)` để ra quyết định đối soát xe và kích hoạt rơ-le.

3. **Chuẩn hóa `ClientType` & Khách đến thăm (`Guest = 4`)**:
   - Xóa bỏ hoàn toàn giá trị `Other = 4`, thay thế bằng `Guest = 4` (Khách đến thăm).
   - Khách đến thăm cho phép tùy chọn liên kết với Nhà thầu (`ContractorId`) hoặc để trống (khách đến làm việc trực tiếp với nhà máy).

4. **Tích hợp Radar sóng milimet qua ngõ AUX IN/Sensor ZKTeco Controller**:
   - Mở rộng [ControllerConfig.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Controller/ControllerConfig.cs) và [ControllerService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Controller/ControllerService.cs) phát hiện sự kiện cảm biến (ZKTeco Event 220 AUX IN hoặc Event 100 Sensor) phát ra sự kiện `OnRadarTriggered`.
   - Làn tự do cấu hình `None` sẽ tự động chụp ảnh camera toàn cảnh và ghi nhận lượt lưu thông khi radar phát hiện xe tiếp cận mà không cần quẹt thẻ hay hạ barrier.

## Hệ quả

- **Tích cực**:
  - Dữ liệu thẻ và FaceID đồng nhất tuyệt đối 10 ký tự số, giải quyết triệt để lỗi lệch mã giữa đầu đọc quẹt thẻ và thiết bị FaceID.
  - Quản trị viên Web Admin có toàn quyền quyết định ai phải kiểm tra biển số xe, ai được miễn kiểm tra chỉ bằng một thao tác click chuột.
  - Hỗ trợ đầy đủ các mô hình làn kiểm soát hiện đại: Làn barrier truyền thống, Làn FaceID kết hợp thẻ, và Làn tự do qua radar.
