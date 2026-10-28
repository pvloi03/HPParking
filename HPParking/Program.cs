using HPParking.Core.Data;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Repositories;
using HPParking.Core.Services.Pricing;
using HPParking.Forms;
using HPParking.Interfaces;
using HPParking.Services.Hardware;
using HPParking.Services.Health;
using HPParking.Services.License;
using HPParking.Services.LPR;
using HPParking.Services.Parking;
using HPParking.Services.Parking.Handlers;
using HPParking.Services.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

namespace HPParking
{
    internal static class Program
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show(ex?.ToString() ?? "Unhandled domain exception", "HPParking Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "HPParking Thread Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            try
            {
                var services = new ServiceCollection();

                try
                {
                    services.AddSingleton(MongoDbContext.Instance);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] Cảnh báo kết nối MongoDB lúc khởi động: {ex.Message}");
                }

                // 2. Generic Repository bao quát toàn bộ Entities kế thừa BaseEntity (bao gồm Lane, Device, Gate, ParkingSession...)
                services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

                services.AddScoped<LicenseManager>();

                services.AddLogging();

                var featureOptions = FeatureOptions.FromAppConfig();
                services.Configure<FeatureOptions>(opts =>
                {
                    opts.EnableFleetDispatch = featureOptions.EnableFleetDispatch;
                });
                services.AddSingleton(featureOptions);

                services.AddSingleton<IParkingFeeCalculator, ParkingFeeCalculator>();
                services.AddScoped<ITariffCache, TariffCache>();
                services.AddSingleton<ILprService, LprService>();
                services.AddSingleton<IImageStorageService, ImageStorageService>();
                services.AddScoped<ILaneHardwareOrchestrator, LaneHardwareOrchestrator>();
                services.AddScoped<IWorkflowImageStorageOrchestrator, WorkflowImageStorageOrchestrator>();
                services.AddScoped<ISharedVehicleWorkflowHandler, SharedVehicleWorkflowHandler>();
                services.AddScoped<IClientVehicleWorkflowHandler, ClientVehicleWorkflowHandler>();
                services.AddScoped<IVisitorVehicleWorkflowHandler, VisitorVehicleWorkflowHandler>();
                services.AddScoped<IParkingWorkflowService, ParkingWorkflowService>();
                services.AddSingleton<IServerHealthService, ServerHealthService>();

                services.AddTransient<FrmMain>();

                ServiceProvider = services.BuildServiceProvider();

                // Warm-up & Fail-fast: Kiểm tra toàn diện đồ thị DI, khởi tạo MongoDB và làm nóng dịch vụ trước Application.Run
                using (var scope = ServiceProvider.CreateScope())
                {
                    // 1. Kiểm tra kết nối MongoDB & đồng bộ các chỉ mục duy nhất (Indexes)
                    try
                    {
                        var dbContext = scope.ServiceProvider.GetService<MongoDbContext>();
                        if (dbContext != null)
                        {
                            MongoDbIndexConfigurator.EnsureIndexesAsync(dbContext).GetAwaiter().GetResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Program] Cảnh báo khởi tạo chỉ mục MongoDB: {ex.Message}");
                    }

                    // 2. Warm-up bộ điều phối quy trình trung tâm (kéo theo toàn bộ Handlers, LPR, Hardware, Storage & Repositories)
                    _ = scope.ServiceProvider.GetRequiredService<IParkingWorkflowService>();
                    _ = scope.ServiceProvider.GetRequiredService<IClientVehicleWorkflowHandler>();
                    _ = scope.ServiceProvider.GetRequiredService<IVisitorVehicleWorkflowHandler>();
                    _ = scope.ServiceProvider.GetRequiredService<ISharedVehicleWorkflowHandler>();
                    _ = scope.ServiceProvider.GetRequiredService<ILaneHardwareOrchestrator>();
                    _ = scope.ServiceProvider.GetRequiredService<IWorkflowImageStorageOrchestrator>();

                    // 3. Warm-up phân hệ tính phí, bộ đệm biểu phí và kiểm toán (ADR-0043)
                    _ = scope.ServiceProvider.GetRequiredService<IParkingFeeCalculator>();
                    _ = scope.ServiceProvider.GetRequiredService<ITariffCache>();
                    _ = scope.ServiceProvider.GetRequiredService<IRepository<AuditLog>>();

                    // 4. Warm-up hạ tầng trạm trực & bản quyền (các dependency của FrmMain)
                    _ = scope.ServiceProvider.GetRequiredService<IServerHealthService>();
                    _ = scope.ServiceProvider.GetRequiredService<LicenseManager>();
                    _ = scope.ServiceProvider.GetRequiredService<IRepository<Lane>>();
                    _ = scope.ServiceProvider.GetRequiredService<IRepository<Gate>>();
                    _ = scope.ServiceProvider.GetRequiredService<IRepository<Device>>();
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Lấy Form thông qua DI thay vì new MainForm()
                var formMain = ServiceProvider.GetRequiredService<FrmMain>();
                Application.Run(formMain);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Lỗi khởi động HPParking", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
