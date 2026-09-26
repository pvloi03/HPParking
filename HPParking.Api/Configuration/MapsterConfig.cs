using Mapster;
using MapsterMapper;
using System.Reflection;

namespace HPParking.Api.Configuration
{
    public static class MapsterConfig
    {
        public static void RegisterMapsterConfiguration(this IServiceCollection services)
        {
            var config = TypeAdapterConfig.GlobalSettings;

            // 1. Tự động quét và đăng ký tất cả các class kế thừa IRegister trong Assembly
            config.Scan(Assembly.GetExecutingAssembly());

            // 2. Pre-compile các expression mapping ngay khi khởi động để loại bỏ độ trễ (Cold Start)
            config.Compile();

            // 3. Đăng ký TypeAdapterConfig và IMapper vào DI container
            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();
        }
    }
}
