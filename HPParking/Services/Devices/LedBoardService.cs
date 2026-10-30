using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Services.Devices
{
    public class LedBoardService(string ip, int port) : IDisposable
    {
        private TcpClient? _client;
        private readonly object _lock = new();
        private Task _pending = Task.CompletedTask;
        private volatile bool _disposed;

        public static string StripDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'Đ') sb.Append('D');
                    else if (c == 'đ') sb.Append('d');
                    else sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string FormatFrame(string text) => $"*[H1]{StripDiacritics(text)}[!]";

        public void Send(string text)
        {
            if (_disposed) return;

            lock (_lock)
            {
                if (_disposed) return;

                // Chaining tuần tự qua _pending bảo đảm FIFO:
                // Lock chỉ giữ trong microseconds để nối task, hoàn toàn không giữ lock khi I/O mạng
                _pending = EnqueueSendAsync(_pending, text);
            }
        }

        internal Task PendingTask
        {
            get
            {
                lock (_lock)
                {
                    return _pending;
                }
            }
        }

        internal TcpClient? Client
        {
            get
            {
                lock (_lock)
                {
                    return _client;
                }
            }
        }

        private async Task EnqueueSendAsync(Task previous, string text)
        {
            try
            {
                await previous.ConfigureAwait(false);
            }
            catch
            {
                // Bỏ qua lỗi của frame trước để không làm nghẽn/hỏng chuỗi phát của các frame kế tiếp
            }

            if (_disposed) return;
            await DoSendAsync(text).ConfigureAwait(false);
        }

        private async Task DoSendAsync(string text)
        {
            if (_disposed) return;

            bool wasAlreadyConnected = _client is { Connected: true };
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    if (_client is not { Connected: true })
                    {
                        _client?.Dispose();
                        _client = new TcpClient { NoDelay = true };
                        using var connCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                        await _client.ConnectAsync(ip, port, connCts.Token).ConfigureAwait(false);
                    }

                    if (_disposed)
                    {
                        _client?.Dispose();
                        _client = null;
                        return;
                    }

                    byte[] data = Encoding.ASCII.GetBytes(FormatFrame(text));
                    using var writeCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                    await _client.GetStream().WriteAsync(data, writeCts.Token).ConfigureAwait(false);
                    return;
                }
                catch
                {
                    lock (_lock)
                    {
                        _client?.Dispose();
                        _client = null;
                    }

                    // Tự động kết nối lại và gửi lại nếu kết nối tái sử dụng bị ngắt do idle timeout / reset
                    if (_disposed || attempt > 0 || !wasAlreadyConnected)
                    {
                        return;
                    }
                    wasAlreadyConnected = false;
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
                _client?.Dispose();
                _client = null;
            }
        }
    }
}
