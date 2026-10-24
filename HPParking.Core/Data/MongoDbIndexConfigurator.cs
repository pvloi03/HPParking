using HPParking.Core.Models.Entities;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Core.Data
{
    /// <summary>
    /// Khởi tạo và đồng bộ các chỉ mục (Indexes) cho cơ sở dữ liệu MongoDB
    /// </summary>
    public static class MongoDbIndexConfigurator
    {
        public const string TokenIndexName = "UX_ClientApprovalRequests_Token";
        public const string ParkingSessionInOrderIndexName = "IX_ParkingSessions_TargetType_InOrderNumber";

        /// <summary>
        /// Tạo Index duy nhất (Unique Index) cho trường Token trong collection ClientApprovalRequests (ADR 0042)
        /// và Index sắp xếp lượt vào theo phân hệ trong ParkingSessions
        /// </summary>
        public static async Task EnsureIndexesAsync(MongoDbContext context, CancellationToken cancellationToken = default)
        {
            var collection = context.GetCollection<ClientApprovalRequest>();
            var indexModel = GetTokenUniqueIndexModel();
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);

            var sessionCollection = context.GetCollection<ParkingSession>();
            var sessionIndexModel = GetParkingSessionInOrderIndexModel();
            await sessionCollection.Indexes.CreateOneAsync(sessionIndexModel, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Tạo Index duy nhất (Unique Index) trực tiếp trên một IMongoCollection
        /// </summary>
        public static async Task EnsureApprovalTokenIndexAsync(IMongoCollection<ClientApprovalRequest> collection, CancellationToken cancellationToken = default)
        {
            var indexModel = GetTokenUniqueIndexModel();
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Lấy IndexModel được định nghĩa cho Token để phục vụ kiểm thử đơn vị hoặc kiểm tra cấu hình
        /// </summary>
        public static CreateIndexModel<ClientApprovalRequest> GetTokenUniqueIndexModel()
        {
            var indexKeys = Builders<ClientApprovalRequest>.IndexKeys.Ascending(x => x.Token);
            var indexOptions = new CreateIndexOptions
            {
                Unique = true,
                Name = TokenIndexName,
                Sparse = true
            };
            return new CreateIndexModel<ClientApprovalRequest>(indexKeys, indexOptions);
        }

        /// <summary>
        /// Lấy IndexModel được định nghĩa cho InOrderNumber theo từng TargetType trong ParkingSession
        /// </summary>
        public static CreateIndexModel<ParkingSession> GetParkingSessionInOrderIndexModel()
        {
            var indexKeys = Builders<ParkingSession>.IndexKeys
                .Ascending(x => x.TargetType)
                .Descending(x => x.InOrderNumber);
            var indexOptions = new CreateIndexOptions
            {
                Name = ParkingSessionInOrderIndexName
            };
            return new CreateIndexModel<ParkingSession>(indexKeys, indexOptions);
        }
    }
}
