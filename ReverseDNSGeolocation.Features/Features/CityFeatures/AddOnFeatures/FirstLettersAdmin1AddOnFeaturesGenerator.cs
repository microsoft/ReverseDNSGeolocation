namespace ReverseDNSGeolocation.Features.CityFeatures.AddOnFeatures
{
    using ReverseDNSGeolocation.GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class FirstLettersAdmin1AddOnFeaturesGenerator : AddOnCityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        public FirstLettersAdmin1AddOnFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.FirstLettersAdmin1NameMatch, false },
                    { CityFeatureType.FirstLettersAdmin1Letters, (byte?)null },
                    { CityFeatureType.FirstLettersAdmin1LettersRatio, (float?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.FirstLettersAdmin1RTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.FirstLettersAdmin1LTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.FirstLettersAdmin1NameMatch, false },
                    { CityFeatureType.FirstLettersAdmin1Letters, (byte?)0 },
                    { CityFeatureType.FirstLettersAdmin1LettersRatio, (float?)0 },
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.FirstLettersAdmin1RTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.FirstLettersAdmin1LTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.FirstLettersAdmin1NameMatch, typeof(bool) },
                { CityFeatureType.FirstLettersAdmin1Letters, typeof(byte?) },
                { CityFeatureType.FirstLettersAdmin1LettersRatio, typeof(float?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.FirstLettersAdmin1RTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.FirstLettersAdmin1LTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.FirstLettersAdmin1NameMatch, FeatureGranularity.Discrete },
                { CityFeatureType.FirstLettersAdmin1Letters, FeatureGranularity.Continuous },
                { CityFeatureType.FirstLettersAdmin1LettersRatio, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.FirstLettersAdmin1RTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.FirstLettersAdmin1LTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void AppendFeatures(HostnameSplitterResult parsedHostname, GeonamesCityEntity cityEntity, Features features)
        {
            if (parsedHostname?.SubdomainParts == null || cityEntity == null || cityEntity.Admin1Entity == null || string.IsNullOrWhiteSpace(cityEntity.Admin1Entity.Name) || features == null)
            {
                return;
            }

            var firstLettersAdmin1NameVariations = this.GenerateVariationsForName(cityEntity.Admin1Entity.Name);

            foreach (var subdomainPart in parsedHostname.SubdomainParts)
            {
                if (firstLettersAdmin1NameVariations.Contains(subdomainPart.Substring))
                {
                    features[CityFeatureType.FirstLettersAdmin1NameMatch] = true;

                    if (!features.ContainsKey(CityFeatureType.FirstLettersAdmin1Letters) || features[CityFeatureType.FirstLettersAdmin1Letters] == null || ((byte)features[CityFeatureType.FirstLettersAdmin1Letters]) < subdomainPart.Substring.Length)
                    {
                        features[CityFeatureType.FirstLettersAdmin1Letters] = Convert.ToByte(subdomainPart.Substring.Length);
                        features[CityFeatureType.FirstLettersAdmin1LettersRatio] = (float?)(subdomainPart.Substring.Length / ((1.0f) * cityEntity.Admin1Entity.Name.Length));
                    }

                    if (this.FeaturesConfig.UseSlotIndex)
                    {
                        features[CityFeatureType.FirstLettersAdmin1RTLSlotIndex] = subdomainPart.RTLSlotIndex;
                        features[CityFeatureType.FirstLettersAdmin1LTRSlotIndex] = subdomainPart.LTRSlotIndex;
                    }
                }
            }
        }

        private HashSet<string> GenerateVariationsForName(string name)
        {
            var variations = new HashSet<string>();

            if (string.IsNullOrEmpty(name))
            {
                return variations;
            }

            var words = name.ToLowerInvariant().Split(' ');
            var firstWord = words[0];

            if (firstWord.Length > 4)
            {
                variations.Add(firstWord.Substring(0, 4));
            }

            if (firstWord.Length > 5)
            {
                variations.Add(firstWord.Substring(0, 5));
            }

            if (firstWord.Length > 6)
            {
                variations.Add(firstWord.Substring(0, 6));
            }

            return variations;
        }
    }
}
