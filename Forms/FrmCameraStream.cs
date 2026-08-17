using HPParking.Services.CCCDReader;
using System;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmCameraStream : Form
    {
        private readonly CccdReaderManager _readerManager;
        private readonly FrmRegisterClient _frmRegisterClient;
        public PhotoCapturedDto CapturedPhoto { get; private set; }

        public FrmCameraStream(CccdReaderManager readerManager)
        {
            InitializeComponent();
            _readerManager = readerManager;

            // Đăng ký nhận sự kiện Stream và Chụp ảnh
            _readerManager.VideoFrameReceived += OnVideoFrameReceived;
            _readerManager.PhotoCaptured += OnPhotoCaptured;
        }

        private void OnVideoFrameReceived(byte[] frameBytes)
        {
            if (frameBytes == null || frameBytes.Length == 0) return;
            if (this.IsDisposed || this.Disposing || !this.IsHandleCreated) return;
            if (pbStreamCapture == null || pbStreamCapture.IsDisposed) return;

            // Đưa dữ liệu frame lên UI thread
            this.BeginInvoke(new Action(() =>
            {
                if (this.IsDisposed || this.Disposing || pbStreamCapture == null || pbStreamCapture.IsDisposed)
                    return;
                _frmRegisterClient.SetPictureBoxImage(pbStreamCapture, frameBytes);
            }));
        }

        private void OnPhotoCaptured(PhotoCapturedDto photo)
        {
            if (IsDisposed || photo == null) return;

            CapturedPhoto = photo;

            // Chụp thành công thì tự động đóng Dialog trả kết quả về
            BeginInvoke(new Action(() =>
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }));
        }

        private async void btnCapture_Click(object sender, EventArgs e)
        {
            try
            {
                await _readerManager.RequestCaptureAsync();
                btnCapture.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnCapture.Enabled = true;
            }
        }

        private void FrmCameraStream_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Hủy đăng ký sự kiện để giải phóng tài nguyên
            _readerManager.VideoFrameReceived -= OnVideoFrameReceived;
            _readerManager.PhotoCaptured -= OnPhotoCaptured;
        }
    }
}