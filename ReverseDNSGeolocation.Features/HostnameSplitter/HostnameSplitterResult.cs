namespace ReverseDNSGeolocation
{
    using System.Collections.Generic;
    using Louw.PublicSuffix;

    public class HostnameSplitterResult
    {
        public DomainInfo DomainInfo { get; set; }

        public HashSet<SubdomainPart> SubdomainParts { get; set; }

        public HashSet<string> SubstringsSet;

        public HashSet<SubdomainPart> FirstLastLetters { get; set; }

        public HashSet<string> FirstLastLettersSet { get; set; }

        public string TLD { get; set; }
    }
}
