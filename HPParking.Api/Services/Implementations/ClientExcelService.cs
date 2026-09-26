using HPParking.Api.Common.Excel;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Excel.Clients;
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
    /// Triển khai dịch vụ nghiệp vụ Nhập/Xuất Excel cho Khách hàng (Độc lập, chỉ trường cơ bản, ADR 0023)
    /// </summary>
    public class ClientExcelService : IClientExcelService
    {
        private const int MaxExportLimit = 10000;

        private readonly IExcelService _excelService;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Contractor> _contractorRepo;
        private readonly ILogger<ClientExcelService> _logger;

        public ClientExcelService(
            IExcelService excelService,
            IRepository<Client> clientRepo,
            IRepository<Company> companyRepo,
            IRepository<Department> departmentRepo,
            IRepository<Contractor> contractorRepo,
            ILogger<ClientExcelService> logger)
        {
            _excelService = excelService;
            _clientRepo = clientRepo;
            _companyRepo = companyRepo;
            _departmentRepo = departmentRepo;
            _contractorRepo = contractorRepo;
            _logger = logger;
        }

        public async Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default)
        {
            var sampleData = new ClientExcelDto
            {
                Code = "NV001",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                BirthDay = new DateTime(1990, 1, 15),
                Address = "Hà Nội",
                Email = "nguyenvana@example.com",
                Type = ClientType.Employee,
                CompanyCode = "CTY-HP",
                DepartmentCode = "PB-KT",
                ContractorCode = null,
                IsActive = true
            };

            var profile = new ClientExcelProfile();
            return await _excelService.GenerateTemplateAsync(profile, sampleData, "Template", cancellationToken);
        }

        public async Task<ExcelImportResultDto> ImportClientsAsync(
            IFormFile file,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default)
        {
            ExcelFileValidator.Validate(file);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var profile = new ClientExcelProfile();
            var options = new ExcelImportOptions
            {
                IsDryRun = dryRun,
                DuplicateAction = duplicateMode
            };

            var parseResult = await _excelService.ReadAsync(memoryStream, profile, options, cancellationToken);

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

            var existingClients = await _clientRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var clientsByPhone = existingClients.ToDictionary(c => c.PhoneNumber, c => c);

            var companiesByCode = (await _companyRepo.FindAsync(x => !x.IsDeleted && !string.IsNullOrEmpty(x.Code), cancellationToken))
                .ToDictionary(c => c.Code.Trim().ToLowerInvariant(), c => c);
            var departmentsByCode = (await _departmentRepo.FindAsync(x => !x.IsDeleted && !string.IsNullOrEmpty(x.Code), cancellationToken))
                .ToDictionary(d => d.Code.Trim().ToLowerInvariant(), d => d);
            var contractorsByCode = (await _contractorRepo.FindAsync(x => !x.IsDeleted && !string.IsNullOrEmpty(x.Code), cancellationToken))
                .ToDictionary(ct => ct.Code.Trim().ToLowerInvariant(), ct => ct);

            var seenPhonesInFile = new HashSet<string>();

            int rowIndex = 1;
            foreach (var row in parseResult.SuccessData)
            {
                rowIndex++;
                bool rowHasError = false;

                var phone = (row.PhoneNumber ?? string.Empty).Trim();
                if (!Regex.IsMatch(phone, @"^0\d{9}$"))
                {
                    resultDto.Errors.Add(new ExcelRowErrorDto
                    {
                        Row = rowIndex,
                        Column = "Số điện thoại",
                        Value = phone,
                        ErrorMessage = "Số điện thoại không đúng định dạng (bắt buộc gồm 10 chữ số bắt đầu bằng 0)."
                    });
                    rowHasError = true;
                }

                if (!seenPhonesInFile.Add(phone))
                {
                    resultDto.Errors.Add(new ExcelRowErrorDto
                    {
                        Row = rowIndex,
                        Column = "Số điện thoại",
                        Value = phone,
                        ErrorMessage = "Số điện thoại bị trùng lặp nhiều lần trong cùng tệp Excel."
                    });
                    rowHasError = true;
                }

                string? companyId = null;
                if (!string.IsNullOrWhiteSpace(row.CompanyCode))
                {
                    var cKey = row.CompanyCode.Trim().ToLowerInvariant();
                    if (!companiesByCode.TryGetValue(cKey, out var matchedComp))
                    {
                        resultDto.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Mã công ty",
                            Value = row.CompanyCode,
                            ErrorMessage = $"Mã công ty '{row.CompanyCode}' không tồn tại trong hệ thống."
                        });
                        rowHasError = true;
                    }
                    else
                    {
                        companyId = matchedComp.Id;
                    }
                }

                string? departmentId = null;
                if (!string.IsNullOrWhiteSpace(row.DepartmentCode))
                {
                    var dKey = row.DepartmentCode.Trim().ToLowerInvariant();
                    if (!departmentsByCode.TryGetValue(dKey, out var matchedDept))
                    {
                        resultDto.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Mã phòng ban",
                            Value = row.DepartmentCode,
                            ErrorMessage = $"Mã phòng ban '{row.DepartmentCode}' không tồn tại trong hệ thống."
                        });
                        rowHasError = true;
                    }
                    else
                    {
                        departmentId = matchedDept.Id;
                    }
                }

                string? contractorId = null;
                if (!string.IsNullOrWhiteSpace(row.ContractorCode))
                {
                    var ctKey = row.ContractorCode.Trim().ToLowerInvariant();
                    if (!contractorsByCode.TryGetValue(ctKey, out var matchedContractor))
                    {
                        resultDto.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Mã nhà thầu",
                            Value = row.ContractorCode,
                            ErrorMessage = $"Mã nhà thầu '{row.ContractorCode}' không tồn tại trong hệ thống."
                        });
                        rowHasError = true;
                    }
                    else
                    {
                        contractorId = matchedContractor.Id;
                    }
                }

                if (rowHasError)
                {
                    resultDto.FailedCount++;
                    continue;
                }

                bool phoneExists = clientsByPhone.TryGetValue(phone, out var existingClient);
                if (phoneExists && existingClient != null)
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
                            Column = "Số điện thoại",
                            Value = phone,
                            ErrorMessage = "Số điện thoại đã tồn tại trên hệ thống."
                        });
                        resultDto.FailedCount++;
                        continue;
                    }

                    // DuplicateMode.Update
                    if (!dryRun)
                    {
                        if (!string.IsNullOrWhiteSpace(row.Name)) existingClient.Name = row.Name.Trim();
                        if (!string.IsNullOrWhiteSpace(row.Code)) existingClient.Code = row.Code.Trim();
                        if (row.BirthDay.HasValue) existingClient.BirthDay = row.BirthDay.Value;
                        if (row.Address != null) existingClient.Address = row.Address.Trim();
                        if (row.Email != null) existingClient.Email = row.Email.Trim();
                        if (row.Type.HasValue) existingClient.Type = row.Type.Value;
                        if (!string.IsNullOrWhiteSpace(row.CompanyCode)) existingClient.CompanyId = companyId;
                        if (!string.IsNullOrWhiteSpace(row.DepartmentCode)) existingClient.DepartmentId = departmentId;
                        if (!string.IsNullOrWhiteSpace(row.ContractorCode)) existingClient.ContractorId = contractorId;
                        if (row.IsActive.HasValue) existingClient.IsActive = row.IsActive.Value;
                        existingClient.UpdatedAt = DateTime.UtcNow;

                        await _clientRepo.UpdateAsync(existingClient, cancellationToken);
                    }

                    resultDto.SuccessCount++;
                    continue;
                }

                if (!dryRun)
                {
                    var newClient = new Client
                    {
                        Code = string.IsNullOrWhiteSpace(row.Code) ? "" : row.Code.Trim(),
                        Name = (row.Name ?? string.Empty).Trim(),
                        PhoneNumber = phone,
                        BirthDay = row.BirthDay ?? DateTime.UtcNow,
                        Address = row.Address?.Trim() ?? "",
                        Email = row.Email?.Trim(),
                        Type = row.Type ?? ClientType.Employee,
                        CompanyId = companyId,
                        DepartmentId = departmentId,
                        ContractorId = contractorId,
                        IsActive = row.IsActive ?? true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _clientRepo.AddAsync(newClient, cancellationToken);
                    clientsByPhone[phone] = newClient;
                }

                resultDto.SuccessCount++;
            }

            resultDto.FailedCount += parseResult.FailedCount;

            _logger.LogInformation("Nhập liệu Excel Khách hàng hoàn tất (DryRun: {DryRun}, Mode: {Mode}): {Success} thành công, {Failed} thất bại, {Skipped} bỏ qua.",
                dryRun, duplicateMode, resultDto.SuccessCount, resultDto.FailedCount, resultDto.SkippedCount);

            return resultDto;
        }

        public async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportClientsAsync(
            ClientFilterQuery query,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildClientFilter(query);

            var totalCount = await _clientRepo.CountAsync(filter, cancellationToken: cancellationToken);
            bool isTruncated = totalCount > MaxExportLimit;
            int take = (int)Math.Min(totalCount, MaxExportLimit);

            var sort = Builders<Client>.Sort.Descending(x => x.CreatedAt);
            var clients = await _clientRepo.FindAsync(filter, sort, skip: 0, limit: take, cancellationToken: cancellationToken);

            var companies = (await _companyRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(c => c.Id, c => c.Name);
            var departments = (await _departmentRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(d => d.Id, d => d.Name);
            var contractors = (await _contractorRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(ct => ct.Id, ct => ct.Name);

            var exportList = clients.Select(client => new ClientExportExcelDto
            {
                Code = client.Code ?? string.Empty,
                Name = client.Name,
                PhoneNumber = client.PhoneNumber,
                BirthDay = client.BirthDay,
                Address = client.Address,
                Email = client.Email,
                Type = client.Type,
                CompanyName = !string.IsNullOrEmpty(client.CompanyId) && companies.TryGetValue(client.CompanyId, out var cName) ? cName : null,
                DepartmentName = !string.IsNullOrEmpty(client.DepartmentId) && departments.TryGetValue(client.DepartmentId, out var dName) ? dName : null,
                ContractorName = !string.IsNullOrEmpty(client.ContractorId) && contractors.TryGetValue(client.ContractorId, out var ctName) ? ctName : null,
                HasFaceId = !string.IsNullOrWhiteSpace(client.Avatar) ? "Đã có" : "Chưa có",
                IsActive = client.IsActive
            }).ToList();

            var profile = new ClientExportExcelProfile();
            var fileBytes = await _excelService.WriteAsync(exportList, profile, "Clients", "DANH SÁCH THÔNG TIN KHÁCH HÀNG", cancellationToken);
            string fileName = $"clients_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Xuất Excel Khách hàng: {Count} dòng (Truncated: {IsTruncated}).", exportList.Count, isTruncated);

            return (fileBytes, fileName, isTruncated);
        }

        private static FilterDefinition<Client> BuildClientFilter(ClientFilterQuery query)
        {
            var builder = Builders<Client>.Filter;
            var filters = new List<FilterDefinition<Client>>
            {
                builder.Eq(x => x.IsDeleted, false)
            };

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var regex = new BsonRegularExpression(Regex.Escape(query.Keyword.Trim()), "i");
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

            return builder.And(filters);
        }
    }
}
