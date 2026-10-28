using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Cards
{
    public class CardDto : AuditableDto
    {
        public string CardNumber { get; set; } = string.Empty;
        public CardTargetType TargetType { get; set; } = CardTargetType.Person;
        public string? ClientId { get; set; }
        public string? ClientCode { get; set; }
        public string? ClientName { get; set; }
        public string? VehicleId { get; set; }
        public string? PlateNumber { get; set; }
        public VehicleType? VehicleType { get; set; }
        public bool IsChargeable { get; set; } = false;
        public string? TariffId { get; set; }
        public string? TariffCode { get; set; }
        public string? TariffName { get; set; }
        public string? ValidFrom { get; set; }
        public string? ValidTo { get; set; }
        public CardStatus Status { get; set; } = CardStatus.InUse;
        public string? Note { get; set; }
    }

    public interface ICardValidityRequest
    {
        CardTargetType TargetType { get; set; }
        string? ValidFrom { get; set; }
        string? ValidTo { get; set; }
    }

    public class CreateCardRequest : ICardValidityRequest
    {
        public string CardNumber { get; set; } = string.Empty;
        public CardTargetType TargetType { get; set; } = CardTargetType.Person;
        public string? ClientId { get; set; }
        public string? VehicleId { get; set; }
        public VehicleType? VehicleType { get; set; }
        public bool IsChargeable { get; set; } = false;
        public string? TariffId { get; set; }
        public string? ValidFrom { get; set; }
        public string? ValidTo { get; set; }
        public CardStatus Status { get; set; } = CardStatus.InUse;
        public string? Note { get; set; }
    }

    public class UpdateCardRequest : ICardValidityRequest
    {
        public CardTargetType TargetType { get; set; } = CardTargetType.Person;
        public string? ClientId { get; set; }
        public string? VehicleId { get; set; }
        public VehicleType? VehicleType { get; set; }
        public bool IsChargeable { get; set; } = false;
        public string? TariffId { get; set; }
        public string? ValidFrom { get; set; }
        public string? ValidTo { get; set; }
        public CardStatus Status { get; set; } = CardStatus.InUse;
        public string? Note { get; set; }
    }
}
