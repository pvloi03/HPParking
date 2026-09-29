using HPParking.Api.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task<bool> SendEmailAsync(
            IEnumerable<string> toEmails, 
            string subject, 
            string htmlBody, 
            string? attachmentPath = null, 
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
            string fromEmail = _configuration["Smtp:FromEmail"] ?? "no-reply@hpparking.local";
            string fromName = _configuration["Smtp:FromName"] ?? "HPParking Alert System";
            bool enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out bool ssl) ? ssl : true;

            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("Email sender: Smtp:Host chưa được cấu hình. Bỏ qua gửi email cảnh báo tới: {Recipients}", string.Join(", ", recipientList));
                return false;
            }

            try
            {
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

                if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath))
                {
                    builder.Attachments.Add(attachmentPath);
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                // Với port 465 dùng SslOnConnect, port 587 dùng StartTls
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
