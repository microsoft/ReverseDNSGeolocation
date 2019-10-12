namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System.Collections.Generic;
    using System.IO;

    public static class CLLICodesParser
    {
        public static IEnumerable<CLLIEntity> Parse(string path)
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
                    var entity = CLLIEntity.Parse(parse);

                    yield return entity;
                }
            }
        }

        public static Dictionary<int, HashSet<string>> ParseToDict(string path)
        {
            var geonamesIdsToCLLICodes = new Dictionary<int, HashSet<string>>();

            foreach (var entity in Parse(path))
            {
                HashSet<string> codesForGeonameId;

                if (!geonamesIdsToCLLICodes.TryGetValue(entity.GeonameId, out codesForGeonameId))
                {
                    codesForGeonameId = new HashSet<string>();
                    geonamesIdsToCLLICodes[entity.GeonameId] = codesForGeonameId;
                }

                codesForGeonameId.Add(entity.CLLICode);
            }

            return geonamesIdsToCLLICodes;
        }
    }
}
