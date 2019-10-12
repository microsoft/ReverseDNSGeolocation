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
    public class HostnamePatternCoarseClustersMiner
    {
        public Dictionary<string, Dictionary<PatternRule, List<PatternMiningCoordinates>>> MinePatternsFromGT(
            GroundTruthParser datasetParser, 
            string inPath,
            int minRuleOcc,
            double clusterThresholdKm,
            int minItemsPerCluster,
            double minSupportRatioPerCluster,
            int pruneIntervalCountPerDomain = 10000,
            int pruneMinKeepThreshold = 2)
        {
            // Example:               frontiernet.net  435463
            var domainCounts = new Dictionary<string, int>();

            // Keys example:                     frontiernet.net         wlfr|rtl1   79
            var ruleCountsForDomains = new Dictionary<string, Dictionary<PatternRule, int>>();

            // Keys example:                            frontiernet.net         wlfr|rtl1           X,Y (coordinates)
            var domainsToRulesToCoordinateCounts = new Dictionary<string, Dictionary<PatternRule, Dictionary<Coord, int>>>();

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

                /*
                if (processCount > 500000)
                {
                    break;
                }
                */

                var domain = splitResults.DomainInfo.RegistrableDomain;
                var subdomainParts = splitResults.SubdomainParts;

                var ruleAtoms = this.CreateRuleAtoms(subdomainParts);
                var rules = this.GeneratePossibleRules(ruleAtoms);

                this.AddRulesCoordinatesToDomain(domainCounts, ruleCountsForDomains, domainsToRulesToCoordinateCounts, domain, rules, gtItem, pruneIntervalCountPerDomain, pruneMinKeepThreshold);
            }

            // TODO: One more prune run here?

            Console.WriteLine("Performing final prune");

            foreach (var domainCountsEntry in domainCounts)
            {
                var domain = domainCountsEntry.Key;
                var ruleCountsForDomain = ruleCountsForDomains[domain];

                Console.WriteLine();
                Console.WriteLine($"Before pruning domain {domain} we had {ruleCountsForDomain.Count} rules");
                this.PruneCounts(ruleCountsForDomains, domainsToRulesToCoordinateCounts, domain, minKeepThreshold: pruneMinKeepThreshold);
                Console.WriteLine($"After pruning domain {domain} we had {ruleCountsForDomain.Count} rules");
                Console.WriteLine();
            }

            this.DeleteRulesBelowOccThreshold(ruleCountsForDomains, domainsToRulesToCoordinateCounts, minRuleOcc);
            this.DeleteEquivalentRules(ruleCountsForDomains, domainsToRulesToCoordinateCounts);

            var domainsToRulesToCentroids = this.FindClusterCentroids(domainCounts, ruleCountsForDomains, domainsToRulesToCoordinateCounts, clusterThresholdKm, minItemsPerCluster, minSupportRatioPerCluster);

            return domainsToRulesToCentroids;
        }

        private Dictionary<string, Dictionary<PatternRule, List<PatternMiningCoordinates>>> FindClusterCentroids(
            Dictionary<string, int> domainCounts,
            Dictionary<string, Dictionary<PatternRule, int>> ruleCoordOccsForDomains,
            Dictionary<string, Dictionary<PatternRule, Dictionary<Coord, int>>> domainsToRulesToCoordinateCounts,
            double clusterThresholdKm,
            int minItemsPerCluster,
            double minSupportRatioPerCluster)
        {
            var domainsToRulesToCentroids = new Dictionary<string, Dictionary<PatternRule, List<PatternMiningCoordinates>>>();

            var totalDomains = domainsToRulesToCoordinateCounts.Count;
            var currentDomainCount = 0;

            foreach (var domainsToRulesToCoordinateCountsEntry in domainsToRulesToCoordinateCounts)
            {
                currentDomainCount++;
                var domain = domainsToRulesToCoordinateCountsEntry.Key;
                var rulesToCoordinateCounts = domainsToRulesToCoordinateCountsEntry.Value;

                var ruleCoordOccsForDomain = ruleCoordOccsForDomains[domain];

                var totalRulesForDomain = rulesToCoordinateCounts.Count();

                Dictionary<PatternRule, List<PatternMiningCoordinates>> rulesToCentroids = null;

                var currentRuleCount = 0;

                foreach (var rulesToCoordinateCountsEntry in rulesToCoordinateCounts)
                {
                    currentRuleCount++;
                    var rule = rulesToCoordinateCountsEntry.Key;
                    List<PatternMiningCoordinates> centroidsForRule = null;

                    Console.WriteLine($"{currentDomainCount}/{totalDomains} - {currentRuleCount}/{totalRulesForDomain} - Finding clusters for domain {domain} and rule {rule}");

                    var coordinateToCounts = rulesToCoordinateCountsEntry.Value;
                    var originalCoordinatesOccSum = ruleCoordOccsForDomain[rule];

                    var coordinatesWithOcc = new HashSet<CoordWithOcc>(coordinateToCounts.Select(x => new CoordWithOcc(x.Key, x.Value)));

                    var clustering = new QTClustering<CoordWithOcc>(
                        distanceHelper: new CoordWithOccDistanceHelper(),
                        clusterDiameter: clusterThresholdKm,
                        itemsSet: coordinatesWithOcc);

                    Cluster<CoordWithOcc> cluster = null;

                    do
                    {
                        cluster = clustering.NextCluster();
                        
                        if (cluster == null)
                        {
                            break;
                        }

                        var clusterMembersSum = cluster.Members.Sum(c => c.Occurrences);

                        if (clusterMembersSum < minItemsPerCluster)
                        {
                            break;
                        }

                        var supportRatio = clusterMembersSum / ((1.0d) * originalCoordinatesOccSum);

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

                        var centroidCoordinates = new PatternMiningCoordinates()
                        {
                            Latitude = cluster.Centroid.Coord.Latitude,
                            Longitude = cluster.Centroid.Coord.Longitude,
                            Confidence = supportRatio
                        };

                        centroidsForRule.Add(centroidCoordinates);

                        Console.WriteLine($"                                                Found centroid: {centroidCoordinates}");

                        coordinatesWithOcc.ExceptWith(cluster.Members);

                        var maxRemainingSupportRatio = coordinatesWithOcc.Sum(c => c.Occurrences) / ((1.0d) * originalCoordinatesOccSum);

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
            Dictionary<string, Dictionary<PatternRule, int>> ruleCountsForDomains,
            Dictionary<string, Dictionary<PatternRule, Dictionary<Coord, int>>> domainsToRulesToCoordinateCounts,
            int minRuleOcc)
        {
            var domainsToDelete = new List<string>();

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
                    var rulesToCoordinates = domainsToRulesToCoordinateCounts[domain];

                    foreach (var ruleToDelete in rulesToDelete)
                    {
                        rulesToCoordinates.Remove(ruleToDelete);
                        ruleCounts.Remove(ruleToDelete);
                    }
                }

                if (ruleCounts.Count == 0)
                {
                    domainsToDelete.Add(domain);
                }
            }

            foreach (var domainToDelete in domainsToDelete)
            {
                this.DeleteDomain(ruleCountsForDomains, domainsToRulesToCoordinateCounts, domainToDelete);
            }
        }

        private void DeleteDomain(
            Dictionary<string, Dictionary<PatternRule, int>> ruleCountsForDomains,
            Dictionary<string, Dictionary<PatternRule, Dictionary<Coord, int>>> domainsToRulesToCoordinateCounts,
            string domainToDelete)
        {
            ruleCountsForDomains.Remove(domainToDelete);
            domainsToRulesToCoordinateCounts.Remove(domainToDelete);
        }

        private void DeleteEquivalentRules(
            Dictionary<string, Dictionary<PatternRule, int>> ruleCountsForDomains,
            Dictionary<string, Dictionary<PatternRule, Dictionary<Coord, int>>> domainsToRulesToCoordinateCounts)
        {
            foreach (var domainToRulesToCoordinateCountsEntry in domainsToRulesToCoordinateCounts)
            {
                var domain = domainToRulesToCoordinateCountsEntry.Key;

                var ruleCounts = ruleCountsForDomains[domain];
                var rulesToCoordinateCounts = domainToRulesToCoordinateCountsEntry.Value;

                var rulesToDelete = new HashSet<PatternRule>();

                var rulesList = new List<PatternRule>(rulesToCoordinateCounts.Keys);

                for (var i = 0; i < rulesList.Count - 1; i++)
                {
                    var rule1 = rulesList[i];
                    var entry1 = rulesToCoordinateCounts[rule1];

                    for (var j = i + 1; j < rulesList.Count; j++)
                    {
                        var rule2 = rulesList[j];
                        var entry2 = rulesToCoordinateCounts[rule2];

                        if (entry1.SequenceEqual(entry2))
                        {
                            rulesToDelete.Add(this.DetermineRuleToDelete(rule1, rule2));
                        }
                    }
                }

                foreach (var rule in rulesToDelete)
                {
                    ruleCounts.Remove(rule);
                    rulesToCoordinateCounts.Remove(rule);
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
            Dictionary<string, Dictionary<PatternRule, Dictionary<Coord, int>>> domainsToRulesToCoordinateCounts,
            string domain, 
            List<PatternRule> rules, 
            DatasetItem gtItem,
            int pruneIntervalCount,
            int pruneMinKeepThreshold)
        {
            if (
                domainsToRulesToCoordinateCounts == null 
                || string.IsNullOrWhiteSpace(domain) 
                || rules == null 
                || rules.Count == 0 
                || gtItem == null)
            {
                return;
            }

            var rulesToCoordinateCounts = this.RetrieveRulesToCoordinateCounts(domainsToRulesToCoordinateCounts, domain);

            // https://gis.stackexchange.com/questions/8650/measuring-accuracy-of-latitude-and-longitude
            var newCoordinates = new Coord()
            {
                Latitude = Math.Round(gtItem.Latitude, 2),
                Longitude = Math.Round(gtItem.Longitude, 2)
            };

            var ruleCountsForDomain = this.AddRetrieveDomainToRuleCounts(ruleCountsForDomains, domain);

            foreach (var rule in rules)
            {
                this.IncrementOccurrences(ruleCountsForDomain, rule);

                Dictionary<Coord, int> coordinateCountsForRule;

                if (!rulesToCoordinateCounts.TryGetValue(rule, out coordinateCountsForRule))
                {
                    coordinateCountsForRule = new Dictionary<Coord, int>();
                    rulesToCoordinateCounts[rule] = coordinateCountsForRule;
                }

                int currentCount;

                if (!coordinateCountsForRule.TryGetValue(newCoordinates, out currentCount))
                {
                    currentCount = 0;
                }

                coordinateCountsForRule[newCoordinates] = currentCount + 1;
            }

            var domainOcc = this.IncrementOccurrences(domainCounts, domain);

            /*
        private void PruneCounts(
            Dictionary<string, int> domainCounts,
            Dictionary<string, Dictionary<PatternRule, int>> ruleCountsForDomains,
            Dictionary<string, Dictionary<PatternRule, Dictionary<Coord, int>>> domainsToRulesToCoordinateCounts,
            string domain,
            int minKeepThreshold)
             */

            if (domainOcc % pruneIntervalCount == 0)
            {
                Console.WriteLine();
                Console.WriteLine($"Before pruning domain {domain} we had {ruleCountsForDomain.Count} rules");
                this.PruneCounts(ruleCountsForDomains, domainsToRulesToCoordinateCounts, domain, minKeepThreshold: pruneMinKeepThreshold);
                Console.WriteLine($"After pruning domain {domain} we had {ruleCountsForDomain.Count} rules");
                Console.WriteLine();
            }
        }

        public Dictionary<PatternRule, Dictionary<Coord, int>> RetrieveRulesToCoordinateCounts(
            Dictionary<string, Dictionary<PatternRule, Dictionary<Coord, int>>> domainsToRulesToCoordinateCounts, 
            string domain)
        {
            Dictionary<PatternRule, Dictionary<Coord, int>> rulesToCoordinates;

            if (!domainsToRulesToCoordinateCounts.TryGetValue(domain, out rulesToCoordinates))
            {
                rulesToCoordinates = new Dictionary<PatternRule, Dictionary<Coord, int>>();
                domainsToRulesToCoordinateCounts[domain] = rulesToCoordinates;
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
            Dictionary<string, Dictionary<PatternRule, int>> ruleCountsForDomains,
            Dictionary<string, Dictionary<PatternRule, Dictionary<Coord, int>>> domainsToRulesToCoordinateCounts,
            string domain,
            int minKeepThreshold)
        {
            var ruleCountsForDomain = ruleCountsForDomains[domain];
            var rulesToCoordinateCounts = domainsToRulesToCoordinateCounts[domain];

            var keysToDelete = new HashSet<PatternRule>();
            var occToDecrement = 0;

            foreach (var entry in ruleCountsForDomain)
            {
                var count = entry.Value;

                if (count < minKeepThreshold)
                {
                    var key = entry.Key;
                    keysToDelete.Add(key);
                    occToDecrement += count;
                }
            }

            foreach (var key in keysToDelete)
            {
                ruleCountsForDomain.Remove(key);
                rulesToCoordinateCounts.Remove(key);
            }

            if (ruleCountsForDomain.Count == 0)
            {
                this.DeleteDomain(ruleCountsForDomains, domainsToRulesToCoordinateCounts, domainToDelete: domain);
            }
        }

        private Dictionary<PatternRule, int> AddRetrieveDomainToRuleCounts(Dictionary<string, Dictionary<PatternRule, int>> subdomainPartCounts, string domain)
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
