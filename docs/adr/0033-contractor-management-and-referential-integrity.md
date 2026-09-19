# 0033: Quản Lý Nhà Thầu (Contractor) Và Chính Sách Toàn Vẹn Tham Chiếu Với Khách Hàng (Client)

Quyết định thiết kế về việc chuẩn hóa mô hình nghiệp vụ Quản lý Nhà thầu / Đối tác (`Contractor`), phân định độc lập với Công ty (`Company`), cung cấp cụm REST API quản lý vòng đời dữ liệu, áp dụng chính sách Chặn xóa toàn diện (Universal Restrict Deletion Policy — 409 Conflict) và Bảo vệ trạng thái hoạt động (Active State Protection) trong mối quan hệ với Khách hàng (`Client`), tuân thủ các nguyên tắc toàn vẹn tham chiếu theo [ADR 0030](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0030-entity-relationship-and-referential-integrity-policy.md) và [ADR 0031](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0031-recycle-bin-restore-and-universal-restrict-deletion.md).

## Ngữ cảnh

Trong các khu công nghiệp, tòa nhà văn phòng và nhà máy quy mô lớn:
1. **Sự khác biệt giữa Đơn vị Thường trú (`Company`) và Nhà thầu Bên ngoài (`Contractor`)**:
   - `Company`: Đại diện cho các đơn vị thường trú cố định (doanh nghiệp thuê văn phòng, ban quản lý nhà máy), gắn liền với bản quyền phần mềm (`LicenseKey`), cây phân cấp phòng ban (`Department`) và danh mục cổng (`Gate`).
   - `Contractor`: Đại diện cho các nhà thầu thi công, đối tác bảo trì, bảo dưỡng, vận chuyển dịch vụ ra vào theo dự án hoặc thời vụ.
   - Việc gộp chung Nhà thầu vào thực thể `Company` gây ô nhiễm mô hình dữ liệu (bản quyền, phòng ban không áp dụng cho nhà thầu ngắn hạn). Do đó, `HPParking.Core` đã có sẵn thực thể độc lập `Contractor`.
2. **Thiếu hụt API và Rủi ro Toàn vẹn Dữ liệu**:
   - Trước đây, `Contractor` chỉ tồn tại dưới dạng thực thể trong `HPParking.Core` mà chưa có tầng REST API quản lý.
   - Khi tạo mới `Client` thuộc phân loại `ClientType.Contractor`, trường `ContractorId` được lưu trữ nhưng tầng API chưa kiểm tra tính tồn tại của nhà thầu, dẫn đến rủi ro lưu mã định danh rác hoặc nhà thầu đã bị xóa mềm.
   - Khi xóa một nhà thầu, hệ thống cần chính sách ngăn chặn việc tạo ra các nhân sự nhà thầu "mồ côi" (orphaned clients).

## Quyết định

1. **Phân Định Thực Thể Độc Lập `Contractor`**:
   - Duy trì `Contractor` là một thực thể gốc độc lập kế thừa `BaseEntity`.
   - Các trường thông tin cốt lõi:
     * `Code`: Mã định danh nhà thầu (ví dụ: `NT_HPD`, `NT_DELTA`), viết hoa, duy nhất trên toàn hệ thống giữa các nhà thầu đang hoạt động (`!IsDeleted`).
     * `Name`: Tên pháp nhân / đơn vị thi công.
     * `ContactPerson`: Người đại diện phụ trách liên hệ / chỉ huy trưởng.
     * `PhoneNumber` & `Email`: Thông tin liên hệ của đơn vị.
     * `IsActive`: Trạng thái kích hoạt phục vụ ra vào.

2. **Chính Sách Chặn Xóa Nghiêm Ngặt (Universal Restrict Deletion Policy — 409 Conflict)**:
   - Khi gọi `DELETE /api/v1/contractors/{id}`:
     * Tầng Service bắt buộc kiểm tra collection `Clients`: nếu còn bất kỳ nhân sự nào đang liên kết (`c.ContractorId == id && !c.IsDeleted`) -> **Chặn xóa và ném `ConflictException` (`409 Conflict` - `CONTRACTOR_HAS_CLIENTS`)**.
     * Quản trị viên bắt buộc phải xóa hoặc chuyển đổi đơn vị cho toàn bộ nhân sự liên quan trước khi xóa nhà thầu.

3. **Bảo Vệ Trạng Thái Hoạt Động (Active State Protection — 400 Bad Request)**:
   - Khi cập nhật nhà thầu từ hoạt động sang ngưng hoạt động (`IsActive = false`):
     * Nếu còn bất kỳ nhân sự nào đang hoạt động (`c.ContractorId == id && c.IsActive == true && !c.IsDeleted`) -> **Từ chối cập nhật với mã lỗi `400 Bad Request` (`CONTRACTOR_ACTIVE_CLIENTS_EXIST`)**.
     * Ngăn chặn việc nhân sự nhà thầu vẫn được cấp quyền mở barie khi đơn vị chủ quản đã bị đình chỉ hoạt động.

4. **Quản Lý Thùng Rác & Khôi Phục Dữ Liệu (Recycle Bin & Restore Engine)**:
   - Tích hợp tham số `?onlyDeleted=true` trong query phân trang danh sách nhà thầu.
   - Chuẩn hóa endpoint khôi phục: `POST /api/v1/contractors/{id}/restore`.
   - Khi khôi phục: Tái xác thực tính duy nhất của mã `Code` trong số các nhà thầu đang hoạt động (`!IsDeleted`). Nếu trùng lặp, trả về `409 Conflict` (`CONTRACTOR_CODE_DUPLICATE`).

5. **Quy Tắc Cha Trước - Con Sau khi Khôi Phục Khách Hàng (Strict Parent-First Restore)**:
   - Cập nhật `RestoreClientAsync`: Nếu khách hàng có trường `ContractorId`, hệ thống bắt buộc kiểm tra nhà thầu cha (`_contractorRepo.GetByIdAsync`). Nếu nhà thầu không tồn tại hoặc đang nằm trong thùng rác (`IsDeleted == true`) -> **Từ chối khôi phục với `400 Bad Request` (`PARENT_IS_DELETED`)**.

6. **Xác Thực Khóa Ngoại khi Tạo / Cập Nhật Khách Hàng**:
   - Khi gọi `POST /api/v1/clients` hoặc `PUT /api/v1/clients/{id}` với `request.ContractorId`:
     * Kiểm tra nhà thầu tương ứng có tồn tại trong MongoDB, chưa bị xóa mềm (`!IsDeleted`) và đang kích hoạt (`IsActive == true`). Nếu không hợp lệ -> Báo lỗi `400 Bad Request` (`CONTRACTOR_NOT_FOUND` hoặc `CONTRACTOR_INACTIVE`).

## Hệ quả

- **Tích cực**:
  - Đảm bảo tính nhất quán 100% với các chính sách toàn vẹn dữ liệu đã ban hành trong ADR 0030 và ADR 0031.
  - Loại bỏ hoàn toàn khả năng sinh ra bản ghi nhân sự nhà thầu mồ côi.
  - Cung cấp trọn vẹn chu trình quản lý vòng đời cho đối tượng Nhà thầu: Tạo mới -> Vận hành -> Đóng thùng rác -> Khôi phục.
- **Đánh đổi**:
  - Tầng `ClientService` cần inject thêm `IRepository<Contractor>` để kiểm tra tham chiếu toàn vẹn khi thêm/sửa/khôi phục nhân sự. Chi phí này là tối thiểu so với lợi ích bảo vệ tính toàn vẹn hệ thống.
