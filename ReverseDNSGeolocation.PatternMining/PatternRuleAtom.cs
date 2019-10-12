namespace ReverseDNSGeolocation.PatternMining
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class PatternRuleAtom
    {
        public string Substring { get; set; }

        public int Index { get; set; }

        public IndexType IndexType { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                if (this.Substring != null)
                {
                    hash = hash * 23 + this.Substring.GetHashCode();
                }

                hash = hash * 23 + this.Index.GetHashCode();

                hash = hash * 23 + this.IndexType.GetHashCode();

                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            var partObj = (PatternRuleAtom)obj;

            return this.Substring == partObj.Substring
                && this.Index == partObj.Index
                && this.IndexType == partObj.IndexType;
        }

        public override string ToString()
        {
            return $"{this.Substring}|{this.IndexType}|{this.Index}";
        }
    }
}
