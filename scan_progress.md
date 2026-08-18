# SCAN PROGRESS - HPParking Bug Report

**Ngày scan:** 2026-08-18  
**Công cụ:** codegraph_explore

---

## BƯỚC 0 — Trạng thái Index

| Thông số | Giá trị |
|-----------|---------|
| Tổng file `.cs` | 69 |
| CodeGraph | ✅ Index sẵn sàng (3.4MB) |

---

## CHECKLIST MODULES

| # | Module | Đường dẫn | File count | Trạng thái | Số lỗi | Ghi chú |
|---|--------|------------|------------|-------------|---------|---------|
| 1 | Services/Camera | Services/Camera/ | 4 | ✅ Đã quét | 1 | - |
| 2 | Services/Controller | Services/Controller/ | 2 | ✅ Đã quét | 3 | - |
| 3 | Services/Devices | Services/Devices/ | 1 | ✅ Đã quét | 4 | - |
| 4 | Services/Parking | Services/Parking/ | 2 | ✅ Đã quét | 5 | - |
| 5 | Services/LPR | Services/LPR/ | 2 | ✅ Đã quét | 3 | - |
| 6 | Services/CCCDReader | Services/CCCDReader/ | 2 | ✅ Đã quét | 3 | 2 trong CCCDReader + 1 trong Form |
| 7 | Services/Storage | Services/Storage/ | 1 | ✅ Đã quét | 2 | - |
| 8 | Services/FaceId | Services/FaceId/ | 1 | ✅ Đã quét | 2 | - |
| 9 | Forms (Root) | Forms/ | 3 | ✅ Đã quét | 2 | - |
| 10 | Forms/ConfigManager | Forms/ConfigManager/ | 9 | ✅ Đã quét | 4 | - |
| 11 | Models/Entities | Models/Entities/ | 7 | ✅ Đã quét | 3 | - |
| 12 | Repositories | Repositories/ | 6 | ✅ Đã quét | 5 | - |
| 13 | Interfaces | Interfaces/ | 10 | ✅ Đã quét | 0 | Interfaces chỉ định nghĩa contracts |
| 14 | Data | Data/ | 1 | ✅ Đã quét | 0 | Các lỗi đã ghi trong Module 11 |
| 15 | SDK | SDK/ | 3 thư mục con | ✅ Đã quét | 1 | SDK là P/Invoke wrappers |
| 16 | Helper | Helper/ | 3 | ✅ Đã quét | 4 | - |
| 17 | UI | UI/ | 1 | ✅ Đã quét | 0 | UI classes chỉ là data containers |
| 18 | Program.cs | Program.cs | 1 | ✅ Đã quét | 0 | Entry point đơn giản |

**Tiến độ: 18/18 modules (100%)**

---

## LỖI ĐÃ TÌM THẤY

### Module 1: Services/Camera (1 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 1 | BaseCameraService.cs | 46 | `Debug.WriteLine(result, string)` - tham số sai, output sẽ là "FalseKết nối thất bại" | 🟡 Trung |

### Module 2: Services/Controller (3 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 2 | ControllerConfig.cs | 41 | `Time = DateTime.Now` - không parse thời gian thực từ log | 🟡 Trung |
| 3 | ParkingWorkflowService.cs | 28 | `MessageBox.Show()` trong Service layer - vi phạm layered architecture | 🔴 Cao |
| 4 | ControllerService.cs | 98-109 | Race condition trong StartAutoReconnect - `Disconnect()` gọi ngoài lock | 🟡 Trung |

### Module 3: Services/Devices (4 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 5 | DeviceOrchestrator.cs | 79-82, 96-99 | Event handlers không unsubscribe - memory leak | 🟡 Trung |
| 6 | DeviceOrchestrator.cs | 160-165 | StartRealtimeLoop race condition với Dispose | 🟡 Trung |
| 7 | DeviceOrchestrator.cs | 133-137 | RemoveFailedConnectTask KeyValuePair comparison thất bại | 🔴 Cao |
| 8 | DeviceOrchestrator.cs | 73, 90 | Cast `int` sang `ushort` không kiểm tra overflow | 🟢 Thấp |

