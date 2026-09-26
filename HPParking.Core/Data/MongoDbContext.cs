using Humanizer;
using MongoDB.Driver;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Core.Data
{
    /// <summary>
    /// Database Context quản lý kết nối MongoDB và các Collection đối tượng trong hệ thống
    /// </summary>
    public class MongoDbContext
    {
        private static readonly Lazy<MongoDbContext> _instance = new(() => new MongoDbContext());
        public static MongoDbContext Instance => _instance.Value;

        private readonly IMongoDatabase _database;
        private readonly MongoClient _client;

        public IMongoClient Client => _client;
        public IMongoDatabase Database => _database;

        private readonly string _serverHost = "127.0.0.1";
        private readonly int _serverPort = 27017;

        public string ServerHost => _serverHost;
        public int ServerPort => _serverPort;

        /// <summary>
        /// Tự động lấy Collection theo tên Type dạng số nhiều (Pluralize: e.g. Clients, Lanes, Vehicles...)
        /// </summary>
        public IMongoCollection<T> GetCollection<T>() => _database.GetCollection<T>(typeof(T).Name.Pluralize());

        /// <summary>
        /// Lấy Collection theo tên chỉ định
        /// </summary>
        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                return GetCollection<T>();
            return _database.GetCollection<T>(collectionName);
        }

        public MongoDbContext() : this(
            ResolveConnectionString(),
            ResolveDatabaseName())
        {
        }

        public MongoDbContext(string connectionString, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Chuỗi kết nối MongoDB không được để trống.", nameof(connectionString));
            }

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            // Thiết lập timeout 3 giây tránh treo phần mềm nếu MongoDB chưa sẵn sàng lúc khởi động
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);

            if (settings.Server != null)
            {
                _serverHost = settings.Server.Host;
                _serverPort = settings.Server.Port;
            }

            _client = new MongoClient(settings);
            _database = _client.GetDatabase(string.IsNullOrWhiteSpace(databaseName) ? "hpparking" : databaseName);
        }

        private static string ResolveConnectionString()
        {
            string? connStr = ConfigurationManager.AppSettings["MongoDb_ConnectionString"]
                ?? ConfigurationManager.AppSettings["MongoDbConnection"];

            if (!string.IsNullOrWhiteSpace(connStr))
            {
                return connStr.Trim();
            }

            return "mongodb://localhost:27017";
        }

        private static string ResolveDatabaseName()
        {
            string? dbName = ConfigurationManager.AppSettings["MongoDb_DatabaseName"]
                ?? ConfigurationManager.AppSettings["DatabaseName"];

            if (!string.IsNullOrWhiteSpace(dbName))
            {
                return dbName.Trim();
            }

            return "hpparking";
        }

        /// <summary>
        /// Kiểm tra nhanh trạng thái sẵn sàng của dịch vụ MongoDB bằng lệnh ping
        /// </summary>
        public async Task<bool> PingAsync(int timeoutMs = 1500, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                var pingCommand = new MongoDB.Bson.BsonDocument("ping", 1);
                await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(pingCommand, cancellationToken: cts.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
