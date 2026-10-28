using HPParking.Api.Common.Excel;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Excel.Cards;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Triển khai dịch vụ nghiệp vụ Nhập/Xuất Excel cho Thẻ định danh (CARDS)
    /// </summary>
    public class CardExcelService : ICardExcelService
    {
        private const int MaxExportLimit = 10000;

        private readonly IExcelService _excelService;
        private readonly ICardService _cardService;
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<ParkingSession>? _sessionRepo;
        private readonly IRepository<Tariff>? _tariffRepo;
        private readonly IAuditLogService? _auditLogService;
        private readonly ILogger<CardExcelService> _logger;

        public CardExcelService(
            IExcelService excelService,
            ICardService cardService,
            IRepository<Card> cardRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            ILogger<CardExcelService> logger,
            IAuditLogService? auditLogService = null,
            IRepository<ParkingSession>? sessionRepo = null,
            IRepository<Tariff>? tariffRepo = null)
        {
            _excelService = excelService;
            _cardService = cardService;
            _cardRepo = cardRepo;
            _clientRepo = clientRepo;
            _vehicleRepo = vehicleRepo;
            _logger = logger;
            _auditLogService = auditLogService;
            _sessionRepo = sessionRepo;
            _tariffRepo = tariffRepo;
        }

        public CardExcelService(
            IExcelService excelService,
            ICardService cardService,
            IRepository<Card> cardRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<ParkingSession> sessionRepo,
            ILogger<CardExcelService> logger,
            IAuditLogService? auditLogService = null)
            : this(excelService, cardService, cardRepo, clientRepo, vehicleRepo, logger, auditLogService, sessionRepo, null)
        {
        }

        [ActivatorUtilitiesConstructor]
        public CardExcelService(
            IExcelService excelService,
            ICardService cardService,
            IRepository<Card> cardRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<ParkingSession> sessionRepo,
            IRepository<Tariff> tariffRepo,
            ILogger<CardExcelService> logger,
            IAuditLogService? auditLogService = null)
            : this(excelService, cardService, cardRepo, clientRepo, vehicleRepo, logger, auditLogService, sessionRepo, tariffRepo)
        {
        }

        public async Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default)
        {
            var sampleData = new CardExcelDto
            {
                CardNumber = "0012345678",
                TargetType = CardTargetType.Monthly,
                ClientCode = "NV001",
                PlateNumber = null,
                IsChargeable = false,
                TariffCode = null,
                Status = CardStatus.InUse,
                Note = "Thẻ cấp cho nhân sự mới"
            };

            var profile = new CardExcelProfile();
            return await _excelService.GenerateTemplateAsync(profile, sampleData, "The_Dinh_Danh", cancellationToken);
        }

        public async Task<ExcelImportResultDto> ImportCardsAsync(
            IFormFile file,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default)
        {
            ExcelFileValidator.Validate(file);
            using var stream = file.OpenReadStream();
            return await ImportCardsAsync(stream, dryRun, duplicateMode, cancellationToken);
        }

        public async Task<ExcelImportResultDto> ImportCardsAsync(
            Stream stream,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default)
        {
            ExcelFileValidator.Validate(stream);

            var profile = new CardExcelProfile();
            var options = new ExcelImportOptions
            {
                IsDryRun = dryRun,
                DuplicateAction = duplicateMode
            };

            var parseResult = await _excelService.ReadAsync(stream, profile, options, cancellationToken);

            var resultDto = new ExcelImportResultDto
            {
                TotalRows = parseResult.TotalRows,
                IsDryRun = dryRun
            };

            foreach (var err in parseResult.Errors)
            {
                resultDto.Errors.Add(new ExcelRowErrorDto
                {
                    Row = err.Row,
                    Column = err.Column,
                    Value = err.Value,
                    ErrorMessage = err.ErrorMessage
                });
            }

            var existingCards = await _cardRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var cardsByNumber = existingCards
                .GroupBy(c => c.CardNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var existingClients = await _clientRepo.FindAsync(c => !c.IsDeleted, cancellationToken);
            var clientsByCode = new Dictionary<string, Client>(StringComparer.OrdinalIgnoreCase);
            var clientsByPhone = new Dictionary<string, Client>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in existingClients)
            {
                if (!string.IsNullOrWhiteSpace(c.Code))
                    clientsByCode[c.Code.Trim()] = c;
                if (!string.IsNullOrWhiteSpace(c.PhoneNumber))
                    clientsByPhone[c.PhoneNumber.Trim()] = c;
            }

            var existingVehicles = await _vehicleRepo.FindAsync(v => !v.IsDeleted, cancellationToken);
            var vehiclesByPlate = new Dictionary<string, Vehicle>(StringComparer.OrdinalIgnoreCase);
            foreach (var v in existingVehicles)
            {
                var clean = PlateHelper.Normalize(v.PlateNumber);
                if (!string.IsNullOrEmpty(clean))
                    vehiclesByPlate[clean] = v;
            }

            var tariffMapByCode = new Dictionary<string, Tariff>(StringComparer.OrdinalIgnoreCase);
            if (_tariffRepo != null)
            {
                var existingTariffs = await _tariffRepo.FindAsync(t => !t.IsDeleted && t.IsActive, cancellationToken);
                foreach (var t in existingTariffs)
                {
                    if (!string.IsNullOrWhiteSpace(t.Code))
                        tariffMapByCode[t.Code.Trim()] = t;
                }
            }

            var seenCardsInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var activeCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_sessionRepo != null)
            {
                var activeSessions = await _sessionRepo.FindAsync(
                    s => s.Status == ParkingSessionStatus.Active && !s.IsDeleted,
                    cancellationToken);
                foreach (var s in activeSessions)
                {
                    if (!string.IsNullOrEmpty(s.CardId))
                    {
                        activeCardIds.Add(s.CardId);
                    }
                }
            }

            int rowIndex = 1;
            foreach (var row in parseResult.SuccessData)
            {
                rowIndex++;
                bool rowHasError = false;

                string normalizedCard = CardHelper.NormalizeCardCode(row.CardNumber);
                if (!CardHelper.IsValidCardCode(row.CardNumber) || string.IsNullOrWhiteSpace(normalizedCard))
                {
                    resultDto.Errors.Add(new ExcelRowErrorDto
                    {
                        Row = rowIndex,
                        Column = "Mã thẻ (10 số)",
                        Value = row.CardNumber,
                        ErrorMessage = "Mã thẻ không hợp lệ hoặc để trống. Mã thẻ phải khác 0 và có độ dài tối đa 10 chữ số."
                    });
                    rowHasError = true;
                }
                else if (!seenCardsInFile.Add(normalizedCard))
                {
                    resultDto.Errors.Add(new ExcelRowErrorDto
                    {
                        Row = rowIndex,
                        Column = "Mã thẻ (10 số)",
                        Value = row.CardNumber,
                        ErrorMessage = $"Mã thẻ '{normalizedCard}' bị trùng lặp nhiều lần trong cùng tệp Excel."
                    });
                    rowHasError = true;
                }

                var targetType = row.TargetType ?? CardTargetType.Monthly;
                string? resolvedClientId = null;
                Client? matchedClient = null;
                string? resolvedVehicleId = null;

                if (targetType.IsClientBound())
                {
                    if (!string.IsNullOrWhiteSpace(row.ClientCode))
                    {
                        var key = row.ClientCode.Trim();
                        if (clientsByCode.TryGetValue(key, out matchedClient) ||
                            clientsByPhone.TryGetValue(key, out matchedClient))
                        {
                            resolvedClientId = matchedClient.Id;
                        }
                        else
                        {
                            resultDto.Errors.Add(new ExcelRowErrorDto
                            {
                                Row = rowIndex,
                                Column = "Mã nhân sự",
                                Value = row.ClientCode,
                                ErrorMessage = $"Mã nhân sự '{row.ClientCode}' không tồn tại trong hệ thống."
                            });
                            rowHasError = true;
                        }
                    }

                    // Xử lý Biển số xe đối với Thẻ khách hàng (xe cá nhân)
                    if (!string.IsNullOrWhiteSpace(row.PlateNumber))
                    {
                        var cleanPlate = PlateHelper.Normalize(row.PlateNumber);
                        if (string.IsNullOrWhiteSpace(cleanPlate))
                        {
                            resultDto.Errors.Add(new ExcelRowErrorDto
                            {
                                Row = rowIndex,
                                Column = "Biển số xe",
                                Value = row.PlateNumber,
                                ErrorMessage = $"Biển số xe '{row.PlateNumber}' không đúng định dạng hợp lệ."
                            });
                            rowHasError = true;
                        }
                        else if (matchedClient == null)
                        {
                            resultDto.Errors.Add(new ExcelRowErrorDto
                            {
                                Row = rowIndex,
                                Column = "Biển số xe",
                                Value = row.PlateNumber,
                                ErrorMessage = "Không thể liên kết biển số xe cho thẻ khi chưa chỉ định mã nhân sự hợp lệ."
                            });
                            rowHasError = true;
                        }
                        else if (vehiclesByPlate.TryGetValue(cleanPlate, out var matchedVehicle))
                        {
                            if (matchedVehicle.IsShared)
                            {
                                resultDto.Errors.Add(new ExcelRowErrorDto
                                {
                                    Row = rowIndex,
                                    Column = "Biển số xe",
                                    Value = row.PlateNumber,
                                    ErrorMessage = $"Biển số xe '{row.PlateNumber}' là phương tiện nội bộ dùng chung, không thể liên kết làm xe cá nhân của nhân sự."
                                });
                                rowHasError = true;
                            }
                            else if (!string.IsNullOrWhiteSpace(matchedVehicle.OwnerClientId) && matchedVehicle.OwnerClientId != resolvedClientId)
                            {
                                resultDto.Errors.Add(new ExcelRowErrorDto
                                {
                                    Row = rowIndex,
                                    Column = "Biển số xe",
                                    Value = row.PlateNumber,
                                    ErrorMessage = $"Biển số xe '{row.PlateNumber}' đã thuộc sở hữu của nhân sự khác."
                                });
                                rowHasError = true;
                            }
                            else
                            {
                                resolvedVehicleId = matchedVehicle.Id;
                                if (!dryRun && matchedVehicle.OwnerClientId != resolvedClientId)
                                {
                                    matchedVehicle.OwnerClientId = resolvedClientId;
                                    await _vehicleRepo.UpdateAsync(matchedVehicle, cancellationToken);
                                }
                            }
                        }
                        else
                        {
                            // Phương tiện chưa tồn tại: Tự động khởi tạo xe cá nhân cho nhân sự
                            var newPersonalVehicle = new Vehicle
                            {
                                PlateNumber = cleanPlate,
                                Type = VehicleType.Car,
                                OwnerClientId = resolvedClientId,
                                IsShared = false,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };

                            if (!dryRun)
                            {
                                await _vehicleRepo.AddAsync(newPersonalVehicle, cancellationToken);
                            }

                            vehiclesByPlate[cleanPlate] = newPersonalVehicle;
                            resolvedVehicleId = newPersonalVehicle.Id;
                        }
                    }
                }
                else if (targetType == CardTargetType.Vehicle)
                {
                    if (!string.IsNullOrWhiteSpace(row.PlateNumber))
                    {
                        var cleanPlate = PlateHelper.Normalize(row.PlateNumber);
                        if (vehiclesByPlate.TryGetValue(cleanPlate, out var matchedVehicle))
                        {
                            resolvedVehicleId = matchedVehicle.Id;
                        }
                        else
                        {
                            resultDto.Errors.Add(new ExcelRowErrorDto
                            {
                                Row = rowIndex,
                                Column = "Biển số xe",
                                Value = row.PlateNumber,
                                ErrorMessage = $"Biển số xe '{row.PlateNumber}' không tồn tại trong hệ thống."
                            });
                            rowHasError = true;
                        }
                    }
                }

                bool isChargeable = row.IsChargeable ?? false;
                string? resolvedTariffId = null;

                if (targetType == CardTargetType.Vehicle)
                {
                    if (row.IsChargeable == true || !string.IsNullOrWhiteSpace(row.TariffCode))
                    {
                        resultDto.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Thu phí",
                            Value = row.IsChargeable?.ToString() ?? row.TariffCode,
                            ErrorMessage = "Thẻ phương tiện nội bộ không áp dụng thu phí hoặc gán biểu phí."
                        });
                        rowHasError = true;
                    }
                }
                else
                {
                    if (row.IsChargeable == false && !string.IsNullOrWhiteSpace(row.TariffCode))
                    {
                        resultDto.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Mã biểu phí",
                            Value = row.TariffCode,
                            ErrorMessage = "Không thể chỉ định mã biểu phí khi cột 'Thu phí' là False/Không."
                        });
                        rowHasError = true;
                    }
                    else if (isChargeable || (!row.IsChargeable.HasValue && !string.IsNullOrWhiteSpace(row.TariffCode)))
                    {
                        isChargeable = true;
                        if (string.IsNullOrWhiteSpace(row.TariffCode))
                        {
                            resultDto.Errors.Add(new ExcelRowErrorDto
                            {
                                Row = rowIndex,
                                Column = "Mã biểu phí",
                                Value = row.TariffCode,
                                ErrorMessage = "Vui lòng chỉ định mã biểu phí khi bật thu phí."
                            });
                            rowHasError = true;
                        }
                        else if (!tariffMapByCode.TryGetValue(row.TariffCode.Trim(), out var matchedTariff))
                        {
                            resultDto.Errors.Add(new ExcelRowErrorDto
                            {
                                Row = rowIndex,
                                Column = "Mã biểu phí",
                                Value = row.TariffCode,
                                ErrorMessage = $"Mã biểu phí '{row.TariffCode}' không tồn tại hoặc đã ngừng hoạt động."
                            });
                            rowHasError = true;
                        }
                        else
                        {
                            resolvedTariffId = matchedTariff.Id;
                        }
                    }
                }

                if (rowHasError)
                {
                    resultDto.FailedCount++;
                    continue;
                }

                CardStatus resolvedStatus;
                if (row.Status.HasValue)
                {
                    resolvedStatus = row.Status.Value;
                }
                else if (resolvedClientId != null || resolvedVehicleId != null)
                {
                    resolvedStatus = CardStatus.InUse;
                }
                else
                {
                    resolvedStatus = CardStatus.Available;
                }

                bool cardExists = cardsByNumber.TryGetValue(normalizedCard, out var existingCard);
                if (cardExists && existingCard != null)
                {
                    if (duplicateMode == DuplicateMode.Skip)
                    {
                        resultDto.SkippedCount++;
                        continue;
                    }

                    if (duplicateMode == DuplicateMode.Error)
                    {
                        resultDto.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Mã thẻ (10 số)",
                            Value = row.CardNumber,
                            ErrorMessage = $"Mã thẻ '{normalizedCard}' đã tồn tại trên hệ thống."
                        });
                        resultDto.FailedCount++;
                        continue;
                    }

                    // Kiểm tra ràng buộc thẻ đang có phiên đỗ xe Active trong bãi (Quy tắc 9 / ADR 0043)
                    if (activeCardIds.Contains(existingCard.Id))
                    {
                        bool isChangingBinding =
                            existingCard.TargetType != targetType ||
                            (targetType.IsClientBound() && existingCard.ClientId != resolvedClientId) ||
                            (targetType == CardTargetType.Vehicle && existingCard.VehicleId != resolvedVehicleId);

                        bool isInvalidatingStatus =
                            row.Status.HasValue &&
                            row.Status.Value == CardStatus.Available &&
                            existingCard.Status != CardStatus.Available;

                        bool isChangingTariff =
                            existingCard.IsChargeable != isChargeable ||
                            (existingCard.TariffId ?? string.Empty) != (resolvedTariffId ?? string.Empty);

                        if (isChangingBinding || isInvalidatingStatus || isChangingTariff)
                        {
                            resultDto.Errors.Add(new ExcelRowErrorDto
                            {
                                Row = rowIndex,
                                Column = "Mã thẻ (10 số)",
                                Value = row.CardNumber,
                                ErrorMessage = $"Thẻ '{normalizedCard}' hiện đang có xe trong bãi (phiên đỗ xe đang hoạt động). Không thể thay đổi đối tượng gán, chủ xe, trạng thái kho hoặc biểu phí/thu phí."
                            });
                            resultDto.FailedCount++;
                            continue;
                        }
                    }

                    // DuplicateMode.Update
                    if (!dryRun)
                    {
                        // 1. Cập nhật hồ sơ Client nếu thay đổi đối tượng gán
                        if (existingCard.ClientId != resolvedClientId)
                        {
                            if (!string.IsNullOrWhiteSpace(existingCard.ClientId))
                            {
                                var oldClient = await _clientRepo.GetByIdAsync(existingCard.ClientId, cancellationToken);
                                if (oldClient != null && oldClient.CardCode == normalizedCard)
                                {
                                    oldClient.CardCode = string.Empty;
                                    await _clientRepo.UpdateAsync(oldClient, cancellationToken);
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(resolvedClientId) && matchedClient != null)
                            {
                                await _cardService.RevokeClientCardsAsync(resolvedClientId, excludeCardId: existingCard.Id, cancellationToken: cancellationToken);
                                matchedClient.CardCode = normalizedCard;
                                await _clientRepo.UpdateAsync(matchedClient, cancellationToken);

                                foreach (var c in cardsByNumber.Values)
                                {
                                    if (c.ClientId == resolvedClientId && c.IsClientBound() && c.Id != existingCard.Id)
                                    {
                                        c.ClientId = null;
                                        c.Status = CardStatus.Available;
                                    }
                                }
                            }
                        }

                        // 2. Thu hồi thẻ phương tiện cũ nếu gán xe nội bộ mới
                        if (targetType == CardTargetType.Vehicle && existingCard.VehicleId != resolvedVehicleId && !string.IsNullOrWhiteSpace(resolvedVehicleId))
                        {
                            await _cardService.RevokeVehicleCardsAsync(resolvedVehicleId, excludeCardId: existingCard.Id, cancellationToken: cancellationToken);

                            foreach (var c in cardsByNumber.Values)
                            {
                                if (c.VehicleId == resolvedVehicleId && c.TargetType == CardTargetType.Vehicle && c.Id != existingCard.Id)
                                {
                                    c.VehicleId = null;
                                    c.Status = CardStatus.Available;
                                }
                            }
                        }

                        existingCard.TargetType = targetType;
                        existingCard.ClientId = targetType.IsClientBound() ? resolvedClientId : null;
                        existingCard.VehicleId = resolvedVehicleId;
                        existingCard.IsChargeable = isChargeable;
                        existingCard.TariffId = resolvedTariffId;
                        existingCard.Status = resolvedStatus;
                        if (!string.IsNullOrWhiteSpace(row.Note)) existingCard.Note = row.Note;
                        existingCard.UpdatedAt = DateTime.UtcNow;

                        await _cardRepo.UpdateAsync(existingCard, cancellationToken);
                    }

                    resultDto.SuccessCount++;
                    continue;
                }

                // Tạo mới Thẻ
                if (!dryRun)
                {
                    if (targetType.IsClientBound() && !string.IsNullOrWhiteSpace(resolvedClientId) && matchedClient != null)
                    {
                        await _cardService.RevokeClientCardsAsync(resolvedClientId, cancellationToken: cancellationToken);
                        matchedClient.CardCode = normalizedCard;
                        await _clientRepo.UpdateAsync(matchedClient, cancellationToken);

                        foreach (var c in cardsByNumber.Values)
                        {
                            if (c.ClientId == resolvedClientId && c.IsClientBound())
                            {
                                c.ClientId = null;
                                c.Status = CardStatus.Available;
                            }
                        }
                    }
                    else if (targetType == CardTargetType.Vehicle && !string.IsNullOrWhiteSpace(resolvedVehicleId))
                    {
                        await _cardService.RevokeVehicleCardsAsync(resolvedVehicleId, cancellationToken: cancellationToken);

                        foreach (var c in cardsByNumber.Values)
                        {
                            if (c.VehicleId == resolvedVehicleId && c.TargetType == CardTargetType.Vehicle)
                            {
                                c.VehicleId = null;
                                c.Status = CardStatus.Available;
                            }
                        }
                    }

                    var newCard = new Card
                    {
                        CardNumber = normalizedCard,
                        TargetType = targetType,
                        ClientId = targetType.IsClientBound() ? resolvedClientId : null,
                        VehicleId = resolvedVehicleId,
                        IsChargeable = isChargeable,
                        TariffId = resolvedTariffId,
                        Status = resolvedStatus,
                        Note = row.Note,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _cardRepo.AddAsync(newCard, cancellationToken);
                    cardsByNumber[normalizedCard] = newCard;
                }

                resultDto.SuccessCount++;
            }

            resultDto.FailedCount += parseResult.FailedCount;

            _logger.LogInformation("Nhập liệu Excel Thẻ định danh hoàn tất (DryRun: {DryRun}, Mode: {Mode}): {Success} thành công, {Failed} thất bại, {Skipped} bỏ qua.",
                dryRun, duplicateMode, resultDto.SuccessCount, resultDto.FailedCount, resultDto.SkippedCount);

            if (!dryRun && resultDto.SuccessCount > 0 && _auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    AuditActionType.Create,
                    "Card",
                    "BATCH_IMPORT",
                    $"{resultDto.SuccessCount} thẻ định danh",
                    reason: $"Nhập hàng loạt thẻ định danh từ tệp Excel: {resultDto.SuccessCount} thành công, {resultDto.FailedCount} thất bại, {resultDto.SkippedCount} bỏ qua.",
                    cancellationToken: cancellationToken);
            }

            return resultDto;
        }

        public async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportCardsAsync(
            CardFilterQuery query,
            CancellationToken cancellationToken = default)
        {
            var (cards, isTruncated) = await _cardService.GetCardsForExportAsync(query, MaxExportLimit, cancellationToken);

            var exportDtos = cards.Select(c =>
            {
                string targetTypeDesc = c.TargetType switch
                {
                    CardTargetType.Vehicle => "Phương tiện nội bộ",
                    CardTargetType.Visitor => "Thẻ lượt (Khách)",
                    CardTargetType.Monthly => "Thẻ tháng",
                    CardTargetType.VIP => "Thẻ VIP",
                    _ => "Không xác định"
                };
                string? assignedCode = null;
                string? assignedName = null;

                if (c.TargetType.IsClientBound() && !string.IsNullOrWhiteSpace(c.ClientId))
                {
                    assignedCode = c.ClientCode;
                    assignedName = c.ClientName;
                }
                else if (c.TargetType == CardTargetType.Vehicle && !string.IsNullOrWhiteSpace(c.VehicleId))
                {
                    assignedCode = c.PlateNumber;
                    assignedName = c.PlateNumber;
                }

                string statusDesc = c.Status switch
                {
                    CardStatus.InUse => "Đang sử dụng",
                    CardStatus.Available => "Trong kho",
                    CardStatus.Locked => "Tạm khóa",
                    CardStatus.Lost => "Báo mất",
                    _ => c.Status.ToString()
                };

                return new CardExportExcelDto
                {
                    CardNumber = c.CardNumber,
                    TargetType = targetTypeDesc,
                    AssignedCode = assignedCode,
                    AssignedName = assignedName,
                    IsChargeable = c.IsChargeable,
                    TariffCode = c.TariffCode,
                    TariffName = c.TariffName,
                    Status = statusDesc,
                    Note = c.Note,
                    CreatedAt = c.CreatedAt
                };
            }).ToList();

            var profile = new CardExportExcelProfile();
            var bytes = await _excelService.WriteAsync(exportDtos, profile, "The_Dinh_Danh", "DANH SÁCH THẺ ĐỊNH DANH HỆ THỐNG", cancellationToken);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    AuditActionType.Export,
                    "Card",
                    "ALL",
                    $"Xuất {exportDtos.Count} thẻ định danh",
                    reason: $"Xuất danh sách {exportDtos.Count} thẻ định danh ra tệp Excel.",
                    cancellationToken: cancellationToken);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"danh_sach_the_dinh_danh_{timestamp}.xlsx";

            return (bytes, fileName, isTruncated);
        }
    }
}
