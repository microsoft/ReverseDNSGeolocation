namespace ReverseDNSGeolocation.GeonamesWebApi
{
    using Newtonsoft.Json;

    public class SearchResult
    {
        [JsonProperty("geonameId")]
        public int GeonameId { get; set; }

        [JsonProperty("toponymName")]
        public string Name { get; set; }

        [JsonProperty("adminCode1")]
        public string AdminCode1 { get; set; }

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("fcode")]
        public string FeatureCode { get; set; }

        [JsonProperty("fcl")]
        public string FeatureClass { get; set; }

        [JsonProperty("lat")]
        public string Latitude { get; set; }

        [JsonProperty("lng")]
        public string Longitude { get; set; }

        [JsonProperty("population")]
        public double Population { get; set; }
    }
}
