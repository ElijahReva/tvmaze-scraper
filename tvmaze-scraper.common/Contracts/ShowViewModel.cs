namespace tvmaze_scraper.common.Contracts;

public class ShowViewModel
{
    public int id { get; set; }
    public string name { get; set; }
    
    public CastViewModel[] cast { get; set; }
}

public class CastViewModel
{
    public int id { get; set; }
    public string  name { get; set; }
    public string birthday { get; set; }
}