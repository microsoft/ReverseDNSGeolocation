namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Text;

    [Serializable]
    public class AlternateCityAbbreviationsFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> variationsToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        private const int MinAbbrvLength = 3;

        public AlternateCityAbbreviationsFeaturesGenerator() : base()
        {
        }

        public AlternateCityAbbreviationsFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.AlternateCityAbbreviationMatch, false },
                    { CityFeatureType.AlternateCityAbbreviationPopulation, (uint?)null },
                    { CityFeatureType.AlternateCityAbbreviationLetters, (byte?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.AlternateCityAbbreviationRTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.AlternateCityAbbreviationLTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.AlternateCityAbbreviationMatch, false },
                    { CityFeatureType.AlternateCityAbbreviationPopulation, (uint?)0 },
                    { CityFeatureType.AlternateCityAbbreviationLetters, (byte?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.AlternateCityAbbreviationRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.AlternateCityAbbreviationLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.AlternateCityAbbreviationMatch, typeof(bool) },
                { CityFeatureType.AlternateCityAbbreviationPopulation, typeof(uint?) },
                { CityFeatureType.AlternateCityAbbreviationLetters, typeof(byte?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.AlternateCityAbbreviationRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.AlternateCityAbbreviationLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.AlternateCityAbbreviationMatch, FeatureGranularity.Discrete},
                { CityFeatureType.AlternateCityAbbreviationPopulation, FeatureGranularity.Continuous },
                { CityFeatureType.AlternateCityAbbreviationLetters, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.AlternateCityAbbreviationRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.AlternateCityAbbreviationLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            var nameVariations = this.GenerateVariationsForNames(entity.AlternateNames);

            foreach (var nameVariation in nameVariations)
            {
                var features = this.InitializeDefaultFeatureValues();

                features[CityFeatureType.AlternateCityAbbreviationMatch] = true;

                if (entity.Population > 0)
                {
                    features[CityFeatureType.AlternateCityAbbreviationPopulation] = (uint?)entity.Population;
                }

                features[CityFeatureType.AlternateCityAbbreviationLetters] = (byte?)nameVariation.Length;

                EntitiesToFeatures entitiesToFeatures;

                if (!variationsToEntitiesToFeatures.TryGetValue(nameVariation, out entitiesToFeatures))
                {
                    entitiesToFeatures = new EntitiesToFeatures();
                    variationsToEntitiesToFeatures[nameVariation] = entitiesToFeatures;
                }

                entitiesToFeatures[entity] = features;
            }
        }

        private HashSet<string> GenerateVariationsForNames(List<GeonamesAlternateNameEntity> nameEntities)
        {
            var variations = new HashSet<string>();

            if (nameEntities != null)
            {
                foreach (var entity in nameEntities)
                {
                    var name = entity.AlternateName;
                    variations.UnionWith(this.GenerateVariationsForName(name));
                }
            }

            return variations;
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
                            features[CityFeatureType.AlternateCityAbbreviationRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.AlternateCityAbbreviationLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
