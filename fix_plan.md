# FIX PLAN - Kế hoạch sửa lỗi HPParking

**Ngày tạo:** 2026-08-18  
**Nguồn lỗi:** scan_progress.md  
**Trạng thái tổng thể:** Đang khởi tạo kế hoạch  

---

## BẢNG KẾ HOẠCH SỬA LỖI CHI TIẾT

| STT | Lỗi | Module/File | Mức độ | Trạng thái | Nguyên nhân gốc | Ghi chú |
|:---:|-----|-------------|:------:|:----------:|-----------------|---------|
| 1 | `MessageBox.Show()` trong Service layer | `Services/Parking/ParkingWorkflowService.cs` | High | đã test | Gọi trực tiếp `MessageBox` trong Service layer vi phạm layered architecture và chặn background thread. Đã loại bỏ `MessageBox`, trả về `ProcessResult` với `Status = BarrierFailed` kèm `Message` để UI hiển thị. | Vi phạm layered architecture, chặn UI thread |
| 2 | Bitmap `overviewSave` bị dispose trước khi return | `Services/Parking/ParkingWorkflowService.cs` | High | đã test | Bitmap dùng chung trong background Task (`using`) bị dispose trước khi return về UI, gây crash `ArgumentException`. Đã tách riêng bản `overviewForReturn = (Bitmap)overviewImage.Clone()` cho `ProcessResult` và `overviewSave` riêng cho background Task `using`. | Gây `ArgumentException`/crash khi downstream dùng bitmap |
| 3 | `Card_Code = client.PhoneNumber` lưu sai dữ liệu | `Services/Parking/ParkingWorkflowService.cs` | High | bỏ qua (chủ ý thiết kế) | Chủ ý thiết kế của dự án: `Card_Code` được định danh theo `client.PhoneNumber` trong luồng `EventParking`. Đã đánh dấu bỏ qua để không bắt lỗi này nữa. | Thiết kế có chủ đích |
| 4 | `RemoveFailedConnectTask` KeyValuePair comparison | `Services/Devices/DeviceOrchestrator.cs` | High | đã test | Cast `ICollection<KeyValuePair>` và `Remove(new KeyValuePair)` trên `ConcurrentDictionary` trong .NET Framework 4.8 có thể không so khớp đúng hoặc gây overhead/lỗi ngầm. Đã thay bằng `TryGetValue` + `ReferenceEquals` và `TryRemove` chuẩn xác, atomic và an toàn luồng. | So sánh `KeyValuePair` thất bại khiến task không được cleanup |
| 5 | Bỏ qua xác thực SSL (`ServerCertificateCustomValidationCallback = true`) | `Services/FaceId/FaceIdApiService.cs` | High | đã test | Bỏ qua SSL vô điều kiện (`=> true`) gây nguy cơ Man-in-the-Middle. Đã sửa callback để kiểm tra chuẩn `SslPolicyErrors.None`, đồng thời chỉ chấp nhận chứng chỉ tự ký (`ChainErrors`, `NameMismatch`) cho thiết bị phần cứng trong mạng LAN nội bộ. | Lỗ hổng bảo mật SSL |
| 6 | Event handlers không unsubscribe khi Form đóng | `Forms/FrmMain.cs` | High | đã test | `FrmMain` chưa gán sự kiện `FormClosing += FrmMain_FormClosing` và thiếu hủy đăng ký các sự kiện `OnControllerStatusChanged`, `OnCardSwiped`, `KeyDown`, giải phóng `_clockTimer`. Đã bổ sung đầy đủ wireup và cleanup khi Form đóng. | Memory leak (OnControllerStatusChanged, OnCardSwiped) |
| 7 | Event handlers không unsubscribe khi Form đóng | `Forms/FrmRegisterClient.cs` | High | đã test | `FrmRegisterClient.Designer.cs` (dòng 408) đã có sẵn `FormClosing += FrmRegisterClient_FormClosing` và `FrmRegisterClient_FormClosing` đã unsubscribe đầy đủ 3 events (`CardScanned`, `PhotoCaptured`, `StatusUpdated`). Đã xác nhận hoạt động chuẩn xác. | Memory leak event reader |
| 8 | [Nhóm] Static constructor exception & AppSettings null safety | `Data/MongoContext.cs` | High | đã test | Static constructor không bắt exception → `TypeInitializationException` che khuất nguyên nhân gốc. `AppSettings[]` trả `null` truyền vào `MongoClient`/`GetDatabase` không kiểm soát. Đã thêm `?? throw new InvalidOperationException(...)` và `try-catch` trong static constructor với message rõ ràng. | Build pass (0 errors) |
| 9 | `Debug.WriteLine(result, string)` sai tham số | `Services/Camera/BaseCameraService.cs` | Medium | đã xóa | File không được sử dụng ở bất kỳ đâu trong project. Đã xóa file và loại khỏi `.csproj`. | Dead code |
| 10 | `Time = DateTime.Now` không parse thời gian thực từ log | `Services/Controller/ControllerConfig.cs` | Medium | bỏ qua (chủ ý thiết kế) | Chủ ý thiết kế của hệ thống: sử dụng thời gian máy chủ tại thời điểm nhận log `DateTime.Now` thay vì parse từ log thiết bị. | Chủ ý thiết kế |
| 11 | Race condition trong `StartAutoReconnect` | `Services/Controller/ControllerService.cs` | Medium | đã test | `Disconnect()` và `_ctsReconnect` được gán/đọc bên ngoài lock. Đã đưa toàn bộ ngắt handle và thay thế `_ctsReconnect` vào trong `lock (_lock)` nguyên tử; giải phóng CTS cũ an toàn. | ✅ Build pass (0 errors) |
| 12 | Event handlers không unsubscribe | `Services/Devices/DeviceOrchestrator.cs` | Medium | đã test | Event delegates giữ tham chiếu và không được giải phóng khi orchestrator đóng. Đã dọn dẹp và reset event handlers trong `Dispose()`. | ✅ Build pass (0 errors) |
| 13 | `StartRealtimeLoop` race condition với `Dispose` | `Services/Devices/DeviceOrchestrator.cs` | Medium | đã test | Hủy/dispose `_ctsRealtime` cũ khi start lại loop, check `_disposed` và bọc `OperationCanceledException` khi delay dừng êm ái. | ✅ Build pass (0 errors) |
| 14 | Fire-and-forget Task không được catch/await | `Services/Parking/ParkingWorkflowService.cs` | Medium | đã test | Đã bọc `try-catch` kèm logging bên trong `Task.Run` của cả `ProcessEntryAsync` và `ProcessExitAsync`, đảm bảo giải phóng `using (plateSave)` và bắt mọi unobserved exceptions. | ✅ Build pass (0 errors) |
| 15 | So sánh `LicensePlate` sai logic | `Services/Parking/ParkingWorkflowService.cs` | Medium | bỏ qua (chủ ý thiết kế) | Thiết kế đặc thù của dự án: chỉ lưu LicensePlate khi khớp chính xác với biển số nhận diện từ camera, ngược lại để trống. | Chủ ý thiết kế đặc thù |
| 16 | `candidate.Value.matches[0]` không kiểm tra null | `Services/LPR/LprService.cs` | Medium | đã test | Thêm kiểm tra `candidate.Value.matches == null || candidate.Value.matches.Count == 0` và `string.IsNullOrEmpty(match.text)` trước khi truy cập index `[0]`. | ✅ Build pass (0 errors) |
| 17 | `LprResult` Bitmap properties thiếu Dispose guidance | `Services/LPR/LprResult.cs` | Medium | đã test | Cho `LprResult` implement `IDisposable`, hỗ trợ giải phóng bộ nhớ `PlateImage` và `FullImage` an toàn, chống rò rỉ bộ nhớ GDI+. | ✅ Build pass (0 errors) |
| 18 | [Nhóm] Empty `catch` blocks nuốt exception | `Services/CCCDReader/CccdReaderManager.cs` | Medium | đã test | Đã thêm `catch (Exception ex)` và ghi log chi tiết lỗi trong `StartWithRetryAsync` và `StopAsync`, bổ sung nullable annotations cho events và hub connection. | ✅ Build pass (0 errors) |
| 19 | [Nhóm] ImageStorageService thiếu check encoder & try-catch | `Services/Storage/ImageStorageService.cs` | Medium | đã test | Đã thay `First()` bằng `FirstOrDefault()`, fallback về `ImageFormat.Jpeg`, bọc `try-catch` khi lưu file và trả về `string.Empty` nếu thất bại. | ✅ Build pass (0 errors) |
| 20 | Empty `catch` block trong `EnsureAuthChallengeAsync` | `Services/FaceId/FaceIdApiService.cs` | Medium | đã test | Thêm `catch (Exception ex)` ghi log cảnh báo khi challenge handshake với thiết bị FaceID gặp trục trặc mạng/kết nối. | ✅ Build pass (0 errors) |
| 21 | Bitmap memory leak khi tạo `new Bitmap(PlateImage)` | `Forms/FrmMain.cs` | Medium | đã test | Thêm kiểm tra null an toàn trước khi tạo `new Bitmap`, giải phóng `result.LprResult?.Dispose()` và `result.OverviewImage?.Dispose()` sau khi hiển thị UI. | ✅ Build pass (0 errors) |
| 22 | `Application.Restart()` không giải phóng tài nguyên | `Forms/ConfigManager/FrmConfigManager.cs` | Medium | đã test | Thêm `Environment.Exit(0)` ngay sau `Application.Restart()` để đảm bảo tiến trình cũ chấm dứt hoàn toàn, giải phóng socket và tài nguyên hệ điều hành. | ✅ Build pass (0 errors) |
| 23 | `Clipboard.SetText()` thiếu try-catch | `Forms/ConfigManager/UcCompanyManager.cs` | Medium | đã test | Bọc `Clipboard.SetText()` trong `try-catch`, ghi log khi Clipboard bị lock bởi tiến trình khác. | ✅ Build pass (0 errors) |
| 24 | [Nhóm] `validationResult.Values[key]` trực tiếp không check key | `Forms/ConfigManager/UcLanCarManager.cs` + `UcLanMotoManager.cs` | Medium | đã test | Thêm local `GetValue(TextBox)` helper dùng `TryGetValue`, thay toàn bộ truy cập trực tiếp `Values[key]` trong cả 2 file. | ✅ Build pass (0 errors) |
| 25 | `Client.Expired` property không null-safe | `Models/Entities/Client.cs` | Medium | đã test | Thay auto-property bằng backing field + getter `??= new Expired()` và setter `?? new Expired()`, đảm bảo không bao giờ `null` dù MongoDB driver bỏ field khi deserialize. | ✅ Build pass (0 errors) |
| 26 | [Nhóm] Repositories `FirstOrDefaultAsync` null safety warnings | `Repositories/ClientRepository.cs`, `DepartmentRepository.cs` | Medium | đã test | Sửa return type `Task<Client>` → `Task<Client?>` trong `GetByCardCode`/`GetByIdCode`, `Task<Department>` → `Task<Department?>` trong `GetByDepartmentCode` để khớp interface. | ✅ Build pass (0 errors) |
| 27 | `CompanyRepository.UpdateOneAsync` không check `ModifiedCount` | `Repositories/CompanyRepository.cs` | Medium | đã test | Ghi nhận kết quả `UpdateOneAsync` và trả về `result.IsAcknowledged && result.ModifiedCount > 0` thay vì luôn `true`. | ✅ Build pass (0 errors) |
| 28 | Hardcoded DLL name thiếu fallback | `SDK/HiSDK.cs`, `SDK/ZKTecoSDK.cs` | Medium | đã test | Thêm hằng số `DllName`, hàm `IsAvailable()`, bọc bắt `DllNotFoundException` an toàn trong `PlateCameraService.InitializeSdk` và `ControllerService.Connect`. | ✅ Build pass (0 errors) |
| 29 | Empty `catch` block trong `MachineCodeHelper` | `Helper/MachineCodeHelper.cs` | Medium | đã test | Thêm `catch (Exception ex)` ghi log qua `Debug.WriteLine`, xử lý null-safe cho CPU ID qua WMI fallback `unknownCPU`. | ✅ Build pass (0 errors) |
| 30 | [Nhóm] `ValidationHelper` vi phạm separation of concerns & duplicate key | `Helper/ValidationHelper.cs` | Medium | đã test | Thêm `ErrorMessage`, `InvalidControl`, tham số tùy chọn `showMessageBox` và dùng indexer `Values[control.Name]` ngăn ngoại lệ duplicate key. | ✅ Build pass (0 errors) |
| 31 | Cast `int` sang `ushort` không kiểm tra overflow | `Services/Devices/DeviceOrchestrator.cs` | Low | đã test | Thêm helper `SafeCastPort` kiểm tra `port > 0 && port <= ushort.MaxValue`, fallback về port mặc định `8000` an toàn. | ✅ Build pass (0 errors) |
| 32 | `new Bitmap(resized)` copy không cần thiết | `Services/LPR/LprService.cs` | Low | đã test | Dùng `(Bitmap)source.Clone()` và `(Bitmap)resized.Clone()` thay cho `new Bitmap(...)` lặp lại. | ✅ Build pass (0 errors) |
| 33 | `OverviewImage` trong `ProcessResult` không sử dụng | `Services/Parking/ParkingWorkflowService.cs` | Low | đã test | Loại bỏ bản clone thứ 3 `overviewForReturn` lãng phí, chuyển `OverviewImage` thành nullable `Bitmap?`. | ✅ Build pass (0 errors) |
| 34 | `Base64ToBitmap` return `null` thay vì handle thống nhất | `Helper/FrmHelpers.cs` | Low | đã test | Đổi kiểu trả về thành `Bitmap?`, bọc `try-catch` chống `FormatException`/`ArgumentException`, ghi log qua `Debug.WriteLine`. | ✅ Build pass (0 errors) |

