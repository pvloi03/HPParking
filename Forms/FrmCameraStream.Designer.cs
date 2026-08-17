namespace HPParking.Forms
{
    partial class FrmCameraStream
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
            this.pbStreamCapture = new System.Windows.Forms.PictureBox();
            this.btnCapture = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pbStreamCapture)).BeginInit();
            this.SuspendLayout();
            // 
            // pbStreamCapture
            // 
            this.pbStreamCapture.Dock = System.Windows.Forms.DockStyle.Top;
            this.pbStreamCapture.Location = new System.Drawing.Point(0, 0);
            this.pbStreamCapture.Name = "pbStreamCapture";
            this.pbStreamCapture.Size = new System.Drawing.Size(542, 302);
            this.pbStreamCapture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbStreamCapture.TabIndex = 0;
            this.pbStreamCapture.TabStop = false;
            // 
            // btnCapture
            // 
            this.btnCapture.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCapture.Location = new System.Drawing.Point(227, 331);
            this.btnCapture.Name = "btnCapture";
            this.btnCapture.Size = new System.Drawing.Size(88, 33);
            this.btnCapture.TabIndex = 1;
            this.btnCapture.Text = "Chụp ảnh";
            this.btnCapture.UseVisualStyleBackColor = true;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            // 
            // FrmCameraStream
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(542, 376);
            this.Controls.Add(this.btnCapture);
            this.Controls.Add(this.pbStreamCapture);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FrmCameraStream";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FrmCameraStream";
            ((System.ComponentModel.ISupportInitialize)(this.pbStreamCapture)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbStreamCapture;
        private System.Windows.Forms.Button btnCapture;
    }
}