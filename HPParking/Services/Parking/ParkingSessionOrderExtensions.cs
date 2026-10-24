using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Services.Parking
{
    /// <summary>
    /// Tiện ích nghiệp vụ miền (Domain Service / Extension) phục vụ việc tính toán và cấp phát
    /// số thứ tự lượt vào (InOrderNumber) cho thực thể ParkingSession theo từng phân hệ (Vehicle vs Pedestrian).
    /// Tuân thủ ADR 0014 và referential-integrity-matrix.md §1.3 (nghiệp vụ phiên thuộc tầng Services).
    /// </summary>
    public static class ParkingSessionOrderExtensions
    {
        /// <summary>
        /// Lấy số thứ tự lượt vào tiếp theo (InOrderNumber = Max + 1) theo phân hệ (Vehicle hoặc Pedestrian).
        /// Truy vấn trên toàn bộ collection khớp chuẩn Compound Index (TargetType ASC, InOrderNumber DESC) đạt O(1),
        /// không lọc IsDeleted để đảm bảo tính tăng đơn điệu và không cấp trùng số nếu bản ghi bị xóa mềm.
        /// Trả về 1 nếu chưa có lượt vào nào trong cơ sở dữ liệu.
        /// </summary>
        public static async Task<long> GetNextInOrderNumberAsync(
            this IRepository<ParkingSession> sessionRepository,
            LaneTargetType targetType,
            CancellationToken cancellationToken = default)
        {
            if (sessionRepository == null) return 1;

            var filter = Builders<ParkingSession>.Filter.And(
                Builders<ParkingSession>.Filter.Eq(x => x.TargetType, targetType),
                Builders<ParkingSession>.Filter.Ne(x => x.InOrderNumber, null)
            );
            var sort = Builders<ParkingSession>.Sort.Descending(x => x.InOrderNumber);

            var task = sessionRepository.FindAsync(filter, sort, skip: 0, limit: 1, cancellationToken);
            var latest = task != null ? await task : null;
            return (latest != null && latest.Count > 0 ? (latest[0].InOrderNumber ?? 0) : 0) + 1;
        }

        /// <summary>
        /// Gán số thứ tự lượt vào (InOrderNumber) cho một phiên đỗ xe nếu phiên có InTime và chưa có số.
        /// </summary>
        public static async Task AssignInOrderNumberAsync(
            this IRepository<ParkingSession> sessionRepository,
            ParkingSession session,
            CancellationToken cancellationToken = default)
        {
            if (session == null || !session.InTime.HasValue || session.InOrderNumber.HasValue) return;
            session.InOrderNumber = await sessionRepository.GetNextInOrderNumberAsync(session.TargetType, cancellationToken);
        }

        /// <summary>
        /// Gán số thứ tự lượt vào (InOrderNumber) cho một danh sách các phiên đỗ xe theo từng phân hệ.
        /// BẮT BUỘC sắp xếp các phiên chưa có số theo InTime tăng dần (từ cũ đến mới) trước khi cấp số,
        /// đảm bảo lượt vào sớm hơn nhận số nhỏ hơn, lượt vào mới nhất nhận số lớn nhất (#lớn -> #bé khi sắp xếp giảm dần theo InTime).
        /// </summary>
        public static async Task AssignInOrderNumbersAsync(
            this IRepository<ParkingSession> sessionRepository,
            IEnumerable<ParkingSession> sessions,
            CancellationToken cancellationToken = default)
        {
            if (sessionRepository == null || sessions == null) return;

            var unassignedGroups = sessions
                .Where(s => s.InTime.HasValue && !s.InOrderNumber.HasValue)
                .GroupBy(s => s.TargetType);

            foreach (var group in unassignedGroups)
            {
                var targetType = group.Key;
                // Sắp xếp tăng dần theo InTime để lượt cũ nhận số nhỏ, lượt mới nhận số lớn
                var chronologicallyOrdered = group.OrderBy(s => s.InTime!.Value).ToList();
                if (chronologicallyOrdered.Count == 0) continue;

                var startOrder = await sessionRepository.GetNextInOrderNumberAsync(targetType, cancellationToken);
                for (int i = 0; i < chronologicallyOrdered.Count; i++)
                {
                    chronologicallyOrdered[i].InOrderNumber = startOrder + i;
                }
            }
        }
    }
}
