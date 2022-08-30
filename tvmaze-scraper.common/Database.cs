using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace tvmaze_scraper.common;

public static class Database
{
    public static IMongoClient CreateMongoClient(this IConfigurationRoot config)
    {
        var connectionString = config.GetConnectionString("MongoDb");
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ConnectTimeout = TimeSpan.FromSeconds(4);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(4);
        return new MongoClient(settings);
    }

    public static IMongoDatabase GetDatabase(this IMongoClient client)
    {
        return client.GetDatabase("tvmaze-scrapper");
    }
}