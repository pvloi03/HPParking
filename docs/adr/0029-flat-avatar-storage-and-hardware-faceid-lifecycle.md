# 0029: Chiến Lược Lưu Trữ Avatar Phẳng Theo Định Danh Và Vòng Đời FaceID Phần Cứng

Quyết định thiết kế về việc quản lý tệp tin ảnh chân dung (Avatar) và chính sách đồng bộ vòng đời dữ liệu FaceID trên thiết bị phần cứng trong `HPParking.Api`: áp dụng cơ chế lưu trữ tệp tin phẳng đặt tên theo định danh khách hàng (`{Code}.jpg` hoặc `{PhoneNumber}.jpg`) không phân chia thư mục con theo ngày tháng, tự động ghi đè khi cập nhật, và thiết lập nguyên tắc phân định rõ ràng giữa Xóa mềm (bảo lưu dữ liệu trên thiết bị FaceID) và Xóa cứng (phát lệnh thu hồi thẻ và xóa người dùng trên toàn bộ thiết bị FaceID của các làn active).

## Ngữ cảnh

Trong hệ thống kiểm soát bãi xe không dùng thẻ từ (Cardless Parking System):
1. **Lưu trữ ảnh chân dung khách hàng**:
   - Ảnh khuôn mặt của Client vừa dùng để hiển thị trên giao diện Web Admin / Mobile App, vừa là nguồn dữ liệu nhị phân đẩy lên các đầu đọc Hikvision FaceID Terminal qua giao thức ISAPI.
   - Nếu áp dụng mô hình phân mảnh thư mục theo ngày tháng upload (`Uploads/Avatar/2026/09/18/{guid}.jpg`), khi một khách hàng cập nhật ảnh mới, tệp tin ảnh cũ sẽ bị bỏ rơi (orphaned files) trên đĩa gây lãng phí dung lượng lưu trữ, đồng thời đường dẫn URL của avatar thay đổi liên tục khiến các client phải xử lý cache phức tạp.
   - Hơn nữa, việc tìm kiếm ảnh gốc trên ổ đĩa máy chủ khi cần sao lưu hoặc kiểm toán thủ công sẽ rất khó khăn nếu tên file là GUID ngẫu nhiên.
2. **Xử lý xóa khách hàng và rủi ro mất dữ liệu sinh trắc học**:
   - Thao tác xóa khách hàng thường xảy ra trong nghiệp vụ nhân sự (nhân viên tạm nghỉ việc, chuyển công tác, khóa tài khoản).
   - Nếu mỗi lần xóa mềm (Soft Delete) trong Database mà API lại tự động phát lệnh xóa khuôn mặt trên thiết bị phần cứng FaceID, khi khách hàng quay trở lại làm việc (kích hoạt lại), quản trị viên sẽ buộc phải gọi người đó lên chụp ảnh và đăng ký lại từ đầu tại bàn lễ tân — gây gián đoạn vận hành nghiêm trọng.
   - Ngược lại, nếu thực hiện xóa vĩnh viễn (Hard Delete) vì lý do an ninh hoặc bảo mật thông tin cá nhân mà không dọn dẹp thiết bị ngoại vi, khuôn mặt của người đó vẫn có thể mở barrier ngoài thực tế.

## Quyết định

1. **Lưu trữ Avatar phẳng theo định danh khách hàng**:
   - Cấu hình tập trung thông qua `StorageSettings`:
     ```json
     "StorageSettings": {
       "UploadPath": "Uploads/Avatar",
       "RequestPath": "/uploads/avatar",
       "MaxFileSizeMB": 5,
       "AllowedExtensions": [ ".jpg", ".jpeg", ".png" ]
     }
     ```
   - Hỗ trợ linh hoạt cả đường dẫn tương đối (trong thư mục API) và đường dẫn tuyệt đối hoặc chia sẻ mạng UNC (`Path.IsPathRooted`) theo ADR 0012.
   - **Quy tắc đặt tên tệp**: Đặt tên trực tiếp theo định danh duy nhất của khách hàng theo thứ tự ưu tiên: `{Client.Code}.jpg` (nếu có CCCD/Mã nhân viên) hoặc `{Client.PhoneNumber}.jpg`.
   - **Cơ chế ghi đè tự động (In-Place Overwrite)**: Khi khách hàng upload ảnh mới, tệp tin cũ cùng tên bị ghi đè trực tiếp. Đường dẫn truy cập công khai của ảnh (`/uploads/avatar/{Code}.jpg`) giữ nguyên bất biến, không sinh tệp rác trên ổ cứng.

