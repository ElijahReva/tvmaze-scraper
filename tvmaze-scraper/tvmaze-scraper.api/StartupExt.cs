using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace tvmaze_scraper;

public static class StartupExt
{
    private class GuidAsStringRepresentationConvention : ConventionBase, IMemberMapConvention
    {
        public void Apply(BsonMemberMap memberMap)
        {
            if (memberMap.MemberType == typeof(Guid))
            {
                memberMap.SetSerializer(
                    new GuidSerializer(BsonType.String));
            }
            else if (memberMap.MemberType == typeof(Guid?))
            {
                memberMap.SetSerializer(
                    new NullableSerializer<Guid>(new GuidSerializer(BsonType.String)));
            }
        }
    }
    public static WebApplicationBuilder AddDataAccess(this WebApplicationBuilder builder)
    {
        IMongoClient ImplementationFactory(IServiceProvider s)
        {
            var connectionString = builder.Configuration.GetConnectionString("MongoDb");
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ConnectTimeout = TimeSpan.FromSeconds(4);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(4);
            return new MongoClient(settings);
        }

        builder.Services.AddSingleton(ImplementationFactory);
        builder.Services.AddSingleton(ctx =>
        {
            var client = ctx.GetService<IMongoClient>();
            return client.GetDatabase("tvmaze-scrapper");
        });

        return builder; 
    }
}