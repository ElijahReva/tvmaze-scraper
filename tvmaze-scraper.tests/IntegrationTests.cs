using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using tvmaze_scraper.common.Contracts;
using tvmaze_scraper.sync;
using Xunit;
using Xunit.Abstractions;

namespace tvmaze_scraper.tests;

public class IntegrationTests : IDisposable
{
    private readonly ITestOutputHelper helper;
    private readonly WebApplicationFactory<Program> application;
    private readonly HttpClient client;

    public IntegrationTests(ITestOutputHelper helper)
    {
        this.helper = helper;
        application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(c => c.AddXunit(helper));
        });
        client = application.CreateClient();
        var db = application.Services.GetService<IMongoDatabase>();
        db.DropCollection("shows");
        db.DropCollection("cast");
        db.DropCollection("syncLock");
    }
    
    [Fact]
    public async Task Sync_List_Success()
    {
        var shows = application.Services.GetService<IMongoCollection<Show>>();
        await SyncHelper.ScrapShows(application.Services,
            r => shows.CountDocuments(Builders<Show>.Filter.Empty) < 150);
        
        
        
    }
    
    [Fact]
    public void Sync_List_Pagination()
    {
        
    }

    public void Dispose()
    {
        application.Dispose();
        client.Dispose();
    }
}