namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class GeonamesAlternateNameEntity
    {
        // 0 - alternateNameId: the id of this alternate name, int
        public int AlternateNameId { get; set; }

        // 1 - geonameid: geonameId referring to id in table 'geoname', int
        public int Id { get; set; }

        // 2 - isolanguage: iso 639 language code 2- or 3-characters; 4-characters 'post' for postal codes and 'iata','icao' and faac for airport codes, fr_1793 for French Revolution names,  abbr for abbreviation, link for a website, varchar(7)
        public string ISOLanguage { get; set; }

        // 3 - alternate name or name variant, varchar(400)
        public string AlternateName { get; set; }

        // 4 - '1', if this alternate name is an official/preferred name
        public bool IsPreferredName { get; set; }

        // 5 - '1', if this is a short name like 'California' for 'State of California'
        public bool IsShortName { get; set; }

        // 6 - '1', if this alternate name is a colloquial or slang term
        public bool IsColloquial { get; set; }

        // 7 - '1', if this alternate name is historic and was used in the past
        public bool IsHistoric { get; set; }

        public static GeonamesAlternateNameEntity Parse(string[] parts)
        {
            if (parts.Length < 4)
            {
                throw new ArgumentException("Expecting at least 4 columns as input");
            }

            var entity = new GeonamesAlternateNameEntity()
            {
                AlternateNameId = int.Parse(parts[0]),
                Id = int.Parse(parts[1]),
                ISOLanguage = parts[2],
                AlternateName = parts[3],
                IsPreferredName = false,
                IsShortName = false,
                IsColloquial = false,
                IsHistoric = false
            };

            if (parts.Length >= 5 && parts[4].Length > 0)
            {
                entity.IsPreferredName = parts[4] == "1" ? true : false;
            }

            if (parts.Length >= 6 && parts[5].Length > 0)
            {
                entity.IsShortName = parts[5] == "1" ? true : false;
            }

            if (parts.Length >= 7 && parts[6].Length > 0)
            {
                entity.IsColloquial = parts[6] == "1" ? true : false;
            }

            if (parts.Length >= 8 && parts[7].Length > 0)
            {
                entity.IsHistoric = parts[7] == "1" ? true : false;
            }

            return entity;
        }
    }
}
