namespace HPParking.Forms
{
    partial class FrmRegisterClient
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
            label1 = new System.Windows.Forms.Label();
            pnRegisterClient = new System.Windows.Forms.Panel();
            btnOpenCamera = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            lbImgFaceId = new System.Windows.Forms.Label();
            pbCapturedFace = new System.Windows.Forms.PictureBox();
            lbPlate = new System.Windows.Forms.Label();
            txtPlate = new System.Windows.Forms.TextBox();
            btnSave = new System.Windows.Forms.Button();
            pbAvatar = new System.Windows.Forms.PictureBox();
            lbDescription = new System.Windows.Forms.Label();
            txtDescription = new System.Windows.Forms.TextBox();
            lbTimeOut = new System.Windows.Forms.Label();
            dtpTimeOut = new System.Windows.Forms.DateTimePicker();
            lbTimeIn = new System.Windows.Forms.Label();
            dtpTimeIn = new System.Windows.Forms.DateTimePicker();
            dtpDateOfBirth = new System.Windows.Forms.DateTimePicker();
            rbFeMale = new System.Windows.Forms.RadioButton();
            rbMale = new System.Windows.Forms.RadioButton();
            lbIdCode = new System.Windows.Forms.Label();
            txtIdCode = new System.Windows.Forms.TextBox();
            lbPhoneNumber = new System.Windows.Forms.Label();
            txtPhoneNumber = new System.Windows.Forms.TextBox();
            lbDateOfBirth = new System.Windows.Forms.Label();
            lbAddress = new System.Windows.Forms.Label();
            txtAddress = new System.Windows.Forms.TextBox();
            lbName = new System.Windows.Forms.Label();
            txtName = new System.Windows.Forms.TextBox();
            lblStatus = new System.Windows.Forms.Label();
            pnRegisterClient.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbCapturedFace).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbAvatar).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label1.Location = new System.Drawing.Point(469, 15);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(237, 29);
            label1.TabIndex = 0;
            label1.Text = "Đăng Ký Thông Tin";
            // 
            // pnRegisterClient
            // 
            pnRegisterClient.BackColor = System.Drawing.Color.White;
            pnRegisterClient.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnRegisterClient.Controls.Add(btnOpenCamera);
            pnRegisterClient.Controls.Add(label2);
            pnRegisterClient.Controls.Add(lbImgFaceId);
            pnRegisterClient.Controls.Add(pbCapturedFace);
            pnRegisterClient.Controls.Add(lbPlate);
            pnRegisterClient.Controls.Add(txtPlate);
            pnRegisterClient.Controls.Add(btnSave);
            pnRegisterClient.Controls.Add(pbAvatar);
            pnRegisterClient.Controls.Add(lbDescription);
            pnRegisterClient.Controls.Add(txtDescription);
            pnRegisterClient.Controls.Add(lbTimeOut);
            pnRegisterClient.Controls.Add(dtpTimeOut);
            pnRegisterClient.Controls.Add(lbTimeIn);
            pnRegisterClient.Controls.Add(dtpTimeIn);
            pnRegisterClient.Controls.Add(dtpDateOfBirth);
            pnRegisterClient.Controls.Add(rbFeMale);
            pnRegisterClient.Controls.Add(rbMale);
            pnRegisterClient.Controls.Add(lbIdCode);
            pnRegisterClient.Controls.Add(txtIdCode);
            pnRegisterClient.Controls.Add(lbPhoneNumber);
            pnRegisterClient.Controls.Add(txtPhoneNumber);
            pnRegisterClient.Controls.Add(lbDateOfBirth);
            pnRegisterClient.Controls.Add(lbAddress);
            pnRegisterClient.Controls.Add(txtAddress);
            pnRegisterClient.Controls.Add(lbName);
            pnRegisterClient.Controls.Add(txtName);
            pnRegisterClient.Location = new System.Drawing.Point(152, 70);
            pnRegisterClient.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pnRegisterClient.Name = "pnRegisterClient";
            pnRegisterClient.Size = new System.Drawing.Size(896, 663);
            pnRegisterClient.TabIndex = 12;
            // 
            // btnOpenCamera
            // 
            btnOpenCamera.AutoSize = true;
            btnOpenCamera.BackColor = System.Drawing.Color.Coral;
            btnOpenCamera.Cursor = System.Windows.Forms.Cursors.Hand;
            btnOpenCamera.FlatAppearance.BorderSize = 0;
            btnOpenCamera.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnOpenCamera.ForeColor = System.Drawing.Color.White;
            btnOpenCamera.Location = new System.Drawing.Point(638, 590);
            btnOpenCamera.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            btnOpenCamera.Name = "btnOpenCamera";
            btnOpenCamera.Size = new System.Drawing.Size(118, 45);
            btnOpenCamera.TabIndex = 14;
            btnOpenCamera.Text = "Chụp ảnh";
            btnOpenCamera.UseVisualStyleBackColor = false;
            btnOpenCamera.Click += btnOpenCamera_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(464, 245);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(95, 25);
            label2.TabIndex = 40;
            label2.Text = "Ảnh CCCD";
            // 
            // lbImgFaceId
            // 
            lbImgFaceId.AutoSize = true;
            lbImgFaceId.Location = new System.Drawing.Point(671, 245);
            lbImgFaceId.Name = "lbImgFaceId";
            lbImgFaceId.Size = new System.Drawing.Size(163, 25);
            lbImgFaceId.TabIndex = 39;
            lbImgFaceId.Text = "Ảnh đăng kí FaceId";
            // 
            // pbCapturedFace
            // 
            pbCapturedFace.BackColor = System.Drawing.Color.WhiteSmoke;
            pbCapturedFace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pbCapturedFace.Location = new System.Drawing.Point(676, 274);
            pbCapturedFace.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pbCapturedFace.Name = "pbCapturedFace";
            pbCapturedFace.Size = new System.Drawing.Size(195, 283);
            pbCapturedFace.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pbCapturedFace.TabIndex = 38;
            pbCapturedFace.TabStop = false;
            // 
            // lbPlate
            // 
            lbPlate.AutoSize = true;
            lbPlate.Location = new System.Drawing.Point(462, 15);
            lbPlate.Name = "lbPlate";
            lbPlate.Size = new System.Drawing.Size(91, 25);
            lbPlate.TabIndex = 36;
            lbPlate.Text = "Biển số xe";
            // 
            // txtPlate
            // 
            txtPlate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtPlate.Location = new System.Drawing.Point(467, 44);
            txtPlate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtPlate.Multiline = true;
            txtPlate.Name = "txtPlate";
            txtPlate.Size = new System.Drawing.Size(402, 37);
            txtPlate.TabIndex = 35;
            // 
            // btnSave
            // 
            btnSave.BackColor = System.Drawing.Color.Teal;
            btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            btnSave.ForeColor = System.Drawing.Color.White;
            btnSave.Location = new System.Drawing.Point(762, 590);
            btnSave.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(107, 45);
            btnSave.TabIndex = 23;
            btnSave.Text = "Lưu";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // pbAvatar
            // 
            pbAvatar.BackColor = System.Drawing.Color.WhiteSmoke;
            pbAvatar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pbAvatar.Location = new System.Drawing.Point(469, 274);
            pbAvatar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pbAvatar.Name = "pbAvatar";
            pbAvatar.Size = new System.Drawing.Size(195, 283);
            pbAvatar.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pbAvatar.TabIndex = 22;
            pbAvatar.TabStop = false;
            // 
            // lbDescription
            // 
            lbDescription.AutoSize = true;
            lbDescription.Location = new System.Drawing.Point(464, 90);
            lbDescription.Name = "lbDescription";
            lbDescription.Size = new System.Drawing.Size(59, 25);
            lbDescription.TabIndex = 34;
            lbDescription.Text = "Mô tả";
            // 
            // txtDescription
            // 
            txtDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtDescription.Location = new System.Drawing.Point(467, 119);
            txtDescription.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtDescription.Multiline = true;
            txtDescription.Name = "txtDescription";
            txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            txtDescription.Size = new System.Drawing.Size(402, 88);
            txtDescription.TabIndex = 33;
            // 
            // lbTimeOut
            // 
            lbTimeOut.AutoSize = true;
            lbTimeOut.Location = new System.Drawing.Point(18, 511);
            lbTimeOut.Name = "lbTimeOut";
            lbTimeOut.Size = new System.Drawing.Size(74, 25);
            lbTimeOut.TabIndex = 32;
            lbTimeOut.Text = "Ngày ra";
            // 
            // dtpTimeOut
            // 
            dtpTimeOut.CustomFormat = "dd/MM/yyyy";
            dtpTimeOut.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            dtpTimeOut.Location = new System.Drawing.Point(21, 540);
            dtpTimeOut.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dtpTimeOut.Name = "dtpTimeOut";
            dtpTimeOut.Size = new System.Drawing.Size(401, 31);
            dtpTimeOut.TabIndex = 31;
            // 
            // lbTimeIn
            // 
            lbTimeIn.AutoSize = true;
            lbTimeIn.Location = new System.Drawing.Point(16, 441);
            lbTimeIn.Name = "lbTimeIn";
            lbTimeIn.Size = new System.Drawing.Size(88, 25);
            lbTimeIn.TabIndex = 30;
            lbTimeIn.Text = "Ngày vào";
            // 
            // dtpTimeIn
            // 
            dtpTimeIn.CustomFormat = "dd/MM/yyyy";
            dtpTimeIn.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            dtpTimeIn.Location = new System.Drawing.Point(20, 470);
            dtpTimeIn.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dtpTimeIn.Name = "dtpTimeIn";
            dtpTimeIn.Size = new System.Drawing.Size(402, 31);
            dtpTimeIn.TabIndex = 29;
            dtpTimeIn.Tag = "Ngày vào";
            // 
            // dtpDateOfBirth
            // 
            dtpDateOfBirth.Cursor = System.Windows.Forms.Cursors.Hand;
            dtpDateOfBirth.CustomFormat = "dd/MM/yyyy";
            dtpDateOfBirth.Enabled = false;
            dtpDateOfBirth.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            dtpDateOfBirth.Location = new System.Drawing.Point(20, 199);
            dtpDateOfBirth.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dtpDateOfBirth.MinimumSize = new System.Drawing.Size(4, 30);
            dtpDateOfBirth.Name = "dtpDateOfBirth";
            dtpDateOfBirth.Size = new System.Drawing.Size(401, 31);
            dtpDateOfBirth.TabIndex = 28;
            // 
            // rbFeMale
            // 
            rbFeMale.AutoSize = true;
            rbFeMale.BackColor = System.Drawing.Color.White;
            rbFeMale.Cursor = System.Windows.Forms.Cursors.Hand;
            rbFeMale.Enabled = false;
            rbFeMale.Location = new System.Drawing.Point(100, 319);
            rbFeMale.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            rbFeMale.Name = "rbFeMale";
            rbFeMale.Size = new System.Drawing.Size(61, 29);
            rbFeMale.TabIndex = 27;
            rbFeMale.TabStop = true;
            rbFeMale.Text = "Nữ";
            rbFeMale.UseVisualStyleBackColor = false;
            // 
            // rbMale
            // 
            rbMale.AutoSize = true;
            rbMale.BackColor = System.Drawing.Color.White;
            rbMale.Cursor = System.Windows.Forms.Cursors.Hand;
            rbMale.Enabled = false;
            rbMale.Location = new System.Drawing.Point(19, 319);
            rbMale.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            rbMale.Name = "rbMale";
            rbMale.Size = new System.Drawing.Size(75, 29);
            rbMale.TabIndex = 26;
            rbMale.TabStop = true;
            rbMale.Text = "Nam";
            rbMale.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            rbMale.UseVisualStyleBackColor = false;
            // 
            // lbIdCode
            // 
            lbIdCode.AutoSize = true;
            lbIdCode.Location = new System.Drawing.Point(17, 15);
            lbIdCode.Name = "lbIdCode";
            lbIdCode.Size = new System.Drawing.Size(84, 25);
            lbIdCode.TabIndex = 25;
            lbIdCode.Text = "Số CCCD";
            // 
            // txtIdCode
            // 
            txtIdCode.BackColor = System.Drawing.Color.White;
            txtIdCode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtIdCode.Enabled = false;
            txtIdCode.Location = new System.Drawing.Point(21, 44);
            txtIdCode.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtIdCode.Multiline = true;
            txtIdCode.Name = "txtIdCode";
            txtIdCode.ReadOnly = true;
            txtIdCode.Size = new System.Drawing.Size(402, 37);
            txtIdCode.TabIndex = 24;
            // 
            // lbPhoneNumber
            // 
            lbPhoneNumber.AutoSize = true;
            lbPhoneNumber.Location = new System.Drawing.Point(14, 360);
            lbPhoneNumber.Name = "lbPhoneNumber";
            lbPhoneNumber.Size = new System.Drawing.Size(117, 25);
            lbPhoneNumber.TabIndex = 21;
            lbPhoneNumber.Text = "Số điện thoại";
            // 
            // txtPhoneNumber
            // 
            txtPhoneNumber.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtPhoneNumber.Location = new System.Drawing.Point(19, 389);
            txtPhoneNumber.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtPhoneNumber.Multiline = true;
            txtPhoneNumber.Name = "txtPhoneNumber";
            txtPhoneNumber.Size = new System.Drawing.Size(402, 37);
            txtPhoneNumber.TabIndex = 20;
            // 
            // lbDateOfBirth
            // 
            lbDateOfBirth.AutoSize = true;
            lbDateOfBirth.Location = new System.Drawing.Point(16, 170);
            lbDateOfBirth.Name = "lbDateOfBirth";
            lbDateOfBirth.Size = new System.Drawing.Size(91, 25);
            lbDateOfBirth.TabIndex = 19;
            lbDateOfBirth.Text = "Ngày sinh";
            // 
            // lbAddress
            // 
            lbAddress.AutoSize = true;
            lbAddress.Location = new System.Drawing.Point(14, 245);
            lbAddress.Name = "lbAddress";
            lbAddress.Size = new System.Drawing.Size(65, 25);
            lbAddress.TabIndex = 15;
            lbAddress.Text = "Địa chỉ";
            // 
            // txtAddress
            // 
            txtAddress.BackColor = System.Drawing.Color.White;
            txtAddress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtAddress.Enabled = false;
            txtAddress.Location = new System.Drawing.Point(19, 274);
            txtAddress.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtAddress.Multiline = true;
            txtAddress.Name = "txtAddress";
            txtAddress.ReadOnly = true;
            txtAddress.Size = new System.Drawing.Size(402, 37);
            txtAddress.TabIndex = 14;
            // 
            // lbName
            // 
            lbName.AutoSize = true;
            lbName.Location = new System.Drawing.Point(17, 90);
            lbName.Name = "lbName";
            lbName.Size = new System.Drawing.Size(89, 25);
            lbName.TabIndex = 13;
            lbName.Text = "Họ và tên";
            // 
            // txtName
            // 
            txtName.BackColor = System.Drawing.Color.White;
            txtName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtName.Enabled = false;
            txtName.Location = new System.Drawing.Point(21, 119);
            txtName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtName.Multiline = true;
            txtName.Name = "txtName";
            txtName.ReadOnly = true;
            txtName.Size = new System.Drawing.Size(402, 37);
            txtName.TabIndex = 12;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new System.Drawing.Point(152, 42);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(126, 25);
            lblStatus.TabIndex = 13;
            lblStatus.Text = "Đang kết nối...";
            // 
            // FrmRegisterClient
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new System.Drawing.Size(1200, 755);
            Controls.Add(lblStatus);
            Controls.Add(pnRegisterClient);
            Controls.Add(label1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            MaximizeBox = false;
            Name = "FrmRegisterClient";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Đăng ký thông tin";
            FormClosing += FrmRegisterClient_FormClosing;
            Load += FrmRegisterClient_Load;
            pnRegisterClient.ResumeLayout(false);
            pnRegisterClient.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbCapturedFace).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbAvatar).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnRegisterClient;
        private System.Windows.Forms.PictureBox pbAvatar;
        private System.Windows.Forms.Label lbPhoneNumber;
        private System.Windows.Forms.TextBox txtPhoneNumber;
        private System.Windows.Forms.Label lbDateOfBirth;
        private System.Windows.Forms.Label lbAddress;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.Label lbName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lbIdCode;
        private System.Windows.Forms.TextBox txtIdCode;
        private System.Windows.Forms.RadioButton rbFeMale;
        private System.Windows.Forms.RadioButton rbMale;
        private System.Windows.Forms.DateTimePicker dtpDateOfBirth;
        private System.Windows.Forms.Label lbTimeIn;
        private System.Windows.Forms.DateTimePicker dtpTimeIn;
        private System.Windows.Forms.Label lbTimeOut;
        private System.Windows.Forms.DateTimePicker dtpTimeOut;
        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.Label lbPlate;
        private System.Windows.Forms.TextBox txtPlate;
        private System.Windows.Forms.PictureBox pbCapturedFace;
        private System.Windows.Forms.Label lbImgFaceId;
        private System.Windows.Forms.Button btnOpenCamera;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblStatus;
    }
}