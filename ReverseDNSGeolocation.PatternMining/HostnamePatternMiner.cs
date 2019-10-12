namespace ReverseDNSGeolocation.PatternMining
{
    using Accord.MachineLearning.Rules;
    using Classification.DatasetParsers;
    using NGeoHash.Portable;
    using ReverseDNSGeolocation;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class HostnamePatternMiner
    {
        public PatternMiningResult MineCommonStringGeohashesFromGT(
            GroundTruthParser datasetParser, 
            string inPath,
            int pruneIntervalCount = 10000,
            int pruneMinKeepThreshold = 10)
        {
            /*
             * Conceptual example for: static-32-213-114-101.wlfr.ct.frontiernet.net
             * key: frontiernet.net
             * value:
             *      key: wlfr|rtl1   (it means the string "wlfr, located at right-to-left index 1")
             *      value:
             *          key: drkh7   (geohash with precision 5 -> +/- 2.4 km)
             *          value: 100   (we found it 100 times in the dataset for this key)
             *          
             */

            // Keys example:             frontiernet.net       wlfr|rtl1           drkh7        15
            var rulesGeohashCounts = new Dictionary<string, Dictionary<PatternRule, Dictionary<string, int>>>();

            // Keys example:                     frontiernet.net         wlfr|rtl1   79
            var rulesCounts = new Dictionary<string, Dictionary<PatternRule, int>>();

            // Example:               frontiernet.net  435463
            var domainCounts = new Dictionary<string, int>();

            var processCount = 0;

            foreach (var item in datasetParser.Parse(inPath, populateTextualLocationInfo: true))
            {
                var hostname = item.Hostname;
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

                var rulesToGeohashCounts = this.AddRetrieveDomainToRulesGeohashCounts(rulesGeohashCounts, domain);
                var rulesToCounts = this.AddRetrieveDomainToRulesCounts(rulesCounts, domain);

                /*
                 * Geohash Precision:
                    #   km
                    1   ±2500
                    2   ±630
                    3   ±78
                    4   ±20
                    5   ±2.4
                    6   ±0.61
                    7   ±0.076
                    8   ±0.019
                */

                var geohashes = new HashSet<string>();
                //geohashes.Add(GeoHash.Encode(item.Latitude, item.Longitude, numberOfChars: 2)); // 2 = ±630km
                //geohashes.Add(GeoHash.Encode(item.Latitude, item.Longitude, numberOfChars: 3)); // 3 = ±78km
                geohashes.Add(GeoHash.Encode(item.Latitude, item.Longitude, numberOfChars: 4)); // 4 = ±20 km

                var ruleAtoms = this.CreateRuleAtoms(subdomainParts);
                var rules = this.GeneratePossibleRules(ruleAtoms);

                this.IncrementGeohashCounts(rulesToGeohashCounts, rulesToCounts, geohashes, rules);

                var domainOcc = this.IncrementOccurrences(domainCounts, domain);

                if (domainOcc % pruneIntervalCount == 0)
                {
                    this.PruneCounts(rulesToGeohashCounts, rulesToCounts, minKeepThreshold: pruneMinKeepThreshold);
                }
            }

            return new PatternMiningResult()
            {
                RulesGeohashCounts = rulesGeohashCounts,
                RulesCounts = rulesCounts
            };
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

        public Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> ReduceRules(PatternMiningResult results)
        {
            var reducedRules = new Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>>();

            foreach (var domainToRulesEntry in results.RulesGeohashCounts)
            {
                var domain = domainToRulesEntry.Key;
                var ruleCountsForDomain = results.RulesCounts[domain];
                var rulesToGeohashCountsForDomain = domainToRulesEntry.Value;

                Dictionary<PatternRule, PatternMiningCoordinates> rulesForDomain;

                if (!reducedRules.TryGetValue(domain, out rulesForDomain))
                {
                    rulesForDomain = new Dictionary<PatternRule, PatternMiningCoordinates>();
                    reducedRules[domain] = rulesForDomain;
                }

                foreach (var ruleEntry in rulesToGeohashCountsForDomain)
                {
                    var rule = ruleEntry.Key;
                    var ruleTotalCount = ruleCountsForDomain[rule];
                    var geohashCounts = ruleEntry.Value;

                    rulesForDomain[rule] = FindBestCoordinates(geohashCounts, ruleTotalCount);
                }
            }

            return reducedRules;
        }

        // TODO: Implement the more sophisticated method
        // TODO: Move coordinates class and add accuracy field?
        private PatternMiningCoordinates FindBestCoordinates(Dictionary<string, int> geohashCounts, int ruleTotalCount)
        {
            string bestGeohash = null;
            double highestFraction = -1;

            foreach (var entry in geohashCounts)
            {
                var geohash = entry.Key;
                var count = entry.Value;

                var occFraction = count / (1.0d * ruleTotalCount);

                if (occFraction > highestFraction)
                {
                    bestGeohash = geohash;
                    highestFraction = occFraction;
                }
            }

            var result = GeoHash.Decode(bestGeohash);

            return new PatternMiningCoordinates()
            {
                Latitude = result.Coordinates.Lat,
                Longitude = result.Coordinates.Lon,
                Confidence = highestFraction
            };
        }

        public PatternMiningResult FilterRules(
            PatternMiningResult results,
            int minimumRuleSupport = 500,
            double minimimRuleOccFraction = 0.7)
        {
            var filteredRulesGeohashCounts = new Dictionary<string, Dictionary<PatternRule, Dictionary<string, int>>>();
            var filteredRulesCounts = new Dictionary<string, Dictionary<PatternRule, int>>();

            foreach (var domainToCounts in results.RulesGeohashCounts)
            {
                var domain = domainToCounts.Key;
                var domainRulesToGeohashCounts = domainToCounts.Value;
                var domainRulesCounts = results.RulesCounts[domain];

                var filteredRulesToGeohashCounts = this.AddRetrieveDomainToRulesGeohashCounts(filteredRulesGeohashCounts, domain);
                var filteredRulesToCounts = this.AddRetrieveDomainToRulesCounts(filteredRulesCounts, domain);

                foreach (var ruleToGeohashCounts in domainRulesToGeohashCounts)
                {
                    var rule = ruleToGeohashCounts.Key;
                    var geohashCounts = ruleToGeohashCounts.Value;

                    var ruleTotalCount = domainRulesCounts[rule];

                    foreach (var geohashCount in geohashCounts)
                    {
                        var geohash = geohashCount.Key;
                        var count = geohashCount.Value;

                        var occFraction = count / (1.0d * ruleTotalCount);

                        if (count >= minimumRuleSupport && occFraction >= minimimRuleOccFraction)
                        {
                            //Console.WriteLine($"{domain}\t{rule}\t{geohash}\t{count} / {ruleTotalCount} =\t{occFraction}");

                            filteredRulesToCounts[rule] = ruleTotalCount;

                            Dictionary<string, int> filteredGeohashCounts;

                            if (!filteredRulesToGeohashCounts.TryGetValue(rule, out filteredGeohashCounts))
                            {
                                filteredGeohashCounts = new Dictionary<string, int>();
                                filteredRulesToGeohashCounts[rule] = filteredGeohashCounts;
                            }

                            filteredGeohashCounts[geohash] = count;
                        }
                    }
                }
            }

            return new PatternMiningResult()
            {
                RulesGeohashCounts = filteredRulesGeohashCounts,
                RulesCounts = filteredRulesCounts
            };
        }

        private void PruneCounts(
            Dictionary<PatternRule, Dictionary<string, int>> rulesToGeohashCounts,
            Dictionary<PatternRule, int> rulesToCounts,
            int minKeepThreshold)
        {
            var keysToDelete = new HashSet<PatternRule>();

            foreach (var entry in rulesToCounts)
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
                rulesToGeohashCounts.Remove(key);
                rulesToCounts.Remove(key);
            }
        }

        private Dictionary<PatternRule, Dictionary<string, int>> AddRetrieveDomainToRulesGeohashCounts(
            Dictionary<string, Dictionary<PatternRule, Dictionary<string, int>>> rulesGeohashCounts, 
            string domain)
        {
            Dictionary<PatternRule, Dictionary<string, int>> subdomainPartsToGeohashCounts;

            if (!rulesGeohashCounts.TryGetValue(domain, out subdomainPartsToGeohashCounts))
            {
                subdomainPartsToGeohashCounts = new Dictionary<PatternRule, Dictionary<string, int>>();
                rulesGeohashCounts[domain] = subdomainPartsToGeohashCounts;
            }

            return subdomainPartsToGeohashCounts;
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

        private void IncrementGeohashCounts(
            Dictionary<PatternRule, Dictionary<string, int>> rulesToGeohashCounts,
            Dictionary<PatternRule, int> rulesToCounts,
            HashSet<string> geohashes,
            List<PatternRule> rules)
        {
            foreach (var rule in rules)
            {
                this.IncrementOccurrences(rulesToCounts, rule);

                Dictionary<string, int> geohashCounts;

                if (!rulesToGeohashCounts.TryGetValue(rule, out geohashCounts))
                {
                    geohashCounts = new Dictionary<string, int>();
                    rulesToGeohashCounts[rule] = geohashCounts;
                }

                foreach (var geohash in geohashes)
                {
                    this.IncrementOccurrences(geohashCounts, geohash);
                }
            }
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

        /*
        private void IncrementGeohashOccurrences(
            Dictionary<string, int> occurrences,
            string key)
        {
            if (occurrences == null || key == null)
            {
                return;
            }

            int currentOcc;

            if (!occurrences.TryGetValue(key, out currentOcc))
            {
                currentOcc = 0;
            }

            currentOcc++;

            occurrences[key] = currentOcc;
        }
        */
    }
}
