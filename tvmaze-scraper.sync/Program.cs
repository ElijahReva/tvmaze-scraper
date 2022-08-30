// See https://aka.ms/new-console-template for more information


using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tvmaze_scraper.common;
using tvmaze_scraper.sync;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.json")
    .Build(); 


var builder = new ServiceCollection();
builder.AddDataAccess(configuration);

var container = builder.BuildServiceProvider();
await SyncHelper.ScrapShows(container);

