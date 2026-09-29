using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Api.Services.Interfaces
{
    public interface ICardService
    {
        Task<PagedResult<CardDto>> GetCardsPagedAsync(PaginationQuery query, string? search = null, CardTargetType? targetType = null, CardStatus? status = null, CancellationToken cancellationToken = default);
        Task<CardDto> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<CardDto> CreateAsync(CreateCardRequest request, CancellationToken cancellationToken = default);
        Task<CardDto> UpdateAsync(string id, UpdateCardRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
