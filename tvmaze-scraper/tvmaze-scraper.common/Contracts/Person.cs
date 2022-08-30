namespace tvmaze_scraper.Controllers;

public class Person
{
    public int id { get; set; }
    public string url { get; set; }
    public string name { get; set; }
    public Country country { get; set; }
    public string birthday { get; set; }
    public object deathday { get; set; }
    public string gender { get; set; }
    public Image image { get; set; }
    public int updated { get; set; }
    public Links _links { get; set; }
}