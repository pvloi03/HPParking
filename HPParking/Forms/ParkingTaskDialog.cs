using System;
using System.Windows.Forms;

namespace HPParking.Forms
{
    /// <summary>
    /// Tiện ích TaskDialog chuẩn hóa dùng chung cho toàn bộ ứng dụng HPParking:
    /// - Hiển thị giao diện hiện đại Windows (Heading, Text, Expander).
    /// - Giữ cố định trên màn hình cho đến khi nhân viên trực cổng xác nhận đóng hoặc tự động giải phóng (DismissActive) khi có lượt quẹt thẻ/radar mới.
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
        /// Hiển thị cảnh báo LỖI (Error).
        /// </summary>
        public static void ShowError(
            IWin32Window? owner,
            string heading,
            string text,
            string? details = null,
            string caption = "HPParking - Cảnh Báo Lỗi")
        {
            ShowDialog(owner, heading, text, TaskDialogIcon.Error, details, caption);
        }

        /// <summary>
        /// Hiển thị CẢNH BÁO (Warning).
        /// </summary>
        public static void ShowWarning(
            IWin32Window? owner,
            string heading,
            string text,
            string? details = null,
            string caption = "HPParking - Cảnh Báo")
        {
            ShowDialog(owner, heading, text, TaskDialogIcon.Warning, details, caption);
        }

        /// <summary>
        /// Hiển thị THÔNG BÁO (Information).
        /// </summary>
        public static void ShowInfo(
            IWin32Window? owner,
            string heading,
            string text,
            string? details = null,
            string caption = "HPParking - Thông Báo")
        {
            ShowDialog(owner, heading, text, TaskDialogIcon.Information, details, caption);
        }

        /// <summary>
        /// Hiển thị hộp thoại XÁC NHẬN (Yes / No).
        /// </summary>
        public static bool ShowConfirmation(
            IWin32Window? owner,
            string heading,
            string text,
            string? details = null,
            string caption = "HPParking - Xác Nhận")
        {
            return ShowConfirmDialog(owner, heading, text, details, caption);
        }

        private static void ShowDialog(
            IWin32Window? owner,
            string heading,
            string text,
            TaskDialogIcon icon,
            string? details,
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
            };

            page.Destroyed += (s, e) =>
            {
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
            };

            page.Destroyed += (s, e) =>
            {
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
                lock (_syncLock)
                {
                    _activeDismissAction = null;
                }
            }
        }
    }
}
