using System.Linq.Expressions;
using System.Reflection;

namespace HPParking.Api.Common.Excel
{
    /// <summary>
    /// Builder hỗ trợ thiết lập cấu hình Fluent cho từng cột Excel
    /// </summary>
    public class ExcelColumnBuilder<T, TProperty>
    {
        private readonly ExcelColumnDefinition _def;

        public ExcelColumnBuilder(ExcelColumnDefinition def)
        {
            _def = def;
        }

        public ExcelColumnBuilder<T, TProperty> ColumnName(string name)
        {
            _def.ColumnName = name;
            return this;
        }

        public ExcelColumnBuilder<T, TProperty> Order(int order)
        {
            _def.Order = order;
            return this;
        }

        public ExcelColumnBuilder<T, TProperty> Required(bool required = true)
        {
            _def.IsRequired = required;
            return this;
        }

        public ExcelColumnBuilder<T, TProperty> WithComment(string comment)
        {
            _def.Comment = comment;
            return this;
        }

        public ExcelColumnBuilder<T, TProperty> Format(string format)
        {
            _def.Format = format;
            return this;
        }

        public ExcelColumnBuilder<T, TProperty> EnumDropdown<TEnum>() where TEnum : struct, Enum
        {
            _def.EnumType = typeof(TEnum);
            _def.DropdownOptions = Enum.GetNames(typeof(TEnum));
            return this;
        }

        public ExcelColumnBuilder<T, TProperty> Dropdown(params string[] options)
        {
            _def.DropdownOptions = options;
            return this;
        }

        public ExcelColumnBuilder<T, TProperty> CustomFormat(Func<TProperty, string> formatter)
        {
            _def.CustomFormatter = obj => obj is TProperty prop ? formatter(prop) : string.Empty;
            return this;
        }

        public ExcelColumnBuilder<T, TProperty> CustomParse(Func<string, TProperty> parser)
        {
            _def.CustomParser = str => parser(str);
            return this;
        }
    }

    /// <summary>
    /// Lớp cơ sở định nghĩa hồ sơ ánh xạ Fluent Profile cho thực thể Excel (ADR 0023)
    /// </summary>
    public abstract class ExcelProfile<T> where T : class
    {
        private readonly List<ExcelColumnDefinition> _columns = new();

        public IReadOnlyList<ExcelColumnDefinition> Columns => _columns.OrderBy(c => c.Order).ToList();

        protected ExcelColumnBuilder<T, TProperty> Map<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            MemberExpression? member = propertyExpression.Body switch
            {
                MemberExpression m => m,
                UnaryExpression { Operand: MemberExpression um } => um,
                _ => null
            };

            if (member == null || member.Member is not PropertyInfo propInfo)
            {
                throw new ArgumentException("Biểu thức phải trỏ tới một thuộc tính hợp lệ.", nameof(propertyExpression));
            }

            var def = new ExcelColumnDefinition
            {
                Property = propInfo,
                ColumnName = propInfo.Name,
                Order = _columns.Count + 1
            };

            _columns.Add(def);
            return new ExcelColumnBuilder<T, TProperty>(def);
        }
    }
}
