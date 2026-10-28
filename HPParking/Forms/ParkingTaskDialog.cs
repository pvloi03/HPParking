using System;
using System.Windows.Forms;

namespace HPParking.Forms
{
    /// <summary>
    /// Tiện ích TaskDialog chuẩn hóa dùng chung cho toàn bộ ứng dụng HPParking:
    /// - Hiển thị giao diện hiện đại Windows (Heading, Text, Expander, Footnote).
    /// - Hỗ trợ đếm ngược tự đóng (Auto-dismiss countdown) để không phong tỏa màn hình khi xe lỗi rời đi.
    /// - Hỗ trợ đóng ngay lập tức (DismissActive) khi có lượt quẹt thẻ hoặc sự kiện mới ở bất kỳ làn nào.
    /// </summary>
    public static class ParkingTaskDialog
    {
        private static Action? _activeDismissAction;
        private static readonly object _syncLock = new();

        /// <summary>
        /// Đóng ngay lập tức TaskDialog đang hiển thị (nếu có), giải phóng màn hình cho lượt xử lý tiếp theo.
        /// </summary>
        public static void DismissActive()
        {
            Action? dismiss;
            lock (_syncLock)
            {
                dismiss = _activeDismissAction;
                _activeDismissAction = null;
            }

            dismiss?.Invoke();
        }

        /// <summary>
        /// Hiển thị cảnh báo LỖI (Error) với đếm ngược tự đóng.
        /// </summary>
        public static void ShowError(
            IWin32Window? owner,
            string heading,
            string text,
            string? details = null,
            int timeoutSeconds = 5,
            string caption = "HPParking - Cảnh Báo Lỗi")
        {
            ShowDialog(owner, heading, text, TaskDialogIcon.Error, details, timeoutSeconds, caption);
        }

        /// <summary>
        /// Hiển thị CẢNH BÁO (Warning) với đếm ngược tự đóng.
        /// </summary>
        public static void ShowWarning(
            IWin32Window? owner,
            string heading,
            string text,
            string? details = null,
            int timeoutSeconds = 5,
            string caption = "HPParking - Cảnh Báo")
        {
            ShowDialog(owner, heading, text, TaskDialogIcon.Warning, details, timeoutSeconds, caption);
        }

        /// <summary>
        /// Hiển thị THÔNG BÁO (Information) với đếm ngược tự đóng.
        /// </summary>
        public static void ShowInfo(
            IWin32Window? owner,
            string heading,
            string text,
            string? details = null,
            int timeoutSeconds = 5,
            string caption = "HPParking - Thông Báo")
        {
            ShowDialog(owner, heading, text, TaskDialogIcon.Information, details, timeoutSeconds, caption);
        }

        /// <summary>
        /// Hiển thị hộp thoại XÁC NHẬN (Yes / No). Mặc định không có timeout trừ khi được chỉ định.
        /// </summary>
        public static bool ShowConfirmation(
            IWin32Window? owner,
            string heading,
            string text,
            string? details = null,
            int timeoutSeconds = 0,
            string caption = "HPParking - Xác Nhận")
        {
            return ShowConfirmDialog(owner, heading, text, details, timeoutSeconds, caption);
        }

