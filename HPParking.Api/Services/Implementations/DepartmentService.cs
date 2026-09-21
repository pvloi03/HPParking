using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Departments;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(
            IRepository<Department> departmentRepo,
            IRepository<Company> companyRepo,
            IRepository<Client> clientRepo,
            ILogger<DepartmentService> logger)
        {
            _departmentRepo = departmentRepo;
            _companyRepo = companyRepo;
            _clientRepo = clientRepo;
            _logger = logger;
        }

        public async Task<PagedResult<DepartmentDto>> GetDepartmentsPagedAsync(DepartmentFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<Department>.Filter;
            var filters = new List<FilterDefinition<Department>>();

            if (!string.IsNullOrWhiteSpace(query.CompanyId))
            {
                filters.Add(builder.Eq(d => d.CompanyId, query.CompanyId));
            }

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(d => d.IsActive, query.IsActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var cleanKw = Regex.Escape(query.Keyword.Trim());
                var regex = new BsonRegularExpression(cleanKw, "i");
                filters.Add(builder.Or(
                    builder.Regex(d => d.Code, regex),
                    builder.Regex(d => d.Name, regex),
                    builder.Regex(d => d.ManagerName, regex),
                    builder.Regex(d => d.PhoneNumber, regex)
                ));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;
            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Department>.Sort.Ascending(d => d.CreatedAt)
                : Builders<Department>.Sort.Descending(d => d.CreatedAt);

            var totalCount = await _departmentRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var departments = await _departmentRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            // Bổ sung CompanyName phẳng vào DepartmentDto (ADR 0030 Enriched Detail DTO Pattern)
            var companyIds = departments
                .Select(d => d.CompanyId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var companyDict = new Dictionary<string, string>();
            if (companyIds.Count > 0)
            {
                var compFilter = Builders<Company>.Filter.In(c => c.Id, companyIds);
                var companies = await _companyRepo.FindAsync(compFilter, cancellationToken: cancellationToken);
                foreach (var c in companies)
                {
                    companyDict[c.Id] = c.Name;
                }
            }

            var dtos = departments.Select(d =>
            {
                var dto = d.Adapt<DepartmentDto>();
                if (!string.IsNullOrEmpty(d.CompanyId) && companyDict.TryGetValue(d.CompanyId, out var compName))
                {
                    dto.CompanyName = compName;
                }
                return dto;
            }).ToList();

            return new PagedResult<DepartmentDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<DepartmentDto> GetDepartmentByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var department = await _departmentRepo.GetByIdAsync(id, cancellationToken);
            if (department == null || department.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin phòng ban với Id đã chỉ định.", ErrorCodes.DEPARTMENT_NOT_FOUND);
            }

            var dto = department.Adapt<DepartmentDto>();
            if (!string.IsNullOrEmpty(department.CompanyId))
            {
                var company = await _companyRepo.GetByIdAsync(department.CompanyId, cancellationToken);
                if (company != null && !company.IsDeleted)
                {
                    dto.CompanyName = company.Name;
                }
            }

            return dto;
        }

        public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Xác thực CompanyId tồn tại và hợp lệ
            var company = await _companyRepo.GetByIdAsync(request.CompanyId, cancellationToken);
            if (company == null || company.IsDeleted)
            {
                throw new NotFoundException($"Không tìm thấy công ty với Id '{request.CompanyId}'.", ErrorCodes.COMPANY_NOT_FOUND);
            }

            if (!company.IsActive)
            {
                throw new BadRequestException($"Công ty '{company.Name}' đang bị vô hiệu hóa, không thể tạo phòng ban trực thuộc.", ErrorCodes.BAD_REQUEST);
            }

            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // 2. Kiểm tra trùng mã Code trong các phòng ban chưa bị xóa
            var existing = await _departmentRepo.FindOneAsync(
                d => d.Code == cleanCode && !d.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Mã phòng ban '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                    ErrorCodes.DEPARTMENT_CODE_DUPLICATE);
            }

            var department = new Department
            {
                CompanyId = company.Id,
                Code = cleanCode,
                Name = cleanName,
                ManagerName = request.ManagerName?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                Email = request.Email?.Trim(),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _departmentRepo.AddAsync(department, cancellationToken);
            _logger.LogInformation("Đã tạo mới phòng ban: {Name} (Code: {Code}) trực thuộc công ty {CompanyId}", department.Name, department.Code, company.Id);

            var dto = department.Adapt<DepartmentDto>();
            dto.CompanyName = company.Name;
            return dto;
        }

        public async Task<DepartmentDto> UpdateDepartmentAsync(string id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
        {
            var department = await _departmentRepo.GetByIdAsync(id, cancellationToken);
            if (department == null || department.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin phòng ban cần cập nhật.", ErrorCodes.DEPARTMENT_NOT_FOUND);
            }

            // Nếu cập nhật CompanyId, kiểm tra tính hợp lệ
            string? targetCompanyName = null;
            if (!string.IsNullOrWhiteSpace(request.CompanyId))
            {
                var company = await _companyRepo.GetByIdAsync(request.CompanyId, cancellationToken);
                if (company == null || company.IsDeleted)
                {
                    throw new NotFoundException($"Không tìm thấy công ty với Id '{request.CompanyId}'.", ErrorCodes.COMPANY_NOT_FOUND);
                }
                department.CompanyId = company.Id;
                targetCompanyName = company.Name;
            }
            else if (!string.IsNullOrEmpty(department.CompanyId))
            {
                var company = await _companyRepo.GetByIdAsync(department.CompanyId, cancellationToken);
                targetCompanyName = company?.Name;
            }

            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // Nếu đổi Code, kiểm tra trùng lặp
            if (!string.Equals(department.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _departmentRepo.FindOneAsync(
                    d => d.Code == cleanCode && d.Id != id && !d.IsDeleted,
                    cancellationToken);

                if (existing != null)
                {
                    throw new ConflictException(
                        $"Mã phòng ban '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                        ErrorCodes.DEPARTMENT_CODE_DUPLICATE);
                }
            }

            // Active State Protection (ADR 0030 & ADR 0033): Không cho tắt Phòng ban nếu còn khách hàng/nhân sự đang active
            if (department.IsActive && !request.IsActive)
            {
                var activeClientCount = await _clientRepo.CountAsync(
                    c => c.DepartmentId == id && c.IsActive && !c.IsDeleted,
                    cancellationToken);

                if (activeClientCount > 0)
                {
                    throw new BadRequestException(
                        $"Không thể vô hiệu hóa Phòng ban '{department.Name}' vì vẫn còn {activeClientCount} khách hàng/nhân sự đang hoạt động. Vui lòng tắt hoặc chuyển nhân sự trước.",
                        ErrorCodes.DEPARTMENT_ACTIVE_CLIENTS_EXIST);
                }
            }

            department.Code = cleanCode;
            department.Name = cleanName;
            department.ManagerName = request.ManagerName?.Trim();
            department.PhoneNumber = request.PhoneNumber?.Trim();
            department.Email = request.Email?.Trim();
            department.IsActive = request.IsActive;
            department.UpdatedAt = DateTime.UtcNow;

            await _departmentRepo.UpdateAsync(department, cancellationToken);
            _logger.LogInformation("Đã cập nhật phòng ban {Id}: {Name} (Code: {Code})", department.Id, department.Name, department.Code);

            var dto = department.Adapt<DepartmentDto>();
            dto.CompanyName = targetCompanyName;
            return dto;
        }

        public async Task<bool> DeleteDepartmentAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var department = await _departmentRepo.GetByIdAsync(id, cancellationToken);
            if (department == null || department.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin phòng ban cần xóa.", ErrorCodes.DEPARTMENT_NOT_FOUND);
            }

            // Restrict Deletion Policy (ADR 0030): Chặn xóa nếu còn khách hàng/nhân sự trực thuộc phòng ban
            var clientCount = await _clientRepo.CountAsync(
                c => c.DepartmentId == id && !c.IsDeleted,
                cancellationToken);

            if (clientCount > 0)
            {
                throw new ConflictException(
                    $"Không thể xóa Phòng ban '{department.Name}' vì vẫn còn {clientCount} khách hàng/nhân sự trực thuộc. Vui lòng chuyển hoặc xóa nhân sự trước.",
                    ErrorCodes.DEPARTMENT_HAS_CLIENTS);
            }

            if (!hardDelete)
            {
                await _departmentRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã XÓA MỀM phòng ban {Id}: {Name}", id, department.Name);
            }
            else
            {
                await _departmentRepo.DeleteAsync(id, softDelete: false, cancellationToken);
                _logger.LogInformation("Đã XÓA CỨNG phòng ban {Id}: {Name}", id, department.Name);
            }

            return true;
        }

        public async Task<DepartmentDto> RestoreDepartmentAsync(string id, CancellationToken cancellationToken = default)
        {
            var department = await _departmentRepo.GetDeletedByIdAsync(id, cancellationToken);
            if (department == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin phòng ban trong thùng rác.", ErrorCodes.DEPARTMENT_NOT_FOUND);
            }

            // Strict Parent-First Restore (ADR 0031): Bắt buộc Công ty cha phải đang hoạt động
            if (string.IsNullOrWhiteSpace(department.CompanyId))
            {
                throw new BadRequestException(
                    "Phòng ban không có thông tin công ty trực thuộc.",
                    ErrorCodes.PARENT_IS_DELETED);
            }

            var company = await _companyRepo.GetByIdAsync(department.CompanyId, cancellationToken);
            if (company == null || company.IsDeleted)
            {
                throw new BadRequestException(
                    "Không thể khôi phục phòng ban vì công ty trực thuộc đang nằm trong thùng rác hoặc không tồn tại. Vui lòng khôi phục công ty trước.",
                    ErrorCodes.PARENT_IS_DELETED);
            }

            // Re-validation: Kiểm tra mã Code xem có bị trùng với Phòng ban đang hoạt động khác không
            var existing = await _departmentRepo.FindOneAsync(
                d => d.Code == department.Code && d.Id != id && !d.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục vì mã phòng ban '{department.Code}' đã được sử dụng bởi phòng ban đang hoạt động '{existing.Name}'.",
                    ErrorCodes.DEPARTMENT_CODE_DUPLICATE);
            }

            var success = await _departmentRepo.RestoreAsync(id, cancellationToken);
            if (!success)
            {
                throw new AppException("Khôi phục phòng ban thất bại.", 500, ErrorCodes.RESTORE_FAILED);
            }

            department.IsDeleted = false;
            department.DeletedAt = null;
            department.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Đã KHÔI PHỤC phòng ban {Id}: {Name} ({Code}) thuộc công ty {CompanyId} từ thùng rác.", department.Id, department.Name, department.Code, company.Id);

            var dto = department.Adapt<DepartmentDto>();
            dto.CompanyName = company.Name;
            return dto;
        }
    }
}
