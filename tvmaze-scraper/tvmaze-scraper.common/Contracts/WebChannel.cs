namespace tvmaze_scraper.Controllers;

public class WebChannel
{
    public int id { get; set; }
    public string name { get; set; }
    public Country country { get; set; }
    public object officialSite { get; set; }
}