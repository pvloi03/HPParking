using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Core.Models.Entities;
using Mapster;

namespace HPParking.Api.Configuration.Mappings
{
    /// <summary>
    /// Cấu hình quy tắc ánh xạ Mapster cho cụm nghiệp vụ Khách Hàng và Phương Tiện
    /// </summary>
    public class ClientMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // 1. Ánh xạ Vehicle -> VehicleDto
            config.NewConfig<Vehicle, VehicleDto>();

            // 2. Ánh xạ Client -> ClientDto
            config.NewConfig<Client, ClientDto>();

            // 3. Ánh xạ Client -> ClientDetailDto (bỏ qua Vehicles để nạp thủ công theo logic nghiệp vụ)
            config.NewConfig<Client, ClientDetailDto>()
                  .Ignore(dest => dest.Vehicles);
        }
    }
}
