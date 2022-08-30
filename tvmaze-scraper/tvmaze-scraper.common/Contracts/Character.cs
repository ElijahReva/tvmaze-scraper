namespace tvmaze_scraper.Controllers;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Character
{
    public int id { get; set; }
    public string url { get; set; }
    public string name { get; set; }
    public object image { get; set; }
    public Links _links { get; set; }
}