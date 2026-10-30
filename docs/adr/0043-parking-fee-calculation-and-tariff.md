# 0043: Module Tính Phí Đỗ Xe Theo Thẻ, Biểu Phí & Đóng Gói Module Điều Vận (Parking Fee Calculation, Tariff & Fleet Dispatch Packaging)

> **Trạng thái:** Chấp thuận (Accepted)  
> **Liên quan (Related):** [ADR 0023](0023-generic-excel-engine-and-import-export-architecture.md), [ADR 0024](0024-realtime-dashboard-and-domain-statistics-architecture.md), [ADR 0030](0030-entity-relationship-and-referential-integrity-policy.md), [ADR 0031](0031-recycle-bin-restore-and-universal-restrict-deletion.md), [ADR 0034](0034-gate-and-lane-management-with-hardware-binding.md), [ADR 0040](0040-multi-target-lane-architecture-and-tuple-pattern-matching.md), [ADR 0041](0041-client-vehicle-workflow-handler-and-rich-domain-model.md), [Referential Integrity Matrix](../standards/referential-integrity-matrix.md)

Tài liệu quyết định kiến trúc (ADR) chuẩn mực quy định về việc thiết kế và hiện thực Module Tính phí đỗ xe theo thẻ định danh (`Tariff`, `ParkingFeeCalculator`), luồng kiểm soát thu phí fail-closed tại trạm trực bốt bảo vệ (`VisitorVehicleWorkflowHandler`, `ClientVehicleWorkflowHandler`), cơ chế điều khiển mở barie không popup modal (`AutoOpenBarrier`), chuẩn hóa Nhập/Xuất Excel đồng bộ (`CardExcelService`, `ReportExcelService`), các nguyên tắc toàn vẹn tham chiếu và cơ chế đóng gói bảo lưu module Điều vận phương tiện nội bộ (`FleetDispatch`) qua Feature Flag.

---

## Ngữ cảnh

1. **Nhu cầu Tính Phí Đỗ Xe Linh Hoạt & Đa Đối Tượng**:
   - Hệ thống trước đây chủ yếu quản lý xe nội bộ và nhân sự (`Person`, `Vehicle`), chưa có cơ chế biểu phí và tính tiền cho khách vãng lai (`Visitor`), thuê bao tháng (`Monthly`) và khách ưu tiên (`VIP`).
   - Cần bổ sung phân hệ Biểu phí (`Tariff`) hỗ trợ 2 chế độ: Cố định theo lượt (`Flat`) và Lũy tiến theo block thời gian (`Block`), kết hợp phụ thu qua đêm (`OvernightSurcharge`) và mức trần giới hạn mỗi ngày (`MaxDailyCap`).
2. **Đóng Gói Bảo Lưu Module Điều Vận Phương Tiện Nội Bộ (`FleetDispatch`)**:
   - Trong giai đoạn triển khai tại các bãi xe thông thường, tính năng Giám sát điều vận liên nhà máy và Tuyến cổng (`GateRoute`) không cần kích hoạt nhưng mã nguồn, mô hình dữ liệu và các ca kiểm thử hồi quy SLA bắt buộc phải được bảo lưu toàn vẹn (không xóa file, không phá vỡ hợp đồng kiểm thử).
3. **Rủi Ro Thất Thoát Tài Chính & Thu Phí Trùng Khi Barrier Lỗi**:
   - Tại bốt trạm, khi nhân viên bảo vệ đã thu tiền mặt của khách nhưng barie gặp sự cố cơ điện không mở được, nếu hệ thống không lưu giao dịch hoặc lưu thất bại, khi thử lại bảo vệ có thể thu phí lần 2 gây khiếu nại gay gắt.
   - Ngược lại, nếu cho barie mở trước khi xác nhận tiền hoặc khi hệ thống bị lỗi tính phí, bãi xe sẽ bị thất thoát tài chính nghiêm trọng.
4. **Xung Đột Mốc Thời Gian Khi Tính Phí Qua Đêm & Nhiều Ngày**:
   - Việc phụ thu qua đêm và áp dụng mức trần 24h thường bị nhầm lẫn giữa mốc nửa đêm theo lịch địa phương (`00:00` ngày mới) và khoảng thời gian 24 giờ liên tục kể từ thời điểm xe vào bãi (`InTime`).
