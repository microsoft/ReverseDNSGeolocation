namespace ReverseDNSGeolocation.Classification.BestGuess
{
    using ReverseDNSGeolocation.Classification.Models;
    using System.Collections.Generic;

    public interface IBestGuesser
    {
        ClassificationResult PickBest(string hostname, List<ClassificationResult> results, double minProbability = 0);
    }
}
