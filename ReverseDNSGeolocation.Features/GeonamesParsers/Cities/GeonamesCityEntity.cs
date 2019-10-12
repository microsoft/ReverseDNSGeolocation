namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    [Serializable]
    public class GeonamesCityEntity
    {
        // 0 geonameid: integer id of record in geonames database
        public int Id { get; set; }

        // 1 name: name of geographical point (utf8) varchar(200)
        public string Name { get; set; }

        // 2 asciiname: name of geographical point in plain ascii characters, varchar(200)
        public string AsciiName { get; set; }

        public List<GeonamesAlternateNameEntity> AlternateNames { get; set; }

        public List<GeonamesAlternateNameEntity> AirportCodes { get; set; }

        public HashSet<string> CLLICodes { get; set; }

        public HashSet<string> UNLOCODECodes { get; set; }

        // 4 latitude: latitude in decimal degrees(wgs84)
        public double Latitude { get; set; }

        // 5 longitude: longitude in decimal degrees (wgs84)
        public double Longitude { get; set; }

        // 6 feature class: see http://www.geonames.org/export/codes.html, char(1)
        public char FeatureClass { get; set; }

        // 7 feature code: see http://www.geonames.org/export/codes.html, varchar(10)
        public string FeatureCode { get; set; }

        // 8 country code: ISO-3166 2-letter country code, 2 characters
        public string CountryCode { get; set; }

        public GeonamesCountryEntity CountryEntity { get; set; }

        // 9 cc2: alternate country codes, comma separated, ISO-3166 2-letter country code, 200 characters
        public List<string> AlternateCountryCodes { get; set; }

        // 10 admin1 code: fipscode (subject to change to iso code), see exceptions below, see file admin1Codes.txt for display names of this code; varchar(20)
        public string Admin1Code { get; set; }

        public GeonamesAdminEntity Admin1Entity { get; set; }

        // 11 admin2 code: code for the second administrative division, a county in the US, see file admin2Codes.txt; varchar(80)
        public string Admin2Code { get; set; }

        public GeonamesAdminEntity Admin2Entity { get; set; }

        // 12 admin3 code: code for third level administrative division, varchar(20)
        public string Admin3Code { get; set; }

        // 13 admin4 code: code for fourth level administrative division, varchar(20)
        public string Admin4Code { get; set; }

        // 14 population: bigint(8 byte int)
        // We did not use BigInteger here on purpose, as there cannot be many (any?) entries bigger than 4 billion
        public uint Population { get; set; }

        // 15 elevation: in meters, integer
        public int? Elevation { get; set; }

        // 16 dem: digital elevation model, srtm3 or gtopo30, average elevation of 3''x3'' (ca 90mx90m) or 30''x30'' (ca 900mx900m) area in meters, integer.srtm processed by cgiar/ciat.
        public string DigitalElevationModel { get; set; }

        // 17 timezone: the iana timezone id(see file timeZone.txt) varchar(40)
        public string Timezone { get; set; }

        // 18 modification date: date of last modification in yyyy-MM-dd format
        public DateTime LastModified { get; set; }

        public static GeonamesCityEntity Parse(
            string[] parts,
            Dictionary<int, Dictionary<string, List<GeonamesAlternateNameEntity>>> alternateNamesDict,
            Dictionary<string, GeonamesAdminEntity> admin1Dict, 
            Dictionary<string, GeonamesAdminEntity> admin2Dict,
            Dictionary<string, GeonamesCountryEntity> countriesDict,
            Dictionary<int, HashSet<string>> geonamesIdsToCLLICodes,
            Dictionary<int, HashSet<string>> geonamesIdsToUNLOCODECodes)
        {
            var entity = new GeonamesCityEntity();

            if (parts.Length != 19)
            {
                throw new ArgumentException("Expecting 19 columns as input");
            }

            int geonameId;

            if (int.TryParse(parts[0], out geonameId))
            {
                entity.Id = geonameId;
            }
            else
            {
                throw new ArgumentException("Could not parse Geonames ID");
            }

            HashSet<string> clliCodes;

            if (geonamesIdsToCLLICodes.TryGetValue(geonameId, out clliCodes))
            {
                entity.CLLICodes = clliCodes;
            }

            HashSet<string> unlocodeCodes;

            if (geonamesIdsToUNLOCODECodes != null && geonamesIdsToUNLOCODECodes.TryGetValue(geonameId, out unlocodeCodes))
            {
                entity.UNLOCODECodes = unlocodeCodes;
            }

            entity.Name = parts[1];
            entity.AsciiName = parts[2];

            var alternateNamesBlacklist = new HashSet<string>()
            {
                "link", // Wikipedia link
                "post", // Post code
                "phon", // Phone?
                string.Empty
            };

            entity.AlternateNames = FilterAlternateNames(alternateNamesDict, geonameId, isoLanguageBlacklist: alternateNamesBlacklist);

            var oldAlternateNames = ParseMultiValue(rawData: parts[3], splitChar: ',');

            var airportCodesWhitelist = new HashSet<string>()
            {
                "icao",
                "iata",
                "faac",
                "tcid"
            };

            entity.AirportCodes = FilterAlternateNames(alternateNamesDict, geonameId, isoLanguageWhitelist: airportCodesWhitelist);

            double latitude;

            if (double.TryParse(parts[4], out latitude))
            {
                entity.Latitude = latitude;
            }
            else
            {
                throw new ArgumentException("Could not parse Latitude");
            }

            double longitude;

            if (double.TryParse(parts[5], out longitude))
            {
                entity.Longitude = longitude;
            }
            else
            {
                throw new ArgumentException("Could not parse Longitude");
            }

            entity.FeatureClass = parts[6][0];
            entity.FeatureCode = parts[7];
            entity.CountryCode = parts[8];
            entity.AlternateCountryCodes = ParseMultiValue(rawData: parts[9], splitChar: ',');
            entity.Admin1Code = parts[10];
            entity.Admin2Code = parts[11];
            entity.Admin3Code = parts[12];
            entity.Admin4Code = parts[13];

            uint population;

            if (uint.TryParse(parts[14], out population))
            {
                entity.Population = population;
            }
            else
            {
                throw new ArgumentException("Could not parse Population");
            }

            int elevation;

            if (!string.IsNullOrEmpty(parts[15]) && int.TryParse(parts[15], out elevation))
            {
                entity.Elevation = elevation;
            }

            entity.DigitalElevationModel = parts[16];
            entity.Timezone = parts[17];
            entity.LastModified = DateTime.ParseExact(parts[18], "yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (admin1Dict != null && !string.IsNullOrWhiteSpace(entity.CountryCode) && !string.IsNullOrWhiteSpace(entity.Admin1Code))
            {
                var concatenatedCodes = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", entity.CountryCode, entity.Admin1Code);

                GeonamesAdminEntity admin1Entity;

                if (admin1Dict.TryGetValue(concatenatedCodes, out admin1Entity))
                {
                    entity.Admin1Entity = admin1Entity;
                }
            }

            if (admin2Dict != null && !string.IsNullOrWhiteSpace(entity.CountryCode) && !string.IsNullOrWhiteSpace(entity.Admin1Code) && !string.IsNullOrWhiteSpace(entity.Admin2Code))
            {
                var concatenatedCodes = string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", entity.CountryCode, entity.Admin1Code, entity.Admin2Code);

                GeonamesAdminEntity admin2Entity;

                if (admin2Dict.TryGetValue(concatenatedCodes, out admin2Entity))
                {
                    entity.Admin2Entity = admin2Entity;
                }
            }

            if (countriesDict != null && !string.IsNullOrWhiteSpace(entity.CountryCode))
            {
                GeonamesCountryEntity countryEntity;

                if (countriesDict.TryGetValue(entity.CountryCode, out countryEntity))
                {
                    entity.CountryEntity = countryEntity;
                }
                else
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Could not find a country entity with code: {0}", entity.CountryCode));
                }
            }

            return entity;
        }

        private static List<GeonamesAlternateNameEntity> FilterAlternateNames(Dictionary<int, Dictionary<string, List<GeonamesAlternateNameEntity>>> alternateNamesDict, int geonameId, HashSet<string> isoLanguageWhitelist = null, HashSet<string> isoLanguageBlacklist = null)
        {
            Dictionary<string, List<GeonamesAlternateNameEntity>> isoLanguagesToAlternameNames;

            if (alternateNamesDict.TryGetValue(geonameId, out isoLanguagesToAlternameNames))
            {
                var alternateNames = new List<GeonamesAlternateNameEntity>();

                foreach (var isoLanguageToAlternameNames in isoLanguagesToAlternameNames)
                {
                    var isoLanguage = isoLanguageToAlternameNames.Key;
                    var alternateNameEntities = isoLanguageToAlternameNames.Value;

                    if (isoLanguageBlacklist == null || !isoLanguageBlacklist.Contains(isoLanguage))
                    {
                        foreach (var entity in alternateNameEntities)
                        {
                            if (!string.IsNullOrWhiteSpace(entity.AlternateName) && (isoLanguageWhitelist == null || isoLanguageWhitelist.Contains(isoLanguage)))
                            {
                                alternateNames.Add(entity);
                            }
                        }
                    }
                }

                if (alternateNames.Count > 0)
                {
                    return alternateNames;
                }
            }

            return null;
        }

        private static List<string> ParseMultiValue(string rawData, char splitChar)
        {
            if (rawData == null)
            {
                throw new ArgumentException("rawData should not be null");
            }

            var alternateNamesRaw = new List<string>(rawData.Split(new char[] { splitChar }));
            alternateNamesRaw = alternateNamesRaw.Where(s => !string.IsNullOrWhiteSpace(s)).ToList<string>();

            if (alternateNamesRaw.Count > 0)
            {
                return alternateNamesRaw;
            }

            return null;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} - {1} - {2}", this.AsciiName, this.Admin1Code, this.CountryCode);
        }
    }
}