5. **Yêu Cầu Tính Thông Suốt Tại Làn Bốt Trạm (Loại Bỏ Popup Modal Gây Tắc Làn)**:
   - Việc hiển thị hộp thoại xác nhận popup (`TaskDialog`) làm chặn (blocking) luồng thao tác thời gian thực tại làn xe, tiềm ẩn rủi ro treo giao diện khi bảo vệ không kịp thao tác hoặc khi luồng xe vào ra dồn dập vào giờ cao điểm.
6. **Tính Đồng Bộ Dữ Liệu Biểu Phí & Báo Cáo Doanh Thu**:
   - Khái niệm biểu phí và thu phí không chỉ giới hạn ở tầng API CRUD đơn lẻ mà bắt buộc phải đồng bộ xuyên suốt qua các công cụ Nhập/Xuất Excel hàng loạt (`CardExcelService`) và tổng hợp doanh thu tài chính chính xác trên sổ cái (`ReportExcelService`).

---

## Quyết định

### 1. Snapshot `TariffId` Khi Xe Vào Bãi & Lưu Trữ BSON Decimal128
- **Snapshot Biểu Phí**: Khi phương tiện/thẻ quẹt vào bãi (`InTime`), hệ thống xác định `TariffId` tương ứng và chốt cứng (`Snapshot`) vào bản ghi `ParkingSession.TariffId`.
- Mọi thay đổi về đơn giá biểu phí, tạm dừng kích hoạt hoặc xóa biểu phí trong tương lai **tuyệt đối không ảnh hưởng** đến các phiên đang đỗ dở dang đã chốt biểu phí tại thời điểm vào.
- Nếu biểu phí bị xóa hoặc ngừng kích hoạt tại thời điểm ra, hệ thống tự động gắn cờ `HasTariffWarning = true` và chuyển sang cơ chế kiểm soát fail-closed.
- **Chuẩn Hóa Lưu Trữ Tiền Tệ BSON Decimal128**:
  - Toàn bộ các trường tiền tệ `decimal` (`FlatPrice`, `BasePrice`, `AdditionalBlockPrice`, `OvernightSurcharge`, `MaxDailyCap`, `FeeAmount`, `OverrideFeeAmount`) được cấu hình serialize bằng `Decimal128Serializer` trong MongoDB để loại trừ hoàn toàn sai số dấu phẩy động (Floating point inaccuracy) khi tính lũy tiến hoặc tổng hợp doanh thu.
- **Chỉ Mục Duy Nhất Có Điều Kiện (Unique Partial Indexes)**:
  - `(PlateNumber, Status = Active)`: Chống 1 biển số xe mở đồng thời 2 phiên đang đỗ trong bãi.
  - `(CardId, Status = Active)`: Chống 1 thẻ định danh quẹt vào 2 lần liên tiếp khi chưa hoàn tất phiên ra.

### 2. Thuật Toán Tính Phí Đỗ Xe & Hai Quy Chiếu Ngày Độc Lập
Thuật toán tính phí triển khai tại `ParkingFeeCalculator` (`IParkingFeeCalculator`) tuân thủ nghiêm ngặt các mốc thời gian:
1. **Thời gian ân hạn (`GracePeriodMinutes`)**: Nếu `durationMinutes <= GracePeriodMinutes`, tổng tiền luôn là `0 VNĐ`.
2. **Chế độ Cố định (`TariffPricingMode.Flat`)**:
   - Tiền gốc = `FlatPrice`.
3. **Chế độ Block Lũy Tiến (`TariffPricingMode.Block`) & Cơ Chế Cycle Reset 24 Giờ**:
   - Quy chiếu trần 24h tính theo chu kỳ lăn (Rolling 24h = 1440 phút) kể từ `InTime`.
   - Mỗi chu kỳ 1440 phút được tính toán độc lập:
     - Block đầu (`BaseBlockMinutes`): tính đơn giá `BasePrice`.
     - Các block phụ tiếp theo (`AdditionalBlockMinutes`): làm tròn lên (`Math.Ceiling`) nhân với `AdditionalBlockPrice`.
     - Phí trong chu kỳ bị khống chế tối đa bởi `MaxDailyCap` (nếu `MaxDailyCap > 0`).
   - Hết mỗi 1440 phút, bộ đếm block và phí tích lũy tự động reset về 0 để bắt đầu chu kỳ 24h tiếp theo.
