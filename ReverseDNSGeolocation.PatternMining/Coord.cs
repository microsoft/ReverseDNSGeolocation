namespace ReverseDNSGeolocation.PatternMining
{
    using System;

    [Serializable]
    public class Coord
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.Latitude.GetHashCode();
                hash = hash * 23 + this.Longitude.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object coordRaw)
        {
            var coord = coordRaw as Coord;

            if (coord == null)
            {
                return false;
            }

            return coord.Latitude == this.Latitude && coord.Longitude == this.Longitude;
        }

        public override string ToString()
        {
            return $"{this.Latitude}, {this.Longitude}";
        }
    }
}
