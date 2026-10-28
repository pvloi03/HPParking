# 0006: Kiến trúc Xác thực Không Thẻ Từ: Nhận diện FaceID và truyền PhoneNumber qua chuẩn Wiegand

> **Trạng thái:** Bị thay thế (Superseded) bởi [ADR 0038](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0038-unified-card-and-multi-auth-verification-architecture.md) và [ADR 0040](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0040-multi-target-lane-architecture-and-tuple-pattern-matching.md).  
> **Lý do thay thế:** Hệ thống đã chuyển đổi từ mô hình "Không dùng thẻ (Cardless) thuần túy" sang mô hình Xác thực đa phương thức thống nhất (Unified Multi-Authentication), hỗ trợ song song Thẻ từ RFID vật lý chuẩn 10 số, Nhận diện FaceID qua SDK native, Cảm biến Radar sóng milimet ngõ AUX IN và Nút bấm cưỡng bức bảo vệ.

Hệ thống ban đầu loại bỏ thẻ từ vật lý và thử nghiệm mô hình xác thực 100% bằng FaceID truyền số điện thoại qua chuẩn Wiegand sang bộ điều khiển ZKTeco Controller.

## Ngữ cảnh

Trong các hệ thống bãi giữ xe truyền thống sử dụng thẻ từ RFID/Mifare, doanh nghiệp gặp phải nhiều nhược điểm:
1. Chi phí mua sắm, in ấn và cấp phát thẻ từ vật lý liên tục khi bị thất lạc, hỏng hóc.
2. Rủi ro gian lận: Người dùng có thể cho mượn thẻ, quẹt hộ hoặc tráo đổi phương tiện.
3. Bất tiện: Khách hàng phải dừng xe, hạ kính, với tay quẹt thẻ gây ùn tắc tại cổng vào những khung giờ cao điểm.

Để giải quyết triệt để, hệ thống chuyển dịch sang mô hình **Không dùng thẻ (Cardless)**:
- Phương tiện vào/ra được nhận diện khuôn mặt tự động bằng thiết bị chuyên dụng FaceID Terminal.
- Bộ điều khiển trung tâm ZKTeco tại trạm kiểm soát vốn dĩ là thiết bị phần cứng Access Control nhận tín hiệu theo chuẩn Wiegand từ các đầu đọc thẻ chuẩn công nghiệp.

## Quyết định

1. **Lưu trữ Số điện thoại thay cho Mã thẻ:**
   - Trong quá trình đăng ký hồ sơ khách hàng ([FrmRegisterClient.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Forms/FrmRegisterClient.cs)), hàm `apiService.AddCardAsync(idCode, phone)` truyền trực tiếp số điện thoại (`phone`) vào trường `cardNumber` trong bộ nhớ của đầu đọc FaceID.
   - Trong cơ sở dữ liệu MongoDB (`Client.cs` và `EventParking.cs`), trường `Card_Code` được gán cố định bằng số điện thoại (`client.PhoneNumber`).
2. **Truyền tín hiệu qua chuẩn Wiegand phần cứng:**
   - Cổng Wiegand Output trên đầu đọc FaceID được đấu nối trực tiếp vào cổng Wiegand Input trên bộ điều khiển ZKTeco Controller.
   - Khi chủ xe đến cổng, đầu đọc FaceID quét khuôn mặt thành công sẽ lập tức bắn chuỗi số điện thoại qua đường truyền Wiegand sang Controller.
3. **Tiếp nhận sự kiện tại phần mềm:**
   - Bộ điều khiển ZKTeco ghi nhận sự kiện và gửi gói tin TCP/IP `RealtimeLog` về phần mềm với trường `CardNo` chứa số điện thoại.
   - Do định dạng số nguyên của chuẩn Wiegand thường làm rụng ký tự `0` ở đầu số điện thoại Việt Nam, phần mềm bù thêm tiền tố `0` (`$"0{data.CardNo}"`) để truy vấn chính xác khách hàng trong cơ sở dữ liệu.

## Hệ quả

- **Tối ưu trải nghiệm:** Khách hàng không cần mang theo bất kỳ chiếc thẻ nào, chỉ cần hướng mặt về phía camera FaceID là barrier tự động mở.
- **Tính toàn vẹn & Thống nhất:** Giải thích rõ ràng tại sao trường `Card_Code` trong toàn bộ mã nguồn và database luôn mang giá trị của `PhoneNumber`, loại bỏ hoàn toàn sự hoài nghi của các lập trình viên hoặc AI về việc "lấy nhầm thuộc tính".
- **Không dùng thẻ vật lý:** Toàn bộ tài liệu nghiệp vụ, giao diện và hướng dẫn vận hành không còn khái niệm thẻ từ RFID.
