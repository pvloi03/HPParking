using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Companies;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    public class CompanyService : ICompanyService
    {
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Gate> _gateRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly ILogger<CompanyService> _logger;

        public CompanyService(
            IRepository<Company> companyRepo,
            IRepository<Department> departmentRepo,
            IRepository<Gate> gateRepo,
            IRepository<Client> clientRepo,
            ILogger<CompanyService> logger)
        {
            _companyRepo = companyRepo;
            _departmentRepo = departmentRepo;
            _gateRepo = gateRepo;
            _clientRepo = clientRepo;
            _logger = logger;
        }

        public async Task<PagedResult<CompanyDto>> GetCompaniesPagedAsync(CompanyFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<Company>.Filter;
            var filters = new List<FilterDefinition<Company>>();

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(c => c.IsActive, query.IsActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var cleanKw = Regex.Escape(query.Keyword.Trim());
                var regex = new BsonRegularExpression(cleanKw, "i");
                filters.Add(builder.Or(
                    builder.Regex(c => c.Code, regex),
                    builder.Regex(c => c.Name, regex),
                    builder.Regex(c => c.PhoneNumber, regex)
                ));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;
            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Company>.Sort.Ascending(c => c.CreatedAt)
                : Builders<Company>.Sort.Descending(c => c.CreatedAt);

            var totalCount = await _companyRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var companies = await _companyRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            var dtos = companies.Adapt<List<CompanyDto>>();
            return new PagedResult<CompanyDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<CompanyDto> GetCompanyByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var company = await _companyRepo.GetByIdAsync(id, cancellationToken);
            if (company == null || company.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin công ty với Id đã chỉ định.", ErrorCodes.COMPANY_NOT_FOUND);
            }

            return company.Adapt<CompanyDto>();
        }

        public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
        {
            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // Kiểm tra trùng mã Code trong các công ty chưa bị xóa
            var existing = await _companyRepo.FindOneAsync(
                c => c.Code == cleanCode && !c.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Mã công ty '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                    ErrorCodes.COMPANY_CODE_DUPLICATE);
            }

            var company = new Company
            {
                Code = cleanCode,
                Name = cleanName,
                PhoneNumber = request.PhoneNumber?.Trim(),
                Email = request.Email?.Trim(),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _companyRepo.AddAsync(company, cancellationToken);
            _logger.LogInformation("Đã tạo mới công ty: {Name} (Code: {Code}) - ID: {Id}", company.Name, company.Code, company.Id);

            return company.Adapt<CompanyDto>();
        }

        public async Task<CompanyDto> UpdateCompanyAsync(string id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
        {
            var company = await _companyRepo.GetByIdAsync(id, cancellationToken);
            if (company == null || company.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin công ty cần cập nhật.", ErrorCodes.COMPANY_NOT_FOUND);
            }

            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // Nếu thay đổi Code, kiểm tra trùng lặp
            if (!string.Equals(company.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _companyRepo.FindOneAsync(
                    c => c.Code == cleanCode && c.Id != id && !c.IsDeleted,
                    cancellationToken);

                if (existing != null)
                {
                    throw new ConflictException(
                        $"Mã công ty '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                        ErrorCodes.COMPANY_CODE_DUPLICATE);
                }
            }

            // Active State Protection (ADR 0030 & ADR 0033): Không cho tắt Công ty nếu còn phòng ban, cổng hoặc khách hàng đang active
            if (company.IsActive && !request.IsActive)
            {
                var activeDepCount = await _departmentRepo.CountAsync(
                    d => d.CompanyId == id && d.IsActive && !d.IsDeleted,
                    cancellationToken);

                if (activeDepCount > 0)
                {
                    throw new BadRequestException(
                        $"Không thể vô hiệu hóa Công ty '{company.Name}' vì vẫn còn {activeDepCount} phòng ban đang hoạt động. Vui lòng tắt các phòng ban trước.",
                        ErrorCodes.COMPANY_ACTIVE_DEPENDENCY_EXISTS);
                }

                var activeGateCount = await _gateRepo.CountAsync(
                    g => g.CompanyId == id && g.IsActive && !g.IsDeleted,
                    cancellationToken);

                if (activeGateCount > 0)
                {
                    throw new BadRequestException(
                        $"Không thể vô hiệu hóa Công ty '{company.Name}' vì vẫn còn {activeGateCount} cổng đang hoạt động. Vui lòng tắt các cổng trước.",
                        ErrorCodes.COMPANY_ACTIVE_DEPENDENCY_EXISTS);
                }

                var activeClientCount = await _clientRepo.CountAsync(
                    c => c.CompanyId == id && c.IsActive && !c.IsDeleted,
                    cancellationToken);

                if (activeClientCount > 0)
                {
                    throw new BadRequestException(
                        $"Không thể vô hiệu hóa Công ty '{company.Name}' vì vẫn còn {activeClientCount} khách hàng/nhân sự đang hoạt động. Vui lòng tắt hoặc chuyển nhân sự trước.",
                        ErrorCodes.COMPANY_ACTIVE_DEPENDENCY_EXISTS);
                }
            }

            company.Code = cleanCode;
            company.Name = cleanName;
            company.PhoneNumber = request.PhoneNumber?.Trim();
            company.Email = request.Email?.Trim();
            company.IsActive = request.IsActive;
            company.UpdatedAt = DateTime.UtcNow;

            await _companyRepo.UpdateAsync(company, cancellationToken);
            _logger.LogInformation("Đã cập nhật công ty {Id}: {Name} (Code: {Code})", company.Id, company.Name, company.Code);

            return company.Adapt<CompanyDto>();
        }

        public async Task<bool> DeleteCompanyAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var company = await _companyRepo.GetByIdAsync(id, cancellationToken);
            if (company == null || company.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin công ty cần xóa.", ErrorCodes.COMPANY_NOT_FOUND);
            }

            // Restrict Deletion Policy (ADR 0030): Chặn xóa nếu còn phòng ban trực thuộc
            var depCount = await _departmentRepo.CountAsync(
                d => d.CompanyId == id && !d.IsDeleted,
                cancellationToken);

            if (depCount > 0)
            {
                throw new ConflictException(
                    $"Không thể xóa Công ty '{company.Name}' vì vẫn còn {depCount} phòng ban trực thuộc. Vui lòng xóa hoặc di chuyển các phòng ban trước.",
                    ErrorCodes.COMPANY_HAS_DEPARTMENTS);
            }

            // Restrict Deletion Policy (ADR 0030): Chặn xóa nếu còn Cổng trực thuộc
            var gateCount = await _gateRepo.CountAsync(
                g => g.CompanyId == id && !g.IsDeleted,
                cancellationToken);

            if (gateCount > 0)
            {
                throw new ConflictException(
                    $"Không thể xóa Công ty '{company.Name}' vì vẫn còn {gateCount} cổng trực thuộc. Vui lòng xóa hoặc di chuyển các cổng trước.",
                    ErrorCodes.COMPANY_HAS_GATES);
            }

            // Restrict Deletion Policy (ADR 0030 & ADR 0033): Chặn xóa nếu còn Khách hàng/Nhân sự trực thuộc
            var clientCount = await _clientRepo.CountAsync(
                c => c.CompanyId == id && !c.IsDeleted,
                cancellationToken);

            if (clientCount > 0)
            {
                throw new ConflictException(
                    $"Không thể xóa Công ty '{company.Name}' vì vẫn còn {clientCount} khách hàng/nhân sự trực thuộc. Vui lòng chuyển hoặc xóa nhân sự trước.",
                    ErrorCodes.COMPANY_HAS_CLIENTS);
            }

            if (!hardDelete)
            {
                await _companyRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã XÓA MỀM công ty {Id}: {Name}", id, company.Name);
            }
            else
            {
                await _companyRepo.DeleteAsync(id, softDelete: false, cancellationToken);
                _logger.LogInformation("Đã XÓA CỨNG công ty {Id}: {Name}", id, company.Name);
            }

            return true;
        }

        public async Task<CompanyDto> RestoreCompanyAsync(string id, CancellationToken cancellationToken = default)
        {
            var company = await _companyRepo.GetDeletedByIdAsync(id, cancellationToken);
            if (company == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin công ty trong thùng rác.", ErrorCodes.COMPANY_NOT_FOUND);
            }

            // Re-validation: Kiểm tra mã Code xem có bị trùng với Công ty đang hoạt động khác không
            var existing = await _companyRepo.FindOneAsync(
                c => c.Code == company.Code && c.Id != id && !c.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục vì mã công ty '{company.Code}' đã được sử dụng bởi công ty đang hoạt động '{existing.Name}'.",
                    ErrorCodes.COMPANY_CODE_DUPLICATE);
            }

            var success = await _companyRepo.RestoreAsync(id, cancellationToken);
            if (!success)
            {
                throw new AppException("Khôi phục công ty thất bại.", 500, ErrorCodes.RESTORE_FAILED);
            }

            company.IsDeleted = false;
            company.DeletedAt = null;
            company.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Đã KHÔI PHỤC công ty {Id}: {Name} ({Code}) từ thùng rác.", company.Id, company.Name, company.Code);
            return company.Adapt<CompanyDto>();
        }
    }
}
