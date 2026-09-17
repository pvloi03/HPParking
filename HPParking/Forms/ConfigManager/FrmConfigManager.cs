using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace HPParking.Forms.ConfigManager
{
    public partial class FrmConfigManager : Form
    {
        private bool _isRestarting = false;
        private UcLanCarManager? _ucLaneCar;
        private UcLanMotoManager? _ucLaneMoto;

        public FrmConfigManager()
        {
            InitializeComponent();
            if (!DesignMode && LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                _ucLaneCar = Program.ServiceProvider!.GetRequiredService<UcLanCarManager>();
                _ucLaneMoto = Program.ServiceProvider!.GetRequiredService<UcLanMotoManager>();

                _ucLaneCar.Dock = DockStyle.Fill;
                _ucLaneMoto.Dock = DockStyle.Fill;

                tpLaneCar.Controls.Clear();
                tpLaneCar.Controls.Add(_ucLaneCar);

                tpLaneMoto.Controls.Clear();
                tpLaneMoto.Controls.Add(_ucLaneMoto);
            }
        }

        /// <summary>
        /// Gán thông tin Cổng để giới hạn phạm vi cấu hình thiết bị đúng Cổng của máy trạm này
        /// </summary>
        public void SetGateInfo(string? gateId, string? gateCode = null)
        {
            if (!string.IsNullOrWhiteSpace(gateCode))
            {
                Text = $"Quản Lý Cấu Hình Thiết Bị - CỔNG: {gateCode.ToUpperInvariant()}";
            }
            _ucLaneCar?.SetGateId(gateId);
            _ucLaneMoto?.SetGateId(gateId);
        }

        public void SetGateCode(string? gateCode) => SetGateInfo(null, gateCode);

        private void FrmConfigManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isRestarting)
                return;

            DialogResult confirm = MessageBox.Show(
                "Ứng dụng sẽ khởi động lại để áp dụng cấu hình mới. Bạn có chắc chắn muốn thoát?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            _isRestarting = true;

            Application.Restart();
            Environment.Exit(0);
        }
    }
}
