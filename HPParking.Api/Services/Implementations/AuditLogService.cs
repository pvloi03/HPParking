using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.AuditLogs;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Triển khai dịch vụ tra cứu chỉ đọc nhật ký kiểm toán hệ thống (Audit Log)
    /// Tuân thủ nguyên tắc Sổ cái bất biến (Append-Only Immutable Ledger)
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly IRepository<AuditLog> _auditLogRepo;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            IRepository<AuditLog> auditLogRepo,
            ILogger<AuditLogService> logger)
        {
            _auditLogRepo = auditLogRepo;
            _logger = logger;
        }

        public async Task<PagedResult<AuditLogDto>> GetAuditLogsPagedAsync(AuditLogFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<AuditLog>.Filter;
            var filters = new List<FilterDefinition<AuditLog>>();

            if (!string.IsNullOrWhiteSpace(query.ActorUsername))
            {
                var cleanUsername = Regex.Escape(query.ActorUsername.Trim());
                filters.Add(builder.Regex(x => x.ActorUsername, new BsonRegularExpression(cleanUsername, "i")));
            }

            if (query.ActionType.HasValue)
            {
                filters.Add(builder.Eq(x => x.ActionType, query.ActionType.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.TargetEntity))
            {
                var cleanTarget = Regex.Escape(query.TargetEntity.Trim());
                filters.Add(builder.Regex(x => x.TargetEntity, new BsonRegularExpression(cleanTarget, "i")));
            }

            if (query.IsSuccess.HasValue)
            {
                filters.Add(builder.Eq(x => x.IsSuccess, query.IsSuccess.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                var cleanSource = Regex.Escape(query.Source.Trim());
                filters.Add(builder.Regex(x => x.Source, new BsonRegularExpression(cleanSource, "i")));
            }

            if (query.FromDate.HasValue)
            {
                filters.Add(builder.Gte(x => x.CreatedAt, query.FromDate.Value));
            }

            if (query.ToDate.HasValue)
            {
                filters.Add(builder.Lte(x => x.CreatedAt, query.ToDate.Value));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

            var sortBuilder = Builders<AuditLog>.Sort;
            var isAsc = query.SortOrder?.ToLower() == "asc";
            var sort = query.SortBy?.ToLower() switch
            {
                "actorusername" => isAsc ? sortBuilder.Ascending(x => x.ActorUsername) : sortBuilder.Descending(x => x.ActorUsername),
                "actiontype" => isAsc ? sortBuilder.Ascending(x => x.ActionType) : sortBuilder.Descending(x => x.ActionType),
                "targetentity" => isAsc ? sortBuilder.Ascending(x => x.TargetEntity) : sortBuilder.Descending(x => x.TargetEntity),
                _ => isAsc ? sortBuilder.Ascending(x => x.CreatedAt) : sortBuilder.Descending(x => x.CreatedAt)
            };

            var totalCount = await _auditLogRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var logs = await _auditLogRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            var items = logs.Adapt<List<AuditLogDto>>();

            _logger.LogInformation("Tra cứu nhật ký kiểm toán: tìm thấy {TotalCount} bản ghi (Trang {PageIndex}/{TotalPages}).",
                totalCount, query.PageIndex, (int)Math.Ceiling((double)totalCount / query.PageSize));

            return new PagedResult<AuditLogDto>(items, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<AuditLogDetailDto> GetAuditLogByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var log = await _auditLogRepo.GetByIdAsync(id, cancellationToken);
            if (log == null)
            {
                throw new NotFoundException("Không tìm thấy bản ghi nhật ký kiểm toán.", ErrorCodes.AUDIT_LOG_NOT_FOUND);
            }

            _logger.LogInformation("Lấy chi tiết nhật ký kiểm toán {Id}: Hành động {ActionType} bởi {ActorUsername} trên {TargetEntity}.",
                log.Id, log.ActionType, log.ActorUsername, log.TargetEntity);

            return log.Adapt<AuditLogDetailDto>();
        }
    }
}
