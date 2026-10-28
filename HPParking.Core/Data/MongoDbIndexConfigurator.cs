using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
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
        public const string TariffCodeActiveIndexName = "UX_Tariffs_Code_Active";
        public const string ParkingSessionActiveCardIndexName = "UX_ParkingSessions_Active_CardId";

        /// <summary>
        /// Tạo Index duy nhất (Unique Index) và các index nghiệp vụ cho MongoDB
        /// </summary>
        public static async Task EnsureIndexesAsync(MongoDbContext context, CancellationToken cancellationToken = default)
        {
            var collection = context.GetCollection<ClientApprovalRequest>();
            var indexModel = GetTokenUniqueIndexModel();
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);

            var sessionCollection = context.GetCollection<ParkingSession>();
            var sessionIndexModel = GetParkingSessionInOrderIndexModel();
            await sessionCollection.Indexes.CreateOneAsync(sessionIndexModel, cancellationToken: cancellationToken);

            var activeCardSessionModel = GetParkingSessionActiveCardUniqueIndexModel();
            await sessionCollection.Indexes.CreateOneAsync(activeCardSessionModel, cancellationToken: cancellationToken);

            var tariffCollection = context.GetCollection<Tariff>();
            var tariffIndexModel = GetTariffCodeUniqueIndexModel();
            await tariffCollection.Indexes.CreateOneAsync(tariffIndexModel, cancellationToken: cancellationToken);
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

        /// <summary>
        /// Lấy IndexModel Partial Unique Index cho Tariff.Code khi IsDeleted == false
        /// </summary>
        public static CreateIndexModel<Tariff> GetTariffCodeUniqueIndexModel()
        {
            var indexKeys = Builders<Tariff>.IndexKeys.Ascending(x => x.Code);
            var partialFilter = Builders<Tariff>.Filter.Eq(x => x.IsDeleted, false);
            var indexOptions = new CreateIndexOptions<Tariff>
            {
                Name = TariffCodeActiveIndexName,
                Unique = true,
                PartialFilterExpression = partialFilter
            };
            return new CreateIndexModel<Tariff>(indexKeys, indexOptions);
        }

        /// <summary>
        /// Lấy IndexModel Composite Partial Unique Index trên (CardId, TargetType) cho ParkingSession khi Status == Active
        /// </summary>
        public static CreateIndexModel<ParkingSession> GetParkingSessionActiveCardUniqueIndexModel()
        {
            var indexKeys = Builders<ParkingSession>.IndexKeys
                .Ascending(x => x.CardId)
                .Ascending(x => x.TargetType);
            var partialFilter = Builders<ParkingSession>.Filter.And(
                Builders<ParkingSession>.Filter.Eq(x => x.Status, ParkingSessionStatus.Active),
                Builders<ParkingSession>.Filter.Eq(x => x.IsDeleted, false),
                Builders<ParkingSession>.Filter.Type(x => x.CardId, BsonType.ObjectId)
            );
            var indexOptions = new CreateIndexOptions<ParkingSession>
            {
                Name = ParkingSessionActiveCardIndexName,
                Unique = true,
                PartialFilterExpression = partialFilter
            };
            return new CreateIndexModel<ParkingSession>(indexKeys, indexOptions);
        }
    }
}
