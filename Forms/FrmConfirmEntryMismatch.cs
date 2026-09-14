using System;
using System.Drawing;
using System.Windows.Forms;
using HPParking.Models.Entities;

namespace HPParking.Forms
{
    public class FrmConfirmEntryMismatch : Form
    {
        public bool IsApproved { get; private set; }

        public FrmConfirmEntryMismatch(Lane lane, Client client, string detectedPlate, string departmentName = "")
        {
            Text = "Cảnh Báo Biển Số Không Khớp";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 260);
            BackColor = Color.FromArgb(250, 250, 252);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            string laneDesc = $"{(lane.InputReader % 2 != 0 ? "Xe máy" : "Ô tô")} - Cổng VÀO (Đầu đọc {lane.InputReader})";

            var lblHeader = new Label
            {
                Text = "CẢNH BÁO: BIỂN SỐ VÀO KHÁC BIỂN SỐ ĐĂNG KÝ!",
                Location = new Point(15, 12),
                Size = new Size(430, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.DarkOrange,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var pnlDetails = new Panel
            {
                Location = new Point(20, 45),
                Size = new Size(420, 140),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            string deptDisplay = !string.IsNullOrWhiteSpace(departmentName) ? departmentName : client.Department_Code;
            var lblClientName = new Label
            {
                Text = $"Khách hàng: {client.Name} (Phòng ban: {deptDisplay})",
                Location = new Point(15, 12),
                Size = new Size(390, 22),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point)
            };

            var lblCardCode = new Label
            {
                Text = $"Số CCCD: {client.ID_Code}",
                Location = new Point(15, 38),
                Size = new Size(390, 22)
            };

            var lblRegistered = new Label
            {
                Text = $"Biển số đăng ký:   {client.LicensePlate}",
                Location = new Point(15, 68),
                Size = new Size(390, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.DarkSlateGray
            };

            var lblDetected = new Label
            {
                Text = $"Biển số thực tế vào: {detectedPlate}",
                Location = new Point(15, 98),
                Size = new Size(390, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.Crimson
            };

            pnlDetails.Controls.Add(lblClientName);
            pnlDetails.Controls.Add(lblCardCode);
            pnlDetails.Controls.Add(lblRegistered);
            pnlDetails.Controls.Add(lblDetected);

            var btnApprove = new Button
            {
                Text = "Cho phép vào (Enter)",
                DialogResult = DialogResult.Yes,
                Location = new Point(60, 200),
                Size = new Size(180, 42),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                Cursor = Cursors.Hand
            };
            btnApprove.FlatAppearance.BorderSize = 0;
            btnApprove.Click += (s, e) =>
            {
                IsApproved = true;
                Close();
            };

            var btnReject = new Button
            {
                Text = "Từ chối vào (Esc)",
                DialogResult = DialogResult.No,
                Location = new Point(250, 200),
                Size = new Size(150, 42),
                BackColor = Color.Crimson,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                Cursor = Cursors.Hand
            };
            btnReject.FlatAppearance.BorderSize = 0;
            btnReject.Click += (s, e) =>
            {
                IsApproved = false;
                Close();
            };

            Controls.Add(lblHeader);
            Controls.Add(pnlDetails);
            Controls.Add(btnApprove);
            Controls.Add(btnReject);

            AcceptButton = btnApprove;
            CancelButton = btnReject;
        }

        public static bool Prompt(IWin32Window? owner, Lane lane, Client client, string detectedPlate, string departmentName = "")
        {
            using var form = new FrmConfirmEntryMismatch(lane, client, detectedPlate, departmentName);
            var result = form.ShowDialog(owner);
            return result == DialogResult.Yes && form.IsApproved;
        }
    }
}
