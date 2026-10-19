using HPParking.Api.DTOs.Vehicles;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Clients
{
    public class UpdateClientRequest
    {
        /// <summary>
        /// Mã định danh nhân sự (bắt buộc, duy nhất, tự động viết hoa, độ dài 1-50 ký tự gồm chữ, số, '-' hoặc '_')
        /// </summary>
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime BirthDay { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? CompanyId { get; set; }
        public string? DepartmentId { get; set; }
        public string? ContractorId { get; set; }
        public ClientType Type { get; set; } = ClientType.Employee;
        public string? Email { get; set; }
        public int Gender { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string CardCode { get; set; } = string.Empty;
        public List<string> AuthMethods { get; set; } = [HPParking.Core.Constants.AuthMethodConstants.FaceId];
        public bool VerifyVehiclePlate { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public Expired Expired { get; set; } = new();
        public string? Note { get; set; }

        /// <summary>
        /// Danh sách phương tiện đăng ký bổ sung kèm theo (tùy chọn)
        /// </summary>
        public List<CreateVehicleRequest>? Vehicles { get; set; }
    }
}
