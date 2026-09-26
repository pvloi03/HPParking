using System.Reflection;

namespace HPParking.Api.Common.Excel
{
    /// <summary>
    /// Định nghĩa cấu hình siêu dữ liệu cho một cột trong bảng tính Excel
    /// </summary>
    public class ExcelColumnDefinition
    {
        public PropertyInfo Property { get; set; } = null!;
        public string ColumnName { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsRequired { get; set; }
        public string? Comment { get; set; }
        public string? Format { get; set; }
        public Type? EnumType { get; set; }
        public string[]? DropdownOptions { get; set; }
        public Func<object, string>? CustomFormatter { get; set; }
        public Func<string, object?>? CustomParser { get; set; }
    }
}
