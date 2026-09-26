using System;

namespace HPParking.Core.Models.Common
{
    /// <summary>
    /// Attribute đánh dấu các thuộc tính chứa dữ liệu nhạy cảm (mật khẩu, token, khóa bí mật)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SensitiveDataAttribute : Attribute
    {
    }
}
