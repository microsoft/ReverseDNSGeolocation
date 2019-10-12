namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class ExactCityFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> variationsToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        public ExactCityFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactCityNameMatch, false },
                    { CityFeatureType.ExactCityNamePopulation, (uint?)null },
                    { CityFeatureType.ExactCityNameLetters, (byte?)null },
                    { CityFeatureType.ExactCityNameAlternateNamesCount, (uint?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactCityNameRTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.ExactCityNameLTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.ExactCityNameMatch, false },
                    { CityFeatureType.ExactCityNamePopulation, (uint?)0 },
                    { CityFeatureType.ExactCityNameLetters, (byte?)0 },
                    { CityFeatureType.ExactCityNameAlternateNamesCount, (uint?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.ExactCityNameRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.ExactCityNameLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.ExactCityNameMatch, typeof(bool) },
                { CityFeatureType.ExactCityNamePopulation, typeof(uint?) },
                { CityFeatureType.ExactCityNameLetters, typeof(byte?) },
                { CityFeatureType.ExactCityNameAlternateNamesCount, typeof(uint?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.ExactCityNameRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.ExactCityNameLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.ExactCityNameMatch, FeatureGranularity.Discrete },
                { CityFeatureType.ExactCityNamePopulation, FeatureGranularity.Continuous },
                { CityFeatureType.ExactCityNameLetters, FeatureGranularity.Continuous },
                { CityFeatureType.ExactCityNameAlternateNamesCount, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.ExactCityNameRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.ExactCityNameLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            var nameVariations = this.GenerateVariationsForName(entity.Name);
            var asciiNameVariations = this.GenerateVariationsForName(entity.AsciiName);
            nameVariations.UnionWith(asciiNameVariations);

            foreach (var nameVariation in nameVariations)
            {
                var features = this.InitializeDefaultFeatureValues();

                features[CityFeatureType.ExactCityNameMatch] = true;

                if (entity.Population > 0)
                {
                    features[CityFeatureType.ExactCityNamePopulation] = (uint?)entity.Population;
                }

                features[CityFeatureType.ExactCityNameLetters] = (byte?)nameVariation.Length;

                if (this.FeaturesConfig.UseAlternateNamesCount)
                {
                    features[CityFeatureType.ExactCityNameAlternateNamesCount] = (uint?)(entity.AlternateNames?.Count ?? 0);
                }

                EntitiesToFeatures entitiesToFeatures;

                if (!variationsToEntitiesToFeatures.TryGetValue(nameVariation, out entitiesToFeatures))
                {
                    entitiesToFeatures = new EntitiesToFeatures();
                    variationsToEntitiesToFeatures[nameVariation] = entitiesToFeatures;
                }

                entitiesToFeatures[entity] = features;
            }
        }

        private HashSet<string> GenerateVariationsForName(string name)
        {
            var variations = new HashSet<string>();

            if (string.IsNullOrEmpty(name))
            {
                return variations;
            }

            variations.Add(name.Replace(" ", string.Empty).ToLowerInvariant());
            variations.Add(name.Replace(" ", "-").ToLowerInvariant());

            return variations;
        }

        public override Dictionary<GeonamesCityEntity, Features> GenerateCandidatesAndFeatures(HostnameSplitterResult parsedHostname)
        {
            var candidatesAndFeatures = new Dictionary<GeonamesCityEntity, Features>();

            if (parsedHostname?.SubdomainParts == null)
            {
                return candidatesAndFeatures;
            }

            foreach (var subdomainPart in parsedHostname.SubdomainParts)
            {
                EntitiesToFeatures entitiesToFeatures;

                if (variationsToEntitiesToFeatures.TryGetValue(subdomainPart.Substring, out entitiesToFeatures))
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
                            features[CityFeatureType.ExactCityNameRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.ExactCityNameLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