### Module 4: Services/Parking (5 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 9 | ParkingWorkflowService.cs | 93-136 | Bitmap `overviewSave` bị dispose trước khi return | 🔴 Cao |
| 10 | ParkingWorkflowService.cs | 98 | Fire-and-forget Task không awaited - exception bị nuốt | 🟡 Trung |
| 11 | ParkingWorkflowService.cs | 112 | `Card_Code = client.PhoneNumber` - lưu nhầm dữ liệu | 🔴 Cao |
| 12 | ParkingWorkflowService.cs | 135 | `OverviewImage` trong ProcessResult không bao giờ dùng | 🟢 Thấp |
| 13 | ParkingWorkflowService.cs | 114 | So sánh LicensePlate sai logic - mất dữ liệu | 🟡 Trung |

### Module 5: Services/LPR (3 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 14 | LprService.cs | 208 | `candidate.Value.matches[0]` không kiểm tra null - có thể NullReferenceException | 🟡 Trung |
| 15 | LprService.cs | 222 | `new Bitmap(resized)` tạo copy không cần thiết - tốn memory | 🟢 Thấp |
| 16 | LprResult.cs | 13-14 | Bitmap properties không có Dispose guidance - potential memory leak | 🟡 Trung |

### Module 6: Services/CCCDReader (3 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 17 | CccdReaderManager.cs | 86-90 | Empty `catch` block - exception bị nuốt mà không log | 🟡 Trung |
| 18 | CccdReaderManager.cs | 122 | Empty `catch` block trong StopAsync - silent swallow | 🟡 Trung |
| 19 | FrmRegisterClient.cs | 38-40 | Event handlers không unsubscribe khi Form đóng - memory leak | 🔴 Cao |

### Module 7: Services/Storage (2 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 20 | ImageStorageService.cs | 27 | `First()` không kiểm tra encoder tồn tại - sẽ throw nếu không tìm thấy | 🟡 Trung |
| 21 | ImageStorageService.cs | 32 | `bitmap.Save()` không có try-catch - exception không được xử lý | 🟡 Trung |

### Module 8: Services/FaceId (2 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 22 | FaceIdApiService.cs | 47 | Empty `catch` block trong EnsureAuthChallengeAsync - exception bị nuốt | 🟡 Trung |
| 23 | FaceIdApiService.cs | 33 | `ServerCertificateCustomValidationCallback = true` - bỏ qua SSL certificate validation | 🔴 Cao |

### Module 9: Forms (Root) (2 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 24 | FrmMain.cs | 54, 124, 252-253 | Event handlers `OnControllerStatusChanged` và `OnCardSwiped` không unsubscribe khi Form đóng | 🔴 Cao |
| 25 | FrmMain.cs | 198, 202 | Tạo `new Bitmap(result.LprResult.PlateImage)` - memory leak | 🟡 Trung |

### Module 10: Forms/ConfigManager (4 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 26 | FrmConfigManager.cs | 54 | `Application.Restart()` không cleanup resources - có thể gây race condition | 🟡 Trung |
| 27 | UcCompanyManager.cs | 162 | `Clipboard.SetText()` không có try-catch - exception nếu clipboard bị lock | 🟡 Trung |
| 28 | UcLanCarManager.cs | 160-164 | `validationResult.Values[key]` - truy cập trực tiếp không kiểm tra KeyNotFoundException | 🟡 Trung |
| 29 | UcLanCarManager.cs | 227-230 | Tương tự #28 - `validationResult.Values` trong FaceIdOut | 🟡 Trung |

---

## TỔNG KẾT

| Mức độ | Số lỗi |
|--------|--------|
| 🔴 Cao | 6 |
| 🟡 Trung | 21 |
| 🟢 Thấp | 2 |
| **Tổng** | **29 lỗi** |

### Module 11: Models/Entities (3 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 30 | MongoContext.cs | 11-15 | Static constructor không xử lý exception - TypeInitializationException khó debug | 🔴 Cao |
| 31 | MongoContext.cs | 13, 19 | `ConfigurationManager.AppSettings` có thể trả về null - không kiểm tra | 🟡 Trung |
| 32 | Client.cs | 38 | `Expired` property không null-safe - deserialize null sẽ NullReferenceException | 🟡 Trung |

---

## TỔNG KẾT

