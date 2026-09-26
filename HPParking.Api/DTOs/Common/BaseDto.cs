namespace HPParking.Api.DTOs.Common
{
    /// <summary>
    /// Lớp cơ sở Generic định danh khóa chính cho DTO
    /// </summary>
    public abstract class BaseDto<TKey>
    {
        public TKey Id { get; set; } = default!;
    }

    /// <summary>
    /// Lớp cơ sở định danh mặc định với khóa chính kiểu string (tương thích MongoDB ObjectId)
    /// </summary>
    public abstract class BaseDto : BaseDto<string>
    {
        public new string Id { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lớp cơ sở Generic chứa thông tin kiểm toán (audit trail) thời gian tạo và cập nhật
    /// </summary>
    public abstract class AuditableDto<TKey> : BaseDto<TKey>
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Lớp cơ sở kiểm toán mặc định với khóa chính kiểu string
    /// </summary>
    public abstract class AuditableDto : AuditableDto<string>
    {
        public new string Id { get; set; } = string.Empty;
    }
}
