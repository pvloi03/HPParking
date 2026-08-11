using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Services.Worker
{
    public class CccdModel
    {
        public string DocumentNumber { get; set; }
        public string FullName { get; set; }
        public string DateOfBirth { get; set; }
        public string Sex { get; set; }
        public string Address { get; set; }
        public string Hometown { get; set; }
        public string IssueDate { get; set; }
        public string ExpiredDate { get; set; }
        public string Mrz { get; set; }
        public string FaceBase64 { get; set; }
    }

    public class CccdListener
    {
        private const string PIPE_NAME = "CccdDataPipeStream";
        private CancellationTokenSource _cts;
        private bool _isListening;

        public event Action<string> OnCccdJsonReceived;

        public void StartListening()
        {
            if (_isListening) return;
            _isListening = true;
            _cts = new CancellationTokenSource();

            // Chạy vòng lặp lắng nghe trên Thread Pool ngầm
            Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Tạo Pipe Server lắng nghe tín hiệu từ Worker (.NET 8)
                    using var server = new NamedPipeServerStream(
                        PIPE_NAME,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);
                    // Chờ Worker kết nối
                    await Task.Factory.FromAsync(server.BeginWaitForConnection, server.EndWaitForConnection, null);

                    // Dùng StreamReader đọc dòng (Read-line) giúp nhận đủ JSON kể cả khi có ảnh Base64 dung lượng lớn
                    using (var reader = new StreamReader(server, Encoding.UTF8))
                    {
                        string jsonResult = await reader.ReadLineAsync();

                        if (!string.IsNullOrWhiteSpace(jsonResult))
                        {
                            OnCccdJsonReceived?.Invoke(jsonResult);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CccdListener Exception]: {ex.Message}");
                }

                // Nghỉ 100ms trước khi tạo Server Instance mới lắng nghe lượt quẹt thẻ tiếp theo
                await Task.Delay(100, token);
            }
        }

        public void Stop()
        {
            _isListening = false;
            _cts?.Cancel();
        }
    }
}