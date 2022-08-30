using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Polly;
using tvmaze_scraper.common.Contracts;

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
        if (@lock is null)
        {
            return new SyncLock()
            {
                LastPage = 0,
                IsRequested = false
            };
        }
        return @lock;
    }


    // Return p
    public static SyncLock ResetPageCount(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Set(c => c.LastPage, 0)
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
                .Inc(c => c.LastPage, 1),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                ReturnDocument = ReturnDocument.After
            }
        );

        return @lock;
    }

    public static async Task Sync()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.json");

        var config = configuration.Build();
        var mongoClient = config.CreateMongoClient();
        var db = mongoClient.GetDatabase();
        await db.SyncShows();
    }

    private static async Task SyncShows(
        this IMongoDatabase db)
    {
        var lockCollection = db.GetCollection<SyncLock>("syncLock");
        var syncLock = lockCollection.GetLock();
        if (CheckRestart(lockCollection, ref syncLock)) return;

        using var client = Http.CreateClient();

        var showCollection = db.GetCollection<Show>("shows");
        var castCollection = db.GetCollection<Person>("cast");
        var response = await client.GetWithRetry($"shows?page={syncLock.LastPage}");
        while (response.StatusCode != HttpStatusCode.NotFound)
        {
            if (response.IsSuccessStatusCode)
            {
                var shows = await response.Content.ReadFromJsonAsync<Show[]>() ?? Array.Empty<Show>();
                foreach (var show in shows)
                {
                    var castResponse = await client.GetWithRetry($"shows/{show.id}/cast");
                    var cast = await castResponse.Content.ReadFromJsonAsync<Cast[]>() ?? Array.Empty<Cast>();
                    var persons = cast
                        .Select(c => c.person)
                        .OrderByDescending(p => p.birthday)
                        .DistinctBy(p => p.id)
                        .ToArray();
                    show.cast = persons.Select(p => p.id).ToArray();
                    foreach (var person in persons)
                    {
                        await castCollection.ReplaceOneAsync(
                            Builders<Person>.Filter.Eq(s => s.id, person.id),
                            person,
                            new ReplaceOptions
                            {
                                IsUpsert = true
                            });
                        
                        Console.WriteLine($"Person Id: {person.id} \t Name:{person.name}");
                    }

                    await showCollection.ReplaceOneAsync(
                        Builders<Show>.Filter.Eq(s => s.id, show.id),
                        show,
                        new ReplaceOptions
                        {
                            IsUpsert = true
                        });
                    
                    Console.WriteLine($"Show Id:{show.id} \t Name:{show.name}");
                }
            }
            else
            {
                Console.WriteLine("Internal server Error\n");
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                Environment.Exit(-1);
            }

            syncLock = lockCollection.IncShowPage();
            Console.WriteLine($"LAST PAGE -- [{syncLock.LastPage}]");
            response = await client.GetWithRetry($"shows?page={syncLock.LastPage}");
        }

        lockCollection.Finish();
    }

    private static bool CheckRestart(IMongoCollection<SyncLock> lockCollection, ref SyncLock syncLock)
    {
        if (!syncLock.IsRequested && syncLock.LastPage > 0)
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