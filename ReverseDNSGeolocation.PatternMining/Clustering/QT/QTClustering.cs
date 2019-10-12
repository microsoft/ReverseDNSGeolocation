namespace ReverseDNSGeolocation.PatternMining.Clustering.QT
{
    using Clustering;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class QTClustering<T> where T : class
    {
        private IDistanceHelper<T> distanceHelper;

        private double clusterDiameter;

        private HashSet<T> itemsSet;

        public QTClustering(
            IDistanceHelper<T> distanceHelper,
            double clusterDiameter,
            HashSet<T> itemsSet)
        {
            this.distanceHelper = distanceHelper;
            this.clusterDiameter = clusterDiameter;
            this.itemsSet = itemsSet;
        }

        public Cluster<T> NextCluster()
        {
            int largestClusterCount = int.MinValue;
            List<T> largestCluster = null;
            T largestClusterCentroid = null;

            var itemsList = new List<T>(itemsSet);

            for (var i = 0; i < itemsList.Count - 1; i++)
            {
                var centroid = itemsList[i];

                var clusterForCentroid = new List<T>();

                for (var j = i + 1; j < itemsList.Count - 1; j++)
                {
                    var item = itemsList[j];

                    var distance = this.distanceHelper.DetermineDistance(centroid, item);

                    if (distance <= this.clusterDiameter)
                    {
                        clusterForCentroid.Add(item);
                    }
                }

                if (clusterForCentroid.Count > 0)
                {
                    if (largestClusterCount < clusterForCentroid.Count)
                    {
                        largestCluster = clusterForCentroid;
                        largestClusterCentroid = centroid;
                        largestClusterCount = clusterForCentroid.Count;
                    }
                }
            }

            if (largestCluster == null)
            {
                return null;
            }

            this.itemsSet.ExceptWith(largestCluster);

            return new Cluster<T>()
            {
                Members = largestCluster,
                Centroid = this.distanceHelper.FindCentroid(largestClusterCentroid, largestCluster)
            };
        }
    }
}