4. **Phụ Thu Qua Đêm (`OvernightSurcharge`) & Mốc Lịch Địa Phương `00:00`**:
   - Quy chiếu theo mốc nửa đêm `00:00` giờ chuẩn Việt Nam (UTC+7) qua tiện ích chuẩn hóa `VietnamTime.ToVietnamTime`.
   - Số đêm qua = số lần vượt qua ranh giới `00:00` giữa ngày vào và ngày ra.
   - Tiền phụ thu = `OvernightDays * OvernightSurcharge`, được cộng độc lập sau khi đã áp dụng `MaxDailyCap`.

### 3. Quy Trình Trạm Trực: Chốt Phí - Mở Barie - Chống Thu Trùng (Save-First + AutoOpenBarrier)
Tại `VisitorVehicleWorkflowHandler` và `ClientVehicleWorkflowHandler`:
1. **Loại Bỏ Hoàn Toàn Hộp Thoại Popup (`TaskDialog`)**:
   - Nhằm tối ưu tốc độ thông xe và bảo đảm luồng xử lý không bị chặn (non-blocking) tại bốt trạm, loại bỏ hoàn toàn việc hiển thị hộp thoại xác nhận popup (`TaskDialog`).
   - Thay vào đó, việc điều khiển mở thanh chắn barie được phân quyền hóa trực tiếp qua cấu hình `AutoOpenBarrier`.
2. **Cấu Hình Mở Thanh Chắn Tự Động (`AutoOpenBarrier`)**:
   - **Đối với Thẻ Khách hàng / Nhân sự (`Client`)**: Cấu hình `client.AutoOpenBarrier` độc lập cho từng chiều (mặc định mở cả 2 làn: `OnEntry = true`, `OnExit = true`; hoặc bắt buộc `true`/`true` đối với chế độ làn tự do `isNoneAuth`). Nếu thẻ nhân sự có tính phí (`IsChargeable = true`), hệ thống ghi nhận thu phí theo Save-First và chỉ mở barie khi `client.ShouldAutoOpenBarrier(LaneDirection.Out) == true`.
   - **Đối với Thẻ Khách vãng lai (`Visitor`)**: Mặc định `AutoOpenBarrier` là `false` cả 2 làn (`OnEntry = false`, `OnExit = false`). Khi quẹt thẻ ra có phát sinh phí (`FeeAmount > 0`), hệ thống chốt giao dịch thu phí xuống DB (`Save-First Pattern`), ghi nhận `FeeAmount` và `IsFeeCollected = true`:
     - Nếu trạm cấu hình mặc định (`OnExit = false`): Barie giữ đóng kín, không popup hộp thoại. Bảo vệ thu tiền thực tế tại bốt (tiền mặt / chuyển khoản) và chủ động bấm nút mở barie thủ công trên bàn phím hoặc bàn điều khiển trạm.
     - Nếu trạm cấu hình `OnExit = true`: Barie tự động mở ngay sau khi giao dịch thu phí đã lưu thành công xuống cơ sở dữ liệu.
