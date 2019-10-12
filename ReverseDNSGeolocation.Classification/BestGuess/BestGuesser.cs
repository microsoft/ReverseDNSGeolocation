namespace ReverseDNSGeolocation.Classification.BestGuess
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class BestGuesser : IBestGuesser
    {
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
    }
}
