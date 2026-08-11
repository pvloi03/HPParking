using HPParking.Interfaces;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HPParking.Services.FaceId
{
    public class FaceIdApiService : IFaceIdApiService
    {
        private readonly HttpClient _httpClient;

        public FaceIdApiService(FaceIdConfig config)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.Expect100Continue = false;

            string baseUrl = $"https://{config.Ip}";
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

        private const int MinFaceDimensionPx = 80;
        private const int MaxFaceImageBytes = 200 * 1024;
        private const int TargetLongEdgePx = 720;

        private static (byte[] Bytes, string ErrorMessage) NormalizeFaceImage(byte[] originalBytes)
        {
            using MemoryStream inputStream = new(originalBytes);
            using Bitmap original = new(inputStream);

            Debug.WriteLine($"[FaceID] Ảnh gốc: {original.Width}x{original.Height}px, {originalBytes.Length / 1024.0:F1}KB");

            if (original.Width < MinFaceDimensionPx || original.Height < MinFaceDimensionPx)
            {
                return (null, $"Ảnh khuôn mặt quá nhỏ ({original.Width}x{original.Height}px). Yêu cầu tối thiểu {MinFaceDimensionPx}x{MinFaceDimensionPx}px.");
            }

            ImageCodecInfo jpgEncoder = ImageCodecInfo
                .GetImageEncoders()
                .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

            // Bước 0: phóng to nếu cạnh dài hiện tại nhỏ hơn TargetLongEdgePx,
            // giữ nguyên tỉ lệ khung hình gốc.
            Bitmap workingImage = original;
            bool isUpscaled = false;
            int currentLongEdge = Math.Max(original.Width, original.Height);

            if (currentLongEdge < TargetLongEdgePx)
            {
                double upscaleRatio = (double)TargetLongEdgePx / currentLongEdge;
                int upscaledWidth = (int)Math.Round(original.Width * upscaleRatio);
                int upscaledHeight = (int)Math.Round(original.Height * upscaleRatio);

                Bitmap upscaled = new(upscaledWidth, upscaledHeight);
                using (Graphics g = Graphics.FromImage(upscaled))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(original, 0, 0, upscaledWidth, upscaledHeight);
                }

                workingImage = upscaled;
                isUpscaled = true;
                Debug.WriteLine($"[FaceID] Đã phóng to lên {upscaledWidth}x{upscaledHeight}px (giữ tỉ lệ {original.Width}:{original.Height}).");
            }

            try
            {
                byte[] workingBytes = isUpscaled ? EncodeJpeg(workingImage, jpgEncoder, 90L) : originalBytes;

                // Nếu đã đạt chuẩn dung lượng, dùng luôn (tránh nén thêm không
                // cần thiết).
                if (workingBytes.Length <= MaxFaceImageBytes)
                {
                    return (workingBytes, null);
                }

                // Bước 1: giảm dần chất lượng nén JPEG (giữ nguyên kích thước pixel).
                for (long quality = 85; quality >= 40; quality -= 15)
                {
                    byte[] compressed = EncodeJpeg(workingImage, jpgEncoder, quality);
                    if (compressed.Length <= MaxFaceImageBytes)
                    {
                        Debug.WriteLine($"[FaceID] Đã nén còn {compressed.Length / 1024.0:F1}KB (chất lượng {quality}%).");
                        return (compressed, null);
                    }
                }

                // Bước 2: nếu vẫn quá 200KB dù đã nén chất lượng thấp, resize giảm
                // kích thước pixel (giữ tỉ lệ), nhưng không nhỏ hơn mức tối thiểu.
                double scale = 0.8;
                while (scale > 0.3)
                {
                    int newWidth = Math.Max(MinFaceDimensionPx, (int)(workingImage.Width * scale));
                    int newHeight = Math.Max(MinFaceDimensionPx, (int)(workingImage.Height * scale));

                    using Bitmap resized = new(newWidth, newHeight);
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.DrawImage(workingImage, 0, 0, newWidth, newHeight);
                    }

                    byte[] compressed = EncodeJpeg(resized, jpgEncoder, 70L);

                    if (compressed.Length <= MaxFaceImageBytes)
                    {
                        Debug.WriteLine($"[FaceID] Đã resize còn {newWidth}x{newHeight}px, {compressed.Length / 1024.0:F1}KB.");
                        return (compressed, null);
                    }

                    scale -= 0.15;
                }

                return (null, "Không thể nén ảnh khuôn mặt xuống dưới 200KB dù đã giảm chất lượng và kích thước.");
            }
            finally
            {
                if (isUpscaled)
                {
                    workingImage.Dispose();
                }
            }
        }

        private static byte[] EncodeJpeg(Bitmap bitmap, ImageCodecInfo encoder, long quality)
        {
            using EncoderParameters encoderParams = new(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            using MemoryStream ms = new();
            bitmap.Save(ms, encoder, encoderParams);
            return ms.ToArray();
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> AddFaceImageAsync(string employeeNo, string faceBase64)
        {
            try
            {
                await EnsureAuthChallengeAsync();

                // ============================================
                // 1. Base64 -> byte[]
                // ============================================

                if (string.IsNullOrWhiteSpace(faceBase64))
                {
                    return (false, "Ảnh khuôn mặt không có dữ liệu.");
                }

                // Hỗ trợ cả:
                // data:image/jpeg;base64,/9j/4AAQ...
                if (faceBase64.Contains(","))
                {
                    faceBase64 = faceBase64.Substring(
                        faceBase64.IndexOf(",") + 1
                    );
                }

                byte[] originalBytes;

                try
                {
                    originalBytes = Convert.FromBase64String(faceBase64);
                }
                catch (FormatException)
                {
                    return (false, "Dữ liệu Base64 của ảnh không hợp lệ.");
                }

                // ============================================
                // 2. Chuẩn hóa ảnh
                // ============================================

                var (normalizedBytes, normalizeError) =
                    NormalizeFaceImage(originalBytes);

                if (normalizedBytes == null)
                {
                    return (false, normalizeError);
                }

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
                        new ByteArrayContent(normalizedBytes);

                    imageContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

                    imageContent.Headers.ContentLength =
                        normalizedBytes.Length;

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