using FluentValidation;
using HPParking.Api.DTOs.Cards;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.ValueObjects;

namespace HPParking.Api.Validators.Cards
{
    public static class CardValidationExtensions
    {
        public static void ApplyMonthlyCardValidityRules<T>(this AbstractValidator<T> validator)
            where T : ICardValidityRequest
        {
            validator.When(x => x.TargetType == CardTargetType.Monthly, () =>
            {
                validator.RuleFor(x => x.ValidFrom)
                    .NotEmpty().WithMessage("Ngày bắt đầu hiệu lực không được để trống đối với thẻ tháng.")
                    .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Ngày bắt đầu hiệu lực phải theo định dạng yyyy-MM-dd.")
                    .Must(CardValidityPeriod.IsValidIsoDate).WithMessage("Ngày bắt đầu hiệu lực không phải ngày hợp lệ.");

                validator.RuleFor(x => x.ValidTo)
                    .NotEmpty().WithMessage("Ngày kết thúc hiệu lực không được để trống đối với thẻ tháng.")
                    .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Ngày kết thúc hiệu lực phải theo định dạng yyyy-MM-dd.")
                    .Must(CardValidityPeriod.IsValidIsoDate).WithMessage("Ngày kết thúc hiệu lực không phải ngày hợp lệ.");

                validator.RuleFor(x => x)
                    .Must(x => !CardValidityPeriod.TryParseDate(x.ValidFrom, out var from) ||
                               !CardValidityPeriod.TryParseDate(x.ValidTo, out var to) ||
                               to >= from)
                    .WithMessage("Ngày kết thúc hiệu lực phải lớn hơn hoặc bằng ngày bắt đầu.")
                    .When(x => !string.IsNullOrEmpty(x.ValidFrom) && !string.IsNullOrEmpty(x.ValidTo));
            });
        }
    }

    public class CreateCardRequestValidator : AbstractValidator<CreateCardRequest>
    {
        public CreateCardRequestValidator()
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty().WithMessage("Mã thẻ không được để trống.")
                .MaximumLength(10).WithMessage("Mã thẻ không được vượt quá 10 ký tự.")
                .Matches(@"^\d+$").WithMessage("Mã thẻ chỉ được chứa các ký tự số.");

            this.ApplyMonthlyCardValidityRules();
        }
    }

    public class UpdateCardRequestValidator : AbstractValidator<UpdateCardRequest>
    {
        public UpdateCardRequestValidator()
        {
            this.ApplyMonthlyCardValidityRules();
        }
    }
}