3. **Thứ Tự Ghi Sổ Trước - Mở Barie Sau (Save-First Pattern) & Cơ Chế Rollback Hai Chiều Độc Lập**:
   - **Rollback Chiều Vào (Entry Rollback — Hard Delete `softDelete: false`)**:
     - Khi lưu phiên vào bãi thành công nhưng phát lệnh mở barrier thất bại (`TryOpenBarrier == false`), phương tiện thực tế **chưa vào bãi** và giao dịch chưa được xác lập.
     - Hệ thống bắt buộc rollback xóa sạch hoàn toàn bản ghi đỗ xe tạm khỏi CSDL bằng `_sessionRepository.DeleteAsync(id, softDelete: false)`.
     - Tuyệt đối không dùng `softDelete: true` để tránh gây ô nhiễm sổ cái giao dịch bất biến (`ParkingSession`), lãng phí số thứ tự lượt vào và ngăn chặn nguy cơ xung đột các Unique Partial Indexes (`(PlateNumber, Status=Active)`, `(CardId, Status=Active)`).
   - **Rollback Chiều Ra (Exit Rollback — Save-First Bảo Lưu Thu Tiền)**:
     - Hệ thống cập nhật `ParkingSession` với `FeeAmount` và `IsFeeCollected = true` xuống cơ sở dữ liệu trước khi phát lệnh mở barie.
     - Nếu barie gặp sự cố cơ điện không mở được: Hệ thống rollback trạng thái phiên về `Active` nhưng **bảo lưu nguyên vẹn `FeeAmount` và `IsFeeCollected = true`**. Lần quét thẻ kế tiếp của khách sẽ nhận diện là "Đã thu tiền" và chỉ gửi lại lệnh mở barie, triệt tiêu hoàn toàn rủi ro thu tiền lần 2.
     - Khi quá trình rollback gặp xung đột khóa (`DuplicateKeyException`), hệ thống ghi nhận thông tin ở mức `LogCritical` theo chuẩn an ninh kiểm toán tài chính (thay vì Warning).
4. **Khóa Nghiêm Ngặt Làn Ra (Strict Exit Lockout)**:
   - Khi camera không nhận diện được biển số tại làn ra của khách vãng lai, hệ thống yêu cầu bảo vệ nhập biển số thủ công và **tuyệt đối không điền sẵn biển số vào** để ngăn ngừa gian lận đổi thẻ/đổi xe.
5. **Quy Định Thao Tác Phím Nóng Mở Barie (`F1`–`F4`) Tại Bốt Trạm**:
   - Thao tác nhấn phím nóng `F1`–`F4` (hoặc nút bấm thủ công trên trạm trực) là thao tác vận hành phần cứng trực tiếp của nhân viên trực ca (đặc biệt khi làn xe cấu hình `AutoOpenBarrier.OnExit = false` để thu tiền thực tế tại bốt rồi chủ động bấm mở barie cho xe qua, hoặc điều tiết phân luồng giờ cao điểm).
   - **Thao tác phím nóng mở barie này TUYỆT ĐỐI KHÔNG GHI AUDITLOG**, nhằm:
     1. Loại bỏ hoàn toàn nguy cơ ngập lụt rác log kiểm toán (log flooding/noise) trong collection `AuditLogs` trên MongoDB, bảo đảm kích thước và hiệu năng truy vấn của sổ cái kiểm toán bất biến.
     2. Đảm bảo độ trễ bằng không (zero-latency) cho thao tác kích mở barie của bảo vệ, không bị phụ thuộc hay chậm trễ do I/O cơ sở dữ liệu.
     3. Phân định rạch ròi: `AuditLog` chỉ ghi nhận các can thiệp quản trị dữ liệu nhạy cảm (như `ForceClose` phiên đỗ, thay đổi cấu hình thẻ/biểu phí, phân quyền người dùng), không theo dõi các sự kiện tương tác phím ngoại vi vật lý hàng ngày của nhân viên trực bốt.

### 4. Đóng Gói Module Điều Vận Nội Bộ Bằng Feature Flag
- Cấu hình tập trung tại `FeatureOptions.EnableFleetDispatch = false`.
- **Backend API**:
  - Nạp qua `builder.Configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>()` từ `appsettings.json`.
  - `FleetDispatchControllerConvention` tự động gắn `[ApiExplorerSettings(IgnoreApi = true)]` và bộ lọc chặn `404 Not Found` trên toàn bộ các controller điều vận (`FleetDispatchController`, `GateRoutesController`, `GateRouteVehiclesController`).
  - `GET /api/v1/system/features` cung cấp cờ trạng thái tính năng cho WebAdmin.
- **WinForms Máy Trạm**:
  - `FeatureOptions.FromAppConfig()`: nạp trực tiếp cấu hình từ `<add key="EnableFleetDispatch" value="false" />` trong `App.config` (với cơ chế fallback an toàn `false`).
- **WebAdmin (React / Vite)**:
  - `FeatureGuard` bảo vệ route `/fleet-dispatch` và `/gate-routes`, tự động điều hướng về `/dashboard` khi tính năng bị tắt.
  - `Sidebar` tự động ẩn nhóm menu "Điều vận phương tiện nội bộ".
  - `VehicleFormDialog`: Ẩn công tắc xe dùng chung liên nhà máy khi tạo mới; khi chỉnh sửa phương tiện hiện có, **bảo toàn nguyên vẹn** giá trị `isShared` và `assignedRouteId` khi tắt cờ (không ghi đè thành false/null).
