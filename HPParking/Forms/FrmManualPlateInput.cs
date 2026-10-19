using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using System;
using System.Linq;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmManualPlateInput : Form
    {
        private static FrmManualPlateInput? _activeInstance;
        private static readonly object _syncLock = new();

        private readonly string? _expectedPlate;
        private readonly string? _mismatchMessage;
        private System.Windows.Forms.Timer? _countdownTimer;
        private int _remainingSeconds = 10;

        public string? EnteredPlate { get; private set; }

        /// <summary>
        /// Đóng ngay lập tức form nhập tay đang mở (nếu có) khi có sự kiện mới.
        /// </summary>
        public static void DismissActive()
        {
            FrmManualPlateInput? active;
            lock (_syncLock)
            {
                active = _activeInstance;
                _activeInstance = null;
            }

            if (active != null && !active.IsDisposed)
            {
                try
                {
                    if (active.InvokeRequired)
                    {
                        active.BeginInvoke(new Action(() =>
                        {
                            active.EnteredPlate = null;
                            active.DialogResult = DialogResult.Cancel;
                            active.Close();
                        }));
                    }
                    else
                    {
                        active.EnteredPlate = null;
                        active.DialogResult = DialogResult.Cancel;
                        active.Close();
                    }
                }
                catch
                {
                    // Ignore if already closing/disposed
                }
            }
        }

        public FrmManualPlateInput()
        {
            InitializeComponent();
        }

        public FrmManualPlateInput(
            Lane lane,
            string reason = "Camera biển số lỗi hoặc không nhận diện được",
            string? expectedPlate = null,
            string? mismatchMessage = null,
            int timeoutSeconds = 10) : this()
        {
            _expectedPlate = expectedPlate;
            _mismatchMessage = mismatchMessage;
            _remainingSeconds = timeoutSeconds > 0 ? timeoutSeconds : 10;

            string laneDesc = $"{(lane.InputReader % 2 != 0 ? "Xe máy" : "Ô tô")} - Cổng {(lane.Direction == LaneDirection.In ? "VÀO" : "RA")} (Đầu đọc {lane.InputReader})";
            string registeredDesc = !string.IsNullOrWhiteSpace(expectedPlate)
                ? $"\nBiển số đăng ký: {expectedPlate}"
                : "";
            lblInfo.Text = $"Làn: {laneDesc}\nLý do: {reason}{registeredDesc}\nVui lòng nhập biển số xe thực tế:";
            lblCountdown.Text = $"⏱️ Tự động hủy sau: {_remainingSeconds}s";
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            string text = txtPlate.Text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(text))
            {
                ShowInlineError("Vui lòng nhập biển số hợp lệ!");
                txtPlate.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(_expectedPlate))
            {
                var expectedPlates = _expectedPlate
                    .Split([';', ',', '|'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToHashSet();

                string cleanEntered = text.Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
                if (expectedPlates.Count > 0 && !expectedPlates.Contains(cleanEntered))
                {
                    string msg = !string.IsNullOrEmpty(_mismatchMessage)
                        ? _mismatchMessage
                        : "Biển số xe không đúng với biển số đăng ký.";
                    ShowInlineError(msg);
                    txtPlate.Focus();
                    txtPlate.SelectAll();
                    return; // Không đóng form, cho phép người dùng nhập lại ngay lập tức
                }
            }

            StopCountdown();
            EnteredPlate = text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            StopCountdown();
            EnteredPlate = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ShowInlineError(string message)
        {
            lblError.Text = $"⚠️ {message}";
            lblError.Visible = true;
        }

        private void txtPlate_TextChanged(object? sender, EventArgs e)
        {
            if (lblError.Visible)
            {
                lblError.Visible = false;
            }
        }

        private void txtPlate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnConfirm.PerformClick();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                btnCancel.PerformClick();
            }
        }

        private void FrmManualPlateInput_Shown(object sender, EventArgs e)
        {
            lock (_syncLock)
            {
                _activeInstance = this;
            }

            txtPlate.Focus();

            if (_remainingSeconds > 0)
            {
                _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                _countdownTimer.Tick += (s, args) =>
                {
                    _remainingSeconds--;
                    if (_remainingSeconds <= 0)
                    {
                        StopCountdown();
                        EnteredPlate = null;
                        DialogResult = DialogResult.Cancel;
                        Close();
                    }
                    else
                    {
                        try
                        {
                            lblCountdown.Text = $"⏱️ Tự động hủy sau: {_remainingSeconds}s";
                        }
                        catch
                        {
                            // Ignore if form closing
                        }
                    }
                };
                _countdownTimer.Start();
            }
        }

        private void StopCountdown()
        {
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
            _countdownTimer = null;
        }

        private void FrmManualPlateInput_FormClosed(object? sender, FormClosedEventArgs e)
        {
            StopCountdown();
            lock (_syncLock)
            {
                if (_activeInstance == this)
                {
                    _activeInstance = null;
                }
            }
        }

        public static string? Prompt(
            IWin32Window? owner,
            Lane lane,
            string reason = "Camera lỗi hoặc không nhận diện được",
            string? expectedPlate = null,
            string? mismatchMessage = null,
            int timeoutSeconds = 10)
        {
            DismissActive();

            using var form = new FrmManualPlateInput(lane, reason, expectedPlate, mismatchMessage, timeoutSeconds);
            var result = form.ShowDialog(owner);
            return result == DialogResult.OK && !string.IsNullOrWhiteSpace(form.EnteredPlate) ? form.EnteredPlate : null;
        }
    }
}
