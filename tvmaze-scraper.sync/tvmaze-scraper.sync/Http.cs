using System.Net;
using System.Net.Http.Headers;
using Polly;

namespace tvmaze_scraper.sync;

public static class Http
{
    public static HttpClient CreateClient()
    {
        var client = new HttpClient();
        
        client.BaseAddress = new Uri("https://api.tvmaze.com/");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
    
    public static Task<HttpResponseMessage> GetWithRetry(this HttpClient client, string path)
    {
        var tooManyRequests = Policy
            .HandleResult<HttpResponseMessage>(message => message.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(15)
            }, (result, timeSpan, retryCount, context) => {
                Console.WriteLine($"Request failed with {result.Result.StatusCode}. Retry count = {retryCount}. Waiting {timeSpan} before next retry. ");
            });;
        
        return tooManyRequests
            .ExecuteAsync(() => client.GetAsync(path));
    }
}