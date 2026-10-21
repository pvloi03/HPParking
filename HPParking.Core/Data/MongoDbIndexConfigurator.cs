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

        /// <summary>
        /// Tạo Index duy nhất (Unique Index) cho trường Token trong collection ClientApprovalRequests (ADR 0042)
        /// </summary>
        public static async Task EnsureIndexesAsync(MongoDbContext context, CancellationToken cancellationToken = default)
        {
            var collection = context.GetCollection<ClientApprovalRequest>();
            var indexModel = GetTokenUniqueIndexModel();
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
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
    }
}
