using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

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


    public class SyncRequest
    {
        public int Key { get; set; }
    }

    [HttpGet]
    [Route("list")]
    public void List()
    {
        // in db syncLock
        // set requested = true
        // if true return current number
        return;
    }
}