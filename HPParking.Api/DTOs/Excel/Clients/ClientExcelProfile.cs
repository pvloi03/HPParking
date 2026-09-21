using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Clients
{
    /// <summary>
    /// Cấu hình Fluent Profile cho Mẫu / Nhập liệu Khách hàng (Dùng Mã nghiệp vụ làm khóa ngoại tham chiếu)
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
            Map(x => x.Email).ColumnName("Email").Order(6);
            Map(x => x.Type).ColumnName("Loại đối tượng").Order(7).EnumDropdown<ClientType>().WithComment("Chọn: Employee, Contractor, Visitor, VIP, Other");
            Map(x => x.CompanyCode).ColumnName("Mã công ty").Order(8).WithComment("Mã công ty trực thuộc (nếu là Employee/Visitor)");
            Map(x => x.DepartmentCode).ColumnName("Mã phòng ban").Order(9).WithComment("Mã phòng ban trực thuộc (nếu là Employee)");
            Map(x => x.ContractorCode).ColumnName("Mã nhà thầu").Order(10).WithComment("Mã nhà thầu trực thuộc (nếu là Contractor)");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(11).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Xuất dữ liệu Khách hàng (Đầy đủ tất cả thông tin, khóa liên kết đổi thành Tên)
    /// </summary>
    public class ClientExportExcelProfile : ExcelProfile<ClientExportExcelDto>
    {
        public ClientExportExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã khách hàng").Order(1);
            Map(x => x.Name).ColumnName("Họ và tên").Order(2);
            Map(x => x.PhoneNumber).ColumnName("Số điện thoại").Order(3);
            Map(x => x.BirthDay).ColumnName("Ngày sinh").Order(4).Format("dd/MM/yyyy");
            Map(x => x.Address).ColumnName("Địa chỉ").Order(5);
            Map(x => x.Email).ColumnName("Email").Order(6);
            Map(x => x.Type).ColumnName("Loại đối tượng").Order(7);
            Map(x => x.CompanyName).ColumnName("Công ty").Order(8);
            Map(x => x.DepartmentName).ColumnName("Phòng ban").Order(9);
            Map(x => x.ContractorName).ColumnName("Nhà thầu").Order(10);
            Map(x => x.HasFaceId).ColumnName("Khuôn mặt FaceID").Order(11);
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(12);
        }
    }
}
