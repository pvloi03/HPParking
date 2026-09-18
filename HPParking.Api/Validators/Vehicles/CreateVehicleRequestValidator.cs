using FluentValidation;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Vehicles;

namespace HPParking.Api.Validators.Vehicles
{
    public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
    {
        public CreateVehicleRequestValidator()
        {
            RuleFor(x => x.PlateNumber)
                .NotEmpty().WithMessage("Biển số xe không được để trống.")
                .MaximumLength(20).WithMessage("Biển số xe không được vượt quá 20 ký tự.")
                .Must(PlateHelper.IsValid).WithMessage("Biển số xe không đúng định dạng chuẩn Việt Nam (ví dụ: 30A-123.45 hoặc 29B1-123.45).");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Loại phương tiện không hợp lệ.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
        }
    }
}
