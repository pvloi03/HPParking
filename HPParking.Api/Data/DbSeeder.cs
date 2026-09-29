using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;

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

        public static async Task SeedDefaultGateRouteAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var routeRepo = scope.ServiceProvider.GetRequiredService<IRepository<GateRouteConfig>>();
            var logger = scope.ServiceProvider.GetService<ILogger<DbSeeder>>();

            try
            {
                var hasDefault = await routeRepo.ExistsAsync(r => (r.IsDefault || r.RouteCode == "DEFAULT") && !r.IsDeleted);
                if (hasDefault)
                {
                    logger?.LogInformation("Đã tồn tại Tuyến đường mặc định trong hệ thống. Bỏ qua bước khởi tạo tuyến.");
                    return;
                }

                var defaultRoute = new GateRouteConfig
                {
                    RouteCode = "DEFAULT",
                    RouteName = "Tuyến tự do mặc định (Free-roam SLA)",
                    Description = "Cấu hình thời gian di chuyển và làm việc mặc định cho xe công vụ chạy tự do giữa các cổng/nhà máy.",
                    IsDefault = true,
                    DefaultTravelMinutes = 15,
                    DefaultStayMinutes = 15,
                    GateSteps = [],
                    IsClosedLoop = true,
                    AlertEmails = [],
                    IsActive = true
                };

                await routeRepo.AddAsync(defaultRoute);
                logger?.LogInformation("Khởi tạo thành công Tuyến đường mặc định 'DEFAULT' (Travel: 15m, Stay: 15m).");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Không thể khởi tạo Tuyến đường mặc định từ DbSeeder: {Message}. Tiếp tục khởi động.", ex.Message);
            }
        }
    }
}
