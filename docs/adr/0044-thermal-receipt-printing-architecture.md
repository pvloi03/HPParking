# 0044: Kiến Trúc In Biên Lai Thu Phí Đỗ Xe Khổ Nhiệt K80 (`ParkingReceipt` Thermal Printing Architecture)

> **Trạng thái:** Chấp thuận (Accepted)  
> **Liên quan (Related):** [ADR 0024](0024-realtime-dashboard-and-domain-statistics-architecture.md), [ADR 0040](0040-multi-target-lane-architecture-and-tuple-pattern-matching.md), [ADR 0043](0043-parking-fee-calculation-and-tariff.md)

Tài liệu quyết định kiến trúc (ADR) quy định về thiết kế và hiện thực phân hệ in **Biên lai thu phí đỗ xe (`ParkingReceipt`)** trên máy in hóa đơn nhiệt khổ **K80 (80mm)** tại trạm kiểm soát cổng (`FrmMain`).

---

## Ngữ cảnh

1. **Nhu Cầu Cấp Chứng Từ Thu Phí Tức Thì Tại Làn Ra**:
   - Sau khi hệ thống tính phí đỗ xe theo biểu phí (`Tariff` - ADR 0043) và ghi nhận giao dịch hợp lệ tại Làn Ra (`Outbound Lane`) có phát sinh phí (`FeeAmount > 0`), khách hàng (đặc biệt là khách vãng lai hoặc tài xế thanh toán công tác phí) có nhu cầu nhận biên lai in giấy xác nhận số tiền đã thu kèm thời gian vào/ra và mã QR đối soát.
   - Thao tác in biên lai phải thuận tiện ngay trên khung thông tin làn ra đang hiển thị kết quả vừa quẹt thẻ, không làm rối bố cục giám sát 4 làn và cho phép in lại (`Reprint`) nếu máy in hết giấy hoặc kẹt giấy mà không làm gián đoạn luồng điều khiển barie.
2. **Hạn Chế Của In Trực Tiếp Từ `WebView2` Hoặc Raw ESC/POS**:
   - **In từ `WebView2.CoreWebView2.PrintAsync`**: Khung thông tin `lane-card.html` được thiết kế tối ưu cho màn hình giám sát ngang (hiển thị 2 ảnh camera song song, nền màu, hiệu ứng viền), hoàn toàn không phù hợp với khổ giấy cuộn dọc hẹp K80 (vùng in thực tế 72mm). Ngoài ra, engine Chromium dễ sinh khoảng trắng thừa cuối trang A4 làm trôi giấy nhiệt.
   - **In Raw ESC/POS qua cổng COM/USB**: Phụ thuộc cứng vào bảng mã tiếng Việt của từng dòng firmware máy in nhiệt (dễ lỗi dấu tiếng Việt Unicode dựng sẵn), phức tạp khi nạp ảnh bitmap Logo thương hiệu và mã QR.
3. **Bất Đồng Nhất Múi Giờ Giữa `InTime` Và `OutTime` Trong Bộ Nhớ**:
   - Trong `ParkingSession`, thuộc tính `InTime` được đánh dấu `[BsonDateTimeOptions(Kind = DateTimeKind.Local)]` nên khi nạp từ MongoDB có `Kind = Local`. Ngược lại, thuộc tính `OutTime` vừa được gán trực tiếp trong bộ nhớ tại luồng `ProcessExitAsync` thông qua `VietnamTime.ToUtcInstant(...)` lại mang `Kind = Utc`.
   - Nếu trích xuất và định dạng trực tiếp `InTime` / `OutTime` mà không chuẩn hóa qua `VietnamTime.ToVietnamTime(...)`, giờ ra trên biên lai và trong chuỗi QR Code sẽ bị lệch 7 tiếng (hiển thị giờ UTC thay vì giờ Việt Nam UTC+7).
