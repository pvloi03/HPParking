namespace HPParking.Api.DTOs.Common
{
    /// <summary>
    /// Chuẩn bao gói phản hồi API (RFC 9110 compliant)
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu phần thân (Data payload)</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public string? TraceId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ApiResponse() { }

        public ApiResponse(bool success, T? data, string message, List<string>? errors = null, string? traceId = null)
        {
            Success = success;
            Data = data;
            Message = message;
            Errors = errors ?? new List<string>();
            TraceId = traceId;
            Timestamp = DateTime.UtcNow;
        }

        public static ApiResponse<T> SuccessResult(T data, string message = "Thao tác thành công", string? traceId = null)
        {
            return new ApiResponse<T>(true, data, message, null, traceId);
        }

        public static ApiResponse<T> CreatedResult(T data, string message = "Tạo mới thành công", string? traceId = null)
        {
            return new ApiResponse<T>(true, data, message, null, traceId);
        }

        public static ApiResponse<T> Failure(string message, List<string>? errors = null, string? traceId = null)
        {
            return new ApiResponse<T>(false, default, message, errors, traceId);
        }

        public static ApiResponse<T> Failure(string message, string error, string? traceId = null)
        {
            return new ApiResponse<T>(false, default, message, new List<string> { error }, traceId);
        }
    }
}
