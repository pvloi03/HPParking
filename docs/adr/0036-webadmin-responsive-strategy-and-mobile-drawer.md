# 0036: Chiến Lược Thích Ứng Màn Hình (Responsive Strategy, Mobile Drawer & Hybrid Data View) Trong Phân Hệ Web Admin

Quyết định thiết kế về chiến lược thích ứng đa kích thước màn hình (Responsive Web Design) cho phân hệ quản trị `HPParking.WebAdmin`, bao gồm cơ chế Mobile Drawer Slide-over thay thế Sidebar trên màn hình dưới 1024px, hiển thị dữ liệu dạng lai (Hybrid Data View: Table trên Desktop/Tablet và Stacked Cards trên Mobile), cửa sổ đối soát ảnh phương tiện xếp chồng dọc, và chuẩn hóa kích thước vùng chạm cảm ứng tối thiểu 44px.

## Ngữ cảnh

1. **Đa dạng hóa đối tượng người dùng và thiết bị truy cập**:
   - Khác với ứng dụng WinForms trạm cố định, phân hệ Web Admin phục vụ đồng thời hai nhóm người dùng trọng yếu:
     - *Nhân viên vận hành / Kế toán / Quản trị viên*: Làm việc chuyên sâu tại văn phòng với máy tính để bàn hoặc laptop màn hình lớn (>= 1280px) để cấu hình danh mục, đối soát số liệu và xuất báo cáo Excel.
     - *Cán bộ quản lý / Giám sát bãi xe*: Di chuyển đi tuần trong khuôn viên bãi xe, sử dụng điện thoại thông minh (màn hình 375px - 430px) hoặc máy tính bảng (768px - 1023px) để xem nhanh KPI thời gian thực, kiểm tra tình trạng làn xe, đối soát cảnh báo lệch biển số và tra cứu lịch sử phương tiện.
2. **Thách thức về không gian hiển thị với bảng biểu nhiều cột**:
   - Các bảng dữ liệu quản trị bãi xe (Lịch sử phiên gửi xe, Thẻ xe, Danh sách phương tiện, Sổ cái kiểm toán) thường chứa từ 7 đến 12 cột thông tin kèm ảnh chụp phương tiện. Nếu giữ nguyên dạng bảng truyền thống trên điện thoại di động, người dùng bắt buộc phải cuộn ngang liên tục, gây khó khăn cho việc tra cứu nhanh và thao tác bằng một tay.
3. **Thách thức về không gian thanh bên điều hướng (Sidebar)**:
   - Sidebar dạng Accordion có chiều rộng cố định 240px (hoặc 68px khi thu gọn). Trên màn hình máy tính bảng và điện thoại di động, việc chiếm dụng không gian cố định của Sidebar làm co hẹp nghiêm trọng vùng làm việc chính của nội dung.

## Quyết định

1. **Quy Chuẩn Ngưỡng Kích Thước Màn Hình (Tailwind Breakpoints)**:
   - Thống nhất áp dụng bộ quy chuẩn Breakpoints chuẩn mực của Tailwind CSS xuyên suốt toàn bộ codebase:
     - **Mobile (Màn hình nhỏ)**: `< 640px` (mặc định không có tiền tố).
     - **Phablet / Mobile xoay ngang**: `640px` - `767px` (`sm:`).
     - **Tablet (Máy tính bảng dọc)**: `768px` - `1023px` (`md:`).
     - **Laptop / Desktop (Máy tính xách tay & màn hình chuẩn)**: `1024px` - `1279px` (`lg:`).
     - **Desktop màn hình lớn**: `>= 1280px` (`xl:` và `2xl:`).

2. **Cơ Chế Mobile Drawer Slide-Over & Ngưỡng Chuyển Đổi (Breakpoint lg: 1024px)**:
   - **Ngưỡng chuyển đổi**: Lấy mốc **`lg` (1024px)** làm ranh giới quyết định giữa Sidebar cố định và Drawer trượt:
     - *Màn hình `>= 1024px`*: Hiển thị Sidebar cố định ở cạnh trái giao diện (`hidden lg:flex`), hỗ trợ thu gọn về dải icon 68px khi bấm nút co giãn.
     - *Màn hình `< 1024px` (Tablet & Mobile)*: Ẩn hoàn toàn Sidebar cố định. Trên thanh `Header` xuất hiện nút biểu tượng Hamburger Menu (`lg:hidden`).
   - **Hành vi Drawer**:
     - Khi bấm Hamburger Menu, một Sheet / Drawer trượt từ mép trái màn hình ra (`Slide-over Sheet`), hiển thị toàn bộ cây danh mục điều hướng kèm thông tin người dùng và nút Đăng xuất.
     - Phía sau Drawer phủ lớp nền mờ (`backdrop-blur-sm bg-black/50`).
     - **Tự động đóng Drawer**: Drawer tự động đóng lại khi:
       - Người dùng bấm chọn bất kỳ mục chuyển trang nào (Route change).
       - Bấm vào vùng nền mờ bên ngoài Drawer.
       - Nhấn phím `ESC` trên bàn phím.
       - Màn hình được resize hoặc xoay ngang vượt ngưỡng `>= 1024px`.
   - **Quản lý trạng thái tập trung**:
     - Bổ sung biến trạng thái `isMobileDrawerOpen: boolean` và hàm `setMobileDrawerOpen(open: boolean)` vào Zustand store `useUiStore` tại `src/stores/uiStore.ts`.

