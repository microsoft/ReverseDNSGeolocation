namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class GeonamesAdminEntity
    {
        private string concatenatedCodes;

        // 0 - Concatenated codes (Country.Admin1Code). Example: US.WA (Washington State in USA)
        public string ConcatenatedCodes
        {
            get
            {
                return this.concatenatedCodes;
            }

            set
            {
                this.concatenatedCodes = value;

                if (value != null)
                {
                    var parts = value.Split('.');

                    if (parts.Length == 2 && ! string.IsNullOrWhiteSpace(parts[1]))
                    {
                        this.Code = parts[1];
                    }
                }
            }
        }

        // 1 - Official name
        public string Name { get; set; }

        // 2 - ASCII Name (Potentially missing?)
        public string AsciiName { get; set; }

        // 3 - Geonames ID
        public int Id { get; set; }

        public string Code { get; set; }

        public HashSet<string> NameVariationsLower
        {
            get
            {
                var variations = new HashSet<string>();

                if (!string.IsNullOrEmpty(this.Code) && IsAdminCodeValid(this.Code))
                {
                    variations.Add(this.Code.ToLowerInvariant());
                }

                if (!string.IsNullOrEmpty(this.Name))
                {
                    var sanitizedName = this.Name.Replace("-", string.Empty).Replace(" ", string.Empty);

                    if (!string.IsNullOrEmpty(sanitizedName))
                    {
                        variations.Add(sanitizedName.ToLowerInvariant());
                    }
                }

                if (!string.IsNullOrEmpty(this.AsciiName))
                {
                    var sanitizedName = this.AsciiName.Replace("-", string.Empty).Replace(" ", string.Empty);

                    if (!string.IsNullOrEmpty(sanitizedName))
                    {
                        variations.Add(sanitizedName.ToLowerInvariant());
                    }
                }

                return variations;
            }
        }

        private bool IsAdminCodeValid(string adminCode)
        {
            if (string.IsNullOrWhiteSpace(adminCode))
            {
                return false;
            }

            foreach (var c in adminCode)
            {
                if (!char.IsLetter(c) && c != ' ')
                {
                    return false;
                }
            }

            return adminCode.Length >= 2;
        }

        public static GeonamesAdminEntity Parse(string[] parts)
        {
            var entity = new GeonamesAdminEntity();

            if (parts.Length != 4)
            {
                throw new ArgumentException("Expecting 4 columns as input");
            }

            int geonameId;

            if (int.TryParse(parts[3], out geonameId))
            {
                entity.Id = geonameId;
            }
            else
            {
                throw new ArgumentException("Could not parse Geonames ID");
            }

            entity.ConcatenatedCodes = parts[0];
            entity.Name = parts[1];
            entity.AsciiName = parts[2];

            return entity;
        }
    }
}
