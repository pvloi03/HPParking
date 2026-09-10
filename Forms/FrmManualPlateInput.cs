using HPParking.Models.Entities;
using System;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmManualPlateInput : Form
    {
        private readonly string? _expectedPlate;
        private readonly string? _mismatchMessage;

        public string? EnteredPlate { get; private set; }

        public FrmManualPlateInput()
        {
            InitializeComponent();
        }

        public FrmManualPlateInput(
            Lane lane,
            string reason = "Camera biển số lỗi hoặc không nhận diện được",
            string? expectedPlate = null,
            string? mismatchMessage = null) : this()
        {
            _expectedPlate = expectedPlate;
            _mismatchMessage = mismatchMessage;

            string laneDesc = $"{(lane.InputReader % 2 != 0 ? "Xe máy" : "Ô tô")} - Cổng {(lane.Type % 2 != 0 ? "VÀO" : "RA")} (Đầu đọc {lane.InputReader})";
            lblInfo.Text = $"Làn: {laneDesc}\nLý do: {reason}\nVui lòng nhập biển số xe thực tế:";
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            string text = txtPlate.Text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(this, "Vui lòng nhập biển số hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPlate.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(_expectedPlate))
            {
                string cleanExpected = _expectedPlate.Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
                string cleanEntered = text.Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
                if (cleanExpected != cleanEntered)
                {
                    string msg = !string.IsNullOrEmpty(_mismatchMessage)
                        ? _mismatchMessage
                        : "Biển số xe không đúng với biển số đăng ký.";
                    MessageBox.Show(this, msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPlate.Focus();
                    txtPlate.SelectAll();
                    return; // Không đóng form, cho phép người dùng nhập lại ngay lập tức
                }
            }

            EnteredPlate = text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            EnteredPlate = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void txtPlate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnConfirm.PerformClick();
            }
        }

        private void FrmManualPlateInput_Shown(object sender, EventArgs e)
        {
            txtPlate.Focus();
        }

        public static string? Prompt(
            IWin32Window? owner,
            Lane lane,
            string reason = "Camera lỗi hoặc không nhận diện được",
            string? expectedPlate = null,
            string? mismatchMessage = null)
        {
            using var form = new FrmManualPlateInput(lane, reason, expectedPlate, mismatchMessage);
            var result = form.ShowDialog(owner);
            return result == DialogResult.OK && !string.IsNullOrWhiteSpace(form.EnteredPlate) ? form.EnteredPlate : null;
        }
    }
}
