using FluentValidation;
using HPParking.Api.DTOs.Users;

namespace HPParking.Api.Validators.Users
{
    public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
                .MinimumLength(3).WithMessage("Tên đăng nhập phải có ít nhất 3 ký tự.")
                .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Tên đăng nhập chỉ được chứa ký tự chữ cái, chữ số, gạch dưới (_) hoặc gạch ngang (-).");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu phải có độ dài tối thiểu 6 ký tự theo chính sách bảo mật.")
                .MaximumLength(100).WithMessage("Mật khẩu không được vượt quá 100 ký tự.");

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
