using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Entities;

namespace HPParking.Api.DTOs.Cards
{
    public class CardDto : AuditableDto
    {
        public string CardNumber { get; set; } = string.Empty;
        public CardTargetType TargetType { get; set; } = CardTargetType.Person;
        public string? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? VehicleId { get; set; }
        public string? PlateNumber { get; set; }
        public CardStatus Status { get; set; } = CardStatus.InUse;
        public string? Note { get; set; }
    }

    public class CreateCardRequest
    {
        public string CardNumber { get; set; } = string.Empty;
        public CardTargetType TargetType { get; set; } = CardTargetType.Person;
        public string? ClientId { get; set; }
        public string? VehicleId { get; set; }
        public CardStatus Status { get; set; } = CardStatus.InUse;
        public string? Note { get; set; }
    }

    public class UpdateCardRequest
    {
        public CardTargetType TargetType { get; set; } = CardTargetType.Person;
        public string? ClientId { get; set; }
        public string? VehicleId { get; set; }
        public CardStatus Status { get; set; } = CardStatus.InUse;
        public string? Note { get; set; }
    }
}
