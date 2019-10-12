namespace ReverseDNSGeolocation.GeonamesWebApi
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class SearchResults
    {
        [JsonProperty("geonames")]
        public List<SearchResult> Results { get; set; }

        public bool CrawledSuccessfully { get; set; }

        public string RawJson { get; set; }
    }
}
