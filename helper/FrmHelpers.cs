using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ookii.Dialogs.WinForms;

namespace HPParking.Helper
{
    public class FrmHelpers
    {
        public static List<TControlItem> GetControls<TControlItem>(IEnumerable<Control> containers)
            where TControlItem : Control
        {
            return [.. containers
                .SelectMany(c => c.Controls.OfType<TControlItem>())
                .OrderBy(c => c.TabIndex)]; // Sắp xếp tăng dần theo TabIndex
        }

        public static Bitmap? Base64ToBitmap(string? base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String)) return null;

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String!);
                using var ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                using var temp = new Bitmap(ms);
                return new Bitmap(temp); // Trả về bản sao an toàn cho PictureBox
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FrmHelpers Error] Không thể chuyển đổi Base64 sang Bitmap: {ex.Message}");
                return null;
            }
        }

        public static void ShowMessage(string message, string title = "Thông báo", TaskDialogIcon icon = TaskDialogIcon.Information)
        {
            using (var dialog = new TaskDialog())
            {
                dialog.WindowTitle = title;
                dialog.MainInstruction = message;
                dialog.MainIcon = icon;
                dialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                dialog.ShowDialog();
            }
        }
    }
}
