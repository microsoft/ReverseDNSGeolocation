namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class ExactUNLOCODEFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> codesToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        public ExactUNLOCODEFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactUNLOCODECodeMatch, false },
                    { CityFeatureType.ExactUNLOCODECodePopulation, (uint?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactUNLOCODECodeRTLSlotIndex] = (uint?)null;
                    FeatureDefaults[CityFeatureType.ExactUNLOCODECodeLTRSlotIndex] = (uint?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactUNLOCODECodeMatch, false },
                    { CityFeatureType.ExactUNLOCODECodePopulation, (uint?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactUNLOCODECodeRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.ExactUNLOCODECodeLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.ExactUNLOCODECodeMatch, typeof(bool) },
                { CityFeatureType.ExactUNLOCODECodePopulation, typeof(uint?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.ExactUNLOCODECodeRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.ExactUNLOCODECodeLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.ExactUNLOCODECodeMatch, FeatureGranularity.Discrete },
                { CityFeatureType.ExactUNLOCODECodePopulation, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.ExactUNLOCODECodeRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.ExactUNLOCODECodeLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            var codes = entity.UNLOCODECodes;

            if (codes == null || codes.Count == 0)
            {
                return;
            }

            foreach (var code in codes)
            {
                var features = this.InitializeDefaultFeatureValues();

                features[CityFeatureType.ExactUNLOCODECodeMatch] = true;

                if (entity.Population > 0)
                {
                    features[CityFeatureType.ExactUNLOCODECodePopulation] = (uint?)entity.Population;
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
                            features[CityFeatureType.ExactUNLOCODECodeRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.ExactUNLOCODECodeLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