2. **Chính sách phân định vòng đời FaceID trên thiết bị phần cứng**:
   - Khi gọi `DELETE /api/v1/clients/{id}`:
     * **Xóa mềm (`hardDelete = false`, mặc định)**:
       - Đánh dấu `Client.IsDeleted = true`, `DeletedAt = DateTime.UtcNow`.
       - Quét toàn bộ phương tiện thuộc sở hữu của khách hàng và cập nhật `Vehicle.IsDeleted = true`.
       - **BẢO LƯU 100% DỮ LIỆU PHẦN CỨNG**: Tuyệt đối không phát bất kỳ lệnh xóa nào tới đầu đọc FaceID. Dữ liệu khuôn mặt và thẻ SĐT vẫn được lưu trong bộ nhớ phần cứng ngoại vi.
     * **Xóa cứng (`hardDelete = true`)**:
       - Xóa vĩnh viễn dữ liệu Client và các Vehicle liên kết khỏi MongoDB.
       - Xóa tệp tin ảnh Avatar vật lý tương ứng trên đĩa lưu trữ.
       - **THU HỒI PHẦN CỨNG TOÀN DIỆN**: Truy vấn danh sách thiết bị FaceID thuộc tất cả các làn xe đang kích hoạt (`Lane.IsActive == true`), lọc trùng IP qua `HashSet<string>`, và đồng thời phát lệnh:
         1. `DeleteCardAsync(ip, client.PhoneNumber)`: Thu hồi thẻ số điện thoại.
         2. `DeleteUserAsync(ip, client.Id)`: Xóa thông tin người dùng và mẫu vector khuôn mặt khỏi bộ nhớ thiết bị.

3. **Quy tắc Tương tác Thiết bị: Fail-Fast Ping 600ms, Chạy Song Song & Không Rollback Xóa User**:
   - **Fail-Fast Ping 600ms**: Trước khi gửi bất kỳ lệnh ISAPI nào tới đầu đọc FaceID, hệ thống ping kiểm tra trong 600ms (`PingFastAsync`). Nếu thiết bị mất mạng LAN, ngắt ngay lập tức với lỗi `"Mất kết nối tới thiết bị"`, tránh việc bị treo kết nối do TCP timeout kéo dài 15-30 giây.
   - **Xử lý song song (`Task.WhenAll`)**: Mọi lệnh CRUD hoặc đồng bộ tới danh sách $N$ thiết bị FaceID được thực thi đồng thời qua `Task.WhenAll`, đảm bảo thời gian phản hồi toàn trình của API luôn $< 0.8$ giây kể cả khi có thiết bị offline.
   - **Loại bỏ vĩnh viễn cơ chế Rollback User (`RollbackUserAsync`)**: Khi tạo mới hoặc đồng bộ FaceID, nếu các bước sau (gán thẻ hoặc nạp ảnh) thất bại (do ảnh mờ, camera bận hoặc lỗi mạng cục bộ), hệ thống **TUYỆT ĐỐI KHÔNG rollback xóa User khỏi thiết bị**. Thông tin người dùng được bảo toàn nguyên vẹn trên đầu đọc để phục vụ retry/đồng bộ lại.
   - **Tự động Self-Healing dọn thẻ cũ (`CleanAndAssignCardAsync`)**: Khi khách hàng đổi số điện thoại (mã thẻ), hệ thống tự động quét toàn bộ thẻ đã gán cho User trên đầu đọc, gửi lệnh xóa sạch các thẻ cũ không khớp và nạp thẻ mới, loại bỏ triệt để rủi ro tồn đọng thẻ rác.

## Hệ quả

- **Tích cực**:
  - Tên tệp avatar trên đĩa khớp 1-1 với mã định danh khách hàng, cực kỳ dễ tra cứu, kiểm toán và sao lưu.
  - Triệt tiêu 100% tệp tin rác mồ côi (orphaned files) khi khách hàng cập nhật ảnh đại diện nhiều lần.
  - Bảo vệ mẫu sinh trắc học quý giá của khách hàng trong các tình huống tạm khóa hoặc xóa mềm thông thường, tránh chi phí vận hành đăng ký lại.
  - Đảm bảo an ninh tuyệt đối khi xóa cứng: dọn sạch dữ liệu từ Database, Ổ đĩa máy chủ đến Bộ nhớ thiết bị đầu đọc FaceID.
  - Tốc độ API cực nhanh (< 0.8s) nhờ Fail-Fast Ping 600ms và chạy song song `Task.WhenAll`, không bao giờ bị nghẽn mạng do thiết bị ngoại vi offline.
  - Loại bỏ hoàn toàn lỗi mất người dùng trên FaceID và tự động dọn sạch thẻ cũ khi đổi số điện thoại.
- **Đánh đổi**:
  - Trình duyệt Web có thể lưu cache ảnh cũ nếu không dùng kỹ thuật thêm query string thời gian (ví dụ: `/uploads/avatar/NV01.jpg?t=123456`) khi tải lại trang sau khi upload ảnh mới.