- **Bộ Test Kiểm Thử Tự Động SLA**:
  - Toàn bộ source code nghiệp vụ điều vận được giữ nguyên vẹn 100%. `FleetDispatchSlaTests` và các unit tests liên quan được cấu hình chạy với `EnableFleetDispatch = true` độc lập để duy trì trọn vẹn 580+ bài test kiểm thử tự động.

### 5. Toàn Vẹn Tham Chiếu Mới & Phê Duyệt Ngoại Lệ Sổ Cái
- **`Tariff`**:
  - Chặn xóa nếu còn thẻ tham chiếu (`409 Conflict - TARIFF_IN_USE_BY_CARDS`).
  - Chặn tắt kích hoạt nếu còn thẻ active tham chiếu (`400 Bad Request - TARIFF_ACTIVE_CARDS_EXIST`, áp dụng chuẩn mã HTTP 400 cho bảo vệ trạng thái hoạt động theo Quy tắc 2).
- **`Card`**:
  - Chặn xóa, chuyển đổi đối tượng gán, chuyển về `Available`, hoặc **thay đổi cấu hình biểu phí (`IsChargeable`, `TariffId`)** nếu thẻ đang có phiên đỗ Active trong bãi (`409 Conflict - CARD_HAS_ACTIVE_PARKING_SESSION`).
  - Quy tắc này áp dụng đồng nhất tuyệt đối trên cả API thao tác đơn lẻ (`CardService`) và công cụ nhập liệu hàng loạt (`CardExcelService`).
  - Cho phép chuyển đổi trạng thái sang `Locked` (Tạm khóa) hoặc `Lost` (Báo mất) ngay cả khi xe đang trong bãi để đảm bảo an ninh (đồng bộ danh sách đen `userType = "blackList"` lên máy FaceID).
  - Cho phép ngoại lệ gia hạn ngày hiệu lực (`ValidTo`) cho thẻ tháng (chặn rút ngắn ngày hoặc sửa `ValidFrom`).
- **Phê duyệt Ngoại lệ Sổ cái Quản trị Sự cố (`Administrative Force-Close Exception` — Matrix Quy tắc 5)**:
  - Phê chuẩn ngoại lệ duy nhất đối với tính bất biến của `ParkingSession` trên Web API: cho phép `POST /api/v1/parking-sessions/{id}/force-close`.
  - Mục đích: Giải phóng các phiên bị treo khi khách mất thẻ (`LostCard`) hoặc sự cố kỹ thuật bốt trạm (`AdminForceClose`), ngăn chặn tê liệt vòng đời thẻ và phương tiện theo Quy tắc 7 & 9.
  - Cơ chế bù đắp an ninh bắt buộc (Compensating Controls):
    1. Phân quyền tối thiểu `Manager, Admin` (`[Authorize(Roles = "Manager,Admin")]`).
    2. Cập nhật nguyên tử có điều kiện (`Status == Active && !IsDeleted`) chống race condition với cổng ra.
    3. Lưu vết kiểm toán bất biến bắt buộc sang `AuditLog`: ghi nhận đầy đủ người đóng (`ClosedBy`), lý do (`CloseReason`), mức phí tính toán tự động ban đầu, mức phí ghi đè thủ công (`OverrideFeeAmount`), cờ thu tiền (`IsFeeCollected`) và ghi chú giải trình.

