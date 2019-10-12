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

    public class PatternMiningBestGuesser : IBestGuesser
    {
        private HostnamePatternMiner miner;
        private Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> reducedRules;
        private int distanceThresholdKm;
        private bool forceIntersect;

        public PatternMiningBestGuesser(
            HostnamePatternMiner miner, 
            Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> reducedRules, 
            int distanceThresholdKm, 
            bool forceIntersect)
        {
            this.miner = miner;
            this.reducedRules = reducedRules;
            this.distanceThresholdKm = distanceThresholdKm;
            this.forceIntersect = forceIntersect;
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

            var bestByPattern = this.PickBestByPattern(hostname, results);

            if (this.forceIntersect)
            {
                return bestByPattern;
            }

            if (bestByPattern != null)
            {
                return bestByPattern;
            }

            var maxScore = results.Max(x => x.Score);

            if (maxScore == null)
            {
                return null;
            }

            results = results.Where(i => i.Score == maxScore).ToList<ClassificationResult>();

            if (results.Count == 1)
            {
                return results[0];
            }

            var maxPopulation = results.Max(x => x.City?.Population);

            if (maxPopulation == null)
            {
                return null;
            }

            results = results.Where(i => i.City.Population == maxPopulation).ToList<ClassificationResult>();

            return results[0]; 
        }

        private ClassificationResult PickBestByPattern(string hostname, List<ClassificationResult> results)
        {
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
                    filteredRulesToCoordinates[rule] = coordinates;
                }
            }

            ClassificationResult closestResult = null;
            double smallestDistanceKm = int.MaxValue;
            PatternRule bestRule = null;

            foreach (var result in results)
            {
                if (result.City != null)
                {
                    foreach (var entry in filteredRulesToCoordinates)
                    {
                        var rule = entry.Key;
                        var coordinates = entry.Value;

                        var distance = DistanceHelper.Distance(result.City.Latitude, result.City.Longitude, coordinates.Latitude, coordinates.Longitude, DistanceUnit.Kilometer);

                        if (distance < smallestDistanceKm)
                        {
                            closestResult = result;
                            smallestDistanceKm = distance;
                            bestRule = rule;
                        }
                    }
                }
            }

            if (closestResult != null && smallestDistanceKm <= this.distanceThresholdKm)
            {
                return closestResult;
            }

            return null;
        }
    }
}