        private static void ShowDialog(
            IWin32Window? owner,
            string heading,
            string text,
            TaskDialogIcon icon,
            string? details,
            int timeoutSeconds,
            string caption)
        {
            // Đóng dialog cũ trước khi mở dialog mới
            DismissActive();

            var closeButton = new TaskDialogButton("Đóng (Enter / Esc)");

            var page = new TaskDialogPage
            {
                Caption = caption,
                Heading = heading,
                Text = text,
                Icon = icon,
                DefaultButton = closeButton
            };
            page.Buttons.Add(closeButton);

            if (!string.IsNullOrWhiteSpace(details))
            {
                page.Expander = new TaskDialogExpander(details)
                {
                    Text = details,
                    CollapsedButtonText = "Chi tiết kỹ thuật ▾",
                    ExpandedButtonText = "Thu gọn chi tiết ▴"
                };
            }

            System.Windows.Forms.Timer? timer = null;
            int remainingSeconds = timeoutSeconds;

            if (timeoutSeconds > 0)
            {
                page.Footnote = new TaskDialogFootnote($"⏱️ Tự động đóng sau {remainingSeconds} giây...");
            }

            page.Created += (s, e) =>
            {
                lock (_syncLock)
                {
                    _activeDismissAction = () =>
                    {
                        try
                        {
                            if (owner is Control ctrl && ctrl.InvokeRequired)
                            {
                                ctrl.BeginInvoke(new Action(() => closeButton.PerformClick()));
                            }
                            else
                            {
                                closeButton.PerformClick();
                            }
                        }
                        catch
                        {
                            // Ignore if already disposed/closed
                        }
                    };
                }

                if (timeoutSeconds > 0)
                {
                    timer = new System.Windows.Forms.Timer { Interval = 1000 };
                    timer.Tick += (ts, te) =>
                    {
                        remainingSeconds--;
                        if (remainingSeconds <= 0)
                        {
                            timer.Stop();
                            try
                            {
                                closeButton.PerformClick();
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                        else
                        {
                            try
                            {
                                if (page.Footnote != null)
                                {
                                    page.Footnote.Text = $"⏱️ Tự động đóng sau {remainingSeconds} giây...";
                                }
                            }
                            catch
                            {
                                // Ignore if already closed
                            }
                        }
                    };
                    timer.Start();
                }
            };

            page.Destroyed += (s, e) =>
            {
                timer?.Stop();
                timer?.Dispose();
                timer = null;

                lock (_syncLock)
                {
                    _activeDismissAction = null;
                }
            };

            try
            {
                if (owner != null)
                {
                    TaskDialog.ShowDialog(owner, page);
                }
                else
                {
                    TaskDialog.ShowDialog(page);
                }
            }
            finally
            {
                timer?.Stop();
                timer?.Dispose();
                lock (_syncLock)
                {
                    _activeDismissAction = null;
                }
            }
        }

        private static bool ShowConfirmDialog(
            IWin32Window? owner,
            string heading,
            string text,
            string? details,
            int timeoutSeconds,
            string caption)
        {
            DismissActive();

            var btnYes = new TaskDialogButton("Đồng ý");
            var btnNo = new TaskDialogButton("Hủy bỏ");

            var page = new TaskDialogPage
            {
                Caption = caption,
                Heading = heading,
                Text = text,
                Icon = TaskDialogIcon.Warning,
                DefaultButton = btnYes
            };
            page.Buttons.Add(btnYes);
            page.Buttons.Add(btnNo);

            if (!string.IsNullOrWhiteSpace(details))
            {
                page.Expander = new TaskDialogExpander(details)
                {
                    Text = details,
                    CollapsedButtonText = "Chi tiết kỹ thuật ▾",
                    ExpandedButtonText = "Thu gọn chi tiết ▴"
                };
            }

            System.Windows.Forms.Timer? timer = null;
            int remainingSeconds = timeoutSeconds;

            if (timeoutSeconds > 0)
            {
                page.Footnote = new TaskDialogFootnote($"⏱️ Tự động hủy sau {remainingSeconds} giây...");
            }

            page.Created += (s, e) =>
            {
                lock (_syncLock)
                {
                    _activeDismissAction = () =>
                    {
                        try
                        {
                            if (owner is Control ctrl && ctrl.InvokeRequired)
                            {
                                ctrl.BeginInvoke(new Action(() => btnNo.PerformClick()));
                            }
                            else
                            {
                                btnNo.PerformClick();
                            }
                        }
                        catch
                        {
                            // Ignore
                        }
                    };
                }

                if (timeoutSeconds > 0)
                {
                    timer = new System.Windows.Forms.Timer { Interval = 1000 };
                    timer.Tick += (ts, te) =>
                    {
                        remainingSeconds--;
                        if (remainingSeconds <= 0)
                        {
                            timer.Stop();
                            try
                            {
                                btnNo.PerformClick();
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                        else
                        {
                            try
                            {
                                if (page.Footnote != null)
                                {
                                    page.Footnote.Text = $"⏱️ Tự động hủy sau {remainingSeconds} giây...";
                                }
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    };
                    timer.Start();
                }
            };

            page.Destroyed += (s, e) =>
            {
                timer?.Stop();
                timer?.Dispose();
                timer = null;

                lock (_syncLock)
                {
                    _activeDismissAction = null;
                }
            };

            try
            {
                var result = owner != null
                    ? TaskDialog.ShowDialog(owner, page)
                    : TaskDialog.ShowDialog(page);
                return result == btnYes;
            }
            finally
            {
                timer?.Stop();
                timer?.Dispose();
                lock (_syncLock)
                {
                    _activeDismissAction = null;
                }
            }
        }
    }
}
