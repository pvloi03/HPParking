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
            this.label1 = new System.Windows.Forms.Label();
            this.pnRegisterClient = new System.Windows.Forms.Panel();
            this.btnOpenCamera = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.lbImgFaceId = new System.Windows.Forms.Label();
            this.pbCapturedFace = new System.Windows.Forms.PictureBox();
            this.lbPlate = new System.Windows.Forms.Label();
            this.txtPlate = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.pbAvatar = new System.Windows.Forms.PictureBox();
            this.lbDescription = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.lbTimeOut = new System.Windows.Forms.Label();
            this.dtpTimeOut = new System.Windows.Forms.DateTimePicker();
            this.lbTimeIn = new System.Windows.Forms.Label();
            this.dtpTimeIn = new System.Windows.Forms.DateTimePicker();
            this.dtpDateOfBirth = new System.Windows.Forms.DateTimePicker();
            this.rbFeMale = new System.Windows.Forms.RadioButton();
            this.rbMale = new System.Windows.Forms.RadioButton();
            this.lbIdCode = new System.Windows.Forms.Label();
            this.txtIdCode = new System.Windows.Forms.TextBox();
            this.lbPhoneNumber = new System.Windows.Forms.Label();
            this.txtPhoneNumber = new System.Windows.Forms.TextBox();
            this.lbDateOfBirth = new System.Windows.Forms.Label();
            this.lbAddress = new System.Windows.Forms.Label();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.lbName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnRegisterClient.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCapturedFace)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAvatar)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(422, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(237, 29);
            this.label1.TabIndex = 0;
            this.label1.Text = "Đăng Ký Thông Tin";
            // 
            // pnRegisterClient
            // 
            this.pnRegisterClient.BackColor = System.Drawing.Color.White;
            this.pnRegisterClient.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnRegisterClient.Controls.Add(this.btnOpenCamera);
            this.pnRegisterClient.Controls.Add(this.label2);
            this.pnRegisterClient.Controls.Add(this.lbImgFaceId);
            this.pnRegisterClient.Controls.Add(this.pbCapturedFace);
            this.pnRegisterClient.Controls.Add(this.lbPlate);
            this.pnRegisterClient.Controls.Add(this.txtPlate);
            this.pnRegisterClient.Controls.Add(this.btnSave);
            this.pnRegisterClient.Controls.Add(this.pbAvatar);
            this.pnRegisterClient.Controls.Add(this.lbDescription);
            this.pnRegisterClient.Controls.Add(this.txtDescription);
            this.pnRegisterClient.Controls.Add(this.lbTimeOut);
            this.pnRegisterClient.Controls.Add(this.dtpTimeOut);
            this.pnRegisterClient.Controls.Add(this.lbTimeIn);
            this.pnRegisterClient.Controls.Add(this.dtpTimeIn);
            this.pnRegisterClient.Controls.Add(this.dtpDateOfBirth);
            this.pnRegisterClient.Controls.Add(this.rbFeMale);
            this.pnRegisterClient.Controls.Add(this.rbMale);
            this.pnRegisterClient.Controls.Add(this.lbIdCode);
            this.pnRegisterClient.Controls.Add(this.txtIdCode);
            this.pnRegisterClient.Controls.Add(this.lbPhoneNumber);
            this.pnRegisterClient.Controls.Add(this.txtPhoneNumber);
            this.pnRegisterClient.Controls.Add(this.lbDateOfBirth);
            this.pnRegisterClient.Controls.Add(this.lbAddress);
            this.pnRegisterClient.Controls.Add(this.txtAddress);
            this.pnRegisterClient.Controls.Add(this.lbName);
            this.pnRegisterClient.Controls.Add(this.txtName);
            this.pnRegisterClient.Location = new System.Drawing.Point(137, 104);
            this.pnRegisterClient.Name = "pnRegisterClient";
            this.pnRegisterClient.Size = new System.Drawing.Size(807, 531);
            this.pnRegisterClient.TabIndex = 12;
            // 
            // btnOpenCamera
            // 
            this.btnOpenCamera.AutoSize = true;
            this.btnOpenCamera.BackColor = System.Drawing.Color.Coral;
            this.btnOpenCamera.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOpenCamera.FlatAppearance.BorderSize = 0;
            this.btnOpenCamera.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenCamera.ForeColor = System.Drawing.Color.White;
            this.btnOpenCamera.Location = new System.Drawing.Point(574, 472);
            this.btnOpenCamera.Name = "btnOpenCamera";
            this.btnOpenCamera.Size = new System.Drawing.Size(106, 36);
            this.btnOpenCamera.TabIndex = 14;
            this.btnOpenCamera.Text = "Chụp ảnh";
            this.btnOpenCamera.UseVisualStyleBackColor = false;
            this.btnOpenCamera.Click += new System.EventHandler(this.btnOpenCamera_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(418, 196);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 20);
            this.label2.TabIndex = 40;
            this.label2.Text = "Ảnh CCCD";
            // 
            // lbImgFaceId
            // 
            this.lbImgFaceId.AutoSize = true;
            this.lbImgFaceId.Location = new System.Drawing.Point(604, 196);
            this.lbImgFaceId.Name = "lbImgFaceId";
            this.lbImgFaceId.Size = new System.Drawing.Size(147, 20);
            this.lbImgFaceId.TabIndex = 39;
            this.lbImgFaceId.Text = "Ảnh đăng kí FaceId";
            // 
            // pbCapturedFace
            // 
            this.pbCapturedFace.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pbCapturedFace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbCapturedFace.Location = new System.Drawing.Point(608, 219);
            this.pbCapturedFace.Name = "pbCapturedFace";
            this.pbCapturedFace.Size = new System.Drawing.Size(176, 227);
            this.pbCapturedFace.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCapturedFace.TabIndex = 38;
            this.pbCapturedFace.TabStop = false;
            // 
            // lbPlate
            // 
            this.lbPlate.AutoSize = true;
            this.lbPlate.Location = new System.Drawing.Point(416, 12);
            this.lbPlate.Name = "lbPlate";
            this.lbPlate.Size = new System.Drawing.Size(82, 20);
            this.lbPlate.TabIndex = 36;
            this.lbPlate.Text = "Biển số xe";
            // 
            // txtPlate
            // 
            this.txtPlate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPlate.Location = new System.Drawing.Point(420, 35);
            this.txtPlate.Multiline = true;
            this.txtPlate.Name = "txtPlate";
            this.txtPlate.Size = new System.Drawing.Size(362, 30);
            this.txtPlate.TabIndex = 35;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.Teal;
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(686, 472);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(96, 36);
            this.btnSave.TabIndex = 23;
            this.btnSave.Text = "Lưu";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // pbAvatar
            // 
            this.pbAvatar.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pbAvatar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbAvatar.Location = new System.Drawing.Point(422, 219);
            this.pbAvatar.Name = "pbAvatar";
            this.pbAvatar.Size = new System.Drawing.Size(176, 227);
            this.pbAvatar.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbAvatar.TabIndex = 22;
            this.pbAvatar.TabStop = false;
            // 
            // lbDescription
            // 
            this.lbDescription.AutoSize = true;
            this.lbDescription.Location = new System.Drawing.Point(418, 72);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Size = new System.Drawing.Size(49, 20);
            this.lbDescription.TabIndex = 34;
            this.lbDescription.Text = "Mô tả";
            // 
            // txtDescription
            // 
            this.txtDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDescription.Location = new System.Drawing.Point(420, 95);
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.txtDescription.Size = new System.Drawing.Size(362, 71);
            this.txtDescription.TabIndex = 33;
            // 
            // lbTimeOut
            // 
            this.lbTimeOut.AutoSize = true;
            this.lbTimeOut.Location = new System.Drawing.Point(16, 409);
            this.lbTimeOut.Name = "lbTimeOut";
            this.lbTimeOut.Size = new System.Drawing.Size(63, 20);
            this.lbTimeOut.TabIndex = 32;
            this.lbTimeOut.Text = "Ngày ra";
            // 
            // dtpTimeOut
            // 
            this.dtpTimeOut.CustomFormat = "dd/MM/yyyy";
            this.dtpTimeOut.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpTimeOut.Location = new System.Drawing.Point(19, 432);
            this.dtpTimeOut.Name = "dtpTimeOut";
            this.dtpTimeOut.Size = new System.Drawing.Size(361, 26);
            this.dtpTimeOut.TabIndex = 31;
            // 
            // lbTimeIn
            // 
            this.lbTimeIn.AutoSize = true;
            this.lbTimeIn.Location = new System.Drawing.Point(14, 353);
            this.lbTimeIn.Name = "lbTimeIn";
            this.lbTimeIn.Size = new System.Drawing.Size(74, 20);
            this.lbTimeIn.TabIndex = 30;
            this.lbTimeIn.Text = "Ngày vào";
            // 
            // dtpTimeIn
            // 
            this.dtpTimeIn.CustomFormat = "dd/MM/yyyy";
            this.dtpTimeIn.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpTimeIn.Location = new System.Drawing.Point(18, 376);
            this.dtpTimeIn.Name = "dtpTimeIn";
            this.dtpTimeIn.Size = new System.Drawing.Size(362, 26);
            this.dtpTimeIn.TabIndex = 29;
            this.dtpTimeIn.Tag = "Ngày vào";
            // 
            // dtpDateOfBirth
            // 
            this.dtpDateOfBirth.Cursor = System.Windows.Forms.Cursors.Hand;
            this.dtpDateOfBirth.CustomFormat = "dd/MM/yyyy";
            this.dtpDateOfBirth.Enabled = false;
            this.dtpDateOfBirth.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDateOfBirth.Location = new System.Drawing.Point(18, 159);
            this.dtpDateOfBirth.MinimumSize = new System.Drawing.Size(4, 30);
            this.dtpDateOfBirth.Name = "dtpDateOfBirth";
            this.dtpDateOfBirth.Size = new System.Drawing.Size(361, 30);
            this.dtpDateOfBirth.TabIndex = 28;
            // 
            // rbFeMale
            // 
            this.rbFeMale.AutoSize = true;
            this.rbFeMale.BackColor = System.Drawing.Color.White;
            this.rbFeMale.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rbFeMale.Enabled = false;
            this.rbFeMale.Location = new System.Drawing.Point(90, 255);
            this.rbFeMale.Name = "rbFeMale";
            this.rbFeMale.Size = new System.Drawing.Size(54, 24);
            this.rbFeMale.TabIndex = 27;
            this.rbFeMale.TabStop = true;
            this.rbFeMale.Text = "Nữ";
            this.rbFeMale.UseVisualStyleBackColor = false;
            // 
            // rbMale
            // 
            this.rbMale.AutoSize = true;
            this.rbMale.BackColor = System.Drawing.Color.White;
            this.rbMale.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rbMale.Enabled = false;
            this.rbMale.Location = new System.Drawing.Point(17, 255);
            this.rbMale.Name = "rbMale";
            this.rbMale.Size = new System.Drawing.Size(67, 24);
            this.rbMale.TabIndex = 26;
            this.rbMale.TabStop = true;
            this.rbMale.Text = "Nam";
            this.rbMale.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbMale.UseVisualStyleBackColor = false;
            // 
            // lbIdCode
            // 
            this.lbIdCode.AutoSize = true;
            this.lbIdCode.Location = new System.Drawing.Point(15, 12);
            this.lbIdCode.Name = "lbIdCode";
            this.lbIdCode.Size = new System.Drawing.Size(78, 20);
            this.lbIdCode.TabIndex = 25;
            this.lbIdCode.Text = "Số CCCD";
            // 
            // txtIdCode
            // 
            this.txtIdCode.BackColor = System.Drawing.Color.White;
            this.txtIdCode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtIdCode.Enabled = false;
            this.txtIdCode.Location = new System.Drawing.Point(19, 35);
            this.txtIdCode.Multiline = true;
            this.txtIdCode.Name = "txtIdCode";
            this.txtIdCode.ReadOnly = true;
            this.txtIdCode.Size = new System.Drawing.Size(362, 30);
            this.txtIdCode.TabIndex = 24;
            // 
            // lbPhoneNumber
            // 
            this.lbPhoneNumber.AutoSize = true;
            this.lbPhoneNumber.Location = new System.Drawing.Point(13, 288);
            this.lbPhoneNumber.Name = "lbPhoneNumber";
            this.lbPhoneNumber.Size = new System.Drawing.Size(102, 20);
            this.lbPhoneNumber.TabIndex = 21;
            this.lbPhoneNumber.Text = "Số điện thoại";
            // 
            // txtPhoneNumber
            // 
            this.txtPhoneNumber.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPhoneNumber.Location = new System.Drawing.Point(17, 311);
            this.txtPhoneNumber.Multiline = true;
            this.txtPhoneNumber.Name = "txtPhoneNumber";
            this.txtPhoneNumber.Size = new System.Drawing.Size(362, 30);
            this.txtPhoneNumber.TabIndex = 20;
            // 
            // lbDateOfBirth
            // 
            this.lbDateOfBirth.AutoSize = true;
            this.lbDateOfBirth.Location = new System.Drawing.Point(14, 136);
            this.lbDateOfBirth.Name = "lbDateOfBirth";
            this.lbDateOfBirth.Size = new System.Drawing.Size(78, 20);
            this.lbDateOfBirth.TabIndex = 19;
            this.lbDateOfBirth.Text = "Ngày sinh";
            // 
            // lbAddress
            // 
            this.lbAddress.AutoSize = true;
            this.lbAddress.Location = new System.Drawing.Point(13, 196);
            this.lbAddress.Name = "lbAddress";
            this.lbAddress.Size = new System.Drawing.Size(57, 20);
            this.lbAddress.TabIndex = 15;
            this.lbAddress.Text = "Địa chỉ";
            // 
            // txtAddress
            // 
            this.txtAddress.BackColor = System.Drawing.Color.White;
            this.txtAddress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtAddress.Enabled = false;
            this.txtAddress.Location = new System.Drawing.Point(17, 219);
            this.txtAddress.Multiline = true;
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.ReadOnly = true;
            this.txtAddress.Size = new System.Drawing.Size(362, 30);
            this.txtAddress.TabIndex = 14;
            // 
            // lbName
            // 
            this.lbName.AutoSize = true;
            this.lbName.Location = new System.Drawing.Point(15, 72);
            this.lbName.Name = "lbName";
            this.lbName.Size = new System.Drawing.Size(77, 20);
            this.lbName.TabIndex = 13;
            this.lbName.Text = "Họ và tên";
            // 
            // txtName
            // 
            this.txtName.BackColor = System.Drawing.Color.White;
            this.txtName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtName.Enabled = false;
            this.txtName.Location = new System.Drawing.Point(19, 95);
            this.txtName.Multiline = true;
            this.txtName.Name = "txtName";
            this.txtName.ReadOnly = true;
            this.txtName.Size = new System.Drawing.Size(362, 30);
            this.txtName.TabIndex = 12;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(137, 78);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 20);
            this.lblStatus.TabIndex = 13;
            // 
            // FrmRegisterClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1080, 666);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.pnRegisterClient);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FrmRegisterClient";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Đăng ký thông tin";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmRegisterClient_FormClosing);
            this.Load += new System.EventHandler(this.FrmRegisterClient_Load);
            this.pnRegisterClient.ResumeLayout(false);
            this.pnRegisterClient.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCapturedFace)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAvatar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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