namespace ReverseDNSGeolocation.PatternMining
{
    using System;

    [Serializable]
    public class CoordWithOcc
    {
        public Coord Coord { get; set; }

        public int Occurrences { get; set; }

        public CoordWithOcc(Coord coord, int occ)
        {
            this.Coord = coord;
            this.Occurrences = occ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.Coord.GetHashCode();
                hash = hash * 23 + this.Occurrences.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object coordWithOccRaw)
        {
            var coord = coordWithOccRaw as CoordWithOcc;

            if (coord == null || coord.Coord == null)
            {
                return false;
            }

            return coord.Coord.Latitude == this.Coord.Latitude && coord.Coord.Longitude == this.Coord.Longitude;
        }

        public override string ToString()
        {
            return $"{this.Coord.Latitude}, {this.Coord.Longitude} - {this.Occurrences}";
        }
    }
}
