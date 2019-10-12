namespace ReverseDNSGeolocation.PatternMining
{
    using Classification;
    using Features;
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;


    [Serializable]
    public class HostnamePatternsFeaturesGenerator : CityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private HostnamePatternMiner miner;

        private Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> hostnamePatternRules;

        public HostnamePatternsFeaturesGenerator()
        {
        }

        public HostnamePatternsFeaturesGenerator(
            FeaturesConfig featuresConfig,
            HostnamePatternMiner miner,
            Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> hostnamePatternRules) : base(featuresConfig)
        {
            this.miner = miner;
            this.hostnamePatternRules = hostnamePatternRules;

            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.HostnamePatternMatch, false },
                    { CityFeatureType.HostnamePatternConfidence, (float?)null }
                };
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.HostnamePatternMatch, false },
                    { CityFeatureType.HostnamePatternConfidence, (float?)0 }
                };
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.HostnamePatternMatch, typeof(bool) },
                { CityFeatureType.HostnamePatternConfidence, typeof(float?) }
            };

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.HostnamePatternMatch, FeatureGranularity.Discrete },
                { CityFeatureType.HostnamePatternConfidence, FeatureGranularity.Continuous }
            };
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
        }

        public override Dictionary<GeonamesCityEntity, Features> GenerateCandidatesAndFeatures(HostnameSplitterResult parsedHostname)
        {
            var candidatesAndFeatures = new Dictionary<GeonamesCityEntity, Features>();

            var domain = parsedHostname?.DomainInfo?.RegistrableDomain;

            var subdomainParts = parsedHostname.SubdomainParts;

            if (subdomainParts == null || subdomainParts.Count == 0)
            {
                return candidatesAndFeatures;
            }

            var ruleAtoms = this.miner.CreateRuleAtoms(subdomainParts);

            var rules = this.miner.GeneratePossibleRules(ruleAtoms);

            if (rules == null || rules.Count == 0)
            {
                return candidatesAndFeatures;
            }

            PatternMiningCoordinates bestCoordinates = null;
            Dictionary<PatternRule, PatternMiningCoordinates> rulesToCoordinates;

            if (this.hostnamePatternRules.TryGetValue(domain, out rulesToCoordinates))
            {
                foreach (var rule in rules)
                {
                    PatternMiningCoordinates currentCoordinates;

                    if (rulesToCoordinates.TryGetValue(rule, out currentCoordinates))
                    {
                        if (currentCoordinates.ClosestCity != null)
                        {
                            if (bestCoordinates == null || currentCoordinates.Confidence > bestCoordinates.Confidence)
                            {
                                bestCoordinates = currentCoordinates;
                            }
                        }
                    }
                }
            }

            if (bestCoordinates != null)
            {
                var features = this.InitializeDefaultFeatureValues();
                features[CityFeatureType.HostnamePatternMatch] = true;
                features[CityFeatureType.HostnamePatternConfidence] = bestCoordinates.Confidence;
                candidatesAndFeatures[bestCoordinates.ClosestCity] = features;
            }

            return candidatesAndFeatures;
        }
    }
}
