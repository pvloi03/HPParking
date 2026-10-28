# 0040: Kiến Trúc Làn Đa Đối Tượng (Xe & Người), Bộ Điều Phối Khớp Mẫu Tuple và Luồng Video Trực Tiếp CHCNetSDK

> **Trạng thái:** Chấp thuận (Accepted)  
> **Thay thế (Supersedes):** [ADR 0002](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0002-card-code-bound-to-phone-number.md), [ADR 0006](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0006-cardless-faceid-wiegand-architecture.md), [ADR 0007](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0007-standard-4-port-controller-parity-architecture.md)  
> **Liên quan (Related):** [ADR 0034](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0034-gate-and-lane-management-with-hardware-binding.md), [ADR 0038](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0038-unified-card-and-multi-auth-verification-architecture.md), [ADR 0039](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0039-multi-factory-fleet-dispatch-and-sla-watcher.md)

Quyết định kiến trúc về việc chuẩn hóa hệ thống HPParking sang mô hình hỗ trợ làn đa đối tượng (Phương tiện cơ giới & Người đi bộ), cho phép cấu hình động 1 đến 4 làn kiểm soát tại mỗi cổng, tái cấu trúc toàn bộ luồng xử lý `ParkingWorkflowService` sang cú pháp so khớp mẫu tuple (`Pattern Matching Switch Dispatcher`), tối ưu hóa kết nối LiveView qua thư viện native `CHCNetSDK` để kiểm soát bộ nhớ RAM tiến trình 32-bit (x86) dưới 250MB, và áp dụng bố cục giao diện linh hoạt Option B trên ứng dụng WinForms `FrmMain`.

---

## Ngữ cảnh

1. **Hạn chế của mô hình 4 làn cố định và quy tắc chia dư chẵn/lẻ (ADR 0007)**:
   - Trước đây, ADR 0007 quy định mỗi cổng trạm kiểm soát bắt buộc phải có đủ 4 làn xe cố định (1 Moto Vào, 1 Moto Ra, 1 Car Vào, 1 Car Ra) dựa trên phép chia dư `% 2` của cổng đọc ZKTeco C3-400.
   - Khi triển khai thực tế tại các cổng phụ nhà máy, phân xưởng hoặc cổng xuất nhập hàng, số lượng làn thực tế thường chỉ có 1 làn hoặc 2 làn (ví dụ: 1 làn xe vào, 1 làn xe ra hoặc làn đảo chiều). Quy tắc chẵn/lẻ cứng nhắc khiến hệ thống không thể vận hành được ở các trạm có quy mô nhỏ hơn 4 làn.
2. **Nhu cầu kiểm soát lối đi bộ (Pedestrian Turnstile / Flap Barrier)**:
   - Các nhà máy có nhu cầu kiểm soát song song cả lối xe cơ giới và lối người đi bộ (công nhân đi bộ qua cổng quay tripod hoặc flap barrier) trên cùng một trạm máy tính bảo vệ.
   - Làn người đi bộ hoàn toàn không có biển số xe, không cần camera chụp biển số và không chạy OCR LPR, nhưng vẫn cần xác thực thẻ hoặc FaceID và mở chốt xoay. Mô hình cũ cố định vào xe máy/ô tô không thể đáp ứng được trường hợp này.
3. **Mã nguồn lặp lại và phức tạp trong luồng xác thực**:
   - Trước khi tái cấu trúc, mã nguồn luồng vào (`ProcessEntryAsync`) và luồng ra (`ProcessExitAsync`) chứa hàng loạt câu lệnh `if-else` lồng nhau để xử lý riêng lẻ từng trường hợp (Thẻ, FaceID, Radar, Manual), gây khó khăn cho việc bảo trì, viết kiểm thử đơn vị và dễ phát sinh lỗi bỏ sót logic kiểm tra hạn dùng hay ghi log.
