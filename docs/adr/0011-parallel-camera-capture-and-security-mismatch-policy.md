# 0011: Tối Ưu Hóa Chụp Ảnh Song Song, Cơ Chế Nhập Biển Số Thủ Công & Chính Sách Đối Soát An Ninh Nghiêm Ngặt Xe Vào/Ra

Hệ thống nâng cấp quy trình kiểm soát xe vào/ra (`ParkingWorkflowService`): chụp ảnh đồng thời 2 camera với timeout cứng 1000ms, cơ chế nhập biển số thủ công dự phòng khi camera lỗi hoặc nhận diện LPR thất bại, đối soát xác nhận của bảo vệ khi xe vào sai biển số đăng ký, và áp dụng chính sách chặn cứng an ninh tuyệt đối (Strict Security Lockout) khi xe ra bị lệch biển số.

## Ngữ cảnh

Quy trình quẹt thẻ và kiểm soát phương tiện vào/ra là luồng nghiệp vụ nhạy cảm và quan trọng nhất của hệ thống bãi xe HPParking. Qua phân tích và đối soát vận hành thực tế, luồng xử lý cũ tồn tại các vấn đề:

1. **Độ trễ cao do chụp ảnh tuần tự:**
   Hệ thống cũ chụp tuần tự Camera Biển Số trước rồi mới chụp Camera Toàn Cảnh bằng lệnh đồng bộ trên ThreadPool. Thời gian chờ tích lũy hai camera làm tăng độ trễ mở barrier từ 400ms đến 800ms cho mỗi lượt xe.
2. **Kẹt làn do phụ thuộc cứng vào phần cứng & LPR:**
   - Khi Camera Biển Số bị rớt mạng hoặc đứt cáp, hệ thống dừng luồng xe hoàn toàn (`CaptureFailed`).
   - Khi Camera Biển Số chụp được nhưng ảnh bị mờ, dính bùn đất hoặc góc chụp nghiêng khiến LPR không đọc được ký tự (`LprFailed`), xe bị kẹt lại cổng. Bảo vệ không có cách nào nhập biển số tay trực tiếp trên phần mềm để cho xe qua.
   - Camera Toàn Cảnh gặp lỗi mạng cũng làm sập toàn bộ lượt xe dù Camera Biển Số vẫn hoạt động bình thường.
3. **Chính sách an ninh lỏng lẻo ở lượt Vào và thiếu linh hoạt ở lượt Ra:**
   - **Lượt Vào**: Khi khách dùng thẻ đã đăng ký nhưng đi xe khác (biển số thực tế khác biển số đăng ký), hệ thống cũ tự động mở barrier cho vào mà không có bất kỳ cảnh báo hay yêu cầu xác nhận nào từ bảo vệ.
   - **Lượt Ra**: Cần chính sách an ninh tuyệt đối để ngăn ngừa hành vi tráo thẻ, trộm cắp xe hoặc gian lận phương tiện. Khi biển số xe ra không khớp với biển số xe lúc vào, hệ thống phải chặn cứng tuyệt đối, không cho phép mở barrier tự động.

## Quyết định

1. **Chụp ảnh bất đồng bộ song song 2 Camera (`CaptureCamerasParallelAsync`):**
   - Sử dụng `Task.WhenAll` kết hợp `Task.WhenAny` với timeout 1000ms để chụp đồng thời cả Camera Biển Số và Camera Toàn Cảnh.
   - Nếu cả 2 hoàn thành trước 1000ms, luồng tiếp tục ngay lập tức mà không phải chờ hết timeout.
   - Phân tách độc lập mức độ nghiêm trọng:
     - **Camera Toàn Cảnh lỗi/timeout**: Không dừng luồng xe, ghi nhận `UrlImageClient = ""` và tiếp tục nhận diện LPR, mở barrier bình thường.
     - **Camera Biển Số lỗi/timeout**: Kích hoạt cơ chế nhập biển số thủ công.
   - **Chống rò rỉ GDI+ handle**: Sử dụng `ContinueWith` giải phóng ngay lập tức mọi `Bitmap` trả về muộn sau mốc timeout.

