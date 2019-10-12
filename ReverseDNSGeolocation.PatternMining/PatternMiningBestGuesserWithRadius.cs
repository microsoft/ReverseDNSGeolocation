namespace ReverseDNSGeolocation.PatternMining
{
    using Classification;
    using Classification.BestGuess;
    using Classification.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PatternMiningBestGuesserWithRadius : IBestGuesser
    {
        private HostnamePatternMiner miner;
        private Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> reducedRules;
        private int distanceThresholdKm;

        public PatternMiningBestGuesserWithRadius(
            HostnamePatternMiner miner, 
            Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> reducedRules, 
            int distanceThresholdKm)
        {
            this.miner = miner;
            this.reducedRules = reducedRules;
            this.distanceThresholdKm = distanceThresholdKm;
        }

        public ClassificationResult PickBest(string hostname, List<ClassificationResult> results, double minProbability = 0)
        {
            if (results == null)
            {
                return null;
            }

            if (minProbability > 0)
            {
                results = results.Where(i => i.Score >= minProbability).ToList<ClassificationResult>();
            }

            if(results.Count == 0)
            {
                return null;
            }

            if (results.Count == 1)
            {
                return results[0];
            }

            var maxScore = results.Max(x => x.Score);

            if (maxScore == null)
            {
                return null;
            }

            results = results.Where(i => i.Score == maxScore).ToList<ClassificationResult>();

            if (results.Count == 1)
            {
                return this.FilterByPatterns(hostname, results[0]);
            }

            var maxPopulation = results.Max(x => x.City?.Population);

            if (maxPopulation == null)
            {
                return null;
            }

            results = results.Where(i => i.City.Population == maxPopulation).ToList<ClassificationResult>();

            return this.FilterByPatterns(hostname, results[0]);
        }

        private ClassificationResult FilterByPatterns(string hostname, ClassificationResult result)
        {
            if (result == null)
            {
                return null;
            }

            var splitResults = HostnameSplitter.Split(hostname);

            if (splitResults == null || splitResults.DomainInfo?.RegistrableDomain == null || splitResults.SubdomainParts == null || splitResults.SubdomainParts.Count == 0)
            {
                return null;
            }

            var domain = splitResults.DomainInfo.RegistrableDomain;

            Dictionary<PatternRule, PatternMiningCoordinates> rulesToCoordinates;

            if (!this.reducedRules.TryGetValue(domain, out rulesToCoordinates))
            {
                return null;
            }

            var subdomainParts = splitResults.SubdomainParts;

            if (subdomainParts == null || subdomainParts.Count == 0)
            {
                return null;
            }

            var ruleAtoms = this.miner.CreateRuleAtoms(subdomainParts);

            if (ruleAtoms == null || ruleAtoms.Count == 0)
            {
                return null;
            }

            var rules = this.miner.GeneratePossibleRules(ruleAtoms);

            if (rules == null || rules.Count == 0)
            {
                return null;
            }

            var filteredRulesToCoordinates = new Dictionary<PatternRule, PatternMiningCoordinates>();

            foreach (var rule in rules)
            {
                PatternMiningCoordinates coordinates;

                if (rulesToCoordinates.TryGetValue(rule, out coordinates))
                {
                    var distance = DistanceHelper.Distance(result.City.Latitude, result.City.Longitude, coordinates.Latitude, coordinates.Longitude, DistanceUnit.Kilometer);

                    if (distance > this.distanceThresholdKm)
                    {
                        return null;
                    }
                }
            }

            return result;
        }
    }
}
