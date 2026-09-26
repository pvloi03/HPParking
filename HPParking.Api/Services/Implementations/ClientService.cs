using HPParking.Api.Common.Exceptions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
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
        private readonly IRepository<Company>? _companyRepo;
        private readonly IRepository<Department>? _departmentRepo;
        private readonly IRepository<Contractor>? _contractorRepo;
        private readonly IFileStorageService _fileStorage;
        private readonly IFaceIdService _faceIdService;
        private readonly IAuditLogService? _auditLogService;
        private readonly ILogger<ClientService> _logger;

        public ClientService(
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<Lane> laneRepo,
            IRepository<Device> deviceRepo,
            IFileStorageService fileStorage,
            IFaceIdService faceIdService,
            ILogger<ClientService> logger,
            IRepository<Company>? companyRepo = null,
            IRepository<Department>? departmentRepo = null,
            IRepository<Contractor>? contractorRepo = null,
            IAuditLogService? auditLogService = null)
        {
            _clientRepo = clientRepo;
            _vehicleRepo = vehicleRepo;
            _laneRepo = laneRepo;
            _deviceRepo = deviceRepo;
            _fileStorage = fileStorage;
            _faceIdService = faceIdService;
            _logger = logger;
            _companyRepo = companyRepo;
            _departmentRepo = departmentRepo;
            _contractorRepo = contractorRepo;
            _auditLogService = auditLogService;
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

            var totalCount = await _clientRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var clients = await _clientRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

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

            // Kiểm tra live trạng thái FaceID của khách hàng trên các làn xe đang hoạt động
            var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
            if (terminals.Count > 0)
            {
                var tasks = terminals.Select(t => _faceIdService.CheckUserStatusAsync(t, client.Code, cancellationToken));
                var statuses = await Task.WhenAll(tasks);
                detailDto.FaceIdTerminals = [.. statuses];
            }

            return detailDto;
        }

        public async Task<ClientFaceIdStatusResponse> CheckFaceIdStatusAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(id, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin khách hàng.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
            var response = new ClientFaceIdStatusResponse
            {
                ClientId = client.Id,
                ClientCode = client.Code,
                ClientName = client.Name,
                TotalDevices = terminals.Count
            };

            if (terminals.Count == 0)
            {
                return response;
            }

            var tasks = terminals.Select(t => _faceIdService.CheckUserStatusAsync(t, client.Code, cancellationToken));
            var statuses = await Task.WhenAll(tasks);

            response.Terminals = [.. statuses];
            response.OnlineDevices = statuses.Count(s => s.IsOnline);
            response.EnrolledFaceDevices = statuses.Count(s => s.HasFace);

            return response;
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

            // 2. Kiểm tra tính duy nhất của CCCD (bắt buộc)
            var cleanCode = request.Code.Trim();
            var existingCode = await _clientRepo.FindOneAsync(
                c => c.Code == cleanCode && !c.IsDeleted,
                cancellationToken);

            if (existingCode != null)
            {
                throw new ConflictException(
                    $"Mã CCCD/Định danh '{cleanCode}' đã tồn tại trong hệ thống ({existingCode.Name}).",
                    ErrorCodes.CLIENT_CODE_DUPLICATE);
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

            // 4. Kiểm tra tính hợp lệ nếu gắn trực thuộc Nhà thầu (ADR 0033)
            if (!string.IsNullOrWhiteSpace(request.ContractorId) && _contractorRepo != null)
            {
                var contractor = await _contractorRepo.GetByIdAsync(request.ContractorId, cancellationToken);
                if (contractor == null || contractor.IsDeleted)
                {
                    throw new NotFoundException($"Không tìm thấy nhà thầu với Id '{request.ContractorId}'.", ErrorCodes.CONTRACTOR_NOT_FOUND);
                }
                if (!contractor.IsActive)
                {
                    throw new BadRequestException($"Nhà thầu '{contractor.Name}' đang bị vô hiệu hóa, không thể gán nhân sự trực thuộc.", ErrorCodes.CONTRACTOR_INACTIVE);
                }
            }

            // 5. Tạo thực thể Client
            var client = new Client
            {
                Code = cleanCode,
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

            // 6. Tự động nạp thông tin Khách hàng (User & Thẻ) lên toàn bộ FaceID active
            try
            {
                var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
                if (terminals.Count > 0)
                {
                    var pushResults = await ExecuteParallelFaceIdActionAsync(
                        terminals,
                        t => _faceIdService.PushUserAsync(
                            t,
                            client.Code,
                            client.Name,
                            client.Gender == 1,
                            client.PhoneNumber,
                            null,
                            cancellationToken),
                        cancellationToken);

                    detailDto.FaceIdTerminals = pushResults.Select(r => new TerminalClientStatusDto
                    {
                        DeviceIp = r.DeviceIp,
                        DeviceName = r.DeviceName,
                        IsOnline = !r.ErrorMessage?.Contains("Mất kết nối") ?? true,
                        UserExists = r.IsSuccess,
                        HasFace = false,
                        CardCount = r.IsSuccess ? 1 : 0,
                        Cards = r.IsSuccess ? new List<string> { client.PhoneNumber } : new(),
                        ErrorMessage = r.IsSuccess ? null : r.ErrorMessage,
                        Timestamp = r.Timestamp
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi nạp FaceID tự động khi tạo khách hàng {Id}: {Message}", client.Id, ex.Message);
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    HPParking.Core.Models.Enums.AuditActionType.Create,
                    "Client",
                    client.Id,
                    client.Name,
                    reason: $"Tạo mới hồ sơ khách hàng '{client.Name}' (Mã định danh: {client.Code}).",
                    cancellationToken: cancellationToken);
            }

            return detailDto;
        }

        public async Task<ClientDetailDto> UpdateClientAsync(string id, UpdateClientRequest request, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(id, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin khách hàng cần cập nhật.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var oldClientDto = client.Adapt<ClientDto>();

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

            var cleanCode = request.Code.Trim();
            if (!string.Equals(client.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
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

            // Kiểm tra tính hợp lệ nếu cập nhật Nhà thầu trực thuộc (ADR 0033)
            if (!string.IsNullOrWhiteSpace(request.ContractorId) && _contractorRepo != null)
            {
                var contractor = await _contractorRepo.GetByIdAsync(request.ContractorId, cancellationToken);
                if (contractor == null || contractor.IsDeleted)
                {
                    throw new NotFoundException($"Không tìm thấy nhà thầu với Id '{request.ContractorId}'.", ErrorCodes.CONTRACTOR_NOT_FOUND);
                }
                if (!contractor.IsActive)
                {
                    throw new BadRequestException($"Nhà thầu '{contractor.Name}' đang bị vô hiệu hóa, không thể gán nhân sự trực thuộc.", ErrorCodes.CONTRACTOR_INACTIVE);
                }
            }

            // 4. Kiểm tra tính duy nhất của danh sách phương tiện bổ sung (nếu có)
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

            var oldCode = client.Code;
            var oldPhone = client.PhoneNumber;
            var isCodeChanged = !string.Equals(client.Code, cleanCode, StringComparison.OrdinalIgnoreCase);
            var isPhoneChanged = !string.Equals(client.PhoneNumber, cleanPhone, StringComparison.OrdinalIgnoreCase);
            var isNameOrGenderChanged = !string.Equals(client.Name, request.Name.Trim(), StringComparison.Ordinal) || client.Gender != request.Gender;

            client.Code = cleanCode;
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

            // Thêm các phương tiện bổ sung vào CSDL
            foreach (var reqV in normalizedVehicles)
            {
                var vehicle = new Vehicle
                {
                    PlateNumber = reqV.PlateNumber,
                    Type = reqV.Type,
                    OwnerClientId = id,
                    IsActive = reqV.IsActive,
                    Note = reqV.Note,
                    CreatedAt = DateTime.UtcNow
                };
                await _vehicleRepo.AddAsync(vehicle, cancellationToken);
            }

            var detailDto = client.Adapt<ClientDetailDto>();
            var vehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == id && !v.IsDeleted, cancellationToken);
            detailDto.Vehicles = vehicles.Adapt<List<VehicleDto>>();

            // Tự động đồng bộ các thay đổi lên các FaceID active song song
            try
            {
                var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
                if (terminals.Count > 0)
                {
                    List<FaceIdTerminalResultDto> faceResults = new();
                    if (isCodeChanged)
                    {
                        // 1. Đổi CCCD/Code: Vì employeeNo là khóa chính trên FaceID không thể sửa đổi,
                        // ta xóa User cũ và nạp lại User mới (kèm Avatar nếu có)
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

                        faceResults = await ExecuteParallelFaceIdActionAsync(
                            terminals,
                            async t =>
                            {
                                await _faceIdService.DeleteUserAsync(t, oldCode, oldPhone, cancellationToken);
                                return await _faceIdService.PushUserAsync(
                                    t,
                                    client.Code,
                                    client.Name,
                                    client.Gender == 1,
                                    client.PhoneNumber,
                                    faceBytes,
                                    cancellationToken);
                            },
                            cancellationToken);
                    }
                    else if (isPhoneChanged || isNameOrGenderChanged)
                    {
                        // 2. Không đổi CCCD:
                        // - Nếu đổi SĐT: Self-Healing quét sạch thẻ cũ và gán thẻ mới
                        // - Nếu đổi Tên hoặc Giới tính: Cập nhật UserInfo
                        faceResults = await ExecuteParallelFaceIdActionAsync(
                            terminals,
                            async t =>
                            {
                                if (isPhoneChanged)
                                {
                                    var cardRes = await _faceIdService.CleanAndAssignCardAsync(t, client.Code, client.PhoneNumber, cancellationToken);
                                    if (!cardRes.IsSuccess) return cardRes;
                                }

                                if (isNameOrGenderChanged)
                                {
                                    return await _faceIdService.UpdateUserInfoAsync(t, client.Code, client.Name, client.Gender == 1, cancellationToken);
                                }

                                return new FaceIdTerminalResultDto
                                {
                                    DeviceIp = t.DeviceIp,
                                    DeviceName = t.DeviceName,
                                    IsSuccess = true
                                };
                            },
                            cancellationToken);
                    }
                    else
                    {
                        // 3. Không đổi các trường ảnh hưởng FaceID (chỉ đổi địa chỉ, email, ghi chú...):
                        // Truy vấn nhanh trạng thái hiện tại để trả về thông tin đầy đủ cho client
                        var statusTasks = terminals.Select(t => _faceIdService.CheckUserStatusAsync(t, client.Code, cancellationToken));
                        var statuses = await Task.WhenAll(statusTasks);
                        detailDto.FaceIdTerminals = [.. statuses];
                    }

                    if (faceResults.Count > 0)
                    {
                        detailDto.FaceIdTerminals = [.. faceResults.Select(r => new TerminalClientStatusDto
                        {
                            DeviceIp = r.DeviceIp,
                            DeviceName = r.DeviceName,
                            IsOnline = !r.ErrorMessage?.Contains("Mất kết nối") ?? true,
                            UserExists = r.IsSuccess,
                            HasFace = false,
                            CardCount = r.IsSuccess ? 1 : 0,
                            Cards = r.IsSuccess ? new List<string> { client.PhoneNumber } : new(),
                            ErrorMessage = r.IsSuccess ? null : r.ErrorMessage,
                            Timestamp = r.Timestamp
                        })];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi đồng bộ FaceID khi cập nhật khách hàng {Id}: {Message}", id, ex.Message);
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    HPParking.Core.Models.Enums.AuditActionType.Update,
                    "Client",
                    client.Id,
                    client.Name,
                    reason: $"Cập nhật hồ sơ khách hàng '{client.Name}' (CCCD: {client.Code}).",
                    cancellationToken: cancellationToken);
            }

            return detailDto;
        }

        public async Task<bool> DeleteClientAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(id, cancellationToken)
                ?? (hardDelete ? await _clientRepo.GetDeletedByIdAsync(id, cancellationToken) : null);

            if (client == null)
            {
                throw new NotFoundException("Không tìm thấy khách hàng cần xóa.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            // Universal Restrict Deletion Policy (ADR 0030 & ADR 0031):
            // Chặn xóa (409 Conflict) nếu còn bất kỳ phương tiện nào ĐANG HOẠT ĐỘNG (!IsDeleted)
            var activeVehicles = await _vehicleRepo.FindAsync(
                v => v.OwnerClientId == id && !v.IsDeleted,
                cancellationToken);

            if (activeVehicles.Count > 0)
            {
                throw new ConflictException(
                    $"Không thể xóa Khách hàng '{client.Name}' vì vẫn còn {activeVehicles.Count} phương tiện đang hoạt động liên kết. Vui lòng xóa hoặc chuyển quyền sở hữu phương tiện trước.",
                    ErrorCodes.CLIENT_HAS_VEHICLES);
            }

            if (!hardDelete)
            {
                // =========================================================================
                // XÓA MỀM (Soft Delete - Mặc định):
                // 1. Đánh dấu xóa mềm Client
                // 2. BẢO LƯU 100% DỮ LIỆU FACEID: Tuyệt đối không xóa trên thiết bị FaceID
                // =========================================================================
                await _clientRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã XÓA MỀM khách hàng {Id}: {Name} (bảo lưu FaceID).", id, client.Name);

                if (_auditLogService != null)
                {
                    await _auditLogService.LogActivityAsync(
                        HPParking.Core.Models.Enums.AuditActionType.Delete,
                        "Client",
                        client.Id,
                        client.Name,
                        reason: $"Chuyển khách hàng '{client.Name}' vào thùng rác.",
                        cancellationToken: cancellationToken);
                }

                return true;
            }

            // =========================================================================
            // XÓA CỨNG (Hard Delete):
            // 1. Xóa vĩnh viễn Client khỏi CSDL MongoDB
            // 2. Dọn dẹp các phương tiện đã xóa mềm trước đó của Client (nếu có)
            // 3. Xóa tệp Avatar vật lý trên đĩa
            // 4. Phát lệnh thu hồi (Delete Card & Delete User) trên toàn bộ FaceID active
            // =========================================================================
            var deletedVehicles = await _vehicleRepo.FindAsync(
                MongoDB.Driver.Builders<Vehicle>.Filter.Eq(v => v.OwnerClientId, id),
                sort: null,
                skip: 0,
                limit: 0,
                onlyDeleted: true,
                cancellationToken);

            await _clientRepo.DeleteAsync(id, softDelete: false, cancellationToken);

            foreach (var v in deletedVehicles)
            {
                await _vehicleRepo.DeleteAsync(v.Id, softDelete: false, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(client.Avatar))
            {
                await _fileStorage.DeleteFileAsync(client.Avatar, cancellationToken);
            }

            // Thu hồi FaceID trên các thiết bị active song song qua Task.WhenAll
            try
            {
                var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
                if (terminals.Count > 0)
                {
                    await ExecuteParallelFaceIdActionAsync(
                        terminals,
                        t => _faceIdService.DeleteUserAsync(t, client.Code, client.PhoneNumber, cancellationToken),
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi thu hồi FaceID khi xóa cứng khách hàng {Id}: {Message}", id, ex.Message);
            }

            _logger.LogInformation("Đã XÓA CỨNG khách hàng {Id} và gửi lệnh thu hồi quyền FaceID.", id);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    HPParking.Core.Models.Enums.AuditActionType.PermanentDelete,
                    "Client",
                    client.Id,
                    client.Name,
                    reason: $"Xóa vĩnh viễn khách hàng '{client.Name}' khỏi CSDL.",
                    cancellationToken: cancellationToken);
            }

            return true;
        }

        public async Task<ClientDto> RestoreClientAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetDeletedByIdAsync(id, cancellationToken);
            if (client == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin khách hàng trong thùng rác.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            // Strict Parent-First Restore (ADR 0031):
            // Nếu khách hàng thuộc Công ty -> Công ty cha phải đang hoạt động
            if (!string.IsNullOrWhiteSpace(client.CompanyId) && _companyRepo != null)
            {
                var company = await _companyRepo.GetByIdAsync(client.CompanyId, cancellationToken);
                if (company == null || company.IsDeleted)
                {
                    throw new BadRequestException(
                        "Không thể khôi phục khách hàng vì công ty trực thuộc đang nằm trong thùng rác hoặc không tồn tại. Vui lòng khôi phục công ty trước.",
                        ErrorCodes.PARENT_IS_DELETED);
                }
            }

            // Nếu khách hàng thuộc Phòng ban -> Phòng ban cha phải đang hoạt động
            if (!string.IsNullOrWhiteSpace(client.DepartmentId) && _departmentRepo != null)
            {
                var department = await _departmentRepo.GetByIdAsync(client.DepartmentId, cancellationToken);
                if (department == null || department.IsDeleted)
                {
                    throw new BadRequestException(
                        "Không thể khôi phục khách hàng vì phòng ban trực thuộc đang nằm trong thùng rác hoặc không tồn tại. Vui lòng khôi phục phòng ban trước.",
                        ErrorCodes.PARENT_IS_DELETED);
                }
            }

            // Nếu khách hàng thuộc Nhà thầu -> Nhà thầu cha phải đang hoạt động (ADR 0031 & ADR 0033)
            if (!string.IsNullOrWhiteSpace(client.ContractorId) && _contractorRepo != null)
            {
                var contractor = await _contractorRepo.GetByIdAsync(client.ContractorId, cancellationToken);
                if (contractor == null || contractor.IsDeleted)
                {
                    throw new BadRequestException(
                        "Không thể khôi phục khách hàng vì nhà thầu trực thuộc đang nằm trong thùng rác hoặc không tồn tại. Vui lòng khôi phục nhà thầu trước.",
                        ErrorCodes.PARENT_IS_DELETED);
                }
            }

            // Re-validation: Kiểm tra PhoneNumber với các khách hàng đang hoạt động
            var existingPhone = await _clientRepo.FindOneAsync(
                c => c.PhoneNumber == client.PhoneNumber && c.Id != id && !c.IsDeleted,
                cancellationToken);

            if (existingPhone != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục vì số điện thoại '{client.PhoneNumber}' đã được sử dụng bởi khách hàng đang hoạt động '{existingPhone.Name}'.",
                    ErrorCodes.CLIENT_PHONE_DUPLICATE);
            }

            // Re-validation: Kiểm tra Code (nếu có) với các khách hàng đang hoạt động
            if (!string.IsNullOrWhiteSpace(client.Code))
            {
                var existingCode = await _clientRepo.FindOneAsync(
                    c => c.Code == client.Code && c.Id != id && !c.IsDeleted,
                    cancellationToken);

                if (existingCode != null)
                {
                    throw new ConflictException(
                        $"Không thể khôi phục vì mã khách hàng '{client.Code}' đã được sử dụng bởi khách hàng đang hoạt động '{existingCode.Name}'.",
                        ErrorCodes.CLIENT_CODE_DUPLICATE);
                }
            }

            var success = await _clientRepo.RestoreAsync(id, cancellationToken);
            if (!success)
            {
                throw new AppException("Khôi phục khách hàng thất bại.", 500, ErrorCodes.RESTORE_FAILED);
            }

            client.IsDeleted = false;
            client.DeletedAt = null;
            client.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Đã KHÔI PHỤC khách hàng {Id}: {Name} ({Phone}) từ thùng rác.", client.Id, client.Name, client.PhoneNumber);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    HPParking.Core.Models.Enums.AuditActionType.Restore,
                    "Client",
                    client.Id,
                    client.Name,
                    reason: $"Khôi phục khách hàng '{client.Name}' (CCCD: {client.Code}) từ thùng rác.",
                    cancellationToken: cancellationToken);
            }

            return client.Adapt<ClientDto>();
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

            // Tự động đẩy Avatar mới lên toàn bộ FaceID active song song
            try
            {
                var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
                if (terminals.Count > 0)
                {
                    var imageBytes = await _fileStorage.ReadFileBytesAsync(avatarUrl, cancellationToken);
                    var results = await ExecuteParallelFaceIdActionAsync(
                        terminals,
                        t => _faceIdService.UpdateFaceImageAsync(t, client.Code, imageBytes, cancellationToken),
                        cancellationToken);

                    // Kiểm tra xem có thiết bị nào từ chối ảnh do chất lượng khuôn mặt không
                    var qualityError = results.FirstOrDefault(r => !r.IsSuccess &&
                        (r.ErrorMessage?.Contains("không đạt chuẩn") == true ||
                         r.ErrorMessage?.Contains("khuôn mặt") == true));

                    if (qualityError != null)
                    {
                        // Rollback ảnh vừa upload để tránh ảnh rác trên đĩa và trong DB
                        try
                        {
                            await _fileStorage.DeleteFileAsync(avatarUrl, cancellationToken);
                            client.Avatar = string.Empty;
                            await _clientRepo.UpdateAsync(client, cancellationToken);
                        }
                        catch { }

                        throw new BadRequestException(
                            qualityError.ErrorMessage ?? "Ảnh khuôn mặt không đạt tiêu chuẩn của thiết bị FaceID (ảnh mờ hoặc không nhận diện rõ). Vui lòng chọn ảnh khác.",
                            ErrorCodes.FACEID_IMAGE_REJECTED);
                    }
                }
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi đẩy avatar lên FaceID cho khách hàng {Id}: {Message}", id, ex.Message);
            }

            return avatarUrl;
        }

        public async Task<(byte[] Bytes, string ContentType)> GetAvatarAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(id, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin khách hàng.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            if (string.IsNullOrWhiteSpace(client.Avatar))
            {
                throw new NotFoundException("Khách hàng chưa có ảnh đại diện.", ErrorCodes.NOT_FOUND);
            }

            var bytes = await _fileStorage.ReadFileBytesAsync(client.Avatar, cancellationToken);
            var extension = System.IO.Path.GetExtension(client.Avatar).ToLowerInvariant();
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            return (bytes, contentType);
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

            var results = await ExecuteParallelFaceIdActionAsync(
                terminals,
                t => _faceIdService.PushUserAsync(
                    t,
                    client.Code,
                    client.Name,
                    client.Gender == 1,
                    client.PhoneNumber,
                    faceBytes,
                    cancellationToken),
                cancellationToken);

            response.Results = results;
            response.SuccessCount = results.Count(r => r.IsSuccess);
            response.FailureCount = results.Count(r => !r.IsSuccess);

            _logger.LogInformation(
                "Đồng bộ FaceID cho khách hàng {Name} hoàn tất: {Success}/{Total} thiết bị thành công.",
                client.Name, response.SuccessCount, response.TotalDevices);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    HPParking.Core.Models.Enums.AuditActionType.ManualOverride,
                    "Client",
                    client.Id,
                    client.Name,
                    reason: $"Đồng bộ nhận diện khuôn mặt FaceID cho khách hàng '{client.Name}': {response.SuccessCount}/{response.TotalDevices} thiết bị thành công.",
                    isSuccess: response.FailureCount == 0,
                    errorMessage: response.FailureCount > 0 ? $"{response.FailureCount} thiết bị đồng bộ thất bại" : null,
                    cancellationToken: cancellationToken);
            }

            return response;
        }

        private async Task<List<FaceIdTerminalResultDto>> ExecuteParallelFaceIdActionAsync(
            List<FaceIdTerminalConfig> terminals,
            Func<FaceIdTerminalConfig, Task<FaceIdTerminalResultDto>> action,
            CancellationToken cancellationToken = default)
        {
            if (terminals == null || terminals.Count == 0)
            {
                return new List<FaceIdTerminalResultDto>();
            }

            var tasks = terminals.Select(async terminal =>
            {
                try
                {
                    // 1. Fail-Fast Ping 600ms
                    var isAlive = await _faceIdService.PingFastAsync(terminal.DeviceIp, 600, cancellationToken);
                    if (!isAlive)
                    {
                        return new FaceIdTerminalResultDto
                        {
                            DeviceIp = terminal.DeviceIp,
                            DeviceName = terminal.DeviceName,
                            IsSuccess = false,
                            ErrorMessage = $"Mất kết nối tới thiết bị [{terminal.DeviceIp}] (Ping timeout 600ms)",
                            Timestamp = DateTime.UtcNow
                        };
                    }

                    // 2. Thiết bị sống -> Thực thi hành động CRUD
                    return await action(terminal);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi thực thi FaceID trên thiết bị {DeviceIp} ({DeviceName}): {Message}",
                        terminal.DeviceIp, terminal.DeviceName, ex.Message);
                    return new FaceIdTerminalResultDto
                    {
                        DeviceIp = terminal.DeviceIp,
                        DeviceName = terminal.DeviceName,
                        IsSuccess = false,
                        ErrorMessage = FaceIdErrorFormatter.Format(ex.Message, terminal.DeviceIp),
                        Timestamp = DateTime.UtcNow
                    };
                }
            });

            return (await Task.WhenAll(tasks)).ToList();
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
