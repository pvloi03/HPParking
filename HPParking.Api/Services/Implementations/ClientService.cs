using HPParking.Api.Common.Exceptions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Core.Models.ValueObjects;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;
using System.Text.RegularExpressions;

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
        private readonly IRepository<Card>? _cardRepo;
        private readonly IRepository<ClientApprovalRequest>? _approvalRepo;
        private readonly IRepository<ParkingSession>? _parkingSessionRepo;
        private readonly IEmailSenderService? _emailSenderService;
        private readonly IConfiguration? _configuration;
        private readonly IClientApprovalEmailTemplateBuilder _emailTemplateBuilder;

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
            IAuditLogService? auditLogService = null,
            IRepository<Card>? cardRepo = null,
            IRepository<ClientApprovalRequest>? approvalRepo = null,
            IRepository<ParkingSession>? parkingSessionRepo = null,
            IEmailSenderService? emailSenderService = null,
            IConfiguration? configuration = null,
            IClientApprovalEmailTemplateBuilder? emailTemplateBuilder = null)
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
            _cardRepo = cardRepo;
            _approvalRepo = approvalRepo;
            _parkingSessionRepo = parkingSessionRepo;
            _emailSenderService = emailSenderService;
            _configuration = configuration;
            _emailTemplateBuilder = emailTemplateBuilder ?? new ClientApprovalEmailTemplateBuilder();
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

            // Lấy thông tin yêu cầu phê duyệt mới nhất (ADR 0042)
            if (_approvalRepo != null)
            {
                var approvals = await _approvalRepo.FindAsync(a => a.ClientId == id && !a.IsDeleted, cancellationToken);
                var latest = approvals.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
                if (latest != null)
                {
                    string deptName = string.Empty;
                    if (_departmentRepo != null && !string.IsNullOrWhiteSpace(latest.ApprovingDepartmentId))
                    {
                        var d = await _departmentRepo.GetByIdAsync(latest.ApprovingDepartmentId, cancellationToken);
                        deptName = d?.Name ?? string.Empty;
                    }

                    detailDto.LatestApprovalRequest = MapToApprovalRequestDto(latest, deptName);
                }
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

            // 2. Kiểm tra tính duy nhất của Mã định danh (bắt buộc, case-insensitive)
            ClientCode clientCode = request.Code;
            var codePattern = $"^{Regex.Escape(clientCode.Value)}$";
            var existingCode = await _clientRepo.FindOneAsync(
                c => c.Code != null && Regex.IsMatch(c.Code, codePattern, RegexOptions.IgnoreCase) && !c.IsDeleted,
                cancellationToken);

            if (existingCode != null)
            {
                throw new ConflictException(
                    $"Mã định danh '{clientCode}' đã tồn tại trong hệ thống ({existingCode.Name}).",
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

            var authMethods = (request.AuthMethods != null && request.AuthMethods.Count > 0)
                ? request.AuthMethods
                : [HPParking.Core.Constants.AuthMethodConstants.FaceId];

            var isNoneAuth = authMethods.Contains(HPParking.Core.Constants.AuthMethodConstants.None);
            var cleanCardCode = isNoneAuth
                ? string.Empty
                : HPParking.Core.Helpers.CardHelper.NormalizeCardCode(request.CardCode);

            if (!string.IsNullOrWhiteSpace(cleanCardCode))
            {
                var otherClient = await _clientRepo.FindOneAsync(c => c.CardCode == cleanCardCode && !c.IsDeleted, cancellationToken);
                if (otherClient != null)
                {
                    throw new BadRequestException($"Mã thẻ '{cleanCardCode}' đang được sử dụng bởi khách hàng '{otherClient.Name}'.");
                }

                if (_cardRepo != null)
                {
                    var card = await _cardRepo.FindOneAsync(c => c.CardNumber == cleanCardCode && c.TargetType == CardTargetType.Person && !c.IsDeleted, cancellationToken);
                    if (card != null && !string.IsNullOrWhiteSpace(card.ClientId))
                    {
                        var assignedClient = await _clientRepo.FindOneAsync(c => c.Id == card.ClientId && !c.IsDeleted, cancellationToken);
                        var ownerName = assignedClient?.Name ?? card.ClientId;
                        throw new BadRequestException($"Thẻ định danh '{cleanCardCode}' đã được gán cho nhân sự '{ownerName}'.");
                    }
                }
            }

            Department? approvingDepartment = null;
            if (!string.IsNullOrWhiteSpace(request.ApprovingDepartmentId) && _departmentRepo != null)
            {
                approvingDepartment = await _departmentRepo.GetByIdAsync(request.ApprovingDepartmentId, cancellationToken);
                if (approvingDepartment == null || approvingDepartment.IsDeleted)
                {
                    throw new NotFoundException($"Không tìm thấy phòng ban phê duyệt với Id '{request.ApprovingDepartmentId}'.", ErrorCodes.DEPARTMENT_NOT_FOUND);
                }
                if (!approvingDepartment.IsActive)
                {
                    throw new BadRequestException($"Phòng ban phê duyệt '{approvingDepartment.Name}' đang bị vô hiệu hóa, không thể tiếp nhận email.", ErrorCodes.BAD_REQUEST);
                }
                if (string.IsNullOrWhiteSpace(approvingDepartment.Email))
                {
                    throw new BadRequestException($"Phòng ban phê duyệt '{approvingDepartment.Name}' chưa được cấu hình địa chỉ email.", ErrorCodes.BAD_REQUEST);
                }
            }

            // 5. Tạo thực thể Client
            var client = new Client
            {
                Code = clientCode,
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
                CardCode = cleanCardCode,
                AuthMethods = authMethods,
                VerifyVehiclePlate = isNoneAuth ? false : request.VerifyVehiclePlate,
                AutoOpenBarrier = isNoneAuth
                    ? new AutoOpenBarrier { OnEntry = true, OnExit = true }
                    : request.AutoOpenBarrier ?? new AutoOpenBarrier(),
                IsActive = false, // Nhân sự mới luôn bắt đầu ở trạng thái Chưa kích hoạt, chờ phòng ban phê duyệt (ADR 0042)
                Expired = request.Expired ?? new(),
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            await _clientRepo.AddAsync(client, cancellationToken);
            _logger.LogInformation("Đã tạo mới nhân sự: {Name} ({Phone}) - ID: {Id}", client.Name, cleanPhone, client.Id);

            // 5. Thêm các phương tiện đi kèm nếu có
            var vehicleDtos = new List<VehicleDto>();
            foreach (var reqV in normalizedVehicles)
            {
                var vehicle = new Vehicle
                {
                    PlateNumber = reqV.PlateNumber,
                    Type = reqV.Type,
                    OwnerClientId = client.Id,
                    // Quy tắc 2 (Active State Protection): Thực thể con không được phép Active khi thực thể cha chưa kích hoạt
                    IsActive = false,
                    Note = reqV.Note,
                    CreatedAt = DateTime.UtcNow
                };
                await _vehicleRepo.AddAsync(vehicle, cancellationToken);
                vehicleDtos.Add(vehicle.Adapt<VehicleDto>());
            }

            if (_cardRepo != null && !string.IsNullOrWhiteSpace(cleanCardCode))
            {
                var card = await _cardRepo.FindOneAsync(c => c.CardNumber == cleanCardCode && c.TargetType == CardTargetType.Person && !c.IsDeleted, cancellationToken);
                if (card != null)
                {
                    card.ClientId = client.Id;
                    card.Status = CardStatus.InUse;
                    await _cardRepo.UpdateAsync(card, cancellationToken);
                }
            }

            var detailDto = client.Adapt<ClientDetailDto>();
            detailDto.Vehicles = vehicleDtos;

            // 6. Tự động nạp thông tin Khách hàng (User & Thẻ) lên toàn bộ FaceID active nếu có bật FaceId
            if (client.AuthMethods.Contains(HPParking.Core.Constants.AuthMethodConstants.FaceId))
            {
                try
                {
                    var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
                    if (terminals.Count > 0)
                    {
                        string faceCardCode = client.CardCode ?? "";
                        var pushResults = await ExecuteParallelFaceIdActionAsync(
                            terminals,
                            t => _faceIdService.PushUserAsync(
                                t,
                                client.Code,
                                client.Name,
                                client.Gender == 1,
                                faceCardCode,
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
                            CardCount = (r.IsSuccess && !string.IsNullOrWhiteSpace(faceCardCode)) ? 1 : 0,
                            Cards = (r.IsSuccess && !string.IsNullOrWhiteSpace(faceCardCode)) ? new List<string> { faceCardCode } : new(),
                            ErrorMessage = r.IsSuccess ? null : r.ErrorMessage,
                            Timestamp = r.Timestamp
                        }).ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi nạp FaceID tự động khi tạo khách hàng {Id}: {Message}", client.Id, ex.Message);
                }
            }

            // 7. Tạo bản ghi yêu cầu phê duyệt và gửi email xác nhận nếu có chỉ định phòng ban (ADR 0042)
            if (approvingDepartment != null && _approvalRepo != null)
            {
                var token = GenerateApprovalToken();
                var approvalRequest = new ClientApprovalRequest
                {
                    ClientId = client.Id,
                    ApprovingDepartmentId = approvingDepartment.Id,
                    ApprovingDepartmentEmail = approvingDepartment.Email!.Trim(),
                    ActionType = ClientApprovalActionType.Activate,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsProcessed = false,
                    CreatedAt = DateTime.UtcNow
                };

                // Gửi email xác nhận đến phòng ban
                if (_emailSenderService != null)
                {
                    try
                    {
                        using var emailCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        emailCts.CancelAfter(TimeSpan.FromSeconds(8));
                        await SendApprovalEmailAsync(client, approvingDepartment, approvalRequest, emailCts.Token);
                        approvalRequest.EmailSent = true;
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        approvalRequest.EmailSent = false;
                        approvalRequest.EmailError = "Quá thời gian chờ gửi email (SMTP Timeout).";
                        _logger.LogWarning("Timeout khi gửi email xác nhận kích hoạt nhân sự {ClientId} tới {Email}", client.Id, approvingDepartment.Email);
                    }
                    catch (Exception ex)
                    {
                        approvalRequest.EmailSent = false;
                        approvalRequest.EmailError = ex.Message;
                        _logger.LogWarning(ex, "Không thể gửi email xác nhận kích hoạt nhân sự {ClientId} tới {Email}: {Message}", client.Id, approvingDepartment.Email, ex.Message);
                    }
                }

                await _approvalRepo.AddAsync(approvalRequest, CancellationToken.None);
                detailDto.LatestApprovalRequest = MapToApprovalRequestDto(approvalRequest, approvingDepartment.Name);
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    HPParking.Core.Models.Enums.AuditActionType.Create,
                    "Client",
                    client.Id,
                    client.Name,
                    reason: $"Tạo mới hồ sơ nhân sự '{client.Name}' (Mã định danh: {client.Code}).",
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

            ClientCode clientCode = request.Code;
            if (!string.Equals(client.Code, clientCode.Value, StringComparison.OrdinalIgnoreCase))
            {
                var codePattern = $"^{Regex.Escape(clientCode.Value)}$";
                var existingCode = await _clientRepo.FindOneAsync(
                    c => c.Code != null && Regex.IsMatch(c.Code, codePattern, RegexOptions.IgnoreCase) && c.Id != id && !c.IsDeleted,
                    cancellationToken);

                if (existingCode != null)
                {
                    throw new ConflictException(
                        $"Mã định danh '{clientCode}' đã tồn tại trong hệ thống ({existingCode.Name}).",
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
            var oldCardCode = client.CardCode;

            var authMethods = (request.AuthMethods != null && request.AuthMethods.Count > 0)
                ? request.AuthMethods
                : [HPParking.Core.Constants.AuthMethodConstants.FaceId];

            var isNoneAuth = authMethods.Contains(HPParking.Core.Constants.AuthMethodConstants.None);
            var cleanCardCode = isNoneAuth
                ? string.Empty
                : HPParking.Core.Helpers.CardHelper.NormalizeCardCode(request.CardCode);
            var isCodeChanged = !string.Equals(client.Code, clientCode.Value, StringComparison.OrdinalIgnoreCase);
            var isPhoneChanged = !string.Equals(client.PhoneNumber, cleanPhone, StringComparison.OrdinalIgnoreCase);
            var isCardChanged = !string.Equals(oldCardCode, cleanCardCode, StringComparison.OrdinalIgnoreCase);
            var isNameOrGenderChanged = !string.Equals(client.Name, request.Name.Trim(), StringComparison.Ordinal) || client.Gender != request.Gender;

            if (isCardChanged && !string.IsNullOrWhiteSpace(cleanCardCode))
            {
                var otherClient = await _clientRepo.FindOneAsync(c => c.CardCode == cleanCardCode && c.Id != id && !c.IsDeleted, cancellationToken);
                if (otherClient != null)
                {
                    throw new BadRequestException($"Mã thẻ '{cleanCardCode}' đang được sử dụng bởi khách hàng '{otherClient.Name}'.");
                }

                if (_cardRepo != null)
                {
                    var card = await _cardRepo.FindOneAsync(c => c.CardNumber == cleanCardCode && c.TargetType == CardTargetType.Person && !c.IsDeleted, cancellationToken);
                    if (card != null && !string.IsNullOrWhiteSpace(card.ClientId) && card.ClientId != id)
                    {
                        var assignedClient = await _clientRepo.FindOneAsync(c => c.Id == card.ClientId && !c.IsDeleted, cancellationToken);
                        var ownerName = assignedClient?.Name ?? card.ClientId;
                        throw new BadRequestException($"Thẻ định danh '{cleanCardCode}' đã được gán cho nhân sự '{ownerName}'.");
                    }
                }
            }

            if (!isNoneAuth && request.VerifyVehiclePlate)
            {
                var existingVehiclesCount = await _vehicleRepo.CountAsync(v => v.OwnerClientId == id && !v.IsDeleted, cancellationToken);
                var newVehiclesCount = normalizedVehicles.Count;
                if (existingVehiclesCount + newVehiclesCount == 0)
                {
                    throw new BadRequestException("Khi bật xác thực đối chiếu xe, bắt buộc khách hàng phải có ít nhất một phương tiện đăng ký.");
                }
            }

            client.Code = clientCode;
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
            client.CardCode = cleanCardCode;
            client.AuthMethods = authMethods;
            client.VerifyVehiclePlate = isNoneAuth ? false : request.VerifyVehiclePlate;
            client.AutoOpenBarrier = isNoneAuth
                ? new AutoOpenBarrier { OnEntry = true, OnExit = true }
                : request.AutoOpenBarrier ?? new AutoOpenBarrier();


            // Bảo toàn trạng thái hoạt động: thay đổi trạng thái IsActive bắt buộc phải thông qua phê duyệt email phòng ban (ADR 0042)
            // client.IsActive được giữ nguyên, không cho phép cập nhật trực tiếp qua form sửa
            client.Expired = request.Expired ?? new();
            client.Note = request.Note;
            client.UpdatedAt = DateTime.UtcNow;

            await _clientRepo.UpdateAsync(client, cancellationToken);
            _logger.LogInformation("Đã cập nhật khách hàng ID {Id}: {Name}", id, client.Name);

            // Cập nhật quan hệ hai chiều với thực thể Thẻ (Card)
            if (_cardRepo != null)
            {
                if (isNoneAuth)
                {
                    // Khi chọn Làn tự do (None): Giải phóng toàn bộ thẻ đang liên kết với nhân sự này
                    var assignedCards = await _cardRepo.FindAsync(c => c.ClientId == id && !c.IsDeleted, cancellationToken);
                    foreach (var card in assignedCards)
                    {
                        card.ClientId = null;
                        card.Status = CardStatus.Available;
                        await _cardRepo.UpdateAsync(card, cancellationToken);
                    }

                    if (!string.IsNullOrWhiteSpace(oldCardCode))
                    {
                        var oldCard = await _cardRepo.FindOneAsync(c => c.CardNumber == oldCardCode && c.TargetType == CardTargetType.Person && !c.IsDeleted, cancellationToken);
                        if (oldCard != null && oldCard.ClientId == id)
                        {
                            oldCard.ClientId = null;
                            oldCard.Status = CardStatus.Available;
                            await _cardRepo.UpdateAsync(oldCard, cancellationToken);
                        }
                    }
                }
                else if (isCardChanged)
                {
                    if (!string.IsNullOrWhiteSpace(oldCardCode))
                    {
                        var oldCard = await _cardRepo.FindOneAsync(c => c.CardNumber == oldCardCode && c.TargetType == CardTargetType.Person && !c.IsDeleted, cancellationToken);
                        if (oldCard != null && oldCard.ClientId == id)
                        {
                            oldCard.ClientId = null;
                            oldCard.Status = CardStatus.Available;
                            await _cardRepo.UpdateAsync(oldCard, cancellationToken);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(cleanCardCode))
                    {
                        var newCard = await _cardRepo.FindOneAsync(c => c.CardNumber == cleanCardCode && c.TargetType == CardTargetType.Person && !c.IsDeleted, cancellationToken);
                        if (newCard != null)
                        {
                            newCard.ClientId = id;
                            newCard.Status = CardStatus.InUse;
                            await _cardRepo.UpdateAsync(newCard, cancellationToken);
                        }
                    }
                }
            }

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
                    if (isNoneAuth)
                    {
                        // 1. Khi chọn Làn tự do (None): Xóa sạch hồ sơ người dùng, ảnh khuôn mặt và thẻ trên toàn bộ thiết bị FaceID
                        faceResults = await ExecuteParallelFaceIdActionAsync(
                            terminals,
                            async t => await _faceIdService.DeleteUserAsync(t, oldCode, oldCardCode ?? "", cancellationToken),
                            cancellationToken);
                    }
                    else if (isCodeChanged)
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
                                await _faceIdService.DeleteUserAsync(t, oldCode, oldCardCode ?? "", cancellationToken);
                                return await _faceIdService.PushUserAsync(
                                    t,
                                    client.Code,
                                    client.Name,
                                    client.Gender == 1,
                                    client.CardCode ?? "",
                                    faceBytes,
                                    cancellationToken);
                            },
                            cancellationToken);
                    }
                    else if (isCardChanged || isNameOrGenderChanged)
                    {
                        // 2. Không đổi CCCD:
                        // - Nếu đổi Thẻ: Self-Healing quét sạch thẻ cũ và gán thẻ mới (dùng client.CardCode chuẩn, KHÔNG dùng SĐT)
                        // - Nếu đổi Tên hoặc Giới tính: Cập nhật UserInfo
                        faceResults = await ExecuteParallelFaceIdActionAsync(
                            terminals,
                            async t =>
                            {
                                if (isCardChanged)
                                {
                                    if (!string.IsNullOrWhiteSpace(client.CardCode))
                                    {
                                        var cardRes = await _faceIdService.CleanAndAssignCardAsync(t, client.Code, client.CardCode, cancellationToken);
                                        if (!cardRes.IsSuccess) return cardRes;
                                    }
                                    else if (!string.IsNullOrWhiteSpace(oldCardCode))
                                    {
                                        var cardRes = await _faceIdService.DeleteCardAsync(t, oldCardCode, cancellationToken);
                                        if (!cardRes.IsSuccess) return cardRes;
                                    }
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
                            UserExists = !isNoneAuth && r.IsSuccess,
                            HasFace = false,
                            CardCount = (!isNoneAuth && r.IsSuccess && !string.IsNullOrWhiteSpace(client.CardCode)) ? 1 : 0,
                            Cards = (!isNoneAuth && r.IsSuccess && !string.IsNullOrWhiteSpace(client.CardCode)) ? new List<string> { client.CardCode } : new(),
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
                    reason: $"Cập nhật hồ sơ nhân sự '{client.Name}' (Mã định danh: {client.Code}).",
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
                throw new NotFoundException("Không tìm thấy nhân sự cần xóa.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            // Universal Restrict Deletion Policy (ADR 0030 & ADR 0031):
            // Chặn xóa (409 Conflict) nếu còn bất kỳ phương tiện nào ĐANG HOẠT ĐỘNG (!IsDeleted)
            var activeVehicles = await _vehicleRepo.FindAsync(
                v => v.OwnerClientId == id && !v.IsDeleted,
                cancellationToken);

            if (activeVehicles.Count > 0)
            {
                throw new ConflictException(
                    $"Không thể xóa nhân sự '{client.Name}' vì vẫn còn {activeVehicles.Count} phương tiện đang hoạt động liên kết. Vui lòng xóa hoặc chuyển quyền sở hữu phương tiện trước.",
                    ErrorCodes.CLIENT_HAS_VEHICLES);
            }

            // Quy tắc 7: Active Parking Session Lockout (Table 1.2 - 409 Conflict)
            if (_parkingSessionRepo != null)
            {
                var hasActiveSession = await _parkingSessionRepo.ExistsAsync(
                    s => !s.IsDeleted && s.Status == ParkingSessionStatus.Active && s.PersonId == id,
                    cancellationToken);

                if (hasActiveSession)
                {
                    throw new ConflictException(
                        $"Không thể xóa nhân sự '{client.Name}' vì đang có phiên đỗ xe hoạt động trong bãi. Vui lòng hoàn tất lượt gửi xe trước khi xóa hồ sơ.",
                        ErrorCodes.CLIENT_HAS_ACTIVE_PARKING_SESSION);
                }
            }

            // Universal Restrict Deletion Policy (Quy tắc 1 & Mục 3):
            // Chặn xóa (409 Conflict) nếu còn yêu cầu phê duyệt qua email ĐANG CHỜ XỬ LÝ (!IsProcessed && !IsDeleted)
            if (_approvalRepo != null)
            {
                var pendingApprovalCount = await _approvalRepo.CountAsync(
                    r => r.ClientId == id && !r.IsProcessed && !r.IsDeleted,
                    cancellationToken);

                if (pendingApprovalCount > 0)
                {
                    throw new ConflictException(
                        $"Không thể xóa nhân sự '{client.Name}' vì vẫn còn {pendingApprovalCount} yêu cầu phê duyệt qua email đang chờ xử lý. Vui lòng xử lý hoặc hủy yêu cầu trước khi xóa.",
                        ErrorCodes.CLIENT_HAS_PENDING_APPROVAL_REQUEST);
                }
            }

            if (!hardDelete)
            {
                // =========================================================================
                // XÓA MỀM (Soft Delete - Mặc định):
                // 1. Tự động gỡ liên kết thẻ định danh (nếu có) và hoàn trả thẻ về kho (Available)
                // 2. Làm rỗng client.CardCode để tránh xung đột mã thẻ trong thùng rác
                // 3. Thu hồi quyền vật lý: Xóa dữ liệu FaceID và thẻ trên toàn bộ thiết bị FaceID active
                // 4. Bảo lưu tệp avatar trên đĩa và trong DB để có thể khôi phục nguyên vẹn sau này
                // 5. Đánh dấu xóa mềm Client trong CSDL
                // =========================================================================
                var oldCardCode = client.CardCode;
                if (_cardRepo != null)
                {
                    var assignedCards = await _cardRepo.FindAsync(c => c.ClientId == id && !c.IsDeleted, cancellationToken);
                    foreach (var card in assignedCards)
                    {
                        if (string.IsNullOrWhiteSpace(oldCardCode))
                        {
                            oldCardCode = card.CardNumber;
                        }
                        card.ClientId = null;
                        card.Status = CardStatus.Available;
                        await _cardRepo.UpdateAsync(card, cancellationToken);
                    }

                    if (!string.IsNullOrWhiteSpace(oldCardCode))
                    {
                        var oldCard = await _cardRepo.FindOneAsync(c => c.CardNumber == oldCardCode && c.TargetType == CardTargetType.Person && !c.IsDeleted, cancellationToken);
                        if (oldCard != null)
                        {
                            oldCard.ClientId = null;
                            oldCard.Status = CardStatus.Available;
                            await _cardRepo.UpdateAsync(oldCard, cancellationToken);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(client.CardCode))
                {
                    client.CardCode = string.Empty;
                    await _clientRepo.UpdateAsync(client, cancellationToken);
                }

                try
                {
                    var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
                    if (terminals.Count > 0)
                    {
                        await ExecuteParallelFaceIdActionAsync(
                            terminals,
                            t => _faceIdService.DeleteUserAsync(t, client.Code, oldCardCode ?? "", cancellationToken),
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi thu hồi FaceID khi xóa mềm nhân sự {Id}: {Message}", id, ex.Message);
                }

                await _clientRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã XÓA MỀM nhân sự {Id}: {Name} (đã gỡ thẻ và thu hồi quyền FaceID).", id, client.Name);



                if (_auditLogService != null)
                {
                    await _auditLogService.LogActivityAsync(
                        HPParking.Core.Models.Enums.AuditActionType.Delete,
                        "Client",
                        client.Id,
                        client.Name,
                        reason: $"Chuyển nhân sự '{client.Name}' vào thùng rác.",
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

            var oldHardCardCode = client.CardCode;
            if (_cardRepo != null)
            {
                var assignedCards = await _cardRepo.FindAsync(c => c.ClientId == id && !c.IsDeleted, cancellationToken);
                foreach (var card in assignedCards)
                {
                    if (string.IsNullOrWhiteSpace(oldHardCardCode))
                    {
                        oldHardCardCode = card.CardNumber;
                    }
                    card.ClientId = null;
                    card.Status = CardStatus.Available;
                    await _cardRepo.UpdateAsync(card, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(oldHardCardCode))
                {
                    var card = await _cardRepo.FindOneAsync(c => c.CardNumber == oldHardCardCode && c.TargetType == CardTargetType.Person && !c.IsDeleted, cancellationToken);
                    if (card != null)
                    {
                        card.ClientId = null;
                        card.Status = CardStatus.Available;
                        await _cardRepo.UpdateAsync(card, cancellationToken);
                    }
                }
            }

            foreach (var v in deletedVehicles)
            {
                await _vehicleRepo.DeleteAsync(v.Id, softDelete: false, cancellationToken);
            }

            // Dọn dẹp vĩnh viễn các bản ghi yêu cầu phê duyệt liên kết (tránh dữ liệu mồ côi khi Hard Delete)
            if (_approvalRepo != null)
            {
                var approvals = await _approvalRepo.FindAsync(
                    r => r.ClientId == id,
                    cancellationToken);

                foreach (var a in approvals)
                {
                    await _approvalRepo.DeleteAsync(a.Id, softDelete: false, cancellationToken);
                }
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
                        t => _faceIdService.DeleteUserAsync(t, client.Code, oldHardCardCode ?? "", cancellationToken),
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

            // Re-validation: Kiểm tra Mã định danh (nếu có) với các nhân sự đang hoạt động
            ClientCode clientCode = client.Code;
            if (!clientCode.IsEmpty)
            {
                var codePattern = $"^{Regex.Escape(clientCode.Value)}$";
                var existingCode = await _clientRepo.FindOneAsync(
                    c => c.Code != null && Regex.IsMatch(c.Code, codePattern, RegexOptions.IgnoreCase) && c.Id != id && !c.IsDeleted,
                    cancellationToken);

                if (existingCode != null)
                {
                    throw new ConflictException(
                        $"Không thể khôi phục vì mã định danh '{clientCode}' đã được sử dụng bởi nhân sự đang hoạt động '{existingCode.Name}'.",
                        ErrorCodes.CLIENT_CODE_DUPLICATE);
                }
            }

            var success = await _clientRepo.RestoreAsync(id, cancellationToken);
            if (!success)
            {
                throw new AppException("Khôi phục nhân sự thất bại.", 500, ErrorCodes.RESTORE_FAILED);
            }

            // Đảm bảo mã thẻ trống khi khôi phục từ thùng rác (tránh xung đột mã thẻ)
            if (!string.IsNullOrWhiteSpace(client.CardCode))
            {
                client.CardCode = string.Empty;
                await _clientRepo.UpdateAsync(client, cancellationToken);
            }
            else
            {
                client.CardCode = string.Empty;
            }

            client.IsDeleted = false;
            client.DeletedAt = null;
            client.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Đã KHÔI PHỤC nhân sự {Id}: {Name} ({Phone}) từ thùng rác.", client.Id, client.Name, client.PhoneNumber);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    HPParking.Core.Models.Enums.AuditActionType.Restore,
                    "Client",
                    client.Id,
                    client.Name,
                    reason: $"Khôi phục nhân sự '{client.Name}' (Mã định danh: {client.Code}) từ thùng rác.",
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

            // Tự động đẩy Avatar mới lên toàn bộ FaceID active song song (chỉ khi có cấu hình FaceID và không phải Làn tự do)
            var hasFaceId = client.AuthMethods != null &&
                            client.AuthMethods.Contains(HPParking.Core.Constants.AuthMethodConstants.FaceId) &&
                            !client.AuthMethods.Contains(HPParking.Core.Constants.AuthMethodConstants.None);

            if (hasFaceId)
            {
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

            if (client.AuthMethods != null && client.AuthMethods.Contains(HPParking.Core.Constants.AuthMethodConstants.None))
            {
                var deleteResults = await ExecuteParallelFaceIdActionAsync(
                    terminals,
                    t => _faceIdService.DeleteUserAsync(t, client.Code, client.CardCode ?? "", cancellationToken),
                    cancellationToken);
                response.SuccessCount = deleteResults.Count(r => r.IsSuccess);
                response.FailureCount = deleteResults.Count(r => !r.IsSuccess);
                response.Results = [.. deleteResults];
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
                    client.CardCode ?? "",
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
                    HPParking.Core.Models.Enums.AuditActionType.FaceIdSync,
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
                    // 1. Kiểm tra kết nối thiết bị với timeout 2000ms
                    var targetIpWithPort = terminal.Port > 0 && terminal.Port != 80 && terminal.Port != 443
                        ? $"{terminal.DeviceIp}:{terminal.Port}"
                        : terminal.DeviceIp;
                    var isAlive = await _faceIdService.PingFastAsync(targetIpWithPort, 2000, cancellationToken);
                    if (!isAlive)
                    {
                        return new FaceIdTerminalResultDto
                        {
                            DeviceIp = terminal.DeviceIp,
                            DeviceName = terminal.DeviceName,
                            IsSuccess = false,
                            ErrorMessage = $"Mất kết nối tới thiết bị [{terminal.DeviceIp}] (Kiểm tra lại nguồn, IP/Port và mạng LAN)",
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
                        // Cố định Port 443 (HTTPS) cho kết nối ISAPI từ API, bỏ qua port SDK (8000) lưu trong CSDL
                        Port = 443,
                        DeviceName = string.IsNullOrWhiteSpace(device.Name) ? lane.Name : device.Name,
                        Username = string.IsNullOrWhiteSpace(device.UserName) ? "admin" : device.UserName.Trim(),
                        Password = device.Password?.Trim() ?? string.Empty
                    });
                }
            }

            return uniqueConfigs;
        }

        private async Task SendApprovalEmailAsync(
            Client client,
            Department department,
            ClientApprovalRequest approvalRequest,
            CancellationToken cancellationToken)
        {
            if (_emailSenderService == null || string.IsNullOrWhiteSpace(department.Email)) return;

            var appBaseUrl = _configuration?["AppBaseUrl"]
                ?? _configuration?["CorsSettings:AllowedOrigins:0"]
                ?? "http://localhost:5173";
            appBaseUrl = appBaseUrl.TrimEnd('/');

            var confirmUrl = $"{appBaseUrl}/confirm-client-status?token={approvalRequest.Token}";

            var vehicleSummaries = new List<string>();
            if (_vehicleRepo != null)
            {
                var vehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == client.Id && !v.IsDeleted, cancellationToken);
                if (vehicles != null && vehicles.Count > 0)
                {
                    vehicleSummaries.AddRange(vehicles.Select(v => $"{v.PlateNumber} ({v.Type})"));
                }
            }

            string? workingDeptName = null;
            if (_departmentRepo != null && !string.IsNullOrWhiteSpace(client.DepartmentId))
            {
                var workDept = await _departmentRepo.GetByIdAsync(client.DepartmentId, cancellationToken);
                if (workDept != null) workingDeptName = workDept.Name;
            }

            var emailModel = new ClientApprovalEmailModel
            {
                Client = client,
                TargetDepartment = department,
                ApprovalRequest = approvalRequest,
                ConfirmUrl = confirmUrl,
                WorkingDepartmentName = workingDeptName,
                VehicleSummaries = vehicleSummaries
            };

            string subject = _emailTemplateBuilder.BuildSubject(client, approvalRequest.ActionType);
            string htmlBody = _emailTemplateBuilder.BuildHtmlBody(emailModel);

            var emailSent = await _emailSenderService.SendEmailAsync(
                [department.Email.Trim()],
                subject,
                htmlBody,
                null,
                cancellationToken);

            if (!emailSent)
            {
                throw new InvalidOperationException($"Không thể gửi email đến '{department.Email}'. Vui lòng kiểm tra lại cấu hình SMTP hoặc thử lại sau.");
            }
        }

        public async Task<ConfirmClientStatusResponse> ConfirmStatusByTokenAsync(ApprovalToken token, CancellationToken cancellationToken = default)
        {
            if (token.IsEmpty)
            {
                throw new BadRequestException("Mã xác thực không được để trống.", ErrorCodes.BAD_REQUEST);
            }

            if (_approvalRepo == null)
            {
                throw new BadRequestException("Hệ thống chưa cấu hình lưu trữ yêu cầu phê duyệt.");
            }

            var approval = await _approvalRepo.FindOneAsync(r => r.Token == token && !r.IsDeleted, cancellationToken);
            if (approval == null)
            {
                throw new NotFoundException("Mã xác thực không tồn tại hoặc không hợp lệ.", ErrorCodes.NOT_FOUND);
            }

            if (!approval.IsValidForExecution())
            {
                if (approval.IsProcessed)
                {
                    var timeStr = approval.ProcessedAt.HasValue
                        ? approval.ProcessedAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                        : "trước đó";
                    throw new ConflictException($"Yêu cầu này đã được phê duyệt xác nhận vào lúc {timeStr}.", ErrorCodes.APPROVAL_REQUEST_ALREADY_PROCESSED);
                }

                if (DateTime.UtcNow > approval.ExpiresAt)
                {
                    throw new BadRequestException("Liên kết xác nhận đã hết hạn (quá 24 giờ). Vui lòng liên hệ quản trị viên để gửi lại yêu cầu mới.", ErrorCodes.APPROVAL_TOKEN_EXPIRED);
                }

                throw new BadRequestException("Yêu cầu này không còn hiệu lực để thực thi.", ErrorCodes.BAD_REQUEST);
            }

            var client = await _clientRepo.GetByIdAsync(approval.ClientId, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Hồ sơ nhân sự tương ứng không tồn tại hoặc đã bị xóa khỏi hệ thống.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            bool targetActive = (approval.ActionType == ClientApprovalActionType.Activate);

            // Quy tắc 7: Active Parking Session Lockout (CLIENT_HAS_ACTIVE_PARKING_SESSION - 400 Bad Request)
            // Chặn khóa hồ sơ nếu nhân sự hoặc xe liên kết đang gửi xe trong bãi
            if (!targetActive && _parkingSessionRepo != null)
            {
                var allClientVehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == client.Id && !v.IsDeleted, cancellationToken);
                var clientPlates = allClientVehicles
                    .Select(v => v.PlateNumber?.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                bool hasActiveSession;
                if (clientPlates.Count > 0)
                {
                    hasActiveSession = await _parkingSessionRepo.ExistsAsync(
                        s => !s.IsDeleted && s.Status == ParkingSessionStatus.Active &&
                             (s.PersonId == client.Id || clientPlates.Contains(s.PlateNumber)),
                        cancellationToken);
                }
                else
                {
                    hasActiveSession = await _parkingSessionRepo.ExistsAsync(
                        s => !s.IsDeleted && s.Status == ParkingSessionStatus.Active && s.PersonId == client.Id,
                        cancellationToken);
                }

                if (hasActiveSession)
                {
                    throw new BadRequestException(
                        $"Không thể vô hiệu hóa nhân sự '{client.Name}' vì nhân sự hoặc phương tiện liên kết đang có phiên đỗ xe hoạt động trong bãi. Vui lòng hoàn tất lượt gửi xe trước khi khóa hồ sơ.",
                        ErrorCodes.CLIENT_HAS_ACTIVE_PARKING_SESSION);
                }
            }

            client.IsActive = targetActive;
            client.UpdatedAt = DateTime.UtcNow;
            await _clientRepo.UpdateAsync(client, cancellationToken);

            if (targetActive)
            {
                // Khi phòng ban xác nhận kích hoạt nhân sự, tự động kích hoạt các phương tiện liên kết đi kèm
                var inactiveVehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == client.Id && !v.IsActive && !v.IsDeleted, cancellationToken);
                foreach (var v in inactiveVehicles)
                {
                    v.IsActive = true;
                    v.UpdatedAt = DateTime.UtcNow;
                    await _vehicleRepo.UpdateAsync(v, cancellationToken);
                }
            }
            else
            {
                // Khi phòng ban xác nhận khóa nhân sự qua email, tự động đồng bộ vô hiệu hóa các phương tiện liên kết (Rule 2 Active State Protection)
                // mà không gây kẹt thao tác của người duyệt email
                var activeVehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == client.Id && v.IsActive && !v.IsDeleted, cancellationToken);
                foreach (var v in activeVehicles)
                {
                    v.IsActive = false;
                    v.UpdatedAt = DateTime.UtcNow;
                    await _vehicleRepo.UpdateAsync(v, cancellationToken);
                }
            }

            approval.IsProcessed = true;
            approval.ProcessedAt = DateTime.UtcNow;
            approval.UpdatedAt = DateTime.UtcNow;
            await _approvalRepo.UpdateAsync(approval, cancellationToken);

            string deptName = "Phòng ban";
            if (_departmentRepo != null && !string.IsNullOrWhiteSpace(approval.ApprovingDepartmentId))
            {
                var dept = await _departmentRepo.GetByIdAsync(approval.ApprovingDepartmentId, cancellationToken);
                if (dept != null) deptName = dept.Name;
            }

            _logger.LogInformation("Xác nhận thành công cho nhân sự {ClientName} ({ClientId}) sang trạng thái {Status} bởi {Dept}",
                client.Name, client.Id, targetActive ? "Kích hoạt" : "Khóa", deptName);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    AuditActionType.Update,
                    "Client",
                    client.Id,
                    client.Name,
                    reason: $"Xác nhận qua email từ '{deptName}': chuyển trạng thái sang '{(targetActive ? "Hoạt động" : "Vô hiệu hóa")}'.",
                    cancellationToken: cancellationToken);
            }

            return new ConfirmClientStatusResponse
            {
                ClientId = client.Id,
                ClientName = client.Name,
                ClientCode = client.Code,
                ActionType = approval.ActionType,
                NewActiveStatus = targetActive,
                ProcessedAt = approval.ProcessedAt.Value,
                ApprovingDepartmentName = deptName,
                Message = $"Hồ sơ nhân sự '{client.Name}' đã được {(targetActive ? "kích hoạt" : "khóa")} thành công."
            };
        }

        public async Task<ClientApprovalRequestDto> RequestStatusChangeAsync(
            string clientId,
            RequestClientStatusChangeRequest request,
            string? currentUserId = null,
            CancellationToken cancellationToken = default)
        {
            if (_approvalRepo == null || _departmentRepo == null)
            {
                throw new BadRequestException("Chức năng phê duyệt qua phòng ban chưa sẵn sàng trên hệ thống.");
            }

            var client = await _clientRepo.GetByIdAsync(clientId, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin nhân sự.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            if (request.ActionType == ClientApprovalActionType.Activate && client.IsActive)
            {
                throw new BadRequestException("Hồ sơ nhân sự này hiện đã ở trạng thái Hoạt động.");
            }

            if (request.ActionType == ClientApprovalActionType.Deactivate)
            {
                if (!client.IsActive)
                {
                    throw new BadRequestException("Hồ sơ nhân sự này hiện đã ở trạng thái Vô hiệu hóa.");
                }

                // Quy tắc 2 & 7: Cảnh báo sớm nếu nhân sự còn phương tiện active hoặc đang có phiên gửi xe trong bãi
                var activeVehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == client.Id && v.IsActive && !v.IsDeleted, cancellationToken);
                if (activeVehicles.Count > 0)
                {
                    throw new BadRequestException(
                        $"Không thể yêu cầu khóa nhân sự '{client.Name}' vì vẫn còn {activeVehicles.Count} phương tiện đang hoạt động liên kết. Vui lòng vô hiệu hóa các phương tiện trước.",
                        ErrorCodes.BAD_REQUEST);
                }

                if (_parkingSessionRepo != null)
                {
                    var allClientVehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == client.Id && !v.IsDeleted, cancellationToken);
                    var clientPlates = allClientVehicles
                        .Select(v => v.PlateNumber?.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => p!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    bool hasActiveSession;
                    if (clientPlates.Count > 0)
                    {
                        hasActiveSession = await _parkingSessionRepo.ExistsAsync(
                            s => !s.IsDeleted && s.Status == ParkingSessionStatus.Active &&
                                 (s.PersonId == client.Id || clientPlates.Contains(s.PlateNumber)),
                            cancellationToken);
                    }
                    else
                    {
                        hasActiveSession = await _parkingSessionRepo.ExistsAsync(
                            s => !s.IsDeleted && s.Status == ParkingSessionStatus.Active && s.PersonId == client.Id,
                            cancellationToken);
                    }

                    if (hasActiveSession)
                    {
                        throw new BadRequestException(
                            $"Không thể yêu cầu khóa nhân sự '{client.Name}' vì nhân sự hoặc phương tiện liên kết đang có phiên đỗ xe hoạt động trong bãi. Vui lòng hoàn tất lượt gửi xe trước khi yêu cầu khóa.",
                            ErrorCodes.CLIENT_HAS_ACTIVE_PARKING_SESSION);
                    }
                }
            }

            var dept = await _departmentRepo.GetByIdAsync(request.ApprovingDepartmentId, cancellationToken);
            if (dept == null || dept.IsDeleted)
            {
                throw new NotFoundException($"Không tìm thấy phòng ban với Id '{request.ApprovingDepartmentId}'.", ErrorCodes.DEPARTMENT_NOT_FOUND);
            }
            if (!dept.IsActive)
            {
                throw new BadRequestException($"Phòng ban '{dept.Name}' đang bị vô hiệu hóa, không thể nhận email.", ErrorCodes.BAD_REQUEST);
            }
            if (string.IsNullOrWhiteSpace(dept.Email))
            {
                throw new BadRequestException($"Phòng ban '{dept.Name}' chưa có email cấu hình trong hệ thống.", ErrorCodes.BAD_REQUEST);
            }

            // Đánh dấu các request chưa xử lý trước đó của client này là đã bị thay thế (không soft-delete để bảo toàn lịch sử kiểm toán)
            await InvalidatePendingApprovalRequestsAsync(clientId, "Hủy do có yêu cầu thay đổi mới", cancellationToken);

            var token = GenerateApprovalToken();
            var approval = new ClientApprovalRequest
            {
                ClientId = client.Id,
                ApprovingDepartmentId = dept.Id,
                ApprovingDepartmentEmail = dept.Email.Trim(),
                ActionType = request.ActionType,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsProcessed = false,
                RequestedByUserId = currentUserId,
                Note = request.Note?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            if (_emailSenderService != null)
            {
                try
                {
                    using var emailCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    emailCts.CancelAfter(TimeSpan.FromSeconds(8));
                    await SendApprovalEmailAsync(client, dept, approval, emailCts.Token);
                    approval.EmailSent = true;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    approval.EmailSent = false;
                    approval.EmailError = "Quá thời gian chờ gửi email (SMTP Timeout).";
                    _logger.LogWarning("Timeout khi gửi email yêu cầu đổi trạng thái nhân sự {ClientId} tới {Email}", client.Id, dept.Email);
                }
                catch (Exception ex)
                {
                    approval.EmailSent = false;
                    approval.EmailError = ex.Message;
                    _logger.LogWarning(ex, "Lỗi gửi email yêu cầu đổi trạng thái nhân sự {ClientId}: {Message}", client.Id, ex.Message);
                }
            }

            await _approvalRepo.AddAsync(approval, CancellationToken.None);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    AuditActionType.Update,
                    "Client",
                    client.Id,
                    client.Name,
                    reason: $"Gửi yêu cầu {(request.ActionType == ClientApprovalActionType.Activate ? "Kích hoạt" : "Khóa")} hồ sơ tới phòng ban '{dept.Name}' ({dept.Email}).",
                    cancellationToken: CancellationToken.None);
            }

            return MapToApprovalRequestDto(approval, dept.Name);
        }

        public async Task<ClientApprovalRequestDto> ResendApprovalEmailAsync(
            string clientId,
            string? currentUserId = null,
            CancellationToken cancellationToken = default)
        {
            if (_approvalRepo == null || _departmentRepo == null)
            {
                throw new BadRequestException("Chức năng phê duyệt qua phòng ban chưa sẵn sàng trên hệ thống.");
            }

            var client = await _clientRepo.GetByIdAsync(clientId, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin nhân sự.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var pendingRequests = await _approvalRepo.FindAsync(r => r.ClientId == clientId && !r.IsDeleted, cancellationToken);
            var latest = pendingRequests.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
            if (latest == null)
            {
                throw new BadRequestException("Không tìm thấy yêu cầu phê duyệt nào trước đó để gửi lại.");
            }

            if (latest.IsProcessed)
            {
                throw new ConflictException("Yêu cầu phê duyệt này đã được xử lý hoàn tất, không thể gửi lại email.", ErrorCodes.CONFLICT);
            }

            var dept = await _departmentRepo.GetByIdAsync(latest.ApprovingDepartmentId, cancellationToken);
            if (dept == null || dept.IsDeleted || string.IsNullOrWhiteSpace(dept.Email))
            {
                throw new BadRequestException("Phòng ban phê duyệt không tồn tại hoặc chưa cấu hình email.");
            }

            // Gia hạn 24 giờ và sinh token mới cho yêu cầu đang chờ
            latest.Token = GenerateApprovalToken();
            latest.ExpiresAt = DateTime.UtcNow.AddHours(24);
            latest.RequestedByUserId = currentUserId;
            latest.UpdatedAt = DateTime.UtcNow;

            if (_emailSenderService != null)
            {
                try
                {
                    using var emailCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    emailCts.CancelAfter(TimeSpan.FromSeconds(8));
                    await SendApprovalEmailAsync(client, dept, latest, emailCts.Token);
                    latest.EmailSent = true;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    latest.EmailSent = false;
                    latest.EmailError = "Quá thời gian chờ gửi email (SMTP Timeout).";
                    _logger.LogWarning("Timeout khi gửi lại email phê duyệt nhân sự {ClientId} tới {Email}", client.Id, dept.Email);
                }
                catch (Exception ex)
                {
                    latest.EmailSent = false;
                    latest.EmailError = ex.Message;
                    _logger.LogWarning(ex, "Lỗi gửi lại email phê duyệt nhân sự {ClientId}: {Message}", client.Id, ex.Message);
                }
            }

            await _approvalRepo.UpdateAsync(latest, CancellationToken.None);
            return MapToApprovalRequestDto(latest, dept.Name);
        }

        private async Task InvalidatePendingApprovalRequestsAsync(string clientId, string reason, CancellationToken cancellationToken = default)
        {
            if (_approvalRepo == null) return;
            var pendingRequests = await _approvalRepo.FindAsync(r => r.ClientId == clientId && !r.IsProcessed && !r.IsDeleted, cancellationToken);
            foreach (var pr in pendingRequests)
            {
                pr.IsProcessed = true;
                pr.ProcessedAt = DateTime.UtcNow;
                pr.Note = string.IsNullOrWhiteSpace(pr.Note) ? reason : $"{pr.Note} ({reason})";
                pr.UpdatedAt = DateTime.UtcNow;
                await _approvalRepo.UpdateAsync(pr, cancellationToken);
            }
        }

        private static ApprovalToken GenerateApprovalToken()
        {
            return ApprovalToken.NewToken();
        }

        private static ClientApprovalRequestDto MapToApprovalRequestDto(ClientApprovalRequest r, string? deptName = null)
        {
            return new ClientApprovalRequestDto
            {
                Id = r.Id,
                ClientId = r.ClientId,
                ApprovingDepartmentId = r.ApprovingDepartmentId,
                ApprovingDepartmentName = deptName ?? string.Empty,
                ApprovingDepartmentEmail = r.ApprovingDepartmentEmail,
                ActionType = r.ActionType,
                ExpiresAt = r.ExpiresAt,
                IsProcessed = r.IsProcessed,
                ProcessedAt = r.ProcessedAt,
                CreatedAt = r.CreatedAt,
                Note = r.Note,
                EmailSent = r.EmailSent,
                EmailError = r.EmailError
            };
        }
    }
}
