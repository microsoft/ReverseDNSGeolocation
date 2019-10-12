namespace ReverseDNSGeolocation.PatternMining.Clustering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Cluster<T> where T : class
    {
        public List<T> Members { get; set; }

        public T Centroid { get; set; }
    }
}
