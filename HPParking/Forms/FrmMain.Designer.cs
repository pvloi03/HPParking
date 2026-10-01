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
            pnlTopDemo = new Panel();
            lblDemoTitle = new Label();
            btnMode4Lanes = new Button();
            btnMode2Vehicles = new Button();
            btnMode2Pedestrians = new Button();
            btnMode3Lanes = new Button();
            btnMode1Lane = new Button();
            lblModeNote = new Label();
            tlpLanes = new TableLayoutPanel();
            tlpLane1 = new TableLayoutPanel();
            tlpHeadLane1 = new TableLayoutPanel();
            lblTitleLane1 = new Label();
            lblSubLane1 = new Label();
            tlpCamsLane1 = new TableLayoutPanel();
            pbLane1Overview = new PictureBox();
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
            btnLane1Test = new Button();
            tlpLane2 = new TableLayoutPanel();
            tlpHeadLane2 = new TableLayoutPanel();
            lblTitleLane2 = new Label();
            lblSubLane2 = new Label();
            tlpCamsLane2 = new TableLayoutPanel();
            pbLane2Overview = new PictureBox();
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
            btnLane2Test = new Button();
            tlpLane3 = new TableLayoutPanel();
            tlpHeadLane3 = new TableLayoutPanel();
            lblTitleLane3 = new Label();
            lblSubLane3 = new Label();
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
            btnLane3Test = new Button();
            tlpLane4 = new TableLayoutPanel();
            tlpHeadLane4 = new TableLayoutPanel();
            lblTitleLane4 = new Label();
            lblSubLane4 = new Label();
            tlpCamsLane4 = new TableLayoutPanel();
            pbLane4Overview = new PictureBox();
            pbLane4Plate = new PictureBox();
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
            btnLane4Test = new Button();
            pnlFooter = new Panel();
            lbServer = new Label();
            lbStatusCtrl = new Label();
            lbdayExpiryDate = new Label();
            btnSimulateAll = new Button();
            lbRealTime = new Label();
            tlpRoot.SuspendLayout();
            pnlTopDemo.SuspendLayout();
            tlpLanes.SuspendLayout();
            tlpLane1.SuspendLayout();
            tlpHeadLane1.SuspendLayout();
            tlpCamsLane1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane1Overview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1Face).BeginInit();
            pnlInfoLane1.SuspendLayout();
            tlpFieldsLane1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane1Avatar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1FaceSnap).BeginInit();
            tlpLane2.SuspendLayout();
            tlpHeadLane2.SuspendLayout();
            tlpCamsLane2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane2Overview).BeginInit();
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
            tlpRoot.Controls.Add(pnlTopDemo, 0, 0);
            tlpRoot.Controls.Add(tlpLanes, 0, 1);
            tlpRoot.Controls.Add(pnlFooter, 0, 2);
            tlpRoot.Dock = DockStyle.Fill;
            tlpRoot.Location = new Point(0, 0);
            tlpRoot.Margin = new Padding(0);
            tlpRoot.Name = "tlpRoot";
            tlpRoot.RowCount = 3;
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 77F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tlpRoot.Size = new Size(1924, 1050);
            tlpRoot.TabIndex = 0;
            // 
            // pnlTopDemo
            // 
            pnlTopDemo.BackColor = Color.FromArgb(15, 23, 42);
            pnlTopDemo.Controls.Add(lblDemoTitle);
            pnlTopDemo.Controls.Add(btnMode4Lanes);
            pnlTopDemo.Controls.Add(btnMode2Vehicles);
            pnlTopDemo.Controls.Add(btnMode2Pedestrians);
            pnlTopDemo.Controls.Add(btnMode3Lanes);
            pnlTopDemo.Controls.Add(btnMode1Lane);
            pnlTopDemo.Controls.Add(lblModeNote);
            pnlTopDemo.Dock = DockStyle.Fill;
            pnlTopDemo.Location = new Point(0, 0);
            pnlTopDemo.Margin = new Padding(0);
            pnlTopDemo.Name = "pnlTopDemo";
            pnlTopDemo.Size = new Size(1924, 77);
            pnlTopDemo.TabIndex = 0;
            // 
            // lblDemoTitle
            // 
            lblDemoTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblDemoTitle.ForeColor = Color.Gold;
            lblDemoTitle.Location = new Point(17, 10);
            lblDemoTitle.Margin = new Padding(4, 0, 4, 0);
            lblDemoTitle.Name = "lblDemoTitle";
            lblDemoTitle.Size = new Size(243, 53);
            lblDemoTitle.TabIndex = 0;
            lblDemoTitle.Text = "CHẾ ĐỘ CỔNG (TEST):";
            lblDemoTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnMode4Lanes
            // 
            btnMode4Lanes.BackColor = Color.FromArgb(2, 132, 199);
            btnMode4Lanes.FlatStyle = FlatStyle.Flat;
            btnMode4Lanes.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnMode4Lanes.ForeColor = Color.White;
            btnMode4Lanes.Location = new Point(269, 12);
            btnMode4Lanes.Margin = new Padding(4, 5, 4, 5);
            btnMode4Lanes.Name = "btnMode4Lanes";
            btnMode4Lanes.Size = new Size(293, 53);
            btnMode4Lanes.TabIndex = 1;
            btnMode4Lanes.Text = "⭐ 4 LÀN (2 NGƯỜI + 2 XE)";
            btnMode4Lanes.UseVisualStyleBackColor = false;
            btnMode4Lanes.Click += BtnMode4Lanes_Click;
            // 
            // btnMode2Vehicles
            // 
            btnMode2Vehicles.BackColor = Color.FromArgb(51, 65, 85);
            btnMode2Vehicles.FlatStyle = FlatStyle.Flat;
            btnMode2Vehicles.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnMode2Vehicles.ForeColor = Color.White;
            btnMode2Vehicles.Location = new Point(571, 12);
            btnMode2Vehicles.Margin = new Padding(4, 5, 4, 5);
            btnMode2Vehicles.Name = "btnMode2Vehicles";
            btnMode2Vehicles.Size = new Size(271, 53);
            btnMode2Vehicles.TabIndex = 2;
            btnMode2Vehicles.Text = "🚗 2 LÀN XE (VÀO / RA)";
            btnMode2Vehicles.UseVisualStyleBackColor = false;
            btnMode2Vehicles.Click += BtnMode2Vehicles_Click;
            // 
            // btnMode2Pedestrians
            // 
            btnMode2Pedestrians.BackColor = Color.FromArgb(51, 65, 85);
            btnMode2Pedestrians.FlatStyle = FlatStyle.Flat;
            btnMode2Pedestrians.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnMode2Pedestrians.ForeColor = Color.White;
            btnMode2Pedestrians.Location = new Point(854, 12);
            btnMode2Pedestrians.Margin = new Padding(4, 5, 4, 5);
            btnMode2Pedestrians.Name = "btnMode2Pedestrians";
            btnMode2Pedestrians.Size = new Size(293, 53);
            btnMode2Pedestrians.TabIndex = 3;
            btnMode2Pedestrians.Text = "🚶 2 LÀN NGƯỜI (VÀO / RA)";
            btnMode2Pedestrians.UseVisualStyleBackColor = false;
            btnMode2Pedestrians.Click += BtnMode2Pedestrians_Click;
            // 
            // btnMode3Lanes
            // 
            btnMode3Lanes.BackColor = Color.FromArgb(51, 65, 85);
            btnMode3Lanes.FlatStyle = FlatStyle.Flat;
            btnMode3Lanes.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnMode3Lanes.ForeColor = Color.White;
            btnMode3Lanes.Location = new Point(1157, 12);
            btnMode3Lanes.Margin = new Padding(4, 5, 4, 5);
            btnMode3Lanes.Name = "btnMode3Lanes";
            btnMode3Lanes.Size = new Size(279, 53);
            btnMode3Lanes.TabIndex = 4;
            btnMode3Lanes.Text = "🔀 3 LÀN (1 NGƯỜI + 2 XE)";
            btnMode3Lanes.UseVisualStyleBackColor = false;
            btnMode3Lanes.Click += BtnMode3Lanes_Click;
            // 
            // btnMode1Lane
            // 
            btnMode1Lane.BackColor = Color.FromArgb(51, 65, 85);
            btnMode1Lane.FlatStyle = FlatStyle.Flat;
            btnMode1Lane.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnMode1Lane.ForeColor = Color.White;
            btnMode1Lane.Location = new Point(1446, 12);
            btnMode1Lane.Margin = new Padding(4, 5, 4, 5);
            btnMode1Lane.Name = "btnMode1Lane";
            btnMode1Lane.Size = new Size(200, 53);
            btnMode1Lane.TabIndex = 5;
            btnMode1Lane.Text = "🎯 1 LÀN XE";
            btnMode1Lane.UseVisualStyleBackColor = false;
            btnMode1Lane.Click += BtnMode1Lane_Click;
            // 
            // lblModeNote
            // 
            lblModeNote.AutoSize = true;
            lblModeNote.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblModeNote.ForeColor = Color.LightSkyBlue;
            lblModeNote.Location = new Point(1664, 23);
            lblModeNote.Margin = new Padding(4, 0, 4, 0);
            lblModeNote.Name = "lblModeNote";
            lblModeNote.Size = new Size(613, 23);
            lblModeNote.TabIndex = 6;
            lblModeNote.Text = "💡 Bấm các nút để kiểm tra khả năng tự co giãn (Responsive) theo số làn thực tế!";
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
            tlpLanes.Location = new Point(0, 77);
            tlpLanes.Margin = new Padding(0);
            tlpLanes.Name = "tlpLanes";
            tlpLanes.RowCount = 1;
            tlpLanes.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpLanes.Size = new Size(1924, 923);
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
            tlpLane1.RowStyles.Add(new RowStyle(SizeType.Absolute, 113F));
            tlpLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane1.Size = new Size(473, 913);
            tlpLane1.TabIndex = 0;
            // 
            // tlpHeadLane1
            // 
            tlpHeadLane1.ColumnCount = 1;
            tlpHeadLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeadLane1.Controls.Add(lblTitleLane1, 0, 0);
            tlpHeadLane1.Controls.Add(lblSubLane1, 0, 1);
            tlpHeadLane1.Dock = DockStyle.Fill;
            tlpHeadLane1.Location = new Point(0, 0);
            tlpHeadLane1.Margin = new Padding(0);
            tlpHeadLane1.Name = "tlpHeadLane1";
            tlpHeadLane1.RowCount = 2;
            tlpHeadLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));
            tlpHeadLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            tlpHeadLane1.Size = new Size(473, 113);
            tlpHeadLane1.TabIndex = 0;
            // 
            // lblTitleLane1
            // 
            lblTitleLane1.BackColor = Color.SeaGreen;
            lblTitleLane1.Dock = DockStyle.Fill;
            lblTitleLane1.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTitleLane1.ForeColor = Color.White;
            lblTitleLane1.Location = new Point(0, 0);
            lblTitleLane1.Margin = new Padding(0);
            lblTitleLane1.Name = "lblTitleLane1";
            lblTitleLane1.Size = new Size(473, 61);
            lblTitleLane1.TabIndex = 0;
            lblTitleLane1.Text = "NGƯỜI";
            lblTitleLane1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSubLane1
            // 
            lblSubLane1.BackColor = Color.DarkCyan;
            lblSubLane1.Dock = DockStyle.Fill;
            lblSubLane1.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblSubLane1.ForeColor = Color.White;
            lblSubLane1.Location = new Point(0, 61);
            lblSubLane1.Margin = new Padding(0);
            lblSubLane1.Name = "lblSubLane1";
            lblSubLane1.Size = new Size(473, 52);
            lblSubLane1.TabIndex = 1;
            lblSubLane1.Text = "LÀN NGƯỜI VÀO (VÀO)";
            lblSubLane1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane1
            // 
            tlpCamsLane1.BackColor = Color.Black;
            tlpCamsLane1.ColumnCount = 1;
            tlpCamsLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpCamsLane1.Controls.Add(pbLane1Overview, 0, 0);
            tlpCamsLane1.Controls.Add(pbLane1Face, 0, 1);
            tlpCamsLane1.Dock = DockStyle.Fill;
            tlpCamsLane1.Location = new Point(0, 118);
            tlpCamsLane1.Margin = new Padding(0, 5, 0, 5);
            tlpCamsLane1.Name = "tlpCamsLane1";
            tlpCamsLane1.RowCount = 2;
            tlpCamsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane1.Size = new Size(473, 430);
            tlpCamsLane1.TabIndex = 1;
            // 
            // pbLane1Overview
            // 
            pbLane1Overview.BackColor = Color.FromArgb(11, 15, 20);
            pbLane1Overview.BorderStyle = BorderStyle.FixedSingle;
            pbLane1Overview.Dock = DockStyle.Fill;
            pbLane1Overview.Location = new Point(3, 3);
            pbLane1Overview.Name = "pbLane1Overview";
            pbLane1Overview.Size = new Size(467, 209);
            pbLane1Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Overview.TabIndex = 0;
            pbLane1Overview.TabStop = false;
            // 
            // pbLane1Face
            // 
            pbLane1Face.BackColor = Color.FromArgb(11, 15, 20);
            pbLane1Face.BorderStyle = BorderStyle.FixedSingle;
            pbLane1Face.Dock = DockStyle.Fill;
            pbLane1Face.Location = new Point(3, 218);
            pbLane1Face.Name = "pbLane1Face";
            pbLane1Face.Size = new Size(467, 209);
            pbLane1Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Face.TabIndex = 1;
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
            pnlInfoLane1.Size = new Size(473, 355);
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
            tlpFieldsLane1.Controls.Add(btnLane1Test, 0, 4);
            tlpFieldsLane1.Dock = DockStyle.Fill;
            tlpFieldsLane1.Location = new Point(9, 10);
            tlpFieldsLane1.Margin = new Padding(0);
            tlpFieldsLane1.Name = "tlpFieldsLane1";
            tlpFieldsLane1.RowCount = 5;
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            tlpFieldsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            tlpFieldsLane1.Size = new Size(455, 335);
            tlpFieldsLane1.TabIndex = 0;
            // 
            // lblLane1Name
            // 
            lblLane1Name.BackColor = Color.White;
            lblLane1Name.BorderStyle = BorderStyle.FixedSingle;
            lblLane1Name.Dock = DockStyle.Fill;
            lblLane1Name.Font = new Font("Arial", 9F);
            lblLane1Name.ForeColor = Color.Black;
            lblLane1Name.Location = new Point(3, 3);
            lblLane1Name.Margin = new Padding(3, 3, 3, 3);
            lblLane1Name.Name = "lblLane1Name";
            lblLane1Name.Padding = new Padding(6, 0, 0, 0);
            lblLane1Name.Size = new Size(221, 54);
            lblLane1Name.TabIndex = 0;
            lblLane1Name.Text = "Họ và tên: Trần Văn Mạnh";
            lblLane1Name.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1Dept
            // 
            lblLane1Dept.BackColor = Color.White;
            lblLane1Dept.BorderStyle = BorderStyle.FixedSingle;
            lblLane1Dept.Dock = DockStyle.Fill;
            lblLane1Dept.Font = new Font("Arial", 9F);
            lblLane1Dept.ForeColor = Color.Black;
            lblLane1Dept.Location = new Point(230, 3);
            lblLane1Dept.Margin = new Padding(3, 3, 3, 3);
            lblLane1Dept.Name = "lblLane1Dept";
            lblLane1Dept.Padding = new Padding(6, 0, 0, 0);
            lblLane1Dept.Size = new Size(222, 54);
            lblLane1Dept.TabIndex = 1;
            lblLane1Dept.Text = "Phòng ban: P. Cơ Điện";
            lblLane1Dept.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1TimeIn
            // 
            lblLane1TimeIn.BackColor = Color.White;
            lblLane1TimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblLane1TimeIn.Dock = DockStyle.Fill;
            lblLane1TimeIn.Font = new Font("Arial", 9F);
            lblLane1TimeIn.ForeColor = Color.Black;
            lblLane1TimeIn.Location = new Point(3, 63);
            lblLane1TimeIn.Margin = new Padding(3, 3, 3, 3);
            lblLane1TimeIn.Name = "lblLane1TimeIn";
            lblLane1TimeIn.Padding = new Padding(6, 0, 0, 0);
            lblLane1TimeIn.Size = new Size(221, 54);
            lblLane1TimeIn.TabIndex = 2;
            lblLane1TimeIn.Text = "Ngày vào: 08:15:22 01/10/2026";
            lblLane1TimeIn.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1TimeOut
            // 
            lblLane1TimeOut.BackColor = Color.White;
            lblLane1TimeOut.BorderStyle = BorderStyle.FixedSingle;
            lblLane1TimeOut.Dock = DockStyle.Fill;
            lblLane1TimeOut.Font = new Font("Arial", 9F);
            lblLane1TimeOut.ForeColor = Color.Black;
            lblLane1TimeOut.Location = new Point(230, 63);
            lblLane1TimeOut.Margin = new Padding(3, 3, 3, 3);
            lblLane1TimeOut.Name = "lblLane1TimeOut";
            lblLane1TimeOut.Padding = new Padding(6, 0, 0, 0);
            lblLane1TimeOut.Size = new Size(222, 54);
            lblLane1TimeOut.TabIndex = 3;
            lblLane1TimeOut.Text = "Ngày ra: --";
            lblLane1TimeOut.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1Code
            // 
            lblLane1Code.BackColor = Color.White;
            lblLane1Code.BorderStyle = BorderStyle.FixedSingle;
            lblLane1Code.Dock = DockStyle.Fill;
            lblLane1Code.Font = new Font("Arial", 9F);
            lblLane1Code.ForeColor = Color.Black;
            lblLane1Code.Location = new Point(3, 123);
            lblLane1Code.Margin = new Padding(3, 3, 3, 3);
            lblLane1Code.Name = "lblLane1Code";
            lblLane1Code.Padding = new Padding(6, 0, 0, 0);
            lblLane1Code.Size = new Size(221, 54);
            lblLane1Code.TabIndex = 4;
            lblLane1Code.Text = "CCCD/Mã: 031092004512";
            lblLane1Code.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane1Role
            // 
            lblLane1Role.BackColor = Color.White;
            lblLane1Role.BorderStyle = BorderStyle.FixedSingle;
            lblLane1Role.Dock = DockStyle.Fill;
            lblLane1Role.Font = new Font("Arial", 9F);
            lblLane1Role.ForeColor = Color.DarkBlue;
            lblLane1Role.Location = new Point(230, 123);
            lblLane1Role.Margin = new Padding(3, 3, 3, 3);
            lblLane1Role.Name = "lblLane1Role";
            lblLane1Role.Padding = new Padding(6, 0, 0, 0);
            lblLane1Role.Size = new Size(222, 54);
            lblLane1Role.TabIndex = 5;
            lblLane1Role.Text = "Đối tượng: Nhân viên";
            lblLane1Role.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbLane1Avatar
            // 
            pbLane1Avatar.BackColor = Color.White;
            pbLane1Avatar.BorderStyle = BorderStyle.FixedSingle;
            pbLane1Avatar.Dock = DockStyle.Fill;
            pbLane1Avatar.Location = new Point(3, 183);
            pbLane1Avatar.Name = "pbLane1Avatar";
            pbLane1Avatar.Size = new Size(221, 107);
            pbLane1Avatar.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Avatar.TabIndex = 6;
            pbLane1Avatar.TabStop = false;
            // 
            // pbLane1FaceSnap
            // 
            pbLane1FaceSnap.BackColor = Color.White;
            pbLane1FaceSnap.BorderStyle = BorderStyle.FixedSingle;
            pbLane1FaceSnap.Dock = DockStyle.Fill;
            pbLane1FaceSnap.Location = new Point(230, 183);
            pbLane1FaceSnap.Name = "pbLane1FaceSnap";
            pbLane1FaceSnap.Size = new Size(222, 107);
            pbLane1FaceSnap.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1FaceSnap.TabIndex = 7;
            pbLane1FaceSnap.TabStop = false;
            // 
            // btnLane1Test
            // 
            btnLane1Test.BackColor = Color.DarkCyan;
            tlpFieldsLane1.SetColumnSpan(btnLane1Test, 2);
            btnLane1Test.Dock = DockStyle.Fill;
            btnLane1Test.FlatStyle = FlatStyle.Flat;
            btnLane1Test.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnLane1Test.ForeColor = Color.White;
            btnLane1Test.Location = new Point(3, 296);
            btnLane1Test.Name = "btnLane1Test";
            btnLane1Test.Size = new Size(449, 36);
            btnLane1Test.TabIndex = 8;
            btnLane1Test.Text = "▶ QUẸT THẺ THỬ LÀN NGƯỜI VÀO";
            btnLane1Test.UseVisualStyleBackColor = false;
            btnLane1Test.Click += BtnLane1Test_Click;
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
            tlpLane2.RowStyles.Add(new RowStyle(SizeType.Absolute, 113F));
            tlpLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane2.Size = new Size(473, 913);
            tlpLane2.TabIndex = 1;
            // 
            // tlpHeadLane2
            // 
            tlpHeadLane2.ColumnCount = 1;
            tlpHeadLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeadLane2.Controls.Add(lblTitleLane2, 0, 0);
            tlpHeadLane2.Controls.Add(lblSubLane2, 0, 1);
            tlpHeadLane2.Dock = DockStyle.Fill;
            tlpHeadLane2.Location = new Point(0, 0);
            tlpHeadLane2.Margin = new Padding(0);
            tlpHeadLane2.Name = "tlpHeadLane2";
            tlpHeadLane2.RowCount = 2;
            tlpHeadLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));
            tlpHeadLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            tlpHeadLane2.Size = new Size(473, 113);
            tlpHeadLane2.TabIndex = 0;
            // 
            // lblTitleLane2
            // 
            lblTitleLane2.BackColor = Color.SeaGreen;
            lblTitleLane2.Dock = DockStyle.Fill;
            lblTitleLane2.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTitleLane2.ForeColor = Color.White;
            lblTitleLane2.Location = new Point(0, 0);
            lblTitleLane2.Margin = new Padding(0);
            lblTitleLane2.Name = "lblTitleLane2";
            lblTitleLane2.Size = new Size(473, 61);
            lblTitleLane2.TabIndex = 0;
            lblTitleLane2.Text = "NGƯỜI";
            lblTitleLane2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSubLane2
            // 
            lblSubLane2.BackColor = Color.DarkCyan;
            lblSubLane2.Dock = DockStyle.Fill;
            lblSubLane2.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblSubLane2.ForeColor = Color.White;
            lblSubLane2.Location = new Point(0, 61);
            lblSubLane2.Margin = new Padding(0);
            lblSubLane2.Name = "lblSubLane2";
            lblSubLane2.Size = new Size(473, 52);
            lblSubLane2.TabIndex = 1;
            lblSubLane2.Text = "LÀN NGƯỜI RA (RA)";
            lblSubLane2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane2
            // 
            tlpCamsLane2.BackColor = Color.Black;
            tlpCamsLane2.ColumnCount = 1;
            tlpCamsLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpCamsLane2.Controls.Add(pbLane2Overview, 0, 0);
            tlpCamsLane2.Controls.Add(pbLane2Face, 0, 1);
            tlpCamsLane2.Dock = DockStyle.Fill;
            tlpCamsLane2.Location = new Point(0, 118);
            tlpCamsLane2.Margin = new Padding(0, 5, 0, 5);
            tlpCamsLane2.Name = "tlpCamsLane2";
            tlpCamsLane2.RowCount = 2;
            tlpCamsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane2.Size = new Size(473, 430);
            tlpCamsLane2.TabIndex = 1;
            // 
            // pbLane2Overview
            // 
            pbLane2Overview.BackColor = Color.FromArgb(11, 15, 20);
            pbLane2Overview.BorderStyle = BorderStyle.FixedSingle;
            pbLane2Overview.Dock = DockStyle.Fill;
            pbLane2Overview.Location = new Point(3, 3);
            pbLane2Overview.Name = "pbLane2Overview";
            pbLane2Overview.Size = new Size(467, 209);
            pbLane2Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Overview.TabIndex = 0;
            pbLane2Overview.TabStop = false;
            // 
            // pbLane2Face
            // 
            pbLane2Face.BackColor = Color.FromArgb(11, 15, 20);
            pbLane2Face.BorderStyle = BorderStyle.FixedSingle;
            pbLane2Face.Dock = DockStyle.Fill;
            pbLane2Face.Location = new Point(3, 218);
            pbLane2Face.Name = "pbLane2Face";
            pbLane2Face.Size = new Size(467, 209);
            pbLane2Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Face.TabIndex = 1;
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
            pnlInfoLane2.Size = new Size(473, 355);
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
            tlpFieldsLane2.Controls.Add(btnLane2Test, 0, 4);
            tlpFieldsLane2.Dock = DockStyle.Fill;
            tlpFieldsLane2.Location = new Point(9, 10);
            tlpFieldsLane2.Margin = new Padding(0);
            tlpFieldsLane2.Name = "tlpFieldsLane2";
            tlpFieldsLane2.RowCount = 5;
            tlpFieldsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            tlpFieldsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            tlpFieldsLane2.Size = new Size(455, 335);
            tlpFieldsLane2.TabIndex = 0;
            // 
            // lblLane2Name
            // 
            lblLane2Name.BackColor = Color.White;
            lblLane2Name.BorderStyle = BorderStyle.FixedSingle;
            lblLane2Name.Dock = DockStyle.Fill;
            lblLane2Name.Font = new Font("Arial", 9F);
            lblLane2Name.ForeColor = Color.Black;
            lblLane2Name.Location = new Point(3, 3);
            lblLane2Name.Margin = new Padding(3, 3, 3, 3);
            lblLane2Name.Name = "lblLane2Name";
            lblLane2Name.Padding = new Padding(6, 0, 0, 0);
            lblLane2Name.Size = new Size(221, 54);
            lblLane2Name.TabIndex = 0;
            lblLane2Name.Text = "Họ và tên: Lê Thị Thu";
            lblLane2Name.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2Dept
            // 
            lblLane2Dept.BackColor = Color.White;
            lblLane2Dept.BorderStyle = BorderStyle.FixedSingle;
            lblLane2Dept.Dock = DockStyle.Fill;
            lblLane2Dept.Font = new Font("Arial", 9F);
            lblLane2Dept.ForeColor = Color.Black;
            lblLane2Dept.Location = new Point(230, 3);
            lblLane2Dept.Margin = new Padding(3, 3, 3, 3);
            lblLane2Dept.Name = "lblLane2Dept";
            lblLane2Dept.Padding = new Padding(6, 0, 0, 0);
            lblLane2Dept.Size = new Size(222, 54);
            lblLane2Dept.TabIndex = 1;
            lblLane2Dept.Text = "Nhà thầu: Xây Dựng 5";
            lblLane2Dept.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2TimeIn
            // 
            lblLane2TimeIn.BackColor = Color.White;
            lblLane2TimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblLane2TimeIn.Dock = DockStyle.Fill;
            lblLane2TimeIn.Font = new Font("Arial", 9F);
            lblLane2TimeIn.ForeColor = Color.Black;
            lblLane2TimeIn.Location = new Point(3, 63);
            lblLane2TimeIn.Margin = new Padding(3, 3, 3, 3);
            lblLane2TimeIn.Name = "lblLane2TimeIn";
            lblLane2TimeIn.Padding = new Padding(6, 0, 0, 0);
            lblLane2TimeIn.Size = new Size(221, 54);
            lblLane2TimeIn.TabIndex = 2;
            lblLane2TimeIn.Text = "Ngày vào: 07:30:10 01/10/2026";
            lblLane2TimeIn.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2TimeOut
            // 
            lblLane2TimeOut.BackColor = Color.White;
            lblLane2TimeOut.BorderStyle = BorderStyle.FixedSingle;
            lblLane2TimeOut.Dock = DockStyle.Fill;
            lblLane2TimeOut.Font = new Font("Arial", 9F);
            lblLane2TimeOut.ForeColor = Color.Black;
            lblLane2TimeOut.Location = new Point(230, 63);
            lblLane2TimeOut.Margin = new Padding(3, 3, 3, 3);
            lblLane2TimeOut.Name = "lblLane2TimeOut";
            lblLane2TimeOut.Padding = new Padding(6, 0, 0, 0);
            lblLane2TimeOut.Size = new Size(222, 54);
            lblLane2TimeOut.TabIndex = 3;
            lblLane2TimeOut.Text = "Ngày ra: 11:45:00 01/10/2026";
            lblLane2TimeOut.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2Code
            // 
            lblLane2Code.BackColor = Color.White;
            lblLane2Code.BorderStyle = BorderStyle.FixedSingle;
            lblLane2Code.Dock = DockStyle.Fill;
            lblLane2Code.Font = new Font("Arial", 9F);
            lblLane2Code.ForeColor = Color.Black;
            lblLane2Code.Location = new Point(3, 123);
            lblLane2Code.Margin = new Padding(3, 3, 3, 3);
            lblLane2Code.Name = "lblLane2Code";
            lblLane2Code.Padding = new Padding(6, 0, 0, 0);
            lblLane2Code.Size = new Size(221, 54);
            lblLane2Code.TabIndex = 4;
            lblLane2Code.Text = "CCCD: 036188001293";
            lblLane2Code.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane2Role
            // 
            lblLane2Role.BackColor = Color.White;
            lblLane2Role.BorderStyle = BorderStyle.FixedSingle;
            lblLane2Role.Dock = DockStyle.Fill;
            lblLane2Role.Font = new Font("Arial", 9F);
            lblLane2Role.ForeColor = Color.DarkGoldenrod;
            lblLane2Role.Location = new Point(230, 123);
            lblLane2Role.Margin = new Padding(3, 3, 3, 3);
            lblLane2Role.Name = "lblLane2Role";
            lblLane2Role.Padding = new Padding(6, 0, 0, 0);
            lblLane2Role.Size = new Size(222, 54);
            lblLane2Role.TabIndex = 5;
            lblLane2Role.Text = "Đối tượng: Nhà thầu";
            lblLane2Role.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbLane2Avatar
            // 
            pbLane2Avatar.BackColor = Color.White;
            pbLane2Avatar.BorderStyle = BorderStyle.FixedSingle;
            pbLane2Avatar.Dock = DockStyle.Fill;
            pbLane2Avatar.Location = new Point(3, 183);
            pbLane2Avatar.Name = "pbLane2Avatar";
            pbLane2Avatar.Size = new Size(221, 107);
            pbLane2Avatar.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Avatar.TabIndex = 6;
            pbLane2Avatar.TabStop = false;
            // 
            // pbLane2FaceSnap
            // 
            pbLane2FaceSnap.BackColor = Color.White;
            pbLane2FaceSnap.BorderStyle = BorderStyle.FixedSingle;
            pbLane2FaceSnap.Dock = DockStyle.Fill;
            pbLane2FaceSnap.Location = new Point(230, 183);
            pbLane2FaceSnap.Name = "pbLane2FaceSnap";
            pbLane2FaceSnap.Size = new Size(222, 107);
            pbLane2FaceSnap.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2FaceSnap.TabIndex = 7;
            pbLane2FaceSnap.TabStop = false;
            // 
            // btnLane2Test
            // 
            btnLane2Test.BackColor = Color.DarkCyan;
            tlpFieldsLane2.SetColumnSpan(btnLane2Test, 2);
            btnLane2Test.Dock = DockStyle.Fill;
            btnLane2Test.FlatStyle = FlatStyle.Flat;
            btnLane2Test.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnLane2Test.ForeColor = Color.White;
            btnLane2Test.Location = new Point(3, 296);
            btnLane2Test.Name = "btnLane2Test";
            btnLane2Test.Size = new Size(449, 36);
            btnLane2Test.TabIndex = 8;
            btnLane2Test.Text = "▶ QUẸT THẺ THỬ LÀN NGƯỜI RA";
            btnLane2Test.UseVisualStyleBackColor = false;
            btnLane2Test.Click += BtnLane2Test_Click;
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
            tlpLane3.RowStyles.Add(new RowStyle(SizeType.Absolute, 113F));
            tlpLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane3.Size = new Size(473, 913);
            tlpLane3.TabIndex = 2;
            // 
            // tlpHeadLane3
            // 
            tlpHeadLane3.ColumnCount = 1;
            tlpHeadLane3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeadLane3.Controls.Add(lblTitleLane3, 0, 0);
            tlpHeadLane3.Controls.Add(lblSubLane3, 0, 1);
            tlpHeadLane3.Dock = DockStyle.Fill;
            tlpHeadLane3.Location = new Point(0, 0);
            tlpHeadLane3.Margin = new Padding(0);
            tlpHeadLane3.Name = "tlpHeadLane3";
            tlpHeadLane3.RowCount = 2;
            tlpHeadLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));
            tlpHeadLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            tlpHeadLane3.Size = new Size(473, 113);
            tlpHeadLane3.TabIndex = 0;
            // 
            // lblTitleLane3
            // 
            lblTitleLane3.BackColor = Color.SeaGreen;
            lblTitleLane3.Dock = DockStyle.Fill;
            lblTitleLane3.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTitleLane3.ForeColor = Color.White;
            lblTitleLane3.Location = new Point(0, 0);
            lblTitleLane3.Margin = new Padding(0);
            lblTitleLane3.Name = "lblTitleLane3";
            lblTitleLane3.Size = new Size(473, 61);
            lblTitleLane3.TabIndex = 0;
            lblTitleLane3.Text = "XE";
            lblTitleLane3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSubLane3
            // 
            lblSubLane3.BackColor = Color.DarkCyan;
            lblSubLane3.Dock = DockStyle.Fill;
            lblSubLane3.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblSubLane3.ForeColor = Color.White;
            lblSubLane3.Location = new Point(0, 61);
            lblSubLane3.Margin = new Padding(0);
            lblSubLane3.Name = "lblSubLane3";
            lblSubLane3.Size = new Size(473, 52);
            lblSubLane3.TabIndex = 1;
            lblSubLane3.Text = "LÀN XE VÀO (VÀO - 3 CAM)";
            lblSubLane3.TextAlign = ContentAlignment.MiddleCenter;
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
            tlpCamsLane3.Location = new Point(0, 118);
            tlpCamsLane3.Margin = new Padding(0, 5, 0, 5);
            tlpCamsLane3.Name = "tlpCamsLane3";
            tlpCamsLane3.RowCount = 2;
            tlpCamsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane3.Size = new Size(473, 430);
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
            pbLane3Overview.Size = new Size(467, 209);
            pbLane3Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3Overview.TabIndex = 0;
            pbLane3Overview.TabStop = false;
            // 
            // pbLane3Plate
            // 
            pbLane3Plate.BackColor = Color.FromArgb(11, 15, 20);
            pbLane3Plate.BorderStyle = BorderStyle.FixedSingle;
            pbLane3Plate.Dock = DockStyle.Fill;
            pbLane3Plate.Location = new Point(3, 218);
            pbLane3Plate.Name = "pbLane3Plate";
            pbLane3Plate.Size = new Size(230, 209);
            pbLane3Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3Plate.TabIndex = 1;
            pbLane3Plate.TabStop = false;
            // 
            // pbLane3Face
            // 
            pbLane3Face.BackColor = Color.FromArgb(11, 15, 20);
            pbLane3Face.BorderStyle = BorderStyle.FixedSingle;
            pbLane3Face.Dock = DockStyle.Fill;
            pbLane3Face.Location = new Point(239, 218);
            pbLane3Face.Name = "pbLane3Face";
            pbLane3Face.Size = new Size(231, 209);
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
            pnlInfoLane3.Size = new Size(473, 355);
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
            tlpFieldsLane3.Controls.Add(btnLane3Test, 0, 4);
            tlpFieldsLane3.Dock = DockStyle.Fill;
            tlpFieldsLane3.Location = new Point(9, 10);
            tlpFieldsLane3.Margin = new Padding(0);
            tlpFieldsLane3.Name = "tlpFieldsLane3";
            tlpFieldsLane3.RowCount = 5;
            tlpFieldsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            tlpFieldsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            tlpFieldsLane3.Size = new Size(455, 335);
            tlpFieldsLane3.TabIndex = 0;
            // 
            // lblLane3Driver
            // 
            lblLane3Driver.BackColor = Color.White;
            lblLane3Driver.BorderStyle = BorderStyle.FixedSingle;
            lblLane3Driver.Dock = DockStyle.Fill;
            lblLane3Driver.Font = new Font("Arial", 9F);
            lblLane3Driver.ForeColor = Color.Black;
            lblLane3Driver.Location = new Point(3, 3);
            lblLane3Driver.Margin = new Padding(3, 3, 3, 3);
            lblLane3Driver.Name = "lblLane3Driver";
            lblLane3Driver.Padding = new Padding(6, 0, 0, 0);
            lblLane3Driver.Size = new Size(221, 54);
            lblLane3Driver.TabIndex = 0;
            lblLane3Driver.Text = "Tài xế/Chủ xe: Phạm Quốc Hùng";
            lblLane3Driver.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3Dept
            // 
            lblLane3Dept.BackColor = Color.White;
            lblLane3Dept.BorderStyle = BorderStyle.FixedSingle;
            lblLane3Dept.Dock = DockStyle.Fill;
            lblLane3Dept.Font = new Font("Arial", 9F);
            lblLane3Dept.ForeColor = Color.Black;
            lblLane3Dept.Location = new Point(230, 3);
            lblLane3Dept.Margin = new Padding(3, 3, 3, 3);
            lblLane3Dept.Name = "lblLane3Dept";
            lblLane3Dept.Padding = new Padding(6, 0, 0, 0);
            lblLane3Dept.Size = new Size(222, 54);
            lblLane3Dept.TabIndex = 1;
            lblLane3Dept.Text = "Phòng ban: Đội Vận Tải NB";
            lblLane3Dept.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3TimeIn
            // 
            lblLane3TimeIn.BackColor = Color.White;
            lblLane3TimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblLane3TimeIn.Dock = DockStyle.Fill;
            lblLane3TimeIn.Font = new Font("Arial", 9F);
            lblLane3TimeIn.ForeColor = Color.Black;
            lblLane3TimeIn.Location = new Point(3, 63);
            lblLane3TimeIn.Margin = new Padding(3, 3, 3, 3);
            lblLane3TimeIn.Name = "lblLane3TimeIn";
            lblLane3TimeIn.Padding = new Padding(6, 0, 0, 0);
            lblLane3TimeIn.Size = new Size(221, 54);
            lblLane3TimeIn.TabIndex = 2;
            lblLane3TimeIn.Text = "Ngày vào: 10:20:15 01/10/2026";
            lblLane3TimeIn.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3TimeOut
            // 
            lblLane3TimeOut.BackColor = Color.White;
            lblLane3TimeOut.BorderStyle = BorderStyle.FixedSingle;
            lblLane3TimeOut.Dock = DockStyle.Fill;
            lblLane3TimeOut.Font = new Font("Arial", 9F);
            lblLane3TimeOut.ForeColor = Color.Black;
            lblLane3TimeOut.Location = new Point(230, 63);
            lblLane3TimeOut.Margin = new Padding(3, 3, 3, 3);
            lblLane3TimeOut.Name = "lblLane3TimeOut";
            lblLane3TimeOut.Padding = new Padding(6, 0, 0, 0);
            lblLane3TimeOut.Size = new Size(222, 54);
            lblLane3TimeOut.TabIndex = 3;
            lblLane3TimeOut.Text = "Ngày ra: --";
            lblLane3TimeOut.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3PlateReg
            // 
            lblLane3PlateReg.BackColor = Color.White;
            lblLane3PlateReg.BorderStyle = BorderStyle.FixedSingle;
            lblLane3PlateReg.Dock = DockStyle.Fill;
            lblLane3PlateReg.Font = new Font("Arial", 9F);
            lblLane3PlateReg.ForeColor = Color.Black;
            lblLane3PlateReg.Location = new Point(3, 123);
            lblLane3PlateReg.Margin = new Padding(3, 3, 3, 3);
            lblLane3PlateReg.Name = "lblLane3PlateReg";
            lblLane3PlateReg.Padding = new Padding(6, 0, 0, 0);
            lblLane3PlateReg.Size = new Size(221, 54);
            lblLane3PlateReg.TabIndex = 4;
            lblLane3PlateReg.Text = "Biển số đăng ký: 29C-888.68";
            lblLane3PlateReg.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane3PlateDet
            // 
            lblLane3PlateDet.BackColor = Color.White;
            lblLane3PlateDet.BorderStyle = BorderStyle.FixedSingle;
            lblLane3PlateDet.Dock = DockStyle.Fill;
            lblLane3PlateDet.Font = new Font("Arial", 9F, FontStyle.Bold);
            lblLane3PlateDet.ForeColor = Color.DarkGreen;
            lblLane3PlateDet.Location = new Point(230, 123);
            lblLane3PlateDet.Margin = new Padding(3, 3, 3, 3);
            lblLane3PlateDet.Name = "lblLane3PlateDet";
            lblLane3PlateDet.Padding = new Padding(6, 0, 0, 0);
            lblLane3PlateDet.Size = new Size(222, 54);
            lblLane3PlateDet.TabIndex = 5;
            lblLane3PlateDet.Text = "Biển số nhận diện: 29C-888.68";
            lblLane3PlateDet.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbLane3PlateCrop
            // 
            pbLane3PlateCrop.BackColor = Color.White;
            pbLane3PlateCrop.BorderStyle = BorderStyle.FixedSingle;
            pbLane3PlateCrop.Dock = DockStyle.Fill;
            pbLane3PlateCrop.Location = new Point(3, 183);
            pbLane3PlateCrop.Name = "pbLane3PlateCrop";
            pbLane3PlateCrop.Size = new Size(221, 107);
            pbLane3PlateCrop.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3PlateCrop.TabIndex = 6;
            pbLane3PlateCrop.TabStop = false;
            // 
            // pbLane3DriverAvatar
            // 
            pbLane3DriverAvatar.BackColor = Color.White;
            pbLane3DriverAvatar.BorderStyle = BorderStyle.FixedSingle;
            pbLane3DriverAvatar.Dock = DockStyle.Fill;
            pbLane3DriverAvatar.Location = new Point(230, 183);
            pbLane3DriverAvatar.Name = "pbLane3DriverAvatar";
            pbLane3DriverAvatar.Size = new Size(222, 107);
            pbLane3DriverAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3DriverAvatar.TabIndex = 7;
            pbLane3DriverAvatar.TabStop = false;
            // 
            // btnLane3Test
            // 
            btnLane3Test.BackColor = Color.SeaGreen;
            tlpFieldsLane3.SetColumnSpan(btnLane3Test, 2);
            btnLane3Test.Dock = DockStyle.Fill;
            btnLane3Test.FlatStyle = FlatStyle.Flat;
            btnLane3Test.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnLane3Test.ForeColor = Color.White;
            btnLane3Test.Location = new Point(3, 296);
            btnLane3Test.Name = "btnLane3Test";
            btnLane3Test.Size = new Size(449, 36);
            btnLane3Test.TabIndex = 8;
            btnLane3Test.Text = "▶ QUẸT THẺ THỬ LÀN XE VÀO";
            btnLane3Test.UseVisualStyleBackColor = false;
            btnLane3Test.Click += BtnLane3Test_Click;
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
            tlpLane4.RowStyles.Add(new RowStyle(SizeType.Absolute, 113F));
            tlpLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane4.Size = new Size(473, 913);
            tlpLane4.TabIndex = 3;
            // 
            // tlpHeadLane4
            // 
            tlpHeadLane4.ColumnCount = 1;
            tlpHeadLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeadLane4.Controls.Add(lblTitleLane4, 0, 0);
            tlpHeadLane4.Controls.Add(lblSubLane4, 0, 1);
            tlpHeadLane4.Dock = DockStyle.Fill;
            tlpHeadLane4.Location = new Point(0, 0);
            tlpHeadLane4.Margin = new Padding(0);
            tlpHeadLane4.Name = "tlpHeadLane4";
            tlpHeadLane4.RowCount = 2;
            tlpHeadLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));
            tlpHeadLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            tlpHeadLane4.Size = new Size(473, 113);
            tlpHeadLane4.TabIndex = 0;
            // 
            // lblTitleLane4
            // 
            lblTitleLane4.BackColor = Color.SeaGreen;
            lblTitleLane4.Dock = DockStyle.Fill;
            lblTitleLane4.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTitleLane4.ForeColor = Color.White;
            lblTitleLane4.Location = new Point(0, 0);
            lblTitleLane4.Margin = new Padding(0);
            lblTitleLane4.Name = "lblTitleLane4";
            lblTitleLane4.Size = new Size(473, 61);
            lblTitleLane4.TabIndex = 0;
            lblTitleLane4.Text = "XE";
            lblTitleLane4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSubLane4
            // 
            lblSubLane4.BackColor = Color.DarkCyan;
            lblSubLane4.Dock = DockStyle.Fill;
            lblSubLane4.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblSubLane4.ForeColor = Color.White;
            lblSubLane4.Location = new Point(0, 61);
            lblSubLane4.Margin = new Padding(0);
            lblSubLane4.Name = "lblSubLane4";
            lblSubLane4.Size = new Size(473, 52);
            lblSubLane4.TabIndex = 1;
            lblSubLane4.Text = "LÀN XE RA (RA - 2 CAM)";
            lblSubLane4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane4
            // 
            tlpCamsLane4.BackColor = Color.Black;
            tlpCamsLane4.ColumnCount = 1;
            tlpCamsLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpCamsLane4.Controls.Add(pbLane4Overview, 0, 0);
            tlpCamsLane4.Controls.Add(pbLane4Plate, 0, 1);
            tlpCamsLane4.Dock = DockStyle.Fill;
            tlpCamsLane4.Location = new Point(0, 118);
            tlpCamsLane4.Margin = new Padding(0, 5, 0, 5);
            tlpCamsLane4.Name = "tlpCamsLane4";
            tlpCamsLane4.RowCount = 2;
            tlpCamsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane4.Size = new Size(473, 430);
            tlpCamsLane4.TabIndex = 1;
            // 
            // pbLane4Overview
            // 
            pbLane4Overview.BackColor = Color.FromArgb(11, 15, 20);
            pbLane4Overview.BorderStyle = BorderStyle.FixedSingle;
            pbLane4Overview.Dock = DockStyle.Fill;
            pbLane4Overview.Location = new Point(3, 3);
            pbLane4Overview.Name = "pbLane4Overview";
            pbLane4Overview.Size = new Size(467, 209);
            pbLane4Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4Overview.TabIndex = 0;
            pbLane4Overview.TabStop = false;
            // 
            // pbLane4Plate
            // 
            pbLane4Plate.BackColor = Color.FromArgb(11, 15, 20);
            pbLane4Plate.BorderStyle = BorderStyle.FixedSingle;
            pbLane4Plate.Dock = DockStyle.Fill;
            pbLane4Plate.Location = new Point(3, 218);
            pbLane4Plate.Name = "pbLane4Plate";
            pbLane4Plate.Size = new Size(467, 209);
            pbLane4Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4Plate.TabIndex = 1;
            pbLane4Plate.TabStop = false;
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
            pnlInfoLane4.Size = new Size(473, 355);
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
            tlpFieldsLane4.Controls.Add(btnLane4Test, 0, 4);
            tlpFieldsLane4.Dock = DockStyle.Fill;
            tlpFieldsLane4.Location = new Point(9, 10);
            tlpFieldsLane4.Margin = new Padding(0);
            tlpFieldsLane4.Name = "tlpFieldsLane4";
            tlpFieldsLane4.RowCount = 5;
            tlpFieldsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tlpFieldsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            tlpFieldsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            tlpFieldsLane4.Size = new Size(455, 335);
            tlpFieldsLane4.TabIndex = 0;
            // 
            // lblLane4Driver
            // 
            lblLane4Driver.BackColor = Color.White;
            lblLane4Driver.BorderStyle = BorderStyle.FixedSingle;
            lblLane4Driver.Dock = DockStyle.Fill;
            lblLane4Driver.Font = new Font("Arial", 9F);
            lblLane4Driver.ForeColor = Color.Black;
            lblLane4Driver.Location = new Point(3, 3);
            lblLane4Driver.Margin = new Padding(3, 3, 3, 3);
            lblLane4Driver.Name = "lblLane4Driver";
            lblLane4Driver.Padding = new Padding(6, 0, 0, 0);
            lblLane4Driver.Size = new Size(221, 54);
            lblLane4Driver.TabIndex = 0;
            lblLane4Driver.Text = "Tài xế/Khách: Vũ Đức Thịnh";
            lblLane4Driver.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4Dept
            // 
            lblLane4Dept.BackColor = Color.White;
            lblLane4Dept.BorderStyle = BorderStyle.FixedSingle;
            lblLane4Dept.Dock = DockStyle.Fill;
            lblLane4Dept.Font = new Font("Arial", 9F);
            lblLane4Dept.ForeColor = Color.Black;
            lblLane4Dept.Location = new Point(230, 3);
            lblLane4Dept.Margin = new Padding(3, 3, 3, 3);
            lblLane4Dept.Name = "lblLane4Dept";
            lblLane4Dept.Padding = new Padding(6, 0, 0, 0);
            lblLane4Dept.Size = new Size(222, 54);
            lblLane4Dept.TabIndex = 1;
            lblLane4Dept.Text = "Đơn vị: Đối Tác Hải Phòng";
            lblLane4Dept.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4TimeIn
            // 
            lblLane4TimeIn.BackColor = Color.White;
            lblLane4TimeIn.BorderStyle = BorderStyle.FixedSingle;
            lblLane4TimeIn.Dock = DockStyle.Fill;
            lblLane4TimeIn.Font = new Font("Arial", 9F);
            lblLane4TimeIn.ForeColor = Color.Black;
            lblLane4TimeIn.Location = new Point(3, 63);
            lblLane4TimeIn.Margin = new Padding(3, 3, 3, 3);
            lblLane4TimeIn.Name = "lblLane4TimeIn";
            lblLane4TimeIn.Padding = new Padding(6, 0, 0, 0);
            lblLane4TimeIn.Size = new Size(221, 54);
            lblLane4TimeIn.TabIndex = 2;
            lblLane4TimeIn.Text = "Ngày vào: 09:05:40 01/10/2026";
            lblLane4TimeIn.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4TimeOut
            // 
            lblLane4TimeOut.BackColor = Color.White;
            lblLane4TimeOut.BorderStyle = BorderStyle.FixedSingle;
            lblLane4TimeOut.Dock = DockStyle.Fill;
            lblLane4TimeOut.Font = new Font("Arial", 9F);
            lblLane4TimeOut.ForeColor = Color.Black;
            lblLane4TimeOut.Location = new Point(230, 63);
            lblLane4TimeOut.Margin = new Padding(3, 3, 3, 3);
            lblLane4TimeOut.Name = "lblLane4TimeOut";
            lblLane4TimeOut.Padding = new Padding(6, 0, 0, 0);
            lblLane4TimeOut.Size = new Size(222, 54);
            lblLane4TimeOut.TabIndex = 3;
            lblLane4TimeOut.Text = "Ngày ra: 10:42:19 01/10/2026";
            lblLane4TimeOut.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4PlateReg
            // 
            lblLane4PlateReg.BackColor = Color.White;
            lblLane4PlateReg.BorderStyle = BorderStyle.FixedSingle;
            lblLane4PlateReg.Dock = DockStyle.Fill;
            lblLane4PlateReg.Font = new Font("Arial", 9F);
            lblLane4PlateReg.ForeColor = Color.Black;
            lblLane4PlateReg.Location = new Point(3, 123);
            lblLane4PlateReg.Margin = new Padding(3, 3, 3, 3);
            lblLane4PlateReg.Name = "lblLane4PlateReg";
            lblLane4PlateReg.Padding = new Padding(6, 0, 0, 0);
            lblLane4PlateReg.Size = new Size(221, 54);
            lblLane4PlateReg.TabIndex = 4;
            lblLane4PlateReg.Text = "Biển số đăng ký: 15A-678.90";
            lblLane4PlateReg.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblLane4PlateDet
            // 
            lblLane4PlateDet.BackColor = Color.White;
            lblLane4PlateDet.BorderStyle = BorderStyle.FixedSingle;
            lblLane4PlateDet.Dock = DockStyle.Fill;
            lblLane4PlateDet.Font = new Font("Arial", 9F, FontStyle.Bold);
            lblLane4PlateDet.ForeColor = Color.DarkGreen;
            lblLane4PlateDet.Location = new Point(230, 123);
            lblLane4PlateDet.Margin = new Padding(3, 3, 3, 3);
            lblLane4PlateDet.Name = "lblLane4PlateDet";
            lblLane4PlateDet.Padding = new Padding(6, 0, 0, 0);
            lblLane4PlateDet.Size = new Size(222, 54);
            lblLane4PlateDet.TabIndex = 5;
            lblLane4PlateDet.Text = "Biển số nhận diện: 15A-678.90";
            lblLane4PlateDet.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbLane4PlateCrop
            // 
            pbLane4PlateCrop.BackColor = Color.White;
            pbLane4PlateCrop.BorderStyle = BorderStyle.FixedSingle;
            pbLane4PlateCrop.Dock = DockStyle.Fill;
            pbLane4PlateCrop.Location = new Point(3, 183);
            pbLane4PlateCrop.Name = "pbLane4PlateCrop";
            pbLane4PlateCrop.Size = new Size(221, 107);
            pbLane4PlateCrop.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4PlateCrop.TabIndex = 6;
            pbLane4PlateCrop.TabStop = false;
            // 
            // pbLane4EntrySnap
            // 
            pbLane4EntrySnap.BackColor = Color.White;
            pbLane4EntrySnap.BorderStyle = BorderStyle.FixedSingle;
            pbLane4EntrySnap.Dock = DockStyle.Fill;
            pbLane4EntrySnap.Location = new Point(230, 183);
            pbLane4EntrySnap.Name = "pbLane4EntrySnap";
            pbLane4EntrySnap.Size = new Size(222, 107);
            pbLane4EntrySnap.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4EntrySnap.TabIndex = 7;
            pbLane4EntrySnap.TabStop = false;
            // 
            // btnLane4Test
            // 
            btnLane4Test.BackColor = Color.SeaGreen;
            tlpFieldsLane4.SetColumnSpan(btnLane4Test, 2);
            btnLane4Test.Dock = DockStyle.Fill;
            btnLane4Test.FlatStyle = FlatStyle.Flat;
            btnLane4Test.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnLane4Test.ForeColor = Color.White;
            btnLane4Test.Location = new Point(3, 296);
            btnLane4Test.Name = "btnLane4Test";
            btnLane4Test.Size = new Size(449, 36);
            btnLane4Test.TabIndex = 8;
            btnLane4Test.Text = "▶ QUẸT THẺ THỬ LÀN XE RA";
            btnLane4Test.UseVisualStyleBackColor = false;
            btnLane4Test.Click += BtnLane4Test_Click;
            // 
            // pnlFooter
            // 
            pnlFooter.BackColor = Color.FromArgb(30, 41, 59);
            pnlFooter.Controls.Add(lbServer);
            pnlFooter.Controls.Add(lbStatusCtrl);
            pnlFooter.Controls.Add(lbdayExpiryDate);
            pnlFooter.Controls.Add(btnSimulateAll);
            pnlFooter.Controls.Add(lbRealTime);
            pnlFooter.Dock = DockStyle.Fill;
            pnlFooter.Location = new Point(0, 1000);
            pnlFooter.Margin = new Padding(0);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Size = new Size(1924, 50);
            pnlFooter.TabIndex = 2;
            // 
            // lbServer
            // 
            lbServer.BackColor = Color.Teal;
            lbServer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbServer.ForeColor = Color.White;
            lbServer.Location = new Point(17, 10);
            lbServer.Margin = new Padding(4, 0, 4, 0);
            lbServer.Name = "lbServer";
            lbServer.Size = new Size(229, 30);
            lbServer.TabIndex = 0;
            lbServer.Text = "MÁY CHỦ: ONLINE";
            lbServer.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lbStatusCtrl
            // 
            lbStatusCtrl.BackColor = Color.SeaGreen;
            lbStatusCtrl.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbStatusCtrl.ForeColor = Color.White;
            lbStatusCtrl.Location = new Point(257, 10);
            lbStatusCtrl.Margin = new Padding(4, 0, 4, 0);
            lbStatusCtrl.Name = "lbStatusCtrl";
            lbStatusCtrl.Size = new Size(286, 53);
            lbStatusCtrl.TabIndex = 1;
            lbStatusCtrl.Text = "BỘ ĐIỀU KHIỂN: ONLINE";
            lbStatusCtrl.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lbdayExpiryDate
            // 
            lbdayExpiryDate.BackColor = Color.DarkGoldenrod;
            lbdayExpiryDate.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbdayExpiryDate.ForeColor = Color.White;
            lbdayExpiryDate.Location = new Point(554, 10);
            lbdayExpiryDate.Margin = new Padding(4, 0, 4, 0);
            lbdayExpiryDate.Name = "lbdayExpiryDate";
            lbdayExpiryDate.Size = new Size(300, 53);
            lbdayExpiryDate.TabIndex = 2;
            lbdayExpiryDate.Text = "HẠN BẢN QUYỀN: 31/12/2026";
            lbdayExpiryDate.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnSimulateAll
            // 
            btnSimulateAll.BackColor = Color.FromArgb(14, 165, 233);
            btnSimulateAll.FlatStyle = FlatStyle.Flat;
            btnSimulateAll.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSimulateAll.ForeColor = Color.White;
            btnSimulateAll.Location = new Point(871, 10);
            btnSimulateAll.Margin = new Padding(0);
            btnSimulateAll.Name = "btnSimulateAll";
            btnSimulateAll.Size = new Size(371, 53);
            btnSimulateAll.TabIndex = 3;
            btnSimulateAll.Text = "⚡ GIẢ LẬP SỰ KIỆN TẤT CẢ LÀN";
            btnSimulateAll.UseVisualStyleBackColor = false;
            btnSimulateAll.Click += BtnSimulateAll_Click;
            // 
            // lbRealTime
            // 
            lbRealTime.Dock = DockStyle.Right;
            lbRealTime.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbRealTime.ForeColor = Color.White;
            lbRealTime.Location = new Point(1407, 0);
            lbRealTime.Margin = new Padding(4, 0, 4, 0);
            lbRealTime.Name = "lbRealTime";
            lbRealTime.Size = new Size(517, 50);
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
            FormClosing += FrmMain_FormClosing;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "HPPARKING - QUẢN LÝ VÀO RA BÃI XE THÔNG MINH";
            WindowState = FormWindowState.Maximized;
            Load += FrmMain_Load;
            tlpRoot.ResumeLayout(false);
            pnlTopDemo.ResumeLayout(false);
            pnlTopDemo.PerformLayout();
            tlpLanes.ResumeLayout(false);
            tlpLane1.ResumeLayout(false);
            tlpHeadLane1.ResumeLayout(false);
            tlpCamsLane1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane1Overview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1Face).EndInit();
            pnlInfoLane1.ResumeLayout(false);
            tlpFieldsLane1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane1Avatar).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane1FaceSnap).EndInit();
            tlpLane2.ResumeLayout(false);
            tlpHeadLane2.ResumeLayout(false);
            tlpCamsLane2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane2Overview).EndInit();
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
            pnlInfoLane4.ResumeLayout(false);
            tlpFieldsLane4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane4PlateCrop).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4EntrySnap).EndInit();
            pnlFooter.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tlpRoot;
        private Panel pnlTopDemo;
        private Label lblDemoTitle;
        private Button btnMode4Lanes;
        private Button btnMode2Vehicles;
        private Button btnMode2Pedestrians;
        private Button btnMode3Lanes;
        private Button btnMode1Lane;
        private Label lblModeNote;

        private TableLayoutPanel tlpLanes;
        private Panel pnlFooter;
        private Label lbServer;
        private Label lbStatusCtrl;
        private Label lbdayExpiryDate;
        private Label lbRealTime;
        private Button btnSimulateAll;

        // Lane 1 controls
        private TableLayoutPanel tlpLane1;
        private TableLayoutPanel tlpHeadLane1;
        private Label lblTitleLane1;
        private Label lblSubLane1;
        private TableLayoutPanel tlpCamsLane1;
        private PictureBox pbLane1Overview;
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
        private Button btnLane1Test;

        // Lane 2 controls
        private TableLayoutPanel tlpLane2;
        private TableLayoutPanel tlpHeadLane2;
        private Label lblTitleLane2;
        private Label lblSubLane2;
        private TableLayoutPanel tlpCamsLane2;
        private PictureBox pbLane2Overview;
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
        private Button btnLane2Test;

        // Lane 3 controls
        private TableLayoutPanel tlpLane3;
        private TableLayoutPanel tlpHeadLane3;
        private Label lblTitleLane3;
        private Label lblSubLane3;
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
        private Button btnLane3Test;

        // Lane 4 controls
        private TableLayoutPanel tlpLane4;
        private TableLayoutPanel tlpHeadLane4;
        private Label lblTitleLane4;
        private Label lblSubLane4;
        private TableLayoutPanel tlpCamsLane4;
        private PictureBox pbLane4Overview;
        private PictureBox pbLane4Plate;
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
        private Button btnLane4Test;
    }
}
