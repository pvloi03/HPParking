# Planning & Implementation Standards

Quy chuẩn lập kế hoạch triển khai (`implementation_plan.md`) và thực thi mã nguồn bám sát bài toán và Skill active cho toàn bộ hệ thống `HPParking`.

---

## 1. Nguyên Tắc Bám Sát Bài Toán & Skill (Fidelity)

1. **Bám sát bài toán (Problem Fidelity)**:
   - Kế hoạch phải ánh xạ trực tiếp từ Issue/Ticket/ADR: Mỗi tiêu chí chấp nhận (Acceptance Criteria) hoặc quy tắc nghiệp vụ phải có một Seam kiểm thử tương ứng.
   - Không tự ý thêm bớt tính năng ngoài phạm vi (tránh Speculative Generality và Scope Creep).

2. **Bám sát Skill (Skill Fidelity)**:
   - Khi thực thi lệnh `/matt-pocock:implement`, kế hoạch và hành động **bắt buộc phải tuân thủ `/matt-pocock:tdd`**.
   - Cấm lập kế hoạch dồn toàn bộ việc viết test xuống phase cuối cùng (**chống anti-pattern Horizontal Slicing**).

---

## 2. Cấu Trúc Kế Hoạch Theo Lát Cắt Dọc (Vertical Slice Seams)

Mọi `implementation_plan.md` khi lập cho task có logic nghiệp vụ phải chia thành các **Seams** (Lát cắt dọc) độc lập:

Mỗi Seam gồm 4 bước bắt buộc:
1. **Public Seam**: Tên interface, API endpoint hoặc method cần can thiệp.
2. **Failing Test (Red)**: Tên file test và kịch bản test cụ thể sẽ viết trước; chạy kiểm thử chứng minh test **FAIL**.
3. **Minimal Code (Green)**: Viết vừa đủ code logic (DTO/Entity/Service) để test **PASS**.
4. **Verify**: Chạy lại unit test đơn lẻ và kiểm tra 0 compilation warnings.

---

## 3. Quy Trình Thực Thi Vòng Lặp TDD (Red → Green Loop)

1. **Tuyệt đối không viết code logic khi chưa có test fail tương ứng tại Seam.**
2. **Chạy test đơn lẻ ngay sau khi viết test**: `dotnet test --filter FullyQualifiedName~<TestName>`.
3. **Chỉ viết code tối thiểu cần thiết để chuyển test từ đỏ sang xanh.**
4. **Không refactor trong vòng lặp**: Việc refactor thuộc về giai đoạn review (`/matt-pocock:code-review`).
5. **Chạy toàn bộ test suite một lần ở cuối task trước khi tạo PR.**
