namespace ReverseDNSGeolocation.Features
{
    using ReverseDNSGeolocation.GeonamesParsers;
    using System;
    using System.Collections.Generic;

    [Serializable]
    public abstract class CityFeaturesGenerator
    {
        public FeaturesConfig FeaturesConfig { get; set; }

        public abstract Features FeatureDefaults { get; set; }

        public abstract FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public abstract FeatureGranularities FeatureGranularities { get; set; }

        public abstract void IngestCityEntity(GeonamesCityEntity entity);

        public CityFeaturesGenerator()
        {
        }

        public CityFeaturesGenerator(FeaturesConfig featuresConfig)
        {
            this.FeaturesConfig = featuresConfig;
        }

        public virtual Dictionary<GeonamesCityEntity, Features> GenerateCandidatesAndFeatures(string hostname)
        {
            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }

            var parsedHostname = HostnameSplitter.Split(hostname);
            return this.GenerateCandidatesAndFeatures(parsedHostname);
        }

        public virtual Features InitializeDefaultFeatureValues()
        {
            var features = new Features();

            if (this.FeaturesConfig.InitializeDefaultFeatures)
            {
                foreach (var featureDefault in FeatureDefaults)
                {
                    features[featureDefault.Key] = featureDefault.Value;
                }
            }

            return features;
        }

        public abstract Dictionary<GeonamesCityEntity, Features> GenerateCandidatesAndFeatures(HostnameSplitterResult parsedHostname);
    }
}
