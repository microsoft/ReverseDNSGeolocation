namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class GeonamesCountriesParser
    {
        public static IEnumerable<GeonamesCountryEntity> ParseToEnumerable(string path)
        {
            using (var file = new StreamReader(path))
            {
                string line;

                while ((line = file.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (line.TrimStart().StartsWith("#"))
                    {
                        continue;
                    }

                    var parse = line.Split(new char[] { '\t' });
                    var entity = GeonamesCountryEntity.Parse(parse);

                    yield return entity;
                }
            }
        }

        public static List<GeonamesCountryEntity> ParseToList(string path)
        {
            return ParseToEnumerable(path).ToList<GeonamesCountryEntity>();
        }

        public static Dictionary<string, GeonamesCountryEntity> ListToISOCodeDict(List<GeonamesCountryEntity> entities)
        {
            var isoCodesToCountries = new Dictionary<string, GeonamesCountryEntity>();

            foreach (var entity in entities)
            {
                if (!string.IsNullOrEmpty(entity.ISOCode))
                {
                    isoCodesToCountries[entity.ISOCode] = entity;
                }
            }

            return isoCodesToCountries;
        }

        public static Dictionary<string, GeonamesCountryEntity> ListToTLDDict(List<GeonamesCountryEntity> entities)
        {
            var tldsToCountries = new Dictionary<string, GeonamesCountryEntity>();

            foreach (var entity in entities)
            {
                if (!string.IsNullOrEmpty(entity.TLD))
                {
                    tldsToCountries[entity.TLD] = entity;
                }
            }

            return tldsToCountries;
        }
    }
}
