using HPParking.Helper;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Forms.ConfigManager
{
    public partial class UcCompanyManager : UserControl
    {
        private readonly ICompanyRepository _companyRepository;
        private Company? _company;

        public UcCompanyManager(ICompanyRepository companyRepository)
        {
            InitializeComponent();
            _companyRepository = companyRepository;
        }

        private async void UcCompanyManager_Load(object sender, EventArgs e)
        {
            try
            {
                label4.Text = $"Mã máy: {MachineCodeHelper.GetMachineCode()}";
                await LoadAndBindDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Tải dữ liệu công ty mới nhất từ Database và đổ lên UI
        /// </summary>
        private async Task LoadAndBindDataAsync()
        {
            _company = await _companyRepository.GetFirstCompanyAsync();
            BindDataToUI();
        }

        /// <summary>
        /// Đổ dữ liệu từ biến _company lên các TextBox trên UI
        /// </summary>
        private void BindDataToUI()
        {
            if (_company == null) return;

            txtNameCompany.Text = _company.Name ?? "";
            txtLisenCompany.Text = _company.Lisen ?? "";
            txtTimeWaitCompany.Text = _company.TimeWait.ToString();
            txtTimeFreeCompany.Text = _company.TimeFree.ToString();
            txtPathImage.Text = _company.PathImage ?? "";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                using VistaFolderBrowserDialog folderDialog = new()
                {
                    Description = "Chọn thư mục lưu file",
                    UseDescriptionForTitle = true,
                };

                Form? parentForm = FindForm();
                if (parentForm != null && folderDialog.ShowDialog(parentForm) == DialogResult.OK)
                {
                    txtPathImage.Text = folderDialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}\n\nStackTrace: {ex.StackTrace}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            try
            {
                List<TextBox> textBoxes = FrmHelpers.GetControls<TextBox>([grbCompany]);

                var validationResult = ValidationHelper.CheckControlsNotEmpty(textBoxes);
                if (!validationResult.IsValid)
                {
                    return;
                }

                // Hàm hỗ trợ đọc giá trị an toàn từ validation hoặc trực tiếp từ TextBox
                string GetValue(TextBox txt)
                {
                    if (validationResult.Values.TryGetValue(txt.Name, out string val))
                    {
                        return val;
                    }
                    return txt.Text.Trim();
                }

                Company companyReq = new()
                {
                    Name = GetValue(txtNameCompany),
                    Lisen = GetValue(txtLisenCompany),
                    TimeWait = int.Parse(GetValue(txtTimeWaitCompany)),
                    TimeFree = int.Parse(GetValue(txtTimeFreeCompany)),
                    PathImage = GetValue(txtPathImage)
                };

                bool success;
                if (_company != null)
                {
                    _company.Name = companyReq.Name;
                    _company.Lisen = companyReq.Lisen;
                    _company.TimeWait = companyReq.TimeWait;
                    _company.TimeFree = companyReq.TimeFree;
                    _company.PathImage = companyReq.PathImage;

                    success = await _companyRepository.UpdateCompanyAsync(_company);
                    if (success)
                    {
                        MessageBox.Show("Cập nhật thông tin thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Cập nhật thông tin thất bại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    success = await _companyRepository.CreateCompanyAsync(companyReq);
                    if (success)
                    {
                        MessageBox.Show("Tạo thông tin công ty thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Tạo thông tin công ty thất bại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // Tải lại dữ liệu mới nhất từ CSDL và hiển thị lại lên giao diện
                await LoadAndBindDataAsync();
            }
            catch (FormatException fEx)
            {
                MessageBox.Show($"Lỗi định dạng số (Thời gian chờ / Thời gian miễn phí): {fEx.Message}", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Sao chép nội dung của Label vào Clipboard
            try
            {
                Clipboard.SetText(label4.Text);
                button1.Text = "Đã Copy";

                // Reset text sau 2 giây
                var timer = new Timer { Interval = 2000 };
                timer.Tick += (s, args) =>
                {
                    button1.Text = "Copy";
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UcCompanyManager Clipboard Error]: {ex.Message}");
            }
        }
    }
}
