using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
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
        // arrange
        var shows = application.Services.GetService<IMongoCollection<Show>>();
        Func<HttpResponseMessage,bool> @while = r => shows.CountDocuments(Builders<Show>.Filter.Empty) < 150;

        // act
        await SyncHelper.ScrapShows(application.Services, @while);
        var response = await client.GetAsync("Shows/list");

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<ShowViewModel[]>();
        list.Should().HaveCount(50);
        var showViewModel = list.First();
        showViewModel.id.Should().Be(1);
        showViewModel.name.Should().NotBeNullOrWhiteSpace();
        showViewModel.cast.Should().NotBeNull();
        showViewModel.cast.Should().HaveCountGreaterThan(0);
        var castViewModel = showViewModel.cast.First();
        castViewModel.id.Should().BeGreaterThan(0);
        castViewModel.name.Should().NotBeNullOrWhiteSpace();
    }
    
    [Fact]
    public async Task Sync_List_Pagination()
    {
        // arrange
        var shows = application.Services.GetService<IMongoCollection<Show>>();
        Func<HttpResponseMessage,bool> @while = r => shows.CountDocuments(Builders<Show>.Filter.Empty) < 150;

        // act
        await SyncHelper.ScrapShows(application.Services, @while);
        var response = await client.GetAsync("Shows/list?page=2");

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<ShowViewModel[]>();
        list.Should().HaveCount(50);
        var showViewModel = list.First();
        showViewModel.id.Should().BeGreaterThan(50);
    }
    
    [Fact]
    public async Task Sync_List_Pagination_NotFound()
    {
        // act
        var response = await client.GetAsync("Shows/list");

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        application.Dispose();
        client.Dispose();
    }
}