### 6. Chuẩn Hóa Nhập/Xuất Excel Đồng Bộ & Báo Cáo Doanh Thu
- **Đồng bộ Biểu phí trên `CardExcelService`**:
  - **Mẫu nhập liệu (`CardExcelProfile`)**: Bổ sung cột thứ 5 `Thu phí` (`IsChargeable`) và cột thứ 6 `Mã biểu phí` (`TariffCode`).
  - **Tệp xuất danh sách thẻ (`CardExportExcelProfile`)**: Bổ sung cột thứ 5 `Thu phí`, cột thứ 6 `Mã biểu phí`, cột thứ 7 `Tên biểu phí` (`TariffName`).
  - **Cơ chế Nhập dữ liệu (`ImportCardsAsync`)**:
    - Pre-load toàn bộ danh mục biểu phí hoạt động từ `IRepository<Tariff>`.
    - Thẻ phương tiện nội bộ (`Vehicle`): Nghiêm cấm gán thu phí hoặc chỉ định mã biểu phí.
    - Validate mã biểu phí: Bắt buộc khi bật thu phí, báo lỗi nếu mã không tồn tại hoặc đã ngừng hoạt động.
    - Báo lỗi nếu cột `Thu phí` là `False` nhưng lại điền mã biểu phí.
    - Áp dụng kiểm tra ràng buộc phiên đỗ xe đang hoạt động (`Active Session Lock`): Chặn thay đổi `IsChargeable` hoặc `TariffId` khi xe đang trong bãi.
- **Báo cáo Doanh thu & Thống kê Tài chính (`ReportExcelService` & `StatisticsService`)**:
  - **Điều kiện Lọc Doanh thu Chuẩn mực**: Mọi phép tính tổng doanh thu (`$sum`), báo cáo phân tích ca trực và xuất file Excel doanh thu bắt buộc phải áp dụng bộ lọc:
    `Status == ParkingSessionStatus.Completed && IsFeeCollected == true && !IsDeleted`.
  - Loại trừ tuyệt đối các phiên chưa hoàn thành (`Active`), phiên không thu tiền (`FeeAmount == 0` hoặc miễn phí) và các phiên đã xóa mềm, bảo đảm số liệu doanh thu trên Excel khớp 100% với số liệu thực tế.
  - **Cấu trúc Báo Cáo Doanh Thu Đa Trang Tính (Multi-Sheet Excel Export — ADR 0023 mở rộng)**:
    - Báo cáo Doanh thu xuất ra file Excel gồm 2 trang tính độc lập thông qua cơ chế `ExcelSheetData<T>` và `IExcelService.WriteTwoSheetsAsync<T1, T2>` (chuẩn hóa theo ADR 0023):
      * **Sheet 1 (`RevenueDailySummaryExcelDto`)**: Bảng tổng hợp doanh thu và số lượt xe theo từng ngày (Ngày, Số lượt thu tiền, Tổng lượt ra, Tổng doanh thu, Doanh thu TB).
      * **Sheet 2 (`RevenueTransactionExcelDto`)**: Bảng kê chi tiết từng giao dịch hoàn tất có thu phí (Lượt vào, Biển số xe, Loại xe, Mã thẻ, Khách hàng, Thời điểm vào, Thời điểm ra, Làn vào, Làn ra, Thời lượng, Tiền phí, Trạng thái thu).
    - Cả 2 sheet đều áp dụng Report Title Banner đồng bộ nhận diện hệ thống và kẹp trần an toàn 10,000 dòng (`X-Export-Truncated`) theo chuẩn ADR 0023.
  - **Thống Kê Doanh Thu Thời Gian Thực (ADR 0024 mở rộng)**:
    - API `GET /api/v1/statistics/revenue` nhận tham số `RevenueStatisticsQuery` và trả về `RevenueReportDto` tổng hợp trực tiếp từ MongoDB qua `$facet` pipeline kèm timeline zero-fill theo từng ngày.

### 7. Xóa Hẳn Loại Thẻ Nhân Sự (`Person`) & Chuẩn Hóa 4 Nhóm Thẻ Vận Hành
- **Lý do & Ngữ cảnh**:
  - Ban đầu (theo ADR 0038), hệ thống chỉ phân biệt hai nhóm thẻ thô sơ: `Person` (Thẻ người) và `Vehicle` (Thẻ xe).
  - Khi phân hệ Tính phí và Vé tháng (`Tariff`) được đưa vào vận hành với các loại thẻ cụ thể: `Monthly` (Thẻ tháng/Vé tháng), `VIP` (Thẻ miễn phí/Ưu tiên), `Visitor` (Thẻ khách vãng lai), và `Vehicle` (Thẻ phương tiện nội bộ), loại thẻ `Person` trở nên dư thừa và gây nhập nhằng trong quản lý biểu phí (không rõ thẻ này có thu phí theo tháng hay miễn phí).
