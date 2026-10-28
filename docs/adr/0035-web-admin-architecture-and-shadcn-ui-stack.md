# 0035: Kiến Trúc Phân Hệ Quản Trị Web Admin (React, shadcn/ui, TanStack Query & HttpOnly Cookie)

Quyết định thiết kế về việc khởi tạo và phát triển phân hệ ứng dụng web quản trị `HPParking.WebAdmin` độc lập đặt tại thư mục gốc repository, sử dụng nền tảng Vite + React (TypeScript) Single Page Application (SPA), hệ thống thành phần giao diện shadcn/ui (New York style) kết hợp Tailwind CSS, quản lý cache và gọi API qua TanStack Query v5, điều hướng qua React Router v6 với `AuthGuard`, quản lý trạng thái giao diện qua Zustand, và thực thi cơ chế xác thực an toàn bằng HttpOnly Cookie tuân thủ [ADR 0026](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0026-httponly-cookie-authentication-and-cors-credentials.md).

## Ngữ cảnh

1. **Hoàn tất tầng REST API trung tâm**:
   - Phân hệ [HPParking.Api](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking.Api) đã hoàn tất 100% các chức năng CRUD, thống kê gom nhóm đa chiều thời gian thực `$facet` ([ADR 0024](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0024-realtime-dashboard-and-domain-statistics-architecture.md)), động cơ xuất nhập bảng tính ClosedXML Excel ([ADR 0023](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0023-generic-excel-engine-and-import-export-architecture.md)), và tài liệu OpenAPI 3.0 đầy đủ tại `docs/api/openapi/hpparking-api-v1.json`.
