using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.AuditLogs;
using HPParking.Api.DTOs.Excel.Reports;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Api.DTOs.Statistics;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Hiện thực hóa dịch vụ Xuất Excel cho Sổ cái và Báo cáo (Nhóm III - ADR 0023, ADR 0030)
    /// </summary>
    public class ReportExcelService : IReportExcelService
    {
        private const int MaxExportLimit = 10000;

        private readonly IExcelService _excelService;
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<AuditLog> _auditLogRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<ReportExcelService> _logger;

        public ReportExcelService(
            IExcelService excelService,
            IRepository<ParkingSession> sessionRepo,
            IRepository<AuditLog> auditLogRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IStatisticsService statisticsService,
            ILogger<ReportExcelService> logger)
        {
            _excelService = excelService;
            _sessionRepo = sessionRepo;
            _auditLogRepo = auditLogRepo;
            _clientRepo = clientRepo;
            _vehicleRepo = vehicleRepo;
            _statisticsService = statisticsService;
            _logger = logger;
        }

        public async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportParkingSessionsAsync(
            ParkingSessionFilterQuery query,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildParkingSessionFilter(query);
            var sort = BuildParkingSessionSort(query);

            var totalCount = await _sessionRepo.CountAsync(filter, cancellationToken);
            var isTruncated = totalCount > MaxExportLimit;

            var sessions = await _sessionRepo.FindAsync(
                filter: filter,
                sort: sort,
                skip: 0,
                limit: MaxExportLimit,
                cancellationToken: cancellationToken);

            var personIds = sessions
                .Where(s => !string.IsNullOrEmpty(s.PersonId))
                .Select(s => s.PersonId!)
                .Distinct()
                .ToHashSet();

            var distinctPlates = sessions
                .Where(s => !string.IsNullOrWhiteSpace(s.PlateNumber))
                .Select(s => s.PlateNumber)
                .Distinct()
                .ToList();

            var plateToClientMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (distinctPlates.Count > 0)
            {
                var searchPlates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in distinctPlates)
                {
                    searchPlates.Add(p);
                    var norm = PlateHelper.Normalize(p);
                    if (!string.IsNullOrEmpty(norm)) searchPlates.Add(norm);
                }

                var vehicleFilter = Builders<Vehicle>.Filter.And(
                    Builders<Vehicle>.Filter.Eq(v => v.IsDeleted, false),
                    Builders<Vehicle>.Filter.Ne(v => v.OwnerClientId, null),
                    Builders<Vehicle>.Filter.In(v => v.PlateNumber, searchPlates)
                );

                var matchedVehicles = await _vehicleRepo.FindAsync(vehicleFilter, cancellationToken: cancellationToken);
                foreach (var v in matchedVehicles)
                {
                    if (!string.IsNullOrWhiteSpace(v.OwnerClientId) && !string.IsNullOrWhiteSpace(v.PlateNumber))
                    {
                        plateToClientMap[v.PlateNumber] = v.OwnerClientId;
                        var norm = PlateHelper.Normalize(v.PlateNumber);
                        if (!string.IsNullOrEmpty(norm)) plateToClientMap[norm] = v.OwnerClientId;
                        personIds.Add(v.OwnerClientId);
                    }
                }
            }

            var clients = personIds.Count > 0
                ? (await _clientRepo.FindAsync(c => personIds.Contains(c.Id) && !c.IsDeleted, cancellationToken: cancellationToken)).ToDictionary(c => c.Id, c => c)
                : new Dictionary<string, Client>();

            var data = sessions.Select(s =>
            {
                string? clientId = s.PersonId;
                if ((string.IsNullOrWhiteSpace(clientId) || !clients.ContainsKey(clientId)) && !string.IsNullOrWhiteSpace(s.PlateNumber))
                {
                    var norm = PlateHelper.Normalize(s.PlateNumber);
                    if (plateToClientMap.TryGetValue(s.PlateNumber, out var cid) || plateToClientMap.TryGetValue(norm, out cid))
                    {
                        clientId = cid;
                    }
                }

                var client = clientId != null && clients.TryGetValue(clientId, out var c) ? c : null;

                return new ParkingSessionExcelDto
                {
                    PlateNumber = s.PlateNumber,
                    VehicleType = s.VehicleType,
                    Status = s.Status,
                    ClientCode = client?.Code,
                    ClientName = client?.Name,
                    InTime = s.InTime,
                    InLaneName = s.InLaneName,
                    InPlateImagePath = s.InPlateImagePath,
                    InOverviewImagePath = s.InOverviewImagePath,
                    OutTime = s.OutTime,
                    OutLaneName = s.OutLaneName,
                    OutPlateImagePath = s.OutPlateImagePath,
                    OutOverviewImagePath = s.OutOverviewImagePath,
                    Duration = FormatDuration(s.InTime, s.OutTime)
                };
            });

            var bytes = await _excelService.WriteAsync(data, new ParkingSessionExcelProfile(), "Lich_Su_Do_Xe", "BÁO CÁO LỊCH SỬ PHIÊN ĐỖ XE", cancellationToken);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            _logger.LogInformation("Đã xuất {Count}/{Total} dòng lịch sử phiên đỗ xe ra Excel (Truncated: {IsTruncated})",
                sessions.Count, totalCount, isTruncated);

            return (bytes, $"Lich_Su_Do_Xe_{timestamp}.xlsx", isTruncated);
        }

        public async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportAuditLogsAsync(
            AuditLogFilterQuery query,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildAuditLogFilter(query);
            var sort = BuildAuditLogSort(query);

            var totalCount = await _auditLogRepo.CountAsync(filter, cancellationToken);
            var isTruncated = totalCount > MaxExportLimit;

            var logs = await _auditLogRepo.FindAsync(
                filter: filter,
                sort: sort,
                skip: 0,
                limit: MaxExportLimit,
                cancellationToken: cancellationToken);

            var data = logs.Select(l => new AuditLogExcelDto
            {
                CreatedAt = l.CreatedAt,
                ActorUsername = l.ActorUsername,
                ActorRole = l.ActorRole,
                ActionType = l.ActionType,
                TargetEntity = l.TargetEntity,
                TargetDisplay = l.TargetDisplay,
                IsSuccess = l.IsSuccess,
                Reason = l.Reason,
                ErrorMessage = l.ErrorMessage
            });

            var bytes = await _excelService.WriteAsync(data, new AuditLogExcelProfile(), "Nhat_Ky_He_Thong", "BÁO CÁO NHẬT KÝ KIỂM TOÁN HỆ THỐNG", cancellationToken);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            _logger.LogInformation("Đã xuất {Count}/{Total} dòng nhật ký kiểm toán ra Excel (Truncated: {IsTruncated})",
                logs.Count, totalCount, isTruncated);

            return (bytes, $"Nhat_Ky_He_Thong_{timestamp}.xlsx", isTruncated);
        }

        public async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportTrafficSummaryAsync(
            TrafficSummaryFilterQuery query,
            CancellationToken cancellationToken = default)
        {
            var items = await _statisticsService.GetTrafficSummaryAsync(query, cancellationToken);
            var isTruncated = items.Count > MaxExportLimit;

            var data = items.Take(MaxExportLimit).Select((item, index) => new TrafficSummaryExcelDto
            {
                Index = index + 1,
                ClientCode = item.ClientCode,
                ClientName = item.ClientName,
                ClientTypeName = item.ClientTypeName,
                CompanyName = item.CompanyName,
                DepartmentName = item.DepartmentName,
                PlateNumber = item.PlateNumber,
                VehicleType = item.VehicleType,
                InCount = item.InCount,
                OutCount = item.OutCount,
                CompletedCount = item.CompletedCount,
                IsInParking = item.IsInParking
            });

            var bytes = await _excelService.WriteAsync(
                data,
                new TrafficSummaryExcelProfile(),
                "Tong_Hop_Luot_Ra_Vao",
                "BÁO CÁO TỔNG HỢP LƯỢT RA VÀO THEO ĐỐI TƯỢNG",
                cancellationToken);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            _logger.LogInformation("Đã xuất {Count} bản ghi tổng hợp lượt ra vào theo người & xe ra Excel", items.Count);

            return (bytes, $"Bao_Cao_Tong_Hop_Luot_Ra_Vao_{timestamp}.xlsx", isTruncated);
        }

        #region Filter & Sort Builders

        private static FilterDefinition<ParkingSession> BuildParkingSessionFilter(ParkingSessionFilterQuery query)
        {
            var builder = Builders<ParkingSession>.Filter;
            var filters = new List<FilterDefinition<ParkingSession>>();

            if (!string.IsNullOrWhiteSpace(query.PlateNumber))
            {
                var cleanPlate = PlateHelper.Normalize(query.PlateNumber);
                filters.Add(builder.Regex(x => x.PlateNumber, new BsonRegularExpression(cleanPlate, "i")));
            }

            if (query.VehicleType.HasValue)
            {
                filters.Add(builder.Eq(x => x.VehicleType, query.VehicleType.Value));
            }

            if (query.Status.HasValue)
            {
                filters.Add(builder.Eq(x => x.Status, query.Status.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.InLaneName))
            {
                var cleanLane = Regex.Escape(query.InLaneName.Trim());
                filters.Add(builder.Regex(x => x.InLaneName, new BsonRegularExpression(cleanLane, "i")));
            }

            if (!string.IsNullOrWhiteSpace(query.OutLaneName))
            {
                var cleanLane = Regex.Escape(query.OutLaneName.Trim());
                filters.Add(builder.Regex(x => x.OutLaneName, new BsonRegularExpression(cleanLane, "i")));
            }

            if (!string.IsNullOrWhiteSpace(query.PersonId))
            {
                filters.Add(builder.Eq(x => x.PersonId, query.PersonId));
            }

            if (query.FromDate.HasValue)
            {
                filters.Add(builder.Gte(x => x.InTime, query.FromDate.Value));
            }

            if (query.ToDate.HasValue)
            {
                filters.Add(builder.Lte(x => x.InTime, query.ToDate.Value));
            }

            return filters.Count > 0 ? builder.And(filters) : builder.Empty;
        }

        private static SortDefinition<ParkingSession> BuildParkingSessionSort(ParkingSessionFilterQuery query)
        {
            var sortBuilder = Builders<ParkingSession>.Sort;
            var isAsc = query.SortOrder?.ToLower() == "asc";
            return query.SortBy?.ToLower() switch
            {
                "platenumber" => isAsc ? sortBuilder.Ascending(x => x.PlateNumber) : sortBuilder.Descending(x => x.PlateNumber),
                "vehicletype" => isAsc ? sortBuilder.Ascending(x => x.VehicleType) : sortBuilder.Descending(x => x.VehicleType),
                "status" => isAsc ? sortBuilder.Ascending(x => x.Status) : sortBuilder.Descending(x => x.Status),
                "outtime" => isAsc ? sortBuilder.Ascending(x => x.OutTime) : sortBuilder.Descending(x => x.OutTime),
                _ => isAsc ? sortBuilder.Ascending(x => x.InTime) : sortBuilder.Descending(x => x.InTime)
            };
        }

        private static FilterDefinition<AuditLog> BuildAuditLogFilter(AuditLogFilterQuery query)
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

            return filters.Count > 0 ? builder.And(filters) : builder.Empty;
        }

        private static SortDefinition<AuditLog> BuildAuditLogSort(AuditLogFilterQuery query)
        {
            var sortBuilder = Builders<AuditLog>.Sort;
            var isAsc = query.SortOrder?.ToLower() == "asc";
            return query.SortBy?.ToLower() switch
            {
                "actorusername" => isAsc ? sortBuilder.Ascending(x => x.ActorUsername) : sortBuilder.Descending(x => x.ActorUsername),
                "actiontype" => isAsc ? sortBuilder.Ascending(x => x.ActionType) : sortBuilder.Descending(x => x.ActionType),
                "targetentity" => isAsc ? sortBuilder.Ascending(x => x.TargetEntity) : sortBuilder.Descending(x => x.TargetEntity),
                _ => isAsc ? sortBuilder.Ascending(x => x.CreatedAt) : sortBuilder.Descending(x => x.CreatedAt)
            };
        }

        private static string FormatDuration(DateTime? inTime, DateTime? outTime)
        {
            if (!inTime.HasValue) return string.Empty;
            if (!outTime.HasValue) return "Đang trong bãi";

            var diff = outTime.Value - inTime.Value;
            if (diff.TotalMinutes < 1) return $"{Math.Max(0, (int)diff.TotalSeconds)} giây";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} phút";
            if (diff.TotalDays < 1) return $"{(int)diff.TotalHours} giờ {diff.Minutes} phút";

            return $"{(int)diff.TotalDays} ngày {diff.Hours} giờ {diff.Minutes} phút";
        }

        #endregion
    }
}
