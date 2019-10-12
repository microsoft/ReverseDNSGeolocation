namespace ReverseDNSGeolocation.Features.CityFeatures.AddOnFeatures
{
    using ReverseDNSGeolocation.GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class DomainNameAddOnFeaturesGenerator : AddOnCityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        public DomainNameAddOnFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (!this.FeaturesConfig.UseDomainAsFeature)
            {
                FeatureDefaults = new Features();
                FeatureDefaultsValueTypes = new FeatureValueTypes();
                FeatureGranularities = new FeatureGranularities();
                return;
            }

            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.Domain, (int?)null }
                };
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.Domain, int.MaxValue }
                };
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.Domain, typeof(int?) }
            };

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.Domain, FeatureGranularity.Discrete }
            };
        }

        public override void AppendFeatures(HostnameSplitterResult parsedHostname, GeonamesCityEntity cityEntity, Features features)
        {
            if (!this.FeaturesConfig.UseDomainAsFeature)
            {
                return;
            }

            if (parsedHostname?.DomainInfo?.RegistrableDomain == null ||  features == null)
            {
                return;
            }

            var domain = parsedHostname.DomainInfo.RegistrableDomain;

            features[CityFeatureType.Domain] = domain.GetHashCode();
        }
    }
}
