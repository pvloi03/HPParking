using HPParking.Api.DTOs.Devices;
using HPParking.Core.Models.Entities;
using Mapster;

namespace HPParking.Api.Configuration.Mappings
{
    /// <summary>
    /// Cấu hình quy tắc ánh xạ Mapster cho thực thể Thiết bị Ngoại vi (Device)
    /// </summary>
    public class DeviceMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // Ánh xạ Device -> DeviceDto: Tự động tính cờ HasPassword và không để lộ Password
            config.NewConfig<Device, DeviceDto>()
                .Map(dest => dest.HasPassword, src => !string.IsNullOrWhiteSpace(src.Password));

            // Ánh xạ CreateDeviceRequest -> Device
            config.NewConfig<CreateDeviceRequest, Device>();

            // Ánh xạ UpdateDeviceRequest -> Device (bỏ qua Password để xử lý bảo mật tại Service)
            config.NewConfig<UpdateDeviceRequest, Device>()
                .Ignore(dest => dest.Password!);
        }
    }
}