4. **Giới hạn bộ nhớ nghiêm ngặt của tiến trình x86 (32-bit)**:
   - Do phụ thuộc vào các thư viện native C/C++ 32-bit (SimpleLPR3 OCR, ZKTeco Pull SDK), toàn bộ ứng dụng WinForms bắt buộc phải chạy ở chế độ x86 32-bit (trần địa chỉ ảo 2GB).
   - Nếu trạm mở đồng thời nhiều luồng giải mã video RTSP bằng thư viện managed hoặc VLC, bộ nhớ RAM tăng vọt dễ dẫn đến tràn bộ nhớ (`OutOfMemoryException`). Thiết bị FaceID và Camera Hikvision hỗ trợ giao thức SDK ở cổng 8000 cho phép render video trực tiếp vào `IntPtr` handle của WinForms Control bằng phần cứng (Direct3D/GDI), giải phóng hoàn toàn bộ nhớ managed của .NET.

---

## Quyết định

### 1. Phân Loại Đối Tượng Làn (`LaneTargetType`) & Ràng Buộc Động 1-4 Làn
- Mở rộng thực thể [Lane.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Core/Models/Entities/Lane.cs):
  - `TargetType`: Enum [LaneTargetType.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Core/Models/Enums/LaneTargetType.cs) gồm `Vehicle` (Phương tiện cơ giới: Ô tô, Xe máy, Xe tải) và `Pedestrian` (Người đi bộ qua cổng quay turnstile / flap barrier).
  - Cờ thiết bị linh hoạt: `UseOverviewCam` (mặc định `true`), `UsePlateCam` (mặc định `true` cho xe, tắt cho người), `UseFaceCam` (mặc định `false`).
- Xóa bỏ ràng buộc cố định 4 làn xe: Mỗi Cổng (`Gate`) cho phép cấu hình từ 1 đến 4 làn bất kỳ, có thể là toàn bộ làn xe, toàn bộ làn người đi bộ, hoặc cổng hỗn hợp (ví dụ: 2 làn xe + 2 làn người đi bộ).

### 2. Bộ Điều Phối Khớp Mẫu Tuple (Tuple Pattern Matching Switch Dispatcher)
- Trong [ParkingWorkflowService.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Services/Parking/ParkingWorkflowService.cs), thay thế toàn bộ chuỗi `if-else` phân mảnh bằng biểu thức switch tuple C# 8+ dựa trên 3 chiều ngữ cảnh:
  `var dispatchKey = (context.Lane.TargetType, trigger.Source, context.Lane.Direction);`
- Hỗ trợ đầy đủ các nhánh nghiệp vụ rõ ràng:
  - `(LaneTargetType.Vehicle, TriggerSource.CardSwipe, LaneDirection.In)` & `Out`
  - `(LaneTargetType.Vehicle, TriggerSource.FaceTerminal, LaneDirection.In)` & `Out`
  - `(LaneTargetType.Vehicle, TriggerSource.Radar, LaneDirection.In)` & `Out`
  - `(LaneTargetType.Vehicle, TriggerSource.Manual, LaneDirection.In)` & `Out`
  - `(LaneTargetType.Pedestrian, TriggerSource.CardSwipe, LaneDirection.In)` & `Out`
  - `(LaneTargetType.Pedestrian, TriggerSource.FaceTerminal, LaneDirection.In)` & `Out`
  - `(LaneTargetType.Pedestrian, TriggerSource.Manual, LaneDirection.In)` & `Out`
- Toàn bộ các nhánh được cấu thành từ **6 khối helper dùng chung** chuẩn mực:
  1. `ResolveIdentityAsync`: Định danh Client/Vehicle từ mã thẻ hoặc mã FaceID.
  2. `CaptureLaneImagesAsync`: Chụp ảnh đồng thời các camera được bật trên làn (`UsePlateCam`, `UseOverviewCam`, `UseFaceCam`).
  3. `RecognizePlateAsync`: Nhận diện biển số tự động qua LPR service (chỉ chạy khi `TargetType == Vehicle` và `VerifyVehiclePlate == true`).
  4. `ValidateClientExpiry`: Kiểm tra hạn sử dụng của khách hàng/nhân sự (`client.ExpireDate`).
  5. `TryOpenBarrier`: Kích hoạt mở thanh chắn barrier hoặc cổng quay turnstile qua relay bộ điều khiển.
  6. `SaveImagesBackground`: Ghi ảnh xuống đĩa chia sẻ không đồng bộ để không chặn luồng giao vận.

