using FluentValidation;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.Validators.ParkingSessions
{
    public class ForceCloseParkingSessionRequestValidator : AbstractValidator<ForceCloseParkingSessionRequest>
    {
        public ForceCloseParkingSessionRequestValidator()
        {
            RuleFor(x => x.CloseReason)
                .IsInEnum()
                .Must(r => r == SessionCloseReason.LostCard || r == SessionCloseReason.AdminForceClose)
                .WithMessage("Lý do đóng phiên cưỡng bức phải là LostCard hoặc AdminForceClose.");

            When(x => x.OverrideFeeAmount.HasValue, () =>
            {
                RuleFor(x => x.OverrideFeeAmount!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Số tiền phí không được âm.");
            });
        }
    }
}
