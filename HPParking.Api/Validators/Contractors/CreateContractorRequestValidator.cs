using FluentValidation;
using HPParking.Api.DTOs.Contractors;

namespace HPParking.Api.Validators.Contractors
{
    public class CreateContractorRequestValidator : AbstractValidator<CreateContractorRequest>
    {
        public CreateContractorRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã nhà thầu không được để trống.")
                .MaximumLength(50).WithMessage("Mã nhà thầu không được vượt quá 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Mã nhà thầu chỉ được chứa các ký tự chữ, số, gạch dưới hoặc gạch ngang.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên nhà thầu không được để trống.")
                .MaximumLength(200).WithMessage("Tên nhà thầu không được vượt quá 200 ký tự.");

            When(x => !string.IsNullOrWhiteSpace(x.ContactPerson), () =>
            {
                RuleFor(x => x.ContactPerson!)
                    .MaximumLength(100).WithMessage("Tên người liên hệ không được vượt quá 100 ký tự.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            {
                RuleFor(x => x.PhoneNumber!)
                    .Matches(@"^[0-9\+\-\s\(\)]{8,20}$").WithMessage("Số điện thoại nhà thầu không đúng định dạng hợp lệ.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            {
                RuleFor(x => x.Email!)
                    .EmailAddress().WithMessage("Email nhà thầu không đúng định dạng hợp lệ.")
                    .MaximumLength(150).WithMessage("Email không được vượt quá 150 ký tự.");
            });
        }
    }
}
