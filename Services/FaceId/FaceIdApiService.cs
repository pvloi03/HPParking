using HPParking.Interfaces;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HPParking.Services.FaceId
{
    public class FaceIdApiService : IFaceIdApiService
    {
        private readonly HttpClient _httpClient;
        public string Ip { get; set; }

        public FaceIdApiService(FaceIdConfig config)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.Expect100Continue = false;

            Ip = config.Ip;
            string baseUrl = $"https://{Ip}";
            Uri baseUri = new(baseUrl);

            var credentialCache = new CredentialCache
            {
                { baseUri, "Digest", new NetworkCredential(config.Username, config.Password) }
            };

            var handler = new HttpClientHandler
            {
                Credentials = credentialCache,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = baseUri,
                Timeout = TimeSpan.FromSeconds(5)
            };

            _httpClient.DefaultRequestHeaders.ConnectionClose = true;
        }

        private async Task EnsureAuthChallengeAsync()
        {
            try { await _httpClient.GetAsync("System/status"); } catch { }
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> AddUserAsync(string employeeNo, string name, bool isMale)
        {
            try
            {
                await EnsureAuthChallengeAsync();

                var newClient = new
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

                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/ISAPI/AccessControl/UserInfo/Record?format=json", newClient);
                if (response.IsSuccessStatusCode)
                {
                    return (true, string.Empty);
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, err);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> AddCardAsync(string employeeNo, string cardNumber)
        {
            try
            {
                var newCard = new
                {
                    CardInfo = new
                    {
                        employeeNo,
                        cardNo = cardNumber,
                        cardType = "normalCard"
                    }
                };

                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/ISAPI/AccessControl/CardInfo/Record?format=json", newCard);
                if (response.IsSuccessStatusCode)
                {
                    return (true, string.Empty);
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, err);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> AddFaceImageAsync(string employeeNo, byte[] faceImg)
        {
            try
            {
                await EnsureAuthChallengeAsync();

                // ============================================
                // 3. JSON FaceDataRecord
                // ============================================

                var faceData = new
                {
                    faceLibType = "blackFD",
                    FDID = "1",
                    FPID = employeeNo
                };

                string jsonPayload =
                    System.Text.Json.JsonSerializer.Serialize(faceData);

                Debug.WriteLine(
                    "[FaceID] FaceDataRecord = " + jsonPayload
                );

                // ============================================
                // 4. Tạo multipart với boundary cố định
                // ============================================

                string boundary =
                    "---------------------------" +
                    DateTime.Now.Ticks.ToString("x");

                using (var content = new MultipartFormDataContent(boundary))
                {
                    // Quan trọng:
                    // Ép Content-Type chính xác giống format Hikvision
                    content.Headers.Remove("Content-Type");

                    content.Headers.TryAddWithoutValidation(
                        "Content-Type",
                        "multipart/form-data; boundary=" + boundary
                    );

                    // ========================================
                    // 5. FaceDataRecord
                    // ========================================

                    byte[] jsonBytes =
                        System.Text.Encoding.UTF8.GetBytes(jsonPayload);

                    var jsonContent =
                        new ByteArrayContent(jsonBytes);

                    jsonContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(
                            "application/json"
                        );

                    // Một số firmware Hikvision khá khó tính
                    // với Content-Length của từng multipart part
                    jsonContent.Headers.ContentLength =
                        jsonBytes.Length;

                    content.Add(
                        jsonContent,
                        "FaceDataRecord"
                    );

                    // ========================================
                    // 6. FaceImage
                    // ========================================

                    var imageContent =
                        new ByteArrayContent(faceImg);

                    imageContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

                    imageContent.Headers.ContentLength =
                        faceImg.Length;

                    content.Add(imageContent, "FaceImage", employeeNo + ".jpg");

                    // ========================================
                    // 8. POST
                    // ========================================

                    HttpResponseMessage response =
                        await _httpClient.PostAsync(
                            "/ISAPI/Intelligent/FDLib/FaceDataRecord?format=json",
                            content
                        );

                    string responseBody =
                        await response.Content.ReadAsStringAsync();

                    Debug.WriteLine(
                        $"[FaceID] HTTP = {(int)response.StatusCode} {response.StatusCode}");

                    Debug.WriteLine(
                        "[FaceID] Response = " +
                        responseBody
                    );

                    // ========================================
                    // 9. Thành công
                    // ========================================

                    if (response.IsSuccessStatusCode)
                    {
                        return (true, string.Empty);
                    }

                    return (
                        false,
                        $"FaceID trả về HTTP {(int)response.StatusCode} " +
                        $"{response.StatusCode}: {responseBody}"
                    );
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi nạp khuôn mặt lên thiết bị FaceID: {ex.Message}");
            }
        }

        public async Task<bool> RollbackUserAsync(string employeeNo)
        {
            try
            {
                var delPayload = new
                {
                    UserInfoDelCond = new
                    {
                        EmployeeNoList = new[]
                        {
                            new { employeeNo }
                        }
                    }
                };

                HttpResponseMessage response = await _httpClient.PutAsJsonAsync("/ISAPI/AccessControl/UserInfo/Delete?format=json", delPayload);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}