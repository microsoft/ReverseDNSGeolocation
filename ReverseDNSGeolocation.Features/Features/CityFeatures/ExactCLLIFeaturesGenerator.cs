namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class ExactCLLIFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> codesToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        public ExactCLLIFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactCLLICodeMatch, false },
                    { CityFeatureType.ExactCLLICodePopulation, (uint?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactCLLICodeRTLSlotIndex] = (uint?)null;
                    FeatureDefaults[CityFeatureType.ExactCLLICodeLTRSlotIndex] = (uint?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactCLLICodeMatch, false },
                    { CityFeatureType.ExactCLLICodePopulation, (uint?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactCLLICodeRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.ExactCLLICodeLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.ExactCLLICodeMatch, typeof(bool) },
                { CityFeatureType.ExactCLLICodePopulation, typeof(uint?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.ExactCLLICodeRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.ExactCLLICodeLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.ExactCLLICodeMatch, FeatureGranularity.Discrete },
                { CityFeatureType.ExactCLLICodePopulation, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.ExactCLLICodeRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.ExactCLLICodeLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            var codes = entity.CLLICodes;

            if (codes == null || codes.Count == 0)
            {
                return;
            }

            foreach (var code in codes)
            {
                var features = this.InitializeDefaultFeatureValues();

                features[CityFeatureType.ExactCLLICodeMatch] = true;

                if (entity.Population > 0)
                {
                    features[CityFeatureType.ExactCLLICodePopulation] = (uint?)entity.Population;
                }

                EntitiesToFeatures entitiesToFeatures;

                if (!codesToEntitiesToFeatures.TryGetValue(code, out entitiesToFeatures))
                {
                    entitiesToFeatures = new EntitiesToFeatures();
                    codesToEntitiesToFeatures[code] = entitiesToFeatures;
                }

                entitiesToFeatures[entity] = features;
            }
        }

        public override Dictionary<GeonamesCityEntity, Features> GenerateCandidatesAndFeatures(HostnameSplitterResult parsedHostname)
        {
            var candidatesAndFeatures = new Dictionary<GeonamesCityEntity, Features>();

            var combinedParts = new HashSet<SubdomainPart>();

            if (parsedHostname?.SubdomainParts != null)
            {
                combinedParts.UnionWith(parsedHostname.SubdomainParts);
            }

            if (parsedHostname?.FirstLastLetters != null)
            {
                combinedParts.UnionWith(parsedHostname.FirstLastLetters);
            }

            foreach (var subdomainPart in combinedParts)
            {
                EntitiesToFeatures entitiesToFeatures;

                if (codesToEntitiesToFeatures.TryGetValue(subdomainPart.Substring, out entitiesToFeatures))
                {
                    foreach (var entry in entitiesToFeatures)
                    {
                        var features = new Features();

                        foreach (var featureEntry in entry.Value)
                        {
                            features[featureEntry.Key] = featureEntry.Value;
                        }

                        if (this.FeaturesConfig.UseSlotIndex)
                        {
                            features[CityFeatureType.ExactCLLICodeRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.ExactCLLICodeLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