4. **Đặc Thù Hệ Tọa Độ GDI+ (`PrintDocument`) & Chế Độ Nội Suy Hình Ảnh**:
   - Mặc định `PrintDocument.DefaultPageSettings.Margins` của .NET là `100` (1 inch = 25.4mm mỗi lề); nếu không đưa về `0`, hai lề trái/phải sẽ nuốt mất hơn 50mm trên tổng khổ 80mm của giấy nhiệt K80.
   - Trên cùng một biên lai có hai loại hình ảnh có bản chất trái ngược nhau: **Logo thương hiệu (`logo.png`)** là ảnh raster đa sắc cần làm mượt khi thu nhỏ (`HighQualityBicubic`), trong khi **Mã QR (`QRCoder`)** là ma trận điểm ảnh đen trắng 1-bit cần giữ biên cạnh vuông sắc nét tuyệt đối (`NearestNeighbor`) trên đầu in nhiệt 203 DPI.

---

## Quyết định

### 1. Chuẩn Hóa Thuật Ngữ Domain (`ParkingReceipt`)
- Thống nhất sử dụng thuật ngữ **`ParkingReceipt`** (*Biên lai thu phí đỗ xe*) trong toàn bộ mã nguồn, giao diện dịch vụ và kiểm thử.
- Không sử dụng các từ dễ gây nhầm lẫn với vé lượt đầu vào hoặc hóa đơn thuế GTGT điện tử như `Bill`, `Ticket`, `Invoice`, `PaymentSlip`.

### 2. Kiến Trúc Engine In C# GDI+ (`PrintDocument`) + `QRCoder` Trên Luồng Nền
- Xây dựng module sâu (`Deep Module`) thông qua giao diện `IParkingReceiptPrinterService` và lớp hiện thực `ParkingReceiptPrinterService`:
  - Sử dụng `System.Drawing.Printing.PrintDocument` kết hợp thư viện `QRCoder` (hoạt động hoàn toàn offline tại trạm cổng).
  - Gán tường minh `PrintController = new StandardPrintController()` để đảm bảo tiến trình gửi lệnh in xuống Windows Print Spooler diễn ra âm thầm trên luồng nền (`Task.Run`).
  - Tên máy in được cấu hình linh hoạt qua khóa `ReceiptPrinterName` trong `App.config` (nếu để trống sẽ tự động sử dụng máy in mặc định **Default Printer** của Windows).
  - Trước khi in, hệ thống kiểm tra `PrinterSettings.IsValid`; nếu máy in chưa kết nối hoặc chưa cài đặt, trả về `ParkingReceiptPrintResult.Failure(...)` để `FrmMain` hiển thị cảnh báo qua `ParkingTaskDialog.ShowWarning` mà không làm văng ngoại lệ.

### 3. Thiết Lập Khổ Giấy K80 Động & Tách Biệt `GraphicsState` Cho Logo Và QR Code
1. **Khổ giấy K80 & Bố cục theo chiều rộng thực tế (`contentWidth`)**:
   - Đặt `DefaultPageSettings.Margins = new Margins(0, 0, 0, 0)` để tận dụng toàn bộ vùng in nhiệt.
   - Tự động giữ nguyên khổ giấy của driver nếu đã cấu hình sẵn khổ K80 (~72–80mm), hoặc thiết lập `PaperSize("K80 (80mm)", 283, 1169)` (`72mm ≈ 283` đơn vị `1/100 inch`) khi in trên driver mặc định khổ lớn.
   - Trong `OnPrintPage`, chiều rộng bố cục `contentWidth` được lấy động từ vùng in thực tế (`e.Graphics.VisibleClipBounds.Width` / `e.PageSettings.PrintableArea.Width`), không hard-code kích thước cố định.
