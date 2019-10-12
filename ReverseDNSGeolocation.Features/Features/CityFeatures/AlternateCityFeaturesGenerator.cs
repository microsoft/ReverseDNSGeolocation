namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class AlternateCityFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> variationsToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        private const int MinNameLength = 4;

        public AlternateCityFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.AlternateCityNameMatch, false },
                    { CityFeatureType.AlternateCityNamePopulation, (uint?)null },
                    { CityFeatureType.AlternateCityNameLetters, (byte?)null },
                    { CityFeatureType.AlternateCityNameAlternateNamesCount, (uint?)null }
                };

                if (this.FeaturesConfig.UseAlternateNameCategories)
                {
                    FeatureDefaults[CityFeatureType.AlternateCityNameIsPreferredName] = false;
                    FeatureDefaults[CityFeatureType.AlternateCityNameIsShortName] = false;
                    FeatureDefaults[CityFeatureType.AlternateCityNameIsColloquial] = false;
                    FeatureDefaults[CityFeatureType.AlternateCityNameIsHistoric] = false;
                }

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.AlternateCityNameRTLSlotIndex] = false;
                    FeatureDefaults[CityFeatureType.AlternateCityNameLTRSlotIndex] = false;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.AlternateCityNameMatch, false },
                    { CityFeatureType.AlternateCityNamePopulation, (uint?)0 },
                    { CityFeatureType.AlternateCityNameLetters, (byte?)0 },
                    { CityFeatureType.AlternateCityNameAlternateNamesCount, (uint?)0 }
                };

                if (this.FeaturesConfig.UseAlternateNameCategories)
                {
                    FeatureDefaults[CityFeatureType.AlternateCityNameIsPreferredName] = false;
                    FeatureDefaults[CityFeatureType.AlternateCityNameIsShortName] = false;
                    FeatureDefaults[CityFeatureType.AlternateCityNameIsColloquial] = false;
                    FeatureDefaults[CityFeatureType.AlternateCityNameIsHistoric] = false;
                }

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.AlternateCityNameRTLSlotIndex] = false;
                    FeatureDefaults[CityFeatureType.AlternateCityNameLTRSlotIndex] = false;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.AlternateCityNameMatch, typeof(bool) },
                { CityFeatureType.AlternateCityNamePopulation, typeof(uint?) },
                { CityFeatureType.AlternateCityNameLetters, typeof(byte?) },
                { CityFeatureType.AlternateCityNameAlternateNamesCount, typeof(uint?) }
            };

            if (this.FeaturesConfig.UseAlternateNameCategories)
            {
                FeatureDefaultsValueTypes[CityFeatureType.AlternateCityNameIsPreferredName] = typeof(bool);
                FeatureDefaultsValueTypes[CityFeatureType.AlternateCityNameIsShortName] = typeof(bool);
                FeatureDefaultsValueTypes[CityFeatureType.AlternateCityNameIsColloquial] = typeof(bool);
                FeatureDefaultsValueTypes[CityFeatureType.AlternateCityNameIsHistoric] = typeof(bool);
            }

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.AlternateCityNameRTLSlotIndex] = typeof(bool);
                FeatureDefaultsValueTypes[CityFeatureType.AlternateCityNameLTRSlotIndex] = typeof(bool);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.AlternateCityNameMatch, FeatureGranularity.Discrete },
                { CityFeatureType.AlternateCityNamePopulation, FeatureGranularity.Continuous },
                { CityFeatureType.AlternateCityNameLetters, FeatureGranularity.Continuous },
                { CityFeatureType.AlternateCityNameAlternateNamesCount, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseAlternateNameCategories)
            {
                FeatureGranularities[CityFeatureType.AlternateCityNameIsPreferredName] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.AlternateCityNameIsShortName] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.AlternateCityNameIsColloquial] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.AlternateCityNameIsHistoric] = FeatureGranularity.Discrete;
            }

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.AlternateCityNameRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.AlternateCityNameLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            if (entity?.AlternateNames != null)
            {
                foreach (var alternateNameEntity in entity.AlternateNames)
                {
                    var nameVariations = this.GenerateVariationsForName(alternateNameEntity);

                    foreach (var nameVariation in nameVariations)
                    {
                        var features = this.InitializeDefaultFeatureValues();

                        features[CityFeatureType.AlternateCityNameMatch] = true;

                        if (entity.Population > 0)
                        {
                            features[CityFeatureType.AlternateCityNamePopulation] = (uint?)entity.Population;
                        }

                        features[CityFeatureType.AlternateCityNameLetters] = (byte?)nameVariation.Length;

                        if (this.FeaturesConfig.UseAlternateNamesCount)
                        {
                            features[CityFeatureType.AlternateCityNameAlternateNamesCount] = (uint?)(entity.AlternateNames?.Count ?? 0);
                        }

                        if (this.FeaturesConfig.UseAlternateNameCategories)
                        {
                            features[CityFeatureType.AlternateCityNameIsPreferredName] = alternateNameEntity.IsPreferredName;
                            features[CityFeatureType.AlternateCityNameIsShortName] = alternateNameEntity.IsShortName;
                            features[CityFeatureType.AlternateCityNameIsColloquial] = alternateNameEntity.IsColloquial;
                            features[CityFeatureType.AlternateCityNameIsHistoric] = alternateNameEntity.IsHistoric;
                        }

                        EntitiesToFeatures entitiesToFeatures;

                        if (!variationsToEntitiesToFeatures.TryGetValue(nameVariation, out entitiesToFeatures))
                        {
                            entitiesToFeatures = new EntitiesToFeatures();
                            variationsToEntitiesToFeatures[nameVariation] = entitiesToFeatures;
                        }

                        Features existingFeatures;

                        if (entitiesToFeatures.TryGetValue(entity, out existingFeatures) && this.FeaturesConfig.UseAlternateNameCategories)
                        {
                            // Merge with existing boolean values
                            existingFeatures[CityFeatureType.AlternateCityNameIsPreferredName] = alternateNameEntity.IsPreferredName || (bool)existingFeatures[CityFeatureType.AlternateCityNameIsPreferredName];
                            existingFeatures[CityFeatureType.AlternateCityNameIsShortName] = alternateNameEntity.IsShortName || (bool)existingFeatures[CityFeatureType.AlternateCityNameIsShortName];
                            existingFeatures[CityFeatureType.AlternateCityNameIsColloquial] = alternateNameEntity.IsColloquial || (bool)existingFeatures[CityFeatureType.AlternateCityNameIsColloquial];
                            existingFeatures[CityFeatureType.AlternateCityNameIsHistoric] = alternateNameEntity.IsHistoric || (bool)existingFeatures[CityFeatureType.AlternateCityNameIsHistoric];
                        }
                        else
                        {
                            entitiesToFeatures[entity] = features;
                        }
                    }
                }
            }
        }

        private HashSet<string> GenerateVariationsForName(GeonamesAlternateNameEntity alternateNameEntity)
        {
            var variations = new HashSet<string>();

            if (alternateNameEntity != null)
            {
                var name = alternateNameEntity.AlternateName;

                foreach (var variation in this.GenerateVariationsForName(name))
                {
                    if (variation.Length >= MinNameLength)
                    {
                        variations.Add(variation);
                    }
                }
            }

            return variations;
        }

        private HashSet<string> GenerateVariationsForName(string name)
        {
            var variations = new HashSet<string>();

            if (!string.IsNullOrWhiteSpace(name))
            {
                variations.Add(name.Replace(" ", string.Empty).ToLowerInvariant());
                variations.Add(name.Replace(" ", "-").ToLowerInvariant());
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
                            features[CityFeatureType.AlternateCityNameRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.AlternateCityNameLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