2. **Cơ chế nhập biển số thủ công dự phòng qua Seam Delegate (`onManualPlateInput`):**
   - Mở rộng hợp đồng `IParkingWorkflowService` nhận thêm delegate tùy chọn `Func<Lane, Task<string?>>? onManualPlateInput = null`.
   - Kích hoạt trong 2 trường hợp:
     a) Camera Biển Số lỗi hoặc timeout.
     b) Camera Biển Số chụp thành công nhưng LPR không nhận diện được ký tự (`!result.Success || string.IsNullOrEmpty(result.Plate)`).
   - Nếu bảo vệ nhập biển số hợp lệ: Chuẩn hóa (`Trim().ToUpper()`) và tiếp tục luồng.
   - **Bảo toàn ảnh đối soát**: Ở trường hợp (b), hệ thống **vẫn lưu ảnh chụp biển số gốc vào ổ đĩa** (`UrlImageLicensePlate`), đảm bảo ban quản lý có thể đối soát hình ảnh thực tế sau này.
   - Nếu bảo vệ hủy bỏ hoặc chuỗi rỗng: Trả về đúng mã lỗi ban đầu (`CaptureFailed` hoặc `LprFailed`).

3. **Quy tắc Chặn cứng An ninh Tuyệt đối (Strict Lockout Policy) khi Biển số không khớp:**
   - **Lượt VÀO (Entry) - CHẶN CỨNG 100%**: Nếu biển số xe thực tế không đúng với biển số đăng ký của khách (`registeredPlate != actualPlate`), hệ thống **chặn cứng tuyệt đối**:
     - **Barrier không mở**, không ghi nhận lượt gửi vào cơ sở dữ liệu.
     - Trả về `ProcessStatus.PlateMismatch` với thông báo lỗi rõ ràng: *"Biển số xe không đúng với biển số đăng ký."*
     - Giao diện `FrmMain` hiển thị popup cảnh báo lỗi Error để nhân viên bảo vệ yêu cầu kiểm tra phương tiện.
   - **Lượt RA (Exit) - CHẶN CỨNG 100%**:
     - Nếu biển số xe lúc ra không khớp với biển số xe lúc vào (`cleanExitPlate != cleanInPlate`), hệ thống **chặn cứng tuyệt đối**, trả về `ProcessStatus.PlateMismatch`.
     - **Barrier tuyệt đối không mở** dưới bất kỳ hình thức nào.
     - Giao diện `FrmMain` hiển thị hộp thoại cảnh báo an ninh màu đỏ nổi bật để nhân viên bảo vệ giữ phương tiện lại kiểm tra trực tiếp.

4. **Tách bạch tầng UI Windows Forms:**
   - Xây dựng `FrmManualPlateInput` gọn gàng, tự động focus, hỗ trợ phím nóng `Enter` (Xác nhận) và `Esc` (Hủy bỏ).
   - Đảm bảo chuyển tiếp an toàn luồng giao diện (`InvokeRequired` / `Invoke`).

## Hệ quả

- **Tối ưu tốc độ qua trạm:** Giảm 400ms – 800ms độ trễ cho mỗi lượt xe nhờ cơ chế chụp ảnh song song, tăng năng lực thông xe trong giờ cao điểm.
- **Vận hành liên tục (High Availability):** Sự cố Camera Toàn Cảnh không còn gây tắc nghẽn trạm; sự cố Camera Biển Số hoặc biển số xe bẩn/mờ được khắc phục tức thời bằng hộp thoại nhập tay.
- **An ninh phương tiện tuyệt đối:** Ngăn chặn hoàn toàn rủi ro trộm xe tại làn ra; kiểm soát chặt chẽ các trường hợp đổi xe khi vào bãi.
- **Kiểm thử tự động 100% tại Seam cao nhất:** Mọi tương tác nhập tay và xác nhận sai lệch đều được kiểm thử thông qua các delegate mock mà không phụ thuộc WinForms UI. Bộ test suite mở rộng lên 66 tests (100% PASS).
