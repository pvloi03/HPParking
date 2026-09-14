using System.Windows.Forms;

namespace HPParking.Forms
{
    partial class FrmMain
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
            tlpMain = new TableLayoutPanel();
            tlpTitleMoto = new TableLayoutPanel();
            lblTitleMoto = new Label();
            lblLaneInMoto = new Label();
            lblLaneOutMoto = new Label();
            tlpTitleCar = new TableLayoutPanel();
            lblTitleCar = new Label();
            lblLaneInCar = new Label();
            lblLaneOutCar = new Label();
            tlpPreviewCar = new TableLayoutPanel();
            pbCarEntryOverview = new PictureBox();
            pbCarEntryPlate = new PictureBox();
            pbCarExitOverview = new PictureBox();
            pbCarExitPlate = new PictureBox();
            pnlInfoCar = new Panel();
            tlpInfoCar = new TableLayoutPanel();
            lblCarFullName = new Label();
            lblCarDepartment = new Label();
            lblCarTimeIn = new Label();
            lblCarTimeOut = new Label();
            lblCarIdentityCard = new Label();
            tlpImgsCar = new TableLayoutPanel();
            pbCarAvatarImg = new PictureBox();
            pbCarPlateImg = new PictureBox();
            lblCarPlateRegistered = new Label();
            lblCarPlateDetected = new Label();
            tlpPreviewMoto = new TableLayoutPanel();
            pbMotoEntryOverview = new PictureBox();
            pbMotoEntryPlate = new PictureBox();
            pbMotoExitOverview = new PictureBox();
            pbMotoExitPlate = new PictureBox();
            pnlInfoMoto = new Panel();
            tlpInfoMoto = new TableLayoutPanel();
            lblMotoFullName = new Label();
            lblMotoDepartment = new Label();
            lblMotoTimeIn = new Label();
            lblMotoTimeOut = new Label();
            lblMotoIdentityCard = new Label();
            tlpImgsMoto = new TableLayoutPanel();
            pbMotoAvatarImg = new PictureBox();
            pbMotoPlateImg = new PictureBox();
            lblMotoPlateRegistered = new Label();
            lblMotoPlateDetected = new Label();
            pnlFooter = new Panel();
            lbStatusCtrl = new Label();
            lbdayExpiryDate = new Label();
            lbRealTime = new Label();
            label3 = new Label();
            tlpMain.SuspendLayout();
            tlpTitleMoto.SuspendLayout();
            tlpTitleCar.SuspendLayout();
            tlpPreviewCar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbCarEntryOverview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCarEntryPlate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCarExitOverview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCarExitPlate).BeginInit();
            pnlInfoCar.SuspendLayout();
            tlpInfoCar.SuspendLayout();
            tlpImgsCar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbCarAvatarImg).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCarPlateImg).BeginInit();
            tlpPreviewMoto.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbMotoEntryOverview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbMotoEntryPlate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbMotoExitOverview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbMotoExitPlate).BeginInit();
            pnlInfoMoto.SuspendLayout();
            tlpInfoMoto.SuspendLayout();
            tlpImgsMoto.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbMotoAvatarImg).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbMotoPlateImg).BeginInit();
            pnlFooter.SuspendLayout();
            SuspendLayout();
            // 
            // tlpMain
            // 
            tlpMain.AutoSize = true;
            tlpMain.BackColor = System.Drawing.Color.White;
            tlpMain.ColumnCount = 2;
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpMain.Controls.Add(tlpTitleMoto, 0, 0);
            tlpMain.Controls.Add(tlpTitleCar, 1, 0);
            tlpMain.Controls.Add(tlpPreviewCar, 1, 1);
            tlpMain.Controls.Add(tlpPreviewMoto, 0, 1);
            tlpMain.Controls.Add(pnlFooter, 0, 2);
            tlpMain.Dock = DockStyle.Fill;
            tlpMain.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            tlpMain.ForeColor = System.Drawing.Color.White;
            tlpMain.Location = new System.Drawing.Point(0, 0);
            tlpMain.Margin = new Padding(0);
            tlpMain.Name = "tlpMain";
            tlpMain.RowCount = 3;
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 85F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tlpMain.Size = new System.Drawing.Size(1924, 1050);
            tlpMain.TabIndex = 0;
            // 
            // tlpTitleMoto
            // 
            tlpTitleMoto.ColumnCount = 2;
            tlpTitleMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpTitleMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpTitleMoto.Controls.Add(lblTitleMoto, 0, 0);
            tlpTitleMoto.Controls.Add(lblLaneInMoto, 0, 1);
            tlpTitleMoto.Controls.Add(lblLaneOutMoto, 1, 1);
            tlpTitleMoto.Dock = DockStyle.Fill;
            tlpTitleMoto.Location = new System.Drawing.Point(11, 0);
            tlpTitleMoto.Margin = new Padding(11, 0, 28, 0);
            tlpTitleMoto.Name = "tlpTitleMoto";
            tlpTitleMoto.RowCount = 2;
            tlpTitleMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpTitleMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpTitleMoto.Size = new System.Drawing.Size(923, 105);
            tlpTitleMoto.TabIndex = 11;
            // 
            // lblTitleMoto
            // 
            lblTitleMoto.BackColor = System.Drawing.Color.SeaGreen;
            tlpTitleMoto.SetColumnSpan(lblTitleMoto, 2);
            lblTitleMoto.Dock = DockStyle.Fill;
            lblTitleMoto.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblTitleMoto.ForeColor = System.Drawing.Color.White;
            lblTitleMoto.Location = new System.Drawing.Point(0, 0);
            lblTitleMoto.Margin = new Padding(0);
            lblTitleMoto.Name = "lblTitleMoto";
            lblTitleMoto.Size = new System.Drawing.Size(923, 52);
            lblTitleMoto.TabIndex = 0;
            lblTitleMoto.Text = "Xe máy";
            lblTitleMoto.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLaneInMoto
            // 
            lblLaneInMoto.BackColor = System.Drawing.Color.DarkCyan;
            lblLaneInMoto.Dock = DockStyle.Fill;
            lblLaneInMoto.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblLaneInMoto.ForeColor = System.Drawing.Color.White;
            lblLaneInMoto.Location = new System.Drawing.Point(0, 58);
            lblLaneInMoto.Margin = new Padding(0, 6, 6, 0);
            lblLaneInMoto.Name = "lblLaneInMoto";
            lblLaneInMoto.Size = new System.Drawing.Size(455, 47);
            lblLaneInMoto.TabIndex = 1;
            lblLaneInMoto.Text = "Làn vào";
            lblLaneInMoto.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLaneOutMoto
            // 
            lblLaneOutMoto.BackColor = System.Drawing.Color.DarkCyan;
            lblLaneOutMoto.Dock = DockStyle.Fill;
            lblLaneOutMoto.Location = new System.Drawing.Point(467, 58);
            lblLaneOutMoto.Margin = new Padding(6, 6, 0, 0);
            lblLaneOutMoto.Name = "lblLaneOutMoto";
            lblLaneOutMoto.Size = new System.Drawing.Size(456, 47);
            lblLaneOutMoto.TabIndex = 2;
            lblLaneOutMoto.Text = "Làn ra";
            lblLaneOutMoto.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tlpTitleCar
            // 
            tlpTitleCar.ColumnCount = 2;
            tlpTitleCar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpTitleCar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpTitleCar.Controls.Add(lblTitleCar, 0, 0);
            tlpTitleCar.Controls.Add(lblLaneInCar, 0, 1);
            tlpTitleCar.Controls.Add(lblLaneOutCar, 1, 1);
            tlpTitleCar.Dock = DockStyle.Fill;
            tlpTitleCar.Location = new System.Drawing.Point(990, 0);
            tlpTitleCar.Margin = new Padding(28, 0, 11, 0);
            tlpTitleCar.Name = "tlpTitleCar";
            tlpTitleCar.RowCount = 2;
            tlpTitleCar.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpTitleCar.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpTitleCar.Size = new System.Drawing.Size(923, 105);
            tlpTitleCar.TabIndex = 12;
            // 
            // lblTitleCar
            // 
            lblTitleCar.BackColor = System.Drawing.Color.SeaGreen;
            tlpTitleCar.SetColumnSpan(lblTitleCar, 2);
            lblTitleCar.Dock = DockStyle.Fill;
            lblTitleCar.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblTitleCar.ForeColor = System.Drawing.Color.White;
            lblTitleCar.Location = new System.Drawing.Point(0, 0);
            lblTitleCar.Margin = new Padding(0);
            lblTitleCar.Name = "lblTitleCar";
            lblTitleCar.Size = new System.Drawing.Size(923, 52);
            lblTitleCar.TabIndex = 0;
            lblTitleCar.Text = "Ô tô";
            lblTitleCar.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLaneInCar
            // 
            lblLaneInCar.BackColor = System.Drawing.Color.DarkCyan;
            lblLaneInCar.Dock = DockStyle.Fill;
            lblLaneInCar.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblLaneInCar.ForeColor = System.Drawing.Color.White;
            lblLaneInCar.Location = new System.Drawing.Point(0, 58);
            lblLaneInCar.Margin = new Padding(0, 6, 6, 0);
            lblLaneInCar.Name = "lblLaneInCar";
            lblLaneInCar.Size = new System.Drawing.Size(455, 47);
            lblLaneInCar.TabIndex = 1;
            lblLaneInCar.Text = "Làn vào";
            lblLaneInCar.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLaneOutCar
            // 
            lblLaneOutCar.BackColor = System.Drawing.Color.DarkCyan;
            lblLaneOutCar.Dock = DockStyle.Fill;
            lblLaneOutCar.Location = new System.Drawing.Point(467, 58);
            lblLaneOutCar.Margin = new Padding(6, 6, 0, 0);
            lblLaneOutCar.Name = "lblLaneOutCar";
            lblLaneOutCar.Size = new System.Drawing.Size(456, 47);
            lblLaneOutCar.TabIndex = 2;
            lblLaneOutCar.Text = "Làn ra";
            lblLaneOutCar.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tlpPreviewCar
            // 
            tlpPreviewCar.BackColor = System.Drawing.Color.Transparent;
            tlpPreviewCar.ColumnCount = 2;
            tlpPreviewCar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpPreviewCar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpPreviewCar.Controls.Add(pbCarEntryOverview, 0, 0);
            tlpPreviewCar.Controls.Add(pbCarEntryPlate, 0, 1);
            tlpPreviewCar.Controls.Add(pbCarExitOverview, 1, 0);
            tlpPreviewCar.Controls.Add(pbCarExitPlate, 1, 1);
            tlpPreviewCar.Controls.Add(pnlInfoCar, 0, 2);
            tlpPreviewCar.Dock = DockStyle.Fill;
            tlpPreviewCar.ForeColor = System.Drawing.Color.White;
            tlpPreviewCar.Location = new System.Drawing.Point(984, 111);
            tlpPreviewCar.Margin = new Padding(22, 6, 6, 6);
            tlpPreviewCar.Name = "tlpPreviewCar";
            tlpPreviewCar.RowCount = 3;
            tlpPreviewCar.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
            tlpPreviewCar.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
            tlpPreviewCar.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
            tlpPreviewCar.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tlpPreviewCar.Size = new System.Drawing.Size(934, 880);
            tlpPreviewCar.TabIndex = 10;
            // 
            // pbCarEntryOverview
            // 
            pbCarEntryOverview.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pbCarEntryOverview.BackColor = System.Drawing.Color.Black;
            pbCarEntryOverview.Location = new System.Drawing.Point(6, 6);
            pbCarEntryOverview.Margin = new Padding(6);
            pbCarEntryOverview.Name = "pbCarEntryOverview";
            pbCarEntryOverview.Size = new System.Drawing.Size(455, 269);
            pbCarEntryOverview.SizeMode = PictureBoxSizeMode.Zoom;
            pbCarEntryOverview.TabIndex = 6;
            pbCarEntryOverview.TabStop = false;
            pbCarEntryOverview.Tag = "pbPreview";
            // 
            // pbCarEntryPlate
            // 
            pbCarEntryPlate.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pbCarEntryPlate.BackColor = System.Drawing.Color.Black;
            pbCarEntryPlate.Location = new System.Drawing.Point(6, 287);
            pbCarEntryPlate.Margin = new Padding(6);
            pbCarEntryPlate.Name = "pbCarEntryPlate";
            pbCarEntryPlate.Size = new System.Drawing.Size(455, 269);
            pbCarEntryPlate.SizeMode = PictureBoxSizeMode.Zoom;
            pbCarEntryPlate.TabIndex = 1;
            pbCarEntryPlate.TabStop = false;
            pbCarEntryPlate.Tag = "pbPreview";
            // 
            // pbCarExitOverview
            // 
            pbCarExitOverview.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pbCarExitOverview.BackColor = System.Drawing.Color.Black;
            pbCarExitOverview.Location = new System.Drawing.Point(473, 6);
            pbCarExitOverview.Margin = new Padding(6);
            pbCarExitOverview.Name = "pbCarExitOverview";
            pbCarExitOverview.Size = new System.Drawing.Size(455, 269);
            pbCarExitOverview.SizeMode = PictureBoxSizeMode.Zoom;
            pbCarExitOverview.TabIndex = 2;
            pbCarExitOverview.TabStop = false;
            pbCarExitOverview.Tag = "pbPreview";
            // 
            // pbCarExitPlate
            // 
            pbCarExitPlate.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pbCarExitPlate.BackColor = System.Drawing.Color.Black;
            pbCarExitPlate.Location = new System.Drawing.Point(473, 287);
            pbCarExitPlate.Margin = new Padding(6);
            pbCarExitPlate.Name = "pbCarExitPlate";
            pbCarExitPlate.Size = new System.Drawing.Size(455, 269);
            pbCarExitPlate.SizeMode = PictureBoxSizeMode.Zoom;
            pbCarExitPlate.TabIndex = 3;
            pbCarExitPlate.TabStop = false;
            pbCarExitPlate.Tag = "pbPreview";
            // 
            // pnlInfoCar
            // 
            pnlInfoCar.BackColor = System.Drawing.Color.Gainsboro;
            tlpPreviewCar.SetColumnSpan(pnlInfoCar, 2);
            pnlInfoCar.Controls.Add(tlpInfoCar);
            pnlInfoCar.Dock = DockStyle.Fill;
            pnlInfoCar.ForeColor = System.Drawing.Color.Black;
            pnlInfoCar.Location = new System.Drawing.Point(6, 587);
            pnlInfoCar.Margin = new Padding(6, 25, 6, 0);
            pnlInfoCar.Name = "pnlInfoCar";
            pnlInfoCar.Padding = new Padding(11, 12, 11, 12);
            pnlInfoCar.Size = new System.Drawing.Size(922, 293);
            pnlInfoCar.TabIndex = 7;
            // 
            // tlpInfoCar
            // 
            tlpInfoCar.ColumnCount = 2;
            tlpInfoCar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpInfoCar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpInfoCar.Controls.Add(lblCarFullName, 0, 0);
            tlpInfoCar.Controls.Add(lblCarDepartment, 1, 0);
            tlpInfoCar.Controls.Add(lblCarTimeIn, 0, 1);
            tlpInfoCar.Controls.Add(lblCarTimeOut, 1, 1);
            tlpInfoCar.Controls.Add(lblCarIdentityCard, 0, 2);
            tlpInfoCar.Controls.Add(tlpImgsCar, 1, 2);
            tlpInfoCar.Controls.Add(lblCarPlateRegistered, 0, 3);
            tlpInfoCar.Controls.Add(lblCarPlateDetected, 0, 4);
            tlpInfoCar.Dock = DockStyle.Fill;
            tlpInfoCar.Location = new System.Drawing.Point(11, 12);
            tlpInfoCar.Margin = new Padding(0);
            tlpInfoCar.Name = "tlpInfoCar";
            tlpInfoCar.RowCount = 5;
            tlpInfoCar.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoCar.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoCar.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoCar.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoCar.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoCar.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tlpInfoCar.Size = new System.Drawing.Size(900, 269);
            tlpInfoCar.TabIndex = 8;
            // 
            // lblCarFullName
            // 
            lblCarFullName.AutoSize = true;
            lblCarFullName.BackColor = System.Drawing.Color.White;
            lblCarFullName.BorderStyle = BorderStyle.FixedSingle;
            lblCarFullName.Dock = DockStyle.Fill;
            lblCarFullName.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblCarFullName.ForeColor = System.Drawing.Color.Black;
            lblCarFullName.Location = new System.Drawing.Point(0, 6);
            lblCarFullName.Margin = new Padding(0, 6, 11, 6);
            lblCarFullName.Name = "lblCarFullName";
            lblCarFullName.Size = new System.Drawing.Size(439, 41);
            lblCarFullName.TabIndex = 0;
            lblCarFullName.Tag = "lblInfo";
            lblCarFullName.Text = "Họ và tên: ";
            lblCarFullName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarDepartment
            // 
            lblCarDepartment.AutoSize = true;
            lblCarDepartment.BackColor = System.Drawing.Color.White;
            lblCarDepartment.BorderStyle = BorderStyle.FixedSingle;
            lblCarDepartment.Dock = DockStyle.Fill;
            lblCarDepartment.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblCarDepartment.ForeColor = System.Drawing.Color.Black;
            lblCarDepartment.Location = new System.Drawing.Point(461, 6);
            lblCarDepartment.Margin = new Padding(11, 6, 0, 6);
            lblCarDepartment.Name = "lblCarDepartment";
            lblCarDepartment.Size = new System.Drawing.Size(439, 41);
            lblCarDepartment.TabIndex = 1;
            lblCarDepartment.Tag = "lblInfo";
            lblCarDepartment.Text = "Phòng ban: ";
            lblCarDepartment.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarTimeIn
            // 
            lblCarTimeIn.AutoSize = true;
            lblCarTimeIn.BackColor = System.Drawing.Color.White;
            lblCarTimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblCarTimeIn.Dock = DockStyle.Fill;
            lblCarTimeIn.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblCarTimeIn.ForeColor = System.Drawing.Color.Black;
            lblCarTimeIn.Location = new System.Drawing.Point(0, 59);
            lblCarTimeIn.Margin = new Padding(0, 6, 11, 6);
            lblCarTimeIn.Name = "lblCarTimeIn";
            lblCarTimeIn.Size = new System.Drawing.Size(439, 41);
            lblCarTimeIn.TabIndex = 4;
            lblCarTimeIn.Tag = "lblInfo";
            lblCarTimeIn.Text = "Ngày vào:";
            lblCarTimeIn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarTimeOut
            // 
            lblCarTimeOut.AutoSize = true;
            lblCarTimeOut.BackColor = System.Drawing.Color.White;
            lblCarTimeOut.BorderStyle = BorderStyle.FixedSingle;
            lblCarTimeOut.Dock = DockStyle.Fill;
            lblCarTimeOut.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblCarTimeOut.ForeColor = System.Drawing.Color.Black;
            lblCarTimeOut.Location = new System.Drawing.Point(461, 59);
            lblCarTimeOut.Margin = new Padding(11, 6, 0, 6);
            lblCarTimeOut.Name = "lblCarTimeOut";
            lblCarTimeOut.Size = new System.Drawing.Size(439, 41);
            lblCarTimeOut.TabIndex = 5;
            lblCarTimeOut.Tag = "lblInfo";
            lblCarTimeOut.Text = "Ngày ra: ";
            lblCarTimeOut.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarIdentityCard
            // 
            lblCarIdentityCard.AutoSize = true;
            lblCarIdentityCard.BackColor = System.Drawing.Color.White;
            lblCarIdentityCard.BorderStyle = BorderStyle.FixedSingle;
            lblCarIdentityCard.Dock = DockStyle.Fill;
            lblCarIdentityCard.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblCarIdentityCard.ForeColor = System.Drawing.Color.Black;
            lblCarIdentityCard.Location = new System.Drawing.Point(0, 112);
            lblCarIdentityCard.Margin = new Padding(0, 6, 11, 6);
            lblCarIdentityCard.Name = "lblCarIdentityCard";
            lblCarIdentityCard.Size = new System.Drawing.Size(439, 41);
            lblCarIdentityCard.TabIndex = 8;
            lblCarIdentityCard.Tag = "lblInfo";
            lblCarIdentityCard.Text = "Số CCCD: ";
            lblCarIdentityCard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tlpImgsCar
            // 
            tlpImgsCar.ColumnCount = 2;
            tlpImgsCar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpImgsCar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpImgsCar.Controls.Add(pbCarAvatarImg, 1, 0);
            tlpImgsCar.Controls.Add(pbCarPlateImg, 0, 0);
            tlpImgsCar.Dock = DockStyle.Fill;
            tlpImgsCar.Location = new System.Drawing.Point(461, 112);
            tlpImgsCar.Margin = new Padding(11, 6, 0, 6);
            tlpImgsCar.Name = "tlpImgsCar";
            tlpImgsCar.RowCount = 1;
            tlpInfoCar.SetRowSpan(tlpImgsCar, 3);
            tlpImgsCar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpImgsCar.Size = new System.Drawing.Size(439, 151);
            tlpImgsCar.TabIndex = 10;
            // 
            // pbCarAvatarImg
            // 
            pbCarAvatarImg.BackColor = System.Drawing.Color.Black;
            pbCarAvatarImg.BorderStyle = BorderStyle.FixedSingle;
            pbCarAvatarImg.Dock = DockStyle.Fill;
            pbCarAvatarImg.Location = new System.Drawing.Point(225, 0);
            pbCarAvatarImg.Margin = new Padding(6, 0, 0, 0);
            pbCarAvatarImg.Name = "pbCarAvatarImg";
            pbCarAvatarImg.Size = new System.Drawing.Size(214, 151);
            pbCarAvatarImg.SizeMode = PictureBoxSizeMode.Zoom;
            pbCarAvatarImg.TabIndex = 1;
            pbCarAvatarImg.TabStop = false;
            pbCarAvatarImg.Tag = "captureImg";
            // 
            // pbCarPlateImg
            // 
            pbCarPlateImg.BackColor = System.Drawing.Color.Black;
            pbCarPlateImg.BorderStyle = BorderStyle.FixedSingle;
            pbCarPlateImg.Dock = DockStyle.Fill;
            pbCarPlateImg.Location = new System.Drawing.Point(0, 0);
            pbCarPlateImg.Margin = new Padding(0, 0, 6, 0);
            pbCarPlateImg.Name = "pbCarPlateImg";
            pbCarPlateImg.Size = new System.Drawing.Size(213, 151);
            pbCarPlateImg.SizeMode = PictureBoxSizeMode.Zoom;
            pbCarPlateImg.TabIndex = 0;
            pbCarPlateImg.TabStop = false;
            pbCarPlateImg.Tag = "captureImg";
            // 
            // lblCarPlateRegistered
            // 
            lblCarPlateRegistered.AutoSize = true;
            lblCarPlateRegistered.BackColor = System.Drawing.Color.White;
            lblCarPlateRegistered.BorderStyle = BorderStyle.FixedSingle;
            lblCarPlateRegistered.Dock = DockStyle.Fill;
            lblCarPlateRegistered.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblCarPlateRegistered.ForeColor = System.Drawing.Color.Black;
            lblCarPlateRegistered.Location = new System.Drawing.Point(0, 165);
            lblCarPlateRegistered.Margin = new Padding(0, 6, 11, 6);
            lblCarPlateRegistered.Name = "lblCarPlateRegistered";
            lblCarPlateRegistered.Size = new System.Drawing.Size(439, 41);
            lblCarPlateRegistered.TabIndex = 6;
            lblCarPlateRegistered.Tag = "lblInfo";
            lblCarPlateRegistered.Text = "Biển số đăng ký: ";
            lblCarPlateRegistered.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarPlateDetected
            // 
            lblCarPlateDetected.AutoSize = true;
            lblCarPlateDetected.BackColor = System.Drawing.Color.White;
            lblCarPlateDetected.BorderStyle = BorderStyle.FixedSingle;
            lblCarPlateDetected.Dock = DockStyle.Fill;
            lblCarPlateDetected.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblCarPlateDetected.ForeColor = System.Drawing.Color.Black;
            lblCarPlateDetected.Location = new System.Drawing.Point(0, 218);
            lblCarPlateDetected.Margin = new Padding(0, 6, 11, 6);
            lblCarPlateDetected.Name = "lblCarPlateDetected";
            lblCarPlateDetected.Size = new System.Drawing.Size(439, 45);
            lblCarPlateDetected.TabIndex = 7;
            lblCarPlateDetected.Tag = "lblInfo";
            lblCarPlateDetected.Text = "Biển số nhận dạng:";
            lblCarPlateDetected.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tlpPreviewMoto
            // 
            tlpPreviewMoto.BackColor = System.Drawing.Color.Transparent;
            tlpPreviewMoto.ColumnCount = 2;
            tlpPreviewMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpPreviewMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpPreviewMoto.Controls.Add(pbMotoEntryOverview, 0, 0);
            tlpPreviewMoto.Controls.Add(pbMotoEntryPlate, 0, 1);
            tlpPreviewMoto.Controls.Add(pbMotoExitOverview, 1, 0);
            tlpPreviewMoto.Controls.Add(pbMotoExitPlate, 1, 1);
            tlpPreviewMoto.Controls.Add(pnlInfoMoto, 0, 2);
            tlpPreviewMoto.Dock = DockStyle.Fill;
            tlpPreviewMoto.ForeColor = System.Drawing.Color.White;
            tlpPreviewMoto.Location = new System.Drawing.Point(6, 111);
            tlpPreviewMoto.Margin = new Padding(6, 6, 22, 6);
            tlpPreviewMoto.Name = "tlpPreviewMoto";
            tlpPreviewMoto.RowCount = 3;
            tlpPreviewMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
            tlpPreviewMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
            tlpPreviewMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
            tlpPreviewMoto.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tlpPreviewMoto.Size = new System.Drawing.Size(934, 880);
            tlpPreviewMoto.TabIndex = 6;
            // 
            // pbMotoEntryOverview
            // 
            pbMotoEntryOverview.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pbMotoEntryOverview.BackColor = System.Drawing.Color.Black;
            pbMotoEntryOverview.Location = new System.Drawing.Point(6, 6);
            pbMotoEntryOverview.Margin = new Padding(6);
            pbMotoEntryOverview.Name = "pbMotoEntryOverview";
            pbMotoEntryOverview.Size = new System.Drawing.Size(455, 269);
            pbMotoEntryOverview.SizeMode = PictureBoxSizeMode.Zoom;
            pbMotoEntryOverview.TabIndex = 6;
            pbMotoEntryOverview.TabStop = false;
            pbMotoEntryOverview.Tag = "pbPreview";
            // 
            // pbMotoEntryPlate
            // 
            pbMotoEntryPlate.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pbMotoEntryPlate.BackColor = System.Drawing.Color.Black;
            pbMotoEntryPlate.Location = new System.Drawing.Point(6, 287);
            pbMotoEntryPlate.Margin = new Padding(6);
            pbMotoEntryPlate.Name = "pbMotoEntryPlate";
            pbMotoEntryPlate.Size = new System.Drawing.Size(455, 269);
            pbMotoEntryPlate.SizeMode = PictureBoxSizeMode.Zoom;
            pbMotoEntryPlate.TabIndex = 1;
            pbMotoEntryPlate.TabStop = false;
            pbMotoEntryPlate.Tag = "pbPreview";
            // 
            // pbMotoExitOverview
            // 
            pbMotoExitOverview.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pbMotoExitOverview.BackColor = System.Drawing.Color.Black;
            pbMotoExitOverview.Location = new System.Drawing.Point(473, 6);
            pbMotoExitOverview.Margin = new Padding(6);
            pbMotoExitOverview.Name = "pbMotoExitOverview";
            pbMotoExitOverview.Size = new System.Drawing.Size(455, 269);
            pbMotoExitOverview.SizeMode = PictureBoxSizeMode.Zoom;
            pbMotoExitOverview.TabIndex = 2;
            pbMotoExitOverview.TabStop = false;
            pbMotoExitOverview.Tag = "pbPreview";
            // 
            // pbMotoExitPlate
            // 
            pbMotoExitPlate.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pbMotoExitPlate.BackColor = System.Drawing.Color.Black;
            pbMotoExitPlate.Location = new System.Drawing.Point(473, 287);
            pbMotoExitPlate.Margin = new Padding(6);
            pbMotoExitPlate.Name = "pbMotoExitPlate";
            pbMotoExitPlate.Size = new System.Drawing.Size(455, 269);
            pbMotoExitPlate.SizeMode = PictureBoxSizeMode.Zoom;
            pbMotoExitPlate.TabIndex = 3;
            pbMotoExitPlate.TabStop = false;
            pbMotoExitPlate.Tag = "pbPreview";
            // 
            // pnlInfoMoto
            // 
            pnlInfoMoto.BackColor = System.Drawing.Color.Gainsboro;
            tlpPreviewMoto.SetColumnSpan(pnlInfoMoto, 2);
            pnlInfoMoto.Controls.Add(tlpInfoMoto);
            pnlInfoMoto.Dock = DockStyle.Fill;
            pnlInfoMoto.ForeColor = System.Drawing.Color.Black;
            pnlInfoMoto.Location = new System.Drawing.Point(6, 587);
            pnlInfoMoto.Margin = new Padding(6, 25, 6, 0);
            pnlInfoMoto.Name = "pnlInfoMoto";
            pnlInfoMoto.Padding = new Padding(11, 12, 11, 12);
            pnlInfoMoto.Size = new System.Drawing.Size(922, 293);
            pnlInfoMoto.TabIndex = 7;
            // 
            // tlpInfoMoto
            // 
            tlpInfoMoto.ColumnCount = 3;
            tlpInfoMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tlpInfoMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            tlpInfoMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19.11111F));
            tlpInfoMoto.Controls.Add(lblMotoFullName, 0, 0);
            tlpInfoMoto.Controls.Add(lblMotoDepartment, 1, 0);
            tlpInfoMoto.Controls.Add(lblMotoTimeIn, 0, 1);
            tlpInfoMoto.Controls.Add(lblMotoTimeOut, 1, 1);
            tlpInfoMoto.Controls.Add(lblMotoIdentityCard, 0, 2);
            tlpInfoMoto.Controls.Add(tlpImgsMoto, 1, 2);
            tlpInfoMoto.Controls.Add(lblMotoPlateRegistered, 0, 3);
            tlpInfoMoto.Controls.Add(lblMotoPlateDetected, 0, 4);
            tlpInfoMoto.Dock = DockStyle.Fill;
            tlpInfoMoto.Location = new System.Drawing.Point(11, 12);
            tlpInfoMoto.Margin = new Padding(0);
            tlpInfoMoto.Name = "tlpInfoMoto";
            tlpInfoMoto.RowCount = 5;
            tlpInfoMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tlpInfoMoto.Size = new System.Drawing.Size(900, 269);
            tlpInfoMoto.TabIndex = 8;
            // 
            // lblMotoFullName
            // 
            lblMotoFullName.AutoSize = true;
            lblMotoFullName.BackColor = System.Drawing.Color.White;
            lblMotoFullName.BorderStyle = BorderStyle.FixedSingle;
            lblMotoFullName.Dock = DockStyle.Fill;
            lblMotoFullName.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblMotoFullName.ForeColor = System.Drawing.Color.Black;
            lblMotoFullName.Location = new System.Drawing.Point(0, 6);
            lblMotoFullName.Margin = new Padding(0, 6, 11, 6);
            lblMotoFullName.Name = "lblMotoFullName";
            lblMotoFullName.Size = new System.Drawing.Size(393, 41);
            lblMotoFullName.TabIndex = 0;
            lblMotoFullName.Tag = "lblInfo";
            lblMotoFullName.Text = "Họ và tên: ";
            lblMotoFullName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoDepartment
            // 
            lblMotoDepartment.AutoSize = true;
            lblMotoDepartment.BackColor = System.Drawing.Color.White;
            lblMotoDepartment.BorderStyle = BorderStyle.FixedSingle;
            tlpInfoMoto.SetColumnSpan(lblMotoDepartment, 2);
            lblMotoDepartment.Dock = DockStyle.Fill;
            lblMotoDepartment.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblMotoDepartment.ForeColor = System.Drawing.Color.Black;
            lblMotoDepartment.Location = new System.Drawing.Point(415, 6);
            lblMotoDepartment.Margin = new Padding(11, 6, 0, 6);
            lblMotoDepartment.Name = "lblMotoDepartment";
            lblMotoDepartment.Size = new System.Drawing.Size(485, 41);
            lblMotoDepartment.TabIndex = 1;
            lblMotoDepartment.Tag = "lblInfo";
            lblMotoDepartment.Text = "Phòng ban: ";
            lblMotoDepartment.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoTimeIn
            // 
            lblMotoTimeIn.AutoSize = true;
            lblMotoTimeIn.BackColor = System.Drawing.Color.White;
            lblMotoTimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblMotoTimeIn.Dock = DockStyle.Fill;
            lblMotoTimeIn.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblMotoTimeIn.ForeColor = System.Drawing.Color.Black;
            lblMotoTimeIn.Location = new System.Drawing.Point(0, 59);
            lblMotoTimeIn.Margin = new Padding(0, 6, 11, 6);
            lblMotoTimeIn.Name = "lblMotoTimeIn";
            lblMotoTimeIn.Size = new System.Drawing.Size(393, 41);
            lblMotoTimeIn.TabIndex = 4;
            lblMotoTimeIn.Tag = "lblInfo";
            lblMotoTimeIn.Text = "Ngày vào:";
            lblMotoTimeIn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoTimeOut
            // 
            lblMotoTimeOut.AutoSize = true;
            lblMotoTimeOut.BackColor = System.Drawing.Color.White;
            lblMotoTimeOut.BorderStyle = BorderStyle.FixedSingle;
            tlpInfoMoto.SetColumnSpan(lblMotoTimeOut, 2);
            lblMotoTimeOut.Dock = DockStyle.Fill;
            lblMotoTimeOut.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblMotoTimeOut.ForeColor = System.Drawing.Color.Black;
            lblMotoTimeOut.Location = new System.Drawing.Point(415, 59);
            lblMotoTimeOut.Margin = new Padding(11, 6, 0, 6);
            lblMotoTimeOut.Name = "lblMotoTimeOut";
            lblMotoTimeOut.Size = new System.Drawing.Size(485, 41);
            lblMotoTimeOut.TabIndex = 5;
            lblMotoTimeOut.Tag = "lblInfo";
            lblMotoTimeOut.Text = "Ngày ra: ";
            lblMotoTimeOut.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoIdentityCard
            // 
            lblMotoIdentityCard.AutoSize = true;
            lblMotoIdentityCard.BackColor = System.Drawing.Color.White;
            lblMotoIdentityCard.BorderStyle = BorderStyle.FixedSingle;
            lblMotoIdentityCard.Dock = DockStyle.Fill;
            lblMotoIdentityCard.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblMotoIdentityCard.ForeColor = System.Drawing.Color.Black;
            lblMotoIdentityCard.Location = new System.Drawing.Point(0, 112);
            lblMotoIdentityCard.Margin = new Padding(0, 6, 11, 6);
            lblMotoIdentityCard.Name = "lblMotoIdentityCard";
            lblMotoIdentityCard.Size = new System.Drawing.Size(393, 41);
            lblMotoIdentityCard.TabIndex = 8;
            lblMotoIdentityCard.Tag = "lblInfo";
            lblMotoIdentityCard.Text = "Số CCCD: ";
            lblMotoIdentityCard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tlpImgsMoto
            // 
            tlpImgsMoto.ColumnCount = 2;
            tlpInfoMoto.SetColumnSpan(tlpImgsMoto, 2);
            tlpImgsMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tlpImgsMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tlpImgsMoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tlpImgsMoto.Controls.Add(pbMotoAvatarImg, 1, 0);
            tlpImgsMoto.Controls.Add(pbMotoPlateImg, 0, 0);
            tlpImgsMoto.Dock = DockStyle.Fill;
            tlpImgsMoto.Location = new System.Drawing.Point(415, 112);
            tlpImgsMoto.Margin = new Padding(11, 6, 0, 6);
            tlpImgsMoto.Name = "tlpImgsMoto";
            tlpImgsMoto.RowCount = 1;
            tlpInfoMoto.SetRowSpan(tlpImgsMoto, 3);
            tlpImgsMoto.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpImgsMoto.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tlpImgsMoto.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tlpImgsMoto.Size = new System.Drawing.Size(485, 151);
            tlpImgsMoto.TabIndex = 9;
            // 
            // pbMotoAvatarImg
            // 
            pbMotoAvatarImg.BackColor = System.Drawing.Color.Black;
            pbMotoAvatarImg.BorderStyle = BorderStyle.FixedSingle;
            pbMotoAvatarImg.Dock = DockStyle.Fill;
            pbMotoAvatarImg.Location = new System.Drawing.Point(248, 0);
            pbMotoAvatarImg.Margin = new Padding(6, 0, 0, 0);
            pbMotoAvatarImg.Name = "pbMotoAvatarImg";
            pbMotoAvatarImg.Size = new System.Drawing.Size(237, 151);
            pbMotoAvatarImg.SizeMode = PictureBoxSizeMode.Zoom;
            pbMotoAvatarImg.TabIndex = 1;
            pbMotoAvatarImg.TabStop = false;
            pbMotoAvatarImg.Tag = "captureImg";
            // 
            // pbMotoPlateImg
            // 
            pbMotoPlateImg.BackColor = System.Drawing.Color.Black;
            pbMotoPlateImg.BorderStyle = BorderStyle.FixedSingle;
            pbMotoPlateImg.Dock = DockStyle.Fill;
            pbMotoPlateImg.Location = new System.Drawing.Point(0, 0);
            pbMotoPlateImg.Margin = new Padding(0, 0, 6, 0);
            pbMotoPlateImg.Name = "pbMotoPlateImg";
            pbMotoPlateImg.Size = new System.Drawing.Size(236, 151);
            pbMotoPlateImg.SizeMode = PictureBoxSizeMode.Zoom;
            pbMotoPlateImg.TabIndex = 0;
            pbMotoPlateImg.TabStop = false;
            pbMotoPlateImg.Tag = "captureImg";
            // 
            // lblMotoPlateRegistered
            // 
            lblMotoPlateRegistered.AutoSize = true;
            lblMotoPlateRegistered.BackColor = System.Drawing.Color.White;
            lblMotoPlateRegistered.BorderStyle = BorderStyle.FixedSingle;
            lblMotoPlateRegistered.Dock = DockStyle.Fill;
            lblMotoPlateRegistered.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblMotoPlateRegistered.ForeColor = System.Drawing.Color.Black;
            lblMotoPlateRegistered.Location = new System.Drawing.Point(0, 165);
            lblMotoPlateRegistered.Margin = new Padding(0, 6, 11, 6);
            lblMotoPlateRegistered.Name = "lblMotoPlateRegistered";
            lblMotoPlateRegistered.Size = new System.Drawing.Size(393, 41);
            lblMotoPlateRegistered.TabIndex = 6;
            lblMotoPlateRegistered.Tag = "lblInfo";
            lblMotoPlateRegistered.Text = "Biển số đăng ký: ";
            lblMotoPlateRegistered.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoPlateDetected
            // 
            lblMotoPlateDetected.AutoSize = true;
            lblMotoPlateDetected.BackColor = System.Drawing.Color.White;
            lblMotoPlateDetected.BorderStyle = BorderStyle.FixedSingle;
            lblMotoPlateDetected.Dock = DockStyle.Fill;
            lblMotoPlateDetected.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblMotoPlateDetected.ForeColor = System.Drawing.Color.Black;
            lblMotoPlateDetected.Location = new System.Drawing.Point(0, 218);
            lblMotoPlateDetected.Margin = new Padding(0, 6, 11, 6);
            lblMotoPlateDetected.Name = "lblMotoPlateDetected";
            lblMotoPlateDetected.Size = new System.Drawing.Size(393, 45);
            lblMotoPlateDetected.TabIndex = 7;
            lblMotoPlateDetected.Tag = "lblInfo";
            lblMotoPlateDetected.Text = "Biển số nhận dạng:";
            lblMotoPlateDetected.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlFooter
            // 
            pnlFooter.BackColor = System.Drawing.Color.Transparent;
            tlpMain.SetColumnSpan(pnlFooter, 2);
            pnlFooter.Controls.Add(lbStatusCtrl);
            pnlFooter.Controls.Add(lbdayExpiryDate);
            pnlFooter.Controls.Add(lbRealTime);
            pnlFooter.Controls.Add(label3);
            pnlFooter.Dock = DockStyle.Fill;
            pnlFooter.Location = new System.Drawing.Point(11, 1009);
            pnlFooter.Margin = new Padding(11, 12, 11, 12);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Size = new System.Drawing.Size(1902, 29);
            pnlFooter.TabIndex = 9;
            // 
            // lbStatusCtrl
            // 
            lbStatusCtrl.BackColor = System.Drawing.Color.SeaGreen;
            lbStatusCtrl.Dock = DockStyle.Left;
            lbStatusCtrl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lbStatusCtrl.ForeColor = System.Drawing.Color.White;
            lbStatusCtrl.Location = new System.Drawing.Point(819, 0);
            lbStatusCtrl.Margin = new Padding(0);
            lbStatusCtrl.Name = "lbStatusCtrl";
            lbStatusCtrl.Padding = new Padding(11, 0, 0, 0);
            lbStatusCtrl.Size = new System.Drawing.Size(240, 29);
            lbStatusCtrl.TabIndex = 6;
            lbStatusCtrl.Tag = "201";
            lbStatusCtrl.Text = "BỘ ĐIỀU KHIỂN: ONLINE";
            lbStatusCtrl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbdayExpiryDate
            // 
            lbdayExpiryDate.BackColor = System.Drawing.Color.Peru;
            lbdayExpiryDate.Dock = DockStyle.Left;
            lbdayExpiryDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lbdayExpiryDate.Location = new System.Drawing.Point(593, 0);
            lbdayExpiryDate.Margin = new Padding(0);
            lbdayExpiryDate.Name = "lbdayExpiryDate";
            lbdayExpiryDate.Padding = new Padding(11, 0, 0, 0);
            lbdayExpiryDate.Size = new System.Drawing.Size(226, 29);
            lbdayExpiryDate.TabIndex = 2;
            lbdayExpiryDate.Text = "THỜI HẠN:";
            lbdayExpiryDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbRealTime
            // 
            lbRealTime.BackColor = System.Drawing.Color.SeaGreen;
            lbRealTime.Dock = DockStyle.Left;
            lbRealTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lbRealTime.Location = new System.Drawing.Point(296, 0);
            lbRealTime.Margin = new Padding(0);
            lbRealTime.Name = "lbRealTime";
            lbRealTime.Padding = new Padding(11, 0, 0, 0);
            lbRealTime.Size = new System.Drawing.Size(297, 29);
            lbRealTime.TabIndex = 3;
            lbRealTime.Text = "HÔM NAY:";
            lbRealTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            label3.BackColor = System.Drawing.Color.Teal;
            label3.Dock = DockStyle.Left;
            label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label3.Location = new System.Drawing.Point(0, 0);
            label3.Margin = new Padding(0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(296, 29);
            label3.TabIndex = 4;
            label3.Text = "F1 - VÀO CẤU HÌNH KỸ THUẬT";
            label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FrmMain
            // 
            AccessibleRole = AccessibleRole.None;
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1924, 1050);
            Controls.Add(tlpMain);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(4, 6, 4, 6);
            Name = "FrmMain";
            SizeGripStyle = SizeGripStyle.Show;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "HPPARKING";
            WindowState = FormWindowState.Maximized;
            FormClosing += FrmMain_FormClosing;
            Load += FrmMain_Load;
            tlpMain.ResumeLayout(false);
            tlpTitleMoto.ResumeLayout(false);
            tlpTitleCar.ResumeLayout(false);
            tlpPreviewCar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbCarEntryOverview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCarEntryPlate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCarExitOverview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCarExitPlate).EndInit();
            pnlInfoCar.ResumeLayout(false);
            tlpInfoCar.ResumeLayout(false);
            tlpInfoCar.PerformLayout();
            tlpImgsCar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbCarAvatarImg).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCarPlateImg).EndInit();
            tlpPreviewMoto.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbMotoEntryOverview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbMotoEntryPlate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbMotoExitOverview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbMotoExitPlate).EndInit();
            pnlInfoMoto.ResumeLayout(false);
            tlpInfoMoto.ResumeLayout(false);
            tlpInfoMoto.PerformLayout();
            tlpImgsMoto.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbMotoAvatarImg).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbMotoPlateImg).EndInit();
            pnlFooter.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private TableLayoutPanel tlpMain;
        private TableLayoutPanel tlpPreviewMoto;
        private PictureBox pbMotoEntryPlate;
        private PictureBox pbMotoExitOverview;
        private PictureBox pbMotoExitPlate;
        private PictureBox pbMotoEntryOverview;
        private Panel pnlFooter;
        private Panel pnlInfoMoto;
        private TableLayoutPanel tlpInfoMoto;
        private Label lblMotoIdentityCard;
        private Label lblMotoPlateDetected;
        private Label lblMotoPlateRegistered;
        private Label lblMotoTimeOut;
        private Label lblMotoTimeIn;
        private Label lblMotoDepartment;
        private Label lblMotoFullName;
        private TableLayoutPanel tlpPreviewCar;
        private PictureBox pbCarEntryOverview;
        private PictureBox pbCarEntryPlate;
        private PictureBox pbCarExitOverview;
        private PictureBox pbCarExitPlate;
        private Panel pnlInfoCar;
        private TableLayoutPanel tlpInfoCar;
        private Label lblCarIdentityCard;
        private Label lblCarPlateDetected;
        private Label lblCarPlateRegistered;
        private Label lblCarTimeOut;
        private Label lblCarTimeIn;
        private Label lblCarDepartment;
        private Label lblCarFullName;
        private TableLayoutPanel tlpTitleMoto;
        private Label lblTitleMoto;
        private Label lblLaneInMoto;
        private Label lblLaneOutMoto;
        private TableLayoutPanel tlpTitleCar;
        private Label lblTitleCar;
        private Label lblLaneInCar;
        private Label lblLaneOutCar;
        private TableLayoutPanel tlpImgsMoto;
        private PictureBox pbMotoPlateImg;
        private PictureBox pbMotoAvatarImg;
        private TableLayoutPanel tlpImgsCar;
        private PictureBox pbCarAvatarImg;
        private PictureBox pbCarPlateImg;
        private Label lbdayExpiryDate;
        private Label lbRealTime;
        private Label label3;
        private Label lbStatusCtrl;
    }
}

