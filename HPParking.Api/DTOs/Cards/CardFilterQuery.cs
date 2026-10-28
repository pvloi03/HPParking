using HPParking.Core.Models.Entities;

namespace HPParking.Api.DTOs.Cards
{
    /// <summary>
    /// Tiêu chuẩn bộ lọc tìm kiếm và truy vấn Thẻ định danh (dùng chung cho API danh sách và xuất Excel)
    /// </summary>
    public class CardFilterQuery
    {
        public string? Search { get; set; }
        public CardTargetType? TargetType { get; set; }
        public CardStatus? Status { get; set; }
        public bool? UnassignedOnly { get; set; }
        public bool? ClientBoundOnly { get; set; }
        public string? AssignedClientId { get; set; }
        public string? AssignedVehicleId { get; set; }
    }
}
