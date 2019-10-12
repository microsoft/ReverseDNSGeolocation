namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System.Collections.Generic;
    using System.IO;

    public static class GeonamesAlternateNamesParser
    {
        public static IEnumerable<GeonamesAlternateNameEntity> Parse(string path)
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
                    var entity = GeonamesAlternateNameEntity.Parse(parse);

                    yield return entity;
                }
            }
        }

        public static Dictionary<int, Dictionary<string, List<GeonamesAlternateNameEntity>>> ParseToDict(string path)
        {
            var idsToAlternateNameEntities = new Dictionary<int, Dictionary<string, List<GeonamesAlternateNameEntity>>>();

            foreach (var entity in Parse(path))
            {
                Dictionary<string, List<GeonamesAlternateNameEntity>> isoLanguagesToAlternameNames;

                if (!idsToAlternateNameEntities.TryGetValue(entity.Id, out isoLanguagesToAlternameNames))
                {
                    isoLanguagesToAlternameNames = new Dictionary<string, List<GeonamesAlternateNameEntity>>();
                    idsToAlternateNameEntities[entity.Id] = isoLanguagesToAlternameNames;
                }

                List<GeonamesAlternateNameEntity> alternateNamesForIsoLanguage;

                if (!isoLanguagesToAlternameNames.TryGetValue(entity.ISOLanguage, out alternateNamesForIsoLanguage))
                {
                    alternateNamesForIsoLanguage = new List<GeonamesAlternateNameEntity>();
                    isoLanguagesToAlternameNames[entity.ISOLanguage] = alternateNamesForIsoLanguage;
                }

                alternateNamesForIsoLanguage.Add(entity);
            }

            return idsToAlternateNameEntities;
        }
    }
}
