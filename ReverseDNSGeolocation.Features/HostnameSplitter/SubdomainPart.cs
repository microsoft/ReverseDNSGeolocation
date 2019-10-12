namespace ReverseDNSGeolocation
{
    public class SubdomainPart
    {
        public string Substring { get; set; }

        public byte RTLSlotIndex { get; set; }

        public byte LTRSlotIndex { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                if (this.Substring != null)
                {
                    hash = hash * 23 + this.Substring.GetHashCode();
                }

                hash = hash * 23 + this.RTLSlotIndex.GetHashCode();

                hash = hash * 23 + this.LTRSlotIndex.GetHashCode();

                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            var partObj = (SubdomainPart)obj;

            return this.Substring == partObj.Substring
                && this.RTLSlotIndex == partObj.RTLSlotIndex
                && this.LTRSlotIndex == partObj.LTRSlotIndex;
        }
    }
}
