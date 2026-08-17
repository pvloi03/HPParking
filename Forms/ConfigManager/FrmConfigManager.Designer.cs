namespace HPParking.Forms.ConfigManager
{
    partial class FrmConfigManager
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tcConfigManager = new System.Windows.Forms.TabControl();
            this.tpCompany = new System.Windows.Forms.TabPage();
            this.tpLaneMoto = new System.Windows.Forms.TabPage();
            this.tpLaneCar = new System.Windows.Forms.TabPage();
            this.tcConfigManager.SuspendLayout();
            this.SuspendLayout();
            // 
            // tcConfigManager
            // 
            this.tcConfigManager.Controls.Add(this.tpCompany);
            this.tcConfigManager.Controls.Add(this.tpLaneMoto);
            this.tcConfigManager.Controls.Add(this.tpLaneCar);
            this.tcConfigManager.Cursor = System.Windows.Forms.Cursors.Hand;
            this.tcConfigManager.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcConfigManager.Location = new System.Drawing.Point(0, 0);
            this.tcConfigManager.Name = "tcConfigManager";
            this.tcConfigManager.SelectedIndex = 0;
            this.tcConfigManager.Size = new System.Drawing.Size(1034, 545);
            this.tcConfigManager.TabIndex = 0;
            // 
            // tpCompany
            // 
            this.tpCompany.Cursor = System.Windows.Forms.Cursors.Default;
            this.tpCompany.Location = new System.Drawing.Point(4, 29);
            this.tpCompany.Name = "tpCompany";
            this.tpCompany.Padding = new System.Windows.Forms.Padding(3);
            this.tpCompany.Size = new System.Drawing.Size(1026, 512);
            this.tpCompany.TabIndex = 0;
            this.tpCompany.Text = "Phân Mềm";
            this.tpCompany.UseVisualStyleBackColor = true;
            // 
            // tpLaneMoto
            // 
            this.tpLaneMoto.Location = new System.Drawing.Point(4, 29);
            this.tpLaneMoto.Name = "tpLaneMoto";
            this.tpLaneMoto.Padding = new System.Windows.Forms.Padding(3);
            this.tpLaneMoto.Size = new System.Drawing.Size(1056, 480);
            this.tpLaneMoto.TabIndex = 1;
            this.tpLaneMoto.Text = "Làn Xe Máy";
            this.tpLaneMoto.UseVisualStyleBackColor = true;
            // 
            // tpLaneCar
            // 
            this.tpLaneCar.Location = new System.Drawing.Point(4, 29);
            this.tpLaneCar.Name = "tpLaneCar";
            this.tpLaneCar.Padding = new System.Windows.Forms.Padding(3);
            this.tpLaneCar.Size = new System.Drawing.Size(1056, 480);
            this.tpLaneCar.TabIndex = 2;
            this.tpLaneCar.Text = "Làn Ô tô";
            this.tpLaneCar.UseVisualStyleBackColor = true;
            // 
            // FrmConfigManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1034, 545);
            this.Controls.Add(this.tcConfigManager);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FrmConfigManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FrmConfigManager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmConfigManager_FormClosing);
            this.tcConfigManager.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tcConfigManager;
        private System.Windows.Forms.TabPage tpCompany;
        private System.Windows.Forms.TabPage tpLaneMoto;
        private System.Windows.Forms.TabPage tpLaneCar;
    }
}