namespace ReverseDNSGeolocation.Classification
{
    using GeonamesParsers;
    using NGeoHash.Portable;
    using System;
    using System.Collections.Generic;

    public class ClosestCityFinder
    {
        Dictionary<string, List<GeonamesCityEntity>> GeohashesToCities;

        public ClosestCityFinder(
            string citiesPath,
            string alternateNamesPath,
            string admin1Path,
            string admin2Path,
            string countriesPath,
            string clliPath,
            string unlocodePath)
        {
            var alternateNamesDict = GeonamesAlternateNamesParser.ParseToDict(alternateNamesPath);
            var admin1Dict = GeonamesAdminParser.ParseToDict(admin1Path);
            var admin2Dict = GeonamesAdminParser.ParseToDict(admin2Path);
            var countryEntities = GeonamesCountriesParser.ParseToList(countriesPath);
            var countryCodesDict = GeonamesCountriesParser.ListToISOCodeDict(countryEntities);
            var geonamesIdsToCLLICodes = CLLICodesParser.ParseToDict(clliPath);
            var geonamesIdsToUNLOCODECodes = UNLOCODECodesParser.ParseToDict(unlocodePath);

            this.GeohashesToCities = new Dictionary<string, List<GeonamesCityEntity>>();

            foreach (var entity in GeonamesCitiesParser.Parse(citiesPath, alternateNamesDict, admin1Dict, admin2Dict, countryCodesDict, geonamesIdsToCLLICodes, geonamesIdsToUNLOCODECodes))
            {
                var geohash = GeoHash.Encode(entity.Latitude, entity.Longitude, numberOfChars: 3); // 3 = ±78km

                List<GeonamesCityEntity> citiesInGeohash;

                if (!this.GeohashesToCities.TryGetValue(geohash, out citiesInGeohash))
                {
                    citiesInGeohash = new List<GeonamesCityEntity>();
                    this.GeohashesToCities[geohash] = citiesInGeohash;
                }

                citiesInGeohash.Add(entity);
            }
        }

        public GeonamesCityEntity FindClosestCityForCoordinates(double latitude, double longitude)
        {
            var coordinatesGeohash = GeoHash.Encode(latitude, longitude, numberOfChars: 3); // 3 = ±78km
            var neighborGeohashes = GeoHash.Neighbors(coordinatesGeohash);

            var targetGeohashes = new HashSet<string>(neighborGeohashes);
            targetGeohashes.Add(coordinatesGeohash);

            var targetCities = new List<GeonamesCityEntity>();

            foreach (var targetGeohash in targetGeohashes)
            {
                List<GeonamesCityEntity> citiesInTargetGeohash;

                if (this.GeohashesToCities.TryGetValue(targetGeohash, out citiesInTargetGeohash))
                {
                    targetCities.AddRange(citiesInTargetGeohash);
                }
            }

            GeonamesCityEntity closestCity = null;
            double closestCityDistance = double.MaxValue;

            foreach (var targetCity in targetCities)
            {
                var distance = DistanceHelper.Distance(targetCity.Latitude, targetCity.Longitude, latitude, longitude, DistanceUnit.Kilometer);

                if (distance <= 50 && distance < closestCityDistance)
                {
                    closestCity = targetCity;
                    closestCityDistance = distance;
                }
            }

            return closestCity;
        }
    }
}
