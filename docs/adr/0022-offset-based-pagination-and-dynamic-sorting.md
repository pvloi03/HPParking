# 0022: Chuẩn Hóa Phân Trang Offset-Based 1-Indexed, Sắp Xếp Động An Toàn Với Whitelist Và Kẹp Trần MaxPageSize

Quyết định chuẩn hóa cơ chế phân trang (Pagination) và sắp xếp dữ liệu (Sorting) cho `HPParking.Api`: áp dụng mô hình Offset-based Pagination chuẩn 1-indexed (`pageIndex` mặc định = 1, `pageSize` mặc định = 20), thiết lập cơ chế kẹp trần an toàn chống tràn bộ nhớ (`MaxPageSize = 100`), bao gói siêu dữ liệu phân trang đầy đủ (`totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`), sắp xếp cố định mặc định theo `CreatedAt desc` để đảm bảo thứ tự bản ghi bất biến giữa các trang, hỗ trợ sắp xếp động (`sortBy` và `sortOrder`) có kiểm tra danh sách trắng (Whitelist), và tạm hoãn tạo Index MongoDB thủ công trong giai đoạn 1 theo nguyên tắc tinh gọn YAGNI.

## Ngữ cảnh

Trong các dịch vụ REST API quản trị bãi đỗ xe thông minh, các danh sách thực thể như Khách hàng (`Clients`), Phương tiện (`Vehicles`) và Lượt gửi xe (`ParkingSessions`) liên tục gia tăng theo thời gian:
1. **Lựa chọn mô hình phân trang phù hợp:**
   - Cân nhắc giữa:
     * *Cursor-based Pagination (Keyset)*: Tối ưu cho cuộn vô tận trên mạng xã hội, nhưng người dùng Web Admin bãi xe không thể nhảy thẳng đến trang mong muốn, không chọn được số dòng hiển thị trên trang và không biết được tổng số trang.
     * *Offset-based Pagination*: Cho phép giao diện bảng hiển thị đầy đủ các nút nhảy trang (1, 2, 3... Trang cuối) và bộ chọn kích thước trang (10, 20, 50, 100 dòng). Đây là giải pháp phù hợp nhất với trải nghiệm người dùng quản trị.
2. **Quy ước chỉ số trang (0-based vs 1-based):**
   - 1-based indexing (`pageIndex = 1` là trang đầu tiên) là quy ước tự nhiên nhất với con người và các thư viện UI (Ant Design, Material UI, Bootstrap Table). Nếu client gửi `pageIndex <= 0`, server tự động chuẩn hóa về `1`.
3. **Bảo vệ tài nguyên máy chủ (Memory Exhaustion Protection):**
   - Nếu không giới hạn kích thước trang tối đa, một yêu cầu độc hại với `pageSize=1000000` có thể làm cạn kiệt RAM của API và quá tải MongoDB. Do đó, cần cơ chế kẹp trần cứng: `Math.Clamp(pageSize, 1, 100)`.
4. **Vấn đề nhảy lộn xộn thứ tự bản ghi (Non-deterministic Sorting):**
   - Trong MongoDB, khi phân trang bằng `skip` và `limit` mà không chỉ định trường sắp xếp cố định, thứ tự trả về giữa các trang có thể bị thay đổi ngẫu nhiên, khiến bản ghi ở trang 1 có thể xuất hiện lại ở trang 2.
   - Giải pháp: Luôn áp dụng sắp xếp mặc định theo `CreatedAt desc` (mới nhất lên đầu).
5. **Sắp xếp động an toàn (Safe Dynamic Sorting):**
   - Cho phép client sắp xếp theo cột trên giao diện bảng qua `sortBy` (`name`, `createdAt`, `phoneNumber`, `code`) và `sortOrder` (`asc`, `desc`), đồng thời có cơ chế Whitelist để chặn đứng rủi ro injection hoặc sort theo trường không hợp lệ.

## Quyết định

1. **Chuẩn Hóa Tham Số Phân Trang Đầu Vào (`PaginationQuery`):**
   - `pageIndex`: Số nguyên >= 1, mặc định = 1.
   - `pageSize`: Số nguyên trong khoảng [1, 100], mặc định = 20.
   - `sortBy`: Tên trường cần sắp xếp (chuỗi ký tự, mặc định: `"createdAt"`).
   - `sortOrder`: Hướng sắp xếp (`"desc"` hoặc `"asc"`, mặc định: `"desc"`).

2. **Cơ Chế Kẹp Trần An Toàn (Safe Guarding):**
   - Nếu `pageSize <= 0`: Tự động gán = 20.
   - Nếu `pageSize > 100`: Tự động kẹp về = 100.
   - Nếu `pageIndex <= 0`: Tự động gán = 1.

3. **Cấu Trúc Siêu Dữ Liệu Phản Hồi (`PagedResult<T>`):**
   ```json
   {
     "success": true,
     "data": [ ... ],
     "pagination": {
       "pageIndex": 1,
       "pageSize": 20,
       "totalCount": 125,
       "totalPages": 7,
       "hasPreviousPage": false,
       "hasNextPage": true
     },
     "message": null,
     "errors": []
   }
   ```
   - Công thức tính số trang: `totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)`.
   - Nếu `totalCount == 0`: `totalPages = 0`, `data = []` (mảng rỗng, không bao giờ null).

4. **Whitelist Kiểm Tra Sắp Xếp Động:**
   - Danh sách trường hợp lệ cho `Client`: `createdAt`, `name`, `phoneNumber`, `code`.
   - Danh sách trường hợp lệ cho `Vehicle`: `createdAt`, `plateNumber`, `type`.
   - Nếu client truyền trường ngoài Whitelist, hệ thống tự động fallback về sắp xếp mặc định `createdAt desc` mà không gây crash lỗi.

5. **Chính Sách Chỉ Mục MongoDB (YAGNI):**
   - Tạm thời chưa tạo index bổ sung trong giai đoạn 1, dựa trên cơ chế truy vấn mặc định của MongoDB nhằm giữ mã nguồn tinh gọn và khởi động nhanh chóng.

## Hệ quả

- **Trải nghiệm bảng biểu mượt mà:** Tương thích tự nhiên với toàn bộ các thư viện UI bảng phân trang phổ biến trên React/Vue/Flutter.
- **An toàn trước tấn công cạn kiệt tài nguyên:** Ngăn chặn tuyệt đối nguy cơ tràn RAM máy chủ do yêu cầu kích thước trang bất thường.
- **Tính nhất quán dữ liệu cao:** Thứ tự các bản ghi giữa các trang luôn ổn định, không bao giờ bị trùng lặp hay sót bản ghi khi chuyển trang.
