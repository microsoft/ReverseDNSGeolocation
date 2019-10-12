using System.Collections.Generic;

namespace ReverseDNSGeolocation.PatternMining.Clustering
{
    public interface IDistanceHelper<T> where T : class
    {
        double DetermineDistance(T item1, T item2);

        T FindCentroid(T initialClusterSeed, List<T> clusterMembers);
    }
}
