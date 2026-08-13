using HPParking.Forms.ConfigManager;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Forms;

namespace HPParking.Forms.ConfigManager
{
    public partial class FrmConfigManager : Form
    {
        private bool _isRestarting = false;
        public FrmConfigManager()
        {
            InitializeComponent();
            if (!DesignMode && LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                UcCompanyManager ucCompany = Program.ServiceProvider.GetRequiredService<UcCompanyManager>();
                UcLanCarManager ucLaneCar = Program.ServiceProvider.GetRequiredService<UcLanCarManager>();
                UcLanMotoManager ucLaneMoto = Program.ServiceProvider.GetRequiredService<UcLanMotoManager>();

                ucCompany.Dock = DockStyle.Fill;
                ucLaneCar.Dock = DockStyle.Fill;
                ucLaneMoto.Dock = DockStyle.Fill;

                tpCompany.Controls.Clear();
                tpCompany.Controls.Add(ucCompany);

                tpLaneCar.Controls.Clear();
                tpLaneCar.Controls.Add(ucLaneCar);

                tpLaneMoto.Controls.Clear();
                tpLaneMoto.Controls.Add(ucLaneMoto);
            }
        }

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
        }
    }
}
