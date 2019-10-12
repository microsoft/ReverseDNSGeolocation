namespace ReverseDNSGeolocation.PatternMining
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public static class PatternRulesSerializer
    {
        public static Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> DeserializeReducedRules(string inputPath)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ////return (Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>>)formatter.Deserialize(stream);

                var reducedRules = new Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>>();

                while (stream.Position != stream.Length)
                {
                    var tuple = (Tuple<string, Dictionary<PatternRule, PatternMiningCoordinates>>)formatter.Deserialize(stream);
                    reducedRules[tuple.Item1] = tuple.Item2;
                }

                return reducedRules;
            }
        }

        public static void SerializeReducedRules(Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>> reducedRules, string outputPath)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                ////formatter.Serialize(stream, reducedRules);

                foreach (var entry in reducedRules)
                {
                    var tuple = new Tuple<string, Dictionary<PatternRule, PatternMiningCoordinates>>(entry.Key, entry.Value);
                    formatter.Serialize(stream, tuple);
                }
            }
        }
    }
}
