# 0031: Kiến Trúc Thùng Rác, Khôi Phục Dữ Liệu Và Chính Sách Chặn Xóa Toàn Diện

Quyết định thiết kế về việc chuẩn hóa cơ chế quản lý vòng đời dữ liệu, Thùng rác (Recycle Bin), khôi phục dữ liệu (Restore) và áp dụng chính sách Chặn xóa toàn diện (Universal Restrict Deletion Policy) cho toàn bộ các thực thể trong `HPParking.Api`: loại bỏ hoàn toàn cơ chế xóa lan truyền (Cascade Delete), áp dụng bắt buộc mã lỗi `409 Conflict` khi xóa bất kỳ thực thể cha nào còn bản ghi con liên kết, hỗ trợ truy vấn thùng rác qua tham số `?onlyDeleted=true`, chuẩn hóa endpoint khôi phục `POST /api/v1/{resource}/{id}/restore`, và kiểm soát nghiêm ngặt thứ tự khôi phục (Cha trước - Con sau) kèm xác thực lại tính duy nhất của dữ liệu.

## Ngữ cảnh

Trong các phiên bản trước:
1. **Sự thiếu nhất quán trong chính sách xóa**:
   - Mối quan hệ `Client` -> `Vehicle` ban đầu áp dụng cơ chế Cascade Soft Delete (xóa Client thì tự động xóa mềm các Vehicle thuộc sở hữu).
   - Tuy nhiên, các quan hệ khác như `Company` -> `Department`/`Gate`, `Department` -> `Client`, `Gate` -> `Lane`, `Device` -> `Lane` lại áp dụng cơ chế Chặn xóa (Restrict — 409 Conflict) theo ADR 0030.
   - Việc duy trì hai cơ chế xóa khác nhau gây khó khăn cho trải nghiệm người dùng trên Web Admin, khó kiểm toán dấu vết lịch sử và dễ gây mất mát phương tiện ngoài ý muốn khi quản trị viên thao tác xóa khách hàng.
2. **Nhu cầu khôi phục dữ liệu (Restore / Recycle Bin)**:
   - Các thao tác xóa mềm (`IsDeleted = true`) đưa bản ghi vào trạng thái lưu trữ thùng rác.
   - Khi khách hàng quay trở lại làm việc, xe gửi lại, hoặc nhân sự bấm nhầm nút xóa, hệ thống cần cung cấp giao diện và API chuẩn mực để xem danh sách thùng rác và phục hồi dữ liệu về trạng thái hoạt động bình thường (`IsDeleted = false`).
3. **Thách thức toàn vẹn khi khôi phục**:
   - Khi một thực thể con (như Xe hoặc Phòng ban) được khôi phục, nếu thực thể cha (Khách hàng hoặc Công ty) vẫn đang nằm trong thùng rác, thực thể con sẽ trở thành bản ghi "mồ côi" không có chủ thể quản lý.
   - Trong thời gian bản ghi nằm trong thùng rác, các mã định danh duy nhất (như Biển số xe, Số điện thoại, Mã Code) có thể đã được người khác đăng ký mới. Thao tác khôi phục bắt buộc phải tái kiểm tra tính duy nhất (Re-validation) trước khi mở khóa.

## Quyết định

1. **Chính Sách Chặn Xóa Toàn Diện (Universal Restrict Deletion Policy — 409 Conflict)**:
   - Áp dụng nguyên tắc **Chặn xóa 100% không ngoại lệ** cho toàn bộ các thực thể trong hệ thống:
     * **Xóa `Company`**: Chặn xóa nếu còn bất kỳ `Department` hoặc `Gate` nào chưa bị xóa (`409 Conflict` — `COMPANY_HAS_DEPARTMENTS` / `COMPANY_HAS_GATES`).
     * **Xóa `Department`**: Chặn xóa nếu còn bất kỳ `Client` nào chưa bị xóa (`409 Conflict` — `DEPARTMENT_HAS_CLIENTS`).
     * **Xóa `Client`**: Chặn xóa nếu còn bất kỳ `Vehicle` nào chưa bị xóa (`409 Conflict` — `CLIENT_HAS_VEHICLES`). Bãi bỏ cơ chế cascade xóa xe tự động của ADR 0013. Quản trị viên bắt buộc phải xóa hoặc chuyển nhượng các phương tiện trước khi xóa khách hàng.
     * **Xóa `Gate`**: Chặn xóa nếu còn bất kỳ `Lane` nào chưa bị xóa (`409 Conflict` — `GATE_HAS_LANES`).
     * **Xóa `Device`**: Chặn xóa nếu thiết bị đang được gán vào bất kỳ `Lane` nào chưa bị xóa (`409 Conflict` — `DEVICE_IN_USE_BY_LANE`).

