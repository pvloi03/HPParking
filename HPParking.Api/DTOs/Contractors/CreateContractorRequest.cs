namespace HPParking.Api.DTOs.Contractors
{
    /// <summary>
    /// Dữ liệu đầu vào để tạo mới nhà thầu
    /// </summary>
    public class CreateContractorRequest
    {
        /// <summary>
        /// Mã định danh nhà thầu (duy nhất, không phân biệt hoa thường)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Tên nhà thầu / đơn vị thi công
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Người đại diện liên hệ
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// Số điện thoại liên hệ
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Email liên hệ
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
