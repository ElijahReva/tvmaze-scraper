using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using tvmaze_scraper.common.Contracts;

namespace tvmaze_scraper.Controllers;

[ApiController]
[Route("[controller]")]
public class ShowsController : ControllerBase
{
    private readonly ILogger<ShowsController> _logger;
    private readonly IMongoDatabase db;

    public ShowsController(ILogger<ShowsController> logger, IMongoDatabase db)
    {
        _logger = logger;
        this.db = db;
    }

    [HttpGet]
    [Route("list")]
    public async Task<IActionResult> List([FromQuery] int page = 1)
    {
        var showCollection = db.GetCollection<Show>("shows");
        var castCollection = db.GetCollection<Person>("cast");
        
        int pageSize = 50;
        var filter = Builders<Show>.Filter.Empty;

        var data = await showCollection.Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var castRequest = data.SelectMany(c => c.cast).Distinct().ToArray();

        var personFilter = Builders<Person>.Filter.In(x => x.id, castRequest);
        var proj = Builders<Person>.Projection.Expression(p => new CastViewModel
        {
            id = p.id,
            birthday = p.birthday,
            name = p.name
        });
        var persons = await castCollection
            .Find(personFilter)
            .Project(proj)
            .ToListAsync();

        var result = data.Select(s =>
        {
            return new ShowViewModel
            {
                id = s.id,
                name = s.name,
                cast = s.cast.Select(c => persons.First(p => p.id == c)).ToArray()
            };
        }).ToArray();

        return Ok(result);
    }
}