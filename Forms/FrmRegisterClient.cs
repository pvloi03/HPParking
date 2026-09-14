using HPParking.Helper;
using HPParking.Interfaces;
using HPParking.Models.Entities;
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
        private readonly IClientRepository _clientRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly ILaneRepository _laneRepository;
        private readonly List<IFaceIdApiService> _faceIdServices = [];
        private Client? _clientExist;
        private string? _pathAvatar;
        private byte[]? _photoBytes;
        private bool _isNewPhotoCaptured;

        public FrmRegisterClient(
            IHn212Client hn212Client,
            IClientRepository clientRepository,
            ICompanyRepository companyRepository,
            ILaneRepository laneRepository)
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

            _clientRepository = clientRepository;
            _companyRepository = companyRepository;
            _laneRepository = laneRepository;
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
                UpdateStatus("Chưa kết nối tới Service HN212Reader", Color.Red);
            }
            else
            {
                UpdateStatus("Đang kiểm tra đầu đọc HN212...", Color.Blue);
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

            var company = await _companyRepository.GetFirstCompanyAsync();
            if (company == null)
            {
                MessageBox.Show("Không tìm thấy đường dẫn lưu ảnh");
                return;
            }
            _pathAvatar = Path.Combine(company.PathImage, "Avatar");

            var lanes = await _laneRepository.GetAllAsync();

            // Lọc bỏ lane null config và loại trùng lặp theo IP (loại bỏ khoảng trắng thừa và không phân biệt hoa thường)
            var uniqueConfigs = lanes
                .Where(l => l?.FaceIdConfig != null && !string.IsNullOrWhiteSpace(l.FaceIdConfig.IP))
                .Select(l => new FaceIdConfig
                {
                    Ip = l!.FaceIdConfig!.IP.Trim(),
                    Username = l.FaceIdConfig!.User?.Trim() ?? "",
                    Password = l.FaceIdConfig!.Pass?.Trim() ?? ""
                })
                .GroupBy(c => c.Ip, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            _faceIdServices.Clear();
            foreach (var config in uniqueConfigs)
            {
                _faceIdServices.Add(new FaceIdApiService(config));
            }
        }

        public void UpdateStatus(string msg, Color color)
        {
            if (lblStatus != null)
            {
                if (lblStatus.IsDisposed) return;

                lblStatus.Text = msg;
                lblStatus.ForeColor = color;
            }
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
                    UpdateStatus("Chưa kết nối đầu đọc HN212", Color.Red);
                }
                else if (status.CardStatus == "Error")
                {
                    UpdateStatus($"✘ {status.Message}", Color.Red);
                }
                else if (status.CardStatus == "Reading")
                {
                    UpdateStatus("Đang đọc thẻ CCCD...", Color.Blue);
                }
                else if (status.CardStatus == "ReadSuccess")
                {
                    UpdateStatus("Đọc thẻ CCCD thành công", Color.SeaGreen);
                }
                else if (status.CardStatus == "Present")
                {
                    UpdateStatus("Đã đặt thẻ CCCD", Color.SeaGreen);
                }
                else
                {
                    UpdateStatus("Đầu đọc sẵn sàng (vui lòng đặt thẻ CCCD)", Color.SeaGreen);
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
                        UpdateStatus(string.IsNullOrWhiteSpace(message) ? "Đang đọc thẻ CCCD..." : message, Color.Blue);
                        break;
                    case "ReadSuccess":
                        UpdateStatus("Đọc thẻ CCCD thành công", Color.SeaGreen);
                        break;
                    case "Present":
                        UpdateStatus(string.IsNullOrWhiteSpace(message) ? "Đã đặt thẻ CCCD, đang đọc..." : message, Color.SeaGreen);
                        break;
                    case "Error":
                        UpdateStatus($"✘ {message}", Color.Red);
                        break;
                    case "Absent":
                    default:
                        UpdateStatus("Đầu đọc sẵn sàng (vui lòng đặt thẻ CCCD)", Color.SeaGreen);
                        break;
                }
            }));
        }

        private void OnConnectionStateChanged(string msg, bool isConnected)
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                UpdateStatus(msg, isConnected ? Color.SeaGreen : Color.Red);
            }));
        }

        public async void DisplayCardData(CardDataDto card)
        {
            if (card == null || string.IsNullOrEmpty(card.DocumentNumber)) return;

            using var waitScope = new WaitCursorScope(this);
            _clientExist = await _clientRepository.GetByIdCode(card.DocumentNumber);

            txtIdCode.Text = card.DocumentNumber;
            txtName.Text = card.FullName;
            txtAddress.Text = card.Address;
            SetPictureBoxImage(pbAvatar, card.ChipFaceBytes);

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
                txtPlate.Text = _clientExist.LicensePlate;
                txtDescription.Text = _clientExist.Description;
                dtpTimeIn.Value = _clientExist.Expired?.StartDay ?? DateTime.Now;
                dtpTimeOut.Value = _clientExist.Expired?.EndDay ?? DateTime.Now;

                if (!string.IsNullOrEmpty(_clientExist.Avatar) && File.Exists(_clientExist.Avatar))
                {
                    try
                    {
                        // Dùng ReadAllBytes để tránh khóa (lock) file ảnh trên đĩa
                        byte[] avatarBytes = await Task.Run(() => File.ReadAllBytes(_clientExist.Avatar));
                        SetPictureBoxImage(pbCapturedFace, avatarBytes);
                        _photoBytes = avatarBytes;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Lỗi đọc ảnh Avatar]: {ex.Message}");
                    }
                }

                _isNewPhotoCaptured = false;
                btnSave.Text = "Cập nhật";
                btnOpenCamera.Enabled = true;
                waitScope.Dispose();
                MessageBox.Show("Khách hàng đã tồn tại trong hệ thống. Bạn có thể chỉnh sửa thông tin hoặc bấm 'Mở Camera' để chụp ảnh mới cập nhật FaceID.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnSave.Text = "Lưu";
            btnOpenCamera.Enabled = true;
            _isNewPhotoCaptured = false;
            dtpTimeOut.Value = dtpTimeOut.Value.AddDays(1);
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
                    SetPictureBoxImage(pbCapturedFace, _photoBytes);
                    btnOpenCamera.Enabled = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OnFaceCaptured Error]: {ex.Message}");
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
                    SetPictureBoxImage(pbCapturedFace, _photoBytes);
                }
                if (result.IsMatch)
                {
                    UpdateStatus($"✔ {result.Message}", Color.SeaGreen);
                }
                else
                {
                    UpdateStatus($"✘ {result.Message}", Color.Red);
                }
                btnOpenCamera.Enabled = true;
            }));
        }

        private void btnOpenCamera_Click(object sender, EventArgs e)
        {
            try
            {
                using var camForm = new FrmCameraCapture(_hn212Client);
                var result = camForm.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    _isNewPhotoCaptured = true;
                    UpdateStatus("Đã chụp và nhận diện khuôn mặt thành công!", Color.SeaGreen);
                }
                else
                {
                    UpdateStatus("Đã hủy chụp ảnh.", Color.OrangeRed);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi mở camera: {ex.Message}", Color.Red);
            }
        }

        public static void SetPictureBoxImage(PictureBox pictureBox, byte[] byteArray)
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
                MessageBox.Show("Vui lòng quét thẻ CCCD hoặc nhập số CCCD trước khi lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtIdCode.Focus();
                return;
            }

            string newPhone = txtPhoneNumber.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(newPhone))
            {
                MessageBox.Show("Vui lòng nhập số điện thoại (dùng làm mã thẻ nhận diện).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhoneNumber.Focus();
                return;
            }

            string rawPlate = txtPlate.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(rawPlate))
            {
                MessageBox.Show("Vui lòng nhập biển số xe.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPlate.Focus();
                return;
            }

            string normalizedPlate = rawPlate
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(".", "")
                .ToUpperInvariant();

            btnSave.Enabled = false;
            using var waitScope = new WaitCursorScope(this);

            // =========================================================================
            // TRƯỜNG HỢP 1: CẬP NHẬT KHÁCH HÀNG ĐÃ TỒN TẠI (_clientExist != null)
            // =========================================================================
            if (_clientExist != null)
            {
                string oldCardNo = !string.IsNullOrEmpty(_clientExist.Card_Code)
                    ? _clientExist.Card_Code
                    : (_clientExist.PhoneNumber ?? "");
                bool cardChanged = !string.Equals(oldCardNo, newPhone, StringComparison.OrdinalIgnoreCase);
                bool photoChanged = _isNewPhotoCaptured && _photoBytes != null && _photoBytes.Length > 0;

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
                            var (updOk, updErr) = await service.UpdateFaceImageAsync(idCode, _photoBytes!);
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
                    _clientExist.Card_Code = newPhone;
                    _clientExist.Description = txtDescription.Text;
                    _clientExist.LicensePlate = normalizedPlate;
                    _clientExist.Expired = new Expired
                    {
                        StartDay = dtpTimeIn.Value,
                        EndDay = dtpTimeOut.Value,
                    };

                    try
                    {
                        await _clientRepository.Update(_clientExist);
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
                        MessageBox.Show(
                            $"Lỗi khi lưu thông tin vào cơ sở dữ liệu: {friendlyDbErr}\n\nHệ thống đã khôi phục trạng thái các thiết bị.",
                            "Lỗi Cơ Sở Dữ Liệu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // 4. XỬ LÝ ẢNH TRÊN ĐĨA KHI CẬP NHẬT THÀNH CÔNG
                    if (photoChanged && !string.IsNullOrEmpty(newAvatarPath) && _photoBytes != null)
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
                            await Task.Run(() => File.WriteAllBytes(newAvatarPath, _photoBytes));
                        }
                        catch (Exception fileEx)
                        {
                            Debug.WriteLine($"[Lỗi xóa/lưu file ảnh Avatar]: {fileEx.Message}");
                        }
                    }

                    _isNewPhotoCaptured = false;
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
                        MessageBox.Show(
                            $"Đã cập nhật Database và {successCount}/{_faceIdServices.Count} thiết bị FaceID.\n\n⚠️ Chú ý các thiết bị chưa đồng bộ được:\n{warnLogs}\n\n(Vui lòng kiểm tra lại kết nối các thiết bị trên để đảm bảo khách hàng có thể quẹt qua các làn này)",
                            "Cảnh Báo Đồng Bộ Thiết Bị",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Cập nhật thông tin và dữ liệu FaceID thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    string friendlyEx = FormatUserFriendlyError(ex.Message);
                    waitScope.Dispose();
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
            if (_photoBytes == null || _photoBytes.Length == 0)
            {
                waitScope.Dispose();
                btnSave.Enabled = true;
                MessageBox.Show("Chưa có ảnh khuôn mặt để đăng ký FaceID. Vui lòng bấm 'Mở Camera' để chụp ảnh hoặc đặt thẻ CCCD vào đầu đọc trước khi lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? createdFilePath = null;

            try
            {
                // 1. NẠP DỮ LIỆU LÊN TOÀN BỘ THIẾT BỊ FACEID
                var pushTasks = _faceIdServices.Select(service =>
                    PushToSingleDeviceAsync(
                        service, idCode, txtName.Text, rbMale.Checked, newPhone, _photoBytes)
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
                    await Task.Run(() => File.WriteAllBytes(createdFilePath, _photoBytes));
                }

                // 3. LƯU DATABASE
                Client newClientDb = new()
                {
                    ID_Code = idCode,
                    Card_Code = newPhone,
                    Name = txtName.Text,
                    BirthDay = dtpDateOfBirth.Value,
                    Address = txtAddress.Text,
                    Gender = rbMale.Checked ? 0 : 1,
                    Avatar = createdFilePath ?? "",
                    PhoneNumber = newPhone,
                    Description = txtDescription.Text,
                    LicensePlate = normalizedPlate,
                    Expired = new Expired
                    {
                        StartDay = dtpTimeIn.Value,
                        EndDay = dtpTimeOut.Value,
                    },
                };

                try
                {
                    await _clientRepository.Insert(newClientDb);
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
                    MessageBox.Show(
                        $"Lỗi khi lưu vào cơ sở dữ liệu: {friendlyDbErr}\n\nHệ thống đã tự động thu hồi dữ liệu trên các thiết bị FaceID để tránh lệch dữ liệu.",
                        "Lỗi Cơ Sở Dữ Liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                _clientExist = newClientDb;
                _isNewPhotoCaptured = false;
                btnSave.Text = "Cập nhật";

                // 4. THÔNG BÁO KẾT QUẢ CHO NGƯỜI DÙNG
                waitScope.Dispose();
                if (failedDevices.Count > 0 && _faceIdServices.Count > 0)
                {
                    var warnLogs = string.Join("\n", failedDevices.Select(f => FormatUserFriendlyError(f.ErrorMsg ?? "", f.DeviceIp)));
                    MessageBox.Show(
                        $"Đăng ký khách hàng thành công vào Database và {successDevices.Count}/{_faceIdServices.Count} thiết bị FaceID.\n\n⚠️ Chú ý các thiết bị chưa đồng bộ được:\n{warnLogs}\n\n(Vui lòng kiểm tra lại thiết bị trên để khách hàng có thể quẹt qua các làn này)",
                        "Cảnh Báo Đồng Bộ Thiết Bị",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
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
                MessageBox.Show($"Xảy ra lỗi hệ thống khi lưu: {friendlyEx}", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
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
        }
    }
}
