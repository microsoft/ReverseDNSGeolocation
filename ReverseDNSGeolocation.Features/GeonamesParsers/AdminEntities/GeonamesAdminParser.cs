namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System.Collections.Generic;
    using System.IO;

    public static class GeonamesAdminParser
    {
        public static IEnumerable<GeonamesAdminEntity> Parse(string path)
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
                    var entity = GeonamesAdminEntity.Parse(parse);

                    yield return entity;
                }
            }
        }

        public static Dictionary<string, GeonamesAdminEntity> ParseToDict(string path)
        {
            var concatenatedCodesToAdmins = new Dictionary<string, GeonamesAdminEntity>();

            foreach (var entity in Parse(path))
            {
                concatenatedCodesToAdmins[entity.ConcatenatedCodes] = entity;
            }

            return concatenatedCodesToAdmins;
        }
    }
}
