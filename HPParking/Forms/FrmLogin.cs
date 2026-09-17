using HPParking.Forms.ConfigManager;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmLogin : Form
    {
        private readonly string? _gateId;
        private readonly string? _gateCode;

        public FrmLogin(string? gateId = null, string? gateCode = null)
        {
            InitializeComponent();
            _gateId = gateId;
            _gateCode = gateCode;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string user = txtUserName.Text;
            string pass = txtPassWord.Text;
            if (user == "admin" && pass == "Hoangphat130225")
            {
                using FrmConfigManager frm = Program.ServiceProvider!.GetRequiredService<FrmConfigManager>();
                frm.SetGateInfo(_gateId, _gateCode);
                Hide();
                frm.ShowDialog(this);
                Close();
            }
            else
            {
                MessageBox.Show("Sai tài khoản hoặc mật khẩu.",
                    "Thông báo",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);
            }
        }
    }
}
