namespace ReverseDNSGeolocation.CLLI
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Web;
    using CsvHelper;
    using F23.StringSimilarity;
    using GeonamesWebApi;

    public static class CLLIGeo
    {
        private const string UrlFormat = "http://api.geonames.org/search?username={0}&country={1}&q={2}&type=json";
        private const string UrlFormatTwoCountries = "http://api.geonames.org/search?username={0}&country={1}&country={2}&q={3}&type=json";

        private static NormalizedLevenshtein nLevenshtein = new NormalizedLevenshtein();

        // Sometimes searching just for the abbreviation does not work, so we need the long names
        private static Dictionary<string, string> UsaStates = new Dictionary<string, string>()
        {
            {"AK", "Alaska"},
            {"AL", "Alabama"},
            {"AR", "Arkansas"},
            {"AS", "American Samoa"},
            {"AZ", "Arizona"},
            {"CA", "California"},
            {"CO", "Colorado"},
            {"CT", "Connecticut"},
            {"DC", "Washington, D.C."},
            {"DE", "Delaware"},
            {"FL", "Florida"},
            {"FM", "Micronesia"},
            {"GA", "Georgia"},
            {"GU", "Guam"},
            {"HI", "Hawaii"},
            {"IA", "Iowa"},
            {"ID", "Idaho"},
            {"IL", "Illinois"},
            {"IN", "Indiana"},
            {"KS", "Kansas"},
            {"KY", "Kentucky"},
            {"LA", "Louisiana"},
            {"MA", "Massachusetts"},
            {"MD", "Maryland"},
            {"ME", "Maine"},
            {"MH", "Marshall Islands"},
            {"MI", "Michigan"},
            {"MN", "Minnesota"},
            {"MO", "Missouri"},
            {"MP", "Northern Marianas"},
            {"MS", "Mississippi"},
            {"MT", "Montana"},
            {"NC", "North Carolina"},
            {"ND", "North Dakota"},
            {"NE", "Nebraska"},
            {"NH", "New Hampshire"},
            {"NJ", "New Jersey"},
            {"NM", "New Mexico"},
            {"NV", "Nevada"},
            {"NY", "New York"},
            {"OH", "Ohio"},
            {"OK", "Oklahoma"},
            {"OR", "Oregon"},
            {"PA", "Pennsylvania"},
            {"PR", "Puerto Rico"},
            {"PW", "Palau"},
            {"RI", "Rhode Island"},
            {"SC", "South Carolina"},
            {"SD", "South Dakota"},
            {"TN", "Tennessee"},
            {"TX", "Texas"},
            {"UT", "Utah"},
            {"VA", "Virginia"},
            {"VI", "Virgin Islands"},
            {"VT", "Vermont"},
            {"WA", "Washington"},
            {"WI", "Wisconsin"},
            {"WV", "West Virginia"},
            {"WY", "Wyoming"}
        };

        // Sometimes searching just for the abbreviation does not work, so we need the long names
        private static Dictionary<string, string> CanadaRegions = new Dictionary<string, string>()
        {
            {"AB", "Alberta"},
            {"BC", "British Columbia"},
            {"MB", "Manitoba"},
            {"NB", "New Brunswick"},
            {"NF", "Newfoundland and Labrador"},
            {"NS", "Nova Scotia"},
            {"NT", "Northwest Territories"},
            {"ON", "Ontario"},
            {"PE", "Prince Edward Island"},
            {"PQ", "Quebec"},
            {"SK", "Saskatchewan"},
            {"VU", "Nunavut"},
            {"YT", "Yukon"}
        };

        public static void FindLocations(string first6CityTsvPath, string username, string outFilePath)
        {
            var cache = new Dictionary<string, string>();

            using (var outFile = new StreamWriter(outFilePath))
            using (var inFile = new StreamReader(first6CityTsvPath))
            {
                var csv = new CsvReader(inFile);
                csv.Configuration.IgnoreQuotes = false;

                while (csv.Read())
                {
                    var clliCodeLower = csv.GetField<string>(0).Trim().ToLowerInvariant();
                    var rawCityNameLower = csv.GetField<string>(1).Trim().ToLowerInvariant();
                    var admin1CodeLower = csv.GetField<string>(2).Trim().ToLowerInvariant(); ;

                    if (
                        string.IsNullOrEmpty(clliCodeLower)
                        || string.IsNullOrEmpty(rawCityNameLower)
                        || rawCityNameLower == "not available"
                        || string.IsNullOrEmpty(admin1CodeLower)
                    )
                    {
                        continue;
                    }

                    var cacheKey = $"{rawCityNameLower}-{admin1CodeLower}";

                    if (cache.ContainsKey(cacheKey))
                    {
                        Console.WriteLine($"### ALREADY SEEN: {clliCodeLower}\t{rawCityNameLower}\t{admin1CodeLower}");

                        var line = $"{clliCodeLower}\t{cache[cacheKey]}";
                        Console.WriteLine($"### From CACHE: {line}");

                        outFile.WriteLine(line);
                        outFile.Flush();

                        continue;
                    }

                    string query;
                    string url;

                    if (UsaStates.ContainsKey(admin1CodeLower.ToUpperInvariant()))
                    {
                        var longStateName = UsaStates[admin1CodeLower.ToUpperInvariant()];
                        query = HttpUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}, {1}", rawCityNameLower, longStateName), Encoding.UTF8);
                        url = string.Format(CultureInfo.InvariantCulture, UrlFormat, username, "US", query);
                    }
                    else if (CanadaRegions.ContainsKey(admin1CodeLower.ToUpperInvariant()))
                    {
                        var longRegionName = CanadaRegions[admin1CodeLower.ToUpperInvariant()];
                        query = HttpUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}, {1}", rawCityNameLower, longRegionName), Encoding.UTF8);
                        url = string.Format(CultureInfo.InvariantCulture, UrlFormat, username, "CA", query);
                    }
                    else
                    {
                        query = HttpUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}, {1}", rawCityNameLower, admin1CodeLower), Encoding.UTF8);
                        url = string.Format(CultureInfo.InvariantCulture, UrlFormatTwoCountries, username, "US", "CA", query);
                    }

                    var searchCityTask = GeonamesApi.Search(url);

                    try
                    {
                        searchCityTask.Wait();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        continue;
                    }

                    var result = searchCityTask.Result;

                    if (result != null && result.Results != null && result.Results.Count > 0)
                    {
                        var bestCandidate = FindBestCandidate(result.Results, rawCityNameLower, admin1CodeLower);

                        if (bestCandidate?.GeonameId > 0)
                        {
                            var linePostfix = $"{rawCityNameLower }\t{admin1CodeLower}\t{bestCandidate.GeonameId}\t{bestCandidate.Name?.Trim()}\t{bestCandidate.AdminCode1?.Trim()}\t{bestCandidate.CountryCode?.Trim()}\t{bestCandidate.FeatureCode?.Trim()}\t{bestCandidate.FeatureClass?.Trim()}\t{bestCandidate.Latitude?.Trim()}\t{bestCandidate.Longitude?.Trim()}\t{bestCandidate.Population}";

                            cache[cacheKey] = linePostfix;

                            var line = $"{clliCodeLower}\t{linePostfix}";
                            Console.WriteLine(line);

                            outFile.WriteLine(line);
                            outFile.Flush();
                        }
                        else
                        {
                            Console.WriteLine($"### ERROR: Invalid result for: {clliCodeLower}\t{rawCityNameLower}\t{admin1CodeLower}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"### ERROR: Invalid results for: {clliCodeLower}\t{rawCityNameLower}\t{admin1CodeLower}");
                    }

                    Thread.Sleep(1800);
                }
            }
        }

        private static SearchResult FindBestCandidate(List<SearchResult> results, string rawCityNameLower, string admin1CodeLower)
        {
            List<SearchResult> filteredResults;

            // Admin1 codes only match for USA
            if (UsaStates.ContainsKey(admin1CodeLower.ToUpperInvariant()))
            {
                filteredResults = results.Where(r => r.AdminCode1.ToLowerInvariant() == admin1CodeLower).ToList<SearchResult>();
            }
            else
            {
                filteredResults = results;
            }

            var featureClassPlaces = filteredResults.Where(r => r.FeatureClass == "P").ToList<SearchResult>();
            var pResult = FindBestCandidateInFilteredResults(featureClassPlaces, rawCityNameLower);

            if (pResult != null)
            {
                return pResult;
            }

            var allResult = FindBestCandidateInFilteredResults(filteredResults, rawCityNameLower);

            if (allResult != null)
            {
                return allResult;
            }

            return null;
        }

        private static SearchResult FindBestCandidateInFilteredResults(List<SearchResult> filteredResults, string rawCityNameLower)
        {
            var nameMatches = filteredResults.Where(r => r.Name.ToLowerInvariant() == rawCityNameLower).ToList<SearchResult>();

            // Same name as rawCityNameLower
            if (nameMatches.Count > 0)
            {
                return nameMatches[0];
            }

            // StartsWith and Contains rawCityNameLower match
            var substringMatchPlaces = filteredResults.Where(r => 
                    r.Name.ToLowerInvariant().StartsWith(rawCityNameLower)
                    || rawCityNameLower.StartsWith(r.Name.ToLowerInvariant())

                    || r.Name.ToLowerInvariant().Contains(rawCityNameLower)
                    || rawCityNameLower.Contains(r.Name.ToLowerInvariant())
                    ).ToList<SearchResult>();

            if (substringMatchPlaces.Count > 0)
            {
                return substringMatchPlaces[0];
            }

            // Normalized Levenshtein distance similarity match (similarity increases as score gets closer to 0)
            var similarNamePlaces = filteredResults.Where(r => nLevenshtein.Distance(r.Name.ToLowerInvariant(), rawCityNameLower) <= 0.2).ToList<SearchResult>();

            if (similarNamePlaces.Count > 0)
            {
                return similarNamePlaces[0];
            }

            return null;
        }
    }
}
