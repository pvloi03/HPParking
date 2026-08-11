using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace HPParking.Services.Worker
{
    public class CccdWorkerManager
    {
        private Process _workerProcess;
        private CccdListener _cccdListener;

        public event Action<CccdModel> OnCccdDataReceived;
        public event Action<string> OnError;

        public void Start()
        {
            try
            {
                string workerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CccdReaderWorker", "CccdReaderWorker.exe");

                if (!File.Exists(workerPath))
                {
                    OnError?.Invoke($"Không tìm thấy file Worker tại: {workerPath}");
                    return;
                }

                Process[] existingProcesses = Process.GetProcessesByName("CccdReaderWorker");
                if (existingProcesses.Length > 0)
                {
                    _workerProcess = existingProcesses[0];
                }
                else
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = workerPath,
                        WorkingDirectory = Path.GetDirectoryName(workerPath),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    _workerProcess = Process.Start(startInfo);
                }

                _cccdListener = new CccdListener();
                _cccdListener.OnCccdJsonReceived += CccdListener_OnCccdJsonReceived;
                _cccdListener.StartListening();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Khởi chạy CccdReaderWorker thất bại: {ex.Message}");
            }
        }

        private void CccdListener_OnCccdJsonReceived(string jsonResult)
        {
            try
            {
                var cccdData = JsonConvert.DeserializeObject<CccdModel>(jsonResult);
                if (cccdData != null)
                {
                    OnCccdDataReceived?.Invoke(cccdData);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Lỗi giải mã dữ liệu CCCD: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                _cccdListener?.Stop();
                if (_workerProcess != null && !_workerProcess.HasExited)
                {
                    _workerProcess.Kill();
                    _workerProcess.Dispose();
                    _workerProcess = null;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Lỗi khi tắt Worker: {ex.Message}");
            }
        }
    }
}