using System;
using System.Configuration;
using MongoDB.Driver;

namespace SparkPoint_Server.Helpers
{

    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private static readonly Lazy<MongoClient> _lazyClient = new Lazy<MongoClient>(CreateMongoClient);
        private static MongoClient Client => _lazyClient.Value;

        public MongoDbContext()
        {
            var databaseName = GetDatabaseName();
            _database = Client.GetDatabase(databaseName);
        }

        private static MongoClient CreateMongoClient()
        {
            var connectionString = GetConnectionString();
            
            // Configure MongoClient with optimal settings
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            
            // Connection pool settings
            settings.MaxConnectionPoolSize = 100;
            settings.MinConnectionPoolSize = 5;
            settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(30);
            settings.ConnectTimeout = TimeSpan.FromSeconds(30);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
            settings.SocketTimeout = TimeSpan.FromMinutes(2);
            
            // Retry settings
            settings.RetryWrites = true;
            settings.RetryReads = true;
            
            return new MongoClient(settings);
        }

        private static string GetConnectionString()
        {
            // Try environment variable first (for production)
            var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = ConfigurationManager.AppSettings["MongoDbConnection"];
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "MongoDB connection string not found. Set MONGODB_CONNECTION_STRING environment variable or MongoDbConnection in app settings.");
            }

            return connectionString;
        }

        private static string GetDatabaseName()
        {
            // Try environment variable first (for production)
            var databaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME");
            
            if (string.IsNullOrEmpty(databaseName))
            {
                // Fallback to app settings
                databaseName = ConfigurationManager.AppSettings["MongoDbDatabase"];
            }

            if (string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException(
                    "MongoDB database name not found. Set MONGODB_DATABASE_NAME environment variable or MongoDbDatabase in app settings.");
            }

            return databaseName;
        }
        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }

        public IMongoDatabase GetDatabase()
        {
            return _database;
        }

        public bool TestConnection()
        {
            try
            {
                // Simple ping to test connectivity
                _database.RunCommand<object>(new MongoDB.Bson.BsonDocument("ping", 1));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}