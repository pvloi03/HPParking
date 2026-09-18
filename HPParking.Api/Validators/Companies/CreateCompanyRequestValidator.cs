using FluentValidation;
using HPParking.Api.DTOs.Companies;

namespace HPParking.Api.Validators.Companies
{
    public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
    {
        public CreateCompanyRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã công ty không được để trống.")
                .MaximumLength(50).WithMessage("Mã công ty không được vượt quá 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Mã công ty chỉ được chứa các ký tự chữ, số, gạch dưới hoặc gạch ngang.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên công ty không được để trống.")
                .MaximumLength(200).WithMessage("Tên công ty không được vượt quá 200 ký tự.");

            When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            {
                RuleFor(x => x.PhoneNumber!)
                    .Matches(@"^[0-9\+\-\s\(\)]{8,20}$").WithMessage("Số điện thoại công ty không đúng định dạng hợp lệ.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            {
                RuleFor(x => x.Email!)
                    .EmailAddress().WithMessage("Email công ty không đúng định dạng hợp lệ.")
                    .MaximumLength(150).WithMessage("Email không được vượt quá 150 ký tự.");
            });
        }
    }
}
