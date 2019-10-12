namespace ReverseDNSGeolocation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /*
     * This class is used to validate results from HostnameSplitter.ExtractDomainWithValidSubdomain
     * That function uses Louw.PublicSuffix.DomainParser to extract domain information, but
     * if the hostname is invalid it sometimes outputs incorrect informatiom. For example for
     * input "118.41" it says that the hostname is "118.0.0.41", the subdomain is "118.0",
     * and the domain is "0.41", which is incorrect. So we first use the HostnameHasValidSuffix
     * from this class to validate that the hostname ends in a valid Suffix 
     */
    public static class PublicSuffixMatcher
    {
        private static HashSet<string> Suffixes;

        static PublicSuffixMatcher()
        {
            Suffixes = new HashSet<string>();

            using (var reader = new StreamReader(@"public_suffix_list.dat"))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.ToLowerInvariant().Trim();

                    if (line.Length > 0 && !line.StartsWith("//"))
                    {
                        if (line.StartsWith("*."))
                        {
                            if (line.Length > 2)
                            {
                                line = line.Substring(2);
                                Suffixes.Add(line);
                            }
                        }
                        else
                        {
                            Suffixes.Add(line);
                        }
                    }
                }
            }
        }

        public static bool HostnameHasValidSuffix(string hostname)
        {
            if (hostname == null)
            {
                return false;
            }

            hostname = hostname.ToLowerInvariant().Trim();

            if (hostname.Length == 0)
            {
                return false;
            }

            var parts = hostname.Split('.').ToList<string>();

            for(var i = 1; i < parts.Count; i++)
            {
                var candidate = string.Join(".", parts.GetRange(i, parts.Count - i));

                if (Suffixes.Contains(candidate))
                {
                    return true;
                }
            }

            return false;
        }
    }
}