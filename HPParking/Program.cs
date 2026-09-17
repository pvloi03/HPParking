using HPParking.Core.Data;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Repositories;
using HPParking.Forms;
using HPParking.Forms.ConfigManager;
using HPParking.Interfaces;
using HPParking.Services.Health;
using HPParking.Services.HN212;
using HPParking.Services.License;
using HPParking.Services.LPR;
using HPParking.Services.Parking;
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

                services.AddSingleton<ILprService, LprService>();
                services.AddSingleton<IImageStorageService, ImageStorageService>();
                services.AddScoped<IParkingWorkflowService, ParkingWorkflowService>();
                services.AddSingleton<IServerHealthService, ServerHealthService>();

                services.AddSingleton<IHn212Client, Hn212Client>();
                services.AddTransient<FrmRegisterClient>();

                services.AddTransient<FrmMain>();
                services.AddTransient<FrmConfigManager>();
                services.AddTransient<FrmLogin>();
                services.AddTransient<UcLanMotoManager>();
                services.AddTransient<UcLanCarManager>();

                ServiceProvider = services.BuildServiceProvider();

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
