namespace HPParking.Forms
{
    partial class FrmCameraCapture
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
            pnlHeader = new System.Windows.Forms.Panel();
            lblHeader = new System.Windows.Forms.Label();
            pnlBottom = new System.Windows.Forms.Panel();
            lblStatus = new System.Windows.Forms.Label();
            btnCancel = new System.Windows.Forms.Button();
            pbLiveStream = new System.Windows.Forms.PictureBox();
            pnlHeader.SuspendLayout();
            pnlBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLiveStream).BeginInit();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = System.Drawing.Color.White;
            pnlHeader.Controls.Add(lblHeader);
            pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            pnlHeader.Location = new System.Drawing.Point(0, 0);
            pnlHeader.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new System.Windows.Forms.Padding(21, 17, 21, 17);
            pnlHeader.Size = new System.Drawing.Size(749, 108);
            pnlHeader.TabIndex = 0;
            // 
            // lblHeader
            // 
            lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            lblHeader.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            lblHeader.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            lblHeader.Location = new System.Drawing.Point(21, 17);
            lblHeader.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblHeader.Name = "lblHeader";
            lblHeader.Size = new System.Drawing.Size(707, 74);
            lblHeader.TabIndex = 0;
            lblHeader.Text = "Vui lòng nhìn thẳng vào camera trên đầu đọc CCCD\r\nHệ thống sẽ tự động chụp ảnh và đóng cửa sổ khi nhận diện xong";
            lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlBottom
            // 
            pnlBottom.BackColor = System.Drawing.Color.White;
            pnlBottom.Controls.Add(lblStatus);
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlBottom.Location = new System.Drawing.Point(0, 776);
            pnlBottom.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Padding = new System.Windows.Forms.Padding(21, 17, 21, 17);
            pnlBottom.Size = new System.Drawing.Size(749, 92);
            pnlBottom.TabIndex = 1;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Italic);
            lblStatus.ForeColor = System.Drawing.Color.FromArgb(13, 110, 253);
            lblStatus.Location = new System.Drawing.Point(21, 30);
            lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(194, 25);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Đang kết nối camera...";
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            btnCancel.Location = new System.Drawing.Point(570, 17);
            btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(157, 57);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Hủy bỏ (ESC)";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // pbLiveStream
            // 
            pbLiveStream.BackColor = System.Drawing.Color.FromArgb(20, 20, 25);
            pbLiveStream.Dock = System.Windows.Forms.DockStyle.Fill;
            pbLiveStream.Location = new System.Drawing.Point(0, 108);
            pbLiveStream.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            pbLiveStream.Name = "pbLiveStream";
            pbLiveStream.Size = new System.Drawing.Size(749, 668);
            pbLiveStream.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pbLiveStream.TabIndex = 2;
            pbLiveStream.TabStop = false;
            // 
            // FrmCameraCapture
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(749, 868);
            Controls.Add(pbLiveStream);
            Controls.Add(pnlBottom);
            Controls.Add(pnlHeader);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmCameraCapture";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Nhận diện khuôn mặt - Đầu đọc CCCD HN212";
            FormClosing += FrmCameraCapture_FormClosing;
            Load += FrmCameraCapture_Load;
            pnlHeader.ResumeLayout(false);
            pnlBottom.ResumeLayout(false);
            pnlBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbLiveStream).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.PictureBox pbLiveStream;
    }
}
