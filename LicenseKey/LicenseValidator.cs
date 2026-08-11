using HPParking.Helper;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;

namespace HPParking.LicenseKey
{
    public class LicenseInfo
    {
        public string FixedCode { get; set; }
        public string ExpiryDate { get; set; }
        public string MachineCode { get; set; }
    }
    public class LicenseValidator
    {
        public static bool ValidateLicense(string licenseKey, out string error, out int dayExpiryDate)
        {
            error = "";
            dayExpiryDate = 0;
            try
            {
                var parts = licenseKey.Split('.');
                if (parts.Length != 2)
                {
                    error = "Sai định dạng license.";
                    return false;
                }

                string json = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                string signature = parts[1];

                if (!VerifySignature(json, signature, GetPublicKey()))
                {
                    error = "Chữ ký không hợp lệ (có thể bị chỉnh sửa).";
                    return false;
                }

                var license = JsonConvert.DeserializeObject<LicenseInfo>(json);

                if (license.FixedCode != "HOANGPHAT130225")
                {
                    error = "Không đúng phần mềm.";
                    return false;
                }

                if (license.MachineCode != GetCurrentMachineCode())
                {
                    error = "Không đúng mã máy.";
                    return false;
                }

                if (DateTime.TryParse(license.ExpiryDate, out DateTime expiry))
                {
                    dayExpiryDate = (expiry - DateTime.Today).Days;
                    if (expiry < DateTime.Today)
                    {
                        error = "License đã hết hạn.";
                        return false;
                    }
                }
                else
                {
                    error = "Ngày hết hạn không hợp lệ.";
                    return false;
                }

                // ===============================================
                // Kiểm tra chống lùi thời gian hệ thống
                // ===============================================
                DateTime now = DateTime.Now;
                DateTime? lastRunTime = GetLastRunTime();
                if (lastRunTime.HasValue)
                {
                    // Cho phép sai số tối đa 5 phút nếu thời gian máy tính bị lệch nhẹ do đồng bộ
                    if (now < lastRunTime.Value.AddMinutes(-5))
                    {
                        error = "Thời gian hệ thống không hợp lệ (Phát hiện lùi ngày giờ máy tính).";
                        return false;
                    }
                }

                // Cập nhật mốc thời gian chạy mới nhất
                UpdateLastRunTime();

                return true;
            }
            catch (Exception ex)
            {
                error = "Lỗi kiểm tra license: " + ex.Message;
                return false;
            }
        }

        public static bool UpdateLastRunTime()
        {
            try
            {
                DateTime now = DateTime.Now;
                DateTime? existingLast = GetLastRunTime();
                // Không cập nhật nếu thời gian hiện tại bị lùi so với mốc đã ghi nhận
                if (existingLast.HasValue && now < existingLast.Value.AddMinutes(-5))
                {
                    return false;
                }

                string dateStr = now.ToString("o"); // Định dạng ISO 8601
                string encrypted = EncryptString(dateStr, GetCurrentMachineCode());

                using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\HPParking");
                if (key != null)
                {
                    key.SetValue("SysCheck", encrypted);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi cập nhật thời gian chạy: {ex.Message}");
            }
            return false;
        }

        private static DateTime? GetLastRunTime()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\HPParking");
                if (key != null)
                {
                    string encrypted = key.GetValue("SysCheck") as string;
                    if (!string.IsNullOrEmpty(encrypted))
                    {
                        string dateStr = DecryptString(encrypted, GetCurrentMachineCode());
                        if (DateTime.TryParse(dateStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastRun))
                        {
                            return lastRun;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi đọc thời gian chạy: {ex.Message}");
            }
            return null;
        }

        private static string EncryptString(string plainText, string secretKey)
        {
            byte[] keyBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(secretKey));
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using Aes aes = Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
            return Convert.ToBase64String(result);
        }

        private static string DecryptString(string cipherText, string secretKey)
        {
            byte[] keyBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(secretKey));
            byte[] fullBytes = Convert.FromBase64String(cipherText);
            using Aes aes = Aes.Create();
            aes.Key = keyBytes;
            byte[] iv = new byte[16];
            byte[] cipherBytes = new byte[fullBytes.Length - 16];
            Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
            Buffer.BlockCopy(fullBytes, 16, cipherBytes, 0, cipherBytes.Length);
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        static bool VerifySignature(string data, string signature, string publicKeyXml)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKeyXml);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] sigBytes = Convert.FromBase64String(signature);
            return rsa.VerifyData(dataBytes, CryptoConfig.MapNameToOID("SHA256"), sigBytes);
        }

        static string GetPublicKey()
        {
            return @"<RSAKeyValue><Modulus>mccewJ4O4N17U2trcbiw/SWztXE1BUT03mPHXZsyUKyd6rejRHoQDmlZoDVFeA8/KxZa2upwDp1XEtSZRDKRffUUTyPc0r4XcmB68LVxqtuAFd8blHId9TwCco+tlzIGjdt38ks6rC8lKQZbzoQyRxHA8aCQ20xXSoe1rtHeTyK52zKU5XIJ/mv81/hTr4kFbNsIwOZYo5a7xpnB1aMAx5A0loRWudavI9hRvI5/WCvFeQ2dcNDdtYZFwfk5FUYfBIVMtojRZquhdLUjF/2t+0IImaeGH8gxFBrBjBGtnGWrd55iAyqI3SoP3oFNBhhhSjEvJF+ke4OJX5k9KL1kaQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>"; // khóa public
        }

        static string GetCurrentMachineCode()
        {
            // Tùy bạn: lấy HDD serial, MAC, tên máy, v.v.
            return MachineCodeHelper.GetMachineCode();
        }


    }
}
