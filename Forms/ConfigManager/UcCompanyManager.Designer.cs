namespace HPParking.Forms.ConfigManager

{

    partial class UcCompanyManager

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



        #region Component Designer generated code



        /// <summary> 

        /// Required method for Designer support - do not modify 

        /// the contents of this method with the code editor.

        /// </summary>

        private void InitializeComponent()

        {
            grbCompany = new System.Windows.Forms.GroupBox();
            label4 = new System.Windows.Forms.Label();
            button4 = new System.Windows.Forms.Button();
            button1 = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            label37 = new System.Windows.Forms.Label();
            txtPathImage = new System.Windows.Forms.TextBox();
            label33 = new System.Windows.Forms.Label();
            txtTimeFreeCompany = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            txtTimeWaitCompany = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            txtNameCompany = new System.Windows.Forms.TextBox();
            txtLisenCompany = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            grbCompany.SuspendLayout();
            SuspendLayout();
            // 
            // grbCompany
            // 
            grbCompany.Controls.Add(label4);
            grbCompany.Controls.Add(button4);
            grbCompany.Controls.Add(button1);
            grbCompany.Controls.Add(button3);
            grbCompany.Controls.Add(label37);
            grbCompany.Controls.Add(txtPathImage);
            grbCompany.Controls.Add(label33);
            grbCompany.Controls.Add(txtTimeFreeCompany);
            grbCompany.Controls.Add(label1);
            grbCompany.Controls.Add(txtTimeWaitCompany);
            grbCompany.Controls.Add(label3);
            grbCompany.Controls.Add(txtNameCompany);
            grbCompany.Controls.Add(txtLisenCompany);
            grbCompany.Controls.Add(label2);
            grbCompany.Dock = System.Windows.Forms.DockStyle.Fill;
            grbCompany.Location = new System.Drawing.Point(0, 0);
            grbCompany.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            grbCompany.Name = "grbCompany";
            grbCompany.Padding = new System.Windows.Forms.Padding(3, 5, 3, 5);
            grbCompany.Size = new System.Drawing.Size(1173, 751);
            grbCompany.TabIndex = 0;
            grbCompany.TabStop = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label4.Location = new System.Drawing.Point(612, 405);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(72, 20);
            label4.TabIndex = 11;
            label4.Text = "Mã máy: ";
            // 
            // button4
            // 
            button4.BackColor = System.Drawing.Color.Coral;
            button4.Cursor = System.Windows.Forms.Cursors.Hand;
            button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button4.ForeColor = System.Drawing.SystemColors.Window;
            button4.Location = new System.Drawing.Point(617, 498);
            button4.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            button4.Name = "button4";
            button4.Size = new System.Drawing.Size(151, 50);
            button4.TabIndex = 13;
            button4.Text = "Lưu tất cả";
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click;
            // 
            // button1
            // 
            button1.BackColor = System.Drawing.Color.Teal;
            button1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            button1.Cursor = System.Windows.Forms.Cursors.Hand;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            button1.ForeColor = System.Drawing.Color.White;
            button1.Location = new System.Drawing.Point(617, 435);
            button1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(144, 38);
            button1.TabIndex = 12;
            button1.Text = "Copy mã máy";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // button3
            // 
            button3.BackColor = System.Drawing.Color.Teal;
            button3.Cursor = System.Windows.Forms.Cursors.Hand;
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            button3.ForeColor = System.Drawing.Color.White;
            button3.Location = new System.Drawing.Point(448, 436);
            button3.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(113, 38);
            button3.TabIndex = 10;
            button3.Text = "Chọn folder";
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;
            // 
            // label37
            // 
            label37.AutoSize = true;
            label37.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label37.Location = new System.Drawing.Point(137, 406);
            label37.Name = "label37";
            label37.Size = new System.Drawing.Size(142, 20);
            label37.TabIndex = 8;
            label37.Text = "Đường dẫn lưu File";
            // 
            // txtPathImage
            // 
            txtPathImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtPathImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtPathImage.Location = new System.Drawing.Point(143, 436);
            txtPathImage.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            txtPathImage.Multiline = true;
            txtPathImage.Name = "txtPathImage";
            txtPathImage.Size = new System.Drawing.Size(298, 37);
            txtPathImage.TabIndex = 9;
            txtPathImage.Tag = "Đường dẫn lưu File";
            // 
            // label33
            // 
            label33.AutoSize = true;
            label33.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label33.Location = new System.Drawing.Point(612, 305);
            label33.Name = "label33";
            label33.Size = new System.Drawing.Size(247, 20);
            label33.TabIndex = 6;
            label33.Text = "Thời gian gửi không tính phí (phút)";
            // 
            // txtTimeFreeCompany
            // 
            txtTimeFreeCompany.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtTimeFreeCompany.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtTimeFreeCompany.Location = new System.Drawing.Point(617, 335);
            txtTimeFreeCompany.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            txtTimeFreeCompany.Multiline = true;
            txtTimeFreeCompany.Name = "txtTimeFreeCompany";
            txtTimeFreeCompany.Size = new System.Drawing.Size(420, 37);
            txtTimeFreeCompany.TabIndex = 7;
            txtTimeFreeCompany.Tag = "Thời gian gửi không tính phí|number";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label1.Location = new System.Drawing.Point(137, 304);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(155, 20);
            label1.TabIndex = 4;
            label1.Text = "Ngày quá hạn (ngày)";
            // 
            // txtTimeWaitCompany
            // 
            txtTimeWaitCompany.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtTimeWaitCompany.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtTimeWaitCompany.Location = new System.Drawing.Point(141, 335);
            txtTimeWaitCompany.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            txtTimeWaitCompany.Multiline = true;
            txtTimeWaitCompany.Name = "txtTimeWaitCompany";
            txtTimeWaitCompany.Size = new System.Drawing.Size(420, 37);
            txtTimeWaitCompany.TabIndex = 5;
            txtTimeWaitCompany.Tag = "Ngày quá hạn|number";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label3.Location = new System.Drawing.Point(137, 204);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(121, 20);
            label3.TabIndex = 0;
            label3.Text = "Đơn vị triển khai";
            // 
            // txtNameCompany
            // 
            txtNameCompany.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtNameCompany.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtNameCompany.Location = new System.Drawing.Point(141, 234);
            txtNameCompany.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            txtNameCompany.Multiline = true;
            txtNameCompany.Name = "txtNameCompany";
            txtNameCompany.Size = new System.Drawing.Size(420, 37);
            txtNameCompany.TabIndex = 1;
            txtNameCompany.Tag = "Tên đơn vị triển khai";
            // 
            // txtLisenCompany
            // 
            txtLisenCompany.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtLisenCompany.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtLisenCompany.Location = new System.Drawing.Point(617, 234);
            txtLisenCompany.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            txtLisenCompany.Multiline = true;
            txtLisenCompany.Name = "txtLisenCompany";
            txtLisenCompany.PasswordChar = '*';
            txtLisenCompany.Size = new System.Drawing.Size(420, 37);
            txtLisenCompany.TabIndex = 3;
            txtLisenCompany.Tag = "License Key";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label2.Location = new System.Drawing.Point(612, 204);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(94, 20);
            label2.TabIndex = 2;
            label2.Text = "License Key";
            // 
            // UcCompanyManager
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(grbCompany);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "UcCompanyManager";
            Size = new System.Drawing.Size(1173, 751);
            Load += UcCompanyManager_Load;
            grbCompany.ResumeLayout(false);
            grbCompany.PerformLayout();
            ResumeLayout(false);

        }



        #endregion



        private System.Windows.Forms.GroupBox grbCompany;

        private System.Windows.Forms.Label label4;

        private System.Windows.Forms.Button button4;

        private System.Windows.Forms.Button button1;

        private System.Windows.Forms.Button button3;

        private System.Windows.Forms.Label label37;

        private System.Windows.Forms.TextBox txtPathImage;

        private System.Windows.Forms.Label label33;

        private System.Windows.Forms.TextBox txtTimeFreeCompany;

        private System.Windows.Forms.Label label1;

        private System.Windows.Forms.TextBox txtTimeWaitCompany;

        private System.Windows.Forms.Label label3;

        private System.Windows.Forms.TextBox txtNameCompany;

        private System.Windows.Forms.TextBox txtLisenCompany;

        private System.Windows.Forms.Label label2;

    }

}

