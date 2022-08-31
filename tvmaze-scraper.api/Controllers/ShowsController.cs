using System.Net;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using tvmaze_scraper.common.Contracts;

namespace tvmaze_scraper.api.Controllers;

[ApiController]
[Route("[controller]")]
public class ShowsController : ControllerBase
{
    private readonly IMongoDatabase db;
    private readonly IMongoCollection<Show> showCollection;
    private readonly IMongoCollection<Person> castCollection;

    public ShowsController(IMongoDatabase db, IMongoCollection<Show> showCollection, IMongoCollection<Person> castCollection)
    {
        this.db = db;
        this.showCollection = showCollection;
        this.castCollection = castCollection;
    }

    [HttpGet]
    [Route("list")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ShowViewModel[]), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var filter = Builders<Show>.Filter.Empty;

        var shows = await showCollection.Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
        
        if (shows.Count == 0)
        {
            return NotFound();
        }

        var castRequest = shows.SelectMany(c => c.cast).Distinct().ToArray();

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

        var result = shows.Select(s =>
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