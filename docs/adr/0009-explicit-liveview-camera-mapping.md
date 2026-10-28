# 0009: Kiến trúc Ánh xạ Tường minh Luồng LiveView Camera (Explicit Semantic Mapping)

Hệ thống loại bỏ cơ chế duyệt chỉ số phẳng (`index * 2`) khi khởi tạo luồng camera xem trực tiếp (LiveView), chuyển sang cơ chế **Ánh xạ tường minh (Explicit Semantic Mapping)** dựa trên loại làn và loại camera để khắc phục triệt để lỗi tráo đổi khung hình trên giao diện chính.

## Ngữ cảnh

Trên giao diện điều khiển trung tâm [FrmMain.cs](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking/Forms/FrmMain.cs), màn hình được chia thành hai khu vực giám sát lớn:
- Cột bên trái: Làn Xe máy (gồm Làn vào và Làn ra, mỗi làn có 1 Camera Biển số và 1 Camera Toàn cảnh).
- Cột bên phải: Làn Ô tô (gồm Làn vào và Làn ra, mỗi làn có 1 Camera Biển số và 1 Camera Toàn cảnh).

Trong thiết kế ban đầu, `FrmMain` thu thập tất cả PictureBox có `Tag == "pbPreview"` vào một danh sách phẳng `List<PictureBox> previews`, sau đó `DeviceOrchestrator` duyệt tuần tự qua `lanes.Select((lane, index) => InitializeLaneAsync(lane, previews, previewSlotStart: index * 2))`.

Thiết kế này dẫn đến 2 lỗi hiển thị nghiêm trọng (BUG tráo đổi Camera LiveView):
1. **Tráo đổi Làn (Cross-Lane Swap):** Thứ tự các bản ghi `Lane` trả về từ MongoDB qua `GetAllAsync()` không có cam kết thứ tự cố định. Nếu một làn Ô tô được tạo hoặc sửa đổi khiến nó đứng đầu danh sách trả về, camera Ô tô sẽ bị gán vào các PictureBox của Xe máy và ngược lại.
2. **Tráo đổi Loại Camera (Camera-Type Swap):** Trong mỗi cột TableLayoutPanel, PictureBox Toàn cảnh nằm ở hàng trên (Row 0) và PictureBox Biển số nằm ở hàng giữa (Row 1). Danh sách phẳng thu thập theo thứ tự `[Overview, Plate, ...]`, nhưng code lại khởi tạo `plateCam` vào index chẵn (`previewSlotStart`) và `overviewCam` vào index lẻ (`previewSlotStart + 1`), làm hình ảnh camera biển số bị chiếu lộn vào khung hình toàn cảnh.

## Quyết định

1. **Bãi bỏ duyệt danh sách phẳng:** Loại bỏ hoàn toàn phương thức gom `List<PictureBox>` và cơ chế tính toán chỉ số `index * 2` trong `DeviceOrchestrator`.
2. **Thiết lập cơ chế Ánh xạ Tường minh (Explicit Semantic Mapping):**
   - Xây dựng cấu trúc dữ liệu `LanePreviewHandles` biểu diễn cặp handle cửa sổ hiển thị cho một làn:
     ```csharp
     public class LanePreviewHandles
     {
         public IntPtr PlateHandle { get; set; }
         public IntPtr OverviewHandle { get; set; }
     }
     ```
   - Tại `FrmMain`, dựa vào quy tắc định danh làn chuẩn (được mở rộng động theo [ADR 0040](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0040-multi-target-lane-architecture-and-tuple-pattern-matching.md)) để ánh xạ chính xác đến từng PictureBox vật lý:
     - **Xe máy Vào** (`InputReader % 2 != 0 && Type % 2 != 0`): `PlateHandle = pbMotoEntryPlate.Handle`, `OverviewHandle = pbMotoEntryOverview.Handle`.
     - **Xe máy Ra** (`InputReader % 2 != 0 && Type % 2 == 0`): `PlateHandle = pbMotoExitPlate.Handle`, `OverviewHandle = pbMotoExitOverview.Handle`.
     - **Ô tô Vào** (`InputReader % 2 == 0 && Type % 2 != 0`): `PlateHandle = pbCarEntryPlate.Handle`, `OverviewHandle = pbCarEntryOverview.Handle`.
     - **Ô tô Ra** (`InputReader % 2 == 0 && Type % 2 == 0`): `PlateHandle = pbCarExitPlate.Handle`, `OverviewHandle = pbCarExitOverview.Handle`.
3. **Khởi tạo độc lập với thứ tự dữ liệu:**
   - `DeviceOrchestrator` nhận cấu trúc ánh xạ theo từng làn (hoặc Dictionary theo Lane ID/Key) và khởi tạo trực tiếp:
     - Camera Biển số $\rightarrow$ phát vào `PlateHandle`.
     - Camera Toàn cảnh $\rightarrow$ phát vào `OverviewHandle`.
   - Giữ nguyên cơ chế khởi tạo bất đồng bộ Non-blocking an toàn, kèm vị trí comment chờ cho chức năng ghi log giám sát camera sau này.

## Hệ quả

- **Chính xác tuyệt đối:** Hình ảnh camera luôn xuất hiện đúng 100% vào vị trí tương ứng trên giao diện (Cam Biển số vào khung Biển số, Cam Toàn cảnh vào khung Toàn cảnh, Xe máy vào khu vực Xe máy, Ô tô vào khu vực Ô tô) bất kể thứ tự lưu trữ trong cơ sở dữ liệu.
- **Tách biệt kiến trúc (Clean Separation):** `DeviceOrchestrator` không còn phụ thuộc ngầm vào thứ tự hay danh sách Control của WinForms; nó chỉ nhận các Handle đồ họa cần phát, tăng tính đóng gói (Deep Module).
