using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace HPParking.Api.Services.Interfaces
{
    public interface IClientService
    {
        Task<PagedResult<ClientDto>> GetClientsPagedAsync(ClientFilterQuery query, CancellationToken cancellationToken = default);
        Task<ClientDetailDto> GetClientByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<ClientDetailDto> CreateClientAsync(CreateClientRequest request, CancellationToken cancellationToken = default);
        Task<ClientDto> UpdateClientAsync(string id, UpdateClientRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteClientAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default);
        Task<ClientDto> RestoreClientAsync(string id, CancellationToken cancellationToken = default);
        Task<string> UploadAvatarAsync(string id, IFormFile file, CancellationToken cancellationToken = default);
        Task<SyncFaceIdResponse> SyncFaceIdAsync(string id, CancellationToken cancellationToken = default);
        Task<(byte[] Bytes, string ContentType)> GetAvatarAsync(string id, CancellationToken cancellationToken = default);
    }
}
