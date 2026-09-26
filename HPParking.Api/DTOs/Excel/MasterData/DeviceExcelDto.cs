using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.MasterData
{
    /// <summary>
    /// DTO đại diện cho dữ liệu Excel của Thiết bị (Device)
    /// </summary>
    public class DeviceExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DeviceType? Type { get; set; } = DeviceType.Camera;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 8000;
        public string? UserName { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Thiết bị (ADR 0023)
    /// </summary>
    public class DeviceExcelProfile : ExcelProfile<DeviceExcelDto>
    {
        public DeviceExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã thiết bị").Order(1).Required().WithComment("Mã định danh duy nhất (VD: CAM_01)");
            Map(x => x.Name).ColumnName("Tên thiết bị").Order(2).Required().WithComment("Tên hiển thị (VD: Camera biển số Làn 1)");
            Map(x => x.Type).ColumnName("Loại thiết bị").Order(3).EnumDropdown<DeviceType>().WithComment("Chọn: Camera, Controller, FaceId, Other");
            Map(x => x.IpAddress).ColumnName("Địa chỉ IP").Order(4).Required().WithComment("IP tĩnh mạng LAN (VD: 192.168.1.100)");
            Map(x => x.Port).ColumnName("Cổng kết nối").Order(5).WithComment("Port kết nối (VD: 8000 cho Hik, 4370 cho ZKTeco)");
            Map(x => x.UserName).ColumnName("Tài khoản").Order(6).WithComment("Tài khoản đăng nhập thiết bị");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(7).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }
}
