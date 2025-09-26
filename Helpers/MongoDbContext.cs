using System;
using System.Configuration;
using MongoDB.Driver;

namespace SparkPoint_Server.Helpers
{
    /// <summary>
    /// MongoDB context with singleton MongoClient for connection reuse
    /// </summary>
    public class MongoDbContext : IDisposable
    {
        private readonly IMongoDatabase _database;
        private static readonly Lazy<MongoClient> _lazyClient = new Lazy<MongoClient>(CreateMongoClient);
        private static MongoClient Client => _lazyClient.Value;

        public MongoDbContext()
        {
            var databaseName = GetDatabaseName();
            _database = Client.GetDatabase(databaseName);
        }

        /// <summary>
        /// Creates the MongoClient singleton with optimal settings
        /// </summary>
        /// <returns>Configured MongoClient instance</returns>
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

        /// <summary>
        /// Gets connection string from configuration with environment variable fallback
        /// </summary>
        /// <returns>MongoDB connection string</returns>
        private static string GetConnectionString()
        {
            // Try environment variable first (for production)
            var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback to app settings
                connectionString = ConfigurationManager.AppSettings["MongoDbConnection"];
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "MongoDB connection string not found. Set MONGODB_CONNECTION_STRING environment variable or MongoDbConnection in app settings.");
            }

            return connectionString;
        }

        /// <summary>
        /// Gets database name from configuration with environment variable fallback
        /// </summary>
        /// <returns>MongoDB database name</returns>
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

        /// <summary>
        /// Gets a MongoDB collection
        /// </summary>
        /// <typeparam name="T">Document type</typeparam>
        /// <param name="name">Collection name</param>
        /// <returns>MongoDB collection</returns>
        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }

        /// <summary>
        /// Gets the database instance
        /// </summary>
        /// <returns>MongoDB database</returns>
        public IMongoDatabase GetDatabase()
        {
            return _database;
        }

        /// <summary>
        /// Tests database connectivity
        /// </summary>
        /// <returns>True if database is accessible</returns>
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

        public void Dispose()
        {
            // MongoClient manages its own resources and should be kept as singleton
            // No disposal needed here
        }
    }
}