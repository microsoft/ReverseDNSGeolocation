namespace ReverseDNSGeolocation.Features.CityFeatures.AddOnFeatures
{
    using ReverseDNSGeolocation.GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class ExactCountryAddOnFeaturesGenerator : AddOnCityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        public ExactCountryAddOnFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactCountryNameMatch, false },
                    { CityFeatureType.ExactCountryLetters, (byte?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactCountryRTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.ExactCountryLTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactCountryNameMatch, false },
                    { CityFeatureType.ExactCountryLetters, (byte?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactCountryRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.ExactCountryLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.ExactCountryNameMatch, typeof(bool) },
                { CityFeatureType.ExactCountryLetters, typeof(byte?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.ExactCountryRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.ExactCountryLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.ExactCountryNameMatch, FeatureGranularity.Discrete },
                { CityFeatureType.ExactCountryLetters, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.ExactCountryRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.ExactCountryLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void AppendFeatures(HostnameSplitterResult parsedHostname, GeonamesCityEntity cityEntity, Features features)
        {
            if (parsedHostname?.SubdomainParts == null || cityEntity == null || cityEntity.CountryEntity == null || string.IsNullOrWhiteSpace(cityEntity.CountryEntity.Name) || features == null)
            {
                return;
            }

            var countryNameVariations = cityEntity.CountryEntity.NameVariationsLower;

            foreach (var subdomainPart in parsedHostname.SubdomainParts)
            {
                if (countryNameVariations.Contains(subdomainPart.Substring))
                {
                    features[CityFeatureType.ExactCountryNameMatch] = true;

                    if (!features.ContainsKey(CityFeatureType.ExactCountryLetters) || features[CityFeatureType.ExactCountryLetters] == null || ((byte)features[CityFeatureType.ExactCountryLetters]) < subdomainPart.Substring.Length)
                    {
                        features[CityFeatureType.ExactCountryLetters] = Convert.ToByte(subdomainPart.Substring.Length);
                    }

                    if (this.FeaturesConfig.UseSlotIndex)
                    {
                        features[CityFeatureType.ExactCountryRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                        features[CityFeatureType.ExactCountryLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                    }
                }
            }
        }
    }
}
