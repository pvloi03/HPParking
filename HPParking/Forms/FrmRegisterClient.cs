using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Helper;
using HPParking.Interfaces;
using HPParking.Services.FaceId;
using HPParking.Services.HN212;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmRegisterClient : Form
    {
        private readonly IHn212Client _hn212Client;
        private readonly IRepository<Client> _clientRepository;
        private readonly IRepository<Lane> _laneRepository;
        private readonly IRepository<Device> _deviceRepository;
        private readonly IRepository<Vehicle> _vehicleRepository;
        private readonly List<IFaceIdApiService> _faceIdServices = [];
        private Client? _clientExist;
        private Vehicle? _vehicleExist;
        private string? _pathAvatar;
        private byte[]? _photoBytes;
        private byte[]? _cccdPhotoBytes;
        private bool _isNewPhotoCaptured;

        public FrmRegisterClient(
            IHn212Client hn212Client,
            IRepository<Client> clientRepository,
            IRepository<Lane> laneRepository,
            IRepository<Device> deviceRepository,
            IRepository<Vehicle> vehicleRepository)
        {
            InitializeComponent();
            _hn212Client = hn212Client;

            // Đăng ký nhận sự kiện từ HN212Reader
            _hn212Client.CardScanned += OnCardScanned;
            _hn212Client.FaceCaptured += OnFaceCaptured;
            _hn212Client.FaceCompared += OnFaceCompared;
            _hn212Client.StatusUpdated += OnStatusUpdated;
            _hn212Client.CardStatusChanged += OnCardStatusChanged;
            _hn212Client.ConnectionStateChanged += OnConnectionStateChanged;
            ckUploadFaceid.CheckedChanged += ckUploadFaceid_CheckedChanged;

            _clientRepository = clientRepository;
            _laneRepository = laneRepository;
            _deviceRepository = deviceRepository;
            _vehicleRepository = vehicleRepository;
        }

        private async void FrmRegisterClient_Load(object sender, EventArgs e)
        {
            using var waitScope = new WaitCursorScope(this);

            // Hiển thị ngay trạng thái hiện tại của đầu đọc khi mở form
            if (_hn212Client.CurrentStatus != null)
            {
                OnStatusUpdated(_hn212Client.CurrentStatus);
            }
            else if (!_hn212Client.IsConnected)
            {
                UpdateStatus("❌ MẤT KẾT NỐI: Chưa kết nối tới Service HN212Reader - Vui lòng kiểm tra dịch vụ.", StatusType.Error);
            }
            else
            {
                UpdateStatus("⏳ KHỞI ĐỘNG: Đang kiểm tra kết nối đầu đọc thẻ HN212...", StatusType.Info);
            }

            // Đồng thời truy vấn trạng thái mới nhất từ service
            try
            {
                var latestStatus = await _hn212Client.GetLatestStatusAsync();
                if (latestStatus != null)
                {
                    OnStatusUpdated(latestStatus);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FrmRegisterClient GetLatestStatus Warning]: {ex.Message}");
            }

            string basePath = StorageConfigHelper.GetPathImage();
            _pathAvatar = Path.Combine(basePath, "Avatar");

            _faceIdServices.Clear();
            _faceIdServices.AddRange(await ResolveUniqueLaneFaceIdServicesAsync());

            cbVehicleType.Items.Clear();
            cbVehicleType.Items.Add("Ô tô");
            cbVehicleType.Items.Add("Xe máy");
            cbVehicleType.SelectedIndex = 0;
        }

        public enum StatusType
        {
            Ready,
            Success,
            Info,
            Warning,
            Error
        }

        public void UpdateStatus(string msg, StatusType type = StatusType.Info)
        {
            if (lblStatus == null || lblStatus.IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateStatus(msg, type)));
                return;
            }

            lblStatus.Text = msg;

            switch (type)
            {
                case StatusType.Ready:
                case StatusType.Success:
                    lblStatus.BackColor = Color.FromArgb(235, 247, 238);
                    lblStatus.ForeColor = Color.FromArgb(15, 115, 60);
                    break;
                case StatusType.Info:
                    lblStatus.BackColor = Color.FromArgb(235, 243, 254);
                    lblStatus.ForeColor = Color.FromArgb(13, 80, 180);
                    break;
                case StatusType.Warning:
                    lblStatus.BackColor = Color.FromArgb(255, 248, 230);
                    lblStatus.ForeColor = Color.FromArgb(180, 95, 0);
                    break;
                case StatusType.Error:
                    lblStatus.BackColor = Color.FromArgb(254, 237, 237);
                    lblStatus.ForeColor = Color.FromArgb(185, 25, 25);
                    break;
            }
        }

        public void UpdateStatus(string msg, Color color)
        {
            StatusType type = StatusType.Info;
            if (color == Color.Red || color == Color.Crimson)
                type = StatusType.Error;
            else if (color == Color.SeaGreen || color == Color.Green)
                type = StatusType.Success;
            else if (color == Color.OrangeRed || color == Color.Orange || color == Color.DarkOrange)
                type = StatusType.Warning;

            UpdateStatus(msg, type);
        }

        private void OnCardScanned(CardDataDto card)
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                DisplayCardData(card);
            }));
        }

        private void OnStatusUpdated(ReaderStatusDto status)
        {
            if (status == null || IsDisposed) return;

            BeginInvoke(new Action(() =>
            {
                if (!status.IsReaderConnected)
                {
                    UpdateStatus("❌ ĐẦU ĐỌC CHƯA KẾT NỐI: Vui lòng kiểm tra cáp USB đầu đọc thẻ HN212.", StatusType.Error);
                }
                else if (status.CardStatus == "Error")
                {
                    UpdateStatus($"❌ LỖI ĐỌC THẺ: {status.Message}. Vui lòng nhấc thẻ ra và đặt lại ngay ngắn.", StatusType.Error);
                }
                else if (status.CardStatus == "Reading")
                {
                    UpdateStatus("⏳ ĐANG ĐỌC THẺ: Đã nhận diện thẻ CCCD, vui lòng giữ yên thẻ...", StatusType.Info);
                }
                else if (status.CardStatus == "ReadSuccess")
                {
                    UpdateStatus("✔ ĐỌC THẺ THÀNH CÔNG: Đã đọc dữ liệu chip CCCD, đang nạp thông tin...", StatusType.Success);
                }
                else if (status.CardStatus == "Present")
                {
                    UpdateStatus("⏳ ĐÃ ĐẶT THẺ: Đã nhận diện thẻ CCCD, đang đọc dữ liệu chip...", StatusType.Info);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(txtIdCode.Text))
                    {
                        if (_photoBytes == null || _photoBytes.Length == 0)
                            UpdateStatus("👉 BƯỚC TIẾP THEO: Vui lòng bấm 'Chụp ảnh' để nhận diện khuôn mặt FaceID.", StatusType.Warning);
                        else
                            UpdateStatus("👉 BƯỚC TIẾP THEO: Kiểm tra thông tin và bấm 'Lưu' để hoàn tất.", StatusType.Success);
                    }
                    else
                    {
                        UpdateStatus("🟢 ĐẦU ĐỌC SẴN SÀNG: Vui lòng đặt thẻ CCCD vào đầu đọc để bắt đầu.", StatusType.Ready);
                    }
                }
            }));
        }

        private void OnCardStatusChanged(string status, string message)
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                switch (status)
                {
                    case "Reading":
                        UpdateStatus(string.IsNullOrWhiteSpace(message) ? "⏳ ĐANG ĐỌC THẺ: Đang đọc chip CCCD, vui lòng giữ yên thẻ..." : message, StatusType.Info);
                        break;
                    case "ReadSuccess":
                        UpdateStatus("✔ ĐỌC THẺ THÀNH CÔNG: Đã đọc dữ liệu chip CCCD, đang nạp thông tin...", StatusType.Success);
                        break;
                    case "Present":
                        UpdateStatus(string.IsNullOrWhiteSpace(message) ? "⏳ ĐÃ ĐẶT THẺ: Đã nhận diện thẻ CCCD, đang đọc..." : message, StatusType.Info);
                        break;
                    case "Error":
                        UpdateStatus($"❌ LỖI ĐỌC THẺ: {(string.IsNullOrWhiteSpace(message) ? "Không thể đọc chip CCCD" : message)}. Vui lòng đặt lại thẻ.", StatusType.Error);
                        break;
                    case "Absent":
                    default:
                        if (!string.IsNullOrWhiteSpace(txtIdCode.Text))
                        {
                            if (_photoBytes == null || _photoBytes.Length == 0)
                                UpdateStatus("👉 Vui lòng bấm 'Chụp ảnh' để nhận diện khuôn mặt FaceID.", StatusType.Warning);
                            else
                                UpdateStatus("👉 Kiểm tra thông tin và bấm 'Lưu' để hoàn tất.", StatusType.Success);
                        }
                        else
                        {
                            UpdateStatus("🟢 ĐẦU ĐỌC SẴN SÀNG: Vui lòng đặt thẻ CCCD vào đầu đọc để bắt đầu.", StatusType.Ready);
                        }
                        break;
                }
            }));
        }

        private void OnConnectionStateChanged(string msg, bool isConnected)
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                UpdateStatus(isConnected ? $"🟢 KẾT NỐI: {msg}" : $"❌ MẤT KẾT NỐI: {msg}", isConnected ? StatusType.Success : StatusType.Error);
            }));
        }

        public async void DisplayCardData(CardDataDto card)
        {
            if (card == null || string.IsNullOrEmpty(card.DocumentNumber)) return;

            using var waitScope = new WaitCursorScope(this);
            _clientExist = await _clientRepository.FindOneAsync(x => x.Code == card.DocumentNumber);

            txtIdCode.Text = card.DocumentNumber;
            txtName.Text = card.FullName;
            txtAddress.Text = card.Address;
            _cccdPhotoBytes = card.ChipFaceBytes;
            SetPictureBoxImage(pbAvatar, card.ChipFaceBytes);

            if (ckUploadFaceid.Checked && _cccdPhotoBytes != null && _cccdPhotoBytes.Length > 0)
            {
                SetPictureBoxImage(pbCapturedFace, _cccdPhotoBytes);
            }

            if (card.Sex?.ToLower() == "nam")
            {
                rbMale.Checked = true;
            }
            else
            {
                rbFeMale.Checked = true;
            }

            if (DateTime.TryParseExact(card.DateOfBirth, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime dob))
            {
                dtpDateOfBirth.Value = dob;
            }

            if (_clientExist != null)
            {
                txtPhoneNumber.Text = _clientExist.PhoneNumber;
                var clientVehicles = await _vehicleRepository.FindAsync(v => v.OwnerClientId == _clientExist.Id && !v.IsDeleted);
                _vehicleExist = clientVehicles?.FirstOrDefault();
                txtPlate.Text = _vehicleExist?.PlateNumber ?? "";
                cbVehicleType.SelectedIndex = (_vehicleExist?.Type == VehicleType.Motorbike) ? 1 : 0;
                txtDescription.Text = _clientExist.Note ?? "";
                chkEnable.Checked = _clientExist.Expired?.Enable ?? false;
                dtpTimeIn.Value = _clientExist.Expired?.StartDay ?? DateTime.Now;
                dtpTimeOut.Value = _clientExist.Expired?.EndDay ?? DateTime.Now;
                bool isRestricted = !chkEnable.Checked;
                dtpTimeIn.Enabled = isRestricted;
                dtpTimeOut.Enabled = isRestricted;
                lbTimeIn.Enabled = isRestricted;
                lbTimeOut.Enabled = isRestricted;

                if (!string.IsNullOrEmpty(_clientExist.Avatar) && File.Exists(_clientExist.Avatar))
                {
                    try
                    {
                        // Dùng ReadAllBytes để tránh khóa (lock) file ảnh trên đĩa
                        byte[] avatarBytes = await Task.Run(() => File.ReadAllBytes(_clientExist.Avatar));
                        if (!ckUploadFaceid.Checked)
                        {
                            SetPictureBoxImage(pbCapturedFace, avatarBytes);
                        }
                        _photoBytes = avatarBytes;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Lỗi đọc ảnh Avatar]: {ex.Message}");
                    }
                }

                _isNewPhotoCaptured = false;
                btnSave.Text = "Cập nhật";
                UpdateCameraCaptureButtonState();
                UpdateStatus($"✔ KHÁCH HÀNG ĐÃ TỒN TẠI: {card.FullName} (CCCD: {card.DocumentNumber}).", StatusType.Success);
                waitScope.Dispose();
                MessageBox.Show("Khách hàng đã tồn tại trong hệ thống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _vehicleExist = null;
            txtPlate.Text = "";
            cbVehicleType.SelectedIndex = 0;
            chkEnable.Checked = false;
            dtpTimeIn.Enabled = true;
            dtpTimeOut.Enabled = true;
            lbTimeIn.Enabled = true;
            lbTimeOut.Enabled = true;
            btnSave.Text = "Lưu";
            UpdateCameraCaptureButtonState();
            _isNewPhotoCaptured = false;
            dtpTimeOut.Value = dtpTimeOut.Value.AddDays(1);
            UpdateStatus($"✔ ĐÃ ĐỌC CCCD: {card.FullName} (CCCD: {card.DocumentNumber}).", StatusType.Info);
        }

        private void OnFaceCaptured(string base64)
        {
            if (string.IsNullOrEmpty(base64) || IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                try
                {
                    _photoBytes = Convert.FromBase64String(base64);
                    _isNewPhotoCaptured = true;
                    if (ckUploadFaceid.Checked)
                    {
                        ckUploadFaceid.Checked = false;
                    }
                    SetPictureBoxImage(pbCapturedFace, _photoBytes);
                    UpdateCameraCaptureButtonState();
                    UpdateStatus("✔ ĐÃ NHẬN DIỆN KHUÔN MẶT: Ảnh FaceID đã sẵn sàng. Vui lòng kiểm tra thông tin và bấm 'Lưu'.", StatusType.Success);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OnFaceCaptured Error]: {ex.Message}");
                    UpdateStatus($"❌ Lỗi xử lý ảnh khuôn mặt: {ex.Message}", StatusType.Error);
                }
            }));
        }

        private void OnFaceCompared(FaceCompareResultDto result)
        {
            if (result == null || IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                if (result.CapturedFaceBytes != null && result.CapturedFaceBytes.Length > 0)
                {
                    _photoBytes = result.CapturedFaceBytes;
                    _isNewPhotoCaptured = true;
                    if (ckUploadFaceid.Checked)
                    {
                        ckUploadFaceid.Checked = false;
                    }
                    SetPictureBoxImage(pbCapturedFace, _photoBytes);
                }
                if (result.IsMatch)
                {
                    UpdateStatus($"✔ KHUÔN MẶT TRÙNG KHỚP: {result.Message} - Vui lòng bấm 'Lưu' để hoàn tất.", StatusType.Success);
                }
                else
                {
                    UpdateStatus($"⚠️ KHÔNG TRÙNG KHỚP: {result.Message} - Vui lòng chụp lại ảnh rõ nét.", StatusType.Warning);
                }
                UpdateCameraCaptureButtonState();
            }));
        }

        private void btnOpenCamera_Click(object sender, EventArgs e)
        {
            if (ckUploadFaceid.Checked)
            {
                UpdateStatus("ℹ️ Bạn đang chọn dùng ảnh CCCD để đăng ký FaceID. Vui lòng bỏ chọn ô 'Cho phép dùng ảnh CCCD' nếu muốn chụp ảnh từ Camera.", StatusType.Info);
                return;
            }

            try
            {
                UpdateStatus("📷 ĐANG MỞ CAMERA: Vui lòng hướng nhìn thẳng vào camera nhận diện...", StatusType.Info);
                using var camForm = new FrmCameraCapture(_hn212Client);
                var result = camForm.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    _isNewPhotoCaptured = true;
                    UpdateStatus("✔ ĐÃ CHỤP ẢNH THÀNH CÔNG: Khuôn mặt đã sẵn sàng. Vui lòng kiểm tra thông tin và bấm 'Lưu'.", StatusType.Success);
                }
                else
                {
                    UpdateStatus("⚠️ ĐÃ HỦY CHỤP ẢNH: Chưa lưu ảnh mới. Bấm 'Chụp ảnh' lại nếu cần nhận diện FaceID.", StatusType.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Lỗi mở camera: {ex.Message}", StatusType.Error);
            }
        }

        public static void SetPictureBoxImage(PictureBox pictureBox, byte[]? byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                var old = pictureBox.Image;
                pictureBox.Image = null;
                old?.Dispose();
                return;
            }

            try
            {
                using var ms = new MemoryStream(byteArray);
                using var tempImg = Image.FromStream(ms);
                var oldImg = pictureBox.Image;
                pictureBox.Image = new Bitmap(tempImg);
                oldImg?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FrmRegisterClient SafeSetImageBytes Error]: {ex.Message}");
            }
        }

        private void UpdateCameraCaptureButtonState()
        {
            if (btnOpenCamera != null && !btnOpenCamera.IsDisposed)
            {
                btnOpenCamera.Enabled = !ckUploadFaceid.Checked;
            }
        }

        private void ckUploadFaceid_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateCameraCaptureButtonState();

            if (ckUploadFaceid.Checked)
            {
                if (_cccdPhotoBytes != null && _cccdPhotoBytes.Length > 0)
                {
                    SetPictureBoxImage(pbCapturedFace, _cccdPhotoBytes);
                    UpdateStatus("ℹ️ ĐÃ CHỌN ẢNH CCCD: Sẽ sử dụng ảnh thẻ CCCD gắn chip để đăng ký FaceID.", StatusType.Info);
                }
                else
                {
                    UpdateStatus("ℹ️ ĐÃ BẬT DÙNG ẢNH CCCD: Vui lòng đặt thẻ CCCD vào đầu đọc để nạp ảnh chip CCCD.", StatusType.Info);
                }
            }
            else
            {
                SetPictureBoxImage(pbCapturedFace, _photoBytes);
                UpdateStatus("ℹ️ ĐÃ BỎ CHỌN ẢNH CCCD: Mở khóa nút chụp ảnh từ Camera để đăng ký FaceID.", StatusType.Info);
            }
        }

        public byte[]? GetTargetFacePhotoBytes()
        {
            return (ckUploadFaceid.Checked && _cccdPhotoBytes != null && _cccdPhotoBytes.Length > 0)
                ? _cccdPhotoBytes
                : _photoBytes;
        }

        public void SetCccdPhotoBytesForTest(byte[]? bytes) => _cccdPhotoBytes = bytes;
        public void SetCapturedPhotoBytesForTest(byte[]? bytes) => _photoBytes = bytes;
        public CheckBox CkUploadFaceId => ckUploadFaceid;
        public Button BtnOpenCamera => btnOpenCamera;

        /// <summary>
        /// Lấy danh sách các thiết bị FaceID được gán vào các làn xe đang hoạt động.
        /// Nếu có nhiều hơn một FaceID cùng địa chỉ IP thì chỉ thao tác với FaceID đầu tiên.
        /// </summary>
        public async Task<List<IFaceIdApiService>> ResolveUniqueLaneFaceIdServicesAsync()
        {
            try
            {
                // 1. Lấy tất cả các làn xe đang hoạt động trong hệ thống
                var lanes = await _laneRepository.FindAsync(l => l.IsActive && !l.IsDeleted);
                var faceDeviceIds = lanes
                    .Where(l => !string.IsNullOrWhiteSpace(l.FaceDeviceId))
                    .Select(l => l.FaceDeviceId!)
                    .Distinct()
                    .ToList();

                if (faceDeviceIds.Count == 0) return [];

                // 2. Lấy thông tin các thiết bị FaceID được gán vào làn xe
                var devices = await _deviceRepository.FindAsync(d => faceDeviceIds.Contains(d.Id) && d.IsActive && !d.IsDeleted);
                var deviceMap = devices
                    .GroupBy(d => d.Id)
                    .ToDictionary(g => g.Key, g => g.First());

                // 3. Duyệt theo thứ tự các làn xe để lấy FaceID.
                // Nếu có nhiều hơn một FaceID cùng địa chỉ IP thì chỉ thao tác với FaceID đầu tiên.
                var uniqueConfigs = new List<FaceIdConfig>();
                var seenIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var lane in lanes)
                {
                    if (string.IsNullOrWhiteSpace(lane.FaceDeviceId)) continue;
                    if (!deviceMap.TryGetValue(lane.FaceDeviceId, out var device)) continue;
                    if (string.IsNullOrWhiteSpace(device.IpAddress)) continue;

                    string ip = device.IpAddress.Trim();
                    if (seenIps.Add(ip))
                    {
                        uniqueConfigs.Add(new FaceIdConfig
                        {
                            Ip = ip,
                            Username = device.UserName?.Trim() ?? "",
                            Password = device.Password?.Trim() ?? ""
                        });
                    }
                }

                return uniqueConfigs.Select(config => (IFaceIdApiService)new FaceIdApiService(config)).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ResolveUniqueLaneFaceIdServicesAsync Error]: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// Chuẩn hóa thông báo lỗi kỹ thuật từ thiết bị FaceID hoặc Database thành thông báo dễ hiểu cho người dùng vận hành.
        /// </summary>
        private static string FormatUserFriendlyError(string rawError, string deviceIp = "")
        {
            return FaceIdErrorFormatter.Format(rawError, deviceIp);
        }

        private static async Task<(bool Success, string DeviceIp, string? ErrorMsg)> PushToSingleDeviceAsync(
            IFaceIdApiService apiService,
            string idCode,
            string name,
            bool isMale,
            string phone,
            byte[] photoBytes)
        {
            string deviceIp = apiService.Ip;

            // 1. Add User
            var (userOk, userErr) = await apiService.AddUserAsync(idCode, name, isMale);
            if (!userOk) return (false, deviceIp, userErr);

            // 2. Add Card
            var (cardOk, cardErr) = await apiService.AddCardAsync(idCode, phone);
            if (!cardOk)
            {
                await apiService.RollbackUserAsync(idCode);
                return (false, deviceIp, cardErr);
            }

            // 3. Add Face Image
            var (faceOk, faceErr) = await apiService.AddFaceImageAsync(idCode, photoBytes);
            if (!faceOk)
            {
                await apiService.RollbackUserAsync(idCode);
                return (false, deviceIp, faceErr);
            }

            return (true, deviceIp, null);
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            // 1. VALIDATE DỮ LIỆU CƠ BẢN
            string idCode = txtIdCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(idCode))
            {
                UpdateStatus("⚠️ THIẾU THÔNG TIN: Vui lòng quét thẻ CCCD hoặc nhập số CCCD trước khi lưu.", StatusType.Warning);
                MessageBox.Show("Vui lòng quét thẻ CCCD hoặc nhập số CCCD trước khi lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtIdCode.Focus();
                return;
            }

            string newPhone = txtPhoneNumber.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(newPhone))
            {
                UpdateStatus("⚠️ THIẾU THÔNG TIN: Vui lòng nhập số điện thoại.", StatusType.Warning);
                MessageBox.Show("Vui lòng nhập số điện thoại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhoneNumber.Focus();
                return;
            }

            string rawPlate = txtPlate.Text?.Trim() ?? "";
            string normalizedPlate = rawPlate
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(".", "")
                .ToUpperInvariant();

            VehicleType selectedVehicleType = cbVehicleType.SelectedIndex == 1 ? VehicleType.Motorbike : VehicleType.Car;

            btnSave.Enabled = false;
            using var waitScope = new WaitCursorScope(this);

            // Kiểm tra nếu người dùng chọn dùng ảnh CCCD nhưng chưa có ảnh từ chip
            if (ckUploadFaceid.Checked && (_cccdPhotoBytes == null || _cccdPhotoBytes.Length == 0))
            {
                waitScope.Dispose();
                btnSave.Enabled = true;
                UpdateStatus("⚠️ THIẾU ẢNH CCCD: Bạn đang chọn dùng ảnh CCCD nhưng chưa đọc được ảnh từ chip thẻ.", StatusType.Warning);
                MessageBox.Show("Đang chọn dùng ảnh CCCD để đăng ký FaceID nhưng chưa đọc được ảnh từ chip thẻ CCCD.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            byte[]? uploadPhotoBytes = (ckUploadFaceid.Checked && _cccdPhotoBytes != null && _cccdPhotoBytes.Length > 0)
                ? _cccdPhotoBytes
                : _photoBytes;

            // Cập nhật lại danh sách FaceID từ các làn xe (nếu trùng IP chỉ thao tác với FaceID đầu tiên)
            _faceIdServices.Clear();
            _faceIdServices.AddRange(await ResolveUniqueLaneFaceIdServicesAsync());

            // =========================================================================
            // TRƯỜNG HỢP 1: CẬP NHẬT KHÁCH HÀNG ĐÃ TỒN TẠI (_clientExist != null)
            // =========================================================================
            if (_clientExist != null)
            {
                UpdateStatus("⏳ ĐANG CẬP NHẬT: Đang đồng bộ dữ liệu FaceID và lưu vào hệ thống...", StatusType.Info);
                string oldCardNo = _clientExist.PhoneNumber ?? "";
                bool cardChanged = !string.Equals(oldCardNo, newPhone, StringComparison.OrdinalIgnoreCase);
                bool photoChanged = (ckUploadFaceid.Checked && _cccdPhotoBytes != null && _cccdPhotoBytes.Length > 0) ||
                                    (_isNewPhotoCaptured && _photoBytes != null && _photoBytes.Length > 0);

                try
                {
                    List<IFaceIdApiService> cardUpdatedDevices = [];
                    List<(string DeviceIp, string ErrorMsg)> failedDevices = [];

                    // 1. CẬP NHẬT MÃ THẺ TRÊN THIẾT BỊ FACEID (nếu số điện thoại thay đổi)
                    if (cardChanged && _faceIdServices.Count > 0)
                    {
                        foreach (var service in _faceIdServices)
                        {
                            // a. Xóa mã thẻ cũ
                            if (!string.IsNullOrEmpty(oldCardNo))
                            {
                                var (delOk, delErr) = await service.DeleteCardAsync(oldCardNo);
                                if (!delOk)
                                {
                                    Debug.WriteLine($"[DeleteCard Warning on {service.Ip}]: {delErr}");
                                }
                            }

                            // b. Thêm mã thẻ mới
                            var (addOk, addErr) = await service.AddCardAsync(idCode, newPhone);
                            if (!addOk)
                            {
                                // Khôi phục lại thẻ cũ trên thiết bị này nếu đã xóa
                                if (!string.IsNullOrEmpty(oldCardNo))
                                {
                                    await service.AddCardAsync(idCode, oldCardNo);
                                }

                                failedDevices.Add((service.Ip, FormatUserFriendlyError(addErr, service.Ip)));
                            }
                            else
                            {
                                cardUpdatedDevices.Add(service);
                            }
                        }

                        // Nếu TẤT CẢ các thiết bị đều thất bại khi đổi thẻ -> Dừng khẩn cấp, không lưu DB
                        if (_faceIdServices.Count > 0 && cardUpdatedDevices.Count == 0)
                        {
                            string errSummary = string.Join("\n", failedDevices.Select(f => f.ErrorMsg));
                            waitScope.Dispose();
                            UpdateStatus("❌ CẬP NHẬT THẤT BẠI: Không thể cập nhật mã thẻ trên thiết bị FaceID.", StatusType.Error);
                            MessageBox.Show(
                                $"Cập nhật mã thẻ thất bại trên toàn bộ thiết bị FaceID:\n{errSummary}\n\nThông tin chưa được lưu vào hệ thống.",
                                "Lỗi Cập Nhật Thẻ",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }

                    // 2. CẬP NHẬT ẢNH KHUÔN MẶT TRÊN THIẾT BỊ FACEID (nếu có chụp ảnh mới)
                    List<IFaceIdApiService> photoUpdatedDevices = [];
                    if (photoChanged && _faceIdServices.Count > 0)
                    {
                        foreach (var service in _faceIdServices)
                        {
                            var (updOk, updErr) = await service.UpdateFaceImageAsync(idCode, uploadPhotoBytes!);
                            if (!updOk)
                            {
                                failedDevices.Add((service.Ip, FormatUserFriendlyError(updErr, service.Ip)));
                            }
                            else
                            {
                                photoUpdatedDevices.Add(service);
                            }
                        }

                        // Nếu TẤT CẢ các thiết bị đều thất bại khi cập nhật ảnh (ảnh mờ/hỏng) -> Dừng khẩn cấp
                        if (_faceIdServices.Count > 0 && photoUpdatedDevices.Count == 0)
                        {
                            // Rollback thẻ nếu vừa đổi
                            if (cardChanged)
                            {
                                foreach (var dev in cardUpdatedDevices)
                                {
                                    await dev.DeleteCardAsync(newPhone);
                                    if (!string.IsNullOrEmpty(oldCardNo))
                                    {
                                        await dev.AddCardAsync(idCode, oldCardNo);
                                    }
                                }
                            }

                            string errSummary = string.Join("\n", failedDevices.Select(f => f.ErrorMsg));
                            waitScope.Dispose();
                            UpdateStatus("❌ CẬP NHẬT THẤT BẠI: Toàn bộ thiết bị FaceID từ chối ảnh mới.", StatusType.Error);
                            MessageBox.Show(
                                $"Cập nhật khuôn mặt thất bại trên toàn bộ thiết bị FaceID:\n{errSummary}\n\nThông tin chưa được lưu vào hệ thống. Vui lòng chụp lại ảnh rõ nét.",
                                "Lỗi Cập Nhật Khuôn Mặt",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }

                    // 3. CẬP NHẬT DATABASE
                    string? previousAvatarPath = _clientExist.Avatar;
                    string? newAvatarPath = null;

                    if (photoChanged && !string.IsNullOrEmpty(_pathAvatar))
                    {
                        Directory.CreateDirectory(_pathAvatar);
                        string fileName = $"{idCode}.jpg";
                        newAvatarPath = Path.Combine(_pathAvatar, fileName);
                        _clientExist.Avatar = newAvatarPath;
                    }

                    _clientExist.PhoneNumber = newPhone;
                    _clientExist.Note = txtDescription.Text;
                    _clientExist.IsActive = true;
                    _clientExist.Expired = new Expired
                    {
                        Enable = chkEnable.Checked,
                        StartDay = dtpTimeIn.Value,
                        EndDay = dtpTimeOut.Value,
                    };

                    try
                    {
                        await _clientRepository.UpdateAsync(_clientExist);

                        if (!string.IsNullOrWhiteSpace(normalizedPlate))
                        {
                            if (_vehicleExist != null)
                            {
                                _vehicleExist.PlateNumber = normalizedPlate;
                                _vehicleExist.Type = selectedVehicleType;
                                _vehicleExist.IsActive = true;
                                await _vehicleRepository.UpdateAsync(_vehicleExist);
                            }
                            else
                            {
                                _vehicleExist = new Vehicle
                                {
                                    OwnerClientId = _clientExist.Id,
                                    PlateNumber = normalizedPlate,
                                    Type = selectedVehicleType,
                                    IsActive = true
                                };
                                await _vehicleRepository.AddAsync(_vehicleExist);
                            }
                        }
                        else
                        {
                            // Ô nhập biển số để trống chính là xóa cứng (Hard Delete) phương tiện
                            if (_vehicleExist != null)
                            {
                                await _vehicleRepository.DeleteAsync(_vehicleExist.Id, softDelete: false);
                                _vehicleExist = null;
                            }
                        }
                    }
                    catch (Exception dbEx)
                    {
                        // Rollback toàn bộ thiết bị nếu lưu DB thất bại
                        if (cardChanged)
                        {
                            foreach (var dev in cardUpdatedDevices)
                            {
                                await dev.DeleteCardAsync(newPhone);
                                if (!string.IsNullOrEmpty(oldCardNo))
                                {
                                    await dev.AddCardAsync(idCode, oldCardNo);
                                }
                            }
                        }

                        _clientExist.Avatar = previousAvatarPath;

                        string friendlyDbErr = FormatUserFriendlyError(dbEx.Message);
                        waitScope.Dispose();
                        UpdateStatus($"❌ LỖI CSDL: {friendlyDbErr}", StatusType.Error);
                        MessageBox.Show(
                            $"Lỗi khi lưu thông tin vào cơ sở dữ liệu: {friendlyDbErr}\n\nHệ thống đã khôi phục trạng thái các thiết bị.",
                            "Lỗi Cơ Sở Dữ Liệu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // 4. XỬ LÝ ẢNH TRÊN ĐĨA KHI CẬP NHẬT THÀNH CÔNG
                    if (photoChanged && !string.IsNullOrEmpty(newAvatarPath) && uploadPhotoBytes != null)
                    {
                        try
                        {
                            // Xóa file ảnh cũ nếu khác đường dẫn
                            if (!string.IsNullOrEmpty(previousAvatarPath) &&
                                !string.Equals(previousAvatarPath, newAvatarPath, StringComparison.OrdinalIgnoreCase) &&
                                File.Exists(previousAvatarPath))
                            {
                                File.Delete(previousAvatarPath);
                            }

                            // Nếu file tại newAvatarPath đã tồn tại, xóa trước khi ghi mới
                            if (File.Exists(newAvatarPath))
                            {
                                File.Delete(newAvatarPath);
                            }

                            // Ghi file ảnh mới vào thư mục Avatar
                            await Task.Run(() => File.WriteAllBytes(newAvatarPath, uploadPhotoBytes));
                        }
                        catch (Exception fileEx)
                        {
                            Debug.WriteLine($"[Lỗi xóa/lưu file ảnh Avatar]: {fileEx.Message}");
                        }
                    }

                    _isNewPhotoCaptured = false;
                    _photoBytes = uploadPhotoBytes;
                    btnSave.Text = "Cập nhật";

                    // 5. THÔNG BÁO KẾT QUẢ CHO NGƯỜI DÙNG
                    var distinctFailed = failedDevices
                        .GroupBy(f => f.DeviceIp)
                        .Select(g => g.First())
                        .ToList();

                    waitScope.Dispose();
                    if (distinctFailed.Count > 0 && _faceIdServices.Count > 0)
                    {
                        int successCount = _faceIdServices.Count - distinctFailed.Count;
                        string warnLogs = string.Join("\n", distinctFailed.Select(f => f.ErrorMsg));
                        UpdateStatus($"⚠️ ĐÃ CẬP NHẬT CSDL: Có {distinctFailed.Count}/{_faceIdServices.Count} thiết bị FaceID chưa đồng bộ.", StatusType.Warning);
                        MessageBox.Show(
                            $"Đã cập nhật Database và {successCount}/{_faceIdServices.Count} thiết bị FaceID.\n\n⚠️ Chú ý các thiết bị chưa đồng bộ được:\n{warnLogs}\n\n(Vui lòng kiểm tra lại kết nối các thiết bị trên để đảm bảo khách hàng có thể quẹt qua các làn này)",
                            "Cảnh Báo Đồng Bộ Thiết Bị",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    else
                    {
                        UpdateStatus("✔ CẬP NHẬT THÀNH CÔNG: Đã lưu thông tin và đồng bộ toàn bộ thiết bị FaceID!", StatusType.Success);
                        MessageBox.Show("Cập nhật thông tin và dữ liệu FaceID thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    string friendlyEx = FormatUserFriendlyError(ex.Message);
                    waitScope.Dispose();
                    UpdateStatus($"❌ LỖI HỆ THỐNG: {friendlyEx}", StatusType.Error);
                    MessageBox.Show($"Xảy ra lỗi trong quá trình cập nhật: {friendlyEx}", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    btnSave.Enabled = true;
                }
            }

            // =========================================================================
            // TRƯỜNG HỢP 2: ĐĂNG KÝ MỚI KHÁCH HÀNG (_clientExist == null)
            // =========================================================================
            if (uploadPhotoBytes == null || uploadPhotoBytes.Length == 0)
            {
                waitScope.Dispose();
                btnSave.Enabled = true;
                UpdateStatus("⚠️ THIẾU ẢNH FACEID: Vui lòng bấm 'Chụp ảnh' hoặc tích chọn dùng ảnh CCCD trước khi lưu.", StatusType.Warning);
                MessageBox.Show("Chưa có ảnh khuôn mặt để đăng ký FaceID.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? createdFilePath = null;

            try
            {
                UpdateStatus("⏳ ĐANG ĐĂNG KÝ MỚI: Đang nạp FaceID lên các thiết bị và lưu CSDL...", StatusType.Info);
                // 1. NẠP DỮ LIỆU LÊN TOÀN BỘ THIẾT BỊ FACEID
                var pushTasks = _faceIdServices.Select(service =>
                    PushToSingleDeviceAsync(
                        service, idCode, txtName.Text, rbMale.Checked, newPhone, uploadPhotoBytes)
                    );

                var results = await Task.WhenAll(pushTasks);

                var failedDevices = results.Where(r => !r.Success).ToList();
                var successDevices = results.Where(r => r.Success).ToList();

                // NẾU TẤT CẢ THIẾT BỊ ĐỀU LỖI (0/N thiết bị thành công khi có cấu hình FaceID) -> STRICT ABORT
                if (_faceIdServices.Count > 0 && successDevices.Count == 0)
                {
                    var errorLogs = string.Join("\n", failedDevices.Select(f => FormatUserFriendlyError(f.ErrorMsg ?? "", f.DeviceIp)));

                    // Rollback bất kỳ dữ liệu sót lại trên thiết bị
                    var rollbackTasks = _faceIdServices.Select(s => s.RollbackUserAsync(idCode));
                    await Task.WhenAll(rollbackTasks);

                    waitScope.Dispose();
                    UpdateStatus("❌ ĐỒNG BỘ THẤT BẠI: Toàn bộ thiết bị FaceID không thể nhận diện ảnh.", StatusType.Error);
                    MessageBox.Show(
                        $"Đồng bộ FaceID thất bại trên toàn bộ thiết bị:\n{errorLogs}\n\nThông tin chưa được lưu vào hệ thống. Vui lòng kiểm tra lại ảnh chụp hoặc đường truyền mạng.",
                        "Lỗi Đồng Bộ FaceID",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // 2. GHI FILE ẢNH LOCAL
                if (!string.IsNullOrEmpty(_pathAvatar))
                {
                    Directory.CreateDirectory(_pathAvatar);
                    string fileName = $"{idCode}.jpg";
                    createdFilePath = Path.Combine(_pathAvatar, fileName);
                    if (File.Exists(createdFilePath))
                    {
                        try { File.Delete(createdFilePath); } catch { }
                    }
                    await Task.Run(() => File.WriteAllBytes(createdFilePath, uploadPhotoBytes));
                }

                // 3. LƯU DATABASE
                Client newClientDb = new()
                {
                    Code = idCode,
                    Name = txtName.Text,
                    BirthDay = dtpDateOfBirth.Value,
                    Address = txtAddress.Text,
                    Gender = rbMale.Checked ? 0 : 1,
                    Avatar = createdFilePath ?? "",
                    PhoneNumber = newPhone,
                    Type = ClientType.VIP,
                    Note = txtDescription.Text,
                    IsActive = true,
                    Expired = new Expired
                    {
                        Enable = chkEnable.Checked,
                        StartDay = dtpTimeIn.Value,
                        EndDay = dtpTimeOut.Value,
                    },
                };

                try
                {
                    await _clientRepository.AddAsync(newClientDb);

                    if (!string.IsNullOrWhiteSpace(normalizedPlate))
                    {
                        _vehicleExist = new Vehicle
                        {
                            OwnerClientId = newClientDb.Id,
                            PlateNumber = normalizedPlate,
                            Type = selectedVehicleType,
                            IsActive = true
                        };
                        await _vehicleRepository.AddAsync(_vehicleExist);
                    }
                }
                catch (Exception dbEx)
                {
                    // Xóa file rác trên ổ đĩa nếu lưu DB lỗi
                    if (!string.IsNullOrEmpty(createdFilePath) && File.Exists(createdFilePath))
                    {
                        try { File.Delete(createdFilePath); } catch { }
                    }

                    // Rollback các thiết bị đã nạp thành công
                    var rollbackTasks = successDevices.Select(s =>
                    {
                        var dev = _faceIdServices.FirstOrDefault(svc => svc.Ip == s.DeviceIp);
                        return dev?.RollbackUserAsync(idCode) ?? Task.FromResult(false);
                    });
                    await Task.WhenAll(rollbackTasks);

                    string friendlyDbErr = FormatUserFriendlyError(dbEx.Message);
                    waitScope.Dispose();
                    UpdateStatus($"❌ LỖI CSDL: {friendlyDbErr}", StatusType.Error);
                    MessageBox.Show(
                        $"Lỗi khi lưu vào cơ sở dữ liệu: {friendlyDbErr}\n\nHệ thống đã tự động thu hồi dữ liệu trên các thiết bị FaceID để tránh lệch dữ liệu.",
                        "Lỗi Cơ Sở Dữ Liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                _clientExist = newClientDb;
                _photoBytes = uploadPhotoBytes;
                _isNewPhotoCaptured = false;
                btnSave.Text = "Cập nhật";

                // 4. THÔNG BÁO KẾT QUẢ CHO NGƯỜI DÙNG
                waitScope.Dispose();
                if (failedDevices.Count > 0 && _faceIdServices.Count > 0)
                {
                    var warnLogs = string.Join("\n", failedDevices.Select(f => FormatUserFriendlyError(f.ErrorMsg ?? "", f.DeviceIp)));
                    UpdateStatus($"⚠️ ĐĂNG KÝ CSDL XONG: Có {failedDevices.Count}/{_faceIdServices.Count} thiết bị FaceID chưa đồng bộ.", StatusType.Warning);
                    MessageBox.Show(
                        $"Đăng ký khách hàng thành công vào Database và {successDevices.Count}/{_faceIdServices.Count} thiết bị FaceID.\n\n⚠️ Chú ý các thiết bị chưa đồng bộ được:\n{warnLogs}\n\n(Vui lòng kiểm tra lại thiết bị trên để khách hàng có thể quẹt qua các làn này)",
                        "Cảnh Báo Đồng Bộ Thiết Bị",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    UpdateStatus("✔ ĐĂNG KÝ THÀNH CÔNG: Đã lưu thông tin khách hàng và đồng bộ toàn bộ thiết bị FaceID!", StatusType.Success);
                    MessageBox.Show("Đăng ký khách hàng thành công vào hệ thống và thiết bị FaceID!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // Xóa file rác trên ổ đĩa nếu lỗi hệ thống
                if (!string.IsNullOrEmpty(createdFilePath) && File.Exists(createdFilePath))
                {
                    try { File.Delete(createdFilePath); } catch { }
                }

                // Rollback thiết bị nếu đã nạp
                var rollbackTasks = _faceIdServices.Select(s => s.RollbackUserAsync(idCode));
                await Task.WhenAll(rollbackTasks);

                string friendlyEx = FormatUserFriendlyError(ex.Message);
                waitScope.Dispose();
                UpdateStatus($"❌ LỖI HỆ THỐNG: {friendlyEx}", StatusType.Error);
                MessageBox.Show($"Xảy ra lỗi hệ thống khi lưu: {friendlyEx}", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }

        private void chkEnable_CheckedChanged(object? sender, EventArgs e)
        {
            bool isRestricted = !chkEnable.Checked;
            dtpTimeIn.Enabled = isRestricted;
            dtpTimeOut.Enabled = isRestricted;
            lbTimeIn.Enabled = isRestricted;
            lbTimeOut.Enabled = isRestricted;
            if (chkEnable.Checked)
            {
                UpdateStatus("ℹ️ THỜI HẠN: Đã đặt chế độ không giới hạn thời gian ra vào cho khách.", StatusType.Info);
            }
            else
            {
                UpdateStatus("ℹ️ THỜI HẠN: Áp dụng giới hạn thời gian ra vào theo Ngày vào - Ngày ra.", StatusType.Info);
            }
        }

        private void FrmRegisterClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            _hn212Client.CardScanned -= OnCardScanned;
            _hn212Client.FaceCaptured -= OnFaceCaptured;
            _hn212Client.FaceCompared -= OnFaceCompared;
            _hn212Client.StatusUpdated -= OnStatusUpdated;
            _hn212Client.CardStatusChanged -= OnCardStatusChanged;
            _hn212Client.ConnectionStateChanged -= OnConnectionStateChanged;
            ckUploadFaceid.CheckedChanged -= ckUploadFaceid_CheckedChanged;
        }
    }
}
