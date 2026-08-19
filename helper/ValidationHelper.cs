using System.Collections.Generic;
using System.Windows.Forms;

namespace HPParking.Helper
{
    public class ValidationHelper
    {
        /// <summary>
        /// Kiểm tra danh sách các Control xem có ô nào bị bỏ trống hay không.
        /// Sử dụng thuộc tính Tag của Control để làm tên hiển thị thông báo.
        /// </summary>
        /// <param name="controls">Danh sách các Control cần kiểm tra (TextBox, ComboBox, ...)</param>
        /// <param name="showMessageBox">Có tự động hiển thị MessageBox khi lỗi hay không</param>
        /// <returns>ValidationResult chứa trạng thái hợp lệ, thông báo lỗi và dictionary giá trị</returns>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public Control? InvalidControl { get; set; }
            public Dictionary<string, string> Values { get; set; } = [];
        }

        public static ValidationResult CheckControlsNotEmpty(IEnumerable<Control> controls, bool showMessageBox = true)
        {
            ValidationResult validationResult = new();
            if (controls == null)
            {
                validationResult.IsValid = false;
                validationResult.ErrorMessage = "Danh sách kiểm tra không tồn tại!";
                validationResult.Values.Clear();

                if (showMessageBox)
                {
                    MessageBox.Show(
                        validationResult.ErrorMessage,
                        "Cảnh báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return validationResult;
            }

            foreach (Control control in controls)
            {
                if (control == null) continue;

                // Không có Tag thì bỏ qua, không kiểm tra
                string? tag = control.Tag?.ToString();
                if (string.IsNullOrWhiteSpace(tag))
                {
                    validationResult.Values[control.Name] = control.Text.Trim();
                    continue;
                }

                string[] parts = tag!.Split('|');

                string fieldName = parts[0];
                string rule = parts.Length > 1 ? parts[1].ToLower() : "";

                string value = control.Text.Trim();

                if (string.IsNullOrWhiteSpace(value))
                {
                    validationResult.IsValid = false;
                    validationResult.InvalidControl = control;
                    validationResult.ErrorMessage = $"{fieldName} không được để trống!";
                    validationResult.Values.Clear();

                    if (showMessageBox)
                    {
                        MessageBox.Show(
                            validationResult.ErrorMessage,
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    control.Focus();
                    return validationResult;
                }

                if (rule == "number" && !int.TryParse(value, out _))
                {
                    validationResult.IsValid = false;
                    validationResult.InvalidControl = control;
                    validationResult.ErrorMessage = $"{fieldName} phải là số.";
                    validationResult.Values.Clear();

                    if (showMessageBox)
                    {
                        MessageBox.Show(
                            validationResult.ErrorMessage,
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    control.Focus();
                    return validationResult;
                }

                validationResult.Values[control.Name] = value;
            }

            validationResult.IsValid = true;
            return validationResult;
        }
    }
}