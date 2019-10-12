namespace ReverseDNSGeolocation.PatternMining.Clustering
{
    using ReverseDNSGeolocation.Classification;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class CoordWithOccDistanceHelper : IDistanceHelper<CoordWithOcc>
    {
        public double DetermineDistance(CoordWithOcc item1, CoordWithOcc item2)
        {
            return DistanceHelper.Distance(item1.Coord.Latitude, item1.Coord.Longitude, item2.Coord.Latitude, item2.Coord.Longitude, DistanceUnit.Kilometer);
        }

        public CoordWithOcc FindCentroid(CoordWithOcc initialClusterSeed, List<CoordWithOcc> clusterMembers)
        {
            // Here we can either return the initialClusterSeed, if we want to make the centroid be an actual point,
            // or we can create a synthetic centroid by combining the coordinates of all cluster members

            return this.GenerateSyntheticCentroid(clusterMembers);
        }

        private CoordWithOcc GenerateSyntheticCentroid(List<CoordWithOcc> members)
        {
            var latSum = 0d;
            var longSum = 0d;

            foreach (var member in members)
            {
                latSum += member.Coord.Latitude;
                longSum += member.Coord.Longitude;
            }

            return new CoordWithOcc(
                new Coord()
                {
                    Latitude = Math.Round(latSum / ((1.0d) * members.Count), 2),
                    Longitude = Math.Round(longSum / ((1.0d) * members.Count), 2)
                },
                occ: 0);
        }
    }
}
