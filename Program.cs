using HPParking.Data;
using HPParking.Forms;
using HPParking.Forms.CofigManager;
using HPParking.Forms.ConfigManager;
using HPParking.Interfaces;
using HPParking.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

namespace HPParking
{
    internal static class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        [STAThread]
        static void Main()
        {
            var services = new ServiceCollection();

            services.AddSingleton<MongoContext>();

            services.AddScoped<ILaneRepository, LaneRepository>();
            services.AddScoped<IEventParkingRepository, EventParkingRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();

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
    }
}