2. **Truy Vấn Thùng Rác Qua Query Parameter (`?onlyDeleted=true`)**:
   - Tích hợp tham số `onlyDeleted (bool, mặc định false)` vào toàn bộ các lớp `PaginationQuery` hiện có (`ClientFilterQuery`, `VehicleFilterQuery`, `CompanyFilterQuery`, `DepartmentFilterQuery`...):
     * Khi `onlyDeleted = false` (mặc định): API trả về các bản ghi đang hoạt động (`IsDeleted == false`).
     * Khi `onlyDeleted = true`: API chỉ trả về các bản ghi nằm trong thùng rác (`IsDeleted == true`).
   - Tái sử dụng 100% logic lọc từ khóa (`keyword`), sắp xếp, và cấu trúc phân trang chuẩn `PagedResult<T>`.

3. **Chuẩn Hóa Endpoint Khôi Phục (`POST /api/v1/{resource}/{id}/restore`)**:
   - Mọi tài nguyên nghiệp vụ đều cung cấp endpoint:
     * `POST /api/v1/companies/{id}/restore`
     * `POST /api/v1/departments/{id}/restore`
     * `POST /api/v1/clients/{id}/restore`
     * `POST /api/v1/vehicles/{id}/restore`
     * `POST /api/v1/gates/{id}/restore`
     * `POST /api/v1/lanes/{id}/restore`
     * `POST /api/v1/devices/{id}/restore`
   - Phân quyền: Yêu cầu vai trò `Manager` hoặc `Admin` (hoặc `X-API-KEY`).
   - Phản hồi: `200 OK` kèm dữ liệu DTO hoàn chỉnh của bản ghi sau khi phục hồi.

4. **Nguyên Tắc Khôi Phục Cha Trước - Con Sau (Strict Parent-First Restore)**:
   - Khi khôi phục một thực thể con:
     * **Khôi phục `Department`**: Kiểm tra Công ty cha `CompanyId`: Công ty cha bắt buộc phải tồn tại và đang hoạt động (`!IsDeleted`). Nếu Công ty cha đang trong thùng rác -> Báo lỗi `400 Bad Request` yêu cầu khôi phục Công ty trước.
     * **Khôi phục `Vehicle`**: Kiểm tra Khách hàng chủ xe `OwnerClientId`: Khách hàng bắt buộc phải tồn tại và đang hoạt động (`!IsDeleted`). Nếu Khách hàng đang trong thùng rác -> Báo lỗi `400 Bad Request` yêu cầu khôi phục Khách hàng trước.
     * **Khôi phục `Lane`**: Kiểm tra Cổng `GateId` và toàn bộ các `DeviceId` liên kết: Cổng và các thiết bị bắt buộc phải tồn tại và `!IsDeleted`.

5. **Xác Thực Lại Tính Duy Nhất Khi Khôi Phục (Re-validation on Restore — 409 Conflict)**:
   - Trước khi khôi phục bản ghi về trạng thái `IsDeleted = false`, tầng Service bắt buộc kiểm tra lại các trường khóa duy nhất trong tập các bản ghi đang hoạt động:
     * `Client`: Kiểm tra `PhoneNumber` và `Code` (nếu có).
     * `Vehicle`: Kiểm tra `PlateNumber` (sau khi chuẩn hóa qua `PlateHelper`).
     * `Company`, `Department`, `Gate`: Kiểm tra `Code`.
     * `Device`: Kiểm tra `Code` và cặp `IpAddress:Port`.
   - Nếu bất kỳ giá trị nào đã bị một bản ghi mới đăng ký chiếm dụng trong thời gian bản ghi cũ nằm trong thùng rác -> Từ chối khôi phục và trả về mã lỗi `409 Conflict` kèm thông báo chi tiết.

6. **Mở Rộng Tầng Repository (`IRepository<T>` & `MongoRepository<T>`)**:
   - Bổ sung các phương thức chuyên trách hỗ trợ thùng rác:
     * `Task<T?> GetDeletedByIdAsync(string id, CancellationToken cancellationToken = default);`
     * `Task<bool> RestoreAsync(string id, CancellationToken cancellationToken = default);`
     * Hỗ trợ tham số `bool includeDeleted = false` hoặc `bool onlyDeleted = false` trong các biểu thức truy vấn.

## Hệ quả

- **Tích cực**:
  - Đạt tính nhất quán tuyệt đối 100% trên toàn bộ hệ thống API: mọi quan hệ đều Chặn xóa khi còn con, không còn bất kỳ ngoại lệ gây hiểu lầm.
  - Cung cấp trọn vẹn chu trình quản lý vòng đời tài nguyên: Tạo mới -> Sử dụng -> Xóa mềm vào thùng rác -> Khôi phục hoặc Xóa vĩnh viễn.
  - Ngăn chặn hoàn toàn tình trạng sinh bản ghi mồ côi khi khôi phục nhờ nguyên tắc Cha trước - Con sau.
  - Bảo vệ tính toàn vẹn dữ liệu tránh xung đột trùng lặp khóa duy nhất khi khôi phục.
- **Đánh đổi**:
  - Quản trị viên cần thực hiện thêm bước xóa xe trước khi xóa khách hàng (thay vì xóa 1 nút cascade như trước). Đây là hành vi đúng đắn và chuẩn mực trong các hệ thống quản trị doanh nghiệp để tránh xóa nhầm dữ liệu.
