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
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.tlpTitleMoto = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitleMoto = new System.Windows.Forms.Label();
            this.lblLaneInMoto = new System.Windows.Forms.Label();
            this.lblLaneOutMoto = new System.Windows.Forms.Label();
            this.tlpTitleCar = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitleCar = new System.Windows.Forms.Label();
            this.lblLaneInCar = new System.Windows.Forms.Label();
            this.lblLaneOutCar = new System.Windows.Forms.Label();
            this.tlpPreviewCar = new System.Windows.Forms.TableLayoutPanel();
            this.pbCarEntryOverview = new System.Windows.Forms.PictureBox();
            this.pbCarEntryPlate = new System.Windows.Forms.PictureBox();
            this.pbCarExitOverview = new System.Windows.Forms.PictureBox();
            this.pbCarExitPlate = new System.Windows.Forms.PictureBox();
            this.pnlInfoCar = new System.Windows.Forms.Panel();
            this.tlpInfoCar = new System.Windows.Forms.TableLayoutPanel();
            this.tlpImgsCar = new System.Windows.Forms.TableLayoutPanel();
            this.pbCarPlateOutImg = new System.Windows.Forms.PictureBox();
            this.pbCarPlateInImg = new System.Windows.Forms.PictureBox();
            this.lblCarIdentityCard = new System.Windows.Forms.Label();
            this.lblCarPlateDetected = new System.Windows.Forms.Label();
            this.lblCarPlateRegistered = new System.Windows.Forms.Label();
            this.lblCarTimeOut = new System.Windows.Forms.Label();
            this.lblCarTimeIn = new System.Windows.Forms.Label();
            this.lblCarCardId = new System.Windows.Forms.Label();
            this.lblCarDepartment = new System.Windows.Forms.Label();
            this.lblCarFullName = new System.Windows.Forms.Label();
            this.tlpPreviewMoto = new System.Windows.Forms.TableLayoutPanel();
            this.pbMotoEntryOverview = new System.Windows.Forms.PictureBox();
            this.pbMotoExitOverview = new System.Windows.Forms.PictureBox();
            this.pbMotoExitPlate = new System.Windows.Forms.PictureBox();
            this.pnlInfoMoto = new System.Windows.Forms.Panel();
            this.tlpInfoMoto = new System.Windows.Forms.TableLayoutPanel();
            this.lblMotoIdentityCard = new System.Windows.Forms.Label();
            this.lblMotoPlateDetected = new System.Windows.Forms.Label();
            this.lblMotoPlateRegistered = new System.Windows.Forms.Label();
            this.lblMotoTimeOut = new System.Windows.Forms.Label();
            this.lblMotoTimeIn = new System.Windows.Forms.Label();
            this.lblMotoCardId = new System.Windows.Forms.Label();
            this.lblMotoDepartment = new System.Windows.Forms.Label();
            this.lblMotoFullName = new System.Windows.Forms.Label();
            this.tlpImgsMoto = new System.Windows.Forms.TableLayoutPanel();
            this.pbMotoPlateOutImg = new System.Windows.Forms.PictureBox();
            this.pbMotoPlateInImg = new System.Windows.Forms.PictureBox();
            this.pbMotoEntryPlate = new System.Windows.Forms.PictureBox();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.lblServerStatus = new System.Windows.Forms.Label();
            this.lbdayExpiryDate = new System.Windows.Forms.Label();
            this.lbRealTime = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tlpMain.SuspendLayout();
            this.tlpTitleMoto.SuspendLayout();
            this.tlpTitleCar.SuspendLayout();
            this.tlpPreviewCar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarEntryOverview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarEntryPlate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarExitOverview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarExitPlate)).BeginInit();
            this.pnlInfoCar.SuspendLayout();
            this.tlpInfoCar.SuspendLayout();
            this.tlpImgsCar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarPlateOutImg)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarPlateInImg)).BeginInit();
            this.tlpPreviewMoto.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoEntryOverview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoExitOverview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoExitPlate)).BeginInit();
            this.pnlInfoMoto.SuspendLayout();
            this.tlpInfoMoto.SuspendLayout();
            this.tlpImgsMoto.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoPlateOutImg)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoPlateInImg)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoEntryPlate)).BeginInit();
            this.pnlFooter.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlpMain
            // 
            this.tlpMain.AutoSize = true;
            this.tlpMain.BackColor = System.Drawing.Color.White;
            this.tlpMain.ColumnCount = 2;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.Controls.Add(this.tlpTitleMoto, 0, 0);
            this.tlpMain.Controls.Add(this.tlpTitleCar, 1, 0);
            this.tlpMain.Controls.Add(this.tlpPreviewCar, 1, 1);
            this.tlpMain.Controls.Add(this.tlpPreviewMoto, 0, 1);
            this.tlpMain.Controls.Add(this.pnlFooter, 0, 2);
            this.tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMain.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tlpMain.ForeColor = System.Drawing.Color.White;
            this.tlpMain.Location = new System.Drawing.Point(0, 0);
            this.tlpMain.Margin = new System.Windows.Forms.Padding(0);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 3;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 85F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpMain.Size = new System.Drawing.Size(1898, 1024);
            this.tlpMain.TabIndex = 0;
            // 
            // tlpTitleMoto
            // 
            this.tlpTitleMoto.ColumnCount = 2;
            this.tlpTitleMoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTitleMoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTitleMoto.Controls.Add(this.lblTitleMoto, 0, 0);
            this.tlpTitleMoto.Controls.Add(this.lblLaneInMoto, 0, 1);
            this.tlpTitleMoto.Controls.Add(this.lblLaneOutMoto, 1, 1);
            this.tlpTitleMoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpTitleMoto.Location = new System.Drawing.Point(10, 0);
            this.tlpTitleMoto.Margin = new System.Windows.Forms.Padding(10, 0, 25, 0);
            this.tlpTitleMoto.Name = "tlpTitleMoto";
            this.tlpTitleMoto.RowCount = 2;
            this.tlpTitleMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTitleMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTitleMoto.Size = new System.Drawing.Size(914, 102);
            this.tlpTitleMoto.TabIndex = 11;
            // 
            // lblTitleMoto
            // 
            this.lblTitleMoto.BackColor = System.Drawing.Color.SeaGreen;
            this.tlpTitleMoto.SetColumnSpan(this.lblTitleMoto, 2);
            this.lblTitleMoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitleMoto.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitleMoto.ForeColor = System.Drawing.Color.White;
            this.lblTitleMoto.Location = new System.Drawing.Point(0, 0);
            this.lblTitleMoto.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitleMoto.Name = "lblTitleMoto";
            this.lblTitleMoto.Size = new System.Drawing.Size(914, 51);
            this.lblTitleMoto.TabIndex = 0;
            this.lblTitleMoto.Text = "Xe máy";
            this.lblTitleMoto.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLaneInMoto
            // 
            this.lblLaneInMoto.BackColor = System.Drawing.Color.DarkCyan;
            this.lblLaneInMoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLaneInMoto.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLaneInMoto.ForeColor = System.Drawing.Color.White;
            this.lblLaneInMoto.Location = new System.Drawing.Point(0, 56);
            this.lblLaneInMoto.Margin = new System.Windows.Forms.Padding(0, 5, 5, 0);
            this.lblLaneInMoto.Name = "lblLaneInMoto";
            this.lblLaneInMoto.Size = new System.Drawing.Size(452, 46);
            this.lblLaneInMoto.TabIndex = 1;
            this.lblLaneInMoto.Text = "Làn vào";
            this.lblLaneInMoto.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLaneOutMoto
            // 
            this.lblLaneOutMoto.BackColor = System.Drawing.Color.DarkCyan;
            this.lblLaneOutMoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLaneOutMoto.Location = new System.Drawing.Point(462, 56);
            this.lblLaneOutMoto.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
            this.lblLaneOutMoto.Name = "lblLaneOutMoto";
            this.lblLaneOutMoto.Size = new System.Drawing.Size(452, 46);
            this.lblLaneOutMoto.TabIndex = 2;
            this.lblLaneOutMoto.Text = "Làn ra";
            this.lblLaneOutMoto.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tlpTitleCar
            // 
            this.tlpTitleCar.ColumnCount = 2;
            this.tlpTitleCar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTitleCar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTitleCar.Controls.Add(this.lblTitleCar, 0, 0);
            this.tlpTitleCar.Controls.Add(this.lblLaneInCar, 0, 1);
            this.tlpTitleCar.Controls.Add(this.lblLaneOutCar, 1, 1);
            this.tlpTitleCar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpTitleCar.Location = new System.Drawing.Point(974, 0);
            this.tlpTitleCar.Margin = new System.Windows.Forms.Padding(25, 0, 10, 0);
            this.tlpTitleCar.Name = "tlpTitleCar";
            this.tlpTitleCar.RowCount = 2;
            this.tlpTitleCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTitleCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTitleCar.Size = new System.Drawing.Size(914, 102);
            this.tlpTitleCar.TabIndex = 12;
            // 
            // lblTitleCar
            // 
            this.lblTitleCar.BackColor = System.Drawing.Color.SeaGreen;
            this.tlpTitleCar.SetColumnSpan(this.lblTitleCar, 2);
            this.lblTitleCar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitleCar.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitleCar.ForeColor = System.Drawing.Color.White;
            this.lblTitleCar.Location = new System.Drawing.Point(0, 0);
            this.lblTitleCar.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitleCar.Name = "lblTitleCar";
            this.lblTitleCar.Size = new System.Drawing.Size(914, 51);
            this.lblTitleCar.TabIndex = 0;
            this.lblTitleCar.Text = "Ô tô";
            this.lblTitleCar.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLaneInCar
            // 
            this.lblLaneInCar.BackColor = System.Drawing.Color.DarkCyan;
            this.lblLaneInCar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLaneInCar.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLaneInCar.ForeColor = System.Drawing.Color.White;
            this.lblLaneInCar.Location = new System.Drawing.Point(0, 56);
            this.lblLaneInCar.Margin = new System.Windows.Forms.Padding(0, 5, 5, 0);
            this.lblLaneInCar.Name = "lblLaneInCar";
            this.lblLaneInCar.Size = new System.Drawing.Size(452, 46);
            this.lblLaneInCar.TabIndex = 1;
            this.lblLaneInCar.Text = "Làn vào";
            this.lblLaneInCar.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLaneOutCar
            // 
            this.lblLaneOutCar.BackColor = System.Drawing.Color.DarkCyan;
            this.lblLaneOutCar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLaneOutCar.Location = new System.Drawing.Point(462, 56);
            this.lblLaneOutCar.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
            this.lblLaneOutCar.Name = "lblLaneOutCar";
            this.lblLaneOutCar.Size = new System.Drawing.Size(452, 46);
            this.lblLaneOutCar.TabIndex = 2;
            this.lblLaneOutCar.Text = "Làn ra";
            this.lblLaneOutCar.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tlpPreviewCar
            // 
            this.tlpPreviewCar.BackColor = System.Drawing.Color.Transparent;
            this.tlpPreviewCar.ColumnCount = 2;
            this.tlpPreviewCar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpPreviewCar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpPreviewCar.Controls.Add(this.pbCarEntryOverview, 0, 0);
            this.tlpPreviewCar.Controls.Add(this.pbCarEntryPlate, 0, 1);
            this.tlpPreviewCar.Controls.Add(this.pbCarExitOverview, 1, 0);
            this.tlpPreviewCar.Controls.Add(this.pbCarExitPlate, 1, 1);
            this.tlpPreviewCar.Controls.Add(this.pnlInfoCar, 0, 2);
            this.tlpPreviewCar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpPreviewCar.ForeColor = System.Drawing.Color.White;
            this.tlpPreviewCar.Location = new System.Drawing.Point(969, 107);
            this.tlpPreviewCar.Margin = new System.Windows.Forms.Padding(20, 5, 5, 5);
            this.tlpPreviewCar.Name = "tlpPreviewCar";
            this.tlpPreviewCar.RowCount = 3;
            this.tlpPreviewCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.tlpPreviewCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.tlpPreviewCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.tlpPreviewCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpPreviewCar.Size = new System.Drawing.Size(924, 860);
            this.tlpPreviewCar.TabIndex = 10;
            // 
            // pbCarEntryOverview
            // 
            this.pbCarEntryOverview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pbCarEntryOverview.BackColor = System.Drawing.Color.Black;
            this.pbCarEntryOverview.Location = new System.Drawing.Point(5, 6);
            this.pbCarEntryOverview.Margin = new System.Windows.Forms.Padding(5);
            this.pbCarEntryOverview.Name = "pbCarEntryOverview";
            this.pbCarEntryOverview.Size = new System.Drawing.Size(452, 262);
            this.pbCarEntryOverview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCarEntryOverview.TabIndex = 6;
            this.pbCarEntryOverview.TabStop = false;
            this.pbCarEntryOverview.Tag = "pbPreview";
            // 
            // pbCarEntryPlate
            // 
            this.pbCarEntryPlate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pbCarEntryPlate.BackColor = System.Drawing.Color.Black;
            this.pbCarEntryPlate.Location = new System.Drawing.Point(5, 281);
            this.pbCarEntryPlate.Margin = new System.Windows.Forms.Padding(5);
            this.pbCarEntryPlate.Name = "pbCarEntryPlate";
            this.pbCarEntryPlate.Size = new System.Drawing.Size(452, 262);
            this.pbCarEntryPlate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCarEntryPlate.TabIndex = 1;
            this.pbCarEntryPlate.TabStop = false;
            this.pbCarEntryPlate.Tag = "pbPreview";
            // 
            // pbCarExitOverview
            // 
            this.pbCarExitOverview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pbCarExitOverview.BackColor = System.Drawing.Color.Black;
            this.pbCarExitOverview.Location = new System.Drawing.Point(467, 6);
            this.pbCarExitOverview.Margin = new System.Windows.Forms.Padding(5);
            this.pbCarExitOverview.Name = "pbCarExitOverview";
            this.pbCarExitOverview.Size = new System.Drawing.Size(452, 262);
            this.pbCarExitOverview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCarExitOverview.TabIndex = 2;
            this.pbCarExitOverview.TabStop = false;
            this.pbCarExitOverview.Tag = "pbPreview";
            // 
            // pbCarExitPlate
            // 
            this.pbCarExitPlate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pbCarExitPlate.BackColor = System.Drawing.Color.Black;
            this.pbCarExitPlate.Location = new System.Drawing.Point(467, 281);
            this.pbCarExitPlate.Margin = new System.Windows.Forms.Padding(5);
            this.pbCarExitPlate.Name = "pbCarExitPlate";
            this.pbCarExitPlate.Size = new System.Drawing.Size(452, 262);
            this.pbCarExitPlate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCarExitPlate.TabIndex = 3;
            this.pbCarExitPlate.TabStop = false;
            this.pbCarExitPlate.Tag = "pbPreview";
            // 
            // pnlInfoCar
            // 
            this.pnlInfoCar.BackColor = System.Drawing.Color.Gainsboro;
            this.tlpPreviewCar.SetColumnSpan(this.pnlInfoCar, 2);
            this.pnlInfoCar.Controls.Add(this.tlpInfoCar);
            this.pnlInfoCar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlInfoCar.ForeColor = System.Drawing.Color.Black;
            this.pnlInfoCar.Location = new System.Drawing.Point(5, 570);
            this.pnlInfoCar.Margin = new System.Windows.Forms.Padding(5, 20, 5, 0);
            this.pnlInfoCar.Name = "pnlInfoCar";
            this.pnlInfoCar.Padding = new System.Windows.Forms.Padding(10);
            this.pnlInfoCar.Size = new System.Drawing.Size(914, 290);
            this.pnlInfoCar.TabIndex = 7;
            // 
            // tlpInfoCar
            // 
            this.tlpInfoCar.ColumnCount = 2;
            this.tlpInfoCar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpInfoCar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpInfoCar.Controls.Add(this.tlpImgsCar, 1, 3);
            this.tlpInfoCar.Controls.Add(this.lblCarIdentityCard, 1, 1);
            this.tlpInfoCar.Controls.Add(this.lblCarPlateDetected, 0, 4);
            this.tlpInfoCar.Controls.Add(this.lblCarPlateRegistered, 0, 3);
            this.tlpInfoCar.Controls.Add(this.lblCarTimeOut, 1, 2);
            this.tlpInfoCar.Controls.Add(this.lblCarTimeIn, 0, 2);
            this.tlpInfoCar.Controls.Add(this.lblCarCardId, 0, 1);
            this.tlpInfoCar.Controls.Add(this.lblCarDepartment, 1, 0);
            this.tlpInfoCar.Controls.Add(this.lblCarFullName, 0, 0);
            this.tlpInfoCar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpInfoCar.Location = new System.Drawing.Point(10, 10);
            this.tlpInfoCar.Margin = new System.Windows.Forms.Padding(0);
            this.tlpInfoCar.Name = "tlpInfoCar";
            this.tlpInfoCar.RowCount = 5;
            this.tlpInfoCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpInfoCar.Size = new System.Drawing.Size(894, 270);
            this.tlpInfoCar.TabIndex = 8;
            // 
            // tlpImgsCar
            // 
            this.tlpImgsCar.ColumnCount = 2;
            this.tlpImgsCar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpImgsCar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpImgsCar.Controls.Add(this.pbCarPlateOutImg, 1, 0);
            this.tlpImgsCar.Controls.Add(this.pbCarPlateInImg, 0, 0);
            this.tlpImgsCar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpImgsCar.Location = new System.Drawing.Point(457, 167);
            this.tlpImgsCar.Margin = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.tlpImgsCar.Name = "tlpImgsCar";
            this.tlpImgsCar.RowCount = 1;
            this.tlpInfoCar.SetRowSpan(this.tlpImgsCar, 2);
            this.tlpImgsCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpImgsCar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpImgsCar.Size = new System.Drawing.Size(437, 98);
            this.tlpImgsCar.TabIndex = 10;
            // 
            // pbCarPlateOutImg
            // 
            this.pbCarPlateOutImg.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.pbCarPlateOutImg.BackColor = System.Drawing.Color.Black;
            this.pbCarPlateOutImg.Location = new System.Drawing.Point(243, 0);
            this.pbCarPlateOutImg.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.pbCarPlateOutImg.Name = "pbCarPlateOutImg";
            this.pbCarPlateOutImg.Size = new System.Drawing.Size(174, 98);
            this.pbCarPlateOutImg.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCarPlateOutImg.TabIndex = 1;
            this.pbCarPlateOutImg.TabStop = false;
            this.pbCarPlateOutImg.Tag = "captureImg";
            // 
            // pbCarPlateInImg
            // 
            this.pbCarPlateInImg.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.pbCarPlateInImg.BackColor = System.Drawing.Color.Black;
            this.pbCarPlateInImg.Location = new System.Drawing.Point(19, 0);
            this.pbCarPlateInImg.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.pbCarPlateInImg.Name = "pbCarPlateInImg";
            this.pbCarPlateInImg.Size = new System.Drawing.Size(174, 98);
            this.pbCarPlateInImg.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCarPlateInImg.TabIndex = 0;
            this.pbCarPlateInImg.TabStop = false;
            this.pbCarPlateInImg.Tag = "captureImg";
            // 
            // lblCarIdentityCard
            // 
            this.lblCarIdentityCard.AutoSize = true;
            this.lblCarIdentityCard.BackColor = System.Drawing.Color.White;
            this.lblCarIdentityCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCarIdentityCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCarIdentityCard.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCarIdentityCard.ForeColor = System.Drawing.Color.Black;
            this.lblCarIdentityCard.Location = new System.Drawing.Point(457, 59);
            this.lblCarIdentityCard.Margin = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.lblCarIdentityCard.Name = "lblCarIdentityCard";
            this.lblCarIdentityCard.Size = new System.Drawing.Size(437, 44);
            this.lblCarIdentityCard.TabIndex = 8;
            this.lblCarIdentityCard.Tag = "lblInfo";
            this.lblCarIdentityCard.Text = "Số CCCD: ";
            this.lblCarIdentityCard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarPlateDetected
            // 
            this.lblCarPlateDetected.AutoSize = true;
            this.lblCarPlateDetected.BackColor = System.Drawing.Color.White;
            this.lblCarPlateDetected.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCarPlateDetected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCarPlateDetected.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCarPlateDetected.ForeColor = System.Drawing.Color.Black;
            this.lblCarPlateDetected.Location = new System.Drawing.Point(0, 221);
            this.lblCarPlateDetected.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblCarPlateDetected.Name = "lblCarPlateDetected";
            this.lblCarPlateDetected.Size = new System.Drawing.Size(437, 44);
            this.lblCarPlateDetected.TabIndex = 7;
            this.lblCarPlateDetected.Tag = "lblInfo";
            this.lblCarPlateDetected.Text = "Biển số nhận dạng:";
            this.lblCarPlateDetected.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarPlateRegistered
            // 
            this.lblCarPlateRegistered.AutoSize = true;
            this.lblCarPlateRegistered.BackColor = System.Drawing.Color.White;
            this.lblCarPlateRegistered.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCarPlateRegistered.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCarPlateRegistered.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCarPlateRegistered.ForeColor = System.Drawing.Color.Black;
            this.lblCarPlateRegistered.Location = new System.Drawing.Point(0, 167);
            this.lblCarPlateRegistered.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblCarPlateRegistered.Name = "lblCarPlateRegistered";
            this.lblCarPlateRegistered.Size = new System.Drawing.Size(437, 44);
            this.lblCarPlateRegistered.TabIndex = 6;
            this.lblCarPlateRegistered.Tag = "lblInfo";
            this.lblCarPlateRegistered.Text = "Biển số đăng ký: ";
            this.lblCarPlateRegistered.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarTimeOut
            // 
            this.lblCarTimeOut.AutoSize = true;
            this.lblCarTimeOut.BackColor = System.Drawing.Color.White;
            this.lblCarTimeOut.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCarTimeOut.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCarTimeOut.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCarTimeOut.ForeColor = System.Drawing.Color.Black;
            this.lblCarTimeOut.Location = new System.Drawing.Point(457, 113);
            this.lblCarTimeOut.Margin = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.lblCarTimeOut.Name = "lblCarTimeOut";
            this.lblCarTimeOut.Size = new System.Drawing.Size(437, 44);
            this.lblCarTimeOut.TabIndex = 5;
            this.lblCarTimeOut.Tag = "lblInfo";
            this.lblCarTimeOut.Text = "Ngày ra: ";
            this.lblCarTimeOut.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarTimeIn
            // 
            this.lblCarTimeIn.AutoSize = true;
            this.lblCarTimeIn.BackColor = System.Drawing.Color.White;
            this.lblCarTimeIn.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCarTimeIn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCarTimeIn.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCarTimeIn.ForeColor = System.Drawing.Color.Black;
            this.lblCarTimeIn.Location = new System.Drawing.Point(0, 113);
            this.lblCarTimeIn.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblCarTimeIn.Name = "lblCarTimeIn";
            this.lblCarTimeIn.Size = new System.Drawing.Size(437, 44);
            this.lblCarTimeIn.TabIndex = 4;
            this.lblCarTimeIn.Tag = "lblInfo";
            this.lblCarTimeIn.Text = "Ngày vào:";
            this.lblCarTimeIn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarCardId
            // 
            this.lblCarCardId.AutoSize = true;
            this.lblCarCardId.BackColor = System.Drawing.Color.White;
            this.lblCarCardId.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCarCardId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCarCardId.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCarCardId.ForeColor = System.Drawing.Color.Black;
            this.lblCarCardId.Location = new System.Drawing.Point(0, 59);
            this.lblCarCardId.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblCarCardId.Name = "lblCarCardId";
            this.lblCarCardId.Size = new System.Drawing.Size(437, 44);
            this.lblCarCardId.TabIndex = 2;
            this.lblCarCardId.Tag = "lblInfo";
            this.lblCarCardId.Text = "Mã thẻ: ";
            this.lblCarCardId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarDepartment
            // 
            this.lblCarDepartment.AutoSize = true;
            this.lblCarDepartment.BackColor = System.Drawing.Color.White;
            this.lblCarDepartment.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCarDepartment.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCarDepartment.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCarDepartment.ForeColor = System.Drawing.Color.Black;
            this.lblCarDepartment.Location = new System.Drawing.Point(457, 5);
            this.lblCarDepartment.Margin = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.lblCarDepartment.Name = "lblCarDepartment";
            this.lblCarDepartment.Size = new System.Drawing.Size(437, 44);
            this.lblCarDepartment.TabIndex = 1;
            this.lblCarDepartment.Tag = "lblInfo";
            this.lblCarDepartment.Text = "Phòng ban: ";
            this.lblCarDepartment.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCarFullName
            // 
            this.lblCarFullName.AutoSize = true;
            this.lblCarFullName.BackColor = System.Drawing.Color.White;
            this.lblCarFullName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCarFullName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCarFullName.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCarFullName.ForeColor = System.Drawing.Color.Black;
            this.lblCarFullName.Location = new System.Drawing.Point(0, 5);
            this.lblCarFullName.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblCarFullName.Name = "lblCarFullName";
            this.lblCarFullName.Size = new System.Drawing.Size(437, 44);
            this.lblCarFullName.TabIndex = 0;
            this.lblCarFullName.Tag = "lblInfo";
            this.lblCarFullName.Text = "Họ và tên: ";
            this.lblCarFullName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tlpPreviewMoto
            // 
            this.tlpPreviewMoto.BackColor = System.Drawing.Color.Transparent;
            this.tlpPreviewMoto.ColumnCount = 2;
            this.tlpPreviewMoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpPreviewMoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpPreviewMoto.Controls.Add(this.pbMotoEntryOverview, 0, 0);
            this.tlpPreviewMoto.Controls.Add(this.pbMotoExitOverview, 1, 0);
            this.tlpPreviewMoto.Controls.Add(this.pbMotoExitPlate, 1, 1);
            this.tlpPreviewMoto.Controls.Add(this.pnlInfoMoto, 0, 2);
            this.tlpPreviewMoto.Controls.Add(this.pbMotoEntryPlate, 0, 1);
            this.tlpPreviewMoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpPreviewMoto.ForeColor = System.Drawing.Color.White;
            this.tlpPreviewMoto.Location = new System.Drawing.Point(5, 107);
            this.tlpPreviewMoto.Margin = new System.Windows.Forms.Padding(5, 5, 20, 5);
            this.tlpPreviewMoto.Name = "tlpPreviewMoto";
            this.tlpPreviewMoto.RowCount = 3;
            this.tlpPreviewMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.tlpPreviewMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.tlpPreviewMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.tlpPreviewMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpPreviewMoto.Size = new System.Drawing.Size(924, 860);
            this.tlpPreviewMoto.TabIndex = 6;
            // 
            // pbMotoEntryOverview
            // 
            this.pbMotoEntryOverview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pbMotoEntryOverview.BackColor = System.Drawing.Color.Black;
            this.pbMotoEntryOverview.Location = new System.Drawing.Point(5, 6);
            this.pbMotoEntryOverview.Margin = new System.Windows.Forms.Padding(5);
            this.pbMotoEntryOverview.Name = "pbMotoEntryOverview";
            this.pbMotoEntryOverview.Size = new System.Drawing.Size(452, 262);
            this.pbMotoEntryOverview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbMotoEntryOverview.TabIndex = 6;
            this.pbMotoEntryOverview.TabStop = false;
            this.pbMotoEntryOverview.Tag = "pbPreview";
            // 
            // pbMotoExitOverview
            // 
            this.pbMotoExitOverview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pbMotoExitOverview.BackColor = System.Drawing.Color.Black;
            this.pbMotoExitOverview.Location = new System.Drawing.Point(467, 6);
            this.pbMotoExitOverview.Margin = new System.Windows.Forms.Padding(5);
            this.pbMotoExitOverview.Name = "pbMotoExitOverview";
            this.pbMotoExitOverview.Size = new System.Drawing.Size(452, 262);
            this.pbMotoExitOverview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbMotoExitOverview.TabIndex = 2;
            this.pbMotoExitOverview.TabStop = false;
            this.pbMotoExitOverview.Tag = "pbPreview";
            // 
            // pbMotoExitPlate
            // 
            this.pbMotoExitPlate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pbMotoExitPlate.BackColor = System.Drawing.Color.Black;
            this.pbMotoExitPlate.Location = new System.Drawing.Point(467, 281);
            this.pbMotoExitPlate.Margin = new System.Windows.Forms.Padding(5);
            this.pbMotoExitPlate.Name = "pbMotoExitPlate";
            this.pbMotoExitPlate.Size = new System.Drawing.Size(452, 262);
            this.pbMotoExitPlate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbMotoExitPlate.TabIndex = 3;
            this.pbMotoExitPlate.TabStop = false;
            this.pbMotoExitPlate.Tag = "pbPreview";
            // 
            // pnlInfoMoto
            // 
            this.pnlInfoMoto.BackColor = System.Drawing.Color.Gainsboro;
            this.tlpPreviewMoto.SetColumnSpan(this.pnlInfoMoto, 2);
            this.pnlInfoMoto.Controls.Add(this.tlpInfoMoto);
            this.pnlInfoMoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlInfoMoto.ForeColor = System.Drawing.Color.Black;
            this.pnlInfoMoto.Location = new System.Drawing.Point(5, 570);
            this.pnlInfoMoto.Margin = new System.Windows.Forms.Padding(5, 20, 5, 0);
            this.pnlInfoMoto.Name = "pnlInfoMoto";
            this.pnlInfoMoto.Padding = new System.Windows.Forms.Padding(10);
            this.pnlInfoMoto.Size = new System.Drawing.Size(914, 290);
            this.pnlInfoMoto.TabIndex = 7;
            // 
            // tlpInfoMoto
            // 
            this.tlpInfoMoto.ColumnCount = 2;
            this.tlpInfoMoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpInfoMoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpInfoMoto.Controls.Add(this.lblMotoIdentityCard, 1, 1);
            this.tlpInfoMoto.Controls.Add(this.lblMotoPlateDetected, 0, 4);
            this.tlpInfoMoto.Controls.Add(this.lblMotoPlateRegistered, 0, 3);
            this.tlpInfoMoto.Controls.Add(this.lblMotoTimeOut, 1, 2);
            this.tlpInfoMoto.Controls.Add(this.lblMotoTimeIn, 0, 2);
            this.tlpInfoMoto.Controls.Add(this.lblMotoCardId, 0, 1);
            this.tlpInfoMoto.Controls.Add(this.lblMotoDepartment, 1, 0);
            this.tlpInfoMoto.Controls.Add(this.lblMotoFullName, 0, 0);
            this.tlpInfoMoto.Controls.Add(this.tlpImgsMoto, 1, 3);
            this.tlpInfoMoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpInfoMoto.Location = new System.Drawing.Point(10, 10);
            this.tlpInfoMoto.Margin = new System.Windows.Forms.Padding(0);
            this.tlpInfoMoto.Name = "tlpInfoMoto";
            this.tlpInfoMoto.RowCount = 5;
            this.tlpInfoMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpInfoMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpInfoMoto.Size = new System.Drawing.Size(894, 270);
            this.tlpInfoMoto.TabIndex = 8;
            // 
            // lblMotoIdentityCard
            // 
            this.lblMotoIdentityCard.AutoSize = true;
            this.lblMotoIdentityCard.BackColor = System.Drawing.Color.White;
            this.lblMotoIdentityCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMotoIdentityCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMotoIdentityCard.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMotoIdentityCard.ForeColor = System.Drawing.Color.Black;
            this.lblMotoIdentityCard.Location = new System.Drawing.Point(457, 59);
            this.lblMotoIdentityCard.Margin = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.lblMotoIdentityCard.Name = "lblMotoIdentityCard";
            this.lblMotoIdentityCard.Size = new System.Drawing.Size(437, 44);
            this.lblMotoIdentityCard.TabIndex = 8;
            this.lblMotoIdentityCard.Tag = "lblInfo";
            this.lblMotoIdentityCard.Text = "Số CCCD: ";
            this.lblMotoIdentityCard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoPlateDetected
            // 
            this.lblMotoPlateDetected.AutoSize = true;
            this.lblMotoPlateDetected.BackColor = System.Drawing.Color.White;
            this.lblMotoPlateDetected.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMotoPlateDetected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMotoPlateDetected.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMotoPlateDetected.ForeColor = System.Drawing.Color.Black;
            this.lblMotoPlateDetected.Location = new System.Drawing.Point(0, 221);
            this.lblMotoPlateDetected.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblMotoPlateDetected.Name = "lblMotoPlateDetected";
            this.lblMotoPlateDetected.Size = new System.Drawing.Size(437, 44);
            this.lblMotoPlateDetected.TabIndex = 7;
            this.lblMotoPlateDetected.Tag = "lblInfo";
            this.lblMotoPlateDetected.Text = "Biển số nhận dạng:";
            this.lblMotoPlateDetected.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoPlateRegistered
            // 
            this.lblMotoPlateRegistered.AutoSize = true;
            this.lblMotoPlateRegistered.BackColor = System.Drawing.Color.White;
            this.lblMotoPlateRegistered.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMotoPlateRegistered.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMotoPlateRegistered.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMotoPlateRegistered.ForeColor = System.Drawing.Color.Black;
            this.lblMotoPlateRegistered.Location = new System.Drawing.Point(0, 167);
            this.lblMotoPlateRegistered.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblMotoPlateRegistered.Name = "lblMotoPlateRegistered";
            this.lblMotoPlateRegistered.Size = new System.Drawing.Size(437, 44);
            this.lblMotoPlateRegistered.TabIndex = 6;
            this.lblMotoPlateRegistered.Tag = "lblInfo";
            this.lblMotoPlateRegistered.Text = "Biển số đăng ký: ";
            this.lblMotoPlateRegistered.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoTimeOut
            // 
            this.lblMotoTimeOut.AutoSize = true;
            this.lblMotoTimeOut.BackColor = System.Drawing.Color.White;
            this.lblMotoTimeOut.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMotoTimeOut.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMotoTimeOut.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMotoTimeOut.ForeColor = System.Drawing.Color.Black;
            this.lblMotoTimeOut.Location = new System.Drawing.Point(457, 113);
            this.lblMotoTimeOut.Margin = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.lblMotoTimeOut.Name = "lblMotoTimeOut";
            this.lblMotoTimeOut.Size = new System.Drawing.Size(437, 44);
            this.lblMotoTimeOut.TabIndex = 5;
            this.lblMotoTimeOut.Tag = "lblInfo";
            this.lblMotoTimeOut.Text = "Ngày ra: ";
            this.lblMotoTimeOut.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoTimeIn
            // 
            this.lblMotoTimeIn.AutoSize = true;
            this.lblMotoTimeIn.BackColor = System.Drawing.Color.White;
            this.lblMotoTimeIn.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMotoTimeIn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMotoTimeIn.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMotoTimeIn.ForeColor = System.Drawing.Color.Black;
            this.lblMotoTimeIn.Location = new System.Drawing.Point(0, 113);
            this.lblMotoTimeIn.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblMotoTimeIn.Name = "lblMotoTimeIn";
            this.lblMotoTimeIn.Size = new System.Drawing.Size(437, 44);
            this.lblMotoTimeIn.TabIndex = 4;
            this.lblMotoTimeIn.Tag = "lblInfo";
            this.lblMotoTimeIn.Text = "Ngày vào:";
            this.lblMotoTimeIn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoCardId
            // 
            this.lblMotoCardId.AutoSize = true;
            this.lblMotoCardId.BackColor = System.Drawing.Color.White;
            this.lblMotoCardId.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMotoCardId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMotoCardId.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMotoCardId.ForeColor = System.Drawing.Color.Black;
            this.lblMotoCardId.Location = new System.Drawing.Point(0, 59);
            this.lblMotoCardId.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblMotoCardId.Name = "lblMotoCardId";
            this.lblMotoCardId.Size = new System.Drawing.Size(437, 44);
            this.lblMotoCardId.TabIndex = 2;
            this.lblMotoCardId.Tag = "lblInfo";
            this.lblMotoCardId.Text = "Mã thẻ: ";
            this.lblMotoCardId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoDepartment
            // 
            this.lblMotoDepartment.AutoSize = true;
            this.lblMotoDepartment.BackColor = System.Drawing.Color.White;
            this.lblMotoDepartment.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMotoDepartment.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMotoDepartment.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMotoDepartment.ForeColor = System.Drawing.Color.Black;
            this.lblMotoDepartment.Location = new System.Drawing.Point(457, 5);
            this.lblMotoDepartment.Margin = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.lblMotoDepartment.Name = "lblMotoDepartment";
            this.lblMotoDepartment.Size = new System.Drawing.Size(437, 44);
            this.lblMotoDepartment.TabIndex = 1;
            this.lblMotoDepartment.Tag = "lblInfo";
            this.lblMotoDepartment.Text = "Phòng ban: ";
            this.lblMotoDepartment.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMotoFullName
            // 
            this.lblMotoFullName.AutoSize = true;
            this.lblMotoFullName.BackColor = System.Drawing.Color.White;
            this.lblMotoFullName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMotoFullName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMotoFullName.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMotoFullName.ForeColor = System.Drawing.Color.Black;
            this.lblMotoFullName.Location = new System.Drawing.Point(0, 5);
            this.lblMotoFullName.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.lblMotoFullName.Name = "lblMotoFullName";
            this.lblMotoFullName.Size = new System.Drawing.Size(437, 44);
            this.lblMotoFullName.TabIndex = 0;
            this.lblMotoFullName.Tag = "lblInfo";
            this.lblMotoFullName.Text = "Họ và tên: ";
            this.lblMotoFullName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tlpImgsMoto
            // 
            this.tlpImgsMoto.ColumnCount = 2;
            this.tlpImgsMoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpImgsMoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpImgsMoto.Controls.Add(this.pbMotoPlateOutImg, 1, 0);
            this.tlpImgsMoto.Controls.Add(this.pbMotoPlateInImg, 0, 0);
            this.tlpImgsMoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpImgsMoto.Location = new System.Drawing.Point(457, 167);
            this.tlpImgsMoto.Margin = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.tlpImgsMoto.Name = "tlpImgsMoto";
            this.tlpImgsMoto.RowCount = 1;
            this.tlpInfoMoto.SetRowSpan(this.tlpImgsMoto, 2);
            this.tlpImgsMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpImgsMoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpImgsMoto.Size = new System.Drawing.Size(437, 98);
            this.tlpImgsMoto.TabIndex = 9;
            // 
            // pbMotoPlateOutImg
            // 
            this.pbMotoPlateOutImg.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.pbMotoPlateOutImg.BackColor = System.Drawing.Color.Black;
            this.pbMotoPlateOutImg.Location = new System.Drawing.Point(243, 0);
            this.pbMotoPlateOutImg.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.pbMotoPlateOutImg.Name = "pbMotoPlateOutImg";
            this.pbMotoPlateOutImg.Size = new System.Drawing.Size(174, 98);
            this.pbMotoPlateOutImg.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbMotoPlateOutImg.TabIndex = 1;
            this.pbMotoPlateOutImg.TabStop = false;
            this.pbMotoPlateOutImg.Tag = "captureImg";
            // 
            // pbMotoPlateInImg
            // 
            this.pbMotoPlateInImg.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.pbMotoPlateInImg.BackColor = System.Drawing.Color.Black;
            this.pbMotoPlateInImg.Location = new System.Drawing.Point(19, 0);
            this.pbMotoPlateInImg.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.pbMotoPlateInImg.Name = "pbMotoPlateInImg";
            this.pbMotoPlateInImg.Size = new System.Drawing.Size(174, 98);
            this.pbMotoPlateInImg.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbMotoPlateInImg.TabIndex = 0;
            this.pbMotoPlateInImg.TabStop = false;
            this.pbMotoPlateInImg.Tag = "captureImg";
            // 
            // pbMotoEntryPlate
            // 
            this.pbMotoEntryPlate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pbMotoEntryPlate.BackColor = System.Drawing.Color.Black;
            this.pbMotoEntryPlate.Location = new System.Drawing.Point(5, 281);
            this.pbMotoEntryPlate.Margin = new System.Windows.Forms.Padding(5);
            this.pbMotoEntryPlate.Name = "pbMotoEntryPlate";
            this.pbMotoEntryPlate.Size = new System.Drawing.Size(452, 262);
            this.pbMotoEntryPlate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbMotoEntryPlate.TabIndex = 1;
            this.pbMotoEntryPlate.TabStop = false;
            this.pbMotoEntryPlate.Tag = "pbPreview";
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.Transparent;
            this.tlpMain.SetColumnSpan(this.pnlFooter, 2);
            this.pnlFooter.Controls.Add(this.lblServerStatus);
            this.pnlFooter.Controls.Add(this.lbdayExpiryDate);
            this.pnlFooter.Controls.Add(this.lbRealTime);
            this.pnlFooter.Controls.Add(this.label3);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFooter.Location = new System.Drawing.Point(10, 982);
            this.pnlFooter.Margin = new System.Windows.Forms.Padding(10);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(1878, 32);
            this.pnlFooter.TabIndex = 9;
            // 
            // lblServerStatus
            // 
            this.lblServerStatus.BackColor = System.Drawing.Color.LightGray;
            this.lblServerStatus.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblServerStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblServerStatus.ForeColor = System.Drawing.Color.Green;
            this.lblServerStatus.Location = new System.Drawing.Point(736, 0);
            this.lblServerStatus.Margin = new System.Windows.Forms.Padding(0);
            this.lblServerStatus.Name = "lblServerStatus";
            this.lblServerStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblServerStatus.Size = new System.Drawing.Size(680, 32);
            this.lblServerStatus.TabIndex = 5;
            this.lblServerStatus.Text = "SERVER ĐẦU ĐỌC CCCD: ONLINE";
            this.lblServerStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbdayExpiryDate
            // 
            this.lbdayExpiryDate.BackColor = System.Drawing.Color.Peru;
            this.lbdayExpiryDate.Dock = System.Windows.Forms.DockStyle.Left;
            this.lbdayExpiryDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbdayExpiryDate.Location = new System.Drawing.Point(533, 0);
            this.lbdayExpiryDate.Margin = new System.Windows.Forms.Padding(0);
            this.lbdayExpiryDate.Name = "lbdayExpiryDate";
            this.lbdayExpiryDate.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lbdayExpiryDate.Size = new System.Drawing.Size(203, 32);
            this.lbdayExpiryDate.TabIndex = 2;
            this.lbdayExpiryDate.Text = "THỜI HẠN:";
            this.lbdayExpiryDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbRealTime
            // 
            this.lbRealTime.BackColor = System.Drawing.Color.SeaGreen;
            this.lbRealTime.Dock = System.Windows.Forms.DockStyle.Left;
            this.lbRealTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbRealTime.Location = new System.Drawing.Point(266, 0);
            this.lbRealTime.Margin = new System.Windows.Forms.Padding(0);
            this.lbRealTime.Name = "lbRealTime";
            this.lbRealTime.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lbRealTime.Size = new System.Drawing.Size(267, 32);
            this.lbRealTime.TabIndex = 3;
            this.lbRealTime.Text = "HÔM NAY:";
            this.lbRealTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.Teal;
            this.label3.Dock = System.Windows.Forms.DockStyle.Left;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(0, 0);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(266, 32);
            this.label3.TabIndex = 4;
            this.label3.Text = "F1 - VÀO CẤU HÌNH KỸ THUẬT";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FrmMain
            // 
            this.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1898, 1024);
            this.Controls.Add(this.tlpMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "FrmMain";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HPPARKING";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.tlpMain.ResumeLayout(false);
            this.tlpTitleMoto.ResumeLayout(false);
            this.tlpTitleCar.ResumeLayout(false);
            this.tlpPreviewCar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbCarEntryOverview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarEntryPlate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarExitOverview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarExitPlate)).EndInit();
            this.pnlInfoCar.ResumeLayout(false);
            this.tlpInfoCar.ResumeLayout(false);
            this.tlpInfoCar.PerformLayout();
            this.tlpImgsCar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbCarPlateOutImg)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarPlateInImg)).EndInit();
            this.tlpPreviewMoto.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoEntryOverview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoExitOverview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoExitPlate)).EndInit();
            this.pnlInfoMoto.ResumeLayout(false);
            this.tlpInfoMoto.ResumeLayout(false);
            this.tlpInfoMoto.PerformLayout();
            this.tlpImgsMoto.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoPlateOutImg)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoPlateInImg)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotoEntryPlate)).EndInit();
            this.pnlFooter.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private Label lblMotoCardId;
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
        private Label lblCarCardId;
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
        private PictureBox pbMotoPlateInImg;
        private PictureBox pbMotoPlateOutImg;
        private TableLayoutPanel tlpImgsCar;
        private PictureBox pbCarPlateOutImg;
        private PictureBox pbCarPlateInImg;
        private Label lbdayExpiryDate;
        private Label lbRealTime;
        private Label label3;
        private Label lblServerStatus;
    }
}

