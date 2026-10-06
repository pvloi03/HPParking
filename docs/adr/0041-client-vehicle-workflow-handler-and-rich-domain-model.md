# 0041: Trích Xuất Trình Xử Lý Xe Cá Nhân (ClientVehicleWorkflowHandler) và Mô Hình Miền Giàu Hành Vi (Rich Domain Model)

> **Trạng thái:** Chấp thuận (Accepted)  
> **Liên quan (Related):** [ADR 0038](0038-unified-card-and-multi-auth-verification-architecture.md), [ADR 0039](0039-multi-factory-fleet-dispatch-and-sla-watcher.md), [ADR 0040](0040-multi-target-lane-architecture-and-tuple-pattern-matching.md)

Quyết định kiến trúc về việc tái cấu trúc luồng kiểm soát xe cá nhân gắn với khách hàng / nhân sự (`Client`) từ các hàm thủ tục nguyên khối bên trong `ParkingWorkflowService` sang Trình xử lý chuyên trách độc lập (`ClientVehicleWorkflowHandler`) theo mẫu thiết kế hướng đối tượng (OOP / Dedicated Handler Pattern), đồng thời nâng cấp các thực thể `Vehicle` và `Client` từ mô hình thiếu máu (Anemic Domain Model) sang mô hình giàu hành vi (Rich Domain Model) nhằm tuân thủ nguyên lý Trách nhiệm Đơn nhất (SRP), Mở/Đóng (OCP) và tăng cường tính Đóng gói (Encapsulation).

---

## Ngữ cảnh

1. **Sự phình to của `ParkingWorkflowService` (God Class Anti-pattern)**:
   - Sau khi bổ sung các luồng Người đi bộ (ADR 0040) và Xe công vụ dùng chung (ADR 0039), lớp [ParkingWorkflowService.cs](../../HPParking/Services/Parking/ParkingWorkflowService.cs) đã vượt quá 1.240 dòng code với 13 dependencies trong constructor.
   - Nhánh xe cá nhân gắn với `Client` được hiện thực bằng hai private method thủ tục lớn (`ProcessVehicleCardEntryAsync`, `ProcessVehicleCardExitAsync`) chiếm hơn 350 dòng, chịu trách nhiệm cho quá nhiều tác vụ: tra cứu xe của client, kiểm tra phiên đỗ trùng, gọi điều phối phần cứng, đối soát biển số, xử lý lỗi barrier, tạo/cập nhật phiên đỗ và lưu ảnh nền ngầm.
2. **Vi phạm nguyên lý Mở/Đóng (Open/Closed Principle)**:
   - Bất kỳ thay đổi hoặc kiểm thử nào liên quan đến quy tắc đối soát biển số xe cá nhân đều buộc phải chỉnh sửa trực tiếp bên trong `ParkingWorkflowService`, làm tăng nguy cơ gây hồi quy (regression) cho các luồng độc lập khác như xe công vụ, xe tự do radar hoặc người đi bộ.
3. **Mô hình miền thiếu máu (Anemic Domain Model) và rò rỉ logic nghiệp vụ**:
   - Hai thực thể cốt lõi `Vehicle` và `Client` chỉ đóng vai trò là các túi dữ liệu (POCO DTO) chứa getter/setter thuần túy.
   - Các logic nghiệp vụ tự thân như chuẩn hóa và so khớp biển số (`NormalizePlate`), xác thực điều kiện qua cổng (`IsExpired`, `IsActive`) và tra cứu xe sở hữu bị rải rác lặp lại ở nhiều service khác nhau thay vì được đóng gói bên trong chính thực thể miền.
4. **Mẫu hình thành công từ `ISharedVehicleWorkflowHandler`**:
   - Việc trích xuất nhánh điều vận xe công vụ sang [ISharedVehicleWorkflowHandler.cs](../../HPParking/Services/Parking/Handlers/ISharedVehicleWorkflowHandler.cs) đã chứng minh tính hiệu quả vượt trội: unit test cô lập 100% không cần khởi tạo toàn bộ workflow service, code rõ ràng và dễ bảo trì.

---

## Quyết định

### 1. Nâng cấp Sang Mô Hình Miền Giàu Hành Vi (Rich Domain Model)
Đóng gói các hành vi nghiệp vụ cốt lõi trực tiếp vào các thực thể trong `HPParking.Core`:

1. **Thực thể [Vehicle.cs](../../HPParking.Core/Models/Entities/Vehicle.cs)**:
   - Thêm phương thức `MatchesPlate(string? candidatePlate)`: Tự chuẩn hóa biển số (loại bỏ khoảng trắng, dấu gạch nối, dấu chấm và viết hoa) và so khớp với biển số của chính xe đó. Thống nhất một quy chuẩn so khớp duy nhất trong toàn hệ thống.
2. **Thực thể [Client.cs](../../HPParking.Core/Models/Entities/Client.cs)**:
   - Thêm phương thức `CanPassGate(DateTime atTime, out string reason)`: Tự kiểm tra trạng thái kích hoạt (`IsActive`) và hạn sử dụng (`Expired`), đóng gói logic kiểm tra hợp lệ mà không phụ thuộc vào helper bên ngoài.
   - Thêm phương thức `FindMatchingVehicle(string? detectedPlate, IEnumerable<Vehicle> activeVehicles)`: Tra cứu trong danh sách xe thuộc quyền sở hữu xem có phương tiện nào khớp với biển số nhận diện từ camera hay không.
   - Thêm phương thức `RequiresPlateVerification()`: Trả về giá trị của cờ `VerifyVehiclePlate`.

