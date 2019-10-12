namespace ReverseDNSGeolocation.GeonamesWebApi
{
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public static class GeonamesApi
    {
        public static async Task<SearchResults> Search(string url)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                var json = await client.DownloadStringTaskAsync(url);
                var results = JsonConvert.DeserializeObject<SearchResults>(json);

                if (results != null)
                {
                    results.RawJson = json;
                    results.CrawledSuccessfully = true;

                    return results;
                }
                else
                {
                    return new SearchResults()
                    {
                        RawJson = json,
                        CrawledSuccessfully = false
                    };
                }
            }
        }
    }
}
