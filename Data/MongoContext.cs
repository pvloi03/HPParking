using MongoDB.Driver;
using System.Configuration;

namespace HPParking.Data
{
    public class MongoContext
    {
        private static readonly MongoClient _client;
        private readonly IMongoDatabase _database;

        static MongoContext()
        {
            string connectionString = ConfigurationManager.AppSettings["MongoConnectionString"];
            _client = new MongoClient(connectionString);
        }

        public MongoContext()
        {
            string databaseName = ConfigurationManager.AppSettings["MongoDatabase"];
            _database = _client.GetDatabase(databaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}