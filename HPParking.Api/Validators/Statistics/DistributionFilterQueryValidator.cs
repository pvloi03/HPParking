using FluentValidation;
using HPParking.Api.DTOs.Statistics;

namespace HPParking.Api.Validators.Statistics
{
    public class DistributionFilterQueryValidator : AbstractValidator<DistributionFilterQuery>
    {
        public DistributionFilterQueryValidator()
        {
            When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
            {
                RuleFor(x => x.ToDate)
                    .GreaterThanOrEqualTo(x => x.FromDate!.Value)
                    .WithMessage("Thời điểm kết thúc (ToDate) phải lớn hơn hoặc bằng thời điểm bắt đầu (FromDate).");
            });

            When(x => !string.IsNullOrWhiteSpace(x.CompanyId), () =>
            {
                RuleFor(x => x.CompanyId!)
                    .Matches(@"^[a-fA-F0-9]{24}$")
                    .WithMessage("CompanyId không đúng định dạng ObjectId hợp lệ.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.DepartmentId), () =>
            {
                RuleFor(x => x.DepartmentId!)
                    .Matches(@"^[a-fA-F0-9]{24}$")
                    .WithMessage("DepartmentId không đúng định dạng ObjectId hợp lệ.");
            });
        }
    }
}
