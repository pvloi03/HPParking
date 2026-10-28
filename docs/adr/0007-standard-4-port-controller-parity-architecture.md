# 0007: Chuẩn hóa Bộ điều khiển 4 cổng ZKTeco C3-400 và Quy tắc Chẵn/Lẻ phân định làn xe

> **Trạng thái:** Bị thay thế (Superseded) bởi [ADR 0040](file:///c:/Users/ADMIN/source/repos/HPParking/docs/adr/0040-multi-target-lane-architecture-and-tuple-pattern-matching.md).  
> **Lý do thay thế:** Quy tắc khóa cứng 4 làn xe và chia dư chẵn/lẻ (`InputReader % 2`) đã được thay thế hoàn toàn bởi Kiến trúc làn đa đối tượng (Multi-Target Lane Architecture). Hệ thống hiện hỗ trợ cấu hình động từ 1 đến 4 làn bất kỳ tại mỗi Cổng, phân định tường minh qua `TargetType` (`Vehicle` hoặc `Pedestrian`) và `Direction` (`In` hoặc `Out`).

Hệ thống trước đây từng khóa cứng tiêu chuẩn phần cứng trạm kiểm soát sử dụng **1 Bộ điều khiển trung tâm ZKTeco 4 cổng (C3-400)**, phân định loại phương tiện (Xe máy / Ô tô) và chiều di chuyển (Vào / Ra) dựa trên tính chẵn/lẻ của số cổng đọc (`InputReader`) và loại làn (`Type`).

## Ngữ cảnh

Trong quá trình thiết kế và triển khai bãi xe thông minh HPParking, một số trạm kiểm soát nảy sinh kịch bản sử dụng các bộ điều khiển 2 cổng (ZKTeco C3-200) hoặc dùng nhiều bộ điều khiển riêng lẻ (mỗi bộ cho một trạm/đảo kiểm soát).

Tuy nhiên, nếu hỗ trợ cấu hình tùy biến linh hoạt cho mọi loại Controller và số cổng tùy ý:
1. Sẽ phải thay đổi mô hình cơ sở dữ liệu MongoDB (thêm các trường `VehicleType`, `LaneDirection` vào thực thể `Lane`), gây rủi ro phá vỡ tính tương thích ngược với các trạm xe đang vận hành và phát sinh yêu cầu di chuyển dữ liệu (data migration).
2. Tăng độ phức tạp của logic nghiệp vụ và truy vấn trong các tầng Service, Form và Device Orchestrator.

Sau khi cân nhắc đánh đổi giữa **sự linh hoạt của chủng loại phần cứng** và **sự ổn định, nhất quán, không thay đổi cấu trúc database ("không sửa DB")**, dự án quyết định chuẩn hóa mô hình trạm kiểm soát bãi đỗ xe theo một tiêu chuẩn phần cứng duy nhất.

## Quyết định

1. **Khóa cứng tiêu chuẩn phần cứng trạm kiểm soát:**
   - Mỗi trạm kiểm soát bãi xe tiêu chuẩn bắt buộc sử dụng **1 Bộ điều khiển ZKTeco 4 cổng (model C3-400)** kết nối qua giao thức mạng TCP/IP.
   - Không hỗ trợ sử dụng bộ điều khiển 2 cổng (C3-200) làm bộ điều khiển độc lập cho 1 cặp làn Vào/Ra cùng loại xe.
2. **Quy chuẩn sơ đồ cổng đấu nối 1-to-1:**
   - **Cổng 1 (`InputReader = 1`, `OutputRelay = 1`, `Type = 1`):** Làn Xe máy Vào (Inbound Motorcycle Lane).
   - **Cổng 2 (`InputReader = 2`, `OutputRelay = 2`, `Type = 1`):** Làn Ô tô Vào (Inbound Car Lane).
   - **Cổng 3 (`InputReader = 3`, `OutputRelay = 3`, `Type = 2`):** Làn Xe máy Ra (Outbound Motorcycle Lane).
   - **Cổng 4 (`InputReader = 4`, `OutputRelay = 4`, `Type = 2`):** Làn Ô tô Ra (Outbound Car Lane).
3. **Quy tắc phân định nghiệp vụ bằng phép chia dư:**
   - **Loại phương tiện:** Phân biệt theo `InputReader % 2`:
     - Số LẺ (`InputReader % 2 != 0`): Xe máy (cổng 1, cổng 3).
     - Số CHẴN (`InputReader % 2 == 0`): Ô tô (cổng 2, cổng 4).
   - **Chiều di chuyển:** Phân biệt theo `Type % 2`:
     - Số LẺ (`Type % 2 != 0`): Chiều Vào (Inbound).
     - Số CHẴN (`Type % 2 == 0`): Chiều Ra (Outbound).
4. **Kiểm soát tính toàn vẹn tại Giao diện Cấu hình:**
   - Bổ sung validation rule `number_odd` (bắt buộc số lẻ) cho Form Xe máy và `number_even` (bắt buộc số chẵn) cho Form Ô tô trong `ValidationHelper`.
   - Bắt buộc kiểm tra `txtInReader != txtOutReader` để chống xung đột trùng cổng đọc trên cùng một trạm.
   - Bổ sung dòng thông báo và hướng dẫn trực quan ở đầu mỗi Form cấu hình làn xe.

## Hệ quả

- **Bảo toàn dữ liệu tuyệt đối:** Không thay đổi bất kỳ trường nào trong schema MongoDB, không cần migration script, đảm bảo các phiên bản phần mềm nâng cấp chạy mượt mà ngay lập tức.
- **Tiêu chuẩn hóa thi công lắp đặt:** Đội ngũ kỹ sư triển khai và thợ điện nhẹ có bản quy chuẩn đấu dây cố định, rõ ràng (Cổng 1: Xe máy vào, Cổng 2: Ô tô vào, Cổng 3: Xe máy ra, Cổng 4: Ô tô ra), loại bỏ hoàn toàn các lỗi nhầm lẫn cấu hình.
- **Rõ ràng về phạm vi hỗ trợ:** Mọi thành viên phát triển và bảo trì hệ thống đều hiểu rõ lý do của phép chia dư `% 2`, không còn nghi ngại về việc tại sao hệ thống lại phân loại phương tiện qua cổng phần cứng.
