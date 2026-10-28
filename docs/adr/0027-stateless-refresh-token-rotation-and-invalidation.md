# 0027: Cơ Chế Stateless Refresh Token, Token Rotation Và Thu Hồi Bằng LastLogoutAt

Quyết định thiết kế về việc quản lý phiên đăng nhập dài hạn trong `HPParking.Api`: áp dụng mô hình Stateless Refresh Token mã hóa bằng JWT (thời hạn 7 ngày, khóa bí mật riêng biệt `RefreshTokenSecretKey`), bắt buộc xoay vòng token (Token Rotation: cấp mới cả Access Token và Refresh Token trong mỗi lần làm mới phiên), đồng thời kiểm soát thu hồi token không trạng thái (Stateless Revocation) thông qua việc so khớp mốc thời gian phát hành (`iat`) với thời điểm đăng xuất gần nhất (`LastLogoutAt`) của người dùng trong MongoDB.

## Ngữ cảnh

Trong ứng dụng Web Admin và Mobile App:
1. **Thời hạn ngắn của Access Token**: Nhằm giảm thiểu rủi ro khi Access Token bị đánh cắp, Access Token được cấu hình thời hạn ngắn (theo ca làm việc 8-12 tiếng). Hết ca làm việc hoặc sau chu kỳ ngắn, người dùng cần cơ chế duy trì phiên mà không phải nhập lại mật khẩu nhiều lần.
2. **Chi phí lưu trữ và quản lý Stateful Token**: Mô hình lưu Refresh Token trong cơ sở dữ liệu (Stateful Token Store) đòi hỏi các bảng/collection quản lý token, I/O ghi nhận mỗi lần login/refresh, và tác vụ định kỳ dọn dẹp token rác. Với kiến trúc microservice hoặc API tải cao, cơ chế Stateless (phi trạng thái) giúp giảm tải I/O tối đa.
3. **Nguy cơ Replay Attack khi dùng Stateless Token**: Nếu Refresh Token là phi trạng thái và không lưu DB, khi token bị lộ, kẻ tấn công có thể sử dụng lại cho đến khi token hết hạn. Do đó cần áp dụng **Token Rotation** (mỗi lần refresh sẽ vô hiệu hóa chuỗi cũ bằng cách cấp một chuỗi mới) và một cơ chế hủy phiên khi người dùng chủ động Đăng xuất hoặc Đổi mật khẩu.
4. **Đồng bộ vai trò người dùng (Live Role Sync)**: Khi tài khoản bị đổi quyền (ví dụ từ Manager xuống Viewer) hoặc bị khóa (`IsActive = false`), Access Token cũ có thể vẫn mang claim cũ. Việc refresh token cần nạp lại thông tin mới nhất từ cơ sở dữ liệu để cập nhật quyền ngay lập tức.

## Quyết định

1. **Cấu trúc và ký mã hóa Refresh Token**:
   - Refresh Token được tạo dưới dạng JWT độc lập với chữ ký riêng biệt (`RefreshTokenSecretKey` trong cấu hình `JwtSettings`).
   - Claim bắt buộc: `token_use = "refresh"`, `sub = user.Id`, `jti = Guid.NewGuid()`, `iat = UnixTimeSeconds`.
   - Thời hạn: `RefreshTokenExpiryDays` (mặc định 7 ngày).
   - Tách biệt hoàn toàn Secret Key của Access Token và Refresh Token để nếu một key bị lộ thì key còn lại vẫn an toàn.

2. **Cơ chế Token Rotation (Xoay vòng Token)**:
   - Tại endpoint `POST /api/v1/auth/refresh-token`:
     - Chấp nhận Refresh Token từ HttpOnly Cookie (`hpparking_refresh_token`) hoặc HTTP Header (`X-Refresh-Token`).
     - Xác thực chữ ký, thời hạn và claim `token_use == "refresh"`.
     - Sau khi xác thực hợp lệ, nạp bản ghi `User` mới nhất từ MongoDB để kiểm tra trạng thái hoạt động (`IsActive == true`).
     - Phát hành **cặp token hoàn toàn mới**: Access Token mới + Refresh Token mới với thời hạn được tái lập.
     - Trình duyệt/Client cập nhật cả hai token, token cũ bị thay thế hoàn toàn.

3. **Stateless Invalidation thông qua `LastLogoutAt`**:
   - Bổ sung trường `LastLogoutAt (DateTime?)` vào thực thể `User` trong `HPParking.Core`.
   - Khi người dùng gọi `POST /api/v1/auth/logout` hoặc đổi mật khẩu:
     - Cập nhật `user.LastLogoutAt = DateTime.UtcNow`.
   - Khi người dùng gửi Refresh Token lên endpoint `/refresh-token`:
     - Trích xuất thời điểm phát hành của token (`iat` claim).
     - Nếu `user.LastLogoutAt` có giá trị và `iat <= user.LastLogoutAt`:
       - Lập tức từ chối với mã lỗi `401 Unauthorized` (`AUTH_INVALID_TOKEN`) và thông báo `"Phiên làm việc đã bị hủy bỏ do đăng xuất."`.
     - Nhờ đó, toàn bộ Refresh Token được cấp trước thời điểm Logout đều bị vô hiệu hóa tức thì mà không cần duy trì Blacklist DB khổng lồ.

4. **Kênh truyền nhận đa nền tảng**:
   - Phân hệ Web Admin: truyền nhận qua HttpOnly Cookie (`hpparking_refresh_token`, `SameSite = Lax`, `Path = /api/v1/auth`).
   - Phân hệ Mobile, Desktop và Third-Party: truyền nhận qua Header `X-Refresh-Token` hoặc JSON response payload.

## Hệ quả

- **Tích cực**:
  - Không cần tạo bảng/collection lưu trữ từng token rác trong MongoDB, tiết kiệm RAM và I/O.
  - Ngăn chặn triệt để tấn công Replay Attack nhờ Token Rotation và chốt chặn `LastLogoutAt`.
  - Thu hồi quyền tức thì khi tài khoản bị khóa (`!IsActive`) hoặc khi người dùng đổi mật khẩu / đăng xuất.
  - Tương thích song song cả môi trường Web Admin (HttpOnly Cookie) và Mobile/Desktop (Header/JSON).
- **Đánh đổi**:
  - Khi người dùng bấm Đăng xuất trên một thiết bị, mọi phiên làm việc trên các thiết bị khác của tài khoản đó cũng sẽ bị vô hiệu hóa ở chu kỳ refresh tiếp theo (Global Invalidation). Đây là hành vi chấp nhận được và tăng tính an toàn cho hệ thống bãi xe nội bộ.
