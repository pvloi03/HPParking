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
        /// <returns>True nếu tất cả đều hợp lệ, False nếu có Control rỗng</returns>
        /// 
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public Dictionary<string, string> Values { get; set; } = [];
        }

        public static ValidationResult CheckControlsNotEmpty(IEnumerable<Control> controls)
        {
            ValidationResult validationResult = new();
            if (controls == null)
            {
                MessageBox.Show("Danh sách kiểm tra không tồn tại!",
                    "Cảnh báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                validationResult.IsValid = false;
                validationResult.Values.Clear();
                return validationResult;
            }

            foreach (Control control in controls)
            {
                if (control == null) continue;

                // Không có Tag thì bỏ qua, không kiểm tra
                string tag = control.Tag?.ToString();
                if (string.IsNullOrWhiteSpace(tag))
                {
                    validationResult.Values.Add(control.Name, control.Text.Trim());
                    continue;
                }

                string[] parts = tag.Split('|');

                string fieldName = parts[0];
                string rule = parts.Length > 1 ? parts[1].ToLower() : "";

                string value = control.Text.Trim();

                if (string.IsNullOrWhiteSpace(value))
                {
                    MessageBox.Show($"{fieldName} không được để trống!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    validationResult.Values.Clear();
                    validationResult.IsValid = false;

                    control.Focus();
                    return validationResult;
                }

                if (rule == "number" && !int.TryParse(value, out _))
                {
                    MessageBox.Show(
                        $"{fieldName} phải là số.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    validationResult.Values.Clear();
                    validationResult.IsValid = false;

                    control.Focus();
                    return validationResult;
                }

                validationResult.Values.Add(control.Name, value);
            }

            validationResult.IsValid = true;
            return validationResult;
        }
    }
}