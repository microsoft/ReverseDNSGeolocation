namespace ReverseDNSGeolocation.Classification
{
    using System.Collections.Generic;

    public class LRUCache<T>
    {
        private Dictionary<T, int> counts;

        private int maxItems;

        public LRUCache(int maxItems)
        {
            this.counts = new Dictionary<T, int>();
            this.maxItems = maxItems;
        }

        public bool ContainsWithIncrement(T item)
        {
            var count = 0;

            if (!this.counts.TryGetValue(item, out count))
            {
                if (this.counts.Count >= this.maxItems)
                {
                    this.EvictSmallest();
                }

                this.counts[item] = count + 1;
                return false;
            }
            else
            {
                this.counts[item] = count + 1;
                return true;
            }
        }

        public bool Contains(T item)
        {
            return this.counts.ContainsKey(item);
        }

        public void Increment(T item)
        {
            var count = 0;

            if (!this.counts.TryGetValue(item, out count))
            {
                if (this.counts.Count >= this.maxItems)
                {
                    this.EvictSmallest();
                }
            }

            this.counts[item] = count + 1;
        }

        private void EvictSmallest()
        {
            if (this.counts.Count > 0)
            {
                T smallestItem = default(T);
                int smallestCount = int.MaxValue; ;

                foreach (var item in this.counts)
                {
                    var localItem = item.Key;
                    var localCount = item.Value;

                    if (localCount < smallestCount)
                    {
                        smallestCount = localCount;
                        smallestItem = localItem;
                    }
                }

                this.counts.Remove(smallestItem);
            }
        }
    }
}
