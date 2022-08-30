using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Polly;
using tvmaze_scraper.common;
using tvmaze_scraper.common.Contracts;

namespace tvmaze_scraper.sync;

public static class SyncHelper
{
 
    public static async Task ScrapShows(IServiceProvider services, Func<HttpResponseMessage, bool>? condition = null)
    {
        var lockCollection = services.GetService<IMongoCollection<SyncLock>>();
        var syncLock = lockCollection.GetLock();
        if (CheckRestart(lockCollection, ref syncLock)) return;

        using var client = Http.CreateClient();
        var cache = new ConcurrentDictionary<int, int>();
        var showCollection = services.GetService<IMongoCollection<Show>>();
        var castCollection = services.GetService<IMongoCollection<Person>>();
        var response = await client.GetWithRetry($"shows?page={syncLock.LastPage}");
        condition ??= (r) => r.StatusCode != HttpStatusCode.NotFound;
        while (condition(response))
        {
            if (response.IsSuccessStatusCode)
            {
                var shows = await response.Content.ReadFromJsonAsync<Show[]>() ?? Array.Empty<Show>();
                var showTasks = shows
                    .Select(s => s.ScrapShowCast(client, cache, castCollection))
                    .ToArray();
                await Task.WhenAll(showTasks);
                
                foreach (var show in showTasks.Select(s => s.Result))
                {
                    // extract to Database
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
            Console.WriteLine($"PAGE -- [{syncLock.LastPage}]");
            response = await client.GetWithRetry($"shows?page={syncLock.LastPage}");
        }

        lockCollection.Finish();
    }

    private static async Task<Show> ScrapShowCast(
        this Show show,
        HttpClient client,
        ConcurrentDictionary<int, int> cache,
        IMongoCollection<Person> castCollection)
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
            if (cache.ContainsKey(person.id)) continue;
            await castCollection.ReplaceOneAsync(
                Builders<Person>.Filter.Eq(s => s.id, person.id),
                person,
                new ReplaceOptions
                {
                    IsUpsert = true
                });
            cache.TryAdd(person.id, person.id);
            Console.WriteLine($"Person Id: {person.id} \t Name:{person.name}");
        }

        return show;
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