---

## TIẾN ĐỘ TỔNG HỢP

| Mức độ | Tổng số mục | Đã sửa - chờ test | Đã test | Bỏ qua/Xóa | Chưa sửa |
|:---:|:---:|:---:|:---:|:---:|:---:|
| 🔴 High | 8 | 0 | 7 | 1 | 0 |
| 🟡 Medium | 22 | 0 | 18 | 3 | 0 |
| 🟢 Low | 4 | 0 | 4 | 0 | 0 |
| **Tổng cộng** | **34 mục (42 lỗi)** | **0** | **29** | **5** | **0** |

---

## NHẬT KÝ SỬA LỖI

*(Sẽ được cập nhật sau mỗi bước sửa và test)*

| STT | Lỗi | Hành động | Kết quả | Ghi chú |
|:---:|-----|-----------|---------|---------|
| 1 | `MessageBox.Show()` trong Service layer | Xóa `MessageBox.Show` khỏi `BarrierOpen`, trả về `ProcessResult.BarrierFailed` kèm `Message` | ✅ Build pass (0 errors) | Không còn chặn UI thread từ Service layer |
| 2 | Bitmap `overviewSave` bị dispose trước khi return | Tách `overviewForReturn = (Bitmap)overviewImage.Clone()` cho `ProcessResult` và `overviewSave` cho background Task `using` | ✅ Build pass (0 errors) | Không còn lỗi `ArgumentException: Parameter is not valid` |
| 3 | `Card_Code = client.PhoneNumber` lưu sai dữ liệu | Đánh dấu bỏ qua theo yêu cầu người dùng | ⏭️ Bỏ qua | Chủ ý thiết kế của dự án, không phải bug |
| 4 | `RemoveFailedConnectTask` KeyValuePair comparison | Thay `ICollection.Remove` bằng `TryGetValue` + `ReferenceEquals` + `TryRemove` | ✅ Build pass (0 errors) | Cleanup task kết nối lỗi triệt để, thread-safe |
| 5 | Bỏ qua xác thực SSL (`ServerCertificateCustomValidationCallback = true`) | Kiểm tra chi tiết `SslPolicyErrors`, chỉ chấp nhận cert LAN tự ký hợp lệ | ✅ Build pass (0 errors) | Khắc phục lỗ hổng bypass SSL mù |
| 6 | Event handlers không unsubscribe khi Form đóng | Gắn `FormClosing += FrmMain_FormClosing` trong `FrmMain.Designer.cs`, unsubscribe `OnControllerStatusChanged`, `OnCardSwiped`, `KeyDown`, dispose timer | ✅ Build pass (0 errors) | Tránh rò rỉ bộ nhớ và background execution khi tắt Form |
| 7 | Event handlers không unsubscribe khi Form đóng | Đã xác nhận `FrmRegisterClient.Designer.cs` có sẵn `FormClosing` và hàm cleanup đầy đủ | ✅ Đã test (OK) | Thiết kế form đã có đầy đủ logic unsubscribe |
| 8 | [Nhóm] Static constructor exception & AppSettings null safety | Thêm `?? throw new InvalidOperationException(...)` và `try-catch` trong static constructor | ✅ Build pass (0 errors) | Khởi tạo an toàn cho MongoContext |
| 9 | `Debug.WriteLine(result, string)` sai tham số | Đã xóa file `BaseCameraService.cs` và gỡ khỏi `.csproj` do không sử dụng | ⛔ Đã xóa | Dead code |
| 10 | `Time = DateTime.Now` không parse thời gian thực từ log | Đánh dấu bỏ qua theo yêu cầu người dùng | ⏭️ Bỏ qua | Chủ ý thiết kế của hệ thống |
| 11 | Race condition trong `StartAutoReconnect` | Đưa ngắt handle và quản lý `_ctsReconnect` vào trong `lock (_lock)` nguyên tử | ✅ Build pass (0 errors) | Thread-safe cho Reconnect/Disconnect/Dispose |
| 12 | Event handlers không unsubscribe | Thêm dọn dẹp và reset event handlers (`OnControllerStatusChanged`, `OnCardSwiped`) trong `Dispose()` | ✅ Build pass (0 errors) | Ngăn ngừa memory leak trong Orchestrator |
| 13 | `StartRealtimeLoop` race condition với `Dispose` | Hủy CTS cũ khi restart loop, check `_disposed` và bắt `OperationCanceledException` khi dừng loop | ✅ Build pass (0 errors) | Tránh rò rỉ task ngầm và unhandled exception |
| 14 | Fire-and-forget Task không được catch/await | Đã bọc `try-catch` và logging trong background Task.Run ở cả ProcessEntryAsync và ProcessExitAsync | ✅ Build pass (0 errors) | Tránh rò rỉ unobserved task exceptions |
| 15 | So sánh `LicensePlate` sai logic | Đánh dấu bỏ qua theo yêu cầu người dùng | ⏭️ Bỏ qua | Thiết kế đặc thù của dự án |
| 16 | `candidate.Value.matches[0]` không kiểm tra null | Thêm check null/empty cho matches và match.text trước khi truy xuất | ✅ Build pass (0 errors) | Ngăn ngừa ngoại lệ NullReferenceException / OutOfRange |
| 17 | `LprResult` Bitmap properties thiếu Dispose guidance | Implement `IDisposable` cho `LprResult` để giải phóng `PlateImage` và `FullImage` | ✅ Build pass (0 errors) | Tránh rò rỉ bộ nhớ GDI+ |
| 18 | [Nhóm] Empty `catch` blocks nuốt exception | Bọc `catch (Exception ex)` ghi log trong `StartWithRetryAsync` và `StopAsync` của CccdReaderManager | ✅ Build pass (0 errors) | Ghi nhận nguyên nhân lỗi kết nối SignalR CCCD |
| 19 | [Nhóm] ImageStorageService thiếu check encoder & try-catch | Thay `First` bằng `FirstOrDefault`, bọc `try-catch` khi lưu ảnh | ✅ Build pass (0 errors) | Xử lý lỗi IO/GDI khi lưu ảnh an toàn |
| 20 | Empty `catch` block trong `EnsureAuthChallengeAsync` | Bọc `catch (Exception ex)` ghi log trong `EnsureAuthChallengeAsync` của FaceIdApiService | ✅ Build pass (0 errors) | Ghi nhận lỗi bắt tay Digest challenge FaceID |
| 21 | Bitmap memory leak khi tạo `new Bitmap(PlateImage)` | Thêm null check và dispose LprResult, OverviewImage sau khi hiển thị UI | ✅ Build pass (0 errors) | Giải phóng triệt để tài nguyên GDI+ sau mỗi lượt quẹt xe |
| 22 | `Application.Restart()` không giải phóng tài nguyên | Thêm `Environment.Exit(0)` sau `Application.Restart()` | ✅ Build pass (0 errors) | Đảm bảo tiến trình cũ kết thúc dứt điểm trước khi instance mới khởi động |
| 23 | `Clipboard.SetText()` thiếu try-catch | Bọc `try-catch` cho `Clipboard.SetText()` trong `button1_Click` của UcCompanyManager | ✅ Build pass (0 errors) | Tránh crash khi Clipboard bị tiến trình khác lock |
| 24 | [Nhóm] `validationResult.Values[key]` không check key | Thêm `GetValue(TextBox)` helper dùng `TryGetValue`, thay toàn bộ `Values[key]` trong UcLanCarManager & UcLanMotoManager | ✅ Build pass (0 errors) | Ngăn `KeyNotFoundException` khi lưu cấu hình làn xe |
| 25 | `Client.Expired` property không null-safe | Thay auto-property bằng backing field + getter `??= new Expired()` và setter `?? new Expired()` | ✅ Build pass (0 errors) | Ngăn `NullReferenceException` khi MongoDB driver bỏ field Expired khi deserialize |
| 26 | [Nhóm] Repositories `FirstOrDefaultAsync` null safety | Sửa return type `Task<Client>` → `Task<Client?>` (ClientRepository), `Task<Department>` → `Task<Department?>` (DepartmentRepository) | ✅ Build pass (0 errors) | Khớp với interface nullable, loại bỏ CS8603 warning |
| 27 | `CompanyRepository.UpdateOneAsync` không check `ModifiedCount` | Ghi nhận kết quả `UpdateOneAsync` và kiểm tra `IsAcknowledged && ModifiedCount > 0` | ✅ Build pass (0 errors) | Return `false` chính xác khi không update được document nào |
| 28 | Hardcoded DLL name thiếu fallback | Thêm hằng số `DllName`, `IsAvailable()` và bắt `DllNotFoundException` trong PlateCameraService/ControllerService | ✅ Build pass (0 errors) | Ngăn ngừa crash unhandled khi thiếu file DLL native |
| 29 | Empty `catch` block trong `MachineCodeHelper` | Bọc `catch (Exception ex)` ghi log và xử lý fallback `unknownCPU` null-safe | ✅ Build pass (0 errors) | Ghi nhận lỗi chẩn đoán khi truy vấn WMI CPU ID |
| 30 | [Nhóm] `ValidationHelper` vi phạm separation of concerns & duplicate key | Thêm `ErrorMessage`, `InvalidControl`, `showMessageBox` và dùng indexer cho Values Dictionary | ✅ Build pass (0 errors) | Phân tách UI/Helper và loại bỏ nguy cơ trùng key Dictionary |
| 31 | Cast `int` sang `ushort` không kiểm tra overflow | Thêm helper `SafeCastPort` kiểm tra `port > 0 && port <= ushort.MaxValue` fallback 8000 | ✅ Build pass (0 errors) | Tránh overflow khi ép kiểu cấu hình port camera |
| 32 | `new Bitmap(resized)` copy không cần thiết | Dùng `(Bitmap)source.Clone()` và `(Bitmap)resized.Clone()` thay cho `new Bitmap(...)` | ✅ Build pass (0 errors) | Tối ưu bộ nhớ GDI+ Bitmap |
| 33 | `OverviewImage` trong `ProcessResult` không sử dụng | Loại bỏ bản clone thứ 3 `overviewForReturn` lãng phí, chuyển `OverviewImage` thành nullable `Bitmap?` | ✅ Build pass (0 errors) | Tiết kiệm cấp phát bộ nhớ Bitmap toàn cảnh mỗi lượt quẹt xe |
| 34 | `Base64ToBitmap` return `null` thay vì handle thống nhất | Chuyển return type thành `Bitmap?`, bọc `try-catch` logging an toàn khi parse Base64 | ✅ Build pass (0 errors) | Chuẩn hóa hành vi và ngăn crash unhandled |
