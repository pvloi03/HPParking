namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ gửi email thông báo / cảnh báo vi phạm
    /// </summary>
    public interface IEmailSenderService
    {
        /// <summary>
        /// Gửi email HTML với tùy chọn đính kèm ảnh
        /// </summary>
        Task<bool> SendEmailAsync(
            IEnumerable<string> toEmails,
            string subject,
            string htmlBody,
            string? attachmentPath = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gửi email HTML với tùy chọn đính kèm ảnh và chỉ định tên hiển thị file đính kèm
        /// </summary>
        Task<bool> SendEmailAsync(
            IEnumerable<string> toEmails,
            string subject,
            string htmlBody,
            string? attachmentPath,
            string? attachmentDisplayName,
            CancellationToken cancellationToken = default);
    }
}
