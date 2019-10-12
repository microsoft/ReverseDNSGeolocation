namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    [Serializable]
    public class CityAdmin1FeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> variationsToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        public CityAdmin1FeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.CityAdmin1NameMatch, false },
                    { CityFeatureType.CityAdmin1NamePopulation, (uint?)null },
                    { CityFeatureType.CityAdmin1LettersBoth, (byte?)null },
                    { CityFeatureType.CityAdmin1LettersCity, (byte?)null },
                    { CityFeatureType.CityAdmin1LettersAdmin1, (byte?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.CityAdmin1RTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.CityAdmin1LTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.CityAdmin1NameMatch, false },
                    { CityFeatureType.CityAdmin1NamePopulation, (uint?)0 },
                    { CityFeatureType.CityAdmin1LettersBoth, (byte?)0 },
                    { CityFeatureType.CityAdmin1LettersCity, (byte?)0 },
                    { CityFeatureType.CityAdmin1LettersAdmin1, (byte?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.CityAdmin1RTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.CityAdmin1LTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.CityAdmin1NameMatch, typeof(bool) },
                { CityFeatureType.CityAdmin1NamePopulation, typeof(uint?) },
                { CityFeatureType.CityAdmin1LettersBoth, typeof(byte?) },
                { CityFeatureType.CityAdmin1LettersCity, typeof(byte?) },
                { CityFeatureType.CityAdmin1LettersAdmin1, typeof(byte?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.CityAdmin1RTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.CityAdmin1LTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.CityAdmin1NameMatch, FeatureGranularity.Discrete },
                { CityFeatureType.CityAdmin1NamePopulation, FeatureGranularity.Continuous },
                { CityFeatureType.CityAdmin1LettersBoth, FeatureGranularity.Continuous },
                { CityFeatureType.CityAdmin1LettersCity, FeatureGranularity.Continuous },
                { CityFeatureType.CityAdmin1LettersAdmin1, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.CityAdmin1RTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.CityAdmin1LTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            var nameVariations = this.GenerateVariationsForCityAdmin1Name(entity.Name, entity.AsciiName, entity.AlternateNames, entity.Admin1Code, entity.Admin1Entity);

            foreach (var nameVariationCompound in nameVariations)
            {
                var features = this.InitializeDefaultFeatureValues();

                features[CityFeatureType.CityAdmin1NameMatch] = true;

                if (entity.Population > 0)
                {
                    features[CityFeatureType.CityAdmin1NamePopulation] = (uint?)entity.Population;
                }

                features[CityFeatureType.CityAdmin1LettersBoth] = (byte?)nameVariationCompound.FullName.Length;
                features[CityFeatureType.CityAdmin1LettersCity] = (byte?)nameVariationCompound.FirstComponent.Length;
                features[CityFeatureType.CityAdmin1LettersAdmin1] = (byte?)nameVariationCompound.SecondComponent.Length;

                EntitiesToFeatures entitiesToFeatures;

                if (!variationsToEntitiesToFeatures.TryGetValue(nameVariationCompound.FullName, out entitiesToFeatures))
                {
                    entitiesToFeatures = new EntitiesToFeatures();
                    variationsToEntitiesToFeatures[nameVariationCompound.FullName] = entitiesToFeatures;
                }

                entitiesToFeatures[entity] = features;
            }
        }

        private HashSet<CompoundName> GenerateVariationsForCityAdmin1Name(string name, string asciiName, List<GeonamesAlternateNameEntity> alternateNameEntities, string admin1Code, GeonamesAdminEntity admin1Entity)
        {
            var variations = new HashSet<CompoundName>();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(admin1Code))
            {
                return variations;
            }

            var admin1NameVariations = new HashSet<string>();

            if (admin1Entity != null)
            {
                admin1NameVariations.UnionWith(admin1Entity.NameVariationsLower);
            }

            var cityNameVariations = new HashSet<string>();

            cityNameVariations.Add(name.Replace(" ", string.Empty).ToLowerInvariant());
            cityNameVariations.Add(name.Replace("-", string.Empty).ToLowerInvariant());

            if (!string.IsNullOrWhiteSpace(asciiName))
            {
                cityNameVariations.Add(asciiName.Replace(" ", string.Empty).ToLowerInvariant());
                cityNameVariations.Add(asciiName.Replace("-", string.Empty).ToLowerInvariant());
            }

            if (alternateNameEntities != null)
            {
                foreach (var alternateNameEntity in alternateNameEntities)
                {
                    if (!string.IsNullOrWhiteSpace(alternateNameEntity.AlternateName))
                    {
                        var alternateName = alternateNameEntity.AlternateName;

                        cityNameVariations.Add(alternateName.Replace(" ", string.Empty).ToLowerInvariant());
                        cityNameVariations.Add(alternateName.Replace("-", string.Empty).ToLowerInvariant());
                    }
                }
            }

            foreach (var cityNameVariation in cityNameVariations)
            {
                // Only output the feature if the city name is at least 2 characters
                if (cityNameVariation.Length >= 2)
                {
                    foreach (var admin1NameVariation in admin1NameVariations)
                    {
                        var variation1 = string.Format(CultureInfo.InvariantCulture, "{0}{1}", cityNameVariation, admin1NameVariation).ToLowerInvariant();
                        variations.Add(new CompoundName(variation1, cityNameVariation.ToLowerInvariant(), admin1NameVariation.ToLowerInvariant()));

                        var variation2 = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", cityNameVariation, admin1NameVariation).ToLowerInvariant();
                        variations.Add(new CompoundName(variation2, cityNameVariation.ToLowerInvariant(), admin1NameVariation.ToLowerInvariant()));
                    }
                }
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
                            features[CityFeatureType.CityAdmin1RTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.CityAdmin1LTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
