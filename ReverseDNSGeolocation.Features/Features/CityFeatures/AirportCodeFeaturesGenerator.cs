namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class AirportCodeFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> variationsToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        public AirportCodeFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.AirportCodeMatch, false },
                    { CityFeatureType.AirportCodeCityPopulation, (uint?)null },
                    { CityFeatureType.AirportCodeLetters, (byte?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.AirportCodeRTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.AirportCodeLTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.AirportCodeMatch, false },
                    { CityFeatureType.AirportCodeCityPopulation, (uint?)0 },
                    { CityFeatureType.AirportCodeLetters, (byte?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.AirportCodeRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.AirportCodeLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.AirportCodeMatch, typeof(bool) },
                { CityFeatureType.AirportCodeCityPopulation, typeof(uint?) },
                { CityFeatureType.AirportCodeLetters, typeof(byte?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.AirportCodeRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.AirportCodeLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.AirportCodeMatch, FeatureGranularity.Discrete },
                { CityFeatureType.AirportCodeCityPopulation, FeatureGranularity.Continuous },
                { CityFeatureType.AirportCodeLetters, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.AirportCodeRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.AirportCodeLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            if (entity.AirportCodes != null)
            {
                foreach (var airportCodeEntity in entity.AirportCodes)
                {
                    if (!string.IsNullOrWhiteSpace(airportCodeEntity.AlternateName))
                    {
                        var airportCode = airportCodeEntity.AlternateName.ToLowerInvariant();

                        var features = this.InitializeDefaultFeatureValues();

                        features[CityFeatureType.AirportCodeMatch] = true;

                        if (entity.Population > 0)
                        {
                            features[CityFeatureType.AirportCodeCityPopulation] = (uint?)entity.Population;
                        }

                        features[CityFeatureType.AirportCodeLetters] = (byte?)airportCode.Length;

                        EntitiesToFeatures entitiesToFeatures;

                        if (!variationsToEntitiesToFeatures.TryGetValue(airportCode, out entitiesToFeatures))
                        {
                            entitiesToFeatures = new EntitiesToFeatures();
                            variationsToEntitiesToFeatures[airportCode] = entitiesToFeatures;
                        }

                        entitiesToFeatures[entity] = features;
                    }
                }
            }
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
                            features[CityFeatureType.AirportCodeRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.AirportCodeLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
