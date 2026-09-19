using FluentValidation;
using HPParking.Api.DTOs.ParkingSessions;

namespace HPParking.Api.Validators.ParkingSessions
{
    public class ParkingSessionFilterQueryValidator : AbstractValidator<ParkingSessionFilterQuery>
    {
        public ParkingSessionFilterQueryValidator()
        {
            RuleFor(x => x.PageIndex)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Chỉ số trang (PageIndex) phải lớn hơn hoặc bằng 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Kích thước trang (PageSize) phải từ 1 đến 100.");

            When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
            {
                RuleFor(x => x.ToDate)
                    .GreaterThanOrEqualTo(x => x.FromDate!.Value)
                    .WithMessage("Thời điểm kết thúc (ToDate) phải lớn hơn hoặc bằng thời điểm bắt đầu (FromDate).");
            });

            When(x => !string.IsNullOrWhiteSpace(x.PersonId), () =>
            {
                RuleFor(x => x.PersonId!)
                    .Matches(@"^[a-fA-F0-9]{24}$")
                    .WithMessage("PersonId không đúng định dạng ObjectId hợp lệ.");
            });
        }
    }
}