2. **Tách biệt `GraphicsState` theo từng loại hình ảnh**:
   - **Logo thương hiệu (`Assets/UI/logo.png`)**: Bọc trong `GraphicsState` riêng với `InterpolationMode.HighQualityBicubic`, `SmoothingMode.HighQuality` và `PixelOffsetMode.HighQuality` để đường nét và chữ trên logo mượt mà khi thu phóng theo tỷ lệ `contentWidth`.
   - **Mã QR đối soát (`QRCoder` — `ECCLevel.M`)**: Bọc trong `GraphicsState` riêng với `InterpolationMode.NearestNeighbor` và `PixelOffsetMode.Half` để giữ nguyên biên vuông sắc nét của từng module QR trên đầu in nhiệt 203 DPI, sau đó `Restore` lại trạng thái `Graphics`.
3. **Khoảng Đệm Đẩy Giấy Cuối Phiếu (Trailing Feed)**:
   - Sau dòng lời cảm ơn ở chân phiếu, vẽ thêm khoảng đệm trắng (trailing feed) để phần chân biên lai trôi hẳn qua miệng máy in/lưỡi răng cưa phục vụ xé giấy thủ công (hoặc kích hoạt dao cắt tự động theo cấu hình *End of Document* của Windows Printer Driver).

### 4. Chuẩn Hóa Múi Giờ Việt Nam (`UTC+7`) Bắt Buộc Qua `VietnamTime.ToVietnamTime`
- Mọi điểm tiếp nhận và xử lý thời gian của biên lai — bao gồm `TryCreateReceiptData`, `BuildQrPayload` và hàm render trang in GDI+ — **bắt buộc** quy đổi `InTime`, `OutTime` và `PrintedAt` qua `VietnamTime.ToVietnamTime(...)` trước khi:
  - Tính thời lượng đỗ xe (`outVn - inVn`).
  - Định dạng chuỗi hiển thị trên mặt phiếu (`HH:mm:ss dd/MM/yyyy`).
  - Kết xuất chuỗi đối soát trong Mã QR:
    `HPPARKING|ID:{SessionId}|BS:{PlateNumber}|THE:{CardNumber}|VAO:{inVn:yyyyMMddHHmmss}|RA:{outVn:yyyyMMddHHmmss}|PHI:{FeeAmount:0}`

### 5. Tương Tác Nút Trượt Hover Trên `lane-card.html` (`WebView2`)
- Trên thẻ thông tin `WebView2` của từng làn (`lane-card.html`), bổ sung nút bấm trượt từ cạnh dưới lên (`🖨️ IN BIÊN LAI THU PHÍ`, `w-full h-max`, có padding):
  - Chỉ kích hoạt khi `CanPrintReceipt == true` (`!isEntry && !isIdle && isSuccess && result.FeeAmount > 0 && result.ParkingSession != null`).
  - Chỉ xuất hiện khi nhân viên trực cổng rê chuột (`hover`) vào khung thông tin của làn ra đó, giữ nguyên không gian hiển thị biển số và ảnh đối chiếu trong suốt quá trình giám sát bình thường.
  - Hỗ trợ nhấn in lại nhiều lần kèm cơ chế khóa chống bấm đúp (`debounce 1000ms`), gửi thông điệp `"PRINT_RECEIPT"` qua `window.chrome.webview.postMessage` về `FrmMain`.

---

## Hệ quả

### Tích cực
- **Tốc độ in tức thì & Không chặn UI**: Lệnh dựng trang GDI+ và đẩy xuống Windows Spooler hoàn tất trong vài chục mili-giây trên luồng nền, nhân viên trực cổng vẫn nhấn phím nóng `F1`–`F4` mở barie bình thường.
- **Chất lượng bản in sắc nét trên khổ K80**: Tiếng Việt Unicode rõ ràng, Logo thương hiệu mượt và Mã QR quét nhạy bằng mọi ứng dụng quét mã tiêu chuẩn.
- **Chính xác tuyệt đối về thời gian và số tiền**: Triệt tiêu hoàn toàn lỗi lệch múi giờ 7 tiếng giữa `InTime` (`Local`) và `OutTime` (`Utc`).

### Đánh đổi
- Máy trạm cổng cần cài đặt sẵn Windows Printer Driver cho máy in nhiệt K80 (hoặc đặt làm Default Printer trong Windows).
