namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    [Serializable]
    public class GeonamesCountryEntity
    {
        // 0 - ISO Code
        public string ISOCode { get; set; }

        // 1 - ISO3 code
        public string ISO3Code { get; set; }

        // 2 - ISO Numeric Code (the codes can have leading zeroes, so we will use string)
        public string ISONumericCode { get; set; }

        // 3 - FIPS Code
        public string FIPSCode { get; set; }

        // 4 - Country name
        public string Name { get; set; }

        // 5 - Capital of country
        public string Capital { get; set; }

        // 6 - Area (in sq km)
        public uint? AreaSqKm { get; set; }

        // 7 - Population
        public uint? Population { get; set; }

        // 8 - Continent Code
        public string ContinentCode { get; set; }

        // 9 - TLD (Top-level domain) - for websites
        public string TLD { get; set; }

        // 10 - Currency Code
        public string CurrencyCode { get; set; }

        // 11 - Currency Name
        public string CurrencyName { get; set; }

        // 12 - Country calling code
        public short? CountryCallingCode { get; set; }

        // 13 - Post code format
        public string PostalCodeFormat { get; set; }

        // 14 - Postal code Regex
        public string PostalCodeRegex { get; set; }

        // 15 - Spoken languages (comma delimited)
        public List<string> Languages { get; set; }

        // 16 - Geonames ID
        public int Id { get; set; }

        // 17 - Neighboring Countries
        public List<string> NeighboringCountries { get; set; }

        // 18 - Equivalent FIPS Code (?)
        public string EquivalentFIPSCode { get; set; }

        public HashSet<string> NameVariationsLower
        {
            get
            {
                var variations = new HashSet<string>();

                if (!string.IsNullOrEmpty(this.ISOCode))
                {
                    variations.Add(this.ISOCode.ToLowerInvariant());
                }

                if (!string.IsNullOrEmpty(this.ISO3Code))
                {
                    variations.Add(this.ISO3Code.ToLowerInvariant());
                }

                if (!string.IsNullOrEmpty(this.Name))
                {
                    var sanitizedName = this.Name.Replace("-", string.Empty).Replace(" ", string.Empty);

                    if (!string.IsNullOrEmpty(sanitizedName))
                    {
                        variations.Add(sanitizedName.ToLowerInvariant());
                    }
                }

                return variations;
            }
        }

        public static GeonamesCountryEntity Parse(string[] parts)
        {
            var entity = new GeonamesCountryEntity();

            if (parts.Length != 19)
            {
                throw new ArgumentException("Expecting 19 columns as input");
            }

            int geonameId;

            if (int.TryParse(parts[16], out geonameId))
            {
                entity.Id = geonameId;
            }
            else
            {
                throw new ArgumentException("Could not parse Geonames ID");
            }

            entity.ISOCode = parts[0];
            entity.ISO3Code = parts[1];
            entity.ISONumericCode = parts[2];
            entity.FIPSCode = parts[3];
            entity.Name = parts[4];
            entity.Capital = parts[5];

            uint areaSqKm;

            if (uint.TryParse(parts[6], out areaSqKm))
            {
                entity.AreaSqKm = areaSqKm;
            }

            uint population;

            if (uint.TryParse(parts[7], out population))
            {
                entity.Population = population;
            }

            entity.ContinentCode = parts[8];
            entity.TLD = parts[9];
            entity.CurrencyCode = parts[10];
            entity.CurrencyName = parts[11];

            short countryCallingCode;

            if (short.TryParse(parts[7], out countryCallingCode))
            {
                entity.CountryCallingCode = countryCallingCode;
            }

            entity.PostalCodeFormat = parts[13];
            entity.PostalCodeRegex = parts[14];
            entity.Languages = parts[15].Trim().Split(',').ToList();
            entity.NeighboringCountries = parts[17].Trim().Split(',').ToList();
            entity.EquivalentFIPSCode = parts[18];

            return entity;
        }
    }
}
