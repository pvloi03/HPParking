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
            tcConfigManager = new System.Windows.Forms.TabControl();
            tpLaneMoto = new System.Windows.Forms.TabPage();
            tpLaneCar = new System.Windows.Forms.TabPage();
            tcConfigManager.SuspendLayout();
            SuspendLayout();
            // 
            // tcConfigManager
            // 
            tcConfigManager.Controls.Add(tpLaneMoto);
            tcConfigManager.Controls.Add(tpLaneCar);
            tcConfigManager.Cursor = System.Windows.Forms.Cursors.Hand;
            tcConfigManager.Dock = System.Windows.Forms.DockStyle.Fill;
            tcConfigManager.Location = new System.Drawing.Point(0, 0);
            tcConfigManager.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tcConfigManager.Name = "tcConfigManager";
            tcConfigManager.SelectedIndex = 0;
            tcConfigManager.Size = new System.Drawing.Size(1188, 794);
            tcConfigManager.TabIndex = 0;
            // 
            // tpLaneMoto
            // 
            tpLaneMoto.Location = new System.Drawing.Point(4, 34);
            tpLaneMoto.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tpLaneMoto.Name = "tpLaneMoto";
            tpLaneMoto.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tpLaneMoto.Size = new System.Drawing.Size(1190, 756);
            tpLaneMoto.TabIndex = 0;
            tpLaneMoto.Text = "Làn Xe Máy";
            tpLaneMoto.UseVisualStyleBackColor = true;
            // 
            // tpLaneCar
            // 
            tpLaneCar.Location = new System.Drawing.Point(4, 34);
            tpLaneCar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tpLaneCar.Name = "tpLaneCar";
            tpLaneCar.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tpLaneCar.Size = new System.Drawing.Size(1190, 756);
            tpLaneCar.TabIndex = 1;
            tpLaneCar.Text = "Làn Ô tô";
            tpLaneCar.UseVisualStyleBackColor = true;
            // 
            // FrmConfigManager
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1188, 794);
            Controls.Add(tcConfigManager);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            MaximizeBox = false;
            Name = "FrmConfigManager";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "FrmConfigManager";
            FormClosing += FrmConfigManager_FormClosing;
            tcConfigManager.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tcConfigManager;
        private System.Windows.Forms.TabPage tpLaneMoto;
        private System.Windows.Forms.TabPage tpLaneCar;
    }
}