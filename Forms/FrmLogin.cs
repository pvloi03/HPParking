using HPParking.Forms.ConfigManager;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmLogin : Form
    {
        public FrmLogin()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string user = txtUserName.Text;
            string pass = txtPassWord.Text;
            if (user == "admin" && pass == "Hoangphat130225")
            {
                using FrmConfigManager frm = Program.ServiceProvider!.GetRequiredService<FrmConfigManager>();
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
