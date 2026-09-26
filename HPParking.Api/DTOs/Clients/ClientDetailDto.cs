using HPParking.Api.DTOs.Vehicles;

namespace HPParking.Api.DTOs.Clients
{
    public class ClientDetailDto : ClientDto
    {
        public IReadOnlyList<VehicleDto> Vehicles { get; set; } = new List<VehicleDto>();
        public List<TerminalClientStatusDto> FaceIdTerminals { get; set; } = new();
    }
}
