using HPParking.Api.Common.Excel;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Excel.MasterData;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Hiện thực hóa dịch vụ Nhập/Xuất Excel cho danh mục Master Data (Nhóm II - ADR 0023)
    /// </summary>
    public class MasterDataExcelService : IMasterDataExcelService
    {
        private const int MaxExportLimit = 10000;

        private readonly IExcelService _excelService;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Contractor> _contractorRepo;
        private readonly IRepository<Gate> _gateRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly IRepository<Device> _deviceRepo;
        private readonly IVehicleExcelService _vehicleExcelService;
        private readonly ILogger<MasterDataExcelService> _logger;

        public MasterDataExcelService(
            IExcelService excelService,
            IRepository<Company> companyRepo,
            IRepository<Department> departmentRepo,
            IRepository<Contractor> contractorRepo,
            IRepository<Gate> gateRepo,
            IRepository<Lane> laneRepo,
            IRepository<Device> deviceRepo,
            IVehicleExcelService vehicleExcelService,
            ILogger<MasterDataExcelService> logger)
        {
            _excelService = excelService;
            _companyRepo = companyRepo;
            _departmentRepo = departmentRepo;
            _contractorRepo = contractorRepo;
            _gateRepo = gateRepo;
            _laneRepo = laneRepo;
            _deviceRepo = deviceRepo;
            _vehicleExcelService = vehicleExcelService;
            _logger = logger;
        }

        public async Task<(byte[] Content, string FileName)> GenerateTemplateAsync(string entity)
        {
            var normalized = NormalizeEntity(entity);
            return normalized switch
            {
                "companies" => (await _excelService.GenerateTemplateAsync(
                    new CompanyExcelProfile(),
                    new CompanyExcelDto { Code = "CTY-HP", Name = "Công ty Cổ phần Hưng Phát", PhoneNumber = "0243123456", Email = "contact@hungphat.vn", IsActive = true },
                    "Mau_Nhap_Cong_Ty"), "Mau_Nhap_Cong_Ty.xlsx"),

                "departments" => (await _excelService.GenerateTemplateAsync(
                    new DepartmentExcelProfile(),
                    new DepartmentExcelDto { Code = "PB-KT", Name = "Phòng Kỹ thuật", CompanyCode = "CTY-HP", ManagerName = "Nguyễn Văn A", PhoneNumber = "0987654321", Email = "kt@hungphat.vn", IsActive = true },
                    "Mau_Nhap_Phong_Ban"), "Mau_Nhap_Phong_Ban.xlsx"),

                "contractors" => (await _excelService.GenerateTemplateAsync(
                    new ContractorExcelProfile(),
                    new ContractorExcelDto { Code = "NT-XD01", Name = "Nhà thầu Xây dựng Thăng Long", ContactPerson = "Trần Đình B", PhoneNumber = "0901234567", Email = "thanglong@gmail.com", IsActive = true },
                    "Mau_Nhap_Nha_Thau"), "Mau_Nhap_Nha_Thau.xlsx"),

                "gates" => (await _excelService.GenerateTemplateAsync(
                    new GateExcelProfile(),
                    new GateExcelDto { Code = "GATE_01", Name = "Cổng Chính Nhà Máy", CompanyCode = "CTY-HP", MachineCode = "PC-GUARD-01", IsActive = true },
                    "Mau_Nhap_Cong_Kiem_Soat"), "Mau_Nhap_Cong_Kiem_Soat.xlsx"),

                "lanes" => (await _excelService.GenerateTemplateAsync(
                    new LaneExcelProfile(),
                    new LaneExcelDto { Code = "LANE_01", Name = "Làn vào xe máy Cổng chính", GateCode = "GATE_01", Direction = LaneDirection.In, OutputRelay = 1, InputReader = 1, IsActive = true },
                    "Mau_Nhap_Lan_Xe"), "Mau_Nhap_Lan_Xe.xlsx"),

                "devices" => (await _excelService.GenerateTemplateAsync(
                    new DeviceExcelProfile(),
                    new DeviceExcelDto { Code = "CAM_01", Name = "Camera biển số Làn 1", Type = DeviceType.Camera, IpAddress = "192.168.1.101", Port = 8000, UserName = "admin", IsActive = true },
                    "Mau_Nhap_Thiet_Bi"), "Mau_Nhap_Thiet_Bi.xlsx"),

                "vehicles" => (await _vehicleExcelService.GenerateTemplateAsync(), "Mau_Nhap_Phuong_Tien.xlsx"),

                _ => throw new BadRequestException($"Thực thể Master Data '{entity}' không hợp lệ. Các giá trị hỗ trợ: companies, departments, contractors, gates, lanes, devices, vehicles.")
            };
        }

        public async Task<ExcelImportResultDto> ImportAsync(
            string entity,
            Stream fileStream,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            bool dryRun = false,
            CancellationToken cancellationToken = default)
        {
            ExcelFileValidator.Validate(fileStream);

            var normalized = NormalizeEntity(entity);
            return normalized switch
            {
                "companies" => await ImportCompaniesInternalAsync(fileStream, duplicateMode, dryRun, cancellationToken),
                "departments" => await ImportDepartmentsInternalAsync(fileStream, duplicateMode, dryRun, cancellationToken),
                "contractors" => await ImportContractorsInternalAsync(fileStream, duplicateMode, dryRun, cancellationToken),
                "gates" => await ImportGatesInternalAsync(fileStream, duplicateMode, dryRun, cancellationToken),
                "lanes" => await ImportLanesInternalAsync(fileStream, duplicateMode, dryRun, cancellationToken),
                "devices" => await ImportDevicesInternalAsync(fileStream, duplicateMode, dryRun, cancellationToken),
                "vehicles" => await _vehicleExcelService.ImportVehiclesAsync(fileStream, dryRun, duplicateMode, cancellationToken),
                _ => throw new BadRequestException($"Thực thể Master Data '{entity}' không hợp lệ.")
            };
        }

        public async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportAsync(
            string entity,
            CancellationToken cancellationToken = default)
        {
            var normalized = NormalizeEntity(entity);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            return normalized switch
            {
                "companies" => await ExportCompaniesInternalAsync(timestamp, cancellationToken),
                "departments" => await ExportDepartmentsInternalAsync(timestamp, cancellationToken),
                "contractors" => await ExportContractorsInternalAsync(timestamp, cancellationToken),
                "gates" => await ExportGatesInternalAsync(timestamp, cancellationToken),
                "lanes" => await ExportLanesInternalAsync(timestamp, cancellationToken),
                "devices" => await ExportDevicesInternalAsync(timestamp, cancellationToken),
                "vehicles" => await _vehicleExcelService.ExportVehiclesAsync(new VehicleFilterQuery(), cancellationToken),
                _ => throw new BadRequestException($"Thực thể Master Data '{entity}' không hợp lệ.")
            };
        }

        #region Internal Import Logic

        private async Task<ExcelImportResultDto> ImportCompaniesInternalAsync(
            Stream stream, DuplicateMode mode, bool dryRun, CancellationToken cancellationToken)
        {
            var parseResult = await _excelService.ReadAsync(stream, new CompanyExcelProfile());
            var result = MapParseResult(parseResult);

            var existingCompanies = (await _companyRepo.GetAllAsync(cancellationToken))
                .ToDictionary(x => x.Code.Trim().ToLowerInvariant(), x => x);

            for (var i = 0; i < parseResult.SuccessData.Count; i++)
            {
                var row = parseResult.SuccessData[i];
                var rowIndex = i + 2;
                var codeKey = row.Code.Trim().ToLowerInvariant();

                if (existingCompanies.TryGetValue(codeKey, out var existing))
                {
                    if (mode == DuplicateMode.Skip)
                    {
                        result.SkippedCount++;
                        continue;
                    }
                    if (mode == DuplicateMode.Error)
                    {
                        result.Errors.Add(new ExcelRowErrorDto { Row = rowIndex, Column = "Mã công ty", Value = row.Code, ErrorMessage = "Mã công ty đã tồn tại." });
                        result.FailedCount++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        existing.Name = row.Name.Trim();
                        existing.PhoneNumber = row.PhoneNumber?.Trim();
                        existing.Email = row.Email?.Trim();
                        if (row.IsActive.HasValue) existing.IsActive = row.IsActive.Value;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _companyRepo.UpdateAsync(existing, cancellationToken);
                    }
                    result.SuccessCount++;
                    continue;
                }

                if (!dryRun)
                {
                    var newComp = new Company
                    {
                        Code = row.Code.Trim(),
                        Name = row.Name.Trim(),
                        PhoneNumber = row.PhoneNumber?.Trim(),
                        Email = row.Email?.Trim(),
                        IsActive = row.IsActive ?? true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _companyRepo.AddAsync(newComp, cancellationToken);
                    existingCompanies[codeKey] = newComp;
                }
                result.SuccessCount++;
            }

            result.FailedCount += parseResult.FailedCount;
            return result;
        }

        private async Task<ExcelImportResultDto> ImportDepartmentsInternalAsync(
            Stream stream, DuplicateMode mode, bool dryRun, CancellationToken cancellationToken)
        {
            var parseResult = await _excelService.ReadAsync(stream, new DepartmentExcelProfile());
            var result = MapParseResult(parseResult);

            var existingDepts = (await _departmentRepo.GetAllAsync(cancellationToken))
                .ToDictionary(x => x.Code.Trim().ToLowerInvariant(), x => x);

            var companiesByCode = (await _companyRepo.GetAllAsync(cancellationToken))
                .Where(c => !string.IsNullOrEmpty(c.Code))
                .ToDictionary(c => c.Code.Trim().ToLowerInvariant(), c => c);

            for (var i = 0; i < parseResult.SuccessData.Count; i++)
            {
                var row = parseResult.SuccessData[i];
                var rowIndex = i + 2;
                var codeKey = row.Code.Trim().ToLowerInvariant();

                string? companyId = null;
                if (!string.IsNullOrWhiteSpace(row.CompanyCode))
                {
                    var compKey = row.CompanyCode.Trim().ToLowerInvariant();
                    if (!companiesByCode.TryGetValue(compKey, out var matchedComp))
                    {
                        result.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Mã công ty",
                            Value = row.CompanyCode,
                            ErrorMessage = $"Mã công ty '{row.CompanyCode}' không tồn tại trong hệ thống."
                        });
                        result.FailedCount++;
                        continue;
                    }
                    companyId = matchedComp.Id;
                }

                if (existingDepts.TryGetValue(codeKey, out var existing))
                {
                    if (mode == DuplicateMode.Skip)
                    {
                        result.SkippedCount++;
                        continue;
                    }
                    if (mode == DuplicateMode.Error)
                    {
                        result.Errors.Add(new ExcelRowErrorDto { Row = rowIndex, Column = "Mã phòng ban", Value = row.Code, ErrorMessage = "Mã phòng ban đã tồn tại." });
                        result.FailedCount++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        existing.Name = row.Name.Trim();
                        if (!string.IsNullOrWhiteSpace(row.CompanyCode)) existing.CompanyId = companyId;
                        existing.ManagerName = row.ManagerName?.Trim();
                        existing.PhoneNumber = row.PhoneNumber?.Trim();
                        existing.Email = row.Email?.Trim();
                        if (row.IsActive.HasValue) existing.IsActive = row.IsActive.Value;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _departmentRepo.UpdateAsync(existing, cancellationToken);
                    }
                    result.SuccessCount++;
                    continue;
                }

                if (!dryRun)
                {
                    var newDept = new Department
                    {
                        Code = row.Code.Trim(),
                        Name = row.Name.Trim(),
                        CompanyId = companyId,
                        ManagerName = row.ManagerName?.Trim(),
                        PhoneNumber = row.PhoneNumber?.Trim(),
                        Email = row.Email?.Trim(),
                        IsActive = row.IsActive ?? true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _departmentRepo.AddAsync(newDept, cancellationToken);
                    existingDepts[codeKey] = newDept;
                }
                result.SuccessCount++;
            }

            result.FailedCount += parseResult.FailedCount;
            return result;
        }

        private async Task<ExcelImportResultDto> ImportContractorsInternalAsync(
            Stream stream, DuplicateMode mode, bool dryRun, CancellationToken cancellationToken)
        {
            var parseResult = await _excelService.ReadAsync(stream, new ContractorExcelProfile());
            var result = MapParseResult(parseResult);

            var existingContractors = (await _contractorRepo.GetAllAsync(cancellationToken))
                .ToDictionary(x => x.Code.Trim().ToLowerInvariant(), x => x);

            for (var i = 0; i < parseResult.SuccessData.Count; i++)
            {
                var row = parseResult.SuccessData[i];
                var rowIndex = i + 2;
                var codeKey = row.Code.Trim().ToLowerInvariant();

                if (existingContractors.TryGetValue(codeKey, out var existing))
                {
                    if (mode == DuplicateMode.Skip)
                    {
                        result.SkippedCount++;
                        continue;
                    }
                    if (mode == DuplicateMode.Error)
                    {
                        result.Errors.Add(new ExcelRowErrorDto { Row = rowIndex, Column = "Mã nhà thầu", Value = row.Code, ErrorMessage = "Mã nhà thầu đã tồn tại." });
                        result.FailedCount++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        existing.Name = row.Name.Trim();
                        existing.ContactPerson = row.ContactPerson?.Trim();
                        existing.PhoneNumber = row.PhoneNumber?.Trim();
                        existing.Email = row.Email?.Trim();
                        if (row.IsActive.HasValue) existing.IsActive = row.IsActive.Value;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _contractorRepo.UpdateAsync(existing, cancellationToken);
                    }
                    result.SuccessCount++;
                    continue;
                }

                if (!dryRun)
                {
                    var newContractor = new Contractor
                    {
                        Code = row.Code.Trim(),
                        Name = row.Name.Trim(),
                        ContactPerson = row.ContactPerson?.Trim(),
                        PhoneNumber = row.PhoneNumber?.Trim(),
                        Email = row.Email?.Trim(),
                        IsActive = row.IsActive ?? true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _contractorRepo.AddAsync(newContractor, cancellationToken);
                    existingContractors[codeKey] = newContractor;
                }
                result.SuccessCount++;
            }

            result.FailedCount += parseResult.FailedCount;
            return result;
        }

        private async Task<ExcelImportResultDto> ImportGatesInternalAsync(
            Stream stream, DuplicateMode mode, bool dryRun, CancellationToken cancellationToken)
        {
            var parseResult = await _excelService.ReadAsync(stream, new GateExcelProfile());
            var result = MapParseResult(parseResult);

            var existingGates = (await _gateRepo.GetAllAsync(cancellationToken))
                .ToDictionary(x => x.Code.Trim().ToLowerInvariant(), x => x);

            var companiesByCode = (await _companyRepo.GetAllAsync(cancellationToken))
                .Where(c => !string.IsNullOrEmpty(c.Code))
                .ToDictionary(c => c.Code.Trim().ToLowerInvariant(), c => c);

            for (var i = 0; i < parseResult.SuccessData.Count; i++)
            {
                var row = parseResult.SuccessData[i];
                var rowIndex = i + 2;
                var codeKey = row.Code.Trim().ToLowerInvariant();

                string? companyId = null;
                if (!string.IsNullOrWhiteSpace(row.CompanyCode))
                {
                    var compKey = row.CompanyCode.Trim().ToLowerInvariant();
                    if (!companiesByCode.TryGetValue(compKey, out var matchedComp))
                    {
                        result.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Mã công ty",
                            Value = row.CompanyCode,
                            ErrorMessage = $"Mã công ty '{row.CompanyCode}' không tồn tại trong hệ thống."
                        });
                        result.FailedCount++;
                        continue;
                    }
                    companyId = matchedComp.Id;
                }

                if (existingGates.TryGetValue(codeKey, out var existing))
                {
                    if (mode == DuplicateMode.Skip)
                    {
                        result.SkippedCount++;
                        continue;
                    }
                    if (mode == DuplicateMode.Error)
                    {
                        result.Errors.Add(new ExcelRowErrorDto { Row = rowIndex, Column = "Mã cổng", Value = row.Code, ErrorMessage = "Mã cổng đã tồn tại." });
                        result.FailedCount++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        existing.Name = row.Name.Trim();
                        if (!string.IsNullOrWhiteSpace(row.CompanyCode)) existing.CompanyId = companyId;
                        if (!string.IsNullOrWhiteSpace(row.MachineCode)) existing.MachineCode = row.MachineCode.Trim();
                        if (row.IsActive.HasValue) existing.IsActive = row.IsActive.Value;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _gateRepo.UpdateAsync(existing, cancellationToken);
                    }
                    result.SuccessCount++;
                    continue;
                }

                if (!dryRun)
                {
                    var newGate = new Gate
                    {
                        Code = row.Code.Trim(),
                        Name = row.Name.Trim(),
                        CompanyId = companyId,
                        MachineCode = row.MachineCode.Trim(),
                        IsActive = row.IsActive ?? true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _gateRepo.AddAsync(newGate, cancellationToken);
                    existingGates[codeKey] = newGate;
                }
                result.SuccessCount++;
            }

            result.FailedCount += parseResult.FailedCount;
            return result;
        }

        private async Task<ExcelImportResultDto> ImportLanesInternalAsync(
            Stream stream, DuplicateMode mode, bool dryRun, CancellationToken cancellationToken)
        {
            var parseResult = await _excelService.ReadAsync(stream, new LaneExcelProfile());
            var result = MapParseResult(parseResult);

            var existingLanes = (await _laneRepo.GetAllAsync(cancellationToken))
                .ToDictionary(x => x.Code.Trim().ToLowerInvariant(), x => x);

            var gatesByCode = (await _gateRepo.GetAllAsync(cancellationToken))
                .Where(g => !string.IsNullOrEmpty(g.Code))
                .ToDictionary(g => g.Code.Trim().ToLowerInvariant(), g => g);

            for (var i = 0; i < parseResult.SuccessData.Count; i++)
            {
                var row = parseResult.SuccessData[i];
                var rowIndex = i + 2;
                var codeKey = row.Code.Trim().ToLowerInvariant();

                string? gateId = null;
                if (!string.IsNullOrWhiteSpace(row.GateCode))
                {
                    var gateKey = row.GateCode.Trim().ToLowerInvariant();
                    if (!gatesByCode.TryGetValue(gateKey, out var matchedGate))
                    {
                        result.Errors.Add(new ExcelRowErrorDto
                        {
                            Row = rowIndex,
                            Column = "Mã cổng",
                            Value = row.GateCode,
                            ErrorMessage = $"Mã cổng '{row.GateCode}' không tồn tại trong hệ thống."
                        });
                        result.FailedCount++;
                        continue;
                    }
                    gateId = matchedGate.Id;
                }

                if (existingLanes.TryGetValue(codeKey, out var existing))
                {
                    if (mode == DuplicateMode.Skip)
                    {
                        result.SkippedCount++;
                        continue;
                    }
                    if (mode == DuplicateMode.Error)
                    {
                        result.Errors.Add(new ExcelRowErrorDto { Row = rowIndex, Column = "Mã làn", Value = row.Code, ErrorMessage = "Mã làn đã tồn tại." });
                        result.FailedCount++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        existing.Name = row.Name.Trim();
                        if (!string.IsNullOrWhiteSpace(row.GateCode)) existing.GateId = gateId;
                        if (row.Direction.HasValue) existing.Direction = row.Direction.Value;
                        existing.OutputRelay = row.OutputRelay;
                        existing.InputReader = row.InputReader;
                        if (row.IsActive.HasValue) existing.IsActive = row.IsActive.Value;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _laneRepo.UpdateAsync(existing, cancellationToken);
                    }
                    result.SuccessCount++;
                    continue;
                }

                if (!dryRun)
                {
                    var newLane = new Lane
                    {
                        Code = row.Code.Trim(),
                        Name = row.Name.Trim(),
                        GateId = gateId,
                        Direction = row.Direction ?? LaneDirection.In,
                        OutputRelay = row.OutputRelay,
                        InputReader = row.InputReader,
                        IsActive = row.IsActive ?? true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _laneRepo.AddAsync(newLane, cancellationToken);
                    existingLanes[codeKey] = newLane;
                }
                result.SuccessCount++;
            }

            result.FailedCount += parseResult.FailedCount;
            return result;
        }

        private async Task<ExcelImportResultDto> ImportDevicesInternalAsync(
            Stream stream, DuplicateMode mode, bool dryRun, CancellationToken cancellationToken)
        {
            var parseResult = await _excelService.ReadAsync(stream, new DeviceExcelProfile());
            var result = MapParseResult(parseResult);

            var existingDevices = (await _deviceRepo.GetAllAsync(cancellationToken))
                .ToDictionary(x => x.Code.Trim().ToLowerInvariant(), x => x);

            for (var i = 0; i < parseResult.SuccessData.Count; i++)
            {
                var row = parseResult.SuccessData[i];
                var rowIndex = i + 2;
                var codeKey = row.Code.Trim().ToLowerInvariant();

                if (existingDevices.TryGetValue(codeKey, out var existing))
                {
                    if (mode == DuplicateMode.Skip)
                    {
                        result.SkippedCount++;
                        continue;
                    }
                    if (mode == DuplicateMode.Error)
                    {
                        result.Errors.Add(new ExcelRowErrorDto { Row = rowIndex, Column = "Mã thiết bị", Value = row.Code, ErrorMessage = "Mã thiết bị đã tồn tại." });
                        result.FailedCount++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        existing.Name = row.Name.Trim();
                        if (row.Type.HasValue) existing.Type = row.Type.Value;
                        if (!string.IsNullOrWhiteSpace(row.IpAddress)) existing.IpAddress = row.IpAddress.Trim();
                        existing.Port = row.Port;
                        existing.UserName = row.UserName?.Trim();
                        if (row.IsActive.HasValue) existing.IsActive = row.IsActive.Value;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _deviceRepo.UpdateAsync(existing, cancellationToken);
                    }
                    result.SuccessCount++;
                    continue;
                }

                if (!dryRun)
                {
                    var newDevice = new Device
                    {
                        Code = row.Code.Trim(),
                        Name = row.Name.Trim(),
                        Type = row.Type ?? DeviceType.Camera,
                        IpAddress = row.IpAddress.Trim(),
                        Port = row.Port,
                        UserName = row.UserName?.Trim(),
                        IsActive = row.IsActive ?? true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _deviceRepo.AddAsync(newDevice, cancellationToken);
                    existingDevices[codeKey] = newDevice;
                }
                result.SuccessCount++;
            }

            result.FailedCount += parseResult.FailedCount;
            return result;
        }

        #endregion

        #region Internal Export Logic

        private async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportCompaniesInternalAsync(
            string timestamp, CancellationToken cancellationToken)
        {
            var all = await _companyRepo.GetAllAsync(cancellationToken);
            var isTruncated = all.Count > MaxExportLimit;
            var data = all.Take(MaxExportLimit).Select(c => new CompanyExcelDto
            {
                Code = c.Code,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                IsActive = c.IsActive
            });

            var bytes = await _excelService.WriteAsync(data, new CompanyExcelProfile(), "Danh_Sach_Cong_Ty", "DANH SÁCH THÔNG TIN CÔNG TY", cancellationToken);
            return (bytes, $"Danh_Sach_Cong_Ty_{timestamp}.xlsx", isTruncated);
        }

        private async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportDepartmentsInternalAsync(
            string timestamp, CancellationToken cancellationToken)
        {
            var all = await _departmentRepo.GetAllAsync(cancellationToken);
            var companies = (await _companyRepo.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
            var isTruncated = all.Count > MaxExportLimit;
            var data = all.Take(MaxExportLimit).Select(d => new DepartmentExportExcelDto
            {
                Code = d.Code,
                Name = d.Name,
                CompanyName = !string.IsNullOrEmpty(d.CompanyId) && companies.TryGetValue(d.CompanyId, out var cName) ? cName : null,
                ManagerName = d.ManagerName,
                PhoneNumber = d.PhoneNumber,
                Email = d.Email,
                IsActive = d.IsActive
            });

            var bytes = await _excelService.WriteAsync(data, new DepartmentExportExcelProfile(), "Danh_Sach_Phong_Ban", "DANH SÁCH THÔNG TIN PHÒNG BAN", cancellationToken);
            return (bytes, $"Danh_Sach_Phong_Ban_{timestamp}.xlsx", isTruncated);
        }

        private async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportContractorsInternalAsync(
            string timestamp, CancellationToken cancellationToken)
        {
            var all = await _contractorRepo.GetAllAsync(cancellationToken);
            var isTruncated = all.Count > MaxExportLimit;
            var data = all.Take(MaxExportLimit).Select(c => new ContractorExcelDto
            {
                Code = c.Code,
                Name = c.Name,
                ContactPerson = c.ContactPerson,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                IsActive = c.IsActive
            });

            var bytes = await _excelService.WriteAsync(data, new ContractorExcelProfile(), "Danh_Sach_Nha_Thau", "DANH SÁCH THÔNG TIN NHÀ THẦU", cancellationToken);
            return (bytes, $"Danh_Sach_Nha_Thau_{timestamp}.xlsx", isTruncated);
        }

        private async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportGatesInternalAsync(
            string timestamp, CancellationToken cancellationToken)
        {
            var all = await _gateRepo.GetAllAsync(cancellationToken);
            var companies = (await _companyRepo.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
            var isTruncated = all.Count > MaxExportLimit;
            var data = all.Take(MaxExportLimit).Select(g => new GateExportExcelDto
            {
                Code = g.Code,
                Name = g.Name,
                CompanyName = !string.IsNullOrEmpty(g.CompanyId) && companies.TryGetValue(g.CompanyId, out var cName) ? cName : null,
                MachineCode = g.MachineCode,
                IsActive = g.IsActive
            });

            var bytes = await _excelService.WriteAsync(data, new GateExportExcelProfile(), "Danh_Sach_Cong_Kiem_Soat", "DANH SÁCH CỔNG KIỂM SOÁT", cancellationToken);
            return (bytes, $"Danh_Sach_Cong_Kiem_Soat_{timestamp}.xlsx", isTruncated);
        }

        private async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportLanesInternalAsync(
            string timestamp, CancellationToken cancellationToken)
        {
            var all = await _laneRepo.GetAllAsync(cancellationToken);
            var gates = (await _gateRepo.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
            var isTruncated = all.Count > MaxExportLimit;
            var data = all.Take(MaxExportLimit).Select(l => new LaneExportExcelDto
            {
                Code = l.Code,
                Name = l.Name,
                GateName = !string.IsNullOrEmpty(l.GateId) && gates.TryGetValue(l.GateId, out var gName) ? gName : null,
                Direction = l.Direction,
                OutputRelay = l.OutputRelay,
                InputReader = l.InputReader,
                IsActive = l.IsActive
            });

            var bytes = await _excelService.WriteAsync(data, new LaneExportExcelProfile(), "Danh_Sach_Lan_Xe", "DANH SÁCH LÀN XE KIỂM SOÁT", cancellationToken);
            return (bytes, $"Danh_Sach_Lan_Xe_{timestamp}.xlsx", isTruncated);
        }

        private async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportDevicesInternalAsync(
            string timestamp, CancellationToken cancellationToken)
        {
            var all = await _deviceRepo.GetAllAsync(cancellationToken);
            var isTruncated = all.Count > MaxExportLimit;
            var data = all.Take(MaxExportLimit).Select(d => new DeviceExcelDto
            {
                Code = d.Code,
                Name = d.Name,
                Type = d.Type,
                IpAddress = d.IpAddress,
                Port = d.Port,
                UserName = d.UserName,
                IsActive = d.IsActive
            });

            var bytes = await _excelService.WriteAsync(data, new DeviceExcelProfile(), "Danh_Sach_Thiet_Bi", "DANH SÁCH THIẾT BỊ HỆ THỐNG", cancellationToken);
            return (bytes, $"Danh_Sach_Thiet_Bi_{timestamp}.xlsx", isTruncated);
        }

        #endregion

        #region Helpers

        private static string NormalizeEntity(string entity) => (entity ?? string.Empty).Trim().ToLowerInvariant();

        private static ExcelImportResultDto MapParseResult<T>(ExcelImportResult<T> parseResult) where T : class, new()
        {
            var result = new ExcelImportResultDto
            {
                TotalRows = parseResult.TotalRows,
                FailedCount = 0,
                SuccessCount = 0,
                SkippedCount = 0
            };

            foreach (var err in parseResult.Errors)
            {
                result.Errors.Add(new ExcelRowErrorDto
                {
                    Row = err.Row,
                    Column = err.Column,
                    Value = err.Value,
                    ErrorMessage = err.ErrorMessage
                });
            }

            return result;
        }

        #endregion
    }
}
