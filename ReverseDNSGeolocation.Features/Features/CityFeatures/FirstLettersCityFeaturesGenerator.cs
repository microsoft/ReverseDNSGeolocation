namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Text;

    [Serializable]
    public class FirstLettersCityFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> variationsToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        public FirstLettersCityFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.FirstLettersCityNameMatch, false },
                    { CityFeatureType.FirstLettersCityNamePopulation, (uint?)null },
                    { CityFeatureType.FirstLettersCityNameLetters, (byte?)null },
                    { CityFeatureType.FirstLettersCityNameLettersRatio, (float?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.FirstLettersCityNameRTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.FirstLettersCityNameLTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.FirstLettersCityNameMatch, false },
                    { CityFeatureType.FirstLettersCityNamePopulation, (uint?)0 },
                    { CityFeatureType.FirstLettersCityNameLetters, (byte?)0 },
                    { CityFeatureType.FirstLettersCityNameLettersRatio, (float?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.FirstLettersCityNameRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.FirstLettersCityNameLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.FirstLettersCityNameMatch, typeof(bool) },
                { CityFeatureType.FirstLettersCityNamePopulation, typeof(uint?) },
                { CityFeatureType.FirstLettersCityNameLetters, typeof(byte?) },
                { CityFeatureType.FirstLettersCityNameLettersRatio, typeof(float?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.FirstLettersCityNameRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.FirstLettersCityNameLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.FirstLettersCityNameMatch, FeatureGranularity.Discrete },
                { CityFeatureType.FirstLettersCityNamePopulation, FeatureGranularity.Continuous },
                { CityFeatureType.FirstLettersCityNameLetters, FeatureGranularity.Continuous },
                { CityFeatureType.FirstLettersCityNameLettersRatio, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.FirstLettersCityNameRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.FirstLettersCityNameLTRSlotIndex] = FeatureGranularity.Discrete;
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

                features[CityFeatureType.FirstLettersCityNameMatch] = true;

                if (entity.Population > 0)
                {
                    features[CityFeatureType.FirstLettersCityNamePopulation] = (uint?)entity.Population;
                }

                features[CityFeatureType.FirstLettersCityNameLetters] = (byte?)nameVariation.Length;

                if (nameVariation.Length > 0)
                {
                    features[CityFeatureType.FirstLettersCityNameLettersRatio] = (float?)(nameVariation.Length / ((1.0f) * entity.Name.Length));
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

            var words = name.ToLowerInvariant().Split(' ');
            var firstWord = words[0];

            if (firstWord.Length > 3)
            {
                variations.Add(firstWord.Substring(0, 3));
            }

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
                            features[CityFeatureType.FirstLettersCityNameRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.FirstLettersCityNameLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
