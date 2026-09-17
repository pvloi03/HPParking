namespace HPParking.Forms
{
    partial class FrmLogin
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
            txtUserName = new System.Windows.Forms.TextBox();
            txtPassWord = new System.Windows.Forms.TextBox();
            button1 = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 163);
            label1.Location = new System.Drawing.Point(60, 58);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(345, 29);
            label1.TabIndex = 0;
            label1.Text = "Đăng nhập tài khoản kỹ thuật";
            // 
            // txtUserName
            // 
            txtUserName.Location = new System.Drawing.Point(51, 168);
            txtUserName.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            txtUserName.Name = "txtUserName";
            txtUserName.Size = new System.Drawing.Size(363, 31);
            txtUserName.TabIndex = 1;
            txtUserName.Text = "admin";
            // 
            // txtPassWord
            // 
            txtPassWord.Location = new System.Drawing.Point(51, 249);
            txtPassWord.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            txtPassWord.Name = "txtPassWord";
            txtPassWord.PasswordChar = '*';
            txtPassWord.Size = new System.Drawing.Size(363, 31);
            txtPassWord.TabIndex = 2;
            txtPassWord.Text = "Hoangphat130225";
            // 
            // button1
            // 
            button1.BackColor = System.Drawing.Color.Teal;
            button1.Cursor = System.Windows.Forms.Cursors.Hand;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button1.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            button1.Location = new System.Drawing.Point(174, 305);
            button1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(117, 36);
            button1.TabIndex = 3;
            button1.Text = "Tiếp tục";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(51, 133);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(86, 25);
            label2.TabIndex = 4;
            label2.Text = "Tài khoản";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(51, 219);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(86, 25);
            label3.TabIndex = 5;
            label3.Text = "Mật khẩu";
            // 
            // FrmLogin
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(465, 436);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(button1);
            Controls.Add(txtPassWord);
            Controls.Add(txtUserName);
            Controls.Add(label1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            MaximizeBox = false;
            Name = "FrmLogin";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Đăng nhập";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.TextBox txtPassWord;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}