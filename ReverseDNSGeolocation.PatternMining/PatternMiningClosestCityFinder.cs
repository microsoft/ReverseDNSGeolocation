namespace ReverseDNSGeolocation.PatternMining
{
    using Classification;
    using GeonamesParsers;
    using NGeoHash.Portable;
    using System;
    using System.Collections.Generic;

    public class PatternMiningClosestCityFinder : ClosestCityFinder
    {
        public PatternMiningClosestCityFinder(
            string citiesPath,
            string alternateNamesPath,
            string admin1Path,
            string admin2Path,
            string countriesPath,
            string clliPath,
            string unlocodePath)
            : base(citiesPath,
                    alternateNamesPath,
                    admin1Path,
                    admin2Path,
                    countriesPath,
                    clliPath,
                    unlocodePath)
        {
        }

        public void FindClosestCitiesForRules(Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> rules)
        {
            var count = 0;

            foreach (var reducedRulesEntry in rules)
            {
                count++;

                var domain = reducedRulesEntry.Key;
                var rulesToCoordinates = reducedRulesEntry.Value;

                Console.WriteLine($"{count}/{rules.Count} - Working on domain {domain} in FindClosestCitiesForRules");

                foreach (var rulesToCoordinatesEntry in rulesToCoordinates)
                {
                    var rule = rulesToCoordinatesEntry.Key;
                    var coordinates = rulesToCoordinatesEntry.Value;

                    var closestCity = this.FindClosestCityForCoordinates(coordinates.Latitude, coordinates.Longitude);

                    if (closestCity != null)
                    {
                        coordinates.ClosestCity = closestCity;
                    }
                }
            }

            Console.WriteLine("Done with FindClosestCitiesForRules");
        }
    }
}
