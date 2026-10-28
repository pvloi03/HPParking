# 0010: Tái Cấu Trúc Lớp Trừu Tượng Hóa Bộ Điều Khiển ZKTeco, Cơ Chế Giám Sát Sức Khỏe Thiết Bị & Khắc Phục Lỗi Auto-Reconnect

Hệ thống nâng cấp toàn diện module điều khiển trung tâm ZKTeco Controller: tách lớp trừu tượng `IZKTecoSdk` và `IDeviceAdapter` phục vụ kiểm thử tự động, tích hợp kiểm tra kết nối mạng qua Socket TCP độc lập với native DLL, áp dụng cơ chế xả bộ đệm tồn đọng (Buffer Draining), debounce lỗi đọc log, khắc phục dứt điểm lỗi vòng lặp kết nối lại vô tận (Infinite Auto-Reconnect Loop) và đồng bộ trạng thái trực quan lên giao diện chính `FrmMain`.

## Ngữ cảnh

Trong hệ thống bãi xe thông minh HPParking, bộ điều khiển trung tâm ZKTeco C3-400 đóng vai trò cốt lõi: tiếp nhận tín hiệu mã định danh truyền từ đầu đọc FaceID qua cổng Wiegand và điều khiển rơle đóng/mở barrier.

Tuy nhiên, qua quá trình rà soát và đối chiếu với kiến trúc thực chiến từ hệ thống chuẩn `PhuXuanParkingSystem`, module Controller cũ tồn tại các vấn đề kỹ thuật nghiêm trọng:
1. **Lỗi logic lặp vô tận trong Auto-Reconnect:**
   Trong phương thức `StartAutoReconnect()`, vòng lặp nền `while (!token.IsCancellationRequested)` gọi `await ConnectAsync(Config)` nhưng không kiểm tra kết quả trả về và không thoát vòng lặp (`break`) khi kết nối lại thành công. Hậu quả là sau khi bị rớt mạng một lần, thiết bị sẽ liên tục bị ngắt kết nối và kết nối lại mỗi 5 giây vô hạn lần, làm mất ổn định hệ thống.
2. **Phụ thuộc cứng vào Native C++ DLL (`plcommpro.dll` x86):**
   `ControllerService` gọi trực tiếp các phương thức static của `ZKTecoSDK`. Điều này khiến việc viết Unit Test tự động cho Controller trở nên bất khả thi nếu không có thiết bị phần cứng thật và môi trường Windows x86.
3. **Hiện tượng nhận quẹt thẻ "ma" khi khởi động lại:**
   Bộ nhớ đệm (buffer) của Controller ZKTeco thường lưu các sự kiện quẹt thẻ từ phiên làm việc trước đó. Khi phần mềm vừa kết nối lại, các log cũ này được đọc ra ngay lập tức, dẫn đến kích hoạt nhầm sự kiện quẹt thẻ.
4. **Phân tích nhầm gói tin broadcast trạng thái cửa (Door/Alarm Keepalive):**
   Controller ZKTeco định kỳ gửi các bản ghi kiểm tra trạng thái cửa với trường `EventType == 255`. Lớp phân tích cũ không lọc trường hợp này, gây lỗi dữ liệu không hợp lệ.
5. **Nghẽn đa luồng chéo giữa các làn xe tại `DeviceOrchestrator`:**
   Vòng lặp đọc log tuần tự `foreach` một luồng duy nhất trong `DeviceOrchestrator` có thể bị đứng/chậm khi một Controller rớt mạng hoặc phản hồi chậm, làm ảnh hưởng nghiêm trọng tới các làn xe còn lại.
6. **Thiếu phản hồi trực quan trên UI:**
   Sự kiện `Controller_OnStatusChanged` trong `FrmMain` chỉ in ra `Debug.WriteLine`, chưa liên kết vào nhãn trạng thái `lbStatusCtrl` tại thanh Footer của màn hình chính.

## Quyết định

1. **Khắc phục triệt để lỗi Auto-Reconnect:**
   - Trong `ControllerService.StartAutoReconnect()`, khi `ConnectAsync` trả về `true`, hệ thống lập tức ghi nhận trạng thái `Connected`, phục hồi luồng đọc dữ liệu và dùng lệnh `break` để thoát dứt điểm vòng lặp thử lại.