| Mức độ | Số lỗi |
|--------|--------|
| 🔴 Cao | 7 |
| 🟡 Trung | 31 |
| 🟢 Thấp | 3 |
| **Tổng** | **42 lỗi** |

### Module 12: Repositories (5 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 33 | ClientRepository.cs | 20, 25 | `FirstOrDefaultAsync()` trả về `Client` nhưng có thể null - CS8618 warning không xử lý | 🟡 Trung |
| 34 | CompanyRepository.cs | 22, 27 | Tương tự #33 | 🟡 Trung |
| 35 | EventParkingRepository.cs | 15-20 | Tương tự #33 | 🟡 Trung |
| 36 | DepartmentRepository.cs | 15 | Tương tự #33 | 🟡 Trung |
| 37 | CompanyRepository.cs | 46-50 | `UpdateOneAsync` không kiểm tra `ModifiedCount` - có thể update thất bại nhưng vẫn return true | 🟡 Trung |

---

## TỔNG KẾT

| Mức độ | Số lỗi |
|--------|--------|
| 🔴 Cao | 7 |
| 🟡 Trung | 27 |
| 🟢 Thấp | 2 |
| **Tổng** | **37 lỗi** |

### Module 13: Interfaces (0 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| - | - | - | Interfaces chỉ định nghĩa contracts, không có implementation bugs | - |

---

## TỔNG KẾT

| Mức độ | Số lỗi |
|--------|--------|
| 🔴 Cao | 7 |
| 🟡 Trung | 27 |
| 🟢 Thấp | 2 |
| **Tổng** | **37 lỗi** |

### Module 14: Data (0 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| - | - | - | Các lỗi đã được ghi nhận trong Module 11 (MongoContext.cs) | - |

---

## TỔNG KẾT

| Mức độ | Số lỗi |
|--------|--------|
| 🔴 Cao | 7 |
| 🟡 Trung | 27 |
| 🟢 Thấp | 2 |
| **Tổng** | **37 lỗi** |

### Module 15: SDK (1 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 38 | HiSDK.cs | 8, ZKTecoSDK.cs:10 | DLL names hardcoded không có fallback - DllNotFoundException nếu DLL không tìm thấy | 🟡 Trung |

---

## TỔNG KẾT

| Mức độ | Số lỗi |
|--------|--------|
| 🔴 Cao | 7 |
| 🟡 Trung | 28 |
| 🟢 Thấp | 2 |
| **Tổng** | **38 lỗi** |

### Module 16: Helper (4 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| 39 | MachineCodeHelper.cs | 27 | Empty `catch` block - exception bị nuốt mà không log | 🟡 Trung |
| 40 | FrmHelpers.cs | 22 | `Base64ToBitmap` return `null` thay vì throw exception - inconsistent | 🟢 Thấp |
| 41 | ValidationHelper.cs | 26, 56, 70 | `MessageBox.Show()` trong Helper class - vi phạm separation of concerns | 🟡 Trung |
| 42 | ValidationHelper.cs | 43, 83 | `Values.Add()` có thể throw nếu key đã tồn tại | 🟡 Trung |

---

## TỔNG KẾT

| Mức độ | Số lỗi |
|--------|--------|
| 🔴 Cao | 7 |
| 🟡 Trung | 31 |
| 🟢 Thấp | 3 |
| **Tổng** | **42 lỗi** |

### Module 17: UI (0 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| - | - | - | UI classes chỉ là data containers đơn giản, không có logic | - |

---

## TỔNG KẾT

| Mức độ | Số lỗi |
|--------|--------|
| 🔴 Cao | 7 |
| 🟡 Trung | 31 |
| 🟢 Thấp | 3 |
| **Tổng** | **42 lỗi** |

### Module 18: Program.cs (0 lỗi)

| # | File | Dòng | Mô tả | Mức độ |
|---|------|------|--------|--------|
| - | - | - | Entry point đơn giản, không có bugs rõ ràng | - |

---

## TỔNG KẾT CUỐI CÙNG

| Mức độ | Số lỗi |
|--------|--------|
| 🔴 Cao | 7 |
| 🟡 Trung | 31 |
| 🟢 Thấp | 3 |
| **Tổng** | **42 lỗi** |

---

## ✅ HOÀN THÀNH QUÉT TOÀN BỘ MODULES
