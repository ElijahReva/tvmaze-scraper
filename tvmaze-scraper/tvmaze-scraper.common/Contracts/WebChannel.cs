namespace tvmaze_scraper.common.Contracts;

public class WebChannel
{
    public int id { get; set; }
    public string name { get; set; }
    public Country country { get; set; }
    public string? officialSite { get; set; }
}