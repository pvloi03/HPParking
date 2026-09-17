using System;
using System.Threading.Tasks;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Data
{
    public class DbSeeder
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetService<ILogger<DbSeeder>>();

            try
            {
                // 1. Kiểm tra xem đã có bất kỳ tài khoản Admin nào tồn tại trong hệ thống chưa
                var hasAdmin = await userRepo.ExistsAsync(u => u.Role == UserRole.Admin);
                if (hasAdmin)
                {
                    logger?.LogInformation("Đã tồn tại tài khoản Quản trị viên trong hệ thống. Bỏ qua bước khởi tạo (DbSeeder).");
                    return;
                }

                // 2. Đọc thông tin cấu hình Admin ban đầu (mặc định admin / admin123)
                var defaultUsername = config["AdminSeeder:DefaultUsername"] ?? "admin";
                var defaultPassword = config["AdminSeeder:DefaultPassword"] ?? "admin123";
                var defaultFullName = config["AdminSeeder:DefaultFullName"] ?? "System Administrator";

                var adminUser = new User
                {
                    Username = defaultUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                    FullName = defaultFullName,
                    Role = UserRole.Admin,
                    IsActive = true
                };

                await userRepo.AddAsync(adminUser);
                logger?.LogInformation("Khởi tạo thành công tài khoản Quản trị viên mặc định '{Username}' (Role: Admin).", defaultUsername);
            }
            catch (Exception ex)
            {
                // Ghi nhận cảnh báo và tiếp tục chạy bình thường, không làm crash ứng dụng
                logger?.LogWarning(ex, "Không thể kết nối hoặc khởi tạo dữ liệu ban đầu từ DbSeeder: {Message}. Tiếp tục khởi động ứng dụng.", ex.Message);
            }
        }
    }
}
