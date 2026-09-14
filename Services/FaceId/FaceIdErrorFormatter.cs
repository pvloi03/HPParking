using System;

namespace HPParking.Services.FaceId
{
    public static class FaceIdErrorFormatter
    {
        /// <summary>
        /// Chuẩn hóa thông báo lỗi kỹ thuật từ thiết bị FaceID hoặc Database thành thông báo dễ hiểu cho người dùng vận hành.
        /// </summary>
        public static string Format(string rawError, string deviceIp = "")
        {
            string prefix = string.IsNullOrWhiteSpace(deviceIp) ? "" : $"[Thiết bị IP {deviceIp}]: ";

            if (string.IsNullOrWhiteSpace(rawError))
            {
                return $"{prefix}Lỗi không xác định. Vui lòng kiểm tra lại thiết bị hoặc kết nối.";
            }

            string errLower = rawError.ToLowerInvariant();

            // 1. Lỗi chất lượng hoặc trích xuất khuôn mặt
            if (errLower.Contains("subpicanalysismodelingerror") ||
                errLower.Contains("modeling failed") ||
                errLower.Contains("face quality") ||
                errLower.Contains("no face") ||
                errLower.Contains("extract face feature") ||
                errLower.Contains("facelibtype"))
            {
                return $"{prefix}Ảnh khuôn mặt không đạt chuẩn (ảnh mờ, bị che khuất hoặc không nhận diện rõ). Vui lòng nhìn thẳng vào camera và chụp lại ảnh rõ nét.";
            }

            // 2. Lỗi trùng mã định danh / nhân viên
            if (errLower.Contains("employeenoalreadyexist") ||
                errLower.Contains("employeeno is duplicated") ||
                errLower.Contains("user already exist"))
            {
                return $"{prefix}Mã định danh (CCCD) này đã tồn tại trên thiết bị FaceID.";
            }

            // 3. Lỗi trùng mã thẻ
            if (errLower.Contains("cardnoalreadyexist") ||
                errLower.Contains("cardno is duplicated") ||
                errLower.Contains("card already exist") ||
                errLower.Contains("cardexist"))
            {
                return $"{prefix}Mã thẻ/số điện thoại này đã được gán cho người dùng khác trên thiết bị FaceID.";
            }

            // 4. Lỗi Database (kiểm tra trước lỗi timeout chung để tránh nhầm timeout mongo với timeout thiết bị)
            if (errLower.Contains("mongoconnectionexception") ||
                errLower.Contains("timeout waiting for a connection from the pool") ||
                errLower.Contains("serverselectiontimeoutexception") ||
                errLower.Contains("mongodb"))
            {
                return "Không thể kết nối đến máy chủ cơ sở dữ liệu (Database). Vui lòng kiểm tra lại dịch vụ MongoDB.";
            }

            if (errLower.Contains("duplicatekey") ||
                errLower.Contains("e11000"))
            {
                return "Dữ liệu khách hàng đã tồn tại trong cơ sở dữ liệu.";
            }

            // 5. Lỗi Timeout kết nối thiết bị
            if (errLower.Contains("taskcanceledexception") ||
                errLower.Contains("timeout") ||
                errLower.Contains("timed out") ||
                errLower.Contains("quá thời gian"))
            {
                return $"{prefix}Không thể kết nối đến thiết bị (quá thời gian chờ phản hồi). Vui lòng kiểm tra cáp mạng và nguồn thiết bị.";
            }

            // 6. Lỗi kết nối Socket / Network
            if (errLower.Contains("httprequestexception") ||
                errLower.Contains("socketexception") ||
                errLower.Contains("no connection could be made") ||
                errLower.Contains("actively refused") ||
                errLower.Contains("host is down") ||
                errLower.Contains("network unreachable"))
            {
                return $"{prefix}Mất kết nối mạng tới thiết bị FaceID. Vui lòng kiểm tra địa chỉ IP và hệ thống mạng nội bộ.";
            }

            // 7. Lỗi xác thực tài khoản thiết bị
            if (errLower.Contains("401") ||
                errLower.Contains("unauthorized"))
            {
                return $"{prefix}Lỗi xác thực (sai tên đăng nhập hoặc mật khẩu quản trị của thiết bị FaceID).";
            }

            // 8. Lỗi bộ nhớ / quyền truy cập thiết bị
            if (errLower.Contains("403") ||
                errLower.Contains("forbidden") ||
                errLower.Contains("memory is full") ||
                errLower.Contains("storage full") ||
                errLower.Contains("fd full"))
            {
                return $"{prefix}Thiết bị từ chối truy cập hoặc bộ nhớ khuôn mặt của thiết bị FaceID đã đầy.";
            }

            return $"{prefix}{rawError}";
        }
    }
}
