namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Text;

    [Serializable]
    public class CityAbbreviationsFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> variationsToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        private const int MinAbbrvLength = 3;

        public CityAbbreviationsFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.CityAbbreviationMatch, false },
                    { CityFeatureType.CityAbbreviationPopulation, (uint?)null },
                    { CityFeatureType.CityAbbreviationLetters, (byte?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.CityAbbreviationRTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.CityAbbreviationLTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.CityAbbreviationMatch, false },
                    { CityFeatureType.CityAbbreviationPopulation, (uint?)0 },
                    { CityFeatureType.CityAbbreviationLetters, (byte?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.CityAbbreviationRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.CityAbbreviationLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.CityAbbreviationMatch, typeof(bool) },
                { CityFeatureType.CityAbbreviationPopulation, typeof(uint?) },
                { CityFeatureType.CityAbbreviationLetters, typeof(byte?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.CityAbbreviationRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.CityAbbreviationLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.CityAbbreviationMatch, FeatureGranularity.Discrete },
                { CityFeatureType.CityAbbreviationPopulation, FeatureGranularity.Continuous },
                { CityFeatureType.CityAbbreviationLetters, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.CityAbbreviationRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.CityAbbreviationLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            var nameVariations = this.GenerateVariationsForName(entity.Name);

            foreach (var nameVariation in nameVariations)
            {
                var features = this.InitializeDefaultFeatureValues();

                features[CityFeatureType.CityAbbreviationMatch] = true;

                if (entity.Population > 0)
                {
                    features[CityFeatureType.CityAbbreviationPopulation] = (uint?)entity.Population;
                }

                features[CityFeatureType.CityAbbreviationLetters] = (byte?)nameVariation.Length;

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

            name = name.ToLowerInvariant();
            var words = new List<string>(name.Split(new char[] { ' ' }));
            var abbreviation = new StringBuilder();

            foreach (var word in words)
            {
                if (word.Length > 0)
                {
                    abbreviation.Append(word[0]);
                }
                else
                {
                    return variations;
                }
            }

            if (abbreviation.Length >= MinAbbrvLength)
            {
                variations.Add(abbreviation.ToString());
            }

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
                            features[CityFeatureType.CityAbbreviationRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.CityAbbreviationLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
