using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using tvmaze_scraper.Controllers;

namespace tvmaze_scraper.sync;

public static class SyncHelper
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
    
    // Return p
    public static SyncLock GetLock(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Set(c => c.Id, 1)
                .Set(c => c.IsRequested, true),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.Before
            }
        );
        
        return @lock;
    }
    
        
    // Return p
    public static SyncLock ResetPageCount(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Set(c => c.LastCastPage, 0)
                .Set(c => c.LastShowPage, 0)
                .Set(c => c.IsRequested, true),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                ReturnDocument = ReturnDocument.After
            }
        );
        
        return @lock;
    }  
    
    // Return p
    public static SyncLock Finish(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Set(c => c.IsRequested, false),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                ReturnDocument = ReturnDocument.After
            }
        );
        
        return @lock;
    } 
    
    // Return p
    public static SyncLock IncShowPage(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Inc(c => c.LastShowPage, 1),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                ReturnDocument = ReturnDocument.After
            }
        );
        
        return @lock;
    }
    
    public static SyncLock IncCastPage(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Inc(c => c.LastCastPage, 1),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                ReturnDocument = ReturnDocument.After
            }
        );
        
        return @lock;
    }

    public static async Task Sync()
    {
        var configuration =  new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.json");
            
        var config = configuration.Build();
        var mongoClient = config.CreateMongoClient();
        var db = mongoClient.GetDatabase();
        var lockCollection = db.GetCollection<SyncLock>("syncLock");
        var syncLock = lockCollection.GetLock();
        if (CheckRestart(lockCollection, ref syncLock)) return;

        var showCollection = db.GetCollection<Show>("shows");
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.tvmaze.com/");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    
           
        await SyncShows(client, syncLock, showCollection, lockCollection);

        lockCollection.Finish();
    }

    private static async Task SyncShows(HttpClient client, SyncLock syncLock, IMongoCollection<Show> showCollection,
        IMongoCollection<SyncLock> lockCollection)
    {

        var castTaskList = new List<Task<Cast>>();
        var response = await client.GetAsync($"shows?page={syncLock.LastShowPage}&embeded=cast");
        while (response.StatusCode != HttpStatusCode.NotFound)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(5_000);

                response = await client.GetAsync($"shows?page={syncLock.LastShowPage}");
                continue;
            }

            if (response.IsSuccessStatusCode)
            {
                var shows = await response.Content.ReadFromJsonAsync<Show[]>() ?? new Show[] { };
                // add references to cast or embed cast
                var writeDocs = shows.Select(s =>
                {
                    var f = Builders<Show>.Filter.Eq(s1 => s1.id, s.id);
                    var replaceOneModel = new ReplaceOneModel<Show>(f, s)
                    {
                        IsUpsert = true
                    };
                    return replaceOneModel;
                }).ToList();
                await showCollection.BulkWriteAsync(writeDocs);
                foreach (var show in shows)
                {
                    Console.WriteLine($"Id:{show.id} \t Name:{show.name}");
                }
            }
            else
            {
                Console.WriteLine("Internal server Error\n");
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
            }

            syncLock = lockCollection.IncShowPage();
            Console.WriteLine($"LAST PAGE -- [{syncLock.LastShowPage}]");
            response = await client.GetAsync($"shows?page={syncLock.LastShowPage}");
        }
    }

    private static bool CheckRestart(IMongoCollection<SyncLock> lockCollection, ref SyncLock syncLock)
    {
        if (!syncLock.IsRequested && syncLock.LastShowPage > 0 && syncLock.LastCastPage > 0)
        {
            if (Environment.GetCommandLineArgs().Length > 0 && Environment.GetCommandLineArgs()[1] == "-y")
            {
                syncLock = lockCollection.ResetPageCount();
                return false;
            }

            Console.WriteLine("Restart Sync from page 0? Y - Yes");
            var input = Console.ReadLine();
            if (input == "Y")
            {
                syncLock = lockCollection.ResetPageCount();
                
                return false;
            }

            return true;
        }

        return false;
    }
}