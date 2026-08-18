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
            string connectionString = ConfigurationManager.AppSettings["MongoDbConnection"];
            _client = new MongoClient(connectionString);
        }

        public MongoContext()
        {
            string databaseName = ConfigurationManager.AppSettings["DatabaseName"];
            _database = _client.GetDatabase(databaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}