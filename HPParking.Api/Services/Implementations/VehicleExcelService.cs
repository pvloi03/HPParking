using HPParking.Api.Common.Excel;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Excel.Vehicles;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Triển khai dịch vụ nghiệp vụ Nhập/Xuất Excel cho Phương tiện (VEHICLES - Độc lập, chỉ trường cơ bản, ADR 0023)
    /// </summary>
    public class VehicleExcelService : IVehicleExcelService
    {
        private const int MaxExportLimit = 10000;

        private readonly IExcelService _excelService;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly ILogger<VehicleExcelService> _logger;

        public VehicleExcelService(
            IExcelService excelService,
            IRepository<Vehicle> vehicleRepo,
            IRepository<Client> clientRepo,
            ILogger<VehicleExcelService> logger)
        {
            _excelService = excelService;
            _vehicleRepo = vehicleRepo;
            _clientRepo = clientRepo;
            _logger = logger;
        }

        public async Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default)
        {
            var sampleData = new VehicleExcelDto
            {
                PlateNumber = "30A-12345",
                Type = VehicleType.Car,
                OwnerClientCode = "NV001",
                IsActive = true
            };

            var profile = new VehicleExcelProfile();
            return await _excelService.GenerateTemplateAsync(profile, sampleData, "Template", cancellationToken);
        }

        public async Task<ExcelImportResultDto> ImportVehiclesAsync(
            IFormFile file,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default)
        {
            ExcelFileValidator.Validate(file);
            using var stream = file.OpenReadStream();
            return await ImportVehiclesAsync(stream, dryRun, duplicateMode, cancellationToken);
        }

        public async Task<ExcelImportResultDto> ImportVehiclesAsync(
            Stream stream,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default)
        {
            ExcelFileValidator.Validate(stream);

            var profile = new VehicleExcelProfile();
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

            var existingVehicles = await _vehicleRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var vehiclesByPlate = existingVehicles.ToDictionary(v => v.PlateNumber, v => v);

            var clientsByCode = (await _clientRepo.FindAsync(c => !c.IsDeleted && !string.IsNullOrEmpty(c.Code), cancellationToken))
                .ToDictionary(c => c.Code.Trim().ToLowerInvariant(), c => c);

            var seenPlatesInFile = new HashSet<string>();

            int rowIndex = 1;
            foreach (var row in parseResult.SuccessData)
            {
                rowIndex++;
                bool rowHasError = false;

                if (string.IsNullOrWhiteSpace(row.PlateNumber))
                {
                    resultDto.Errors.Add(new ExcelRowErrorDto
                    {
                        Row = rowIndex,
                        Column = "Biển số xe",
                        Value = string.Empty,
                        ErrorMessage = "Biển số xe không được để trống."
                    });
                    rowHasError = true;
                }

                var cleanPlate = PlateHelper.Normalize(row.PlateNumber ?? string.Empty);
                if (!seenPlatesInFile.Add(cleanPlate))
                {
                    resultDto.Errors.Add(new ExcelRowErrorDto
                    {
                        Row = rowIndex,
                        Column = "Biển số xe",
                        Value = row.PlateNumber,
                        ErrorMessage = "Biển số xe bị trùng lặp nhiều lần trong cùng tệp Excel."
                    });
                    rowHasError = true;
                }

                string? ownerClientId = null;
                if (!string.IsNullOrWhiteSpace(row.OwnerClientCode))
                {
                    var clientKey = row.OwnerClientCode.Trim().ToLowerInvariant();
                    if (!clientsByCode.TryGetValue(clientKey, out var matchedClient))
                    {
                        resultDto.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Mã chủ xe",
                            Value = row.OwnerClientCode,
                            ErrorMessage = $"Mã chủ xe '{row.OwnerClientCode}' không tồn tại trong hệ thống."
                        });
                        rowHasError = true;
                    }
                    else
                    {
                        ownerClientId = matchedClient.Id;
                    }
                }

                if (rowHasError)
                {
                    resultDto.FailedCount++;
                    continue;
                }

                bool plateExists = vehiclesByPlate.TryGetValue(cleanPlate, out var existingVeh);
                if (plateExists && existingVeh != null)
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
                            Column = "Biển số xe",
                            Value = row.PlateNumber,
                            ErrorMessage = $"Biển số xe '{cleanPlate}' đã tồn tại trên hệ thống."
                        });
                        resultDto.FailedCount++;
                        continue;
                    }

                    // DuplicateMode.Update
                    if (!dryRun)
                    {
                        if (row.Type.HasValue) existingVeh.Type = row.Type.Value;
                        if (!string.IsNullOrWhiteSpace(row.OwnerClientCode)) existingVeh.OwnerClientId = ownerClientId;
                        if (row.IsActive.HasValue) existingVeh.IsActive = row.IsActive.Value;
                        existingVeh.UpdatedAt = DateTime.UtcNow;

                        await _vehicleRepo.UpdateAsync(existingVeh, cancellationToken);
                    }

                    resultDto.SuccessCount++;
                    continue;
                }

                if (!dryRun)
                {
                    var newVehicle = new Vehicle
                    {
                        PlateNumber = cleanPlate,
                        Type = row.Type ?? VehicleType.Car,
                        OwnerClientId = ownerClientId,
                        IsActive = row.IsActive ?? true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _vehicleRepo.AddAsync(newVehicle, cancellationToken);
                    vehiclesByPlate[cleanPlate] = newVehicle;
                }

                resultDto.SuccessCount++;
            }

            resultDto.FailedCount += parseResult.FailedCount;

            _logger.LogInformation("Nhập liệu Excel Phương tiện hoàn tất (DryRun: {DryRun}, Mode: {Mode}): {Success} thành công, {Failed} thất bại, {Skipped} bỏ qua.",
                dryRun, duplicateMode, resultDto.SuccessCount, resultDto.FailedCount, resultDto.SkippedCount);

            return resultDto;
        }

        public async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportVehiclesAsync(
            VehicleFilterQuery query,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildVehicleFilter(query);

            var totalCount = await _vehicleRepo.CountAsync(filter, cancellationToken: cancellationToken);
            bool isTruncated = totalCount > MaxExportLimit;
            int take = (int)Math.Min(totalCount, MaxExportLimit);

            var sort = Builders<Vehicle>.Sort.Descending(x => x.CreatedAt);
            var vehicles = await _vehicleRepo.FindAsync(filter, sort, skip: 0, limit: take, cancellationToken: cancellationToken);

            var clientIds = vehicles.Where(v => !string.IsNullOrEmpty(v.OwnerClientId))
                                    .Select(v => v.OwnerClientId!)
                                    .Distinct()
                                    .ToHashSet();

            var clients = clientIds.Count > 0
                ? (await _clientRepo.FindAsync(c => clientIds.Contains(c.Id), cancellationToken: cancellationToken)).ToDictionary(c => c.Id, c => c)
                : new Dictionary<string, Client>();

            var exportList = vehicles.Select(v => new VehicleExportExcelDto
            {
                PlateNumber = v.PlateNumber,
                Type = v.Type,
                OwnerClientCode = v.OwnerClientId != null && clients.TryGetValue(v.OwnerClientId, out var client) ? client.Code : null,
                OwnerClientName = v.OwnerClientId != null && clients.TryGetValue(v.OwnerClientId, out var c) ? c.Name : null,
                IsActive = v.IsActive
            }).ToList();

            var profile = new VehicleExportExcelProfile();
            var fileBytes = await _excelService.WriteAsync(exportList, profile, "Vehicles", "DANH SÁCH THÔNG TIN PHƯƠNG TIỆN", cancellationToken);
            string fileName = $"vehicles_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Xuất Excel Phương tiện: {Count} dòng (Truncated: {IsTruncated}).", exportList.Count, isTruncated);

            return (fileBytes, fileName, isTruncated);
        }

        private static FilterDefinition<Vehicle> BuildVehicleFilter(VehicleFilterQuery query)
        {
            var builder = Builders<Vehicle>.Filter;
            var filters = new List<FilterDefinition<Vehicle>>
            {
                builder.Eq(x => x.IsDeleted, false)
            };

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var regex = new BsonRegularExpression(Regex.Escape(query.Keyword.Trim()), "i");
                filters.Add(builder.Regex(x => x.PlateNumber, regex));
            }

            if (query.Type.HasValue)
            {
                filters.Add(builder.Eq(x => x.Type, query.Type.Value));
            }

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(x => x.IsActive, query.IsActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.OwnerClientId))
            {
                filters.Add(builder.Eq(x => x.OwnerClientId, query.OwnerClientId));
            }

            return builder.And(filters);
        }
    }
}
