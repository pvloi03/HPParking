using FluentValidation;
using HPParking.Api.DTOs.Clients;

namespace HPParking.Api.Validators.Clients
{
    public class UpdateClientRequestValidator : AbstractValidator<UpdateClientRequest>
    {
        public UpdateClientRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Họ và tên khách hàng không được để trống.")
                .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .Matches(@"^0\d{9}$").WithMessage("Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0 (ví dụ: 0364336088).");

            When(x => !string.IsNullOrWhiteSpace(x.Code), () =>
            {
                RuleFor(x => x.Code!)
                    .Matches(@"^[0-9]{9,12}$").WithMessage("Số CCCD/Định danh cá nhân phải gồm 9 đến 12 chữ số.");
            });

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
        }
    }
}
