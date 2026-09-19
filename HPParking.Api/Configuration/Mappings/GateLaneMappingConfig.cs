using HPParking.Api.DTOs.Devices;
using HPParking.Api.DTOs.Gates;
using HPParking.Api.DTOs.Lanes;
using HPParking.Core.Models.Entities;
using Mapster;

namespace HPParking.Api.Configuration.Mappings
{
    /// <summary>
    /// Cấu hình quy tắc ánh xạ Mapster cho cụm nghiệp vụ Cổng &amp; Làn xe (Gate &amp; Lane Management)
    /// </summary>
    public class GateLaneMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // 1. Ánh xạ Gate -> GateDto &amp; GateSummaryDto
            config.NewConfig<Gate, GateDto>();
            config.NewConfig<Gate, GateSummaryDto>();

            // 2. Ánh xạ Lane -> LaneDto &amp; LaneDetailDto
            config.NewConfig<Lane, LaneDto>();
            config.NewConfig<Lane, LaneDetailDto>();

            // 3. Ánh xạ Device -> DeviceSummaryDto
            config.NewConfig<Device, DeviceSummaryDto>();
        }
    }
}
