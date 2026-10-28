using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Entities;

namespace HPParking.Api.DTOs.Excel.Cards
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu Thẻ định danh trong tệp Excel mẫu / nhập liệu
    /// </summary>
    public class CardExcelDto
    {
        public string CardNumber { get; set; } = string.Empty;
        public CardTargetType? TargetType { get; set; } = CardTargetType.Monthly;
        public string? ClientCode { get; set; }
        public string? PlateNumber { get; set; }
        public bool? IsChargeable { get; set; }
        public string? TariffCode { get; set; }
        public CardStatus? Status { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Thẻ định danh (Đã phân giải mã và tên đối tượng liên kết)
    /// </summary>
    public class CardExportExcelDto
    {
        public string CardNumber { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string? AssignedCode { get; set; }
        public string? AssignedName { get; set; }
        public bool IsChargeable { get; set; }
        public string? TariffCode { get; set; }
        public string? TariffName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Mẫu / Nhập liệu Thẻ định danh
    /// </summary>
    public class CardExcelProfile : ExcelProfile<CardExcelDto>
    {
        public CardExcelProfile()
        {
            Map(x => x.CardNumber).ColumnName("Mã thẻ (10 số)").Order(1).Required().WithComment("Mã thẻ 10 chữ số (VD: 0012345678). Nếu nhập thiếu số 0 ở đầu, hệ thống sẽ tự động đệm đủ 10 số.");
            Map(x => x.TargetType).ColumnName("Loại đối tượng").Order(2).EnumDropdown<CardTargetType>().WithComment("Chọn: Monthly (Thẻ tháng), VIP (Thẻ VIP), Visitor (Thẻ vãng lai) hoặc Vehicle (Phương tiện nội bộ)");
            Map(x => x.ClientCode).ColumnName("Mã nhân sự").Order(3).WithComment("Mã nhân sự hoặc CCCD/SĐT nếu loại thẻ là Monthly hoặc VIP (VD: NV001). Bỏ trống nếu là thẻ lưu kho.");
            Map(x => x.PlateNumber).ColumnName("Biển số xe").Order(4).WithComment("Biển số xe cá nhân (đối với thẻ Monthly hoặc VIP) hoặc xe nội bộ (đối với thẻ Vehicle, VD: 30A-12345). Bỏ trống nếu là thẻ lưu kho.");
            Map(x => x.IsChargeable).ColumnName("Thu phí").Order(5).WithComment("Có thu phí hay không (Có/Không hoặc True/False)");
            Map(x => x.TariffCode).ColumnName("Mã biểu phí").Order(6).WithComment("Mã biểu phí áp dụng khi có thu phí (VD: TARIFF_CAR_FLAT). Bỏ trống nếu không thu phí.");
            Map(x => x.Status).ColumnName("Trạng thái").Order(7).EnumDropdown<CardStatus>().WithComment("Chọn: Available (Trong kho), InUse (Đang sử dụng), Locked (Tạm khóa), Lost (Báo mất)");
            Map(x => x.Note).ColumnName("Ghi chú").Order(8).WithComment("Ghi chú bổ sung (tùy chọn)");
        }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Xuất dữ liệu Thẻ định danh ra Excel
    /// </summary>
    public class CardExportExcelProfile : ExcelProfile<CardExportExcelDto>
    {
        public CardExportExcelProfile()
        {
            Map(x => x.CardNumber).ColumnName("Mã thẻ (10 số)").Order(1);
            Map(x => x.TargetType).ColumnName("Đối tượng gán").Order(2);
            Map(x => x.AssignedCode).ColumnName("Mã đối tượng").Order(3);
            Map(x => x.AssignedName).ColumnName("Tên đối tượng").Order(4);
            Map(x => x.IsChargeable).ColumnName("Thu phí").Order(5);
            Map(x => x.TariffCode).ColumnName("Mã biểu phí").Order(6);
            Map(x => x.TariffName).ColumnName("Tên biểu phí").Order(7);
            Map(x => x.Status).ColumnName("Trạng thái").Order(8);
            Map(x => x.Note).ColumnName("Ghi chú").Order(9);
            Map(x => x.CreatedAt).ColumnName("Ngày tạo").Order(10).Format("dd/MM/yyyy HH:mm");
        }
    }
}
