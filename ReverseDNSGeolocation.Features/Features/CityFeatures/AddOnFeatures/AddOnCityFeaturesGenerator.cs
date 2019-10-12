namespace ReverseDNSGeolocation.Features.CityFeatures.AddOnFeatures
{
    using ReverseDNSGeolocation.GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public abstract class AddOnCityFeaturesGenerator
    {
        public FeaturesConfig FeaturesConfig { get; set; }

        public abstract Features FeatureDefaults { get; set; }

        public abstract FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public abstract FeatureGranularities FeatureGranularities { get; set; }

        public AddOnCityFeaturesGenerator(FeaturesConfig featuresConfig)
        {
            this.FeaturesConfig = featuresConfig;
        }

        public virtual void AppendFeatures(string hostname, GeonamesCityEntity cityEntity, Features features)
        {
            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }

            if (cityEntity == null)
            {
                throw new ArgumentNullException("cityEntity");
            }

            if (features == null)
            {
                throw new ArgumentNullException("features");
            }

            var parsedHostname = HostnameSplitter.Split(hostname);

            if (this.FeaturesConfig.InitializeDefaultFeatures)
            {
                this.InitializeDefaultFeatureValues(features);
            }

            this.AppendFeatures(parsedHostname, cityEntity, features);
        }

        public virtual void InitializeDefaultFeatureValues(Features currentFeatures)
        {
            if (this.FeaturesConfig.InitializeDefaultFeatures)
            {
                foreach (var featureDefault in FeatureDefaults)
                {
                    currentFeatures[featureDefault.Key] = featureDefault.Value;
                }
            }
        }

        public abstract void AppendFeatures(HostnameSplitterResult parsedHostname, GeonamesCityEntity cityEntity, Features features);
    }
}
