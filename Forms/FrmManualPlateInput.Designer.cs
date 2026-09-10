namespace HPParking.Forms
{
    partial class FrmManualPlateInput
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
            lblInfo = new System.Windows.Forms.Label();
            txtPlate = new System.Windows.Forms.TextBox();
            btnConfirm = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // lblInfo
            // 
            lblInfo.Dock = System.Windows.Forms.DockStyle.Top;
            lblInfo.Font = new System.Drawing.Font("Segoe UI", 10F);
            lblInfo.ForeColor = System.Drawing.Color.FromArgb(40, 40, 40);
            lblInfo.Location = new System.Drawing.Point(0, 0);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new System.Drawing.Size(467, 100);
            lblInfo.TabIndex = 0;
            lblInfo.Text = "Làn: ...\r\nLý do: Camera lỗi hoặc không nhận diện được\r\nVui lòng nhập biển số xe thực tế:";
            // 
            // txtPlate
            // 
            txtPlate.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            txtPlate.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            txtPlate.Location = new System.Drawing.Point(43, 131);
            txtPlate.Name = "txtPlate";
            txtPlate.Size = new System.Drawing.Size(380, 50);
            txtPlate.TabIndex = 1;
            txtPlate.KeyDown += txtPlate_KeyDown;
            // 
            // btnConfirm
            // 
            btnConfirm.BackColor = System.Drawing.Color.SeaGreen;
            btnConfirm.Cursor = System.Windows.Forms.Cursors.Hand;
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnConfirm.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnConfirm.ForeColor = System.Drawing.Color.White;
            btnConfirm.Location = new System.Drawing.Point(88, 196);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.Size = new System.Drawing.Size(150, 42);
            btnConfirm.TabIndex = 2;
            btnConfirm.Text = "Xác nhận (Enter)";
            btnConfirm.UseVisualStyleBackColor = false;
            btnConfirm.Click += btnConfirm_Click;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = System.Drawing.Color.Crimson;
            btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnCancel.ForeColor = System.Drawing.Color.White;
            btnCancel.Location = new System.Drawing.Point(248, 196);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(130, 42);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Hủy bỏ (Esc)";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // FrmManualPlateInput
            // 
            AcceptButton = btnConfirm;
            AutoScaleDimensions = new System.Drawing.SizeF(11F, 28F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(467, 290);
            Controls.Add(btnCancel);
            Controls.Add(btnConfirm);
            Controls.Add(txtPlate);
            Controls.Add(lblInfo);
            Font = new System.Drawing.Font("Segoe UI", 10F);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmManualPlateInput";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Nhập Biển Số Thủ Công";
            Shown += FrmManualPlateInput_Shown;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Label lblInfo;
        public System.Windows.Forms.TextBox txtPlate;
        public System.Windows.Forms.Button btnConfirm;
        public System.Windows.Forms.Button btnCancel;
    }
}
