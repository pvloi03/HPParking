# 0020: Chuẩn Hóa Mã Trạng Thái HTTP (RFC 9110), Ánh Xạ Lỗi Xung Đột 409 Conflict Và Phản Hồi Đồng Nhất Qua ApiResponse

Quyết định chuẩn hóa toàn diện ma trận mã trạng thái HTTP (HTTP Status Codes) cho dịch vụ `HPParking.Api`: tuân thủ nghiêm ngặt ngữ nghĩa chuẩn RESTful RFC 9110 (200, 201, 400, 401, 403, 404, 409, 429, 500), sử dụng mã chuyên biệt `409 Conflict` cho các trường hợp vi phạm tính duy nhất (trùng số điện thoại hoặc trùng biển số xe đang kích hoạt), chuẩn hóa thao tác XÓA (`DELETE`) trả về `200 OK` kèm `ApiResponse<bool>` chứa thông điệp rõ ràng để đảm bảo 100% tính đồng nhất của body JSON cho Frontend, và phản hồi `200 OK` kèm báo cáo chi tiết thiết bị cho kịch bản đồng bộ FaceID thành công một phần (Partial Success).

## Ngữ cảnh

Trong thiết kế REST API chuyên nghiệp, mã trạng thái HTTP (HTTP Status Code) đóng vai trò là hợp đồng giao tiếp chuẩn mực ở tầng giao vận:
1. **Sự mơ hồ giữa lỗi nhập liệu (Bad Request) và lỗi xung đột dữ liệu (Conflict):**
   - Lỗi nhập liệu do thiếu trường hoặc sai định dạng (`Validation Error`) khác biệt bản chất với lỗi xung đột trạng thái dữ liệu trong CSDL (ví dụ số điện thoại đã được đăng ký cho người khác).
   - Nếu trả về chung mã 400 Bad Request, ứng dụng phía Frontend (Web Admin / Mobile App) rất khó nhận biết để hiển thị thông báo phù hợp (ví dụ: cảnh báo mở hồ sơ khách hàng cũ).
   - Do đó, việc tách biệt rõ ràng: `400 Bad Request` cho vi phạm validation và `409 Conflict` cho vi phạm tính duy nhất là quy chuẩn thiết kế tối ưu.
2. **Sự đánh đổi trong thao tác Xóa (DELETE): 200 OK vs 204 No Content:**
   - Chuẩn lý thuyết RESTful thuần túy thường đề xuất mã `204 No Content` (body rỗng hoàn toàn) khi xóa thành công.
   - Tuy nhiên, trong thực tế phát triển Web/Mobile, các thư viện client phổ biến như Fetch API hoặc Axios khi gọi hàm `.json()` trên một response rỗng sẽ ném lỗi ngoại lệ cú pháp (`SyntaxError: Unexpected end of JSON input`). Ngoài ra, body rỗng không thể truyền tải câu thông báo tiếng Việt giải thích kết quả cho người dùng.
   - Quyết định: Chuẩn hóa mọi response (kể cả DELETE) đều trả về `200 OK` kèm bao gói `ApiResponse<bool>` có body JSON chứa thông điệp rõ ràng.
3. **Kịch bản Đồng bộ FaceID Thành công một phần (Partial Success):**
   - Trong bãi đỗ xe có nhiều làn và nhiều đầu đọc FaceID kết nối qua mạng LAN, có thể xảy ra trường hợp 2 thiết bị nạp thành công nhưng 1 thiết bị ở cổng phụ bị mất mạng (offline).
   - Nếu trả về lỗi `500 Internal Server Error`, hệ thống sẽ coi toàn bộ quá trình là thất bại và chặn luồng nghiệp vụ.
   - Do đó, trả về `200 OK` kèm báo cáo chi tiết (`totalDevices`, `successCount`, `failedCount`, danh sách trạng thái từng IP) giúp hệ thống vận hành liên tục mà vẫn cảnh báo chính xác cho bảo vệ về thiết bị đang lỗi.

## Quyết định

