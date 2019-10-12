namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    [Serializable]
    public class CityCountryFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> variationsToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        public CityCountryFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.CityCountryNameMatch, false },
                    { CityFeatureType.CityCountryNamePopulation, (uint?)null },
                    { CityFeatureType.CityCountryLettersBoth, (byte?)null },
                    { CityFeatureType.CityCountryLettersCity, (byte?)null },
                    { CityFeatureType.CityCountryLettersCountry, (byte?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.CityCountryRTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.CityCountryLTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.CityCountryNameMatch, false },
                    { CityFeatureType.CityCountryNamePopulation, (uint?)0 },
                    { CityFeatureType.CityCountryLettersBoth, (byte?)0 },
                    { CityFeatureType.CityCountryLettersCity, (byte?)0 },
                    { CityFeatureType.CityCountryLettersCountry, (byte?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.CityCountryRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.CityCountryLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.CityCountryNameMatch, typeof(bool) },
                { CityFeatureType.CityCountryNamePopulation, typeof(uint?) },
                { CityFeatureType.CityCountryLettersBoth, typeof(byte?) },
                { CityFeatureType.CityCountryLettersCity, typeof(byte?) },
                { CityFeatureType.CityCountryLettersCountry, typeof(byte?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.CityCountryRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.CityCountryLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.CityCountryNameMatch, FeatureGranularity.Discrete },
                { CityFeatureType.CityCountryNamePopulation, FeatureGranularity.Continuous},
                { CityFeatureType.CityCountryLettersBoth, FeatureGranularity.Continuous },
                { CityFeatureType.CityCountryLettersCity, FeatureGranularity.Continuous },
                { CityFeatureType.CityCountryLettersCountry, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.CityCountryRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.CityCountryLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            var nameVariations = this.GenerateVariationsForCityCountryName(entity.Name, entity.AsciiName, entity.AlternateNames, entity.CountryCode, entity.CountryEntity);

            foreach (var nameVariationCompound in nameVariations)
            {
                var features = this.InitializeDefaultFeatureValues();

                features[CityFeatureType.CityCountryNameMatch] = true;

                if (entity.Population > 0)
                {
                    features[CityFeatureType.CityCountryNamePopulation] = (uint?)entity.Population;
                }

                features[CityFeatureType.CityCountryLettersBoth] = (byte?)nameVariationCompound.FullName.Length;
                features[CityFeatureType.CityCountryLettersCity] = (byte?)nameVariationCompound.FirstComponent.Length;
                features[CityFeatureType.CityCountryLettersCountry] = (byte?)nameVariationCompound.SecondComponent.Length;

                EntitiesToFeatures entitiesToFeatures;

                if (!variationsToEntitiesToFeatures.TryGetValue(nameVariationCompound.FullName, out entitiesToFeatures))
                {
                    entitiesToFeatures = new EntitiesToFeatures();
                    variationsToEntitiesToFeatures[nameVariationCompound.FullName] = entitiesToFeatures;
                }

                entitiesToFeatures[entity] = features;
            }
        }

        private HashSet<CompoundName> GenerateVariationsForCityCountryName(string name, string asciiName, List<GeonamesAlternateNameEntity> alternateNameEntities, string countryCode, GeonamesCountryEntity countryEntity)
        {
            var variations = new HashSet<CompoundName>();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(countryCode))
            {
                return variations;
            }

            var countryNameVariations = new HashSet<string>();

            countryNameVariations.Add(countryCode.ToLowerInvariant());

            if (countryEntity != null)
            {
                countryNameVariations.UnionWith(countryEntity.NameVariationsLower);
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
                    foreach (var countryNameVariation in countryNameVariations)
                    {
                        var variation1 = string.Format(CultureInfo.InvariantCulture, "{0}{1}", cityNameVariation, countryNameVariation).ToLowerInvariant();
                        variations.Add(new CompoundName(variation1, cityNameVariation.ToLowerInvariant(), countryNameVariation.ToLowerInvariant()));

                        var variation2 = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", cityNameVariation, countryNameVariation).ToLowerInvariant();
                        variations.Add(new CompoundName(variation2, cityNameVariation.ToLowerInvariant(), countryNameVariation.ToLowerInvariant()));
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
                            features[CityFeatureType.CityCountryRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.CityCountryLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
