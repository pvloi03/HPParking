using FluentValidation;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.Validators.Vehicles;

namespace HPParking.Api.Validators.Clients
{
    public class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
    {
        public CreateClientRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Họ và tên khách hàng không được để trống.")
                .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .Matches(@"^0\d{9}$").WithMessage("Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0 (ví dụ: 0364336088).");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Số CCCD/Định danh cá nhân không được để trống.")
                .Matches(@"^[0-9]{9,12}$").WithMessage("Số CCCD/Định danh cá nhân phải gồm 9 đến 12 chữ số.");

            When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            {
                RuleFor(x => x.Email!)
                    .EmailAddress().WithMessage("Email không đúng định dạng hợp lệ.")
                    .MaximumLength(150).WithMessage("Email không được vượt quá 150 ký tự.");
            });

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Loại khách hàng không hợp lệ.");

            RuleFor(x => x.Gender)
                .InclusiveBetween(0, 2).WithMessage("Giới tính không hợp lệ (0: Nữ, 1: Nam, 2: Khác).");

            RuleFor(x => x.Address)
                .MaximumLength(300).WithMessage("Địa chỉ không được vượt quá 300 ký tự.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");

            RuleFor(x => x.AuthMethods)
                .NotEmpty().WithMessage("Phương thức xác thực người dùng không được để trống.")
                .Must(HPParking.Core.Constants.AuthMethodConstants.IsValid)
                .WithMessage("Phương thức xác thực không hợp lệ. Nếu chọn 'None' (làn tự do) thì không được kết hợp cùng 'Card' hoặc 'FaceId'.");

            When(x => x.AuthMethods != null && x.AuthMethods.Any(m => !string.Equals(m, HPParking.Core.Constants.AuthMethodConstants.None, StringComparison.OrdinalIgnoreCase)), () =>
            {
                RuleFor(x => x.CardCode)
                    .NotEmpty()
                    .WithMessage("Khi sử dụng phương thức xác thực (Thẻ/FaceID), bắt buộc phải gán thẻ định danh RFID.");
            });

            When(x => x.VerifyVehiclePlate, () =>
            {
                RuleFor(x => x.Vehicles)
                    .Must(v => v != null && v.Count > 0)
                    .WithMessage("Khi bật xác thực đối chiếu xe, bắt buộc phải đăng ký ít nhất một phương tiện.");
            });

            When(x => x.Type == HPParking.Core.Models.Enums.ClientType.Contractor, () =>
            {
                RuleFor(x => x.ContractorId)
                    .NotEmpty().WithMessage("Đối tượng Nhà thầu bắt buộc phải chọn Nhà thầu.");
            });

            When(x => x.Vehicles != null && x.Vehicles.Count > 0, () =>
            {
                RuleForEach(x => x.Vehicles)
                    .SetValidator(new CreateVehicleRequestValidator());
            });
        }
    }
}
