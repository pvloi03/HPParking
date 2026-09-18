using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    public class ClientService : IClientService
    {
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly IRepository<Device> _deviceRepo;
        private readonly IFileStorageService _fileStorage;
        private readonly IFaceIdService _faceIdService;
        private readonly ILogger<ClientService> _logger;

        public ClientService(
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<Lane> laneRepo,
            IRepository<Device> deviceRepo,
            IFileStorageService fileStorage,
            IFaceIdService faceIdService,
            ILogger<ClientService> logger)
        {
            _clientRepo = clientRepo;
            _vehicleRepo = vehicleRepo;
            _laneRepo = laneRepo;
            _deviceRepo = deviceRepo;
            _fileStorage = fileStorage;
            _faceIdService = faceIdService;
            _logger = logger;
        }

        public async Task<PagedResult<ClientDto>> GetClientsPagedAsync(ClientFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<Client>.Filter;
            var filters = new List<FilterDefinition<Client>>();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = query.Keyword.Trim();
                var regex = new BsonRegularExpression(kw, "i");
                filters.Add(builder.Or(
                    builder.Regex(x => x.Name, regex),
                    builder.Regex(x => x.PhoneNumber, regex),
                    builder.Regex(x => x.Code, regex)
                ));
            }

            if (query.Type.HasValue)
            {
                filters.Add(builder.Eq(x => x.Type, query.Type.Value));
            }

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(x => x.IsActive, query.IsActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.CompanyId))
            {
                filters.Add(builder.Eq(x => x.CompanyId, query.CompanyId));
            }

            if (!string.IsNullOrWhiteSpace(query.DepartmentId))
            {
                filters.Add(builder.Eq(x => x.DepartmentId, query.DepartmentId));
            }

            if (!string.IsNullOrWhiteSpace(query.ContractorId))
            {
                filters.Add(builder.Eq(x => x.ContractorId, query.ContractorId));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Client>.Sort.Ascending(x => x.CreatedAt)
                : Builders<Client>.Sort.Descending(x => x.CreatedAt);

            var totalCount = await _clientRepo.CountAsync(filter, cancellationToken);
            var clients = await _clientRepo.FindAsync(filter, sort, query.Skip, query.PageSize, cancellationToken);

            var dtos = clients.Adapt<List<ClientDto>>();
            return new PagedResult<ClientDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<ClientDetailDto> GetClientByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(id, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin khách hàng.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var vehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == id && !v.IsDeleted, cancellationToken);
            var vehicleDtos = vehicles.Adapt<List<VehicleDto>>();

            var detailDto = client.Adapt<ClientDetailDto>();
            detailDto.Vehicles = vehicleDtos;
            return detailDto;
        }

        public async Task<ClientDetailDto> CreateClientAsync(CreateClientRequest request, CancellationToken cancellationToken = default)
        {
            var cleanPhone = request.PhoneNumber.Trim();

            // 1. Kiểm tra tính duy nhất của Số điện thoại
            var existingPhone = await _clientRepo.FindOneAsync(
                c => c.PhoneNumber == cleanPhone && !c.IsDeleted,
                cancellationToken);

            if (existingPhone != null)
            {
                throw new ConflictException(
                    $"Số điện thoại '{cleanPhone}' đã được sử dụng bởi khách hàng khác ({existingPhone.Name}).",
                    ErrorCodes.CLIENT_PHONE_DUPLICATE);
            }

            // 2. Kiểm tra tính duy nhất của CCCD (nếu có nhập)
            var cleanCode = request.Code?.Trim();
            if (!string.IsNullOrWhiteSpace(cleanCode))
            {
                var existingCode = await _clientRepo.FindOneAsync(
                    c => c.Code == cleanCode && !c.IsDeleted,
                    cancellationToken);

                if (existingCode != null)
                {
                    throw new ConflictException(
                        $"Mã CCCD/Định danh '{cleanCode}' đã tồn tại trong hệ thống ({existingCode.Name}).",
                        ErrorCodes.CLIENT_CODE_DUPLICATE);
                }
            }

            // 3. Kiểm tra tính duy nhất của danh sách phương tiện đính kèm ban đầu
            var normalizedVehicles = new List<CreateVehicleRequest>();
            if (request.Vehicles != null && request.Vehicles.Count > 0)
            {
                foreach (var v in request.Vehicles)
                {
                    var normPlate = PlateHelper.Normalize(v.PlateNumber);
                    var existingVehicle = await _vehicleRepo.FindOneAsync(
                        x => x.PlateNumber == normPlate && x.IsActive && !x.IsDeleted,
                        cancellationToken);

                    if (existingVehicle != null)
                    {
                        throw new ConflictException(
                            $"Biển số xe '{normPlate}' đã được đăng ký và đang hoạt động cho một khách hàng khác.",
                            ErrorCodes.VEHICLE_PLATE_DUPLICATE);
                    }

                    normalizedVehicles.Add(new CreateVehicleRequest
                    {
                        PlateNumber = normPlate,
                        Type = v.Type,
                        IsActive = v.IsActive,
                        Note = v.Note
                    });
                }
            }

            // 4. Tạo thực thể Client
            var client = new Client
            {
                Code = cleanCode ?? string.Empty,
                Name = request.Name.Trim(),
                BirthDay = request.BirthDay,
                Address = request.Address?.Trim() ?? string.Empty,
                CompanyId = request.CompanyId,
                DepartmentId = request.DepartmentId,
                ContractorId = request.ContractorId,
                Type = request.Type,
                Email = request.Email?.Trim(),
                Gender = request.Gender,
                PhoneNumber = cleanPhone,
                IsActive = request.IsActive,
                Expired = request.Expired ?? new(),
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            await _clientRepo.AddAsync(client, cancellationToken);
            _logger.LogInformation("Đã tạo mới khách hàng: {Name} ({Phone}) - ID: {Id}", client.Name, cleanPhone, client.Id);

            // 5. Thêm các phương tiện đi kèm nếu có
            var vehicleDtos = new List<VehicleDto>();
            foreach (var reqV in normalizedVehicles)
            {
                var vehicle = new Vehicle
                {
                    PlateNumber = reqV.PlateNumber,
                    Type = reqV.Type,
                    OwnerClientId = client.Id,
                    IsActive = reqV.IsActive,
                    Note = reqV.Note,
                    CreatedAt = DateTime.UtcNow
                };
                await _vehicleRepo.AddAsync(vehicle, cancellationToken);
                vehicleDtos.Add(vehicle.Adapt<VehicleDto>());
            }

            var detailDto = client.Adapt<ClientDetailDto>();
            detailDto.Vehicles = vehicleDtos;
            return detailDto;
        }

        public async Task<ClientDto> UpdateClientAsync(string id, UpdateClientRequest request, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(id, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin khách hàng cần cập nhật.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var cleanPhone = request.PhoneNumber.Trim();
            if (!string.Equals(client.PhoneNumber, cleanPhone, StringComparison.OrdinalIgnoreCase))
            {
                var existingPhone = await _clientRepo.FindOneAsync(
                    c => c.PhoneNumber == cleanPhone && c.Id != id && !c.IsDeleted,
                    cancellationToken);

                if (existingPhone != null)
                {
                    throw new ConflictException(
                        $"Số điện thoại '{cleanPhone}' đã được sử dụng bởi khách hàng khác ({existingPhone.Name}).",
                        ErrorCodes.CLIENT_PHONE_DUPLICATE);
                }
            }

            var cleanCode = request.Code?.Trim();
            if (!string.IsNullOrWhiteSpace(cleanCode) && !string.Equals(client.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
            {
                var existingCode = await _clientRepo.FindOneAsync(
                    c => c.Code == cleanCode && c.Id != id && !c.IsDeleted,
                    cancellationToken);

                if (existingCode != null)
                {
                    throw new ConflictException(
                        $"Mã CCCD/Định danh '{cleanCode}' đã tồn tại trong hệ thống ({existingCode.Name}).",
                        ErrorCodes.CLIENT_CODE_DUPLICATE);
                }
            }

            client.Code = cleanCode ?? string.Empty;
            client.Name = request.Name.Trim();
            client.BirthDay = request.BirthDay;
            client.Address = request.Address?.Trim() ?? string.Empty;
            client.CompanyId = request.CompanyId;
            client.DepartmentId = request.DepartmentId;
            client.ContractorId = request.ContractorId;
            client.Type = request.Type;
            client.Email = request.Email?.Trim();
            client.Gender = request.Gender;
            client.PhoneNumber = cleanPhone;
            client.IsActive = request.IsActive;
            client.Expired = request.Expired ?? new();
            client.Note = request.Note;
            client.UpdatedAt = DateTime.UtcNow;

            await _clientRepo.UpdateAsync(client, cancellationToken);
            _logger.LogInformation("Đã cập nhật khách hàng ID {Id}: {Name}", id, client.Name);

            return client.Adapt<ClientDto>();
        }

        public async Task<bool> DeleteClientAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(id, cancellationToken);
            if (client == null || (!hardDelete && client.IsDeleted))
            {
                throw new NotFoundException("Không tìm thấy khách hàng cần xóa.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var vehicles = await _vehicleRepo.FindAsync(
                v => v.OwnerClientId == id && (!hardDelete ? !v.IsDeleted : true),
                cancellationToken);

            if (!hardDelete)
            {
                // =========================================================================
                // XÓA MỀM (Soft Delete - Mặc định):
                // 1. Đánh dấu xóa mềm Client
                // 2. Đánh dấu xóa mềm toàn bộ xe thuộc Client
                // 3. BẢO LƯU 100% DỮ LIỆU FACEID: Tuyệt đối không xóa trên thiết bị FaceID
                // =========================================================================
                await _clientRepo.DeleteAsync(id, softDelete: true, cancellationToken);

                foreach (var v in vehicles)
                {
                    await _vehicleRepo.DeleteAsync(v.Id, softDelete: true, cancellationToken);
                }

                _logger.LogInformation("Đã XÓA MỀM khách hàng {Id} và {VehicleCount} xe liên kết (bảo lưu FaceID).", id, vehicles.Count);
                return true;
            }

            // =========================================================================
            // XÓA CỨNG (Hard Delete):
            // 1. Xóa vĩnh viễn Client và xe khỏi CSDL MongoDB
            // 2. Xóa tệp Avatar vật lý trên đĩa
            // 3. Phát lệnh thu hồi (Delete Card & Delete User) trên toàn bộ FaceID active
            // =========================================================================
            await _clientRepo.DeleteAsync(id, softDelete: false, cancellationToken);

            foreach (var v in vehicles)
            {
                await _vehicleRepo.DeleteAsync(v.Id, softDelete: false, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(client.Avatar))
            {
                await _fileStorage.DeleteFileAsync(client.Avatar, cancellationToken);
            }

            // Thu hồi FaceID trên các thiết bị active
            try
            {
                var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
                foreach (var terminal in terminals)
                {
                    await _faceIdService.DeleteUserAsync(terminal, client.Code, client.PhoneNumber, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi thu hồi FaceID khi xóa cứng khách hàng {Id}: {Message}", id, ex.Message);
            }

            _logger.LogInformation("Đã XÓA CỨNG khách hàng {Id} và gửi lệnh thu hồi quyền FaceID.", id);
            return true;
        }

        public async Task<string> UploadAvatarAsync(string id, IFormFile file, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(id, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin khách hàng để nạp ảnh đại diện.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var baseFileName = !string.IsNullOrWhiteSpace(client.Code)
                ? client.Code.Trim()
                : client.PhoneNumber.Trim();

            var avatarUrl = await _fileStorage.SaveAvatarAsync(file, baseFileName, cancellationToken);
            client.Avatar = avatarUrl;
            client.UpdatedAt = DateTime.UtcNow;

            await _clientRepo.UpdateAsync(client, cancellationToken);
            _logger.LogInformation("Đã cập nhật Avatar cho khách hàng {Id} ({Name}) -> {AvatarUrl}", id, client.Name, avatarUrl);

            return avatarUrl;
        }

        public async Task<SyncFaceIdResponse> SyncFaceIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(id, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin khách hàng để đồng bộ FaceID.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
            var response = new SyncFaceIdResponse
            {
                ClientId = client.Id,
                ClientName = client.Name,
                TotalDevices = terminals.Count
            };

            if (terminals.Count == 0)
            {
                _logger.LogWarning("Không tìm thấy thiết bị FaceID nào đang hoạt động trên các làn xe.");
                return response;
            }

            byte[]? faceBytes = null;
            if (!string.IsNullOrWhiteSpace(client.Avatar))
            {
                try
                {
                    faceBytes = await _fileStorage.ReadFileBytesAsync(client.Avatar, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không đọc được tệp ảnh đại diện của khách hàng {Id}: {Message}", id, ex.Message);
                }
            }

            foreach (var terminal in terminals)
            {
                var result = await _faceIdService.PushUserAsync(
                    terminal,
                    client.Code,
                    client.Name,
                    client.Gender == 1,
                    client.PhoneNumber,
                    faceBytes,
                    cancellationToken);

                response.Results.Add(result);
                if (result.IsSuccess)
                {
                    response.SuccessCount++;
                }
                else
                {
                    response.FailureCount++;
                }
            }

            _logger.LogInformation(
                "Đồng bộ FaceID cho khách hàng {Name} hoàn tất: {Success}/{Total} thiết bị thành công.",
                client.Name, response.SuccessCount, response.TotalDevices);

            return response;
        }

        private async Task<List<FaceIdTerminalConfig>> ResolveActiveFaceIdTerminalsAsync(CancellationToken cancellationToken)
        {
            var lanes = await _laneRepo.FindAsync(l => l.IsActive && !l.IsDeleted, cancellationToken);
            var faceDeviceIds = lanes
                .Where(l => !string.IsNullOrWhiteSpace(l.FaceDeviceId))
                .Select(l => l.FaceDeviceId!)
                .Distinct()
                .ToList();

            if (faceDeviceIds.Count == 0) return new List<FaceIdTerminalConfig>();

            var devices = await _deviceRepo.FindAsync(
                d => faceDeviceIds.Contains(d.Id) && d.IsActive && !d.IsDeleted,
                cancellationToken);

            var deviceMap = devices
                .GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var uniqueConfigs = new List<FaceIdTerminalConfig>();
            var seenIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var lane in lanes)
            {
                if (string.IsNullOrWhiteSpace(lane.FaceDeviceId)) continue;
                if (!deviceMap.TryGetValue(lane.FaceDeviceId, out var device)) continue;
                if (string.IsNullOrWhiteSpace(device.IpAddress)) continue;

                var ip = device.IpAddress.Trim();
                if (seenIps.Add(ip))
                {
                    uniqueConfigs.Add(new FaceIdTerminalConfig
                    {
                        DeviceIp = ip,
                        DeviceName = string.IsNullOrWhiteSpace(device.Name) ? lane.Name : device.Name,
                        Username = string.IsNullOrWhiteSpace(device.UserName) ? "admin" : device.UserName.Trim(),
                        Password = device.Password?.Trim() ?? string.Empty
                    });
                }
            }

            return uniqueConfigs;
        }
    }
}
