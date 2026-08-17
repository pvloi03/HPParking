using HPParking.Interfaces;
using HPParking.Models.Entities;
using HPParking.Services.CCCDReader;
using HPParking.Services.FaceId;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmRegisterClient : Form
    {
        private readonly CccdReaderManager _readerManager;
        private readonly IClientRepository _clientRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IFaceIdApiService _faceIdApiService;
        private Client _clientExist;
        private string _pathAvatar;
        private PhotoCapturedDto _photo;

        public FrmRegisterClient(
            CccdReaderManager readerManager,
            IClientRepository clientRepository,
            ICompanyRepository companyRepository)
        {
            InitializeComponent();
            _readerManager = readerManager;

            // Đăng ký nhận sự kiện Chụp ảnh từ Manager
            _readerManager.CardScanned += OnCardScanned;
            _readerManager.PhotoCaptured += OnPhotoCaptured;
            _readerManager.StatusUpdated += OnStatusUpdated;

            _clientRepository = clientRepository;
            _companyRepository = companyRepository;

            // Sử dụng Dependency Injection hoặc đọc Config từ File/Database
            _faceIdApiService = new FaceIdApiService(new FaceIdConfig
            {
                Ip = "192.168.1.205",
                Username = "admin",
                Password = "Hoangphat130225"
            });
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

        // Hàm hỗ trợ vẽ mảng byte[] ảnh an toàn
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
            bool isUserAdded = false;
            string createdFilePath = null;

            try
            {
                // 2. NẠP DỮ LIỆU LÊN THIẾT BỊ HIKVISION
                var (userOk, _) = await _faceIdApiService.AddUserAsync(txtIdCode.Text, txtName.Text, rbMale.Checked);
                if (!userOk)
                {
                    MessageBox.Show("Lỗi tạo User trên thiết bị FaceID.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                isUserAdded = true;

                var (cardOk, _) = await _faceIdApiService.AddCardAsync(txtIdCode.Text, txtPhoneNumber.Text);
                if (!cardOk)
                {
                    MessageBox.Show("Lỗi gán thẻ trên thiết bị FaceID. Đang thu hồi dữ liệu...", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    await RollbackFaceIdAsync();
                    return;
                }

                var (faceOk, _) = await _faceIdApiService.AddFaceImageAsync(txtIdCode.Text, _photo.PhotoBytes);
                if (!faceOk)
                {
                    MessageBox.Show("Lỗi nạp khuôn mặt lên thiết bị FaceID. Đang thu hồi dữ liệu...", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    await RollbackFaceIdAsync();
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
                if (isUserAdded)
                {
                    await RollbackFaceIdAsync();
                }

                MessageBox.Show($"Xảy ra lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }

        private async Task RollbackFaceIdAsync()
        {
            bool rollbackOk = await _faceIdApiService.RollbackUserAsync(txtIdCode.Text);
            if (!rollbackOk)
            {
                Debug.WriteLine($"[FaceID] CẢNH BÁO: Rollback User {txtIdCode.Text} thất bại — có thể còn User rác trên thiết bị.");
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