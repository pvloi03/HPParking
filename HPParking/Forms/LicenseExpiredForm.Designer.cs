namespace HPParking.Forms
{
    partial class LicenseExpiredForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlHeader = new System.Windows.Forms.Panel();
            lblTitle = new System.Windows.Forms.Label();
            pnlBody = new System.Windows.Forms.Panel();
            btnExit = new System.Windows.Forms.Button();
            btnActivate = new System.Windows.Forms.Button();
            grpKeyInput = new System.Windows.Forms.GroupBox();
            btnBrowseFile = new System.Windows.Forms.Button();
            txtLicenseKey = new System.Windows.Forms.TextBox();
            lblKeyHelp = new System.Windows.Forms.Label();
            grpMachine = new System.Windows.Forms.GroupBox();
            btnCopyMachineCode = new System.Windows.Forms.Button();
            txtMachineCode = new System.Windows.Forms.TextBox();
            lblMachineHelp = new System.Windows.Forms.Label();
            pnlHeader.SuspendLayout();
            pnlBody.SuspendLayout();
            grpKeyInput.SuspendLayout();
            grpMachine.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = System.Drawing.Color.FromArgb(225, 29, 72);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            pnlHeader.Location = new System.Drawing.Point(0, 0);
            pnlHeader.Margin = new System.Windows.Forms.Padding(4);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
            pnlHeader.Size = new System.Drawing.Size(1118, 92);
            pnlHeader.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.Color.White;
            lblTitle.Location = new System.Drawing.Point(20, 15);
            lblTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(1078, 62);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "🔒BẢN QUYỀN PHẦN MỀM";
            lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlBody
            // 
            pnlBody.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            pnlBody.Controls.Add(btnExit);
            pnlBody.Controls.Add(btnActivate);
            pnlBody.Controls.Add(grpKeyInput);
            pnlBody.Controls.Add(grpMachine);
            pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlBody.Location = new System.Drawing.Point(0, 92);
            pnlBody.Margin = new System.Windows.Forms.Padding(4);
            pnlBody.Name = "pnlBody";
            pnlBody.Padding = new System.Windows.Forms.Padding(20);
            pnlBody.Size = new System.Drawing.Size(1118, 583);
            pnlBody.TabIndex = 1;
            // 
            // btnExit
            // 
            btnExit.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnExit.BackColor = System.Drawing.Color.FromArgb(239, 68, 68);
            btnExit.Cursor = System.Windows.Forms.Cursors.Hand;
            btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnExit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            btnExit.ForeColor = System.Drawing.Color.White;
            btnExit.Location = new System.Drawing.Point(714, 509);
            btnExit.Margin = new System.Windows.Forms.Padding(4);
            btnExit.Name = "btnExit";
            btnExit.Size = new System.Drawing.Size(157, 50);
            btnExit.TabIndex = 3;
            btnExit.Text = "❌ Thoát";
            btnExit.UseVisualStyleBackColor = false;
            btnExit.Click += btnExit_Click;
            // 
            // btnActivate
            // 
            btnActivate.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnActivate.BackColor = System.Drawing.Color.FromArgb(16, 185, 129);
            btnActivate.Cursor = System.Windows.Forms.Cursors.Hand;
            btnActivate.FlatAppearance.BorderSize = 0;
            btnActivate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnActivate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            btnActivate.ForeColor = System.Drawing.Color.White;
            btnActivate.Location = new System.Drawing.Point(877, 509);
            btnActivate.Margin = new System.Windows.Forms.Padding(4);
            btnActivate.Name = "btnActivate";
            btnActivate.Size = new System.Drawing.Size(221, 50);
            btnActivate.TabIndex = 2;
            btnActivate.Text = "⚡ KÍCH HOẠT NGAY";
            btnActivate.UseVisualStyleBackColor = false;
            btnActivate.Click += btnActivate_Click;
            // 
            // grpKeyInput
            // 
            grpKeyInput.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpKeyInput.Controls.Add(btnBrowseFile);
            grpKeyInput.Controls.Add(txtLicenseKey);
            grpKeyInput.Controls.Add(lblKeyHelp);
            grpKeyInput.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            grpKeyInput.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            grpKeyInput.Location = new System.Drawing.Point(20, 150);
            grpKeyInput.Margin = new System.Windows.Forms.Padding(4);
            grpKeyInput.Name = "grpKeyInput";
            grpKeyInput.Padding = new System.Windows.Forms.Padding(15);
            grpKeyInput.Size = new System.Drawing.Size(1078, 346);
            grpKeyInput.TabIndex = 1;
            grpKeyInput.TabStop = false;
            grpKeyInput.Text = "2. Nạp License Key Hoặc File Bản Quyền Mới";
            // 
            // btnBrowseFile
            // 
            btnBrowseFile.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnBrowseFile.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            btnBrowseFile.Cursor = System.Windows.Forms.Cursors.Hand;
            btnBrowseFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnBrowseFile.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            btnBrowseFile.ForeColor = System.Drawing.Color.White;
            btnBrowseFile.Location = new System.Drawing.Point(876, 25);
            btnBrowseFile.Margin = new System.Windows.Forms.Padding(4);
            btnBrowseFile.Name = "btnBrowseFile";
            btnBrowseFile.Size = new System.Drawing.Size(188, 40);
            btnBrowseFile.TabIndex = 2;
            btnBrowseFile.Text = "📂 Chọn File (.lic)";
            btnBrowseFile.UseVisualStyleBackColor = false;
            btnBrowseFile.Click += btnBrowseFile_Click;
            // 
            // txtLicenseKey
            // 
            txtLicenseKey.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtLicenseKey.BackColor = System.Drawing.Color.White;
            txtLicenseKey.Font = new System.Drawing.Font("Consolas", 9F);
            txtLicenseKey.Location = new System.Drawing.Point(15, 72);
            txtLicenseKey.Margin = new System.Windows.Forms.Padding(4);
            txtLicenseKey.Multiline = true;
            txtLicenseKey.Name = "txtLicenseKey";
            txtLicenseKey.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtLicenseKey.Size = new System.Drawing.Size(1047, 257);
            txtLicenseKey.TabIndex = 1;
            // 
            // lblKeyHelp
            // 
            lblKeyHelp.AutoSize = true;
            lblKeyHelp.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            lblKeyHelp.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            lblKeyHelp.Location = new System.Drawing.Point(15, 35);
            lblKeyHelp.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblKeyHelp.Name = "lblKeyHelp";
            lblKeyHelp.Size = new System.Drawing.Size(521, 23);
            lblKeyHelp.TabIndex = 0;
            lblKeyHelp.Text = "Dán chuỗi License Key hoặc bấm nút Chọn File .lic để nạp tự động:";
            // 
            // grpMachine
            // 
            grpMachine.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpMachine.Controls.Add(btnCopyMachineCode);
            grpMachine.Controls.Add(txtMachineCode);
            grpMachine.Controls.Add(lblMachineHelp);
            grpMachine.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            grpMachine.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            grpMachine.Location = new System.Drawing.Point(20, 15);
            grpMachine.Margin = new System.Windows.Forms.Padding(4);
            grpMachine.Name = "grpMachine";
            grpMachine.Padding = new System.Windows.Forms.Padding(15);
            grpMachine.Size = new System.Drawing.Size(1078, 125);
            grpMachine.TabIndex = 0;
            grpMachine.TabStop = false;
            grpMachine.Text = "1. Mã Máy Tính Của Trạm Này (Machine Code)";
            // 
            // btnCopyMachineCode
            // 
            btnCopyMachineCode.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnCopyMachineCode.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            btnCopyMachineCode.Cursor = System.Windows.Forms.Cursors.Hand;
            btnCopyMachineCode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnCopyMachineCode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnCopyMachineCode.ForeColor = System.Drawing.Color.White;
            btnCopyMachineCode.Location = new System.Drawing.Point(876, 65);
            btnCopyMachineCode.Margin = new System.Windows.Forms.Padding(4);
            btnCopyMachineCode.Name = "btnCopyMachineCode";
            btnCopyMachineCode.Size = new System.Drawing.Size(188, 40);
            btnCopyMachineCode.TabIndex = 2;
            btnCopyMachineCode.Text = "📋 Sao Chép Mã";
            btnCopyMachineCode.UseVisualStyleBackColor = false;
            btnCopyMachineCode.Click += btnCopyMachineCode_Click;
            // 
            // txtMachineCode
            // 
            txtMachineCode.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtMachineCode.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            txtMachineCode.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            txtMachineCode.ForeColor = System.Drawing.Color.FromArgb(30, 58, 138);
            txtMachineCode.Location = new System.Drawing.Point(15, 68);
            txtMachineCode.Margin = new System.Windows.Forms.Padding(4);
            txtMachineCode.Name = "txtMachineCode";
            txtMachineCode.ReadOnly = true;
            txtMachineCode.Size = new System.Drawing.Size(843, 36);
            txtMachineCode.TabIndex = 1;
            txtMachineCode.Text = "PX-0000-0000-0000-0000";
            // 
            // lblMachineHelp
            // 
            lblMachineHelp.AutoSize = true;
            lblMachineHelp.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            lblMachineHelp.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            lblMachineHelp.Location = new System.Drawing.Point(15, 35);
            lblMachineHelp.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblMachineHelp.Name = "lblMachineHelp";
            lblMachineHelp.Size = new System.Drawing.Size(676, 23);
            lblMachineHelp.TabIndex = 0;
            lblMachineHelp.Text = "Sao chép mã máy tính bên dưới và gửi cho nhà cung cấp để được cấp License Key mới:";
            // 
            // LicenseExpiredForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.White;
            ClientSize = new System.Drawing.Size(1118, 675);
            Controls.Add(pnlBody);
            Controls.Add(pnlHeader);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Margin = new System.Windows.Forms.Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LicenseExpiredForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Quản lý thông tin bản quyền";
            pnlHeader.ResumeLayout(false);
            pnlBody.ResumeLayout(false);
            grpKeyInput.ResumeLayout(false);
            grpKeyInput.PerformLayout();
            grpMachine.ResumeLayout(false);
            grpMachine.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlBody;
        private System.Windows.Forms.GroupBox grpMachine;
        private System.Windows.Forms.Label lblMachineHelp;
        private System.Windows.Forms.TextBox txtMachineCode;
        private System.Windows.Forms.Button btnCopyMachineCode;
        private System.Windows.Forms.GroupBox grpKeyInput;
        private System.Windows.Forms.Label lblKeyHelp;
        private System.Windows.Forms.TextBox txtLicenseKey;
        private System.Windows.Forms.Button btnBrowseFile;
        private System.Windows.Forms.Button btnActivate;
        private System.Windows.Forms.Button btnExit;
    }
}
