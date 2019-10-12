namespace ReverseDNSGeolocation.Features.CityFeatures.AddOnFeatures
{
    using ReverseDNSGeolocation.GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class ExactAdmin1AddOnFeaturesGenerator : AddOnCityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        public ExactAdmin1AddOnFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactAdmin1NameMatch, false },
                    { CityFeatureType.ExactAdmin1Letters, (byte?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactAdmin1RTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.ExactAdmin1LTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactAdmin1NameMatch, false },
                    { CityFeatureType.ExactAdmin1Letters, (byte?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactAdmin1RTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.ExactAdmin1LTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.ExactAdmin1NameMatch, typeof(bool) },
                { CityFeatureType.ExactAdmin1Letters, typeof(byte?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.ExactAdmin1RTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.ExactAdmin1LTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.ExactAdmin1NameMatch, FeatureGranularity.Discrete },
                { CityFeatureType.ExactAdmin1Letters, FeatureGranularity.Continuous },
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.ExactAdmin1RTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.ExactAdmin1LTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void AppendFeatures(HostnameSplitterResult parsedHostname, GeonamesCityEntity cityEntity, Features features)
        {
            if (parsedHostname?.SubdomainParts == null || cityEntity == null || cityEntity.Admin1Entity == null || string.IsNullOrWhiteSpace(cityEntity.Admin1Entity.Name) || features == null)
            {
                return;
            }

            var admin1NameVariations = cityEntity.Admin1Entity.NameVariationsLower;

            foreach (var subdomainPart in parsedHostname.SubdomainParts)
            {
                if (admin1NameVariations.Contains(subdomainPart.Substring))
                {
                    features[CityFeatureType.ExactAdmin1NameMatch] = true;

                    if (!features.ContainsKey(CityFeatureType.ExactAdmin1Letters) || features[CityFeatureType.ExactAdmin1Letters] == null || ((byte)features[CityFeatureType.ExactAdmin1Letters]) < subdomainPart.Substring.Length)
                    {
                        features[CityFeatureType.ExactAdmin1Letters] = Convert.ToByte(subdomainPart.Substring.Length);
                    }

                    if (this.FeaturesConfig.UseSlotIndex)
                    {
                        features[CityFeatureType.ExactAdmin1RTLSlotIndex] = subdomainPart.RTLSlotIndex;
                        features[CityFeatureType.ExactAdmin1LTRSlotIndex] = subdomainPart.LTRSlotIndex;
                    }
                }
            }
        }
    }
}
