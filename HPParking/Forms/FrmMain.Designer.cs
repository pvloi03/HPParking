using System.Drawing;
using System.Windows.Forms;

namespace HPParking.Forms
{
    partial class FrmMain
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
            tlpRoot = new TableLayoutPanel();
            pnlHeader = new Panel();
            lblGateInfo = new Label();
            tlpLanes = new TableLayoutPanel();
            tlpLane1 = new TableLayoutPanel();
            tlpHeadLane1 = new TableLayoutPanel();
            lblTitleLane1 = new Label();
            tlpCamsLane1 = new TableLayoutPanel();
            pbLane1Overview = new PictureBox();
            pbLane1Plate = new PictureBox();
            pbLane1Face = new PictureBox();
            pnlInfoLane1 = new Panel();
            tlpFieldsLane1 = new TableLayoutPanel();
            lblLane1Name = new Label();
            lblLane1Dept = new Label();
            lblLane1TimeIn = new Label();
            lblLane1TimeOut = new Label();
            lblLane1Code = new Label();
            lblLane1Role = new Label();
            pbLane1Avatar = new PictureBox();
            pbLane1FaceSnap = new PictureBox();
            tlpLane2 = new TableLayoutPanel();
            tlpHeadLane2 = new TableLayoutPanel();
            lblTitleLane2 = new Label();
            tlpCamsLane2 = new TableLayoutPanel();
            pbLane2Overview = new PictureBox();
            pbLane2Plate = new PictureBox();
            pbLane2Face = new PictureBox();
            pnlInfoLane2 = new Panel();
            tlpFieldsLane2 = new TableLayoutPanel();
            lblLane2Name = new Label();
            lblLane2Dept = new Label();
            lblLane2TimeIn = new Label();
            lblLane2TimeOut = new Label();
            lblLane2Code = new Label();
            lblLane2Role = new Label();
            pbLane2Avatar = new PictureBox();
            pbLane2FaceSnap = new PictureBox();
            tlpLane3 = new TableLayoutPanel();
            tlpHeadLane3 = new TableLayoutPanel();
            lblTitleLane3 = new Label();
            tlpCamsLane3 = new TableLayoutPanel();
            pbLane3Overview = new PictureBox();
            pbLane3Plate = new PictureBox();
            pbLane3Face = new PictureBox();
            pnlInfoLane3 = new Panel();
            tlpFieldsLane3 = new TableLayoutPanel();
            lblLane3Driver = new Label();
            lblLane3Dept = new Label();
            lblLane3TimeIn = new Label();
            lblLane3TimeOut = new Label();
            lblLane3PlateReg = new Label();
            lblLane3PlateDet = new Label();
            pbLane3PlateCrop = new PictureBox();
            pbLane3DriverAvatar = new PictureBox();
            tlpLane4 = new TableLayoutPanel();
            tlpHeadLane4 = new TableLayoutPanel();
            lblTitleLane4 = new Label();
            tlpCamsLane4 = new TableLayoutPanel();
            pbLane4Overview = new PictureBox();
            pbLane4Plate = new PictureBox();
            pbLane4Face = new PictureBox();
            pnlInfoLane4 = new Panel();
            tlpFieldsLane4 = new TableLayoutPanel();
            lblLane4Driver = new Label();
            lblLane4Dept = new Label();
            lblLane4TimeIn = new Label();
            lblLane4TimeOut = new Label();
            lblLane4PlateReg = new Label();
            lblLane4PlateDet = new Label();
            pbLane4PlateCrop = new PictureBox();
            pbLane4EntrySnap = new PictureBox();
            pnlFooter = new Panel();
            btnReloadHardware = new Button();
            lbServer = new Label();
            lbStatusCtrl = new Label();
            lbdayExpiryDate = new Label();
            lbRealTime = new Label();
            tlpRoot.SuspendLayout();
            pnlHeader.SuspendLayout();
            tlpLanes.SuspendLayout();
            tlpLane1.SuspendLayout();
            tlpHeadLane1.SuspendLayout();
            tlpCamsLane1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane1Overview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1Plate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1Face).BeginInit();
            pnlInfoLane1.SuspendLayout();
            tlpFieldsLane1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane1Avatar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1FaceSnap).BeginInit();
            tlpLane2.SuspendLayout();
            tlpHeadLane2.SuspendLayout();
            tlpCamsLane2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane2Overview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2Plate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2Face).BeginInit();
            pnlInfoLane2.SuspendLayout();
            tlpFieldsLane2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane2Avatar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2FaceSnap).BeginInit();
            tlpLane3.SuspendLayout();
            tlpHeadLane3.SuspendLayout();
            tlpCamsLane3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane3Overview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3Plate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3Face).BeginInit();
            pnlInfoLane3.SuspendLayout();
            tlpFieldsLane3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane3PlateCrop).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3DriverAvatar).BeginInit();
            tlpLane4.SuspendLayout();
            tlpHeadLane4.SuspendLayout();
            tlpCamsLane4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane4Overview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4Plate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4Face).BeginInit();
            pnlInfoLane4.SuspendLayout();
            tlpFieldsLane4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane4PlateCrop).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4EntrySnap).BeginInit();
            pnlFooter.SuspendLayout();
            SuspendLayout();
            // 
            // tlpRoot
            // 
            tlpRoot.ColumnCount = 1;
            tlpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRoot.Controls.Add(pnlHeader, 0, 0);
            tlpRoot.Controls.Add(tlpLanes, 0, 1);
            tlpRoot.Controls.Add(pnlFooter, 0, 2);
            tlpRoot.Dock = DockStyle.Fill;
            tlpRoot.Location = new Point(0, 0);
            tlpRoot.Margin = new Padding(0);
            tlpRoot.Name = "tlpRoot";
            tlpRoot.RowCount = 3;
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tlpRoot.Size = new Size(1924, 1050);
            tlpRoot.TabIndex = 0;
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(15, 23, 42);
            pnlHeader.Controls.Add(lblGateInfo);
            pnlHeader.Dock = DockStyle.Fill;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Margin = new Padding(0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1924, 45);
            pnlHeader.TabIndex = 0;
            // 
            // lblGateInfo
            // 
            lblGateInfo.BackColor = Color.DimGray;
            lblGateInfo.Dock = DockStyle.Fill;
            lblGateInfo.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblGateInfo.ForeColor = Color.White;
            lblGateInfo.Location = new Point(0, 0);
            lblGateInfo.Margin = new Padding(0);
            lblGateInfo.Name = "lblGateInfo";
            lblGateInfo.Padding = new Padding(15, 0, 0, 0);
            lblGateInfo.Size = new Size(1924, 45);
            lblGateInfo.TabIndex = 0;
            lblGateInfo.Text = "HỆ THỐNG KIỂM SOÁT VÀO RA";
            lblGateInfo.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tlpLanes
            // 
            tlpLanes.ColumnCount = 4;
            tlpLanes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpLanes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpLanes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpLanes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpLanes.Controls.Add(tlpLane1, 0, 0);
            tlpLanes.Controls.Add(tlpLane2, 1, 0);
            tlpLanes.Controls.Add(tlpLane3, 2, 0);
            tlpLanes.Controls.Add(tlpLane4, 3, 0);
            tlpLanes.Dock = DockStyle.Fill;
            tlpLanes.Location = new Point(0, 45);
            tlpLanes.Margin = new Padding(0);
            tlpLanes.Name = "tlpLanes";
            tlpLanes.RowCount = 1;
            tlpLanes.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpLanes.Size = new Size(1924, 975);
            tlpLanes.TabIndex = 1;
            // 
            // tlpLane1
            // 
            tlpLane1.ColumnCount = 1;
            tlpLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLane1.Controls.Add(tlpHeadLane1, 0, 0);
            tlpLane1.Controls.Add(tlpCamsLane1, 0, 1);
            tlpLane1.Controls.Add(pnlInfoLane1, 0, 2);
            tlpLane1.Dock = DockStyle.Fill;
            tlpLane1.Location = new Point(4, 5);
            tlpLane1.Margin = new Padding(4, 5, 4, 5);
            tlpLane1.Name = "tlpLane1";
            tlpLane1.RowCount = 3;
            tlpLane1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tlpLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane1.Size = new Size(473, 965);
            tlpLane1.TabIndex = 0;
            // 
            // tlpHeadLane1
            // 
            tlpHeadLane1.ColumnCount = 1;
            tlpHeadLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeadLane1.Controls.Add(lblTitleLane1, 0, 0);
            tlpHeadLane1.Dock = DockStyle.Fill;
            tlpHeadLane1.Location = new Point(0, 0);
            tlpHeadLane1.Margin = new Padding(0);
            tlpHeadLane1.Name = "tlpHeadLane1";
            tlpHeadLane1.RowCount = 1;
            tlpHeadLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpHeadLane1.Size = new Size(473, 50);
            tlpHeadLane1.TabIndex = 0;
            // 
            // lblTitleLane1
            // 
            lblTitleLane1.BackColor = Color.SeaGreen;
            lblTitleLane1.Dock = DockStyle.Fill;
            lblTitleLane1.Font = new Font("Arial", 9F, FontStyle.Bold);
            lblTitleLane1.ForeColor = Color.White;
            lblTitleLane1.Location = new Point(0, 0);
            lblTitleLane1.Margin = new Padding(0);
            lblTitleLane1.Name = "lblTitleLane1";
            lblTitleLane1.Size = new Size(473, 50);
            lblTitleLane1.TabIndex = 0;
            lblTitleLane1.Text = "LÀN VÀO 01";
            lblTitleLane1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane1
            // 
            tlpCamsLane1.BackColor = Color.Black;
            tlpCamsLane1.ColumnCount = 2;
            tlpCamsLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane1.Controls.Add(pbLane1Overview, 0, 0);
            tlpCamsLane1.Controls.Add(pbLane1Plate, 0, 1);
            tlpCamsLane1.Controls.Add(pbLane1Face, 1, 1);
            tlpCamsLane1.Dock = DockStyle.Fill;
            tlpCamsLane1.Location = new Point(0, 55);
            tlpCamsLane1.Margin = new Padding(0, 5, 0, 5);
            tlpCamsLane1.Name = "tlpCamsLane1";
            tlpCamsLane1.RowCount = 2;
            tlpCamsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane1.Size = new Size(473, 493);
            tlpCamsLane1.TabIndex = 1;
            // 
            // pbLane1Overview
            // 
            pbLane1Overview.BackColor = Color.FromArgb(11, 15, 20);
            pbLane1Overview.BorderStyle = BorderStyle.FixedSingle;
            tlpCamsLane1.SetColumnSpan(pbLane1Overview, 2);
            pbLane1Overview.Dock = DockStyle.Fill;
            pbLane1Overview.Location = new Point(3, 3);
            pbLane1Overview.Name = "pbLane1Overview";
            pbLane1Overview.Size = new Size(467, 240);
            pbLane1Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Overview.TabIndex = 0;
            pbLane1Overview.TabStop = false;
            // 
            // pbLane1Plate
            // 
            pbLane1Plate.BackColor = Color.FromArgb(11, 15, 20);
            pbLane1Plate.BorderStyle = BorderStyle.FixedSingle;
            pbLane1Plate.Dock = DockStyle.Fill;
            pbLane1Plate.Location = new Point(3, 249);
            pbLane1Plate.Name = "pbLane1Plate";
            pbLane1Plate.Size = new Size(230, 241);
            pbLane1Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Plate.TabIndex = 1;
            pbLane1Plate.TabStop = false;
            // 
            // pbLane1Face
            // 
            pbLane1Face.BackColor = Color.FromArgb(11, 15, 20);
            pbLane1Face.BorderStyle = BorderStyle.FixedSingle;
            pbLane1Face.Dock = DockStyle.Fill;
            pbLane1Face.Location = new Point(239, 249);
            pbLane1Face.Name = "pbLane1Face";
            pbLane1Face.Size = new Size(231, 241);
            pbLane1Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Face.TabIndex = 2;
            pbLane1Face.TabStop = false;
            // 
            // pnlInfoLane1
            // 
            pnlInfoLane1.BackColor = Color.Gainsboro;
            pnlInfoLane1.Controls.Add(tlpFieldsLane1);
            pnlInfoLane1.Dock = DockStyle.Fill;
            pnlInfoLane1.Location = new Point(0, 558);
            pnlInfoLane1.Margin = new Padding(0, 5, 0, 0);
            pnlInfoLane1.Name = "pnlInfoLane1";
            pnlInfoLane1.Padding = new Padding(9, 10, 9, 10);
            pnlInfoLane1.Size = new Size(473, 407);
            pnlInfoLane1.TabIndex = 2;
            // 
            // tlpFieldsLane1
            // 
            tlpFieldsLane1.ColumnCount = 2;
            tlpFieldsLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpFieldsLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpFieldsLane1.Controls.Add(lblLane1Name, 0, 0);
            tlpFieldsLane1.Controls.Add(lblLane1Dept, 1, 0);
            tlpFieldsLane1.Controls.Add(lblLane1TimeIn, 0, 1);
            tlpFieldsLane1.Controls.Add(lblLane1TimeOut, 1, 1);
            tlpFieldsLane1.Controls.Add(lblLane1Code, 0, 2);
            tlpFieldsLane1.Controls.Add(lblLane1Role, 1, 2);
            tlpFieldsLane1.Controls.Add(pbLane1Avatar, 0, 3);
            tlpFieldsLane1.Controls.Add(pbLane1FaceSnap, 1, 3);
            tlpFieldsLane1.Dock = DockStyle.Fill;
            tlpFieldsLane1.Location = new Point(9, 10);
            tlpFieldsLane1.Margin = new Padding(0);
            tlpFieldsLane1.Name = "tlpFieldsLane1";
            tlpFieldsLane1.RowCount = 4;
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tlpFieldsLane1.Size = new Size(455, 387);
            tlpFieldsLane1.TabIndex = 0;
            // 
            // lblLane1Name
            // 
            lblLane1Name.BackColor = Color.White;
            lblLane1Name.BorderStyle = BorderStyle.FixedSingle;
            lblLane1Name.Dock = DockStyle.Fill;
            lblLane1Name.Font = new Font("Arial", 8F);
            lblLane1Name.ForeColor = Color.Black;
            lblLane1Name.Location = new Point(3, 3);
            lblLane1Name.Margin = new Padding(3);
            lblLane1Name.Name = "lblLane1Name";
            lblLane1Name.Padding = new Padding(6, 0, 0, 0);
            lblLane1Name.Size = new Size(221, 63);
            lblLane1Name.TabIndex = 0;
            lblLane1Name.Text = "Họ và tên:";
            lblLane1Name.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1Dept
            // 
            lblLane1Dept.BackColor = Color.White;
            lblLane1Dept.BorderStyle = BorderStyle.FixedSingle;
            lblLane1Dept.Dock = DockStyle.Fill;
            lblLane1Dept.Font = new Font("Arial", 8F);
            lblLane1Dept.ForeColor = Color.Black;
            lblLane1Dept.Location = new Point(230, 3);
            lblLane1Dept.Margin = new Padding(3);
            lblLane1Dept.Name = "lblLane1Dept";
            lblLane1Dept.Padding = new Padding(6, 0, 0, 0);
            lblLane1Dept.Size = new Size(222, 63);
            lblLane1Dept.TabIndex = 1;
            lblLane1Dept.Text = "Đơn vị:";
            lblLane1Dept.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1TimeIn
            // 
            lblLane1TimeIn.BackColor = Color.White;
            lblLane1TimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblLane1TimeIn.Dock = DockStyle.Fill;
            lblLane1TimeIn.Font = new Font("Arial", 8F);
            lblLane1TimeIn.ForeColor = Color.Black;
            lblLane1TimeIn.Location = new Point(3, 72);
            lblLane1TimeIn.Margin = new Padding(3);
            lblLane1TimeIn.Name = "lblLane1TimeIn";
            lblLane1TimeIn.Padding = new Padding(6, 0, 0, 0);
            lblLane1TimeIn.Size = new Size(221, 63);
            lblLane1TimeIn.TabIndex = 2;
            lblLane1TimeIn.Text = "Ngày vào: --";
            lblLane1TimeIn.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1TimeOut
            // 
            lblLane1TimeOut.BackColor = Color.White;
            lblLane1TimeOut.BorderStyle = BorderStyle.FixedSingle;
            lblLane1TimeOut.Dock = DockStyle.Fill;
            lblLane1TimeOut.Font = new Font("Arial", 8F);
            lblLane1TimeOut.ForeColor = Color.Black;
            lblLane1TimeOut.Location = new Point(230, 72);
            lblLane1TimeOut.Margin = new Padding(3);
            lblLane1TimeOut.Name = "lblLane1TimeOut";
            lblLane1TimeOut.Padding = new Padding(6, 0, 0, 0);
            lblLane1TimeOut.Size = new Size(222, 63);
            lblLane1TimeOut.TabIndex = 3;
            lblLane1TimeOut.Text = "Ngày ra: --";
            lblLane1TimeOut.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1Code
            // 
            lblLane1Code.BackColor = Color.White;
            lblLane1Code.BorderStyle = BorderStyle.FixedSingle;
            lblLane1Code.Dock = DockStyle.Fill;
            lblLane1Code.Font = new Font("Arial", 8F);
            lblLane1Code.ForeColor = Color.Black;
            lblLane1Code.Location = new Point(3, 141);
            lblLane1Code.Margin = new Padding(3);
            lblLane1Code.Name = "lblLane1Code";
            lblLane1Code.Padding = new Padding(6, 0, 0, 0);
            lblLane1Code.Size = new Size(221, 63);
            lblLane1Code.TabIndex = 4;
            lblLane1Code.Text = "Mã:";
            lblLane1Code.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1Role
            // 
            lblLane1Role.BackColor = Color.White;
            lblLane1Role.BorderStyle = BorderStyle.FixedSingle;
            lblLane1Role.Dock = DockStyle.Fill;
            lblLane1Role.Font = new Font("Arial", 8F);
            lblLane1Role.ForeColor = Color.DarkBlue;
            lblLane1Role.Location = new Point(230, 141);
            lblLane1Role.Margin = new Padding(3);
            lblLane1Role.Name = "lblLane1Role";
            lblLane1Role.Padding = new Padding(6, 0, 0, 0);
            lblLane1Role.Size = new Size(222, 63);
            lblLane1Role.TabIndex = 5;
            lblLane1Role.Text = "Đối tượng:";
            lblLane1Role.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbLane1Avatar
            // 
            pbLane1Avatar.BackColor = Color.White;
            pbLane1Avatar.BorderStyle = BorderStyle.FixedSingle;
            pbLane1Avatar.Dock = DockStyle.Fill;
            pbLane1Avatar.Location = new Point(3, 210);
            pbLane1Avatar.Name = "pbLane1Avatar";
            pbLane1Avatar.Size = new Size(221, 174);
            pbLane1Avatar.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Avatar.TabIndex = 6;
            pbLane1Avatar.TabStop = false;
            // 
            // pbLane1FaceSnap
            // 
            pbLane1FaceSnap.BackColor = Color.White;
            pbLane1FaceSnap.BorderStyle = BorderStyle.FixedSingle;
            pbLane1FaceSnap.Dock = DockStyle.Fill;
            pbLane1FaceSnap.Location = new Point(230, 210);
            pbLane1FaceSnap.Name = "pbLane1FaceSnap";
            pbLane1FaceSnap.Size = new Size(222, 174);
            pbLane1FaceSnap.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1FaceSnap.TabIndex = 7;
            pbLane1FaceSnap.TabStop = false;
            // 
            // tlpLane2
            // 
            tlpLane2.ColumnCount = 1;
            tlpLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLane2.Controls.Add(tlpHeadLane2, 0, 0);
            tlpLane2.Controls.Add(tlpCamsLane2, 0, 1);
            tlpLane2.Controls.Add(pnlInfoLane2, 0, 2);
            tlpLane2.Dock = DockStyle.Fill;
            tlpLane2.Location = new Point(485, 5);
            tlpLane2.Margin = new Padding(4, 5, 4, 5);
            tlpLane2.Name = "tlpLane2";
            tlpLane2.RowCount = 3;
            tlpLane2.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tlpLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane2.Size = new Size(473, 965);
            tlpLane2.TabIndex = 1;
            // 
            // tlpHeadLane2
            // 
            tlpHeadLane2.ColumnCount = 1;
            tlpHeadLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeadLane2.Controls.Add(lblTitleLane2, 0, 0);
            tlpHeadLane2.Dock = DockStyle.Fill;
            tlpHeadLane2.Location = new Point(0, 0);
            tlpHeadLane2.Margin = new Padding(0);
            tlpHeadLane2.Name = "tlpHeadLane2";
            tlpHeadLane2.RowCount = 1;
            tlpHeadLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpHeadLane2.Size = new Size(473, 50);
            tlpHeadLane2.TabIndex = 0;
            // 
            // lblTitleLane2
            // 
            lblTitleLane2.BackColor = Color.SeaGreen;
            lblTitleLane2.Dock = DockStyle.Fill;
            lblTitleLane2.Font = new Font("Arial", 9F, FontStyle.Bold);
            lblTitleLane2.ForeColor = Color.White;
            lblTitleLane2.Location = new Point(0, 0);
            lblTitleLane2.Margin = new Padding(0);
            lblTitleLane2.Name = "lblTitleLane2";
            lblTitleLane2.Size = new Size(473, 50);
            lblTitleLane2.TabIndex = 0;
            lblTitleLane2.Text = "LÀN RA 02";
            lblTitleLane2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane2
            // 
            tlpCamsLane2.BackColor = Color.Black;
            tlpCamsLane2.ColumnCount = 2;
            tlpCamsLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane2.Controls.Add(pbLane2Overview, 0, 0);
            tlpCamsLane2.Controls.Add(pbLane2Plate, 0, 1);
            tlpCamsLane2.Controls.Add(pbLane2Face, 1, 1);
            tlpCamsLane2.Dock = DockStyle.Fill;
            tlpCamsLane2.Location = new Point(0, 55);
            tlpCamsLane2.Margin = new Padding(0, 5, 0, 5);
            tlpCamsLane2.Name = "tlpCamsLane2";
            tlpCamsLane2.RowCount = 2;
            tlpCamsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane2.Size = new Size(473, 493);
            tlpCamsLane2.TabIndex = 1;
            // 
            // pbLane2Overview
            // 
            pbLane2Overview.BackColor = Color.FromArgb(11, 15, 20);
            pbLane2Overview.BorderStyle = BorderStyle.FixedSingle;
            tlpCamsLane2.SetColumnSpan(pbLane2Overview, 2);
            pbLane2Overview.Dock = DockStyle.Fill;
            pbLane2Overview.Location = new Point(3, 3);
            pbLane2Overview.Name = "pbLane2Overview";
            pbLane2Overview.Size = new Size(467, 240);
            pbLane2Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Overview.TabIndex = 0;
            pbLane2Overview.TabStop = false;
            // 
            // pbLane2Plate
            // 
            pbLane2Plate.BackColor = Color.FromArgb(11, 15, 20);
            pbLane2Plate.BorderStyle = BorderStyle.FixedSingle;
            pbLane2Plate.Dock = DockStyle.Fill;
            pbLane2Plate.Location = new Point(3, 249);
            pbLane2Plate.Name = "pbLane2Plate";
            pbLane2Plate.Size = new Size(230, 241);
            pbLane2Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Plate.TabIndex = 1;
            pbLane2Plate.TabStop = false;
            // 
            // pbLane2Face
            // 
            pbLane2Face.BackColor = Color.FromArgb(11, 15, 20);
            pbLane2Face.BorderStyle = BorderStyle.FixedSingle;
            pbLane2Face.Dock = DockStyle.Fill;
            pbLane2Face.Location = new Point(239, 249);
            pbLane2Face.Name = "pbLane2Face";
            pbLane2Face.Size = new Size(231, 241);
            pbLane2Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Face.TabIndex = 2;
            pbLane2Face.TabStop = false;
            // 
            // pnlInfoLane2
            // 
            pnlInfoLane2.BackColor = Color.Gainsboro;
            pnlInfoLane2.Controls.Add(tlpFieldsLane2);
            pnlInfoLane2.Dock = DockStyle.Fill;
            pnlInfoLane2.Location = new Point(0, 558);
            pnlInfoLane2.Margin = new Padding(0, 5, 0, 0);
            pnlInfoLane2.Name = "pnlInfoLane2";
            pnlInfoLane2.Padding = new Padding(9, 10, 9, 10);
            pnlInfoLane2.Size = new Size(473, 407);
            pnlInfoLane2.TabIndex = 2;
            // 
            // tlpFieldsLane2
            // 
            tlpFieldsLane2.ColumnCount = 2;
            tlpFieldsLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpFieldsLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpFieldsLane2.Controls.Add(lblLane2Name, 0, 0);
            tlpFieldsLane2.Controls.Add(lblLane2Dept, 1, 0);
            tlpFieldsLane2.Controls.Add(lblLane2TimeIn, 0, 1);
            tlpFieldsLane2.Controls.Add(lblLane2TimeOut, 1, 1);
            tlpFieldsLane2.Controls.Add(lblLane2Code, 0, 2);
            tlpFieldsLane2.Controls.Add(lblLane2Role, 1, 2);
            tlpFieldsLane2.Controls.Add(pbLane2Avatar, 0, 3);
            tlpFieldsLane2.Controls.Add(pbLane2FaceSnap, 1, 3);
            tlpFieldsLane2.Dock = DockStyle.Fill;
            tlpFieldsLane2.Location = new Point(9, 10);
            tlpFieldsLane2.Margin = new Padding(0);
            tlpFieldsLane2.Name = "tlpFieldsLane2";
            tlpFieldsLane2.RowCount = 4;
            tlpFieldsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            tlpFieldsLane2.Size = new Size(455, 387);
            tlpFieldsLane2.TabIndex = 0;
            // 
            // lblLane2Name
            // 
            lblLane2Name.BackColor = Color.White;
            lblLane2Name.BorderStyle = BorderStyle.FixedSingle;
            lblLane2Name.Dock = DockStyle.Fill;
            lblLane2Name.Font = new Font("Arial", 8F);
            lblLane2Name.ForeColor = Color.Black;
            lblLane2Name.Location = new Point(3, 3);
            lblLane2Name.Margin = new Padding(3);
            lblLane2Name.Name = "lblLane2Name";
            lblLane2Name.Padding = new Padding(6, 0, 0, 0);
            lblLane2Name.Size = new Size(221, 63);
            lblLane2Name.TabIndex = 0;
            lblLane2Name.Text = "Họ và tên:";
            lblLane2Name.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2Dept
            // 
            lblLane2Dept.BackColor = Color.White;
            lblLane2Dept.BorderStyle = BorderStyle.FixedSingle;
            lblLane2Dept.Dock = DockStyle.Fill;
            lblLane2Dept.Font = new Font("Arial", 8F);
            lblLane2Dept.ForeColor = Color.Black;
            lblLane2Dept.Location = new Point(230, 3);
            lblLane2Dept.Margin = new Padding(3);
            lblLane2Dept.Name = "lblLane2Dept";
            lblLane2Dept.Padding = new Padding(6, 0, 0, 0);
            lblLane2Dept.Size = new Size(222, 63);
            lblLane2Dept.TabIndex = 1;
            lblLane2Dept.Text = "Đơn vị:";
            lblLane2Dept.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2TimeIn
            // 
            lblLane2TimeIn.BackColor = Color.White;
            lblLane2TimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblLane2TimeIn.Dock = DockStyle.Fill;
            lblLane2TimeIn.Font = new Font("Arial", 8F);
            lblLane2TimeIn.ForeColor = Color.Black;
            lblLane2TimeIn.Location = new Point(3, 72);
            lblLane2TimeIn.Margin = new Padding(3);
            lblLane2TimeIn.Name = "lblLane2TimeIn";
            lblLane2TimeIn.Padding = new Padding(6, 0, 0, 0);
            lblLane2TimeIn.Size = new Size(221, 63);
            lblLane2TimeIn.TabIndex = 2;
            lblLane2TimeIn.Text = "Ngày vào:";
            lblLane2TimeIn.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2TimeOut
            // 
            lblLane2TimeOut.BackColor = Color.White;
            lblLane2TimeOut.BorderStyle = BorderStyle.FixedSingle;
            lblLane2TimeOut.Dock = DockStyle.Fill;
            lblLane2TimeOut.Font = new Font("Arial", 8F);
            lblLane2TimeOut.ForeColor = Color.Black;
            lblLane2TimeOut.Location = new Point(230, 72);
            lblLane2TimeOut.Margin = new Padding(3);
            lblLane2TimeOut.Name = "lblLane2TimeOut";
            lblLane2TimeOut.Padding = new Padding(6, 0, 0, 0);
            lblLane2TimeOut.Size = new Size(222, 63);
            lblLane2TimeOut.TabIndex = 3;
            lblLane2TimeOut.Text = "Ngày ra:";
            lblLane2TimeOut.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2Code
            // 
            lblLane2Code.BackColor = Color.White;
            lblLane2Code.BorderStyle = BorderStyle.FixedSingle;
            lblLane2Code.Dock = DockStyle.Fill;
            lblLane2Code.Font = new Font("Arial", 8F);
            lblLane2Code.ForeColor = Color.Black;
            lblLane2Code.Location = new Point(3, 141);
            lblLane2Code.Margin = new Padding(3);
            lblLane2Code.Name = "lblLane2Code";
            lblLane2Code.Padding = new Padding(6, 0, 0, 0);
            lblLane2Code.Size = new Size(221, 63);
            lblLane2Code.TabIndex = 4;
            lblLane2Code.Text = "Mã:";
            lblLane2Code.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2Role
            // 
            lblLane2Role.BackColor = Color.White;
            lblLane2Role.BorderStyle = BorderStyle.FixedSingle;
            lblLane2Role.Dock = DockStyle.Fill;
            lblLane2Role.Font = new Font("Arial", 8F);
            lblLane2Role.ForeColor = Color.DarkBlue;
            lblLane2Role.Location = new Point(230, 141);
            lblLane2Role.Margin = new Padding(3);
            lblLane2Role.Name = "lblLane2Role";
            lblLane2Role.Padding = new Padding(6, 0, 0, 0);
            lblLane2Role.Size = new Size(222, 63);
            lblLane2Role.TabIndex = 5;
            lblLane2Role.Text = "Đối tượng:";
            lblLane2Role.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbLane2Avatar
            // 
            pbLane2Avatar.BackColor = Color.White;
            pbLane2Avatar.BorderStyle = BorderStyle.FixedSingle;
            pbLane2Avatar.Dock = DockStyle.Fill;
            pbLane2Avatar.Location = new Point(3, 210);
            pbLane2Avatar.Name = "pbLane2Avatar";
            pbLane2Avatar.Size = new Size(221, 174);
            pbLane2Avatar.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Avatar.TabIndex = 6;
            pbLane2Avatar.TabStop = false;
            // 
            // pbLane2FaceSnap
            // 
            pbLane2FaceSnap.BackColor = Color.White;
            pbLane2FaceSnap.BorderStyle = BorderStyle.FixedSingle;
            pbLane2FaceSnap.Dock = DockStyle.Fill;
            pbLane2FaceSnap.Location = new Point(230, 210);
            pbLane2FaceSnap.Name = "pbLane2FaceSnap";
            pbLane2FaceSnap.Size = new Size(222, 174);
            pbLane2FaceSnap.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2FaceSnap.TabIndex = 7;
            pbLane2FaceSnap.TabStop = false;
            // 
            // tlpLane3
            // 
            tlpLane3.ColumnCount = 1;
            tlpLane3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLane3.Controls.Add(tlpHeadLane3, 0, 0);
            tlpLane3.Controls.Add(tlpCamsLane3, 0, 1);
            tlpLane3.Controls.Add(pnlInfoLane3, 0, 2);
            tlpLane3.Dock = DockStyle.Fill;
            tlpLane3.Location = new Point(966, 5);
            tlpLane3.Margin = new Padding(4, 5, 4, 5);
            tlpLane3.Name = "tlpLane3";
            tlpLane3.RowCount = 3;
            tlpLane3.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tlpLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane3.Size = new Size(473, 965);
            tlpLane3.TabIndex = 2;
            // 
            // tlpHeadLane3
            // 
            tlpHeadLane3.ColumnCount = 1;
            tlpHeadLane3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeadLane3.Controls.Add(lblTitleLane3, 0, 0);
            tlpHeadLane3.Dock = DockStyle.Fill;
            tlpHeadLane3.Location = new Point(0, 0);
            tlpHeadLane3.Margin = new Padding(0);
            tlpHeadLane3.Name = "tlpHeadLane3";
            tlpHeadLane3.RowCount = 1;
            tlpHeadLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpHeadLane3.Size = new Size(473, 50);
            tlpHeadLane3.TabIndex = 0;
            // 
            // lblTitleLane3
            // 
            lblTitleLane3.BackColor = Color.SeaGreen;
            lblTitleLane3.Dock = DockStyle.Fill;
            lblTitleLane3.Font = new Font("Arial", 9F, FontStyle.Bold);
            lblTitleLane3.ForeColor = Color.White;
            lblTitleLane3.Location = new Point(0, 0);
            lblTitleLane3.Margin = new Padding(0);
            lblTitleLane3.Name = "lblTitleLane3";
            lblTitleLane3.Size = new Size(473, 50);
            lblTitleLane3.TabIndex = 0;
            lblTitleLane3.Text = "LÀN VÀO 03";
            lblTitleLane3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane3
            // 
            tlpCamsLane3.BackColor = Color.Black;
            tlpCamsLane3.ColumnCount = 2;
            tlpCamsLane3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane3.Controls.Add(pbLane3Overview, 0, 0);
            tlpCamsLane3.Controls.Add(pbLane3Plate, 0, 1);
            tlpCamsLane3.Controls.Add(pbLane3Face, 1, 1);
            tlpCamsLane3.Dock = DockStyle.Fill;
            tlpCamsLane3.Location = new Point(0, 55);
            tlpCamsLane3.Margin = new Padding(0, 5, 0, 5);
            tlpCamsLane3.Name = "tlpCamsLane3";
            tlpCamsLane3.RowCount = 2;
            tlpCamsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane3.Size = new Size(473, 493);
            tlpCamsLane3.TabIndex = 1;
            // 
            // pbLane3Overview
            // 
            pbLane3Overview.BackColor = Color.FromArgb(11, 15, 20);
            pbLane3Overview.BorderStyle = BorderStyle.FixedSingle;
            tlpCamsLane3.SetColumnSpan(pbLane3Overview, 2);
            pbLane3Overview.Dock = DockStyle.Fill;
            pbLane3Overview.Location = new Point(3, 3);
            pbLane3Overview.Name = "pbLane3Overview";
            pbLane3Overview.Size = new Size(467, 240);
            pbLane3Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3Overview.TabIndex = 0;
            pbLane3Overview.TabStop = false;
            // 
            // pbLane3Plate
            // 
            pbLane3Plate.BackColor = Color.FromArgb(11, 15, 20);
            pbLane3Plate.BorderStyle = BorderStyle.FixedSingle;
            pbLane3Plate.Dock = DockStyle.Fill;
            pbLane3Plate.Location = new Point(3, 249);
            pbLane3Plate.Name = "pbLane3Plate";
            pbLane3Plate.Size = new Size(230, 241);
            pbLane3Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3Plate.TabIndex = 1;
            pbLane3Plate.TabStop = false;
            // 
            // pbLane3Face
            // 
            pbLane3Face.BackColor = Color.FromArgb(11, 15, 20);
            pbLane3Face.BorderStyle = BorderStyle.FixedSingle;
            pbLane3Face.Dock = DockStyle.Fill;
            pbLane3Face.Location = new Point(239, 249);
            pbLane3Face.Name = "pbLane3Face";
            pbLane3Face.Size = new Size(231, 241);
            pbLane3Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3Face.TabIndex = 2;
            pbLane3Face.TabStop = false;
            // 
            // pnlInfoLane3
            // 
            pnlInfoLane3.BackColor = Color.Gainsboro;
            pnlInfoLane3.Controls.Add(tlpFieldsLane3);
            pnlInfoLane3.Dock = DockStyle.Fill;
            pnlInfoLane3.Location = new Point(0, 558);
            pnlInfoLane3.Margin = new Padding(0, 5, 0, 0);
            pnlInfoLane3.Name = "pnlInfoLane3";
            pnlInfoLane3.Padding = new Padding(9, 10, 9, 10);
            pnlInfoLane3.Size = new Size(473, 407);
            pnlInfoLane3.TabIndex = 2;
            // 
            // tlpFieldsLane3
            // 
            tlpFieldsLane3.ColumnCount = 2;
            tlpFieldsLane3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpFieldsLane3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpFieldsLane3.Controls.Add(lblLane3Driver, 0, 0);
            tlpFieldsLane3.Controls.Add(lblLane3Dept, 1, 0);
            tlpFieldsLane3.Controls.Add(lblLane3TimeIn, 0, 1);
            tlpFieldsLane3.Controls.Add(lblLane3TimeOut, 1, 1);
            tlpFieldsLane3.Controls.Add(lblLane3PlateReg, 0, 2);
            tlpFieldsLane3.Controls.Add(lblLane3PlateDet, 1, 2);
            tlpFieldsLane3.Controls.Add(pbLane3PlateCrop, 0, 3);
            tlpFieldsLane3.Controls.Add(pbLane3DriverAvatar, 1, 3);
            tlpFieldsLane3.Dock = DockStyle.Fill;
            tlpFieldsLane3.Location = new Point(9, 10);
            tlpFieldsLane3.Margin = new Padding(0);
            tlpFieldsLane3.Name = "tlpFieldsLane3";
            tlpFieldsLane3.RowCount = 4;
            tlpFieldsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            tlpFieldsLane3.Size = new Size(455, 387);
            tlpFieldsLane3.TabIndex = 0;
            // 
            // lblLane3Driver
            // 
            lblLane3Driver.BackColor = Color.White;
            lblLane3Driver.BorderStyle = BorderStyle.FixedSingle;
            lblLane3Driver.Dock = DockStyle.Fill;
            lblLane3Driver.Font = new Font("Arial", 8F);
            lblLane3Driver.ForeColor = Color.Black;
            lblLane3Driver.Location = new Point(3, 3);
            lblLane3Driver.Margin = new Padding(3);
            lblLane3Driver.Name = "lblLane3Driver";
            lblLane3Driver.Padding = new Padding(6, 0, 0, 0);
            lblLane3Driver.Size = new Size(221, 63);
            lblLane3Driver.TabIndex = 0;
            lblLane3Driver.Text = "Tài xế:";
            lblLane3Driver.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3Dept
            // 
            lblLane3Dept.BackColor = Color.White;
            lblLane3Dept.BorderStyle = BorderStyle.FixedSingle;
            lblLane3Dept.Dock = DockStyle.Fill;
            lblLane3Dept.Font = new Font("Arial", 8F);
            lblLane3Dept.ForeColor = Color.Black;
            lblLane3Dept.Location = new Point(230, 3);
            lblLane3Dept.Margin = new Padding(3);
            lblLane3Dept.Name = "lblLane3Dept";
            lblLane3Dept.Padding = new Padding(6, 0, 0, 0);
            lblLane3Dept.Size = new Size(222, 63);
            lblLane3Dept.TabIndex = 1;
            lblLane3Dept.Text = "Đơn vị:";
            lblLane3Dept.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3TimeIn
            // 
            lblLane3TimeIn.BackColor = Color.White;
            lblLane3TimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblLane3TimeIn.Dock = DockStyle.Fill;
            lblLane3TimeIn.Font = new Font("Arial", 8F);
            lblLane3TimeIn.ForeColor = Color.Black;
            lblLane3TimeIn.Location = new Point(3, 72);
            lblLane3TimeIn.Margin = new Padding(3);
            lblLane3TimeIn.Name = "lblLane3TimeIn";
            lblLane3TimeIn.Padding = new Padding(6, 0, 0, 0);
            lblLane3TimeIn.Size = new Size(221, 63);
            lblLane3TimeIn.TabIndex = 2;
            lblLane3TimeIn.Text = "Ngày vào:";
            lblLane3TimeIn.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3TimeOut
            // 
            lblLane3TimeOut.BackColor = Color.White;
            lblLane3TimeOut.BorderStyle = BorderStyle.FixedSingle;
            lblLane3TimeOut.Dock = DockStyle.Fill;
            lblLane3TimeOut.Font = new Font("Arial", 8F);
            lblLane3TimeOut.ForeColor = Color.Black;
            lblLane3TimeOut.Location = new Point(230, 72);
            lblLane3TimeOut.Margin = new Padding(3);
            lblLane3TimeOut.Name = "lblLane3TimeOut";
            lblLane3TimeOut.Padding = new Padding(6, 0, 0, 0);
            lblLane3TimeOut.Size = new Size(222, 63);
            lblLane3TimeOut.TabIndex = 3;
            lblLane3TimeOut.Text = "Ngày ra: --";
            lblLane3TimeOut.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3PlateReg
            // 
            lblLane3PlateReg.BackColor = Color.White;
            lblLane3PlateReg.BorderStyle = BorderStyle.FixedSingle;
            lblLane3PlateReg.Dock = DockStyle.Fill;
            lblLane3PlateReg.Font = new Font("Arial", 8F);
            lblLane3PlateReg.ForeColor = Color.Black;
            lblLane3PlateReg.Location = new Point(3, 141);
            lblLane3PlateReg.Margin = new Padding(3);
            lblLane3PlateReg.Name = "lblLane3PlateReg";
            lblLane3PlateReg.Padding = new Padding(6, 0, 0, 0);
            lblLane3PlateReg.Size = new Size(221, 63);
            lblLane3PlateReg.TabIndex = 4;
            lblLane3PlateReg.Text = "Biển số đăng ký:";
            lblLane3PlateReg.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3PlateDet
            // 
            lblLane3PlateDet.BackColor = Color.White;
            lblLane3PlateDet.BorderStyle = BorderStyle.FixedSingle;
            lblLane3PlateDet.Dock = DockStyle.Fill;
            lblLane3PlateDet.Font = new Font("Arial", 8F);
            lblLane3PlateDet.ForeColor = Color.DarkGreen;
            lblLane3PlateDet.Location = new Point(230, 141);
            lblLane3PlateDet.Margin = new Padding(3);
            lblLane3PlateDet.Name = "lblLane3PlateDet";
            lblLane3PlateDet.Padding = new Padding(6, 0, 0, 0);
            lblLane3PlateDet.Size = new Size(222, 63);
            lblLane3PlateDet.TabIndex = 5;
            lblLane3PlateDet.Text = "Biển số nhận diện:";
            lblLane3PlateDet.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbLane3PlateCrop
            // 
            pbLane3PlateCrop.BackColor = Color.White;
            pbLane3PlateCrop.BorderStyle = BorderStyle.FixedSingle;
            pbLane3PlateCrop.Dock = DockStyle.Fill;
            pbLane3PlateCrop.Location = new Point(3, 210);
            pbLane3PlateCrop.Name = "pbLane3PlateCrop";
            pbLane3PlateCrop.Size = new Size(221, 174);
            pbLane3PlateCrop.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3PlateCrop.TabIndex = 6;
            pbLane3PlateCrop.TabStop = false;
            // 
            // pbLane3DriverAvatar
            // 
            pbLane3DriverAvatar.BackColor = Color.White;
            pbLane3DriverAvatar.BorderStyle = BorderStyle.FixedSingle;
            pbLane3DriverAvatar.Dock = DockStyle.Fill;
            pbLane3DriverAvatar.Location = new Point(230, 210);
            pbLane3DriverAvatar.Name = "pbLane3DriverAvatar";
            pbLane3DriverAvatar.Size = new Size(222, 174);
            pbLane3DriverAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3DriverAvatar.TabIndex = 7;
            pbLane3DriverAvatar.TabStop = false;
            // 
            // tlpLane4
            // 
            tlpLane4.ColumnCount = 1;
            tlpLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLane4.Controls.Add(tlpHeadLane4, 0, 0);
            tlpLane4.Controls.Add(tlpCamsLane4, 0, 1);
            tlpLane4.Controls.Add(pnlInfoLane4, 0, 2);
            tlpLane4.Dock = DockStyle.Fill;
            tlpLane4.Location = new Point(1447, 5);
            tlpLane4.Margin = new Padding(4, 5, 4, 5);
            tlpLane4.Name = "tlpLane4";
            tlpLane4.RowCount = 3;
            tlpLane4.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tlpLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane4.Size = new Size(473, 965);
            tlpLane4.TabIndex = 3;
            // 
            // tlpHeadLane4
            // 
            tlpHeadLane4.ColumnCount = 1;
            tlpHeadLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeadLane4.Controls.Add(lblTitleLane4, 0, 0);
            tlpHeadLane4.Dock = DockStyle.Fill;
            tlpHeadLane4.Location = new Point(0, 0);
            tlpHeadLane4.Margin = new Padding(0);
            tlpHeadLane4.Name = "tlpHeadLane4";
            tlpHeadLane4.RowCount = 1;
            tlpHeadLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpHeadLane4.Size = new Size(473, 50);
            tlpHeadLane4.TabIndex = 0;
            // 
            // lblTitleLane4
            // 
            lblTitleLane4.BackColor = Color.SeaGreen;
            lblTitleLane4.Dock = DockStyle.Fill;
            lblTitleLane4.Font = new Font("Arial", 9F, FontStyle.Bold);
            lblTitleLane4.ForeColor = Color.White;
            lblTitleLane4.Location = new Point(0, 0);
            lblTitleLane4.Margin = new Padding(0);
            lblTitleLane4.Name = "lblTitleLane4";
            lblTitleLane4.Size = new Size(473, 50);
            lblTitleLane4.TabIndex = 0;
            lblTitleLane4.Text = "LÀN RA 04";
            lblTitleLane4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane4
            // 
            tlpCamsLane4.BackColor = Color.Black;
            tlpCamsLane4.ColumnCount = 2;
            tlpCamsLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane4.Controls.Add(pbLane4Overview, 0, 0);
            tlpCamsLane4.Controls.Add(pbLane4Plate, 0, 1);
            tlpCamsLane4.Controls.Add(pbLane4Face, 1, 1);
            tlpCamsLane4.Dock = DockStyle.Fill;
            tlpCamsLane4.Location = new Point(0, 55);
            tlpCamsLane4.Margin = new Padding(0, 5, 0, 5);
            tlpCamsLane4.Name = "tlpCamsLane4";
            tlpCamsLane4.RowCount = 2;
            tlpCamsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane4.Size = new Size(473, 493);
            tlpCamsLane4.TabIndex = 1;
            // 
            // pbLane4Overview
            // 
            pbLane4Overview.BackColor = Color.FromArgb(11, 15, 20);
            pbLane4Overview.BorderStyle = BorderStyle.FixedSingle;
            tlpCamsLane4.SetColumnSpan(pbLane4Overview, 2);
            pbLane4Overview.Dock = DockStyle.Fill;
            pbLane4Overview.Location = new Point(3, 3);
            pbLane4Overview.Name = "pbLane4Overview";
            pbLane4Overview.Size = new Size(467, 240);
            pbLane4Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4Overview.TabIndex = 0;
            pbLane4Overview.TabStop = false;
            // 
            // pbLane4Plate
            // 
            pbLane4Plate.BackColor = Color.FromArgb(11, 15, 20);
            pbLane4Plate.BorderStyle = BorderStyle.FixedSingle;
            pbLane4Plate.Dock = DockStyle.Fill;
            pbLane4Plate.Location = new Point(3, 249);
            pbLane4Plate.Name = "pbLane4Plate";
            pbLane4Plate.Size = new Size(230, 241);
            pbLane4Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4Plate.TabIndex = 1;
            pbLane4Plate.TabStop = false;
            // 
            // pbLane4Face
            // 
            pbLane4Face.BackColor = Color.FromArgb(11, 15, 20);
            pbLane4Face.BorderStyle = BorderStyle.FixedSingle;
            pbLane4Face.Dock = DockStyle.Fill;
            pbLane4Face.Location = new Point(239, 249);
            pbLane4Face.Name = "pbLane4Face";
            pbLane4Face.Size = new Size(231, 241);
            pbLane4Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4Face.TabIndex = 2;
            pbLane4Face.TabStop = false;
            // 
            // pnlInfoLane4
            // 
            pnlInfoLane4.BackColor = Color.Gainsboro;
            pnlInfoLane4.Controls.Add(tlpFieldsLane4);
            pnlInfoLane4.Dock = DockStyle.Fill;
            pnlInfoLane4.Location = new Point(0, 558);
            pnlInfoLane4.Margin = new Padding(0, 5, 0, 0);
            pnlInfoLane4.Name = "pnlInfoLane4";
            pnlInfoLane4.Padding = new Padding(9, 10, 9, 10);
            pnlInfoLane4.Size = new Size(473, 407);
            pnlInfoLane4.TabIndex = 2;
            // 
            // tlpFieldsLane4
            // 
            tlpFieldsLane4.ColumnCount = 2;
            tlpFieldsLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpFieldsLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpFieldsLane4.Controls.Add(lblLane4Driver, 0, 0);
            tlpFieldsLane4.Controls.Add(lblLane4Dept, 1, 0);
            tlpFieldsLane4.Controls.Add(lblLane4TimeIn, 0, 1);
            tlpFieldsLane4.Controls.Add(lblLane4TimeOut, 1, 1);
            tlpFieldsLane4.Controls.Add(lblLane4PlateReg, 0, 2);
            tlpFieldsLane4.Controls.Add(lblLane4PlateDet, 1, 2);
            tlpFieldsLane4.Controls.Add(pbLane4PlateCrop, 0, 3);
            tlpFieldsLane4.Controls.Add(pbLane4EntrySnap, 1, 3);
            tlpFieldsLane4.Dock = DockStyle.Fill;
            tlpFieldsLane4.Location = new Point(9, 10);
            tlpFieldsLane4.Margin = new Padding(0);
            tlpFieldsLane4.Name = "tlpFieldsLane4";
            tlpFieldsLane4.RowCount = 4;
            tlpFieldsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            tlpFieldsLane4.Size = new Size(455, 387);
            tlpFieldsLane4.TabIndex = 0;
            // 
            // lblLane4Driver
            // 
            lblLane4Driver.BackColor = Color.White;
            lblLane4Driver.BorderStyle = BorderStyle.FixedSingle;
            lblLane4Driver.Dock = DockStyle.Fill;
            lblLane4Driver.Font = new Font("Arial", 8F);
            lblLane4Driver.ForeColor = Color.Black;
            lblLane4Driver.Location = new Point(3, 3);
            lblLane4Driver.Margin = new Padding(3);
            lblLane4Driver.Name = "lblLane4Driver";
            lblLane4Driver.Padding = new Padding(6, 0, 0, 0);
            lblLane4Driver.Size = new Size(221, 63);
            lblLane4Driver.TabIndex = 0;
            lblLane4Driver.Text = "Tài xế:";
            lblLane4Driver.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4Dept
            // 
            lblLane4Dept.BackColor = Color.White;
            lblLane4Dept.BorderStyle = BorderStyle.FixedSingle;
            lblLane4Dept.Dock = DockStyle.Fill;
            lblLane4Dept.Font = new Font("Arial", 8F);
            lblLane4Dept.ForeColor = Color.Black;
            lblLane4Dept.Location = new Point(230, 3);
            lblLane4Dept.Margin = new Padding(3);
            lblLane4Dept.Name = "lblLane4Dept";
            lblLane4Dept.Padding = new Padding(6, 0, 0, 0);
            lblLane4Dept.Size = new Size(222, 63);
            lblLane4Dept.TabIndex = 1;
            lblLane4Dept.Text = "Đơn vị:";
            lblLane4Dept.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4TimeIn
            // 
            lblLane4TimeIn.BackColor = Color.White;
            lblLane4TimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblLane4TimeIn.Dock = DockStyle.Fill;
            lblLane4TimeIn.Font = new Font("Arial", 8F);
            lblLane4TimeIn.ForeColor = Color.Black;
            lblLane4TimeIn.Location = new Point(3, 72);
            lblLane4TimeIn.Margin = new Padding(3);
            lblLane4TimeIn.Name = "lblLane4TimeIn";
            lblLane4TimeIn.Padding = new Padding(6, 0, 0, 0);
            lblLane4TimeIn.Size = new Size(221, 63);
            lblLane4TimeIn.TabIndex = 2;
            lblLane4TimeIn.Text = "Ngày vào:";
            lblLane4TimeIn.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4TimeOut
            // 
            lblLane4TimeOut.BackColor = Color.White;
            lblLane4TimeOut.BorderStyle = BorderStyle.FixedSingle;
            lblLane4TimeOut.Dock = DockStyle.Fill;
            lblLane4TimeOut.Font = new Font("Arial", 8F);
            lblLane4TimeOut.ForeColor = Color.Black;
            lblLane4TimeOut.Location = new Point(230, 72);
            lblLane4TimeOut.Margin = new Padding(3);
            lblLane4TimeOut.Name = "lblLane4TimeOut";
            lblLane4TimeOut.Padding = new Padding(6, 0, 0, 0);
            lblLane4TimeOut.Size = new Size(222, 63);
            lblLane4TimeOut.TabIndex = 3;
            lblLane4TimeOut.Text = "Ngày ra:";
            lblLane4TimeOut.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4PlateReg
            // 
            lblLane4PlateReg.BackColor = Color.White;
            lblLane4PlateReg.BorderStyle = BorderStyle.FixedSingle;
            lblLane4PlateReg.Dock = DockStyle.Fill;
            lblLane4PlateReg.Font = new Font("Arial", 8F);
            lblLane4PlateReg.ForeColor = Color.Black;
            lblLane4PlateReg.Location = new Point(3, 141);
            lblLane4PlateReg.Margin = new Padding(3);
            lblLane4PlateReg.Name = "lblLane4PlateReg";
            lblLane4PlateReg.Padding = new Padding(6, 0, 0, 0);
            lblLane4PlateReg.Size = new Size(221, 63);
            lblLane4PlateReg.TabIndex = 4;
            lblLane4PlateReg.Text = "Biển số đăng ký:";
            lblLane4PlateReg.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4PlateDet
            // 
            lblLane4PlateDet.BackColor = Color.White;
            lblLane4PlateDet.BorderStyle = BorderStyle.FixedSingle;
            lblLane4PlateDet.Dock = DockStyle.Fill;
            lblLane4PlateDet.Font = new Font("Arial", 8F);
            lblLane4PlateDet.ForeColor = Color.DarkGreen;
            lblLane4PlateDet.Location = new Point(230, 141);
            lblLane4PlateDet.Margin = new Padding(3);
            lblLane4PlateDet.Name = "lblLane4PlateDet";
            lblLane4PlateDet.Padding = new Padding(6, 0, 0, 0);
            lblLane4PlateDet.Size = new Size(222, 63);
            lblLane4PlateDet.TabIndex = 5;
            lblLane4PlateDet.Text = "Biển số nhận diện:";
            lblLane4PlateDet.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbLane4PlateCrop
            // 
            pbLane4PlateCrop.BackColor = Color.White;
            pbLane4PlateCrop.BorderStyle = BorderStyle.FixedSingle;
            pbLane4PlateCrop.Dock = DockStyle.Fill;
            pbLane4PlateCrop.Location = new Point(3, 210);
            pbLane4PlateCrop.Name = "pbLane4PlateCrop";
            pbLane4PlateCrop.Size = new Size(221, 174);
            pbLane4PlateCrop.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4PlateCrop.TabIndex = 6;
            pbLane4PlateCrop.TabStop = false;
            // 
            // pbLane4EntrySnap
            // 
            pbLane4EntrySnap.BackColor = Color.White;
            pbLane4EntrySnap.BorderStyle = BorderStyle.FixedSingle;
            pbLane4EntrySnap.Dock = DockStyle.Fill;
            pbLane4EntrySnap.Location = new Point(230, 210);
            pbLane4EntrySnap.Name = "pbLane4EntrySnap";
            pbLane4EntrySnap.Size = new Size(222, 174);
            pbLane4EntrySnap.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4EntrySnap.TabIndex = 7;
            pbLane4EntrySnap.TabStop = false;
            // 
            // pnlFooter
            // 
            pnlFooter.BackColor = Color.Gray;
            pnlFooter.Controls.Add(btnReloadHardware);
            pnlFooter.Controls.Add(lbServer);
            pnlFooter.Controls.Add(lbStatusCtrl);
            pnlFooter.Controls.Add(lbdayExpiryDate);
            pnlFooter.Controls.Add(lbRealTime);
            pnlFooter.Dock = DockStyle.Fill;
            pnlFooter.Location = new Point(0, 1020);
            pnlFooter.Margin = new Padding(0);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Size = new Size(1924, 30);
            pnlFooter.TabIndex = 2;
            // 
            // btnReloadHardware
            // 
            btnReloadHardware.BackColor = Color.Crimson;
            btnReloadHardware.Cursor = Cursors.Hand;
            btnReloadHardware.Dock = DockStyle.Left;
            btnReloadHardware.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            btnReloadHardware.FlatAppearance.BorderSize = 0;
            btnReloadHardware.FlatStyle = FlatStyle.Flat;
            btnReloadHardware.Font = new Font("Arial", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnReloadHardware.ForeColor = Color.White;
            btnReloadHardware.Location = new Point(678, 0);
            btnReloadHardware.Margin = new Padding(0);
            btnReloadHardware.Name = "btnReloadHardware";
            btnReloadHardware.Size = new Size(227, 30);
            btnReloadHardware.TabIndex = 5;
            btnReloadHardware.Text = "KHỞI ĐỘNG LẠI";
            btnReloadHardware.UseVisualStyleBackColor = false;
            btnReloadHardware.Click += BtnReloadHardware_Click;
            // 
            // lbServer
            // 
            lbServer.BackColor = Color.Teal;
            lbServer.Dock = DockStyle.Left;
            lbServer.Font = new Font("Arial", 8F);
            lbServer.ForeColor = Color.White;
            lbServer.Location = new Point(477, 0);
            lbServer.Margin = new Padding(4, 0, 4, 0);
            lbServer.Name = "lbServer";
            lbServer.Size = new Size(201, 30);
            lbServer.TabIndex = 0;
            lbServer.Text = "MÁY CHỦ: ONLINE";
            lbServer.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lbStatusCtrl
            // 
            lbStatusCtrl.BackColor = Color.SeaGreen;
            lbStatusCtrl.Dock = DockStyle.Left;
            lbStatusCtrl.Font = new Font("Arial", 8F);
            lbStatusCtrl.ForeColor = Color.White;
            lbStatusCtrl.Location = new Point(229, 0);
            lbStatusCtrl.Margin = new Padding(4, 0, 4, 0);
            lbStatusCtrl.Name = "lbStatusCtrl";
            lbStatusCtrl.Size = new Size(248, 30);
            lbStatusCtrl.TabIndex = 1;
            lbStatusCtrl.Text = "BỘ ĐIỀU KHIỂN: ONLINE";
            lbStatusCtrl.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lbdayExpiryDate
            // 
            lbdayExpiryDate.BackColor = Color.DarkGoldenrod;
            lbdayExpiryDate.Dock = DockStyle.Left;
            lbdayExpiryDate.Font = new Font("Arial", 8F);
            lbdayExpiryDate.ForeColor = Color.White;
            lbdayExpiryDate.Location = new Point(0, 0);
            lbdayExpiryDate.Margin = new Padding(4, 0, 4, 0);
            lbdayExpiryDate.Name = "lbdayExpiryDate";
            lbdayExpiryDate.Size = new Size(229, 30);
            lbdayExpiryDate.TabIndex = 2;
            lbdayExpiryDate.Text = "BẢN QUYỀN: VĨNH VIỄN";
            lbdayExpiryDate.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lbRealTime
            // 
            lbRealTime.Dock = DockStyle.Right;
            lbRealTime.Font = new Font("Arial", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lbRealTime.ForeColor = Color.White;
            lbRealTime.Location = new Point(1567, 0);
            lbRealTime.Margin = new Padding(4, 0, 4, 0);
            lbRealTime.Name = "lbRealTime";
            lbRealTime.Size = new Size(357, 30);
            lbRealTime.TabIndex = 4;
            lbRealTime.Text = "HÔM NAY: 10:00:00 01/10/2026";
            lbRealTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1924, 1050);
            Controls.Add(tlpRoot);
            Font = new Font("Segoe UI", 9F);
            Margin = new Padding(4, 5, 4, 5);
            Name = "FrmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "HPPARKING - QUẢN LÝ VÀO RA BÃI XE THÔNG MINH";
            WindowState = FormWindowState.Maximized;
            FormClosing += FrmMain_FormClosing;
            Load += FrmMain_Load;
            tlpRoot.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            tlpLanes.ResumeLayout(false);
            tlpLane1.ResumeLayout(false);
            tlpHeadLane1.ResumeLayout(false);
            tlpCamsLane1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane1Overview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1Plate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1Face).EndInit();
            pnlInfoLane1.ResumeLayout(false);
            tlpFieldsLane1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane1Avatar).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1FaceSnap).EndInit();
            tlpLane2.ResumeLayout(false);
            tlpHeadLane2.ResumeLayout(false);
            tlpCamsLane2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane2Overview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2Plate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2Face).EndInit();
            pnlInfoLane2.ResumeLayout(false);
            tlpFieldsLane2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane2Avatar).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2FaceSnap).EndInit();
            tlpLane3.ResumeLayout(false);
            tlpHeadLane3.ResumeLayout(false);
            tlpCamsLane3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane3Overview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3Plate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3Face).EndInit();
            pnlInfoLane3.ResumeLayout(false);
            tlpFieldsLane3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane3PlateCrop).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3DriverAvatar).EndInit();
            tlpLane4.ResumeLayout(false);
            tlpHeadLane4.ResumeLayout(false);
            tlpCamsLane4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane4Overview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4Plate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4Face).EndInit();
            pnlInfoLane4.ResumeLayout(false);
            tlpFieldsLane4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane4PlateCrop).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4EntrySnap).EndInit();
            pnlFooter.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tlpRoot;
        private Panel pnlHeader;
        private Label lblGateInfo;

        private TableLayoutPanel tlpLanes;
        private Panel pnlFooter;
        private Label lbServer;
        private Label lbStatusCtrl;
        private Label lbdayExpiryDate;
        private Label lbRealTime;

        // Lane 1 controls
        private TableLayoutPanel tlpLane1;
        private TableLayoutPanel tlpHeadLane1;
        private Label lblTitleLane1;
        private TableLayoutPanel tlpCamsLane1;
        private PictureBox pbLane1Overview;
        private PictureBox pbLane1Plate;
        private PictureBox pbLane1Face;
        private Panel pnlInfoLane1;
        private TableLayoutPanel tlpFieldsLane1;
        private Label lblLane1Name;
        private Label lblLane1Dept;
        private Label lblLane1TimeIn;
        private Label lblLane1TimeOut;
        private Label lblLane1Code;
        private Label lblLane1Role;
        private PictureBox pbLane1Avatar;
        private PictureBox pbLane1FaceSnap;

        // Lane 2 controls
        private TableLayoutPanel tlpLane2;
        private TableLayoutPanel tlpHeadLane2;
        private Label lblTitleLane2;
        private TableLayoutPanel tlpCamsLane2;
        private PictureBox pbLane2Overview;
        private PictureBox pbLane2Plate;
        private PictureBox pbLane2Face;
        private Panel pnlInfoLane2;
        private TableLayoutPanel tlpFieldsLane2;
        private Label lblLane2Name;
        private Label lblLane2Dept;
        private Label lblLane2TimeIn;
        private Label lblLane2TimeOut;
        private Label lblLane2Code;
        private Label lblLane2Role;
        private PictureBox pbLane2Avatar;
        private PictureBox pbLane2FaceSnap;

        // Lane 3 controls
        private TableLayoutPanel tlpLane3;
        private TableLayoutPanel tlpHeadLane3;
        private Label lblTitleLane3;
        private TableLayoutPanel tlpCamsLane3;
        private PictureBox pbLane3Overview;
        private PictureBox pbLane3Plate;
        private PictureBox pbLane3Face;
        private Panel pnlInfoLane3;
        private TableLayoutPanel tlpFieldsLane3;
        private Label lblLane3Driver;
        private Label lblLane3Dept;
        private Label lblLane3TimeIn;
        private Label lblLane3TimeOut;
        private Label lblLane3PlateReg;
        private Label lblLane3PlateDet;
        private PictureBox pbLane3PlateCrop;
        private PictureBox pbLane3DriverAvatar;

        // Lane 4 controls
        private TableLayoutPanel tlpLane4;
        private TableLayoutPanel tlpHeadLane4;
        private Label lblTitleLane4;
        private TableLayoutPanel tlpCamsLane4;
        private PictureBox pbLane4Overview;
        private PictureBox pbLane4Plate;
        private PictureBox pbLane4Face;
        private Panel pnlInfoLane4;
        private TableLayoutPanel tlpFieldsLane4;
        private Label lblLane4Driver;
        private Label lblLane4Dept;
        private Label lblLane4TimeIn;
        private Label lblLane4TimeOut;
        private Label lblLane4PlateReg;
        private Label lblLane4PlateDet;
        private PictureBox pbLane4PlateCrop;
        private PictureBox pbLane4EntrySnap;
        private Button btnReloadHardware;
    }
}
