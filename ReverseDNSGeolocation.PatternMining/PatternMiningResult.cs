namespace ReverseDNSGeolocation.PatternMining
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PatternMiningResult
    {
        public Dictionary<string, Dictionary<PatternRule, Dictionary<string, int>>> RulesGeohashCounts { get; set; }

        public Dictionary<string, Dictionary<PatternRule, int>> RulesCounts { get; set; }
    }
}
