# 0001: Khóa kiến trúc 32-bit (x86) trên nền tảng .NET 8

Ứng dụng bắt buộc biên dịch và thực thi dưới kiến trúc 32-bit (`x86`) trên nền tảng `net8.0-windows` để đảm bảo tính tương thích với các thư viện Native P/Invoke DLL của thiết bị ngoại vi.

## Ngữ cảnh

Hệ thống tích hợp trực tiếp các thư viện C/C++ native bao gồm:
- Engine nhận diện biển số xe SimpleLPR 3.x (`SimpleLPR3.dll`).
- SDK điều khiển Barrier & Controller ZKTeco (`plcommpro.dll`).
- SDK giải mã và hiển thị hình ảnh camera Hikvision (`HCNetSDK.dll`, `PlayCtrl.dll`).

Các DLL này được nhà sản xuất cung cấp dưới dạng nhị phân 32-bit không có phiên bản 64-bit thay thế. Khi chạy trên môi trường 64-bit, CLR sẽ ném lỗi `BadImageFormatException` ngay tại thời điểm gọi P/Invoke đầu tiên.

## Quyết định

1. Thiết lập `<PlatformTarget>x86</PlatformTarget>` và `<Platform Condition=" '$(Platform)' == '' ">x86</Platform>` trong file dự án `HPParking.csproj` và `HPParking.Tests.csproj`.
2. Thiết lập `<AppendPlatformToOutputPath>false</AppendPlatformToOutputPath>` để thư mục đầu ra luôn đồng nhất tại `bin\Debug\net8.0-windows\HPParking.exe`, ngăn chặn lỗi tìm kiếm file thực thi của lệnh `dotnet run`.
3. Tự động sao chép các DLL native từ `Libs/x86/` và `Libs/native/` ra thư mục output khi biên dịch.

## Hệ quả

- Toàn bộ các kiểm thử tự động xUnit và tiến trình phần mềm phải chạy trên runner 32-bit.
- Ứng dụng hoạt động ổn định và gọi thành công 100% các hàm điều khiển barrier và giải mã hình ảnh camera mà không bị xung đột bộ nhớ.
