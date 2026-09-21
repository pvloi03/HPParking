using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Vehicles
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu Phương tiện trong tệp Excel mẫu / nhập liệu (Dùng Mã chủ xe làm khóa tham chiếu)
    /// </summary>
    public class VehicleExcelDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType? Type { get; set; } = VehicleType.Car;
        public string? OwnerClientCode { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Phương tiện (Đầy đủ tất cả thông tin, khóa liên kết đổi thành Tên)
    /// </summary>
    public class VehicleExportExcelDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType? Type { get; set; } = VehicleType.Car;
        public string? OwnerClientCode { get; set; }
        public string? OwnerClientName { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Mẫu / Nhập liệu Phương tiện
    /// </summary>
    public class VehicleExcelProfile : ExcelProfile<VehicleExcelDto>
    {
        public VehicleExcelProfile()
        {
            Map(x => x.PlateNumber).ColumnName("Biển số xe").Order(1).Required().WithComment("VD: 30A-12345 hoặc 29M1-99999");
            Map(x => x.Type).ColumnName("Loại xe").Order(2).EnumDropdown<VehicleType>().WithComment("Chọn: Car, Motorbike, Bicycle, Other");
            Map(x => x.OwnerClientCode).ColumnName("Mã chủ xe").Order(3).WithComment("Mã nhân sự hoặc CCCD của chủ xe (VD: NV001)");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(4).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Xuất dữ liệu Phương tiện (Khóa liên kết đổi thành Tên)
    /// </summary>
    public class VehicleExportExcelProfile : ExcelProfile<VehicleExportExcelDto>
    {
        public VehicleExportExcelProfile()
        {
            Map(x => x.PlateNumber).ColumnName("Biển số xe").Order(1);
            Map(x => x.Type).ColumnName("Loại xe").Order(2);
            Map(x => x.OwnerClientCode).ColumnName("Mã chủ xe").Order(3);
            Map(x => x.OwnerClientName).ColumnName("Tên chủ xe").Order(4);
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(5);
        }
    }
}
