using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Gates;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    public class GateService : IGateService
    {
        private readonly IRepository<Gate> _gateRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly ILogger<GateService> _logger;

        public GateService(
            IRepository<Gate> gateRepo,
            IRepository<Company> companyRepo,
            IRepository<Lane> laneRepo,
            ILogger<GateService> logger)
        {
            _gateRepo = gateRepo;
            _companyRepo = companyRepo;
            _laneRepo = laneRepo;
            _logger = logger;
        }

        public async Task<PagedResult<GateDto>> GetGatesPagedAsync(GateFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<Gate>.Filter;
            var filters = new List<FilterDefinition<Gate>>();

            if (!string.IsNullOrWhiteSpace(query.CompanyId))
            {
                filters.Add(builder.Eq(g => g.CompanyId, query.CompanyId));
            }

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(g => g.IsActive, query.IsActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var cleanKw = Regex.Escape(query.Keyword.Trim());
                var regex = new BsonRegularExpression(cleanKw, "i");
                filters.Add(builder.Or(
                    builder.Regex(g => g.Code, regex),
                    builder.Regex(g => g.Name, regex),
                    builder.Regex(g => g.MachineCode, regex)
                ));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;
            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Gate>.Sort.Ascending(g => g.CreatedAt)
                : Builders<Gate>.Sort.Descending(g => g.CreatedAt);

            var totalCount = await _gateRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var gates = await _gateRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            // Bổ sung CompanyName phẳng vào GateDto (ADR 0030 Enriched Detail DTO Pattern)
            var companyIds = gates
                .Select(g => g.CompanyId)
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

            var dtos = gates.Select(g =>
            {
                var dto = g.Adapt<GateDto>();
                if (!string.IsNullOrEmpty(g.CompanyId) && companyDict.TryGetValue(g.CompanyId, out var compName))
                {
                    dto.CompanyName = compName;
                }
                return dto;
            }).ToList();

            return new PagedResult<GateDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<GateDto> GetGateByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var gate = await _gateRepo.GetByIdAsync(id, cancellationToken);
            if (gate == null || gate.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin cổng với Id đã chỉ định.", ErrorCodes.GATE_NOT_FOUND);
            }

            var dto = gate.Adapt<GateDto>();
            if (!string.IsNullOrEmpty(gate.CompanyId))
            {
                var company = await _companyRepo.GetByIdAsync(gate.CompanyId, cancellationToken);
                if (company != null)
                {
                    dto.CompanyName = company.Name;
                }
            }

            return dto;
        }

        public async Task<GateDto> CreateGateAsync(CreateGateRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Xác thực CompanyId tồn tại và hợp lệ
            var company = await _companyRepo.GetByIdAsync(request.CompanyId, cancellationToken);
            if (company == null || company.IsDeleted)
            {
                throw new NotFoundException($"Không tìm thấy công ty với Id '{request.CompanyId}'.", ErrorCodes.COMPANY_NOT_FOUND);
            }

            if (!company.IsActive)
            {
                throw new BadRequestException($"Công ty '{company.Name}' đang bị vô hiệu hóa, không thể tạo cổng trực thuộc.", ErrorCodes.BAD_REQUEST);
            }

            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // 2. Kiểm tra trùng mã Code trong các cổng chưa bị xóa
            var existing = await _gateRepo.FindOneAsync(
                g => g.Code == cleanCode && !g.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Mã cổng '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                    ErrorCodes.GATE_CODE_DUPLICATE);
            }

            var gate = new Gate
            {
                CompanyId = company.Id,
                Code = cleanCode,
                Name = cleanName,
                MachineCode = request.MachineCode.Trim(),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _gateRepo.AddAsync(gate, cancellationToken);
            _logger.LogInformation("Đã tạo mới cổng: {Name} (Code: {Code}) thuộc công ty {CompanyName} - ID: {Id}", gate.Name, gate.Code, company.Name, gate.Id);

            var dto = gate.Adapt<GateDto>();
            dto.CompanyName = company.Name;
            return dto;
        }

        public async Task<GateDto> UpdateGateAsync(string id, UpdateGateRequest request, CancellationToken cancellationToken = default)
        {
            var gate = await _gateRepo.GetByIdAsync(id, cancellationToken);
            if (gate == null || gate.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin cổng cần cập nhật.", ErrorCodes.GATE_NOT_FOUND);
            }

            // 1. Xác thực CompanyId hợp lệ nếu chỉ định
            var company = await _companyRepo.GetByIdAsync(request.CompanyId, cancellationToken);
            if (company == null || company.IsDeleted)
            {
                throw new NotFoundException($"Không tìm thấy công ty với Id '{request.CompanyId}'.", ErrorCodes.COMPANY_NOT_FOUND);
            }

            if (!company.IsActive)
            {
                throw new BadRequestException($"Công ty '{company.Name}' đang bị vô hiệu hóa, không thể gán cổng trực thuộc.", ErrorCodes.BAD_REQUEST);
            }

            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // 2. Kiểm tra trùng mã Code nếu thay đổi
            if (!string.Equals(gate.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _gateRepo.FindOneAsync(
                    g => g.Code == cleanCode && g.Id != id && !g.IsDeleted,
                    cancellationToken);

                if (existing != null)
                {
                    throw new ConflictException(
                        $"Mã cổng '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                        ErrorCodes.GATE_CODE_DUPLICATE);
                }
            }

            // 3. Active State Protection (ADR 0030): Chặn tắt hoạt động Cổng nếu còn Làn xe active
            if (gate.IsActive && !request.IsActive)
            {
                var activeLaneCount = await _laneRepo.CountAsync(
                    l => l.GateId == id && l.IsActive && !l.IsDeleted,
                    cancellationToken: cancellationToken);

                if (activeLaneCount > 0)
                {
                    throw new BadRequestException(
                        $"Không thể vô hiệu hóa Cổng '{gate.Name}' vì vẫn còn {activeLaneCount} làn xe đang hoạt động. Vui lòng tắt các làn xe trước.",
                        ErrorCodes.INFRA_ACTIVE_DEPENDENCY_EXISTS);
                }
            }

            gate.Code = cleanCode;
            gate.Name = cleanName;
            gate.CompanyId = company.Id;
            gate.MachineCode = request.MachineCode.Trim();
            gate.IsActive = request.IsActive;
            gate.UpdatedAt = DateTime.UtcNow;

            await _gateRepo.UpdateAsync(gate, cancellationToken);
            _logger.LogInformation("Đã cập nhật cổng {Id}: {Name} (Code: {Code})", gate.Id, gate.Name, gate.Code);

            var dto = gate.Adapt<GateDto>();
            dto.CompanyName = company.Name;
            return dto;
        }

        public async Task<bool> DeleteGateAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var gate = await _gateRepo.GetByIdAsync(id, cancellationToken)
                ?? (hardDelete ? await _gateRepo.GetDeletedByIdAsync(id, cancellationToken) : null);

            if (gate == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin cổng cần xóa.", ErrorCodes.GATE_NOT_FOUND);
            }

            // Universal Restrict Deletion Policy (ADR 0030 & ADR 0031):
            // Chặn xóa nếu còn Làn xe chưa bị xóa (!IsDeleted) trực thuộc cổng
            var laneCount = await _laneRepo.CountAsync(
                l => l.GateId == id && !l.IsDeleted,
                cancellationToken: cancellationToken);

            if (laneCount > 0)
            {
                throw new ConflictException(
                    $"Không thể xóa Cổng '{gate.Name}' vì vẫn còn {laneCount} làn xe trực thuộc chưa bị xóa. Vui lòng xóa hoặc di chuyển các làn xe trước.",
                    ErrorCodes.GATE_HAS_LANES);
            }

            if (!hardDelete)
            {
                await _gateRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã xóa mềm cổng {Id}: {Name} ({Code}) vào thùng rác", id, gate.Name, gate.Code);
            }
            else
            {
                await _gateRepo.DeleteAsync(id, softDelete: false, cancellationToken);
                _logger.LogInformation("Đã xóa vĩnh viễn cổng {Id}: {Name} ({Code}) khỏi cơ sở dữ liệu", id, gate.Name, gate.Code);
            }

            return true;
        }

        public async Task<GateDto> RestoreGateAsync(string id, CancellationToken cancellationToken = default)
        {
            var gate = await _gateRepo.GetDeletedByIdAsync(id, cancellationToken);
            if (gate == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin cổng trong thùng rác.", ErrorCodes.GATE_NOT_FOUND);
            }

            // Strict Parent-First Restore Policy (ADR 0031):
            // Kiểm tra công ty cha: bắt buộc phải tồn tại và chưa bị xóa (!IsDeleted)
            Company? company = null;
            if (!string.IsNullOrEmpty(gate.CompanyId))
            {
                company = await _companyRepo.GetByIdAsync(gate.CompanyId, cancellationToken);
                if (company == null || company.IsDeleted)
                {
                    throw new BadRequestException(
                        $"Không thể khôi phục Cổng '{gate.Name}' vì Công ty cha đã bị xóa hoặc không tồn tại. Vui lòng khôi phục công ty cha trước.",
                        ErrorCodes.PARENT_IS_DELETED);
                }
            }

            // Re-validation on Restore (ADR 0031): Kiểm tra trùng mã Code trong các bản ghi đang hoạt động
            var existingCode = await _gateRepo.FindOneAsync(
                g => g.Code == gate.Code && !g.IsDeleted,
                cancellationToken);

            if (existingCode != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục Cổng vì mã '{gate.Code}' đã được sử dụng bởi cổng đang hoạt động '{existingCode.Name}'.",
                    ErrorCodes.GATE_CODE_DUPLICATE);
            }

            await _gateRepo.RestoreAsync(id, cancellationToken);
            _logger.LogInformation("Đã khôi phục thành công cổng {Id}: {Name} ({Code}) từ thùng rác", id, gate.Name, gate.Code);

            gate.IsDeleted = false;
            gate.DeletedAt = null;

            var dto = gate.Adapt<GateDto>();
            if (company != null)
            {
                dto.CompanyName = company.Name;
            }

            return dto;
        }
    }
}
