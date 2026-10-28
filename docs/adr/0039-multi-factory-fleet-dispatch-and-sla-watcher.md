# 0039: Quản Lý Điều Vận Xe Công Vụ Liên Nhà Máy & Giám Sát SLA Tự Động (Fleet Dispatch & SLA Watcher)

Quyết định kiến trúc về mô hình quản lý xe công vụ / xe dùng chung (`Vehicle.IsShared = true`) tinh gọn không phụ thuộc cứng công ty/phòng ban, cơ chế định tuyến đa cổng linh hoạt (Tuyến cố định và Tuyến tự do mặc định), điểm xuất phát động (`OriginGateId`), và dịch vụ nền quét vi phạm SLA thời gian thực gửi cảnh báo Email kèm ảnh bằng chứng tài xế qua MailKit.

## Ngữ cảnh

1. **Nhu cầu điều xe linh hoạt giữa các nhà máy**:
   - Trong các cụm công nghiệp lớn gồm nhiều nhà máy vệ tinh, các xe bán tải và xe tải nội bộ được dùng chung để chuyên chở linh kiện và kỹ thuật viên qua lại giữa các xưởng.
   - Các xe này không thuộc biên chế cứng của bất kỳ phòng ban (`DepartmentInCharge`) hay nhà máy (`HomeCompanyId`) nào. Một chiếc xe sáng có thể xuất phát từ Nhà máy 1, chiều kết thúc tại Nhà máy 3 và sáng hôm sau lại bắt đầu hành trình từ Nhà máy 3. Do đó, việc gán cứng nơi xuất phát hoặc đơn vị chủ quản là không thực tế.
2. **Nguy cơ tài xế trốn việc hoặc chiếm dụng xe**:
   - Nếu không có cơ chế giám sát thời gian, tài xế có thể lấy xe công vụ đi việc riêng ngoài khuôn viên nhà máy (trốn việc) hoặc đỗ xe tại một phân xưởng quá lâu làm gián đoạn dây chuyền logistics.
3. **Đa dạng lộ trình di chuyển**:
   - Một số chuyến xe có lộ trình giao nhận hàng định kỳ cố định (Cổng A -> Cổng B -> Cổng C -> Cổng A) với thời gian di chuyển và lưu lại quy định cho từng chặng.
   - Ngược lại, đa số các xe chạy đột xuất cần cơ chế Tuyến tự do mặc định (Free-roam) đi bất kỳ đâu nhưng phải tuân thủ giới hạn thời gian (15 phút di chuyển và 15 phút làm việc tại mỗi điểm).

## Quyết định

1. **Thực thể Xe Dùng Chung Tinh Gọn (Lean Shared Vehicle)**:
   - Thêm thuộc tính `IsShared: bool` và `AssignedRouteId: string?` vào [Vehicle.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Core/Models/Entities/Vehicle.cs).
   - Loại bỏ hoàn toàn các trường gán tĩnh `HomeCompanyId` và `DepartmentInCharge`.
   - Mỗi xe dùng chung được cắm một thẻ định danh vật lý cố định (`Card.TargetType = Vehicle`).

2. **Cơ chế Điểm Xuất Phát Động (`OriginGateId`) & Định Tuyến Linh Hoạt**:
   - Khi xe dùng chung quẹt thẻ xuất phát khỏi một cổng bất kỳ lần đầu (làn RA), hệ thống tự động gán cổng đó làm `OriginGateId` cho chuyến đi [VehicleDispatchTrip.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Core/Models/Entities/VehicleDispatchTrip.cs).
   - **Tuyến cố định (`GateRouteConfig`)**: Xác định danh sách các chặng cổng (`RouteGateStep`) với `MaxTravelMinutes` và `MaxStayMinutes` riêng biệt cho từng chặng.
   - **Tuyến tự do mặc định (Free-Roam SLA)**: Khi xe không gán tuyến cố định (`AssignedRouteId == null`), hệ thống tự động áp dụng SLA chuẩn: tối đa 15 phút di chuyển giữa 2 cổng và tối đa 15 phút dừng đỗ làm việc tại điểm đến trước khi quay về cổng ban đầu.

3. **Dịch vụ Giám Sát SLA Thời Gian Thực ([VehicleTransitWatcherService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Api/Services/BackgroundServices/VehicleTransitWatcherService.cs))**:
   - Chạy nền chu kỳ mỗi 60 giây quét toàn bộ các chuyến xe đang hoạt động (`InTransit` hoặc `WorkingAtGate`).
   - Tự động so sánh thời gian hiện tại với `NextDeadline`. Nếu quá hạn:
     - Chuyển trạng thái sang `OverdueTransit` (quá giờ di chuyển) hoặc `OverdueStay` (quá giờ dừng đỗ).
     - Gửi email cảnh báo khẩn cấp qua [EmailSenderService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Api/Services/EmailSenderService.cs) (dùng MailKit SMTP) tới danh sách email cấu hình trên tuyến hoặc Ban quản lý điều vận.
     - Đính kèm trực tiếp file ảnh chụp toàn cảnh tài xế gần nhất (`LastDriverImagePath`) làm bằng chứng xác thực.
     - Đánh dấu `IsAlertSent = true` để không gửi lặp lại gây spam hòm thư.

4. **Trực Quan Hóa Trên Web Admin & WinForms Trạm Cổng**:
   - **Web Admin ([FleetDispatchPage.tsx](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.WebAdmin/src/pages/FleetDispatchPage.tsx))**: Bảng điều khiển thời gian thực với đồng hồ đếm ngược live (Live Countdown), cảnh báo Overdue nhấp nháy đỏ, hiển thị vị trí chặng và danh sách các chuyến xe đang lưu thông.
   - **Trạm cổng WinForms ([ParkingWorkflowService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Parking/ParkingWorkflowService.cs))**: Tự động nhận diện thẻ xe cắm trên xe, ghi nhận chặng di chuyển, tự động tính mốc `NextDeadline` và kích hoạt mở barrier thông suốt.

## Hệ quả

- **Tích cực**:
  - Tối ưu hóa 100% quy trình điều xe liên nhà máy, loại bỏ toàn bộ việc ghi sổ tay hoặc gọi điện thoại hỏi vị trí xe.
  - Tự động phát hiện ngay lập tức các trường hợp xe đi sai tuyến, trốn việc hoặc đỗ xe sai quy định.
  - Ban quản lý nhận được báo cáo vi phạm qua Email tức thì kèm ảnh tài xế làm bằng chứng minh bạch.
