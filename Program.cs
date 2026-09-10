using HPParking.Data;
using HPParking.Forms;
using HPParking.Forms.ConfigManager;
using HPParking.Interfaces;
using HPParking.Repositories;
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

                services.AddSingleton<MongoContext>();

                services.AddScoped<ILaneRepository, LaneRepository>();
                services.AddScoped<IEventParkingRepository, EventParkingRepository>();
                services.AddScoped<IClientRepository, ClientRepository>();
                services.AddScoped<ICompanyRepository, CompanyRepository>();
                services.AddScoped<IDepartmentRepository, DepartmentRepository>();

                services.AddSingleton<ILprService, LprService>();
                services.AddSingleton<IImageStorageService, ImageStorageService>();
                services.AddScoped<IParkingWorkflowService, ParkingWorkflowService>();

                services.AddTransient<FrmMain>();
                services.AddTransient<FrmConfigManager>();
                services.AddTransient<FrmLogin>();
                services.AddTransient<UcCompanyManager>();
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
