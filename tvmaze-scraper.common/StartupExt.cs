using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using tvmaze_scraper.common.Contracts;

namespace tvmaze_scraper.common;

public static class StartupExt
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfigurationRoot config)
    {
        services.AddSingleton(config.CreateMongoClient());
        services.AddSingleton(ctx =>
        {
            var client = ctx.GetService<IMongoClient>();
            return client.GetDatabase("tvmaze-scrapper");
        });     
        
        services.AddSingleton(ctx =>
        {
            var db = ctx.GetService<IMongoDatabase>();
            return db.GetCollection<Show>("shows");
        });  
        
        services.AddSingleton(ctx =>
        {
            var db = ctx.GetService<IMongoDatabase>();
            return db.GetCollection<Person>("cast");
        });
        
        services.AddSingleton(f =>
        {
            var db = f.GetService<IMongoDatabase>();
            return db.GetCollection<SyncLock>("syncLock");
        });

        return services; 
    }
}