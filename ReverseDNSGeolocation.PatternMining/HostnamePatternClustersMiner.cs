namespace ReverseDNSGeolocation.PatternMining
{
    using Accord.MachineLearning.Rules;
    using Classification.DatasetParsers;
    using Clustering;
    using Clustering.QT;
    using NGeoHash.Portable;
    using ReverseDNSGeolocation;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class HostnamePatternClustersMiner
    {
        public Dictionary<string, Dictionary<PatternRule, List<PatternMiningCoordinates>>> MinePatternsFromGT(
            GroundTruthParser datasetParser, 
            string inPath,
            int minRuleOcc,
            double clusterThresholdKm,
            int minItemsPerCluster,
            double minSupportRatioPerCluster,
            int pruneIntervalCount = 10000,
            int pruneMinKeepThreshold = 10)
        {
            // Example:               frontiernet.net  435463
            var domainCounts = new Dictionary<string, int>();

            // Keys example:                     frontiernet.net         wlfr|rtl1   79
            var ruleCountsForDomains = new Dictionary<string, Dictionary<PatternRule, int>>();

            // Keys example:                            frontiernet.net         wlfr|rtl1           X,Y (coordinates)
            var domainsToRulesToCoordinates = new Dictionary<string, Dictionary<PatternRule, HashSet<PatternMiningCoordinates>>>();

            /*
            // Keys example:             frontiernet.net       wlfr|rtl1           drkh7        15
            var rulesGeohashCounts = new Dictionary<string, Dictionary<PatternRule, Dictionary<string, int>>>();
            */

            var processCount = 0;

            foreach (var gtItem in datasetParser.Parse(inPath, populateTextualLocationInfo: true))
            {
                var hostname = gtItem.Hostname;
                var splitResults = HostnameSplitter.Split(hostname);

                if (splitResults == null || splitResults.DomainInfo?.RegistrableDomain == null || splitResults.SubdomainParts == null || splitResults.SubdomainParts.Count == 0)
                {
                    continue;
                }

                processCount++;

                if (processCount % 100000 == 0)
                {
                    Console.WriteLine(processCount);
                }

                var domain = splitResults.DomainInfo.RegistrableDomain;
                var subdomainParts = splitResults.SubdomainParts;

                var ruleAtoms = this.CreateRuleAtoms(subdomainParts);
                var rules = this.GeneratePossibleRules(ruleAtoms);

                this.AddRulesCoordinatesToDomain(domainCounts, ruleCountsForDomains, domainsToRulesToCoordinates, domain, rules, gtItem, pruneIntervalCount, pruneMinKeepThreshold);
            }

            this.DeleteRulesBelowOccThreshold(domainCounts, ruleCountsForDomains, domainsToRulesToCoordinates, minRuleOcc);
            this.DeleteEquivalentRules(ruleCountsForDomains, domainsToRulesToCoordinates);

            var domainsToRulesToCentroids = this.FindClusterCentroids(domainCounts, ruleCountsForDomains, domainsToRulesToCoordinates, clusterThresholdKm, minItemsPerCluster, minSupportRatioPerCluster);

            return domainsToRulesToCentroids;
        }

        private Dictionary<string, Dictionary<PatternRule, List<PatternMiningCoordinates>>> FindClusterCentroids(
            Dictionary<string, int> domainCounts,
            Dictionary<string, Dictionary<PatternRule, int>> ruleCountsForDomains,
            Dictionary<string, Dictionary<PatternRule, HashSet<PatternMiningCoordinates>>> domainsToRulesToCoordinates,
            double clusterThresholdKm,
            int minItemsPerCluster,
            double minSupportRatioPerCluster)
        {
            var domainsToRulesToCentroids = new Dictionary<string, Dictionary<PatternRule, List<PatternMiningCoordinates>>>();

            foreach (var domainsToRulesToCoordinatesEntry in domainsToRulesToCoordinates)
            {
                var domain = domainsToRulesToCoordinatesEntry.Key;
                var rulesToCoordinates = domainsToRulesToCoordinatesEntry.Value;

                Dictionary<PatternRule, List<PatternMiningCoordinates>> rulesToCentroids = null;

                foreach (var rulesToCoordinatesEntry in rulesToCoordinates)
                {
                    var rule = rulesToCoordinatesEntry.Key;
                    List<PatternMiningCoordinates> centroidsForRule = null;

                    var coordinates = rulesToCoordinatesEntry.Value;
                    var originalCoordinatesCount = coordinates.Count;

                    var clustering = new QTClustering<PatternMiningCoordinates>(
                        distanceHelper: new PatternMiningCoordinatesDistanceHelper(),
                        clusterDiameter: clusterThresholdKm,
                        itemsSet: coordinates);

                    Cluster<PatternMiningCoordinates> cluster = null;

                    do
                    {
                        cluster = clustering.NextCluster();
                        
                        if (cluster == null)
                        {
                            break;
                        }

                        if (cluster.Members.Count < minItemsPerCluster)
                        {
                            break;
                        }

                        var supportRatio = cluster.Members.Count / ((1.0d) * originalCoordinatesCount);

                        if (supportRatio < minSupportRatioPerCluster)
                        {
                            break;
                        }

                        if (rulesToCentroids == null)
                        {
                            rulesToCentroids = this.RetrieveRulesToCentroidsList(domainsToRulesToCentroids, domain);
                        }

                        if (centroidsForRule == null)
                        {
                            centroidsForRule = this.RetrieveCentroidsList(rulesToCentroids, rule);
                        }

                        centroidsForRule.Add(new PatternMiningCoordinates()
                        {
                            Latitude = cluster.Centroid.Latitude,
                            Longitude = cluster.Centroid.Longitude,
                            Confidence = supportRatio
                        });

                        coordinates.ExceptWith(cluster.Members);

                        var maxRemainingSupportRatio = coordinates.Count / ((1.0d) * originalCoordinatesCount);

                        if (maxRemainingSupportRatio < minSupportRatioPerCluster)
                        {
                            break;
                        }
                    }
                    while (cluster != null);
                }
            }

            return domainsToRulesToCentroids;
        }

        private void DeleteRulesBelowOccThreshold(
            Dictionary<string, int> domainCounts,
            Dictionary<string, Dictionary<PatternRule, int>> ruleCountsForDomains,
            Dictionary<string, Dictionary<PatternRule, HashSet<PatternMiningCoordinates>>> domainsToRulesToCoordinates,
            int minRuleOcc)
        {
            foreach (var domainToRuleCountsEntry in ruleCountsForDomains)
            {
                var domain = domainToRuleCountsEntry.Key;
                var ruleCounts = domainToRuleCountsEntry.Value;

                var rulesToDelete = new List<PatternRule>();

                foreach (var ruleCountEntry in ruleCounts)
                {
                    if (ruleCountEntry.Value < minRuleOcc)
                    {
                        rulesToDelete.Add(ruleCountEntry.Key);
                    }
                }

                if (rulesToDelete.Count > 0)
                {
                    var rulesToCoordinates = domainsToRulesToCoordinates[domain];

                    foreach (var ruleToDelete in rulesToDelete)
                    {
                        rulesToCoordinates.Remove(ruleToDelete);
                        ruleCounts.Remove(ruleToDelete);
                    }
                }

                if (ruleCounts.Count == 0)
                {
                    domainCounts.Remove(domain);
                }
            }
        }

        private void DeleteEquivalentRules(
            Dictionary<string, Dictionary<PatternRule, int>> ruleCountsForDomains,
            Dictionary<string, Dictionary<PatternRule, HashSet<PatternMiningCoordinates>>> domainsToRulesToCoordinates)
        {
            foreach (var domainToRulesToCoordinatesEntry in domainsToRulesToCoordinates)
            {
                var domain = domainToRulesToCoordinatesEntry.Key;

                var ruleCounts = ruleCountsForDomains[domain];
                var rulesToCoordinates = domainToRulesToCoordinatesEntry.Value;

                var rulesToDelete = new HashSet<PatternRule>();

                var rulesList = new List<PatternRule>(rulesToCoordinates.Keys);

                for (var i = 0; i < rulesList.Count - 1; i++)
                {
                    var rule1 = rulesList[i];
                    var entry1 = rulesToCoordinates[rule1];

                    for (var j = i + 1; j < rulesList.Count; j++)
                    {
                        var rule2 = rulesList[j];
                        var entry2 = rulesToCoordinates[rule2];

                        if (entry1.SequenceEqual(entry2))
                        {
                            rulesToDelete.Add(this.DetermineRuleToDelete(rule1, rule2));
                        }
                    }
                }

                foreach (var rule in rulesToDelete)
                {
                    ruleCounts.Remove(rule);
                    rulesToCoordinates.Remove(rule);
                }
            }
        }

        private PatternRule DetermineRuleToDelete(PatternRule rule1, PatternRule rule2)
        {
            if (rule1.Atoms.Count > rule2.Atoms.Count)
            {
                return rule2;
            }
            else if (rule1.Atoms.Count == rule2.Atoms.Count)
            {
                var rule1AtomsLength = 0;
                rule1.Atoms.ForEach(a => rule1AtomsLength += a.Substring.Length);

                var rule2AtomsLength = 0;
                rule2.Atoms.ForEach(a => rule2AtomsLength += a.Substring.Length);

                if (rule1AtomsLength > rule2AtomsLength)
                {
                    return rule2;
                }
                if (rule1AtomsLength == rule2AtomsLength)
                {
                    var rule1RTLCount = 0;
                    rule1.Atoms.ForEach(a => rule1RTLCount += a.IndexType == IndexType.RTL ? 1 : 0);

                    var rule2RTLCount = 0;
                    rule2.Atoms.ForEach(a => rule2RTLCount += a.IndexType == IndexType.RTL ? 1 : 0);

                    if (rule1RTLCount >= rule2RTLCount)
                    {
                        return rule2;
                    }

                    return rule1;
                }
                else
                {
                    return rule1;
                }
            }
            else
            {
                return rule1;
            }
        }

        private void AddRulesCoordinatesToDomain(
            Dictionary<string, int> domainCounts,
            Dictionary<string, Dictionary<PatternRule, int>> ruleCountsForDomains,
            Dictionary<string, Dictionary<PatternRule, HashSet<PatternMiningCoordinates>>> domainsToRulesToCoordinates,
            string domain, 
            List<PatternRule> rules, 
            DatasetItem gtItem,
            int pruneIntervalCount,
            int pruneMinKeepThreshold)
        {
            if (
                domainsToRulesToCoordinates == null 
                || string.IsNullOrWhiteSpace(domain) 
                || rules == null 
                || rules.Count == 0 
                || gtItem == null)
            {
                return;
            }

            var rulesToCoordinates = this.RetrieveRulesToCoordinatesSet(domainsToRulesToCoordinates, domain);

            var newCoordinates = new PatternMiningCoordinates()
            {
                Latitude = gtItem.Latitude,
                Longitude = gtItem.Longitude,
                Confidence = 0d
            };

            var rulesCountsForDomain = this.AddRetrieveDomainToRulesCounts(ruleCountsForDomains, domain);

            foreach (var rule in rules)
            {
                this.IncrementOccurrences(rulesCountsForDomain, rule);

                HashSet<PatternMiningCoordinates> coordinatesForRule;

                if (!rulesToCoordinates.TryGetValue(rule, out coordinatesForRule))
                {
                    coordinatesForRule = new HashSet<PatternMiningCoordinates>();
                    rulesToCoordinates[rule] = coordinatesForRule;
                }

                coordinatesForRule.Add(newCoordinates);
            }

            var domainOcc = this.IncrementOccurrences(domainCounts, domain);

            if (domainOcc % pruneIntervalCount == 0)
            {
                this.PruneCounts(rulesCountsForDomain, rulesToCoordinates, minKeepThreshold: pruneMinKeepThreshold);
            }
        }

        public Dictionary<PatternRule, HashSet<PatternMiningCoordinates>> RetrieveRulesToCoordinatesSet(
            Dictionary<string, Dictionary<PatternRule, HashSet<PatternMiningCoordinates>>> domainsToRulesToCoordinates, 
            string domain)
        {
            Dictionary<PatternRule, HashSet<PatternMiningCoordinates>> rulesToCoordinates;

            if (!domainsToRulesToCoordinates.TryGetValue(domain, out rulesToCoordinates))
            {
                rulesToCoordinates = new Dictionary<PatternRule, HashSet<PatternMiningCoordinates>>();
                domainsToRulesToCoordinates[domain] = rulesToCoordinates;
            }

            return rulesToCoordinates;
        }

        public Dictionary<PatternRule, List<PatternMiningCoordinates>> RetrieveRulesToCentroidsList(
            Dictionary<string, Dictionary<PatternRule, List<PatternMiningCoordinates>>> domainsToRulesToCentroids,
            string domain)
        {
            Dictionary<PatternRule, List<PatternMiningCoordinates>> rulesToCentroids;

            if (!domainsToRulesToCentroids.TryGetValue(domain, out rulesToCentroids))
            {
                rulesToCentroids = new Dictionary<PatternRule, List<PatternMiningCoordinates>>();
                domainsToRulesToCentroids[domain] = rulesToCentroids;
            }

            return rulesToCentroids;
        }

        public List<PatternMiningCoordinates> RetrieveCentroidsList(
            Dictionary<PatternRule, List<PatternMiningCoordinates>> rulesToCentroids,
            PatternRule rule)
        {
            List<PatternMiningCoordinates> centroids;

            if (!rulesToCentroids.TryGetValue(rule, out centroids))
            {
                centroids = new List<PatternMiningCoordinates>();
                rulesToCentroids[rule] = centroids;
            }

            return centroids;
        }

        public List<PatternRuleAtom> CreateRuleAtoms(HashSet<SubdomainPart> subdomainParts)
        {
            var ruleAtoms = new List<PatternRuleAtom>();

            foreach (var part in subdomainParts)
            {
                if (!string.IsNullOrWhiteSpace(part.Substring))
                {
                    ruleAtoms.Add(new PatternRuleAtom()
                    {
                        Substring = part.Substring,
                        IndexType = IndexType.LTR,
                        Index = part.LTRSlotIndex
                    });

                    ruleAtoms.Add(new PatternRuleAtom()
                    {
                        Substring = part.Substring,
                        IndexType = IndexType.RTL,
                        Index = part.RTLSlotIndex
                    });

                }
            }

            return ruleAtoms;
        }

        private void PruneCounts(
            Dictionary<PatternRule, int> rulesCountsForDomain,
            Dictionary<PatternRule, HashSet<PatternMiningCoordinates>> rulesToCoordinates,
            int minKeepThreshold)
        {
            var keysToDelete = new HashSet<PatternRule>();

            foreach (var entry in rulesCountsForDomain)
            {
                var count = entry.Value;

                if (count < minKeepThreshold)
                {
                    var key = entry.Key;
                    keysToDelete.Add(key);
                }
            }

            foreach (var key in keysToDelete)
            {
                rulesCountsForDomain.Remove(key);
                rulesToCoordinates.Remove(key);
            }
        }

        private Dictionary<PatternRule, int> AddRetrieveDomainToRulesCounts(Dictionary<string, Dictionary<PatternRule, int>> subdomainPartCounts, string domain)
        {
            Dictionary<PatternRule, int> subdomainPartsToCounts;

            if (!subdomainPartCounts.TryGetValue(domain, out subdomainPartsToCounts))
            {
                subdomainPartsToCounts = new Dictionary<PatternRule, int>();
                subdomainPartCounts[domain] = subdomainPartsToCounts;
            }

            return subdomainPartsToCounts;
        }

        public List<PatternRule> GeneratePossibleRules(List<PatternRuleAtom> ruleAtoms)
        {
            var rules = new List<PatternRule>();

            // One atom per rule
            foreach (var atom in ruleAtoms)
            {
                rules.Add(new PatternRule()
                {
                    Atoms = new List<PatternRuleAtom>()
                    {
                        atom
                    }
                });
            }

            // Two atoms per rule
            for (var i = 0; i < ruleAtoms.Count; i++)
            {
                for (var j = i + 1; j < ruleAtoms.Count; j++)
                {
                    var atomA = ruleAtoms[i];
                    var atomB = ruleAtoms[j];

                    if (atomA.Substring != atomB.Substring)
                    {
                        rules.Add(new PatternRule()
                        {
                            Atoms = new List<PatternRuleAtom>()
                            {
                                atomA,
                                atomB
                            }
                        });
                    }
                }
            }

            return rules;
        }

        private int IncrementOccurrences<T>(
            Dictionary<T, int> occurrences,
            T key)
        {
            if (occurrences == null || key == null)
            {
                return -1;
            }

            int currentOcc;

            if (!occurrences.TryGetValue(key, out currentOcc))
            {
                currentOcc = 0;
            }

            currentOcc++;
            occurrences[key] = currentOcc;

            return currentOcc;
        }
    }
}
