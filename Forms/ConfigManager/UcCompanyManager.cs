using HPParking.helper;
using HPParking.Helper;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Forms.CofigManager
{
    public partial class UcCompanyManager : UserControl
    {
        private readonly ICompanyRepository _companyRepository;
        private Company _company;

        public UcCompanyManager(ICompanyRepository companyRepository)
        {
            InitializeComponent();
            _companyRepository = companyRepository;
        }

        private async void UcCompanyManager_Load(object sender, EventArgs e)
        {
            label4.Text = $"Mã máy: {MachineCodeHelper.GetMachineCode()}";
            await LoadAndBindDataAsync();
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
            using FolderBrowserDialog folderDialog = new();
            folderDialog.Description = "Chọn thư mục lưu file";
            folderDialog.ShowNewFolderButton = true;

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtPathImage.Text = folderDialog.SelectedPath;
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

                if (_company != null)
                {
                    _company.Name = companyReq.Name;
                    _company.Lisen = companyReq.Lisen;
                    _company.TimeWait = companyReq.TimeWait;
                    _company.TimeFree = companyReq.TimeFree;
                    _company.PathImage = companyReq.PathImage;

                    bool resultUpdateCompany = await _companyRepository.UpdateCompanyAsync(_company);
                    if (resultUpdateCompany)
                    {
                        MessageBox.Show("Cập nhật thông tin thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    bool resultCreateCompany = await _companyRepository.CreateCompanyAsync(companyReq);
                    if (resultCreateCompany)
                    {
                        MessageBox.Show("Tạo thông tin công ty thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            Clipboard.SetText(label4.Text);
            button1.Text = "Đã Copy";
        }
    }
}