# 0008: Quy trình Xử lý Ngoại lệ khi Mở Barrier Thất bại (Troubleshooting & Manual Recovery)

Hệ thống thiết lập cơ chế xử lý ngoại lệ tương tác qua `TaskDialog` khi lệnh kích mở Barrier tự động thất bại, cho phép người vận hành thử lại nhiều lần (Retry) hoặc xác nhận phương tiện đã qua cổng bằng barrier cơ (Manual Release) để bảo toàn dữ liệu phiên gửi xe `EventParking` trong MongoDB.

## Ngữ cảnh

Trong quy trình gửi xe tự động tại bãi đỗ xe HPParking, sau khi nhận diện khuôn mặt qua FaceID và quét biển số xe LPR thành công, hệ thống gửi lệnh kích relay mở Barrier qua bộ điều khiển ZKTeco Controller.

Tuy nhiên, lệnh kích mở tự động có thể thất bại do:
1. Nhầm lẫn số cổng relay (đã được sửa bằng cách ưu tiên `lane.OutputRelay` thay cho `lane.InputReader`).
2. Bộ điều khiển ZKTeco bị ngắt kết nối mạng TCP/IP, chập chờn hoặc mất nguồn tạm thời.

Trong các bãi xe thực tế, khi barrier tự động không mở được, bảo vệ tại trạm thường sử dụng remote cầm tay hoặc nút bấm vật lý để mở cưỡng bức cho xe qua nhằm tránh ùn tắc cổng. Nếu phần mềm lập tức hủy phiên gửi xe và không lưu dữ liệu vào cơ sở dữ liệu:
- Đối với lượt vào: Xe đã vào trong bãi thực tế, nhưng MongoDB không có bản ghi `EventParking`. Khi xe này ra khỏi bãi, hệ thống kiểm tra và báo lỗi `NotInParking` ("Khách hàng này không có xe trong bãi"), khiến phương tiện bị kẹt lại cổng ra.
- Đối với lượt ra: Không cập nhật được trạng thái `Status = "OUT"`, khiến xe vẫn mang trạng thái đang đỗ trong bãi (`AlreadyInParking`).

## Quyết định

1. **Sửa dứt điểm cổng Relay trong code:**
   - Trong phương thức `BarrierOpen(lane)`, hệ thống kiểm tra và kích hoạt đúng cổng rơle:
     ```csharp
     int relayPort = lane.OutputRelay > 0 ? lane.OutputRelay : lane.InputReader;
     return lane.Ctrl.OpenBarrier(relayPort, 1);
     ```
   - Đảm bảo tương thích ngược nếu các bản ghi cũ chưa gán `OutputRelay`.
2. **Quy trình tương tác xử lý sự cố (TaskDialog Workflow):**
   - Khi `BarrierOpen` trả về `false`, giao diện chính kích hoạt `TaskDialog` chuẩn của .NET 8 WinForms với 2 nút hành động:
     - **[Thử lại (Retry)]**: Thử kích hoạt lại lệnh mở relay tới bộ điều khiển. Cho phép nhấn thử lại nhiều lần nếu mạng đang tự động kết nối lại. Khi thành công, hệ thống tiếp tục lưu ảnh và ghi dữ liệu DB.
     - **[Hủy (Cancel)]**: Kích hoạt bước xác nhận phương tiện thực tế.
3. **Xác nhận Mở thủ công (Manual Release Confirmation):**
   - Khi người vận hành nhấn [Hủy], hệ thống mở hộp thoại xác nhận:
     - Với làn vào: *"Bảo vệ đã mở barrier thủ công và xe đã vào bãi chưa?"*
     - Với làn ra: *"Bảo vệ đã mở barrier thủ công và xe đã ra khỏi bãi chưa?"*
   - **Nếu chọn [Có (Yes)]**:
     - Hệ thống tiến hành lưu trữ ảnh vào đĩa (`ImageIn` / `ImageOut`).
     - Ghi nhận / Cập nhật bản ghi `EventParking` vào MongoDB.
     - Cập nhật giao diện `FrmMain`.
   - **Nếu chọn [Không (No)]**:
     - Hủy ảnh Bitmap (tránh rò rỉ bộ nhớ).
     - Không ghi nhận dữ liệu vào MongoDB.
     - Trả về mã lỗi `ProcessStatus.BarrierFailed`.

## Hệ quả

- **Vận hành liên tục (Business Continuity):** Bãi xe không bị gián đoạn hay ùn ứ khi thiết bị phần cứng gặp sự cố mạng tạm thời.
- **Bảo toàn dữ liệu đối soát:** Đảm bảo mọi phương tiện đã thực sự vào bãi (kể cả khi mở barrier bằng nút cơ) đều có bản ghi `EventParking` hợp lệ, ngăn chặn dứt điểm lỗi `NotInParking` khi xe ra.
- **Kiểm soát an toàn tài nguyên:** Ảnh Bitmap chỉ được lưu khi có xác nhận hợp lệ, và luôn được giải phóng (Dispose) sạch sẽ khi hủy bỏ để chống tràn RAM.
