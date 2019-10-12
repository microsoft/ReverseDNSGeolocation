namespace ReverseDNSGeolocation.Classification
{
    using GeonamesParsers;
    using ReverseDNSGeolocation.Features;

    public class TrainingDataSample
    {
        public string Hostname { get; set; }

        public GeonamesCityEntity City { get; set; }

        public string FeaturesSignature { get; set; }

        public Features Features { get; set; }

        public bool IsPositiveExample { get; set; }
    }
}