2. **Nhu cầu giao diện quản trị từ xa**:
   - Khác với ứng dụng WinForms máy trạm ([HPParking](file:///c:/Users/ADMIN/source/repos/HPParking/HPParking)) chuyên phục vụ tại bốt bảo vệ kiểm soát làn xe trực tiếp, cán bộ quản lý, kế toán và nhân viên nhân sự cần truy cập hệ thống từ xa qua trình duyệt web trên máy tính để quản lý khách hàng, cấp phát quyền FaceID, phân bổ phương tiện, giám sát dung lượng bãi xe và đối soát kiểm toán.
3. **Yêu cầu bảo mật và trải nghiệm người dùng hiện đại**:
   - Cần một ứng dụng web phản hồi nhanh (sub-second UI transitions), giao diện chuẩn mực, hỗ trợ Dark/Light mode, sử dụng 100% ngôn ngữ tiếng Việt đồng bộ với từ điển nghiệp vụ [CONTEXT.md](file:///c:/Users/ADMIN/source/repos/HPParking/CONTEXT.md).
   - Bảo mật chống tấn công XSS tuyệt đối bằng việc sử dụng cơ chế HttpOnly Cookie đã tích hợp sẵn trong API backend theo [ADR 0026](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0026-httponly-cookie-authentication-and-cors-credentials.md).

## Quyết định

1. **Cấu Trúc Không Gian Làm Việc (Workspace Location & Project Setup)**:
   - Đặt tại thư mục `HPParking.WebAdmin` tại gốc repository, bảo đảm tính nhất quán với quy ước đặt tên của solution (`HPParking.Api`, `HPParking.Core`, `HPParking.Tests`).
   - Khởi tạo bằng **Vite + React (TypeScript) SPA**, tận dụng môi trường Node.js v22.x và npm v10.x có sẵn.
   - Cấu hình Vite reverse proxy (`server.proxy`) trong `vite.config.ts` để định tuyến các cuộc gọi `/api` về cổng dịch vụ `HPParking.Api` (mặc định `http://localhost:5000`), bảo đảm tính cùng nguồn (Same-Origin) khi phát triển cục bộ. Cổng phát triển `http://localhost:5173` đã được cấu hình sẵn trong danh sách `CorsSettings:AllowedOrigins` của API.

2. **Hệ Thống Thiết Kế & Giao Diện (Design System & Styling)**:
   - Sử dụng thư viện **shadcn/ui** với kiểu dáng **New York** dựa trên nền tảng Radix UI primitives không phụ thuộc runtime CSS, kết hợp **Tailwind CSS**.
   - Bộ biểu tượng thống nhất: **Lucide React**.
   - Hỗ trợ chuyển đổi chủ đề **Dark/Light Theme** thông qua `ThemeProvider` lưu trạng thái vào `localStorage` và chuyển đổi class `dark` trên thẻ `<html>`.
   - Ngôn ngữ giao diện: **100% Tiếng Việt**, bám sát chính xác các thuật ngữ trong từ điển [CONTEXT.md](file:///c:/Users/ADMIN/source/repos/HPParking/CONTEXT.md) (Khách hàng, Phương tiện, Cổng, Làn, Thiết bị, Phiên gửi xe, Bằng chứng ảnh, Nhật ký kiểm toán).

3. **Cơ Chế Xác Thực & Quản Lý Phiên (HttpOnly Cookie Authentication)**:
   - Sử dụng cơ chế xác thực thuần **HttpOnly Cookie** với cờ `credentials: "include"` trong mọi yêu cầu gọi API.
   - Token truy cập `hpparking_access_token` được trình duyệt lưu trữ tự động trong cookie an toàn và gửi kèm request mà JavaScript không thể truy cập, triệt tiêu hoàn toàn nguy cơ rò rỉ token qua lỗ hổng XSS.
   - Endpoint đăng nhập `POST /api/v1/auth/login` thiết lập cookie; endpoint đăng xuất `POST /api/v1/auth/logout` xóa cookie và ghi nhận AuditLog; endpoint `GET /api/v1/auth/me` nạp thông tin người dùng và vai trò hiện tại.

4. **Hệ Sinh Thái Thư Viện Cốt Lõi (Ecosystem Stack)**:
   - **TanStack Query (React Query) v5**: Xử lý toàn bộ tầng giao tiếp API, tự động quản lý cache dữ liệu, refetch nền khi chuyển tab, deduplication và hỗ trợ phân trang offset 1-indexed mượt mà thông qua `keepPreviousData` / `placeholderData`.
   - **React Router v6**: Quản lý định tuyến SPA, hỗ trợ cấu trúc Nested Layouts (Sidebar, Header, Main Content, Breadcrumbs) và bọc các tuyến đường riêng tư bằng thành phần `AuthGuard`.
   - **Zustand**: Quản lý trạng thái giao diện phía client gọn nhẹ (thông tin người dùng `userProfile`, trạng thái thu gọn Sidebar, trạng thái bộ lọc bảng).
   - **React Hook Form + Zod**: Quản lý biểu mẫu (Form) và kiểm tra tính hợp lệ của dữ liệu đầu vào (Validation) theo chuẩn tích hợp của shadcn/ui.

5. **Chiến Lược Phân Kỳ Triển Khai (4 Milestones Cuốn Chiếu)**:
   - **Milestone 1: Khung Vỏ Hạ Tầng, Xác Thực & Layout (Shell & Auth)**: Khởi tạo Vite, Tailwind CSS, shadcn/ui, ThemeProvider, App Layout (Sidebar/Header), Login Page, AuthGuard và Zustand Auth Store.
   - **Milestone 2: Dashboard Thời Gian Thực & Danh Mục Quản Trị (Dashboard & Master Data)**: Thống kê KPIs đa chiều, module quản lý Công ty, Phòng ban, Nhà thầu, Cổng, Làn xe, và Thiết bị ngoại vi.
   - **Milestone 3: Khách Hàng, Phương Tiện & Động Cơ Excel (Clients, Vehicles & Excel)**: Quản lý Khách hàng (tải avatar, đồng bộ FaceID), Phương tiện (chuẩn hóa biển số xe), tích hợp tải mẫu/nhập/xuất Excel qua ClosedXML engine.
   - **Milestone 4: Lịch Sử Đỗ Xe, Kiểm Toán & Quản Trị Người Dùng (Ledgers, Audit & Users)**: Tra cứu lịch sử phiên gửi xe kèm 4 ảnh bằng chứng, nhật ký kiểm toán hệ thống, và phân quyền người dùng nội bộ.

## Hệ quả

- **Tích cực**:
  - Đạt độ bảo mật tối đa cho ứng dụng Web Admin trước các cuộc tấn công XSS nhờ xác thực thuần HttpOnly Cookie.
  - Tối ưu hóa hiệu năng ứng dụng: bundle nhỏ gọn, tải trang nhanh, không phụ thuộc vào framework nặng nề.
  - Tái sử dụng trọn vẹn và khai thác tối đa năng lực của 100% các endpoint REST API đã xây dựng trong Giai đoạn 1.
  - Trải nghiệm người dùng đồng nhất, hiện đại và chuẩn mực với shadcn/ui và tiếng Việt chuyên ngành bãi đỗ xe thông minh.
- **Đánh đổi**:
  - Khi phát triển cục bộ (Local Development), bắt buộc phải khởi chạy đồng thời dịch vụ `HPParking.Api` (hoặc thông qua mock/proxy) để xử lý cơ chế HttpOnly Cookie.
