using MongoDB.Driver;
using System;
using System.Configuration;

namespace HPParking.Data
{
    public class MongoContext
    {
        private static readonly MongoClient _client;
        private readonly IMongoDatabase _database;

        static MongoContext()
        {
            string connectionString = ConfigurationManager.AppSettings["MongoDbConnection"]
                ?? throw new InvalidOperationException(
                    "Thiếu cấu hình 'MongoDbConnection' trong App.config.");

            try
            {
                _client = new MongoClient(connectionString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Không thể khởi tạo MongoClient với connection string đã cấu hình: {ex.Message}", ex);
            }
        }

        public MongoContext()
        {
            string databaseName = ConfigurationManager.AppSettings["DatabaseName"]
                ?? throw new InvalidOperationException(
                    "Thiếu cấu hình 'DatabaseName' trong App.config.");

            _database = _client.GetDatabase(databaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}