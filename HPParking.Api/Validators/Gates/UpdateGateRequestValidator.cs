using FluentValidation;
using HPParking.Api.DTOs.Gates;

namespace HPParking.Api.Validators.Gates
{
    public class UpdateGateRequestValidator : AbstractValidator<UpdateGateRequest>
    {
        public UpdateGateRequestValidator()
        {
            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("Mã định danh công ty (CompanyId) không được để trống.")
                .Matches(@"^[a-fA-F0-9]{24}$").WithMessage("CompanyId phải là chuỗi 24 ký tự Hex của MongoDB ObjectId hợp lệ.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã cổng không được để trống.")
                .MaximumLength(50).WithMessage("Mã cổng không được vượt quá 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Mã cổng chỉ được chứa các ký tự chữ, số, gạch dưới hoặc gạch ngang.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên cổng không được để trống.")
                .MaximumLength(150).WithMessage("Tên cổng không được vượt quá 150 ký tự.");

            RuleFor(x => x.MachineCode)
                .NotEmpty().WithMessage("Mã máy trạm (MachineCode) không được để trống.")
                .MaximumLength(100).WithMessage("Mã máy trạm không được vượt quá 100 ký tự.");
        }
    }
}
