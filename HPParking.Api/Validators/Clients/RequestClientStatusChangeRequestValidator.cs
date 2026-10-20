using FluentValidation;
using HPParking.Api.DTOs.Clients;

namespace HPParking.Api.Validators.Clients
{
    public class RequestClientStatusChangeRequestValidator : AbstractValidator<RequestClientStatusChangeRequest>
    {
        public RequestClientStatusChangeRequestValidator()
        {
            RuleFor(x => x.ApprovingDepartmentId)
                .NotEmpty().WithMessage("Vui lòng chọn phòng ban tiếp nhận email phê duyệt.");

            RuleFor(x => x.ActionType)
                .IsInEnum().WithMessage("Loại hành động phê duyệt không hợp lệ.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
        }
    }
}
