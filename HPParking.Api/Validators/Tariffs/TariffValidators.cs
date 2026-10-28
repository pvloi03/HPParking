using FluentValidation;
using HPParking.Api.DTOs.Tariffs;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.Validators.Tariffs
{
    public class CreateTariffRequestValidator : AbstractValidator<CreateTariffRequest>
    {
        public CreateTariffRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã biểu phí không được để trống.")
                .MaximumLength(50).WithMessage("Mã biểu phí không được vượt quá 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Mã biểu phí chỉ được chứa các ký tự chữ, số, gạch dưới hoặc gạch ngang.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên biểu phí không được để trống.")
                .MaximumLength(200).WithMessage("Tên biểu phí không được vượt quá 200 ký tự.");

            RuleFor(x => x.VehicleType)
                .IsInEnum().WithMessage("Loại phương tiện không hợp lệ.");

            RuleFor(x => x.PricingMode)
                .IsInEnum().WithMessage("Chế độ tính phí không hợp lệ.");

            RuleFor(x => x.GracePeriodMinutes)
                .GreaterThanOrEqualTo(0).WithMessage("Số phút ân hạn không được âm.");

            RuleFor(x => x.OvernightSurcharge)
                .GreaterThanOrEqualTo(0).WithMessage("Phụ thu qua đêm không được âm.");

            RuleFor(x => x.MaxDailyCap)
                .GreaterThanOrEqualTo(0).WithMessage("Mức trần mỗi ngày không được âm.");

            When(x => x.PricingMode == TariffPricingMode.Flat, () =>
            {
                RuleFor(x => x.FlatPrice)
                    .GreaterThanOrEqualTo(0).WithMessage("Giá cố định theo lượt không được âm.");
            });

            When(x => x.PricingMode == TariffPricingMode.Block, () =>
            {
                RuleFor(x => x.BaseBlockMinutes)
                    .GreaterThan(0).WithMessage("Thời lượng block đầu phải lớn hơn 0 phút.");

                RuleFor(x => x.AdditionalBlockMinutes)
                    .GreaterThan(0).WithMessage("Thời lượng block tiếp theo phải lớn hơn 0 phút.");

                RuleFor(x => x.BasePrice)
                    .GreaterThanOrEqualTo(0).WithMessage("Giá block đầu không được âm.");

                RuleFor(x => x.AdditionalBlockPrice)
                    .GreaterThanOrEqualTo(0).WithMessage("Giá mỗi block tiếp theo không được âm.");
            });
        }
    }

    public class UpdateTariffRequestValidator : AbstractValidator<UpdateTariffRequest>
    {
        public UpdateTariffRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã biểu phí không được để trống.")
                .MaximumLength(50).WithMessage("Mã biểu phí không được vượt quá 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Mã biểu phí chỉ được chứa các ký tự chữ, số, gạch dưới hoặc gạch ngang.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên biểu phí không được để trống.")
                .MaximumLength(200).WithMessage("Tên biểu phí không được vượt quá 200 ký tự.");

            RuleFor(x => x.VehicleType)
                .IsInEnum().WithMessage("Loại phương tiện không hợp lệ.");

            RuleFor(x => x.PricingMode)
                .IsInEnum().WithMessage("Chế độ tính phí không hợp lệ.");

            RuleFor(x => x.GracePeriodMinutes)
                .GreaterThanOrEqualTo(0).WithMessage("Số phút ân hạn không được âm.");

            RuleFor(x => x.OvernightSurcharge)
                .GreaterThanOrEqualTo(0).WithMessage("Phụ thu qua đêm không được âm.");

            RuleFor(x => x.MaxDailyCap)
                .GreaterThanOrEqualTo(0).WithMessage("Mức trần mỗi ngày không được âm.");

            When(x => x.PricingMode == TariffPricingMode.Flat, () =>
            {
                RuleFor(x => x.FlatPrice)
                    .GreaterThanOrEqualTo(0).WithMessage("Giá cố định theo lượt không được âm.");
            });

            When(x => x.PricingMode == TariffPricingMode.Block, () =>
            {
                RuleFor(x => x.BaseBlockMinutes)
                    .GreaterThan(0).WithMessage("Thời lượng block đầu phải lớn hơn 0 phút.");

                RuleFor(x => x.AdditionalBlockMinutes)
                    .GreaterThan(0).WithMessage("Thời lượng block tiếp theo phải lớn hơn 0 phút.");

                RuleFor(x => x.BasePrice)
                    .GreaterThanOrEqualTo(0).WithMessage("Giá block đầu không được âm.");

                RuleFor(x => x.AdditionalBlockPrice)
                    .GreaterThanOrEqualTo(0).WithMessage("Giá mỗi block tiếp theo không được âm.");
            });
        }
    }
}
