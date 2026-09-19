using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.Common.Excel;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Excel.Clients;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Triển khai dịch vụ nghiệp vụ Nhập/Xuất Excel cho Khách hàng và Phương tiện (ADR 0023)
    /// </summary>
    public class ClientExcelService : IClientExcelService
    {
        private const int MaxExportLimit = 10000;

        private readonly IExcelService _excelService;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly ILogger<ClientExcelService> _logger;

        public ClientExcelService(
            IExcelService excelService,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<Company> companyRepo,
            IRepository<Department> departmentRepo,
            ILogger<ClientExcelService> logger)
        {
            _excelService = excelService;
            _clientRepo = clientRepo;
            _vehicleRepo = vehicleRepo;
            _companyRepo = companyRepo;
            _departmentRepo = departmentRepo;
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
                PlateNumber = "30A-12345",
                VehicleType = VehicleType.Car,
                CompanyName = "Công ty TNHH Hoàng Phát",
                DepartmentName = "Phòng Kỹ thuật"
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

            // Chuyển các lỗi định dạng ban đầu sang DTO
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

            // 1. Tải cache Công ty, Phòng ban và Dữ liệu hiện có để đối soát
            var companies = await _companyRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var departments = await _departmentRepo.FindAsync(x => !x.IsDeleted, cancellationToken);

            var existingClients = await _clientRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var clientsByPhone = existingClients.ToDictionary(c => c.PhoneNumber, c => c);
            var clientsByCode = existingClients.Where(c => !string.IsNullOrEmpty(c.Code)).ToDictionary(c => c.Code!, c => c);

            var existingVehicles = await _vehicleRepo.FindAsync(x => !x.IsDeleted && x.IsActive, cancellationToken);
            var vehiclesByPlate = existingVehicles.ToDictionary(v => v.PlateNumber, v => v);

            var seenPhonesInFile = new HashSet<string>();
            var seenPlatesInFile = new HashSet<string>();

            int rowIndex = 1; // Hàng dữ liệu bắt đầu từ 2
            foreach (var row in parseResult.SuccessData)
            {
                rowIndex++;
                bool rowHasError = false;

                // 2. Validate định dạng Số điện thoại (chuẩn 10 chữ số)
                var phone = row.PhoneNumber.Trim();
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

                // 3. Kiểm tra trùng số điện thoại trong chính tệp
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

                // 4. Kiểm tra biển số xe
                string? cleanPlate = null;
                if (!string.IsNullOrWhiteSpace(row.PlateNumber))
                {
                    cleanPlate = PlateHelper.Normalize(row.PlateNumber);
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
                }

                if (rowHasError)
                {
                    resultDto.FailedCount++;
                    continue;
                }

                // 5. Đối soát trùng lặp với CSDL
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
                        existingClient.UpdatedAt = DateTime.UtcNow;

                        ResolveCompanyAndDepartment(row, companies, departments, out var compId, out var deptId);
                        if (compId != null) existingClient.CompanyId = compId;
                        if (deptId != null) existingClient.DepartmentId = deptId;

                        await _clientRepo.UpdateAsync(existingClient, cancellationToken);

                        // Cập nhật phương tiện nếu có biển số
                        if (!string.IsNullOrEmpty(cleanPlate))
                        {
                            await UpsertVehicleAsync(existingClient.Id, cleanPlate, row.VehicleType ?? VehicleType.Motorbike, vehiclesByPlate, cancellationToken);
                        }
                    }

                    resultDto.SuccessCount++;
                    continue;
                }

                // 6. Tạo mới Client
                ResolveCompanyAndDepartment(row, companies, departments, out var resolvedCompId, out var resolvedDeptId);

                // Kiểm tra biển số xe đã tồn tại của xe khác chưa
                if (!string.IsNullOrEmpty(cleanPlate) && vehiclesByPlate.TryGetValue(cleanPlate, out var vehOwner))
                {
                    resultDto.Errors.Add(new ExcelRowErrorDto
                    {
                        Row = rowIndex,
                        Column = "Biển số xe",
                        Value = row.PlateNumber,
                        ErrorMessage = $"Biển số xe '{cleanPlate}' đã được đăng ký cho phương tiện khác trên hệ thống."
                    });
                    resultDto.FailedCount++;
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
                        CompanyId = resolvedCompId,
                        DepartmentId = resolvedDeptId,
                        Type = ClientType.Employee,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _clientRepo.AddAsync(newClient, cancellationToken);
                    clientsByPhone[phone] = newClient;

                    if (!string.IsNullOrEmpty(cleanPlate))
                    {
                        var newVehicle = new Vehicle
                        {
                            PlateNumber = cleanPlate,
                            Type = row.VehicleType ?? VehicleType.Motorbike,
                            OwnerClientId = newClient.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _vehicleRepo.AddAsync(newVehicle, cancellationToken);
                        vehiclesByPlate[cleanPlate] = newVehicle;
                    }
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

            var clientIds = clients.Select(c => c.Id).ToHashSet();
            var vehicles = await _vehicleRepo.FindAsync(v => !v.IsDeleted && !string.IsNullOrEmpty(v.OwnerClientId) && clientIds.Contains(v.OwnerClientId!), cancellationToken);
            var vehiclesByClient = vehicles.GroupBy(v => v.OwnerClientId!).ToDictionary(g => g.Key, g => g.ToList());

            var companies = (await _companyRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(c => c.Id, c => c.Name);
            var departments = (await _departmentRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(d => d.Id, d => d.Name);

            var exportList = new List<ClientExcelDto>();

            foreach (var client in clients)
            {
                string companyName = !string.IsNullOrEmpty(client.CompanyId) && companies.TryGetValue(client.CompanyId, out var cName) ? cName : string.Empty;
                string deptName = !string.IsNullOrEmpty(client.DepartmentId) && departments.TryGetValue(client.DepartmentId, out var dName) ? dName : string.Empty;

                if (vehiclesByClient.TryGetValue(client.Id, out var clientVehicles) && clientVehicles.Count > 0)
                {
                    foreach (var v in clientVehicles)
                    {
                        exportList.Add(new ClientExcelDto
                        {
                            Code = client.Code ?? string.Empty,
                            Name = client.Name,
                            PhoneNumber = client.PhoneNumber,
                            BirthDay = client.BirthDay,
                            Address = client.Address,
                            PlateNumber = v.PlateNumber,
                            VehicleType = v.Type,
                            CompanyName = companyName,
                            DepartmentName = deptName
                        });
                    }
                }
                else
                {
                    exportList.Add(new ClientExcelDto
                    {
                        Code = client.Code ?? string.Empty,
                        Name = client.Name,
                        PhoneNumber = client.PhoneNumber,
                        BirthDay = client.BirthDay,
                        Address = client.Address,
                        PlateNumber = string.Empty,
                        VehicleType = null,
                        CompanyName = companyName,
                        DepartmentName = deptName
                    });
                }
            }

            var profile = new ClientExcelProfile();
            var fileBytes = await _excelService.WriteAsync(exportList, profile, "Clients", cancellationToken);
            string fileName = $"clients_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Xuất Excel Khách hàng: {Count} dòng (Truncated: {IsTruncated}).", exportList.Count, isTruncated);

            return (fileBytes, fileName, isTruncated);
        }

        private static void ResolveCompanyAndDepartment(
            ClientExcelDto row,
            IReadOnlyList<Company> companies,
            IReadOnlyList<Department> departments,
            out string? compId,
            out string? deptId)
        {
            compId = null;
            deptId = null;

            if (!string.IsNullOrWhiteSpace(row.CompanyName))
            {
                var cleanComp = row.CompanyName.Trim();
                var matchedComp = companies.FirstOrDefault(c =>
                    string.Equals(c.Name, cleanComp, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Code, cleanComp, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Id, cleanComp, StringComparison.OrdinalIgnoreCase));
                compId = matchedComp?.Id;
            }

            var localCompId = compId;
            if (!string.IsNullOrWhiteSpace(row.DepartmentName))
            {
                var cleanDept = row.DepartmentName.Trim();
                var matchedDept = departments.FirstOrDefault(d =>
                    (localCompId == null || d.CompanyId == localCompId) &&
                    (string.Equals(d.Name, cleanDept, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(d.Code, cleanDept, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(d.Id, cleanDept, StringComparison.OrdinalIgnoreCase)));
                deptId = matchedDept?.Id;
                if (compId == null && matchedDept != null && !string.IsNullOrEmpty(matchedDept.CompanyId))
                {
                    compId = matchedDept.CompanyId;
                }
            }
        }

        private async Task UpsertVehicleAsync(
            string clientId,
            string plateNumber,
            VehicleType vehicleType,
            Dictionary<string, Vehicle> vehiclesByPlate,
            CancellationToken cancellationToken)
        {
            if (vehiclesByPlate.TryGetValue(plateNumber, out var existingVeh))
            {
                if (existingVeh.OwnerClientId == clientId)
                {
                    existingVeh.Type = vehicleType;
                    existingVeh.UpdatedAt = DateTime.UtcNow;
                    await _vehicleRepo.UpdateAsync(existingVeh, cancellationToken);
                }
            }
            else
            {
                var newVeh = new Vehicle
                {
                    PlateNumber = plateNumber,
                    Type = vehicleType,
                    OwnerClientId = clientId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _vehicleRepo.AddAsync(newVeh, cancellationToken);
                vehiclesByPlate[plateNumber] = newVeh;
            }
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
