using FluentValidation;
using HPParking.Api.DTOs.Lanes;

namespace HPParking.Api.Validators.Lanes
{
    public class CreateLaneRequestValidator : AbstractValidator<CreateLaneRequest>
    {
        public CreateLaneRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã làn xe không được để trống.")
                .MaximumLength(50).WithMessage("Mã làn xe không được vượt quá 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Mã làn xe chỉ được chứa ký tự chữ, số, gạch dưới hoặc gạch ngang.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên làn xe không được để trống.")
                .MaximumLength(150).WithMessage("Tên làn xe không được vượt quá 150 ký tự.");

            RuleFor(x => x.GateId)
                .NotEmpty().WithMessage("Mã định danh cổng (GateId) không được để trống.")
                .Matches(@"^[a-fA-F0-9]{24}$").WithMessage("GateId phải là chuỗi 24 ký tự Hex của MongoDB ObjectId hợp lệ.");

            RuleFor(x => x.Direction)
                .IsInEnum().WithMessage("Hướng di chuyển của làn xe không hợp lệ (1: In, 2: Out, 3: Bidirectional).");

            RuleFor(x => x.OutputRelay)
                .GreaterThanOrEqualTo(0).WithMessage("Cổng OutputRelay phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.InputReader)
                .GreaterThanOrEqualTo(0).WithMessage("Cổng InputReader phải lớn hơn hoặc bằng 0.");

            When(x => !string.IsNullOrWhiteSpace(x.OverviewCameraDeviceId), () =>
            {
                RuleFor(x => x.OverviewCameraDeviceId!)
                    .Matches(@"^[a-fA-F0-9]{24}$").WithMessage("OverviewCameraDeviceId phải là chuỗi 24 ký tự Hex của MongoDB ObjectId hợp lệ.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.PlateCameraDeviceId), () =>
            {
                RuleFor(x => x.PlateCameraDeviceId!)
                    .Matches(@"^[a-fA-F0-9]{24}$").WithMessage("PlateCameraDeviceId phải là chuỗi 24 ký tự Hex của MongoDB ObjectId hợp lệ.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.ControllerDeviceId), () =>
            {
                RuleFor(x => x.ControllerDeviceId!)
                    .Matches(@"^[a-fA-F0-9]{24}$").WithMessage("ControllerDeviceId phải là chuỗi 24 ký tự Hex của MongoDB ObjectId hợp lệ.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.FaceDeviceId), () =>
            {
                RuleFor(x => x.FaceDeviceId!)
                    .Matches(@"^[a-fA-F0-9]{24}$").WithMessage("FaceDeviceId phải là chuỗi 24 ký tự Hex của MongoDB ObjectId hợp lệ.");
            });
        }
    }
}
