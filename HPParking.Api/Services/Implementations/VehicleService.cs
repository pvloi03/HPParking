using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    public class VehicleService : IVehicleService
    {
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly ILogger<VehicleService> _logger;

        public VehicleService(
            IRepository<Vehicle> vehicleRepo,
            IRepository<Client> clientRepo,
            ILogger<VehicleService> logger)
        {
            _vehicleRepo = vehicleRepo;
            _clientRepo = clientRepo;
            _logger = logger;
        }

        public async Task<PagedResult<VehicleDto>> GetVehiclesPagedAsync(VehicleFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<Vehicle>.Filter;
            var filters = new List<FilterDefinition<Vehicle>>();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var normalizedKeyword = PlateHelper.Normalize(query.Keyword);
                filters.Add(builder.Regex(x => x.PlateNumber, new BsonRegularExpression(normalizedKeyword, "i")));
            }

            if (query.Type.HasValue)
            {
                filters.Add(builder.Eq(x => x.Type, query.Type.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.OwnerClientId))
            {
                filters.Add(builder.Eq(x => x.OwnerClientId, query.OwnerClientId));
            }

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(x => x.IsActive, query.IsActive.Value));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Vehicle>.Sort.Ascending(x => x.CreatedAt)
                : Builders<Vehicle>.Sort.Descending(x => x.CreatedAt);

            var totalCount = await _vehicleRepo.CountAsync(filter, cancellationToken);
            var vehicles = await _vehicleRepo.FindAsync(filter, sort, query.Skip, query.PageSize, cancellationToken);

            var dtos = vehicles.Adapt<List<VehicleDto>>();
            return new PagedResult<VehicleDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<IReadOnlyList<VehicleDto>> GetVehiclesByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(clientId, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy khách hàng với Id đã chỉ định.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var vehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == clientId && !v.IsDeleted, cancellationToken);
            return vehicles.Adapt<List<VehicleDto>>();
        }

        public async Task<VehicleDto> GetVehicleByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var vehicle = await _vehicleRepo.GetByIdAsync(id, cancellationToken);
            if (vehicle == null || vehicle.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy phương tiện với Id đã chỉ định.", ErrorCodes.VEHICLE_NOT_FOUND);
            }

            return vehicle.Adapt<VehicleDto>();
        }

        public async Task<VehicleDto> CreateVehicleAsync(string clientId, CreateVehicleRequest request, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(clientId, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy khách hàng để gắn phương tiện.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var normalizedPlate = PlateHelper.Normalize(request.PlateNumber);

            // Kiểm tra trùng lặp biển số đang hoạt động
            var existing = await _vehicleRepo.FindOneAsync(
                v => v.PlateNumber == normalizedPlate && v.IsActive && !v.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Biển số xe '{normalizedPlate}' đã được đăng ký và đang hoạt động trong hệ thống.",
                    ErrorCodes.VEHICLE_PLATE_DUPLICATE);
            }

            var vehicle = new Vehicle
            {
                PlateNumber = normalizedPlate,
                Type = request.Type,
                OwnerClientId = clientId,
                IsActive = request.IsActive,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            await _vehicleRepo.AddAsync(vehicle, cancellationToken);
            _logger.LogInformation("Đã thêm phương tiện mới: {PlateNumber} cho khách hàng {ClientId}", normalizedPlate, clientId);

            return vehicle.Adapt<VehicleDto>();
        }

        public async Task<VehicleDto> UpdateVehicleAsync(string id, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
        {
            var vehicle = await _vehicleRepo.GetByIdAsync(id, cancellationToken);
            if (vehicle == null || vehicle.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy phương tiện cần cập nhật.", ErrorCodes.VEHICLE_NOT_FOUND);
            }

            var normalizedPlate = PlateHelper.Normalize(request.PlateNumber);

            // Nếu biển số thay đổi, kiểm tra tính duy nhất
            if (!string.Equals(vehicle.PlateNumber, normalizedPlate, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _vehicleRepo.FindOneAsync(
                    v => v.PlateNumber == normalizedPlate && v.Id != id && v.IsActive && !v.IsDeleted,
                    cancellationToken);

                if (existing != null)
                {
                    throw new ConflictException(
                        $"Biển số xe '{normalizedPlate}' đã thuộc về một phương tiện đang hoạt động khác.",
                        ErrorCodes.VEHICLE_PLATE_DUPLICATE);
                }
            }

            vehicle.PlateNumber = normalizedPlate;
            vehicle.Type = request.Type;
            vehicle.IsActive = request.IsActive;
            vehicle.Note = request.Note;
            vehicle.UpdatedAt = DateTime.UtcNow;

            await _vehicleRepo.UpdateAsync(vehicle, cancellationToken);
            _logger.LogInformation("Đã cập nhật phương tiện {Id}: {PlateNumber}", id, normalizedPlate);

            return vehicle.Adapt<VehicleDto>();
        }

        public async Task<bool> DeleteVehicleAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var vehicle = await _vehicleRepo.GetByIdAsync(id, cancellationToken);
            if (vehicle == null || (!hardDelete && vehicle.IsDeleted))
            {
                throw new NotFoundException("Không tìm thấy phương tiện cần xóa.", ErrorCodes.VEHICLE_NOT_FOUND);
            }

            if (hardDelete)
            {
                await _vehicleRepo.DeleteAsync(id, softDelete: false, cancellationToken);
                _logger.LogInformation("Đã XÓA CỨNG phương tiện {Id} khỏi CSDL.", id);
            }
            else
            {
                await _vehicleRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã XÓA MỀM phương tiện {Id}.", id);
            }

            return true;
        }
    }
}
