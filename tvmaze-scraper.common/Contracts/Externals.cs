namespace tvmaze_scraper.common.Contracts;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Externals
    {
        public int? tvrage { get; set; }
        public int? thetvdb { get; set; }
        public string? imdb { get; set; }
    }