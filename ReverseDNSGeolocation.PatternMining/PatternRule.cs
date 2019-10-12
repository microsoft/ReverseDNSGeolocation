namespace ReverseDNSGeolocation.PatternMining
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class PatternRule
    {
        public List<PatternRuleAtom> Atoms { get; set; }

        public override int GetHashCode()
        {
            var hash = this.Atoms?.GetSequenceHashCode();
            return hash ?? 0;
        }

        public override bool Equals(object obj)
        {
            var partObj = (PatternRule)obj;

            return this.Atoms.SequenceEqual(partObj.Atoms);
        }

        public override string ToString()
        {
            return string.Join(", ", this.Atoms);
        }
    }
}
