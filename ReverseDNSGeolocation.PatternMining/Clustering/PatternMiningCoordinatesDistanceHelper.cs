namespace ReverseDNSGeolocation.PatternMining.Clustering
{
    using ReverseDNSGeolocation.Classification;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PatternMiningCoordinatesDistanceHelper : IDistanceHelper<PatternMiningCoordinates>
    {
        public double DetermineDistance(PatternMiningCoordinates item1, PatternMiningCoordinates item2)
        {
            return DistanceHelper.Distance(item1.Latitude, item1.Longitude, item2.Latitude, item2.Longitude);
        }

        // TODO: This function is not actually used!
        public PatternMiningCoordinates FindCentroid(PatternMiningCoordinates initialClusterSeed, List<PatternMiningCoordinates> clusterMembers)
        {
            // Here we can either return the initialClusterSeed, if we want to make the centroid be an actual point,
            // or we can create a synthetic centroid by combining the coordinates of all cluster members

            return this.GenerateSyntheticCentroid(clusterMembers);
        }

        private PatternMiningCoordinates GenerateSyntheticCentroid(List<PatternMiningCoordinates> members)
        {
            var latSum = 0d;
            var longSum = 0d;

            foreach (var member in members)
            {
                latSum += member.Latitude;
                longSum += member.Longitude;
            }

            return new PatternMiningCoordinates()
            {
                Latitude = Math.Round(latSum / ((1.0d) * members.Count), 2),
                Longitude = Math.Round(longSum / ((1.0d) * members.Count), 2)
            };
        }
    }
}
