using HPParking.Api.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.IO;

namespace HPParking.Api.Services.Implementations
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSenderService> _logger;

        public EmailSenderService(IConfiguration configuration, ILogger<EmailSenderService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string? ResolvePhysicalAttachmentPath(string? rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return null;

            if (Path.IsPathRooted(rawPath) && File.Exists(rawPath))
            {
                return rawPath;
            }

            var rootPathConfig = _configuration["StorageSettings:RootPath"]
                ?? _configuration["StorageSettings:UploadPath"]
                ?? @"C:\Users\ADMIN\Pictures\hpparking";

            var cleanPath = rawPath.Trim().Replace('\\', '/');
            if (cleanPath.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(8);
            }
            else if (cleanPath.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(7);
            }
            cleanPath = cleanPath.TrimStart('/');

            var candidates = new List<string>
            {
                Path.Combine(rootPathConfig, cleanPath.Replace('/', Path.DirectorySeparatorChar)),
                Path.Combine(rootPathConfig, "Captures", cleanPath.Replace('/', Path.DirectorySeparatorChar)),
                Path.Combine(Directory.GetCurrentDirectory(), cleanPath.Replace('/', Path.DirectorySeparatorChar))
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        public MimeMessage CreateMimeMessage(
            IEnumerable<string> toEmails,
            string subject,
            string htmlBody,
            string? attachmentPath = null,
            string? attachmentDisplayName = null)
        {
            var recipientList = toEmails?.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).Distinct().ToList()
                ?? new List<string>();

            string fromEmail = _configuration["Smtp:FromEmail"] ?? "no-reply@hpparking.local";
            string fromName = _configuration["Smtp:FromName"] ?? "HPParking Alert System";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));

            foreach (var email in recipientList)
            {
                message.To.Add(MailboxAddress.Parse(email));
            }

            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            var resolvedAttachment = ResolvePhysicalAttachmentPath(attachmentPath);
            if (!string.IsNullOrWhiteSpace(resolvedAttachment) && File.Exists(resolvedAttachment))
            {
                var attachment = builder.Attachments.Add(resolvedAttachment);
                if (!string.IsNullOrWhiteSpace(attachmentDisplayName))
                {
                    attachment.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment)
                    {
                        FileName = attachmentDisplayName
                    };
                    attachment.ContentType.Name = attachmentDisplayName;
                }
            }

            message.Body = builder.ToMessageBody();
            return message;
        }

        public Task<bool> SendEmailAsync(
            IEnumerable<string> toEmails,
            string subject,
            string htmlBody,
            string? attachmentPath = null,
            CancellationToken cancellationToken = default)
        {
            return SendEmailAsync(toEmails, subject, htmlBody, attachmentPath, null, cancellationToken);
        }

        public async Task<bool> SendEmailAsync(
            IEnumerable<string> toEmails,
            string subject,
            string htmlBody,
            string? attachmentPath,
            string? attachmentDisplayName,
            CancellationToken cancellationToken = default)
        {
            var recipientList = toEmails?.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).Distinct().ToList();
            if (recipientList == null || recipientList.Count == 0)
            {
                _logger.LogWarning("Email sender: danh sách người nhận trống.");
                return false;
            }

            string host = _configuration["Smtp:Host"] ?? string.Empty;
            int port = int.TryParse(_configuration["Smtp:Port"], out int p) ? p : 587;
            string username = _configuration["Smtp:Username"] ?? string.Empty;
            string password = _configuration["Smtp:Password"] ?? string.Empty;
            bool enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out bool ssl) ? ssl : true;

            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("Email sender: Smtp:Host chưa được cấu hình. Bỏ qua gửi email cảnh báo tới: {Recipients}", string.Join(", ", recipientList));
                return false;
            }

            try
            {
                var message = CreateMimeMessage(recipientList, subject, htmlBody, attachmentPath, attachmentDisplayName);

                using var client = new SmtpClient();
                client.Timeout = 15000; // 15s timeout
                var secureSocketOptions = port == 465 ? SecureSocketOptions.SslOnConnect : (enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                await client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    await client.AuthenticateAsync(username, password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Gửi email thành công tới: {Recipients}, Tiêu đề: {Subject}", string.Join(", ", recipientList), subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email qua SMTP: {Message}", ex.Message);
                return false;
            }
        }
    }
}
