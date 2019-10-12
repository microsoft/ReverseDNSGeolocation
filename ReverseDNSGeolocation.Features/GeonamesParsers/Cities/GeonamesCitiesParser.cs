namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System.Collections.Generic;
    using System.IO;

    public static class GeonamesCitiesParser
    {
        public static IEnumerable<GeonamesCityEntity> Parse(
            string path,
            Dictionary<int, Dictionary<string, List<GeonamesAlternateNameEntity>>> alternateNamesDict,
            Dictionary<string, GeonamesAdminEntity> admin1Dict,
            Dictionary<string, GeonamesAdminEntity> admin2Dict,
            Dictionary<string, GeonamesCountryEntity> countriesDict,
            Dictionary<int, HashSet<string>> geonamesIdsToCLLICodes,
            Dictionary<int, HashSet<string>> geonamesIdsToUNLOCODECodes)
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
                    var entity = GeonamesCityEntity.Parse(parse, alternateNamesDict, admin1Dict, admin2Dict, countriesDict, geonamesIdsToCLLICodes, geonamesIdsToUNLOCODECodes);

                    yield return entity;
                }
            }
        }
    }
}
