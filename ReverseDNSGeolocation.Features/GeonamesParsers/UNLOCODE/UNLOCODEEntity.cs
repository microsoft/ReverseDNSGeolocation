namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System;

    [Serializable]
    public class UNLOCODEEntity
    {
        public string UNLOCODECode { get; set; }

        public int GeonameId { get; set; }

        public static UNLOCODEEntity Parse(string[] parts)
        {
            var entity = new UNLOCODEEntity();

            if (parts.Length != 3)
            {
                throw new ArgumentException("Expecting at least 4 columns as input");
            }

            var country = parts[0];
            var location = parts[1];
            var geonameIdRaw = parts[2];

            entity.UNLOCODECode = $"{country.ToLowerInvariant()}{location.ToLowerInvariant()}";

            int geonameId;

            if (int.TryParse(geonameIdRaw, out geonameId))
            {
                entity.GeonameId = geonameId;
            }
            else
            {
                throw new ArgumentException("Could not parse Geonames ID");
            }

            return entity;
        }
    }
}