1. **Ma Trận Mã Trạng Thái HTTP Toàn Diện:**
   | Mã HTTP | Tên chuẩn | Ngữ nghĩa nghiệp vụ trong HPParking.Api |
   |---|---|---|
   | **200 OK** | Success | Truy vấn thành công (`GET`), cập nhật thành công (`PUT`), xóa mềm thành công (`DELETE`), đổi mật khẩu thành công, đồng bộ FaceID (toàn bộ hoặc một phần). |
   | **201 Created** | Created | Tạo mới thành công hồ sơ khách hàng (`POST /api/v1/clients`) hoặc thêm xe mới (`POST /api/v1/clients/{id}/vehicles`), kèm header `Location`. |
   | **400 Bad Request** | Validation / Client Error | Vi phạm quy tắc kiểm tra FluentValidation (dữ liệu rỗng, sai format), sai mật khẩu hiện tại khi đổi mật khẩu, thông tin đăng nhập không chính xác. |
   | **401 Unauthorized** | Unauthenticated | Thiếu token hoặc token JWT hết hạn/sai chữ ký; thiếu hoặc sai khóa `X-API-KEY`. |
   | **403 Forbidden** | Access Denied | Tài khoản bị vô hiệu hóa (`IsActive == false`), hoặc người dùng không đủ quyền hạn vai trò (ví dụ: `Manager` gọi `DELETE`). |
   | **404 Not Found** | Resource Not Found | Không tìm thấy Client hoặc Vehicle theo định danh Id trong CSDL; endpoint không tồn tại. |
   | **409 Conflict** | Business Conflict | Xung đột tính duy nhất: Số điện thoại (`PhoneNumber`) đã thuộc về Client khác; hoặc Biển số xe (`PlateNumber`) đã được đăng ký cho phương tiện đang kích hoạt khác. |
   | **429 Too Many Requests** | Rate Limited | Vượt quá giới hạn tần suất gọi API (ví dụ: đăng nhập sai quá 5 lần trong 1 phút). |
   | **500 Internal Server Error** | Server Error | Lỗi ngoại lệ hệ thống không mong muốn được bắt và xử lý tại `ExceptionMiddleware`. |

2. **Quy Chuẩn Phản Hồi Cho Thao Tác Xóa:**
   - Endpoint `DELETE /api/v1/clients/{id}` và `DELETE /api/v1/vehicles/{id}` phản hồi HTTP **200 OK** với cấu trúc:
     ```json
     {
       "success": true,
       "data": true,
       "message": "Đã xóa mềm khách hàng và thu hồi quyền FaceID thành công.",
       "errors": []
     }
     ```

3. **Quy Chuẩn Phản Hồi Khi Đồng Bộ FaceID:**
   - Endpoint `POST /api/v1/clients/{id}/sync-faceid` phản hồi HTTP **200 OK** với `data` chứa `FaceIdSyncResultDto`:
     ```json
     {
       "success": true,
       "data": {
         "clientId": "64f1a2b3c4d5e6f7a8b9c0d1",
         "totalDevices": 3,
         "successCount": 2,
         "failedCount": 1,
         "devices": [
           { "deviceIp": "192.168.1.205", "isSuccess": true, "message": "Nạp thành công" },
           { "deviceIp": "192.168.1.206", "isSuccess": false, "message": "Lỗi kết nối timeout" }
         ]
       },
       "message": "Đã hoàn tất đồng bộ FaceID (2/3 thiết bị thành công).",
       "errors": []
     }
     ```

## Hệ quả

- **Tương thích hoàn hảo với Frontend:** Loại bỏ triệt để các lỗi phân giải JSON rỗng trên Web/Mobile, cấu trúc response nhất quán 100%.
- **Bắt lỗi nghiệp vụ trực quan:** Ứng dụng phía client có thể dựa vào mã `409 Conflict` để kích hoạt các kịch bản đối soát dữ liệu trùng lặp chuyên biệt cho người dùng.
- **Tính khả dụng cao của hệ thống bãi xe:** Bãi đỗ xe không bị gián đoạn hoạt động đăng ký cư dân chỉ vì một thiết bị cổng phụ gặp sự cố tạm thời.
