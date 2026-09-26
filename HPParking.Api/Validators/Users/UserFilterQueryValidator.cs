using FluentValidation;
using HPParking.Api.DTOs.Users;

namespace HPParking.Api.Validators.Users
{
    public class UserFilterQueryValidator : AbstractValidator<UserFilterQuery>
    {
        public UserFilterQueryValidator()
        {
            RuleFor(x => x.PageIndex)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Chỉ số trang (PageIndex) phải lớn hơn hoặc bằng 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Kích thước trang (PageSize) phải từ 1 đến 100.");
        }
    }
}
