using HPParking.Interfaces;
using HPParking.Models.Entities;
using HPParking.Services.CCCDReader;
using HPParking.Services.FaceId;
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
        private readonly CccdReaderManager _readerManager;
        private readonly IClientRepository _clientRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly ILaneRepository _laneRepository;
        private readonly List<IFaceIdApiService> _faceIdServices = [];
        private readonly List<FaceIdConfig> _faceIdConfigs = [];
        private Client _clientExist;
        private string _pathAvatar;
        private PhotoCapturedDto _photo;

        public FrmRegisterClient(
            CccdReaderManager readerManager,
            IClientRepository clientRepository,
            ICompanyRepository companyRepository,
            ILaneRepository laneRepository)
        {
            InitializeComponent();
            _readerManager = readerManager;

            // Đăng ký nhận sự kiện Chụp ảnh từ Manager
            _readerManager.CardScanned += OnCardScanned;
            _readerManager.PhotoCaptured += OnPhotoCaptured;
            _readerManager.StatusUpdated += OnStatusUpdated;

            _clientRepository = clientRepository;
            _companyRepository = companyRepository;
            _laneRepository = laneRepository;
        }

        private async void FrmRegisterClient_Load(object sender, EventArgs e)
        {
            var company = await _companyRepository.GetFirstCompanyAsync();
            if (company == null)
            {
                MessageBox.Show("KHông tìm thấy đường dẫn lưu ảnh");
                return;
            }
            _pathAvatar = Path.Combine(company.PathImage, "Avatar");

            if (string.IsNullOrEmpty(txtIdCode.Text))
                UpdateStatus("✘Đọc dữ liệu thất bại vui lòng thử lại!", Color.Red);

            var lanes = await _laneRepository.GetAllAsync();

            // Lọc bỏ lane null config và loại trùng lặp theo IP
            var uniqueConfigs = lanes
                .Where(l => l?.FaceIdConfig != null && !string.IsNullOrWhiteSpace(l.FaceIdConfig.IP))
                .Select(l => new FaceIdConfig
                {
                    Ip = l.FaceIdConfig.IP,
                    Username = l.FaceIdConfig.User,
                    Password = l.FaceIdConfig.Pass
                })
                .GroupBy(c => c.Ip)
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

        private void OnStatusUpdated(DeviceStatusDto status)
        {
            if (status == null || IsDisposed) return;

            this.BeginInvoke(new Action(() =>
            {
                // Cập nhật thông báo tiến trình hoặc thông báo LỖI
                if (!string.IsNullOrEmpty(status.ErrorMessage))
                {
                    UpdateStatus($"✘{status.ErrorMessage}", Color.Red);
                }
                if (!status.IsCardPresent) { UpdateStatus(status.StatusMessage, Color.Red); }
                else UpdateStatus(status.StatusMessage, Color.SeaGreen);

            }));
        }

        public async void DisplayCardData(CardDataDto card)
        {
            if (card == null || string.IsNullOrEmpty(card.DocumentNumber)) return;

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
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Lỗi đọc ảnh Avatar]: {ex.Message}");
                    }
                }

                MessageBox.Show("Khách hàng đã tồn tại trong hệ thống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                pnRegisterClient.Enabled = false;
                btnSave.Enabled = false; // Disable nút bấm để tránh thao tác thừa
                return;
            }
            dtpTimeOut.Value = dtpTimeOut.Value.AddDays(1);
        }

        private void OnPhotoCaptured(PhotoCapturedDto photo)
        {
            if (photo == null || this.IsDisposed) return;
            _photo = photo;
            this.BeginInvoke(new Action(() =>
            {
                SetPictureBoxImage(pbCapturedFace, photo.PhotoBytes);
                btnOpenCamera.Enabled = true;
            }));
        }

        private async void btnOpenCamera_Click(object sender, EventArgs e)
        {
            try
            {
                btnOpenCamera.Enabled = false;
                await _readerManager.RequestCaptureAsync();
            }
            catch (Exception)
            {
                UpdateStatus("Chụp ảnh thành công!", Color.Red);
            }
        }

        public void SetPictureBoxImage(PictureBox pictureBox, byte[] byteArray)
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
            catch { }
        }

        private async Task<(bool Success, string DeviceIp, string ErrorMsg)> PushToSingleDeviceAsync(
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
            if (!userOk) return (false, deviceIp, $"Lỗi tạo User trên thiết bị FaceID: {userErr}");

            // 2. Add Card
            var (cardOk, cardErr) = await apiService.AddCardAsync(idCode, phone);
            if (!cardOk)
            {
                await apiService.RollbackUserAsync(idCode);
                return (false, deviceIp, $"Lỗi gán thẻ trên thiết bị FaceID: {cardErr}");
            }

            // 3. Add Face Image
            var (faceOk, faceErr) = await apiService.AddFaceImageAsync(idCode, photoBytes);
            if (!faceOk)
            {
                await apiService.RollbackUserAsync(idCode);
                return (false, deviceIp, $"Lỗi nạp khuôn mặt lên thiết bị FaceID: {faceErr}");
            }

            return (true, deviceIp, null);
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            // 1. VALIDATE DỮ LIỆU IN-MEMORY
            if (string.IsNullOrWhiteSpace(txtPhoneNumber.Text))
            {
                MessageBox.Show("Vui lòng nhập số điện thoại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhoneNumber.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPlate.Text))
            {
                MessageBox.Show("Vui lòng nhập biển số xe.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPlate.Focus();
                return;
            }


            // Kiểm tra an toàn biến _photo tránh NullReferenceException
            if (_photo == null || _photo.PhotoBytes == null || _photo.PhotoBytes.Length == 0)
            {
                MessageBox.Show("Không tìm thấy ảnh đăng ký FaceId. Vui lòng chụp ảnh trước khi lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;
            string createdFilePath = null;

            try
            {
                // 2. NẠP DỮ LIỆU LÊN THIẾT BỊ HIKVISION
                var pushTasks = _faceIdServices.Select(service =>
                    PushToSingleDeviceAsync(
                        service, txtIdCode.Text, txtName.Text, rbMale.Checked, txtPhoneNumber.Text, _photo.PhotoBytes)
                    );

                var results = await Task.WhenAll(pushTasks);

                // Kiểm tra kết quả các thiết bị
                var failedDevices = results.Where(r => !r.Success).ToList();

                if (failedDevices.Any())
                {
                    var errorLogs = string.Join("\n", failedDevices.Select(f => $"- IP {f.DeviceIp}: {f.ErrorMsg}"));

                    // Lựa chọn 1: Rollback tất cả thiết bị đã thành công trước đó (nếu muốn tính toàn vẹn 100%)
                    var rollbackTasks = _faceIdServices.Select(s => s.RollbackUserAsync(txtIdCode.Text));
                    await Task.WhenAll(rollbackTasks);

                    MessageBox.Show($"Đồng bộ FaceID thất bại trên một số thiết bị:\n{errorLogs}\n\nĐã thu hồi dữ liệu toàn bộ thiết bị.", "Lỗi Đồng Bộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 3. GHI FILE ẢNH LOCAL
                Directory.CreateDirectory(_pathAvatar);
                string fileName = $"{txtIdCode.Text}.jpg";
                createdFilePath = Path.Combine(_pathAvatar, fileName);
                await Task.Run(() => File.WriteAllBytes(createdFilePath, _photo.PhotoBytes));

                // 4. LƯU DATABASE
                Client newClientDb = new()
                {
                    ID_Code = txtIdCode.Text,
                    Card_Code = txtPhoneNumber.Text,
                    Name = txtName.Text,
                    BirthDay = dtpDateOfBirth.Value,
                    Address = txtAddress.Text,
                    Gender = rbMale.Checked ? 0 : 1,
                    Avatar = createdFilePath,
                    PhoneNumber = txtPhoneNumber.Text?.Trim() ?? "",
                    Description = txtDescription.Text,
                    LicensePlate = txtPlate.Text?
                        .Trim()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace(".", "")
                        .ToUpperInvariant() ?? "",
                    Expired = new Expired
                    {
                        StartDay = dtpTimeIn.Value,
                        EndDay = dtpTimeOut.Value,
                    },
                };

                await _clientRepository.Insert(newClientDb);

                MessageBox.Show("Lưu thành công vào Database và Thiết bị FaceID!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Xóa file rác trên ổ đĩa nếu lưu DB lỗi
                if (!string.IsNullOrEmpty(createdFilePath) && File.Exists(createdFilePath))
                {
                    try { File.Delete(createdFilePath); } catch { }
                }

                // Rollback thiết bị nếu đã lỡ AddUser
                var rollbackTasks = _faceIdServices.Select(s => s.RollbackUserAsync(txtIdCode.Text));
                await Task.WhenAll(rollbackTasks);

                MessageBox.Show($"Xảy ra lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }

        private void FrmRegisterClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            _readerManager.CardScanned -= OnCardScanned;
            _readerManager.PhotoCaptured -= OnPhotoCaptured;
            _readerManager.StatusUpdated -= OnStatusUpdated;
        }
    }
}