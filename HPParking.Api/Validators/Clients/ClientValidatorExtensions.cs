using FluentValidation;
using HPParking.Core.Helpers;

namespace HPParking.Api.Validators.Clients
{
    /// <summary>
    /// Các phương thức mở rộng quy tắc FluentValidation cho thực thể Nhân sự (Client)
    /// </summary>
    public static class ClientValidatorExtensions
    {
        /// <summary>
        /// Áp dụng bộ quy tắc kiểm tra tính hợp lệ của Mã định danh nhân sự (bắt buộc, tối đa 50 ký tự, regex chuẩn)
        /// </summary>
        public static IRuleBuilderOptions<T, string> ValidClientCode<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage(ClientCodeHelper.MessageNotEmpty)
                .MaximumLength(ClientCodeHelper.MaxLength).WithMessage(ClientCodeHelper.MessageMaxLength)
                .Matches(ClientCodeHelper.Pattern).WithMessage(ClientCodeHelper.MessageInvalidFormat);
        }
    }
}
