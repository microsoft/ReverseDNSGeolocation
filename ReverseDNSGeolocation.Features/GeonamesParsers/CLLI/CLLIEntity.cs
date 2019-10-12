namespace ReverseDNSGeolocation.GeonamesParsers
{
    using System;

    [Serializable]
    public class CLLIEntity
    {
        // 0 - clliCodeLower
        public string CLLICode { get; set; }

        // 3 - bestCandidate.GeonameId
        public int GeonameId { get; set; }

        public static CLLIEntity Parse(string[] parts)
        {
            var entity = new CLLIEntity();

            if (parts.Length < 4)
            {
                throw new ArgumentException("Expecting at least 4 columns as input");
            }

            entity.CLLICode = parts[0];

            int geonameId;

            if (int.TryParse(parts[3], out geonameId))
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
