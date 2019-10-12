namespace ReverseDNSGeolocation.PatternMining
{
    using GeonamesParsers;
    using System;

    [Serializable]
    public class PatternMiningCoordinates
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Confidence { get; set; }

        public GeonamesCityEntity ClosestCity { get; set; }

        public override string ToString()
        {
            return $"{this.Latitude}, {this.Longitude} - {this.Confidence}";
        }
    }
}