3. **Chiến Lược Dữ Liệu Lai (Hybrid Responsive Data View: Table vs. Stacked Cards)**:
   - Để bảo đảm trải nghiệm tra cứu tối ưu trên mọi thiết bị:
     - *Trên Desktop và Tablet (`>= 640px`)*: Hiển thị giao diện Bảng dữ liệu chuẩn (`table`), bọc trong vùng cuộn ngang `overflow-x-auto` với tiêu đề bảng cố định (`sticky top-0`) khi cuộn dọc.
     - *Trên Mobile (`< 640px`)*: Tự động chuyển đổi bảng dữ liệu thành **Dạng Thẻ Thông Tin Xếp Chồng (Stacked Data Cards)**:
       - Mỗi thẻ đại diện cho một bản ghi, hiển thị nổi bật 3 trường thông tin cốt lõi nhất: Biển số xe (font mono in đậm), Thời điểm quét thẻ/vào bãi, và Huy hiệu trạng thái (`Badge` Đang đỗ / Hoàn tất / Cảnh báo lệch biển số).
       - Cho phép người dùng chạm vào toàn bộ thân thẻ để mở Modal / Bottom Sheet hiển thị trọn vẹn chi tiết bản ghi, đầy đủ các cột thuộc tính và ảnh phương tiện.
       - Cung cấp nút thao tác nhanh (Xem ảnh, In hóa đơn, Mở barrier) ở góc phải của thẻ.

4. **Tổ Chức Bộ Lọc Nâng Cao Trên Màn Hình Nhỏ (Filter Sheet)**:
   - Trên màn hình Desktop: Thanh tìm kiếm, các bộ chọn khoảng thời gian, dropdown lọc theo Công ty, Cổng và Làn xe được xếp thành một dải hàng ngang phía trên bảng dữ liệu.
   - Trên màn hình Mobile (`< 640px`):
     - Chỉ giữ duy nhất ô tìm kiếm nhanh (Biển số xe / Tên khách hàng) ở giao diện chính.
     - Gom toàn bộ các tiêu chí lọc phụ (Khoảng ngày, trạng thái, công ty, cổng/làn) vào nút **"Bộ lọc"** có gắn huy hiệu (badge) đếm số lượng điều kiện đang áp dụng.
     - Khi bấm nút "Bộ lọc", một Bottom Sheet trượt từ đáy màn hình lên cho phép người dùng thiết lập các bộ lọc một cách thoải mái bằng ngón tay cái, kèm nút "Đặt lại" và "Áp dụng".

5. **Bố Cục Cửa Sổ Đối Soát Ảnh Phương Tiện (Evidence Photo Inspection)**:
   - Đối với tính năng đối soát ảnh biển số và toàn cảnh lúc xe vào và lúc xe ra:
     - *Desktop & Tablet*: Hiển thị song song hai khung ảnh cạnh nhau (Side-by-side): Khung ảnh lượt vào bên trái, Khung ảnh lượt ra bên phải, cùng các thông số nhận diện OCR để so sánh đối chiếu trực quan.
     - *Mobile (`< 640px`)*: Chuyển thành **Bố cục xếp chồng dọc (Vertical Stack)**: Ảnh lượt vào hiển thị ở trên, Ảnh lượt ra hiển thị ở dưới; hỗ trợ chạm vào ảnh để phóng to toàn màn hình (Fullscreen Lightbox) với khả năng zoom hai ngón tay (Pinch-to-zoom).

6. **Quy Chuẩn Kích Thước Vùng Bấm Cảm Ứng (Touch-Friendly Tap Targets)**:
   - Tuân thủ nghiêm ngặt tiêu chuẩn WCAG 2.1 (Target Size) và Apple Human Interface Guidelines:
     - Tất cả các nút bấm (`Button`), biểu tượng chuyển trang, hàng menu điều hướng và icon đóng/mở trên thiết bị cảm ứng đều có kích thước vùng chạm tối thiểu **`44x44px`** (`min-h-[44px] min-w-[44px]` hoặc có padding đệm) nhằm loại bỏ hoàn toàn hiện tượng bấm nhầm khi nhân viên đang di chuyển trong bãi xe.

## Hệ quả

- **Tích cực**:
  - Trải nghiệm người dùng vượt trội và đồng nhất trên toàn bộ dải thiết bị: từ màn hình máy trạm 4K, laptop văn phòng, iPad đi tuần đến điện thoại di động cầm tay.
  - Loại bỏ hoàn toàn tình trạng vỡ giao diện, chữ bị co cụm hoặc tràn màn hình trên các thiết bị di động.
  - Quản lý bãi xe có thể xử lý công việc và tra cứu số liệu ngay tại hiện trường mà không cần phải mang theo máy tính xách tay.
- **Đánh đổi**:
  - Các component hiển thị bảng dữ liệu (như danh sách phiên đỗ xe, thẻ xe, phương tiện) cần duy trì hai chế độ render (`table` cho `>= sm` và `cards` cho `< sm`), đòi hỏi cấu trúc component phải tách bạch logic dữ liệu khỏi phần view trình bày.
