using FluentValidation;
using HPParking.Api.DTOs.Departments;

namespace HPParking.Api.Validators.Departments
{
    public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
    {
        public UpdateDepartmentRequestValidator()
        {
            When(x => !string.IsNullOrWhiteSpace(x.CompanyId), () =>
            {
                RuleFor(x => x.CompanyId!)
                    .Matches(@"^[a-fA-F0-9]{24}$").WithMessage("CompanyId phải là chuỗi 24 ký tự Hex của MongoDB ObjectId hợp lệ.");
            });

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã phòng ban không được để trống.")
                .MaximumLength(50).WithMessage("Mã phòng ban không được vượt quá 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Mã phòng ban chỉ được chứa các ký tự chữ, số, gạch dưới hoặc gạch ngang.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên phòng ban không được để trống.")
                .MaximumLength(150).WithMessage("Tên phòng ban không được vượt quá 150 ký tự.");

            When(x => !string.IsNullOrWhiteSpace(x.ManagerName), () =>
            {
                RuleFor(x => x.ManagerName!)
                    .MaximumLength(100).WithMessage("Tên người phụ trách không được vượt quá 100 ký tự.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            {
                RuleFor(x => x.PhoneNumber!)
                    .Matches(@"^[0-9\+\-\s\(\)]{8,20}$").WithMessage("Số điện thoại phòng ban không đúng định dạng hợp lệ.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            {
                RuleFor(x => x.Email!)
                    .EmailAddress().WithMessage("Email phòng ban không đúng định dạng hợp lệ.")
                    .MaximumLength(150).WithMessage("Email không được vượt quá 150 ký tự.");
            });
        }
    }
}
