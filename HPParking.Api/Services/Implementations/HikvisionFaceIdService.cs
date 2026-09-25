using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.Services.Interfaces;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
                    result.ErrorMessage = FaceIdErrorFormatter.Format(userErr ?? "Lỗi tạo hồ sơ người dùng trên thiết bị", terminal.DeviceIp);
                    _logger.LogWarning("Nạp User thất bại [{DeviceIp}]: {Error}", terminal.DeviceIp, result.ErrorMessage);
                    return result;
                }

                // 2. Gán Thẻ CardInfo (tự động dọn thẻ cũ và gán thẻ mới)
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    var cleanCardResult = await CleanAndAssignCardInternalAsync(client, terminal, employeeNo, phoneNumber, cancellationToken);
                    if (!cleanCardResult.IsSuccess)
                    {
                        // Tuyệt đối không xóa User của khách hàng (No rollback), chỉ ghi nhận lỗi cho bước này
                        result.IsSuccess = false;
                        result.ErrorMessage = cleanCardResult.ErrorMessage;
                        _logger.LogWarning("Gán Thẻ thất bại [{DeviceIp}]: {Error}", terminal.DeviceIp, result.ErrorMessage);
                        return result;
                    }
                }

                // 3. Nạp ảnh khuôn mặt (nếu có)
                if (faceImageBytes != null && faceImageBytes.Length > 0)
                {
                    var (faceOk, faceErr) = await UpsertFaceImageAsync(client, employeeNo, faceImageBytes, cancellationToken);
                    if (!faceOk)
                    {
                        // Tuyệt đối không xóa User của khách hàng (No rollback), bảo toàn thẻ và hồ sơ đã nạp
                        result.IsSuccess = false;
                        result.ErrorMessage = FaceIdErrorFormatter.Format(faceErr ?? "Lỗi nạp ảnh khuôn mặt lên thiết bị", terminal.DeviceIp);
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
                result.ErrorMessage = FaceIdErrorFormatter.Format(ex.Message, terminal.DeviceIp);
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

                // Bước 1: Xóa toàn bộ Thẻ (CardInfo) của User
                var userCards = await SearchUserCardsAsync(client, employeeNo, cancellationToken);
                if (!string.IsNullOrWhiteSpace(phoneNumber) && !userCards.Contains(phoneNumber))
                {
                    userCards.Add(phoneNumber);
                }

                if (userCards.Count > 0)
                {
                    await DeleteCardsInternalAsync(client, userCards, cancellationToken);
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
                result.ErrorMessage = FaceIdErrorFormatter.Format(ex.Message, terminal.DeviceIp);
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

        public async Task<bool> PingFastAsync(string deviceIp, int timeoutMs = 600, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceIp)) return false;
            var cleanIp = deviceIp.Trim();
            if (cleanIp.Contains(':')) cleanIp = cleanIp.Split(':')[0];
            if (cleanIp.Contains('/')) cleanIp = cleanIp.Split('/')[0];

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(cleanIp, timeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            catch
            {
                // Fallback thử TCP socket connect cổng 443
            }

            try
            {
                using var tcpClient = new TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);
                await tcpClient.ConnectAsync(cleanIp, 443, cts.Token);
                return tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }

        public async Task<TerminalClientStatusDto> CheckUserStatusAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            CancellationToken cancellationToken = default)
        {
            var status = new TerminalClientStatusDto
            {
                DeviceIp = terminal.DeviceIp,
                DeviceName = terminal.DeviceName,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // 1. Ping nhanh trước
                bool isAlive = await PingFastAsync(terminal.DeviceIp, 600, cancellationToken);
                if (!isAlive)
                {
                    status.IsOnline = false;
                    status.ErrorMessage = "Thiết bị mất kết nối mạng LAN (Không phản hồi Ping)";
                    return status;
                }

                status.IsOnline = true;
                var client = GetOrCreateHttpClient(terminal);

                // 2. Tìm kiếm UserInfo
                var searchPayload = new
                {
                    UserInfoSearchCond = new
                    {
                        searchID = "1",
                        searchResultPosition = 0,
                        maxResults = 1,
                        EmployeeNoList = new[]
                        {
                            new { employeeNo }
                        }
                    }
                };

                using var searchContent = new StringContent(
                    JsonSerializer.Serialize(searchPayload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync("/ISAPI/AccessControl/UserInfo/Search?format=json", searchContent, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    status.ErrorMessage = $"Lỗi truy vấn thiết bị: HTTP {(int)response.StatusCode}";
                    return status;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("UserInfoSearch", out var userInfoSearch))
                {
                    int matches = 0;
                    if (userInfoSearch.TryGetProperty("numOfMatches", out var matchesProp))
                    {
                        matches = matchesProp.GetInt32();
                    }

                    if (matches > 0 && userInfoSearch.TryGetProperty("UserInfo", out var userInfoArray) && userInfoArray.GetArrayLength() > 0)
                    {
                        var userElem = userInfoArray[0];
                        status.UserExists = true;

                        if (userElem.TryGetProperty("numOfFace", out var numFaceProp))
                        {
                            status.HasFace = numFaceProp.GetInt32() > 0;
                        }

                        if (userElem.TryGetProperty("numOfCard", out var numCardProp))
                        {
                            status.CardCount = numCardProp.GetInt32();
                        }
                    }
                }

                // 3. Nếu User tồn tại, truy vấn thêm danh sách thẻ qua CardInfo/Search
                if (status.UserExists)
                {
                    var cards = await SearchUserCardsAsync(client, employeeNo, cancellationToken);
                    status.Cards = cards;
                    if (status.Cards.Count > 0)
                    {
                        status.CardCount = status.Cards.Count;
                    }
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái FaceID [{DeviceIp}] cho {EmployeeNo}: {Msg}", terminal.DeviceIp, employeeNo, ex.Message);
                status.IsOnline = false;
                status.ErrorMessage = FaceIdErrorFormatter.Format(ex.Message, terminal.DeviceIp);
                return status;
            }
        }

        public async Task<FaceIdTerminalResultDto> CleanAndAssignCardAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            string newCardNumber,
            CancellationToken cancellationToken = default)
        {
            var client = GetOrCreateHttpClient(terminal);
            return await CleanAndAssignCardInternalAsync(client, terminal, employeeNo, newCardNumber, cancellationToken);
        }

        private async Task<FaceIdTerminalResultDto> CleanAndAssignCardInternalAsync(
            HttpClient client,
            FaceIdTerminalConfig terminal,
            string employeeNo,
            string newCardNumber,
            CancellationToken cancellationToken)
        {
            var result = new FaceIdTerminalResultDto
            {
                DeviceIp = terminal.DeviceIp,
                DeviceName = terminal.DeviceName,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // 1. Quét tìm tất cả các thẻ đang có của employeeNo trên đầu đọc
                var existingCards = await SearchUserCardsAsync(client, employeeNo, cancellationToken);

                // 2. Tìm các thẻ cũ khác với thẻ mới để xóa sạch (Self-Healing)
                var cardsToDelete = existingCards
                    .Where(c => !string.Equals(c, newCardNumber, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .ToList();

                if (cardsToDelete.Count > 0)
                {
                    await DeleteCardsInternalAsync(client, cardsToDelete, cancellationToken);
                    _logger.LogInformation("Đã tự động dọn {Count} thẻ cũ ({Cards}) của {EmployeeNo} trên đầu đọc [{DeviceIp}].",
                        cardsToDelete.Count, string.Join(", ", cardsToDelete), employeeNo, terminal.DeviceIp);
                }

                // 3. Gán thẻ mới
                if (!string.IsNullOrWhiteSpace(newCardNumber))
                {
                    var (assignOk, assignErr) = await AssignCardAsync(client, employeeNo, newCardNumber, cancellationToken);
                    if (!assignOk)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = FaceIdErrorFormatter.Format(assignErr ?? "Lỗi gán thẻ mới", terminal.DeviceIp);
                        return result;
                    }
                }

                result.IsSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi CleanAndAssignCard trên [{DeviceIp}]: {Msg}", terminal.DeviceIp, ex.Message);
                result.IsSuccess = false;
                result.ErrorMessage = FaceIdErrorFormatter.Format(ex.Message, terminal.DeviceIp);
                return result;
            }
        }

        public async Task<FaceIdTerminalResultDto> DeleteCardAsync(
            FaceIdTerminalConfig terminal,
            string cardNumber,
            CancellationToken cancellationToken = default)
        {
            var result = new FaceIdTerminalResultDto
            {
                DeviceIp = terminal.DeviceIp,
                DeviceName = terminal.DeviceName,
                Timestamp = DateTime.UtcNow
            };

            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                result.IsSuccess = true;
                return result;
            }

            try
            {
                var client = GetOrCreateHttpClient(terminal);
                await DeleteCardsInternalAsync(client, new List<string> { cardNumber }, cancellationToken);
                result.IsSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi xóa thẻ {Card} trên [{DeviceIp}]: {Msg}", cardNumber, terminal.DeviceIp, ex.Message);
                result.IsSuccess = false;
                result.ErrorMessage = FaceIdErrorFormatter.Format(ex.Message, terminal.DeviceIp);
                return result;
            }
        }

        public async Task<FaceIdTerminalResultDto> UpdateUserInfoAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            string name,
            bool isMale,
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
                var (ok, err) = await UpsertUserAsync(client, employeeNo, name, isMale, cancellationToken);
                result.IsSuccess = ok;
                if (!ok)
                {
                    result.ErrorMessage = FaceIdErrorFormatter.Format(err ?? "Lỗi cập nhật User", terminal.DeviceIp);
                }
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = FaceIdErrorFormatter.Format(ex.Message, terminal.DeviceIp);
                return result;
            }
        }

        public async Task<FaceIdTerminalResultDto> UpdateFaceImageAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            byte[] faceImageBytes,
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
                var (ok, err) = await UpsertFaceImageAsync(client, employeeNo, faceImageBytes, cancellationToken);
                result.IsSuccess = ok;
                if (!ok)
                {
                    result.ErrorMessage = FaceIdErrorFormatter.Format(err ?? "Lỗi cập nhật ảnh khuôn mặt", terminal.DeviceIp);
                }
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = FaceIdErrorFormatter.Format(ex.Message, terminal.DeviceIp);
                return result;
            }
        }

        private async Task<List<string>> SearchUserCardsAsync(HttpClient client, string employeeNo, CancellationToken cancellationToken)
        {
            var cards = new List<string>();
            try
            {
                var cardSearchPayload = new
                {
                    CardInfoSearchCond = new
                    {
                        searchID = "1",
                        searchResultPosition = 0,
                        maxResults = 30,
                        EmployeeNoList = new[]
                        {
                            new { employeeNo }
                        }
                    }
                };

                using var content = new StringContent(
                    JsonSerializer.Serialize(cardSearchPayload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync("/ISAPI/AccessControl/CardInfo/Search?format=json", content, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("CardInfoSearch", out var cardSearch) &&
                        cardSearch.TryGetProperty("CardInfo", out var cardArray))
                    {
                        foreach (var cardItem in cardArray.EnumerateArray())
                        {
                            if (cardItem.TryGetProperty("cardNo", out var cardNoProp))
                            {
                                var cardNo = cardNoProp.GetString();
                                if (!string.IsNullOrWhiteSpace(cardNo))
                                {
                                    cards.Add(cardNo);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Lỗi truy vấn thẻ của {EmployeeNo}: {Msg}", employeeNo, ex.Message);
            }
            return cards;
        }

        private async Task DeleteCardsInternalAsync(HttpClient client, List<string> cardNumbers, CancellationToken cancellationToken)
        {
            if (cardNumbers == null || cardNumbers.Count == 0) return;

            var deleteCardPayload = new
            {
                CardInfoDelCond = new
                {
                    CardNoList = cardNumbers.Select(c => new { cardNo = c }).ToArray()
                }
            };

            using var cardContent = new StringContent(
                JsonSerializer.Serialize(deleteCardPayload),
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync("/ISAPI/AccessControl/CardInfo/Delete?format=json", cardContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Xóa danh sách thẻ thất bại: HTTP {Status}: {Body}", response.StatusCode, body);
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
                body.Contains("cardNoAlreadyExist", StringComparison.OrdinalIgnoreCase) ||
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

            // Sửa lỗi: Đọc lỗi chính xác từ updateResponse thay vì response của lệnh POST
            var errBody = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
            return (false, $"HTTP {(int)updateResponse.StatusCode}: {errBody}");
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
