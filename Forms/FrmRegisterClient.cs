using HPParking.Interfaces;
using HPParking.Models.Entities;
using HPParking.Services.FaceId;
using HPParking.Services.Worker;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmRegisterClient : Form
    {
        private readonly CccdModel cccdData;
        private readonly IClientRepository _clientRepository;
        private readonly IFaceIdApiService _faceIdApiService;
        private Client _clientExist;
        private string _pathAvatar;

        public FrmRegisterClient(CccdModel cccdModel, IClientRepository clientRepository, Company company, IFaceIdApiService faceIdApiService = null)
        {
            InitializeComponent();
            cccdData = cccdModel;
            _clientRepository = clientRepository;
            _pathAvatar = Path.Combine(company.PathImage, "Avatar");

            // Sử dụng Dependency Injection hoặc đọc Config từ File/Database
            _faceIdApiService = faceIdApiService ?? new FaceIdApiService(new FaceIdConfig
            {
                Ip = "192.168.1.205",
                Username = "admin",
                Password = "Hoangphat130225"
            });
        }

        private async void FrmRegisterClient_Load(object sender, EventArgs e)
        {
            _clientExist = await _clientRepository.GetByIdCode(cccdData.DocumentNumber);

            if (!string.IsNullOrEmpty(cccdData.FaceBase64))
            {
                try
                {
                    byte[] imageBytes = Convert.FromBase64String(cccdData.FaceBase64);
                    using var ms = new MemoryStream(imageBytes);
                    using var tempBitmap = new Bitmap(ms);
                    pbAvatar.Image?.Dispose();
                    pbAvatar.Image = new Bitmap(tempBitmap); // Tạo bản sao an toàn cho GDI+
                }
                catch (Exception)
                {
                    Debug.WriteLine("Lỗi hiển thị ảnh CCCD");
                }
            }

            txtIdCode.Text = cccdData.DocumentNumber;
            txtName.Text = cccdData.FullName;
            txtAddress.Text = cccdData.Address;

            if (DateTime.TryParseExact(cccdData.DateOfBirth, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime dob))
            {
                dtpDateOfBirth.Value = dob;
            }

            if (cccdData.Sex?.ToLower() == "nam")
            {
                rbMale.Checked = true;
            }
            else
            {
                rbFeMale.Checked = true;
            }

            if (_clientExist != null)
            {
                txtPhoneNumber.Text = _clientExist.PhoneNumber;
                txtPlate.Text = _clientExist.LicensePlate;
                txtDescription.Text = _clientExist.Description;
                dtpTimeIn.Value = _clientExist.Expired?.StartDay ?? DateTime.Now;
                dtpTimeOut.Value = _clientExist.Expired?.EndDay ?? DateTime.Now;

                MessageBox.Show("Khách hàng đã tồn tại trong hệ thống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                pnRegisterClient.Enabled = false;
                btnSave.ForeColor = Color.White;
                return;
            }
            dtpTimeOut.Value = dtpTimeOut.Value.AddDays(1);
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            // --- BƯỚC 1: VALIDATE ---
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

            if (_clientExist != null)
            {
                MessageBox.Show("Khách hàng đã tồn tại trong hệ thống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Bắt buộc phải có ảnh khuôn mặt từ CCCD mới cho đăng ký — nếu không có
            // khuôn mặt, khách hàng sẽ không thể chấm công/mở barrier bằng FaceID.
            if (string.IsNullOrEmpty(cccdData.FaceBase64))
            {
                MessageBox.Show("Không tìm thấy ảnh khuôn mặt từ CCCD. Vui lòng quét lại thẻ trước khi lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;
            bool isUserAdded = false;

            try
            {
                // --- BƯỚC 2: TẠO USER TRÊN HIKVISION ---
                var (userOk, userErr) = await _faceIdApiService.AddUserAsync(txtIdCode.Text, txtName.Text, rbMale.Checked);
                if (!userOk)
                {
                    MessageBox.Show($"Lỗi tạo User trên thiết bị FaceID", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                isUserAdded = true;

                // --- BƯỚC 3: GÁN THẺ TRÊN HIKVISION ---
                var (cardOk, cardErr) = await _faceIdApiService.AddCardAsync(txtIdCode.Text, txtPhoneNumber.Text);
                if (!cardOk)
                {
                    MessageBox.Show($"Lỗi gán thẻ trên thiết bị FaceID. Đang thu hồi dữ liệu...", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    bool rollbackOk = await _faceIdApiService.RollbackUserAsync(txtIdCode.Text);
                    if (!rollbackOk)
                    {
                        Debug.WriteLine($"[FaceID] CẢNH BÁO: Rollback User {txtIdCode.Text} thất bại sau lỗi gán thẻ — có thể còn User rác trên thiết bị.");
                    }
                    return;
                }

                // --- BƯỚC 4: NẠP KHUÔN MẶT LÊN HIKVISION (bắt buộc) ---
                var (faceOk, faceErr) = await _faceIdApiService.AddFaceImageAsync(txtIdCode.Text, cccdData.FaceBase64);
                if (!faceOk)
                {
                    MessageBox.Show($"Lỗi nạp khuôn mặt lên thiết bị FaceID. Đang thu hồi dữ liệu...", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    bool rollbackOk = await _faceIdApiService.RollbackUserAsync(txtIdCode.Text);
                    if (!rollbackOk)
                    {
                        Debug.WriteLine($"[FaceID] CẢNH BÁO: Rollback User {txtIdCode.Text} thất bại sau lỗi nạp khuôn mặt — có thể còn User rác trên thiết bị.");
                    }
                    return;
                }
                string faceBase64 = cccdData.FaceBase64;
                if (faceBase64.Contains(","))
                {
                    faceBase64 = faceBase64.Split(',')[1];
                }

                byte[] imageBytes = Convert.FromBase64String(faceBase64);

                Directory.CreateDirectory(_pathAvatar);

                string fileName = $"{cccdData.DocumentNumber}.jpg";
                string filePath = Path.Combine(_pathAvatar, fileName);

                File.WriteAllBytes(filePath, imageBytes);

                // --- BƯỚC 5: LƯU DATABASE ---
                Client newClientDb = new()
                {
                    ID_Code = txtIdCode.Text,
                    Card_Code = txtPhoneNumber.Text,
                    Name = txtName.Text,
                    BirthDay = dtpDateOfBirth.Value,
                    Address = txtAddress.Text,
                    Gender = rbMale.Checked ? 0 : 1,
                    Avatar = filePath,
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
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception)
            {
                if (isUserAdded)
                {
                    bool rollbackOk = await _faceIdApiService.RollbackUserAsync(txtIdCode.Text);
                    if (!rollbackOk)
                    {
                        Debug.WriteLine($"[FaceID] CẢNH BÁO: Rollback User {txtIdCode.Text} thất bại sau lỗi hệ thống — có thể còn User rác trên thiết bị.");
                    }
                }
                MessageBox.Show($"Xảy ra lỗi hệ thống: Đã hoàn tác dữ liệu trên thiết bị!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }
    }
}