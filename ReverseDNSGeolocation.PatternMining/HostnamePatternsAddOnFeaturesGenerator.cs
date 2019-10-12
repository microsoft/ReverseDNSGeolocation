namespace ReverseDNSGeolocation.PatternMining
{
    using Classification;
    using Features;
    using Features.CityFeatures.AddOnFeatures;
    using PatternMining;
    using ReverseDNSGeolocation.GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class HostnamePatternsAddOnFeaturesGenerator : AddOnCityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private HostnamePatternMiner miner;

        private Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> hostnamePatternRules;

        public HostnamePatternsAddOnFeaturesGenerator(FeaturesConfig featuresConfig, 
            HostnamePatternMiner miner, 
            Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> hostnamePatternRules) : base(featuresConfig)
        {
            this.miner = miner;
            this.hostnamePatternRules = hostnamePatternRules;

            FeatureDefaults = new Features()
            {
                { CityFeatureType.HostnamePatternMatch, false },
            };

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.HostnamePatternMatch, typeof(bool) },
            };

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.HostnamePatternMatch, FeatureGranularity.Discrete }
            };
        }

        public override void AppendFeatures(HostnameSplitterResult parsedHostname, GeonamesCityEntity cityEntity, Features features)
        {
            if (parsedHostname == null 
                || parsedHostname.DomainInfo?.RegistrableDomain == null 
                || parsedHostname.SubdomainParts == null 
                || parsedHostname.SubdomainParts.Count == 0
                || cityEntity == null)
            {
                return;
            }

            var domain = parsedHostname.DomainInfo.RegistrableDomain;

            Dictionary<PatternRule, PatternMiningCoordinates> rulesToCoordinates;

            if (!this.hostnamePatternRules.TryGetValue(domain, out rulesToCoordinates))
            {
                return;
            }

            var subdomainParts = parsedHostname.SubdomainParts;

            if (subdomainParts == null || subdomainParts.Count == 0)
            {
                return;
            }

            var ruleAtoms = this.miner.CreateRuleAtoms(subdomainParts);

            if (ruleAtoms == null || ruleAtoms.Count == 0)
            {
                return;
            }

            var rules = this.miner.GeneratePossibleRules(ruleAtoms);

            if (rules == null || rules.Count == 0)
            {
                return;
            }

            foreach (var rule in rules)
            {
                PatternMiningCoordinates coordinates;

                if (rulesToCoordinates.TryGetValue(rule, out coordinates))
                {
                    var distance = DistanceHelper.Distance(cityEntity.Latitude, cityEntity.Longitude, coordinates.Latitude, coordinates.Longitude, DistanceUnit.Kilometer);

                    // TODO: Make this configurable
                    // TODO: Distance should vary depending on geohash length?
                    if (distance <= 100)
                    {
                        features[CityFeatureType.HostnamePatternMatch] = true;
                    }
                }
            }
        }
    }
}