### 3. Native Direct Preview qua `CHCNetSDK` (Cổng 8000)
- Tích hợp thư viện native [CHCNetSDK.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/SDK/HikSDK/CHCNetSDK.cs) kết nối trực tiếp đến Camera Hikvision và Đầu đọc FaceID thông qua cổng quản lý SDK mặc định 8000.
- Truyền thẳng con trỏ cửa sổ đồ họa `pictureBox.Handle` vào hàm `NET_DVR_RealPlay_V40` để card màn hình trực tiếp vẽ khung hình lên màn hình, không tạo bitmap đệm trong heap managed của .NET.
- Giữ mức tiêu thụ bộ nhớ RAM của tiến trình 32-bit luôn ổn định **dưới 250MB** kể cả khi chạy liên tục 4 luồng video thời gian thực.
- Thiết bị camera của các hãng khác hoặc FaceID dạng RTSP thuần túy vẫn được hỗ trợ thông qua trường cấu hình `Device.RtspUrl`.

### 4. Bố Cục Thích Ứng Giao Diện Làn Động (Option B Layout)
- Giao diện chính trạm kiểm soát [FrmMain.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Forms/FrmMain.cs) tự động căn chỉnh tỷ lệ theo số lượng làn đang hoạt động của Cổng:
  - **Trạm có 1 đến 3 làn**: Các khung LiveView camera được gom thành một hàng ngang trên đỉnh màn hình với chiều cao cố định **280px**, dành trọn vẹn 70% diện tích bên dưới cho bảng thông tin đối soát, ảnh bằng chứng vào/ra và lịch sử xe.
  - **Trạm có 4 làn**: Bố cục chia đều thành 4 cột dọc độc lập với tỷ lệ 55% camera phía trên và 45% thông tin đối soát phía dưới.
  - Tự động ẩn các ô camera biển số đối với các làn người đi bộ (`Pedestrian`) để tối ưu hóa không gian hiển thị.

### 5. Hủy Bỏ và Thay Thế (Supersedes) Các Quyết Định Cũ
- **Supersedes ADR 0002**: Không còn ép buộc gán `Card_Code = client.PhoneNumber`. Mã thẻ được quản lý độc lập 10 chữ số trong thực thể `Card`.
- **Supersedes ADR 0006**: Bỏ khái niệm "Cardless thuần túy". Hệ thống hỗ trợ đa phương thức xác thực (Thẻ vật lý, FaceID, Radar, Manual) kết hợp đồng bộ.
- **Supersedes ADR 0007**: Bỏ quy tắc chia dư chẵn/lẻ cứng (`InputReader % 2`) và quy định cứng 4 làn xe. Chuyển sang mô hình cấu hình động 1-4 làn và phân loại tường minh bằng `TargetType` và `Direction`.

---

## Hệ quả

### Tích cực
1. **Linh hoạt tối đa trong triển khai**: Cho phép lắp đặt phần mềm tại mọi loại cổng trong nhà máy (cổng chính 4 làn, cổng phụ 1-2 làn, cổng quay người đi bộ) mà không cần viết lại mã nguồn.
2. **Kiến trúc luồng mạch lạc, dễ mở rộng**: Cú pháp so khớp mẫu tuple giúp việc bổ sung các loại cảm biến mới (ví dụ: đầu đọc mã QR, RFID tầm xa UHF) chỉ cần thêm một nhánh switch mà không ảnh hưởng đến các nhánh hiện hữu.
3. **Hiệu năng và độ tin cậy vượt trội**: Mức tiêu thụ RAM < 250MB triệt tiêu hoàn toàn rủi ro sập ứng dụng WinForms 32-bit sau nhiều ngày vận hành liên tục.
4. **Trải nghiệm bảo vệ trực quan**: Bố cục Option B tối ưu không gian hiển thị, giúp bảo vệ dễ dàng quan sát biển số và ảnh chân dung đối soát.

### Đánh đổi & Lưu ý
1. **Khai báo cấu hình chính xác**: Người quản trị trên Web Admin cần chỉ định đúng `TargetType` và các cờ camera (`UsePlateCam`, `UseOverviewCam`) để hệ thống tối ưu hóa luồng chụp ảnh và hiển thị.
2. **Đồng bộ mã thẻ 10 số**: Khi cấp phát thẻ hoặc nạp FaceID, mã thẻ luôn phải tuân thủ chuẩn 10 chữ số thống nhất qua hàm `CardHelper.NormalizeCardCode`.
