using MongoDB.Driver;
using System.Configuration;

namespace HPParking.Data
{
    public class MongoContext
    {
        private readonly IMongoDatabase _database;

        public MongoContext()
        {
            string connectionString = ConfigurationManager.AppSettings["MongoConnectionString"];
            string databaseName = ConfigurationManager.AppSettings["MongoDatabase"];

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}