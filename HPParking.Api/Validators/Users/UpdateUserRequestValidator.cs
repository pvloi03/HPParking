using FluentValidation;
using HPParking.Api.DTOs.Users;

namespace HPParking.Api.Validators.Users
{
    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống.")
                .MaximumLength(150).WithMessage("Họ và tên không được vượt quá 150 ký tự.");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Vai trò tài khoản không hợp lệ (hỗ trợ Admin, Manager, Viewer).");

            When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            {
                RuleFor(x => x.Email!)
                    .EmailAddress().WithMessage("Địa chỉ email không đúng định dạng hợp lệ.")
                    .MaximumLength(150).WithMessage("Email không được vượt quá 150 ký tự.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            {
                RuleFor(x => x.PhoneNumber!)
                    .Matches(@"^[0-9\+\-\s\(\)]{8,20}$").WithMessage("Số điện thoại không đúng định dạng hợp lệ.");
            });
        }
    }
}
