using FluentValidation;
using HPParking.Api.DTOs.Devices;
using System.Net;

namespace HPParking.Api.Validators.Devices
{
    public class UpdateDeviceRequestValidator : AbstractValidator<UpdateDeviceRequest>
    {
        public UpdateDeviceRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã thiết bị không được để trống.")
                .MaximumLength(50).WithMessage("Mã thiết bị không được vượt quá 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Mã thiết bị chỉ được chứa các ký tự chữ, số, gạch dưới hoặc gạch ngang.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên thiết bị không được để trống.")
                .MaximumLength(100).WithMessage("Tên thiết bị không được vượt quá 100 ký tự.");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Loại thiết bị không hợp lệ (1: Camera, 2: Controller, 3: FaceId, 4: Other).");

            RuleFor(x => x.IpAddress)
                .NotEmpty().WithMessage("Địa chỉ IP không được để trống.")
                .Must(BeValidIpv4).WithMessage("Địa chỉ IP không đúng định dạng IPv4 hợp lệ (Ví dụ: 192.168.1.100).");

            RuleFor(x => x.Port)
                .InclusiveBetween(1, 65535).WithMessage("Cổng mạng (Port) phải nằm trong khoảng từ 1 đến 65535.");

            When(x => !string.IsNullOrWhiteSpace(x.UserName), () =>
            {
                RuleFor(x => x.UserName!)
                    .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Password), () =>
            {
                RuleFor(x => x.Password!)
                    .MaximumLength(100).WithMessage("Mật khẩu không được vượt quá 100 ký tự.");
            });
        }

        private static bool BeValidIpv4(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            var parts = ip.Split('.');
            if (parts.Length != 4) return false;

            return IPAddress.TryParse(ip, out var address) &&
                   address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }
    }
}
