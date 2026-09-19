using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Contractors;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    public class ContractorService : IContractorService
    {
        private readonly IRepository<Contractor> _contractorRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly ILogger<ContractorService> _logger;

        public ContractorService(
            IRepository<Contractor> contractorRepo,
            IRepository<Client> clientRepo,
            ILogger<ContractorService> logger)
        {
            _contractorRepo = contractorRepo;
            _clientRepo = clientRepo;
            _logger = logger;
        }

        public async Task<PagedResult<ContractorDto>> GetContractorsPagedAsync(ContractorFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<Contractor>.Filter;
            var filters = new List<FilterDefinition<Contractor>>();

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
                    builder.Regex(c => c.ContactPerson, regex),
                    builder.Regex(c => c.PhoneNumber, regex)
                ));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;
            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Contractor>.Sort.Ascending(c => c.CreatedAt)
                : Builders<Contractor>.Sort.Descending(c => c.CreatedAt);

            var totalCount = await _contractorRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var contractors = await _contractorRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            var dtos = contractors.Adapt<List<ContractorDto>>();
            return new PagedResult<ContractorDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<ContractorDto> GetContractorByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var contractor = await _contractorRepo.GetByIdAsync(id, cancellationToken);
            if (contractor == null || contractor.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin nhà thầu với Id đã chỉ định.", ErrorCodes.CONTRACTOR_NOT_FOUND);
            }

            return contractor.Adapt<ContractorDto>();
        }

        public async Task<ContractorDto> CreateContractorAsync(CreateContractorRequest request, CancellationToken cancellationToken = default)
        {
            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // Kiểm tra trùng mã Code trong các nhà thầu chưa bị xóa
            var existing = await _contractorRepo.FindOneAsync(
                c => c.Code == cleanCode && !c.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Mã nhà thầu '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                    ErrorCodes.CONTRACTOR_CODE_DUPLICATE);
            }

            var contractor = new Contractor
            {
                Code = cleanCode,
                Name = cleanName,
                ContactPerson = request.ContactPerson?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                Email = request.Email?.Trim(),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _contractorRepo.AddAsync(contractor, cancellationToken);
            _logger.LogInformation("Đã tạo mới nhà thầu: {Name} (Code: {Code}) - ID: {Id}", contractor.Name, contractor.Code, contractor.Id);

            return contractor.Adapt<ContractorDto>();
        }

        public async Task<ContractorDto> UpdateContractorAsync(string id, UpdateContractorRequest request, CancellationToken cancellationToken = default)
        {
            var contractor = await _contractorRepo.GetByIdAsync(id, cancellationToken);
            if (contractor == null || contractor.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin nhà thầu cần cập nhật.", ErrorCodes.CONTRACTOR_NOT_FOUND);
            }

            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // Nếu đổi Code, kiểm tra trùng lặp
            if (!string.Equals(contractor.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _contractorRepo.FindOneAsync(
                    c => c.Code == cleanCode && c.Id != id && !c.IsDeleted,
                    cancellationToken);

                if (existing != null)
                {
                    throw new ConflictException(
                        $"Mã nhà thầu '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                        ErrorCodes.CONTRACTOR_CODE_DUPLICATE);
                }
            }

            // Active State Protection (ADR 0030 & ADR 0033): Không cho tắt Nhà thầu nếu còn nhân sự đang active
            if (contractor.IsActive && !request.IsActive)
            {
                var activeClientCount = await _clientRepo.CountAsync(
                    c => c.ContractorId == id && c.IsActive && !c.IsDeleted,
                    cancellationToken);

                if (activeClientCount > 0)
                {
                    throw new BadRequestException(
                        $"Không thể vô hiệu hóa Nhà thầu '{contractor.Name}' vì vẫn còn {activeClientCount} khách hàng/nhân sự đang hoạt động. Vui lòng tắt hoặc chuyển nhân sự trước.",
                        ErrorCodes.CONTRACTOR_ACTIVE_CLIENTS_EXIST);
                }
            }

            contractor.Code = cleanCode;
            contractor.Name = cleanName;
            contractor.ContactPerson = request.ContactPerson?.Trim();
            contractor.PhoneNumber = request.PhoneNumber?.Trim();
            contractor.Email = request.Email?.Trim();
            contractor.IsActive = request.IsActive;
            contractor.UpdatedAt = DateTime.UtcNow;

            await _contractorRepo.UpdateAsync(contractor, cancellationToken);
            _logger.LogInformation("Đã cập nhật nhà thầu {Id}: {Name} (Code: {Code})", contractor.Id, contractor.Name, contractor.Code);

            return contractor.Adapt<ContractorDto>();
        }

        public async Task<bool> DeleteContractorAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var contractor = await _contractorRepo.GetByIdAsync(id, cancellationToken);
            if (contractor == null || contractor.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin nhà thầu cần xóa.", ErrorCodes.CONTRACTOR_NOT_FOUND);
            }

            // Restrict Deletion Policy (ADR 0030, 0031 & ADR 0033): Chặn xóa nếu còn khách hàng/nhân sự trực thuộc
            var clientCount = await _clientRepo.CountAsync(
                c => c.ContractorId == id && !c.IsDeleted,
                cancellationToken);

            if (clientCount > 0)
            {
                throw new ConflictException(
                    $"Không thể xóa Nhà thầu '{contractor.Name}' vì vẫn còn {clientCount} khách hàng/nhân sự trực thuộc. Vui lòng chuyển hoặc xóa nhân sự trước.",
                    ErrorCodes.CONTRACTOR_HAS_CLIENTS);
            }

            if (!hardDelete)
            {
                await _contractorRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã XÓA MỀM nhà thầu {Id}: {Name}", id, contractor.Name);
            }
            else
            {
                await _contractorRepo.DeleteAsync(id, softDelete: false, cancellationToken);
                _logger.LogInformation("Đã XÓA CỨNG nhà thầu {Id}: {Name}", id, contractor.Name);
            }

            return true;
        }

        public async Task<ContractorDto> RestoreContractorAsync(string id, CancellationToken cancellationToken = default)
        {
            var contractor = await _contractorRepo.GetDeletedByIdAsync(id, cancellationToken);
            if (contractor == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin nhà thầu trong thùng rác.", ErrorCodes.CONTRACTOR_NOT_FOUND);
            }

            // Re-validation: Kiểm tra mã Code xem có bị trùng với Nhà thầu đang hoạt động khác không
            var existing = await _contractorRepo.FindOneAsync(
                c => c.Code == contractor.Code && c.Id != id && !c.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục vì mã nhà thầu '{contractor.Code}' đã được sử dụng bởi nhà thầu đang hoạt động '{existing.Name}'.",
                    ErrorCodes.CONTRACTOR_CODE_DUPLICATE);
            }

            var success = await _contractorRepo.RestoreAsync(id, cancellationToken);
            if (!success)
            {
                throw new AppException("Khôi phục nhà thầu thất bại.", 500, ErrorCodes.RESTORE_FAILED);
            }

            contractor.IsDeleted = false;
            contractor.DeletedAt = null;
            contractor.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Đã KHÔI PHỤC nhà thầu {Id}: {Name} ({Code}) từ thùng rác.", contractor.Id, contractor.Name, contractor.Code);
            return contractor.Adapt<ContractorDto>();
        }
    }
}
