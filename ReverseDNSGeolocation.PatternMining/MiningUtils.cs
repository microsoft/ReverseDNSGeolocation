namespace ReverseDNSGeolocation.PatternMining
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class MiningUtils
    {
        public static int GetSequenceHashCode<T>(this IList<T> sequence)
        {
            const int seed = 487;
            const int modifier = 31;

            unchecked
            {
                return sequence.Aggregate(seed, (current, item) => (current * modifier) + item.GetHashCode());
            }
        }
    }
}
