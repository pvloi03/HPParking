using Microsoft.Web.WebView2.WinForms;
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
            wvInfoLane1 = new WebView2();
            tlpLane2 = new TableLayoutPanel();
            tlpHeadLane2 = new TableLayoutPanel();
            lblTitleLane2 = new Label();
            tlpCamsLane2 = new TableLayoutPanel();
            pbLane2Overview = new PictureBox();
            pbLane2Plate = new PictureBox();
            pbLane2Face = new PictureBox();
            pnlInfoLane2 = new Panel();
            wvInfoLane2 = new WebView2();
            tlpLane3 = new TableLayoutPanel();
            tlpHeadLane3 = new TableLayoutPanel();
            lblTitleLane3 = new Label();
            tlpCamsLane3 = new TableLayoutPanel();
            pbLane3Overview = new PictureBox();
            pbLane3Plate = new PictureBox();
            pbLane3Face = new PictureBox();
            pnlInfoLane3 = new Panel();
            wvInfoLane3 = new WebView2();
            tlpLane4 = new TableLayoutPanel();
            tlpHeadLane4 = new TableLayoutPanel();
            lblTitleLane4 = new Label();
            tlpCamsLane4 = new TableLayoutPanel();
            pbLane4Overview = new PictureBox();
            pbLane4Plate = new PictureBox();
            pbLane4Face = new PictureBox();
            pnlInfoLane4 = new Panel();
            wvInfoLane4 = new WebView2();
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
            ((System.ComponentModel.ISupportInitialize)wvInfoLane1).BeginInit();
            tlpLane2.SuspendLayout();
            tlpHeadLane2.SuspendLayout();
            tlpCamsLane2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane2Overview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2Plate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2Face).BeginInit();
            pnlInfoLane2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)wvInfoLane2).BeginInit();
            tlpLane3.SuspendLayout();
            tlpHeadLane3.SuspendLayout();
            tlpCamsLane3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane3Overview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3Plate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3Face).BeginInit();
            pnlInfoLane3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)wvInfoLane3).BeginInit();
            tlpLane4.SuspendLayout();
            tlpHeadLane4.SuspendLayout();
            tlpCamsLane4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbLane4Overview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4Plate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4Face).BeginInit();
            pnlInfoLane4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)wvInfoLane4).BeginInit();
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
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
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
            pnlHeader.Size = new Size(1924, 1);
            pnlHeader.TabIndex = 0;
            pnlHeader.Visible = false;
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
            lblGateInfo.Size = new Size(1924, 1);
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
            tlpLanes.Location = new Point(0, 0);
            tlpLanes.Margin = new Padding(0);
            tlpLanes.Name = "tlpLanes";
            tlpLanes.RowCount = 1;
            tlpLanes.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpLanes.Size = new Size(1924, 1020);
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
            tlpLane1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tlpLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane1.Size = new Size(473, 1010);
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
            tlpHeadLane1.Size = new Size(473, 30);
            tlpHeadLane1.TabIndex = 0;
            // 
            // lblTitleLane1
            // 
            lblTitleLane1.BackColor = Color.SeaGreen;
            lblTitleLane1.Dock = DockStyle.Fill;
            lblTitleLane1.Font = new Font("Arial", 8F);
            lblTitleLane1.ForeColor = Color.White;
            lblTitleLane1.Location = new Point(0, 0);
            lblTitleLane1.Margin = new Padding(0);
            lblTitleLane1.Name = "lblTitleLane1";
            lblTitleLane1.Size = new Size(473, 30);
            lblTitleLane1.TabIndex = 0;
            lblTitleLane1.Text = "LÀN VÀO 01";
            lblTitleLane1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane1
            // 
            tlpCamsLane1.BackColor = Color.Transparent;
            tlpCamsLane1.ColumnCount = 2;
            tlpCamsLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane1.Controls.Add(pbLane1Overview, 0, 0);
            tlpCamsLane1.Controls.Add(pbLane1Plate, 0, 1);
            tlpCamsLane1.Controls.Add(pbLane1Face, 1, 1);
            tlpCamsLane1.Dock = DockStyle.Fill;
            tlpCamsLane1.Location = new Point(0, 32);
            tlpCamsLane1.Margin = new Padding(0, 2, 0, 2);
            tlpCamsLane1.Name = "tlpCamsLane1";
            tlpCamsLane1.RowCount = 2;
            tlpCamsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane1.Size = new Size(473, 535);
            tlpCamsLane1.TabIndex = 1;
            // 
            // pbLane1Overview
            // 
            pbLane1Overview.BackColor = Color.LightGray;
            tlpCamsLane1.SetColumnSpan(pbLane1Overview, 2);
            pbLane1Overview.Dock = DockStyle.Fill;
            pbLane1Overview.Location = new Point(0, 0);
            pbLane1Overview.Margin = new Padding(0);
            pbLane1Overview.Name = "pbLane1Overview";
            pbLane1Overview.Size = new Size(473, 267);
            pbLane1Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Overview.TabIndex = 0;
            pbLane1Overview.TabStop = false;
            // 
            // pbLane1Plate
            // 
            pbLane1Plate.BackColor = Color.LightGray;
            pbLane1Plate.Dock = DockStyle.Fill;
            pbLane1Plate.Location = new Point(0, 267);
            pbLane1Plate.Margin = new Padding(0);
            pbLane1Plate.Name = "pbLane1Plate";
            pbLane1Plate.Size = new Size(236, 268);
            pbLane1Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Plate.TabIndex = 1;
            pbLane1Plate.TabStop = false;
            // 
            // pbLane1Face
            // 
            pbLane1Face.BackColor = Color.LightGray;
            pbLane1Face.Dock = DockStyle.Fill;
            pbLane1Face.Location = new Point(236, 267);
            pbLane1Face.Margin = new Padding(0);
            pbLane1Face.Name = "pbLane1Face";
            pbLane1Face.Size = new Size(237, 268);
            pbLane1Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane1Face.TabIndex = 2;
            pbLane1Face.TabStop = false;
            // 
            // pnlInfoLane1
            // 
            pnlInfoLane1.BackColor = Color.FromArgb(241, 245, 249);
            pnlInfoLane1.Controls.Add(wvInfoLane1);
            pnlInfoLane1.Dock = DockStyle.Fill;
            pnlInfoLane1.Location = new Point(0, 569);
            pnlInfoLane1.Margin = new Padding(0);
            pnlInfoLane1.Name = "pnlInfoLane1";
            pnlInfoLane1.Size = new Size(473, 441);
            pnlInfoLane1.TabIndex = 2;
            // 
            // wvInfoLane1
            // 
            wvInfoLane1.AllowExternalDrop = false;
            wvInfoLane1.CreationProperties = null;
            wvInfoLane1.DefaultBackgroundColor = Color.FromArgb(241, 245, 249);
            wvInfoLane1.Dock = DockStyle.Fill;
            wvInfoLane1.Location = new Point(0, 0);
            wvInfoLane1.Margin = new Padding(0);
            wvInfoLane1.Name = "wvInfoLane1";
            wvInfoLane1.Size = new Size(473, 441);
            wvInfoLane1.TabIndex = 0;
            wvInfoLane1.ZoomFactor = 1D;
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
            tlpLane2.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tlpLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane2.Size = new Size(473, 1010);
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
            tlpHeadLane2.Size = new Size(473, 30);
            tlpHeadLane2.TabIndex = 0;
            // 
            // lblTitleLane2
            // 
            lblTitleLane2.BackColor = Color.SeaGreen;
            lblTitleLane2.Dock = DockStyle.Fill;
            lblTitleLane2.Font = new Font("Arial", 8F);
            lblTitleLane2.ForeColor = Color.White;
            lblTitleLane2.Location = new Point(0, 0);
            lblTitleLane2.Margin = new Padding(0);
            lblTitleLane2.Name = "lblTitleLane2";
            lblTitleLane2.Size = new Size(473, 30);
            lblTitleLane2.TabIndex = 0;
            lblTitleLane2.Text = "LÀN RA 02";
            lblTitleLane2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane2
            // 
            tlpCamsLane2.BackColor = Color.Transparent;
            tlpCamsLane2.ColumnCount = 2;
            tlpCamsLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane2.Controls.Add(pbLane2Overview, 0, 0);
            tlpCamsLane2.Controls.Add(pbLane2Plate, 0, 1);
            tlpCamsLane2.Controls.Add(pbLane2Face, 1, 1);
            tlpCamsLane2.Dock = DockStyle.Fill;
            tlpCamsLane2.Location = new Point(0, 32);
            tlpCamsLane2.Margin = new Padding(0, 2, 0, 2);
            tlpCamsLane2.Name = "tlpCamsLane2";
            tlpCamsLane2.RowCount = 2;
            tlpCamsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane2.Size = new Size(473, 535);
            tlpCamsLane2.TabIndex = 1;
            // 
            // pbLane2Overview
            // 
            pbLane2Overview.BackColor = Color.LightGray;
            tlpCamsLane2.SetColumnSpan(pbLane2Overview, 2);
            pbLane2Overview.Dock = DockStyle.Fill;
            pbLane2Overview.Location = new Point(0, 0);
            pbLane2Overview.Margin = new Padding(0);
            pbLane2Overview.Name = "pbLane2Overview";
            pbLane2Overview.Size = new Size(473, 267);
            pbLane2Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Overview.TabIndex = 0;
            pbLane2Overview.TabStop = false;
            // 
            // pbLane2Plate
            // 
            pbLane2Plate.BackColor = Color.LightGray;
            pbLane2Plate.Dock = DockStyle.Fill;
            pbLane2Plate.Location = new Point(0, 267);
            pbLane2Plate.Margin = new Padding(0);
            pbLane2Plate.Name = "pbLane2Plate";
            pbLane2Plate.Size = new Size(236, 268);
            pbLane2Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Plate.TabIndex = 1;
            pbLane2Plate.TabStop = false;
            // 
            // pbLane2Face
            // 
            pbLane2Face.BackColor = Color.LightGray;
            pbLane2Face.Dock = DockStyle.Fill;
            pbLane2Face.Location = new Point(236, 267);
            pbLane2Face.Margin = new Padding(0);
            pbLane2Face.Name = "pbLane2Face";
            pbLane2Face.Size = new Size(237, 268);
            pbLane2Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane2Face.TabIndex = 2;
            pbLane2Face.TabStop = false;
            // 
            // pnlInfoLane2
            // 
            pnlInfoLane2.BackColor = Color.FromArgb(241, 245, 249);
            pnlInfoLane2.Controls.Add(wvInfoLane2);
            pnlInfoLane2.Dock = DockStyle.Fill;
            pnlInfoLane2.Location = new Point(0, 569);
            pnlInfoLane2.Margin = new Padding(0);
            pnlInfoLane2.Name = "pnlInfoLane2";
            pnlInfoLane2.Size = new Size(473, 441);
            pnlInfoLane2.TabIndex = 2;
            // 
            // wvInfoLane2
            // 
            wvInfoLane2.AllowExternalDrop = false;
            wvInfoLane2.CreationProperties = null;
            wvInfoLane2.DefaultBackgroundColor = Color.FromArgb(241, 245, 249);
            wvInfoLane2.Dock = DockStyle.Fill;
            wvInfoLane2.Location = new Point(0, 0);
            wvInfoLane2.Margin = new Padding(0);
            wvInfoLane2.Name = "wvInfoLane2";
            wvInfoLane2.Size = new Size(473, 441);
            wvInfoLane2.TabIndex = 0;
            wvInfoLane2.ZoomFactor = 1D;
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
            tlpLane3.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tlpLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane3.Size = new Size(473, 1010);
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
            tlpHeadLane3.Size = new Size(473, 30);
            tlpHeadLane3.TabIndex = 0;
            // 
            // lblTitleLane3
            // 
            lblTitleLane3.BackColor = Color.SeaGreen;
            lblTitleLane3.Dock = DockStyle.Fill;
            lblTitleLane3.Font = new Font("Arial", 8F);
            lblTitleLane3.ForeColor = Color.White;
            lblTitleLane3.Location = new Point(0, 0);
            lblTitleLane3.Margin = new Padding(0);
            lblTitleLane3.Name = "lblTitleLane3";
            lblTitleLane3.Size = new Size(473, 30);
            lblTitleLane3.TabIndex = 0;
            lblTitleLane3.Text = "LÀN VÀO 03";
            lblTitleLane3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane3
            // 
            tlpCamsLane3.BackColor = Color.Transparent;
            tlpCamsLane3.ColumnCount = 2;
            tlpCamsLane3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane3.Controls.Add(pbLane3Overview, 0, 0);
            tlpCamsLane3.Controls.Add(pbLane3Plate, 0, 1);
            tlpCamsLane3.Controls.Add(pbLane3Face, 1, 1);
            tlpCamsLane3.Dock = DockStyle.Fill;
            tlpCamsLane3.Location = new Point(0, 32);
            tlpCamsLane3.Margin = new Padding(0, 2, 0, 2);
            tlpCamsLane3.Name = "tlpCamsLane3";
            tlpCamsLane3.RowCount = 2;
            tlpCamsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane3.Size = new Size(473, 535);
            tlpCamsLane3.TabIndex = 1;
            // 
            // pbLane3Overview
            // 
            pbLane3Overview.BackColor = Color.LightGray;
            tlpCamsLane3.SetColumnSpan(pbLane3Overview, 2);
            pbLane3Overview.Dock = DockStyle.Fill;
            pbLane3Overview.Location = new Point(0, 0);
            pbLane3Overview.Margin = new Padding(0);
            pbLane3Overview.Name = "pbLane3Overview";
            pbLane3Overview.Size = new Size(473, 267);
            pbLane3Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3Overview.TabIndex = 0;
            pbLane3Overview.TabStop = false;
            // 
            // pbLane3Plate
            // 
            pbLane3Plate.BackColor = Color.LightGray;
            pbLane3Plate.Dock = DockStyle.Fill;
            pbLane3Plate.Location = new Point(0, 267);
            pbLane3Plate.Margin = new Padding(0);
            pbLane3Plate.Name = "pbLane3Plate";
            pbLane3Plate.Size = new Size(236, 268);
            pbLane3Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3Plate.TabIndex = 1;
            pbLane3Plate.TabStop = false;
            // 
            // pbLane3Face
            // 
            pbLane3Face.BackColor = Color.LightGray;
            pbLane3Face.Dock = DockStyle.Fill;
            pbLane3Face.Location = new Point(236, 267);
            pbLane3Face.Margin = new Padding(0);
            pbLane3Face.Name = "pbLane3Face";
            pbLane3Face.Size = new Size(237, 268);
            pbLane3Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane3Face.TabIndex = 2;
            pbLane3Face.TabStop = false;
            // 
            // pnlInfoLane3
            // 
            pnlInfoLane3.BackColor = Color.FromArgb(241, 245, 249);
            pnlInfoLane3.Controls.Add(wvInfoLane3);
            pnlInfoLane3.Dock = DockStyle.Fill;
            pnlInfoLane3.Location = new Point(0, 569);
            pnlInfoLane3.Margin = new Padding(0);
            pnlInfoLane3.Name = "pnlInfoLane3";
            pnlInfoLane3.Size = new Size(473, 441);
            pnlInfoLane3.TabIndex = 2;
            // 
            // wvInfoLane3
            // 
            wvInfoLane3.AllowExternalDrop = false;
            wvInfoLane3.CreationProperties = null;
            wvInfoLane3.DefaultBackgroundColor = Color.FromArgb(241, 245, 249);
            wvInfoLane3.Dock = DockStyle.Fill;
            wvInfoLane3.Location = new Point(0, 0);
            wvInfoLane3.Margin = new Padding(0);
            wvInfoLane3.Name = "wvInfoLane3";
            wvInfoLane3.Size = new Size(473, 441);
            wvInfoLane3.TabIndex = 0;
            wvInfoLane3.ZoomFactor = 1D;
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
            tlpLane4.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tlpLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpLane4.Size = new Size(473, 1010);
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
            tlpHeadLane4.Size = new Size(473, 30);
            tlpHeadLane4.TabIndex = 0;
            // 
            // lblTitleLane4
            // 
            lblTitleLane4.BackColor = Color.SeaGreen;
            lblTitleLane4.Dock = DockStyle.Fill;
            lblTitleLane4.Font = new Font("Arial", 8F);
            lblTitleLane4.ForeColor = Color.White;
            lblTitleLane4.Location = new Point(0, 0);
            lblTitleLane4.Margin = new Padding(0);
            lblTitleLane4.Name = "lblTitleLane4";
            lblTitleLane4.Size = new Size(473, 30);
            lblTitleLane4.TabIndex = 0;
            lblTitleLane4.Text = "LÀN RA 04";
            lblTitleLane4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCamsLane4
            // 
            tlpCamsLane4.BackColor = Color.Transparent;
            tlpCamsLane4.ColumnCount = 2;
            tlpCamsLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCamsLane4.Controls.Add(pbLane4Overview, 0, 0);
            tlpCamsLane4.Controls.Add(pbLane4Plate, 0, 1);
            tlpCamsLane4.Controls.Add(pbLane4Face, 1, 1);
            tlpCamsLane4.Dock = DockStyle.Fill;
            tlpCamsLane4.Location = new Point(0, 32);
            tlpCamsLane4.Margin = new Padding(0, 2, 0, 2);
            tlpCamsLane4.Name = "tlpCamsLane4";
            tlpCamsLane4.RowCount = 2;
            tlpCamsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCamsLane4.Size = new Size(473, 535);
            tlpCamsLane4.TabIndex = 1;
            // 
            // pbLane4Overview
            // 
            pbLane4Overview.BackColor = Color.LightGray;
            tlpCamsLane4.SetColumnSpan(pbLane4Overview, 2);
            pbLane4Overview.Dock = DockStyle.Fill;
            pbLane4Overview.Location = new Point(0, 0);
            pbLane4Overview.Margin = new Padding(0);
            pbLane4Overview.Name = "pbLane4Overview";
            pbLane4Overview.Size = new Size(473, 267);
            pbLane4Overview.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4Overview.TabIndex = 0;
            pbLane4Overview.TabStop = false;
            // 
            // pbLane4Plate
            // 
            pbLane4Plate.BackColor = Color.LightGray;
            pbLane4Plate.Dock = DockStyle.Fill;
            pbLane4Plate.Location = new Point(0, 267);
            pbLane4Plate.Margin = new Padding(0);
            pbLane4Plate.Name = "pbLane4Plate";
            pbLane4Plate.Size = new Size(236, 268);
            pbLane4Plate.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4Plate.TabIndex = 1;
            pbLane4Plate.TabStop = false;
            // 
            // pbLane4Face
            // 
            pbLane4Face.BackColor = Color.LightGray;
            pbLane4Face.Dock = DockStyle.Fill;
            pbLane4Face.Location = new Point(236, 267);
            pbLane4Face.Margin = new Padding(0);
            pbLane4Face.Name = "pbLane4Face";
            pbLane4Face.Size = new Size(237, 268);
            pbLane4Face.SizeMode = PictureBoxSizeMode.Zoom;
            pbLane4Face.TabIndex = 2;
            pbLane4Face.TabStop = false;
            // 
            // pnlInfoLane4
            // 
            pnlInfoLane4.BackColor = Color.FromArgb(241, 245, 249);
            pnlInfoLane4.Controls.Add(wvInfoLane4);
            pnlInfoLane4.Dock = DockStyle.Fill;
            pnlInfoLane4.Location = new Point(0, 569);
            pnlInfoLane4.Margin = new Padding(0);
            pnlInfoLane4.Name = "pnlInfoLane4";
            pnlInfoLane4.Size = new Size(473, 441);
            pnlInfoLane4.TabIndex = 2;
            // 
            // wvInfoLane4
            // 
            wvInfoLane4.AllowExternalDrop = false;
            wvInfoLane4.CreationProperties = null;
            wvInfoLane4.DefaultBackgroundColor = Color.FromArgb(241, 245, 249);
            wvInfoLane4.Dock = DockStyle.Fill;
            wvInfoLane4.Location = new Point(0, 0);
            wvInfoLane4.Margin = new Padding(0);
            wvInfoLane4.Name = "wvInfoLane4";
            wvInfoLane4.Size = new Size(473, 441);
            wvInfoLane4.TabIndex = 0;
            wvInfoLane4.ZoomFactor = 1D;
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
            ((System.ComponentModel.ISupportInitialize)wvInfoLane1).EndInit();
            tlpLane2.ResumeLayout(false);
            tlpHeadLane2.ResumeLayout(false);
            tlpCamsLane2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane2Overview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2Plate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane2Face).EndInit();
            pnlInfoLane2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)wvInfoLane2).EndInit();
            tlpLane3.ResumeLayout(false);
            tlpHeadLane3.ResumeLayout(false);
            tlpCamsLane3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane3Overview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3Plate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane3Face).EndInit();
            pnlInfoLane3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)wvInfoLane3).EndInit();
            tlpLane4.ResumeLayout(false);
            tlpHeadLane4.ResumeLayout(false);
            tlpCamsLane4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbLane4Overview).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4Plate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLane4Face).EndInit();
            pnlInfoLane4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)wvInfoLane4).EndInit();
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
        private WebView2 wvInfoLane1;

        // Lane 2 controls
        private TableLayoutPanel tlpLane2;
        private TableLayoutPanel tlpHeadLane2;
        private Label lblTitleLane2;
        private TableLayoutPanel tlpCamsLane2;
        private PictureBox pbLane2Overview;
        private PictureBox pbLane2Plate;
        private PictureBox pbLane2Face;
        private Panel pnlInfoLane2;
        private WebView2 wvInfoLane2;

        // Lane 3 controls
        private TableLayoutPanel tlpLane3;
        private TableLayoutPanel tlpHeadLane3;
        private Label lblTitleLane3;
        private TableLayoutPanel tlpCamsLane3;
        private PictureBox pbLane3Overview;
        private PictureBox pbLane3Plate;
        private PictureBox pbLane3Face;
        private Panel pnlInfoLane3;
        private WebView2 wvInfoLane3;

        // Lane 4 controls
        private TableLayoutPanel tlpLane4;
        private TableLayoutPanel tlpHeadLane4;
        private Label lblTitleLane4;
        private TableLayoutPanel tlpCamsLane4;
        private PictureBox pbLane4Overview;
        private PictureBox pbLane4Plate;
        private PictureBox pbLane4Face;
        private Panel pnlInfoLane4;
        private WebView2 wvInfoLane4;
        private Button btnReloadHardware;
    }
}
