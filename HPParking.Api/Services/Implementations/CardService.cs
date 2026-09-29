using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Api.Services.Implementations
{
    public class CardService : ICardService
    {
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;

        public CardService(
            IRepository<Card> cardRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo)
        {
            _cardRepo = cardRepo;
            _clientRepo = clientRepo;
            _vehicleRepo = vehicleRepo;
        }

        public async Task<PagedResult<CardDto>> GetCardsPagedAsync(
            PaginationQuery query, 
            string? search = null, 
            CardTargetType? targetType = null, 
            CardStatus? status = null, 
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<Card>.Filter.Eq(x => x.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchFilter = Builders<Card>.Filter.Regex(x => x.CardNumber, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i"));
                filter &= searchFilter;
            }

            if (targetType.HasValue)
            {
                filter &= Builders<Card>.Filter.Eq(x => x.TargetType, targetType.Value);
            }

            if (status.HasValue)
            {
                filter &= Builders<Card>.Filter.Eq(x => x.Status, status.Value);
            }

            var allCards = await _cardRepo.FindAsync(filter);
            var cardList = allCards.OrderByDescending(x => x.CreatedAt).ToList();

            var totalItems = cardList.Count;
            var pagedItems = cardList
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var dtoList = new List<CardDto>();
            foreach (var card in pagedItems)
            {
                var dto = await MapToDtoAsync(card);
                dtoList.Add(dto);
            }

            return new PagedResult<CardDto>(dtoList, totalItems, query.PageIndex, query.PageSize);
        }

        public async Task<CardDto> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepo.GetByIdAsync(id) 
                ?? throw new NotFoundException($"Không tìm thấy thẻ với ID: {id}");

            if (card.IsDeleted)
                throw new NotFoundException($"Thẻ ID: {id} đã bị xóa.");

            return await MapToDtoAsync(card);
        }

        public async Task<CardDto> CreateAsync(CreateCardRequest request, CancellationToken cancellationToken = default)
        {
            string normalizedCard = CardHelper.NormalizeCardCode(request.CardNumber);
            if (string.IsNullOrWhiteSpace(normalizedCard))
                throw new BadRequestException("Mã thẻ không được để trống.");

            var existing = await _cardRepo.FindOneAsync(x => x.CardNumber == normalizedCard && !x.IsDeleted);
            if (existing != null)
                throw new ConflictException($"Mã thẻ {normalizedCard} đã tồn tại trong hệ thống.");

            var card = new Card
            {
                CardNumber = normalizedCard,
                TargetType = request.TargetType,
                ClientId = request.TargetType == CardTargetType.Person ? request.ClientId : null,
                VehicleId = request.TargetType == CardTargetType.Vehicle ? request.VehicleId : null,
                Status = request.Status,
                Note = request.Note
            };

            await _cardRepo.AddAsync(card, cancellationToken);
            return await MapToDtoAsync(card);
        }

        public async Task<CardDto> UpdateAsync(string id, UpdateCardRequest request, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepo.GetByIdAsync(id, cancellationToken) 
                ?? throw new NotFoundException($"Không tìm thấy thẻ với ID: {id}");

            if (card.IsDeleted)
                throw new NotFoundException($"Thẻ ID: {id} đã bị xóa.");

            card.TargetType = request.TargetType;
            card.ClientId = request.TargetType == CardTargetType.Person ? request.ClientId : null;
            card.VehicleId = request.TargetType == CardTargetType.Vehicle ? request.VehicleId : null;
            card.Status = request.Status;
            card.Note = request.Note;

            await _cardRepo.UpdateAsync(card, cancellationToken);
            return await MapToDtoAsync(card);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepo.GetByIdAsync(id, cancellationToken) 
                ?? throw new NotFoundException($"Không tìm thấy thẻ với ID: {id}");

            await _cardRepo.DeleteAsync(id, softDelete: true, cancellationToken: cancellationToken);
        }

        private async Task<CardDto> MapToDtoAsync(Card card)
        {
            string? clientName = null;
            if (!string.IsNullOrWhiteSpace(card.ClientId))
            {
                var client = await _clientRepo.GetByIdAsync(card.ClientId);
                clientName = client?.Name;
            }

            string? plateNumber = null;
            if (!string.IsNullOrWhiteSpace(card.VehicleId))
            {
                var vehicle = await _vehicleRepo.GetByIdAsync(card.VehicleId);
                plateNumber = vehicle?.PlateNumber;
            }

            return new CardDto
            {
                Id = card.Id,
                CardNumber = card.CardNumber,
                TargetType = card.TargetType,
                ClientId = card.ClientId,
                ClientName = clientName,
                VehicleId = card.VehicleId,
                PlateNumber = plateNumber,
                Status = card.Status,
                Note = card.Note,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt
            };
        }
    }
}