- **Quy chuẩn Xóa Bỏ Hoàn Toàn (Hệ thống mới không cần tương thích ngược)**:
  - **Xóa hẳn khỏi Enum `CardTargetType`**: Loại bỏ hoàn toàn giá trị `Person = 1` khỏi định nghĩa enum trong C# và TypeScript. Do hệ thống chưa có dữ liệu vận hành thực tế nên không cần duy trì các cơ chế tương thích ngược phức tạp. Hệ thống chuẩn hóa tuyệt đối 4 loại thẻ vận hành: `Vehicle = 2`, `Visitor = 3`, `Monthly = 4`, `VIP = 5`.
  - **Lưu trữ chuẩn BSON String**: `Card.TargetType` lưu trữ thuần túy bằng `[BsonRepresentation(BsonType.String)]`, không cần custom migration serializer.
  - **Phương thức Domain `card.IsClientBound()`**: Được định nghĩa tập trung qua `CardHelper.IsClientBound(this CardTargetType targetType)`: `targetType == CardTargetType.Monthly || targetType == CardTargetType.VIP`.
  - **Loại bỏ hoàn toàn trên WebAdmin**: Mọi dropdown, bộ lọc, form tạo mới/chỉnh sửa thẻ và tài liệu mẫu Excel không còn bất kỳ dấu vết nào của loại thẻ `Person`.
  - **Bộ lọc ClientBoundOnly**: Bổ sung cờ `clientBoundOnly` vào `CardFilterQuery` giúp các màn hình quản lý khách hàng (`ClientFormDialog`) tải nhanh toàn bộ các thẻ gán người dùng (`Monthly`, `VIP`).

---

## Hệ quả & Đánh đổi (Consequences)

### Tích cực
- **Thông suốt làn xe bốt trạm 100%**: Loại trừ hoàn toàn sự cố đứng làn/nghẽn bốt do popup modal blocking `TaskDialog`, điều khiển thanh chắn linh hoạt qua `AutoOpenBarrier`.
- **Minh bạch tài chính 100%**: Loại trừ hoàn toàn tình huống thu tiền 2 lần hoặc thất thoát phí khi barie gặp trục trặc; số liệu báo cáo doanh thu sổ cái lọc chuẩn xác.
- **Đồng bộ toàn diện xuyên suốt**: Ràng buộc biểu phí và thu phí được áp dụng nhất quán giữa Web API, WinForms và công cụ Nhập/Xuất Excel hàng loạt.
- **Triển khai linh hoạt**: Bật/tắt module điều vận bằng 1 dòng cấu hình mà không làm xáo trộn codebase hay rủi ro hồi quy; WinForms và API đều hỗ trợ cấu hình chuẩn mực.
- **Hiệu năng cao**: Caching biểu phí 60 giây tại làn trạm trực giúp phản hồi quẹt thẻ trong dưới 50ms.
- **Bảo toàn dữ liệu lịch sử**: Snapshot biểu phí bảo vệ hệ thống khỏi biến động giá trong tương lai; lưu trữ `Decimal128` bảo đảm độ chính xác tiền tệ.
- **Xử lý sự cố linh hoạt**: Quản trị viên có công cụ chuẩn mực để giải phóng thẻ/xe bị kẹt mà không cần can thiệp trực tiếp vào DB thủ công.

### Đánh đổi
- Phải đồng bộ múi giờ UTC+7 qua `VietnamTime` trên toàn hệ thống để tránh lệch mốc 00:00 giữa máy chủ cloud và trạm bốt bảo vệ.
- Khi barie lỗi sau khi thu tiền, nhân viên bảo vệ cần thao tác hỗ trợ mở tay hoặc quẹt lại thẻ để hệ thống kích hoạt mở lại mà không thu thêm phí.
- **Kiểm soát rủi ro gian lận khi can thiệp phí thủ công**: Việc cho phép Quản lý nhập `OverrideFeeAmount` trong `ForceClose` tiềm ẩn rủi ro thao túng doanh thu; rủi ro này được bù đắp hoàn toàn bằng việc lưu vết kiểm toán bất biến bắt buộc sang `AuditLog` ghi nhận cả mức phí hệ thống tính và mức phí ghi đè phục vụ hậu kiểm.
