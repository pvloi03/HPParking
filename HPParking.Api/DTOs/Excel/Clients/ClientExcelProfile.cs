using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Clients
{
    /// <summary>
    /// Cấu hình Fluent Profile cho thực thể Khách hàng và Phương tiện (ADR 0023)
    /// </summary>
    public class ClientExcelProfile : ExcelProfile<ClientExcelDto>
    {
        public ClientExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã khách hàng").Order(1).Required().WithComment("Mã nhân viên hoặc số CCCD");
            Map(x => x.Name).ColumnName("Họ và tên").Order(2).Required();
            Map(x => x.PhoneNumber).ColumnName("Số điện thoại").Order(3).Required().WithComment("Định dạng 10 chữ số (VD: 0912345678)");
            Map(x => x.BirthDay).ColumnName("Ngày sinh").Order(4).Format("dd/MM/yyyy").WithComment("Định dạng dd/MM/yyyy");
            Map(x => x.Address).ColumnName("Địa chỉ").Order(5);
            Map(x => x.PlateNumber).ColumnName("Biển số xe").Order(6).WithComment("VD: 30A-12345 hoặc 29M1-99999");
            Map(x => x.VehicleType).ColumnName("Loại xe").Order(7).EnumDropdown<VehicleType>().WithComment("Chọn: Car, Motorbike, Bicycle, Other");
            Map(x => x.CompanyName).ColumnName("Công ty").Order(8).WithComment("Tên hoặc Mã công ty");
            Map(x => x.DepartmentName).ColumnName("Phòng ban").Order(9).WithComment("Tên hoặc Mã phòng ban");
        }
    }
}
