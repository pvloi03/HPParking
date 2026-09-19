using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Triển khai dịch vụ tra cứu chỉ đọc lịch sử và phiên đỗ xe hiện hành (Read-Only)
    /// </summary>
    public class ParkingSessionService : IParkingSessionService
    {
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly ILogger<ParkingSessionService> _logger;

        public ParkingSessionService(
            IRepository<ParkingSession> sessionRepo,
            IRepository<Client> clientRepo,
            ILogger<ParkingSessionService> logger)
        {
            _sessionRepo = sessionRepo;
            _clientRepo = clientRepo;
            _logger = logger;
        }

        public async Task<PagedResult<ParkingSessionDto>> GetParkingSessionsPagedAsync(ParkingSessionFilterQuery query, CancellationToken cancellationToken = default)
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

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

            var sortBuilder = Builders<ParkingSession>.Sort;
            var isAsc = query.SortOrder?.ToLower() == "asc";
            var sort = query.SortBy?.ToLower() switch
            {
                "platenumber" => isAsc ? sortBuilder.Ascending(x => x.PlateNumber) : sortBuilder.Descending(x => x.PlateNumber),
                "status" => isAsc ? sortBuilder.Ascending(x => x.Status) : sortBuilder.Descending(x => x.Status),
                "outtime" => isAsc ? sortBuilder.Ascending(x => x.OutTime) : sortBuilder.Descending(x => x.OutTime),
                _ => isAsc ? sortBuilder.Ascending(x => x.InTime) : sortBuilder.Descending(x => x.InTime)
            };

            var totalCount = await _sessionRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var sessions = await _sessionRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            var items = sessions.Adapt<List<ParkingSessionDto>>();

            _logger.LogInformation("Tra cứu danh sách phiên đỗ xe: tìm thấy {TotalCount} bản ghi (Trang {PageIndex}/{TotalPages}).",
                totalCount, query.PageIndex, (int)Math.Ceiling((double)totalCount / query.PageSize));

            return new PagedResult<ParkingSessionDto>(items, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<ParkingSessionDetailDto> GetParkingSessionByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepo.GetByIdAsync(id, cancellationToken);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin phiên đỗ xe.", ErrorCodes.PARKING_SESSION_NOT_FOUND);
            }

            var detail = session.Adapt<ParkingSessionDetailDto>();

            if (!string.IsNullOrWhiteSpace(session.PersonId))
            {
                var client = await _clientRepo.GetByIdAsync(session.PersonId, cancellationToken);
                if (client != null)
                {
                    detail.PersonFullName = client.Name;
                    detail.PersonPhoneNumber = client.PhoneNumber;
                    detail.PersonCode = client.Code;
                }
            }

            _logger.LogInformation("Lấy chi tiết phiên đỗ xe {Id}: Biển số {PlateNumber}, Trạng thái {Status}.",
                session.Id, session.PlateNumber, session.Status);

            return detail;
        }
    }
}