### 2. Định Nghĩa Giao Diện Chuyên Trách `IClientVehicleWorkflowHandler` và `ClientVehicleExecutionContext`
Áp dụng mẫu thiết kế **Introduce Parameter Object** nhằm triệt tiêu mùi mã **Data Clumps** (7 tham số lặp lại), tạo mới DTO `ClientVehicleExecutionContext` và giao diện [IClientVehicleWorkflowHandler.cs](../../HPParking/Services/Parking/Handlers/IClientVehicleWorkflowHandler.cs) tại tầng Service:

```csharp
namespace HPParking.Services.Parking.Handlers
{
    public record ClientVehicleExecutionContext(
        LaneRuntimeContext Context,
        WorkflowTriggerEvent Trigger,
        Client Client,
        string ImageBasePath,
        Func<LaneRuntimeContext, bool>? OnBarrierOpenFailed = null,
        Func<LaneRuntimeContext, string?, Task<string?>>? OnManualPlateInput = null,
        string? DepartmentName = null);

    public interface IClientVehicleWorkflowHandler
    {
        Task<ProcessResult> ProcessEntryAsync(ClientVehicleExecutionContext request);

        Task<ProcessResult> ProcessExitAsync(ClientVehicleExecutionContext request);
    }
}
```

### 3. Hiện Thực `ClientVehicleWorkflowHandler`
Lớp `ClientVehicleWorkflowHandler` đóng gói toàn bộ quy trình kiểm soát xe cá nhân:
- **Dependencies tối giản**: Chỉ nhận `IRepository<Vehicle>`, `IRepository<ParkingSession>`, `ILaneHardwareOrchestrator`, `IImageStorageService` và `ILogger`.
- **Quy trình Lúc Vào (`ProcessEntryAsync`)**:
  1. Kiểm tra phiên gửi đang hoạt động (`AlreadyInParking`) thông qua `ParkingSessionStatus.Active`.
  2. Tra cứu xe của Client và kiểm tra điều kiện biển số qua `Client.FindMatchingVehicle`.
  3. Chụp camera song song và nhận diện OCR qua `ILaneHardwareOrchestrator`.
  4. Đối soát biển số (nếu `VerifyVehiclePlate == true` mà không khớp -> trả về `PlateMismatch`).
  5. Mở barrier và tạo phiên `ParkingSession` mới, lưu ảnh ngầm qua `IImageStorageService`.
- **Quy trình Lúc Ra (`ProcessExitAsync`)**:
  1. Kiểm tra phiên gửi đang hoạt động (`NotInParking`).
  2. Chụp camera làn ra và chạy LPR nhận diện biển số ra.
  3. Thực thi chính sách **Strict Exit Lockout**: Nếu biển số ra không khớp biển số vào (`Vehicle.MatchesPlate`), chặn cứng 100%, không mở barrier và trả về `PlateMismatch`.
  4. Mở barrier ra, cập nhật phiên `ParkingSession` (`Status = Completed`, `OutTime`) và lưu ảnh ra nền ngầm.

### 4. Mỏng Hóa `ParkingWorkflowService` (Thin Dispatcher)
- Inject `IClientVehicleWorkflowHandler` vào constructor của `ParkingWorkflowService`.
- Giữ constructor phụ (fallback constructor) tự khởi tạo `ClientVehicleWorkflowHandler` nếu null để đảm bảo tương thích ngược 100% với toàn bộ bộ test cũ.
- Xóa bỏ hoàn toàn hai private methods cồng kềnh `ProcessVehicleCardEntryAsync` và `ProcessVehicleCardExitAsync`, chuyển giao ủy quyền trực tiếp cho handler:
  ```csharp
  (LaneTargetType.Vehicle, TriggerSource.CardSwipe, LaneDirection.In)
      => await _clientVehicleHandler.ProcessEntryAsync(new ClientVehicleExecutionContext(
          context, trigger, client!, imageBasePath, onBarrierOpenFailed, onManualPlateInput, departmentName)),
  (LaneTargetType.Vehicle, TriggerSource.CardSwipe, LaneDirection.Out)
      => await _clientVehicleHandler.ProcessExitAsync(new ClientVehicleExecutionContext(
          context, trigger, client!, imageBasePath, onBarrierOpenFailed, onManualPlateInput, departmentName)),
  ```

---

## Hệ Quả & Đánh Đổi

### Tích cực
1. **Tuân thủ triệt để SRP & OCP**: Tách biệt hoàn toàn luồng xe cá nhân khỏi bộ điều phối chung, giảm tải kích thước `ParkingWorkflowService` xuống còn một lớp điều phối mỏng.
2. **Khả năng kiểm thử cô lập cao**: Có thể viết bộ test chuyên biệt `ClientVehicleWorkflowHandlerTests` giả lập hardware và repository mà không cần khởi tạo tới 13 dependencies của `ParkingWorkflowService`.
3. **Tăng cường tính đóng gói**: Logic nghiệp vụ chuẩn hóa và so khớp biển số được quy về một nơi duy nhất trên `Vehicle`, triệt tiêu sự phân mảnh mã nguồn.

### Tiêu cực / Đánh đổi
- Thêm một interface và class handler mới cần đăng ký vào Dependency Injection container.
- Cần duy trì tính tương thích ngược ở constructor của `ParkingWorkflowService` để tránh làm vỡ các bài kiểm thử tích hợp và kiểm thử hồi quy sẵn có.