2. **Tách lớp trừu tượng SDK & Chuẩn hóa Adapter thiết bị:**
   - Tạo interface `IZKTecoSdk` và lớp wrapper `ZKTecoSdkWrapper` gọi P/Invoke thực tế.
   - Xây dựng `MockZKTecoSdk` cho phép mô phỏng toàn bộ hành vi kết nối, mở barrier, đọc log đệm, giả lập lỗi để viết Unit Test tự động 100% không cần DLL native.
   - Chuẩn hóa vòng đời thiết bị qua `IDeviceAdapter` và `IControllerService`, quản lý bằng enum `DeviceStatus` (`Disconnected`, `Connecting`, `Connected`, `Streaming`, `Reconnecting`, `Error`).
3. **Cơ chế Active TCP Socket Ping (`PingAsync`):**
   - Bổ sung phương thức `PingAsync(timeoutMs = 2000)` sử dụng `System.Net.Sockets.TcpClient` kết nối trực tiếp đến IP:Port của Controller mà không qua native SDK.
   - Cho phép kiểm tra trạng thái vật lý của dây mạng và nguồn thiết bị một cách nhẹ nhàng, nhanh chóng và an toàn tuyệt đối (không sợ treo luồng P/Invoke).
4. **Xả bộ đệm tồn đọng lúc khởi động (Stale Buffer Draining):**
   - Thiết lập mốc thời gian `_drainUntil = DateTime.Now.AddMilliseconds(1500)` ngay khi vừa kết nối thành công.
   - Tự động bỏ qua mọi sự kiện đọc được trong 1.5 giây đầu tiên để loại bỏ hoàn toàn các log cũ còn sót trong bộ đệm của thiết bị.
5. **Lọc gói tin định kỳ Door/Alarm & Debounce lỗi liên tiếp:**
   - Bỏ qua các bản ghi có `parts[4] == "255"`.
   - Thiết lập ngưỡng đếm lỗi liên tiếp (`consecutiveErrors >= 5`, tương đương ~1 giây) trong `ListenLoopAsync` trước khi chính thức kết luận mất kết nối và chuyển sang chế độ Reconnect, hạn chế tối đa hiện tượng rung lắc mạng tạm thời.
6. **Mô hình luồng đọc độc lập (Self-contained Listen Loop):**
   - Mỗi thực thể `ControllerService` tự quản lý một `Task.Run` chạy ngầm độc lập và tự động phát tán sự kiện `OnCardSwiped`.
   - `DeviceOrchestrator` chỉ đóng vai trò điều phối cấp cao, loại bỏ hoàn toàn vòng lặp tuần tự `foreach` 500ms block luồng trước đây.
7. **Đồng bộ trạng thái trực quan an toàn luồng lên `FrmMain.lbStatusCtrl`:**
   - Trong `FrmMain.cs`, cập nhật phương thức `Controller_OnStatusChanged` sử dụng `InvokeRequired` / `BeginInvoke`:
     - Khi `isConnected == true`: Nhãn `lbStatusCtrl` chuyển sang màu xanh lá (`SeaGreen`), hiển thị `BỘ ĐIỀU KHIỂN: ONLINE`.
     - Khi `isConnected == false`: Nhãn `lbStatusCtrl` chuyển sang màu đỏ (`Crimson`), hiển thị `BỘ ĐIỀU KHIỂN: OFFLINE`.

## Hệ quả

- **Tính sẵn sàng và tự phục hồi cao:** Triệt tiêu hoàn toàn lỗi rò rỉ luồng reconnect và hiện tượng ngắt/nối liên tục mỗi 5s. Khi đường truyền mạng phục hồi, Controller kết nối lại và vận hành bình thường ngay lập tức.
- **Bảo toàn dữ liệu & Chống sự kiện giả:** Không còn hiện tượng quẹt thẻ ảo hoặc mở barrier ngoài ý muốn lúc vừa bật ứng dụng hoặc vừa khôi phục kết nối.
- **Khả năng kiểm thử tự động toàn diện:** Bộ Unit Test tự động cho Controller (`ControllerServiceTests.cs`) đạt tỷ lệ Pass 100% (9/9 tests) mà không cần cấu hình phần cứng vật lý hay phụ thuộc kiến trúc 32-bit khi test.
- **Trải nghiệm vận hành minh bạch:** Nhân viên bảo vệ và kỹ thuật viên giám sát tại trạm nhận biết tình trạng kết nối của bộ điều khiển ngay lập tức thông qua màu sắc và nhãn văn bản trên Footer màn hình chính.
