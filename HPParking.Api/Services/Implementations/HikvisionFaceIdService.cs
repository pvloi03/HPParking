using HPParking.Api.DTOs.Clients;
using HPParking.Api.Services.Interfaces;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HPParking.Api.Services.Implementations
{
    public class HikvisionFaceIdService : IFaceIdService
    {
        private readonly ConcurrentDictionary<string, HttpClient> _clientCache = new();
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly ILogger<HikvisionFaceIdService> _logger;

        public HikvisionFaceIdService(
            ILogger<HikvisionFaceIdService> logger,
            IHttpClientFactory? httpClientFactory = null)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<FaceIdTerminalResultDto> PushUserAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            string name,
            bool isMale,
            string phoneNumber,
            byte[]? faceImageBytes,
            CancellationToken cancellationToken = default)
        {
            var result = new FaceIdTerminalResultDto
            {
                DeviceIp = terminal.DeviceIp,
                DeviceName = terminal.DeviceName,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                var client = GetOrCreateHttpClient(terminal);

                // 1. Tạo hoặc Cập nhật UserInfo
                var (userOk, userErr) = await UpsertUserAsync(client, employeeNo, name, isMale, cancellationToken);
                if (!userOk)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Lỗi tạo hồ sơ người dùng trên thiết bị: {userErr}";
                    _logger.LogWarning("Nạp User thất bại [{DeviceIp}]: {Error}", terminal.DeviceIp, result.ErrorMessage);
                    return result;
                }

                // 2. Gán Thẻ CardInfo (CardNo = PhoneNumber)
                var (cardOk, cardErr) = await AssignCardAsync(client, employeeNo, phoneNumber, cancellationToken);
                if (!cardOk)
                {
                    // Compensation: Rollback User nếu gán thẻ thất bại
                    await RollbackUserAsync(client, employeeNo);
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Lỗi gán thẻ (SĐT) cho người dùng: {cardErr}";
                    _logger.LogWarning("Gán Thẻ thất bại [{DeviceIp}]: {Error}", terminal.DeviceIp, result.ErrorMessage);
                    return result;
                }

                // 3. Nạp ảnh khuôn mặt (nếu có)
                if (faceImageBytes != null && faceImageBytes.Length > 0)
                {
                    var (faceOk, faceErr) = await UpsertFaceImageAsync(client, employeeNo, faceImageBytes, cancellationToken);
                    if (!faceOk)
                    {
                        // Compensation: Rollback User nếu nạp ảnh khuôn mặt thất bại
                        await RollbackUserAsync(client, employeeNo);
                        result.IsSuccess = false;
                        result.ErrorMessage = $"Lỗi nạp ảnh khuôn mặt lên thiết bị: {faceErr}";
                        _logger.LogWarning("Nạp Face thất bại [{DeviceIp}]: {Error}", terminal.DeviceIp, result.ErrorMessage);
                        return result;
                    }
                }

                result.IsSuccess = true;
                _logger.LogInformation("Nạp FaceID thành công cho {Name} ({EmployeeNo}) trên đầu đọc [{DeviceIp}].", name, employeeNo, terminal.DeviceIp);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ngoại lệ kết nối tới đầu đọc FaceID [{DeviceIp}]: {Message}", terminal.DeviceIp, ex.Message);
                result.IsSuccess = false;
                result.ErrorMessage = $"Lỗi kết nối tới thiết bị ({terminal.DeviceIp}): {ex.Message}";
                return result;
            }
        }

        public async Task<FaceIdTerminalResultDto> DeleteUserAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            var result = new FaceIdTerminalResultDto
            {
                DeviceIp = terminal.DeviceIp,
                DeviceName = terminal.DeviceName,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                var client = GetOrCreateHttpClient(terminal);

                // Bước 1: Xóa Thẻ (CardInfo) để giải phóng chỉ mục duy nhất PhoneNumber
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    var deleteCardPayload = new
                    {
                        CardInfoDelCond = new
                        {
                            CardNoList = new[]
                            {
                                new { cardNo = phoneNumber }
                            }
                        }
                    };

                    using var cardContent = new StringContent(
                        JsonSerializer.Serialize(deleteCardPayload),
                        Encoding.UTF8,
                        "application/json");

                    await client.PutAsync("/ISAPI/AccessControl/CardInfo/Delete?format=json", cardContent, cancellationToken);
                }

                // Bước 2: Xóa Người dùng (UserInfo) -> Thiết bị tự động cascade xóa vector khuôn mặt
                if (!string.IsNullOrWhiteSpace(employeeNo))
                {
                    var deleteUserPayload = new
                    {
                        UserInfoDelCond = new
                        {
                            EmployeeNoList = new[]
                            {
                                new { employeeNo }
                            }
                        }
                    };

                    using var userContent = new StringContent(
                        JsonSerializer.Serialize(deleteUserPayload),
                        Encoding.UTF8,
                        "application/json");

                    var userResponse = await client.PutAsync("/ISAPI/AccessControl/UserInfo/Delete?format=json", userContent, cancellationToken);
                    if (!userResponse.IsSuccessStatusCode)
                    {
                        var userBody = await userResponse.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Xóa User trên FaceID [{DeviceIp}] trả về status {StatusCode}: {Body}", terminal.DeviceIp, userResponse.StatusCode, userBody);
                    }
                }

                result.IsSuccess = true;
                _logger.LogInformation("Đã xóa người dùng {EmployeeNo} trên đầu đọc FaceID [{DeviceIp}].", employeeNo, terminal.DeviceIp);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa người dùng trên FaceID [{DeviceIp}]: {Message}", terminal.DeviceIp, ex.Message);
                result.IsSuccess = false;
                result.ErrorMessage = $"Lỗi kết nối tới thiết bị ({terminal.DeviceIp}): {ex.Message}";
                return result;
            }
        }

        public async Task<bool> PingDeviceAsync(FaceIdTerminalConfig terminal, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetOrCreateHttpClient(terminal);
                using var response = await client.GetAsync("/ISAPI/System/deviceInfo", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Ping FaceID [{DeviceIp}] thất bại: {Message}", terminal.DeviceIp, ex.Message);
                return false;
            }
        }

        private async Task<(bool Success, string? Error)> UpsertUserAsync(
            HttpClient client,
            string employeeNo,
            string name,
            bool isMale,
            CancellationToken cancellationToken)
        {
            var userPayload = new
            {
                UserInfo = new
                {
                    employeeNo,
                    name,
                    userType = "normal",
                    gender = isMale ? "male" : "female",
                    Valid = new
                    {
                        enable = false,
                        beginTime = "2026-01-01T00:00:00",
                        endTime = "2037-12-31T23:59:59",
                        timeType = "local"
                    },
                    doorRight = "1",
                    RightPlan = new[]
                    {
                        new { doorNo = 1, planTemplateNo = "1" }
                    },
                    localUIRight = false
                }
            };

            var json = JsonSerializer.Serialize(userPayload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/ISAPI/AccessControl/UserInfo/Record?format=json", content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            // Nếu đã tồn tại, thử cập nhật thông tin người dùng
            using var modifyContent = new StringContent(json, Encoding.UTF8, "application/json");
            var modifyResponse = await client.PutAsync("/ISAPI/AccessControl/UserInfo/Modify?format=json", modifyContent, cancellationToken);
            if (modifyResponse.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return (false, $"HTTP {(int)response.StatusCode}: {errBody}");
        }

        private async Task<(bool Success, string? Error)> AssignCardAsync(
            HttpClient client,
            string employeeNo,
            string phoneNumber,
            CancellationToken cancellationToken)
        {
            var cardPayload = new
            {
                CardInfo = new
                {
                    employeeNo,
                    cardNo = phoneNumber,
                    cardType = "normalCard"
                }
            };

            var json = JsonSerializer.Serialize(cardPayload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/ISAPI/AccessControl/CardInfo/Record?format=json", content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Contains("cardAlreadyExist", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("cardNoConflict", StringComparison.OrdinalIgnoreCase))
            {
                // Thẻ đã được gán trước đó, chấp nhận hợp lệ
                return (true, null);
            }

            return (false, $"HTTP {(int)response.StatusCode}: {body}");
        }

        private async Task<(bool Success, string? Error)> UpsertFaceImageAsync(
            HttpClient client,
            string employeeNo,
            byte[] faceImageBytes,
            CancellationToken cancellationToken)
        {
            var boundary = "---------------------------" + DateTime.UtcNow.Ticks.ToString("x");
            using var multipart = new MultipartFormDataContent(boundary);
            multipart.Headers.Remove("Content-Type");
            multipart.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);

            // Part 1: FaceDataRecord
            var faceData = new
            {
                faceLibType = "blackFD",
                FDID = "1",
                FPID = employeeNo
            };
            var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(faceData));
            var jsonPart = new ByteArrayContent(jsonBytes);
            jsonPart.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            jsonPart.Headers.ContentLength = jsonBytes.Length;
            multipart.Add(jsonPart, "FaceDataRecord");

            // Part 2: FaceImage
            var imgPart = new ByteArrayContent(faceImageBytes);
            imgPart.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            imgPart.Headers.ContentLength = faceImageBytes.Length;
            multipart.Add(imgPart, "FaceImage", $"{employeeNo}.jpg");

            // Gửi tạo mới ảnh khuôn mặt
            var response = await client.PostAsync("/ISAPI/Intelligent/FDLib/FaceDataRecord?format=json", multipart, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            // Nếu tạo mới lỗi, thử gửi lệnh sửa/cập nhật khuôn mặt
            var updateBoundary = "---------------------------" + DateTime.UtcNow.Ticks.ToString("x");
            using var updateMultipart = new MultipartFormDataContent(updateBoundary);
            updateMultipart.Headers.Remove("Content-Type");
            updateMultipart.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + updateBoundary);

            var jsonPart2 = new ByteArrayContent(jsonBytes);
            jsonPart2.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            jsonPart2.Headers.ContentLength = jsonBytes.Length;
            updateMultipart.Add(jsonPart2, "FaceDataRecord");

            var imgPart2 = new ByteArrayContent(faceImageBytes);
            imgPart2.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            imgPart2.Headers.ContentLength = faceImageBytes.Length;
            updateMultipart.Add(imgPart2, "FaceImage", $"{employeeNo}.jpg");

            var updateResponse = await client.PutAsync("/ISAPI/Intelligent/FDLib/FDSetUp?format=json", updateMultipart, cancellationToken);
            if (updateResponse.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return (false, $"HTTP {(int)response.StatusCode}: {errBody}");
        }

        private async Task RollbackUserAsync(HttpClient client, string employeeNo)
        {
            try
            {
                var deleteUserPayload = new
                {
                    UserInfoDelCond = new
                    {
                        EmployeeNoList = new[]
                        {
                            new { employeeNo }
                        }
                    }
                };

                using var content = new StringContent(
                    JsonSerializer.Serialize(deleteUserPayload),
                    Encoding.UTF8,
                    "application/json");

                await client.PutAsync("/ISAPI/AccessControl/UserInfo/Delete?format=json", content);
                _logger.LogInformation("Đã kích hoạt rollback User {EmployeeNo} trên thiết bị FaceID.", employeeNo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rollback User {EmployeeNo} thất bại: {Message}", employeeNo, ex.Message);
            }
        }

        private HttpClient GetOrCreateHttpClient(FaceIdTerminalConfig terminal)
        {
            var cacheKey = $"{terminal.DeviceIp}|{terminal.Username}|{terminal.Password}";
            return _clientCache.GetOrAdd(cacheKey, _ =>
            {
                if (string.IsNullOrWhiteSpace(terminal.DeviceIp))
                {
                    throw new ArgumentException("Địa chỉ IP thiết bị không được để trống.", nameof(terminal.DeviceIp));
                }

                var baseUri = new Uri($"https://{terminal.DeviceIp}");
                var credentialCache = new CredentialCache
                {
                    { baseUri, "Digest", new NetworkCredential(terminal.Username, terminal.Password) }
                };

                var handler = new HttpClientHandler
                {
                    Credentials = credentialCache,
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };

                var client = new HttpClient(handler)
                {
                    BaseAddress = baseUri,
                    Timeout = TimeSpan.FromSeconds(15)
                };
                client.DefaultRequestHeaders.ConnectionClose = true;
                return client;
            });
        }

        // Cho phép inject HttpClient giả lập trong môi trường Unit Test
        public void RegisterTestClient(string cacheKey, HttpClient client)
        {
            _clientCache[cacheKey] = client;
        }
    }
}
