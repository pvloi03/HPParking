using HPParking.Core.Interfaces;
using HPParking.Interfaces;
using HPParking.Core.Licensing;
using HPParking.Core.Models.Entities;
using System;
using System.Threading.Tasks;

namespace HPParking.Services.License
{
    /// <summary>
    /// Quản lý License bản quyền của hệ thống: Lưu và truy vấn trực tiếp từ CSDL MongoDB thông qua generic IRepository<LicenseInfo>
    /// </summary>
    public class LicenseManager(IRepository<LicenseInfo> licenseRepo)
    {
        private readonly IRepository<LicenseInfo> _licenseRepo = licenseRepo ?? throw new ArgumentNullException(nameof(licenseRepo));

        /// <summary>
        /// Lấy LicenseKey đang kích hoạt gần nhất trực tiếp từ MongoDB (theo MachineCode của máy trạm)
        /// </summary>
        public async Task<string?> GetCurrentLicenseKeyAsync(string? machineCode = null)
        {
            var license = await GetCurrentLicenseInfoAsync(machineCode);
            if (license != null && !string.IsNullOrWhiteSpace(license.LicenseKey) && license.IsActive)
            {
                return license.LicenseKey;
            }

            return null;
        }

        /// <summary>
        /// Lấy toàn bộ thực thể LicenseInfo hiện tại (theo MachineCode của máy trạm)
        /// </summary>
        public async Task<LicenseInfo?> GetCurrentLicenseInfoAsync(string? machineCode = null)
        {
            try
            {
                string targetMachineCode = string.IsNullOrWhiteSpace(machineCode)
                    ? HardwareFingerprint.GetMachineCode()
                    : machineCode.Trim();

                // 1. Ưu tiên tìm bản quyền khớp chính xác với MachineCode của máy trạm này
                var license = await _licenseRepo.FindOneAsync(x => x.MachineCode == targetMachineCode && !x.IsDeleted && x.IsActive);
                if (license != null)
                {
                    return license;
                }

                // 2. Fallback nếu trong DB có bản ghi cũ chưa cập nhật MachineCode
                var fallbackLicense = await _licenseRepo.FindOneAsync(x => !x.IsDeleted && x.IsActive);
                if (fallbackLicense != null && (string.IsNullOrWhiteSpace(fallbackLicense.MachineCode) || fallbackLicense.MachineCode == targetMachineCode))
                {
                    return fallbackLicense;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LicenseManager] Lỗi khi truy vấn LicenseInfo: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Lưu thông tin License mới kích hoạt trực tiếp vào MongoDB
        /// </summary>
        public async Task SaveLicenseKeyAsync(string licenseKey, LicensePayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            try
            {
                string cleanKey = licenseKey.Trim();
                string machineCode = payload.MachineCode;
                string customerName = payload.CustomerName;
                DateTime expiryDate = payload.ExpiryDate;
                string signature = cleanKey.Contains('.') ? cleanKey.Split('.')[1] : "";

                var existing = await _licenseRepo.FindOneAsync(x => x.MachineCode == machineCode && !x.IsDeleted);
                if (existing != null)
                {
                    existing.LicenseKey = cleanKey;
                    existing.MachineCode = machineCode;
                    existing.ExpiryDate = expiryDate;
                    existing.Signature = signature;
                    if (!string.IsNullOrWhiteSpace(customerName))
                    {
                        existing.CustomerName = customerName;
                    }
                    existing.IsActive = true;

                    await _licenseRepo.UpdateAsync(existing);
                }
                else
                {
                    var newLicense = new LicenseInfo(
                        customerName: customerName,
                        machineCode: machineCode,
                        expiryDate: expiryDate,
                        licenseKey: cleanKey,
                        signature: signature
                    );
                    await _licenseRepo.AddAsync(newLicense);
                }

                Console.WriteLine($"[LicenseManager] Đã lưu License bản quyền cho máy [{machineCode}] / khách hàng [{customerName}] vào MongoDB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LicenseManager] Lỗi khi lưu License bản quyền vào CSDL MongoDB: {ex.Message}");
                throw;
            }
        }
    }
}
