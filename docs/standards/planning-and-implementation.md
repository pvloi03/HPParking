# Planning & Implementation Standards

Quy chuẩn lập kế hoạch (`implementation_plan.md`) và thực thi mã nguồn bám sát bài toán và phong cách kỹ thuật.

---

## 1. Nguyên Tắc Cốt Lõi (Core Principles)

### 1.1. Bám Sát Bản Chất Bài Toán (Problem Fidelity)
- Kế hoạch phải phản ánh trực tiếp **mục tiêu nghiệp vụ cần đạt được (Definition of Done)** của task/issue, không phải là bản liệt kê danh sách file kỹ thuật đơn thuần.
- Tập trung giải quyết trực diện khúc mắc chính, không vẽ thêm việc ngoài phạm vi

### 1.2. Nắm Bắt Tinh Thần Của Skill (Skill Fidelity)
- Khi một skill hoặc phương pháp luận được kích hoạt, kế hoạch và quy trình thực thi **phải phản ánh đúng tinh thần của skill đó**:

### 1.3. Thực Chất & Linh Hoạt (Pragmatism over Dogmatism)
- Tránh bẫy giáo điều: Quy trình sinh ra để tăng độ tin cậy và kiểm soát chất lượng, không phải là thủ tục hành chính cản trở dòng chảy công việc (flow).
- Bản plan cần vừa đủ chi tiết để kiểm soát rủi ro, vừa đủ thoáng để linh hoạt ứng biến theo tình huống thực tế.

---

## 2. Quy Trình 3 Bước Thực Thi

```
   [1. HIỂU ĐÚNG ĐỀ]                [2. LẬP PLAN THỰC CHẤT]                    [3. THỰC THI NHẤT QUÁN]
   Đọc rõ yêu cầu bài toán          Tập trung vào mốc nghiệp vụ                Làm đúng tinh thần đã chọn
   + Nắm tinh thần Skill            + Phản ánh đúng phong cách skill           + Tinh gọn, đo lường được
```

1. **Hiểu đúng đề & Chọn đúng cách tiếp cận**:
   - Xác định rõ đích đến của task và phong cách làm việc mà người dùng hướng tới thông qua skill.
2. **Lập plan thực chất**:
   - Vạch ra lộ trình mạch lạc, tập trung vào các mốc nghiệp vụ mấu chốt cần xử lý và kiểm chứng.
3. **Thực thi nhất quán**:
   - Bám sát lộ trình đã cam kết, kiểm chứng chất lượng ở từng chốt chặn, nghiệm thu bằng kết quả thực tế (code chạy ổn định, test xanh, zero warning).
