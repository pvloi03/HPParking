using HPParking.Services.HN212;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmCameraCapture : Form
    {
        private readonly IHn212Client? _hn212Client;
        private bool _isCaptured;
        private int _isProcessingFrame;

        public FrmCameraCapture()
        {
            InitializeComponent();
        }

        public FrmCameraCapture(IHn212Client hn212Client) : this()
        {
            _hn212Client = hn212Client;
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private async void FrmCameraCapture_Load(object? sender, EventArgs e)
        {
            if (DesignMode || _hn212Client == null) return;

            _isCaptured = false;

            _hn212Client.VideoFrameReceived += OnVideoFrameReceived;
            _hn212Client.FaceCaptured += OnFaceCaptured;
            _hn212Client.FaceCompared += OnFaceCompared;

            try
            {
                lblStatus.Text = "Đang bật camera nhận diện...";
                bool ok = await _hn212Client.StartCaptureFaceAsync();
                if (ok)
                {
                    lblStatus.Text = "Camera đang hoạt động, xin giữ yên vị trí...";
                    lblStatus.ForeColor = Color.SeaGreen;
                }
                else
                {
                    lblStatus.Text = "Không thể khởi động camera đầu đọc";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi camera: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void OnVideoFrameReceived(byte[] frame)
        {
            if (IsDisposed || !IsHandleCreated || frame == null || frame.Length == 0) return;

            // Bỏ qua frame nếu frame trước vẫn đang được giải mã hoặc vẽ (tránh nghẽn hàng đợi UI gây lag/trễ)
            if (System.Threading.Interlocked.CompareExchange(ref _isProcessingFrame, 1, 0) != 0)
            {
                return;
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                Bitmap? bmp = null;
                try
                {
                    using var ms = new MemoryStream(frame);
                    using var temp = Image.FromStream(ms);
                    bmp = new Bitmap(temp);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[VideoFrame Decode Error]: {ex.Message}");
                    System.Threading.Interlocked.Exchange(ref _isProcessingFrame, 0);
                    return;
                }

                if (IsDisposed || !IsHandleCreated)
                {
                    bmp?.Dispose();
                    System.Threading.Interlocked.Exchange(ref _isProcessingFrame, 0);
                    return;
                }

                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (IsDisposed || pbLiveStream.IsDisposed)
                            {
                                bmp?.Dispose();
                                return;
                            }

                            var oldImg = pbLiveStream.Image;
                            pbLiveStream.Image = bmp;
                            oldImg?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[VideoFrame Render Error]: {ex.Message}");
                            bmp?.Dispose();
                        }
                        finally
                        {
                            System.Threading.Interlocked.Exchange(ref _isProcessingFrame, 0);
                        }
                    }));
                }
                catch
                {
                    bmp?.Dispose();
                    System.Threading.Interlocked.Exchange(ref _isProcessingFrame, 0);
                }
            });
        }

        private void OnFaceCaptured(string base64)
        {
            if (IsDisposed) return;
            _isCaptured = true;

            BeginInvoke(new Action(() =>
            {
                DialogResult = DialogResult.OK;
                Close();
            }));
        }

        private void OnFaceCompared(FaceCompareResultDto result)
        {
            if (IsDisposed) return;
            _isCaptured = true;

            BeginInvoke(new Action(() =>
            {
                DialogResult = DialogResult.OK;
                Close();
            }));
        }

        private void FrmCameraCapture_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_hn212Client != null)
            {
                _hn212Client.VideoFrameReceived -= OnVideoFrameReceived;
                _hn212Client.FaceCaptured -= OnFaceCaptured;
                _hn212Client.FaceCompared -= OnFaceCompared;

                if (!_isCaptured)
                {
                    // Hủy chụp nếu người dùng bấm nút Hủy hoặc đóng Form
                    _ = _hn212Client.CancelCaptureFaceAsync();
                }
            }

            var oldImg = pbLiveStream.Image;
            pbLiveStream.Image = null;
            oldImg?.Dispose();
        }
    }
}