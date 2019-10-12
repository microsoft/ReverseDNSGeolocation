namespace ReverseDNSGeolocation
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using Louw.PublicSuffix;

    public static class HostnameSplitter
    {
        private static HashSet<string> Blacklist;

        private static DomainParser DomainParser;

        private static IdnMapping idnMapping;

        static HostnameSplitter()
        {
            idnMapping = new IdnMapping();

            Blacklist = new HashSet<string>()
            {
                "adsl",
                "agg",
                "anycast",
                "bband",
                "broad",
                "broadband",
                "cable",
                "cablemodem",
                "calix",
                "catv",
                "central",
                "cisco",
                "client",
                "clients",
                "core",
                "cpc",
                "cpe",
                "cust",
                "customer",
                "dedicated",
                "dhcp",
                "dial",
                "dialup",
                "dns",
                "dsl",
                "dyn",
                "dynamic",
                "dynamicip",
                "edge",
                "fbx",
                "fiber",
                "fios",
                "ftth",
                "ge",
                "global",
                "host",
                "hostname",
                "internet",
                "imap",
                "ip",
                "ipv4",
                "ipv6",
                "juniper",
                "lan",
                "local",
                "localhost",
                "loop",
                "loopback",
                "mail",
                "mesh",
                "metro",
                "metropolitan",
                "modem",
                "modemcable",
                "mtr",
                "nat",
                "net",
                "network",
                "ns",
                "ntwrk",
                "pool",
                "pools",
                "pptp",
                "ppp",
                "pppoe",
                "public",
                "res",
                "residential",
                "reverse",
                "roaming",
                "rover",
                "satelite",
                "satellite",
                "smtp",
                "static",
                "umts",
                "unnamed",
                "unknown",
                "user",
                "users",
                "virtual",
                "vpn",
                "web",
                "website",
                "websites",
                "wireless",
                "wlan",

                "bras",
                "br",
                "bridge",
                "ddr",
                "drr",
                "adr"
            };

            DomainParser = new DomainParser(new FileTldRuleProvider("public_suffix_list.dat"));
        }

        public static string TryDecodeIDN(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return null;
            }

            if (!IsAscii(hostname))
            {
                return hostname;
            }

            try
            {
                return idnMapping.GetUnicode(hostname);
            }
            catch (Exception)
            {
                return hostname;
            }
        }

        public static IEnumerable<string> StringToCodePoints(string inputString)
        {
            for (int i = 0; i < inputString.Length; ++i)
            {
                yield return char.ConvertFromUtf32(char.ConvertToUtf32(inputString, i));

                if (char.IsHighSurrogate(inputString, i))
                {
                    i++;
                }
            }
        }

        public static bool IsAscii(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            foreach (var symbol in StringToCodePoints(text))
            {
                if (symbol.Length > 1)
                {
                    return false;
                }
                else if (symbol.Length == 1 && symbol[0] > 0x007f)
                {
                    // 0x007f is the last ASCII character
                    return false;
                }
            }

            return true;
        }

        public static HostnameSplitterResult Split(string hostname, bool useBlacklist = true)
        {
            hostname = TryDecodeIDN(hostname);

            if (string.IsNullOrEmpty(hostname))
            {
                return null;
            }

            var parseHostnameTask = DomainParser.ParseAsync(hostname);

            try
            {
                parseHostnameTask.Wait();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "Could not parse hostname: {0} in HostnameSplitter with Exception: {1}", hostname, ex));
                return null;
            }

            var domainInfo = parseHostnameTask.Result;

            if (domainInfo == null)
            {
                return null;
            }

            var parts = new HashSet<SubdomainPart>();
            var firstLastLetters = new HashSet<SubdomainPart>();

            if (domainInfo.SubDomain == null)
            {
                return null;
            }

            var lowerSubdomain = domainInfo.SubDomain.ToLowerInvariant();
            var dotParts = lowerSubdomain.Split(new char[] { '.' });

            for (var i = dotParts.Length - 1; i >= 0; i--)
            {
                var dotPart = dotParts[i];

                if (dotPart.Length > 0)
                {
                    var closestDotRTL = (byte)(dotParts.Length - i - 1);
                    var closestDotLTR = (byte)i;

                    var dashParts = dotPart.Split(new char[] { '-' });

                    foreach (var dashPart in dashParts)
                    {
                        var cleanedPart = new StringBuilder();

                        foreach (var c in dashPart)
                        {
                            if (char.IsLetter(c))
                            {
                                cleanedPart.Append(c);
                            }
                            else if (cleanedPart.Length > 0)
                            {
                                var cleanedPartStr = cleanedPart.ToString();

                                if (useBlacklist == false || !Blacklist.Contains(cleanedPartStr))
                                {
                                    parts.Add(new SubdomainPart()
                                    {
                                        Substring = cleanedPartStr,
                                        RTLSlotIndex = closestDotRTL,
                                        LTRSlotIndex = closestDotLTR
                                    });
                                }

                                cleanedPart = new StringBuilder();
                            }
                        }

                        if (cleanedPart.Length > 0)
                        {
                            var cleanedPartStr = cleanedPart.ToString();

                            if (useBlacklist == false || !Blacklist.Contains(cleanedPartStr))
                            {
                                parts.Add(new SubdomainPart()
                                {
                                    Substring = cleanedPartStr,
                                    RTLSlotIndex = closestDotRTL,
                                    LTRSlotIndex = closestDotLTR
                                });
                            }
                        }

                        foreach (var ngram in GenerateFirstLastLetterNGrams(dashPart, useBlacklist))
                        {
                            firstLastLetters.Add(new SubdomainPart()
                            {
                                Substring = ngram,
                                RTLSlotIndex = closestDotRTL,
                                LTRSlotIndex = closestDotLTR
                            });
                        }
                    }

                    if (
                        !ContainsOnlyNumbersOrEmpty(dotPart)
                        && (useBlacklist == false || !Blacklist.Contains(dotPart)))
                    {
                        parts.Add(new SubdomainPart()
                        {
                            Substring = dotPart,
                            RTLSlotIndex = closestDotRTL,
                            LTRSlotIndex = closestDotLTR
                        });
                    }
                }
            }

            if (parts.Count > 0)
            {
                var substringsSet = new HashSet<string>();

                foreach (var part in parts)
                {
                    substringsSet.Add(part.Substring);
                }

                var firstLastLettersSet = new HashSet<string>();

                foreach (var ngram in firstLastLetters)
                {
                    firstLastLettersSet.Add(ngram.Substring);
                }

                return new HostnameSplitterResult()
                {
                    DomainInfo = domainInfo,
                    SubdomainParts = parts,
                    SubstringsSet = substringsSet,
                    FirstLastLetters = firstLastLetters,
                    FirstLastLettersSet = firstLastLettersSet,
                    TLD = domainInfo.Tld
                };
            }

            return null;

            /*
            var rawParts = lowerSubdomain.Split(new char[] { '.', '-', ' ' });

            foreach (var rawPart in rawParts)
            {
                var cleanedPart = new StringBuilder();

                foreach (var c in rawPart)
                {
                    if (char.IsLetter(c))
                    {
                        cleanedPart.Append(c);
                    }
                    else if (cleanedPart.Length > 0)
                    {
                        var cleanedPartStr = cleanedPart.ToString();

                        if (useBlacklist == false || !Blacklist.Contains(cleanedPartStr))
                        {
                            parts.Add(cleanedPartStr);
                        }

                        cleanedPart = new StringBuilder();
                    }
                }

                if (cleanedPart.Length > 0)
                {
                    var cleanedPartStr = cleanedPart.ToString();

                    if (useBlacklist == false || !Blacklist.Contains(cleanedPartStr))
                    {
                        parts.Add(cleanedPartStr);
                    }
                }
            }

            var dotParts = lowerSubdomain.Split(new char[] { '.', ' ' });

            foreach (var dotPart in dotParts)
            {
                if (dotPart.Length > 0)
                {
                    if (ContainsOnlyNumbersOrEmpty(dotPart))
                    {
                        continue;
                    }
                    else
                    {
                        parts.Add(dotPart);
                    }

                    var dashParts = dotPart.Split(new char[] { '-' }).ToList<string>();

                    if (dashParts.Count > 0)
                    {
                        firstLastLetters.UnionWith(GenerateFirstLastLetterNGrams(dashParts, useBlacklist));
                        parts.UnionWith(GenerateDashNgrams(dashParts, ngramSize: 1, useBlacklist: useBlacklist));
                        parts.UnionWith(GenerateDashNgrams(dashParts, ngramSize: 2, useBlacklist: useBlacklist));
                        parts.UnionWith(GenerateDashNgrams(dashParts, ngramSize: 3, useBlacklist: useBlacklist));
                    }
                }
            }

            if (parts.Count > 0)
            {
                return new HostnameSplitterResult()
                {
                    DomainInfo = domainInfo,
                    SubdomainParts = parts,
                    FirstLastLetters = firstLastLetters,
                    TLD = domainInfo.Tld
                };
            }

            return null;
            */
        }

        public static string ExtractDomainWithValidSubdomain(string hostname)
        {
            hostname = TryDecodeIDN(hostname);

            if (hostname == null)
            {
                return null;
            }

            var domainInfo = ExtractDomainInfo(hostname);

            if (domainInfo == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(domainInfo.SubDomain))
            {
                return null;
            }

            return domainInfo.RegistrableDomain.ToLowerInvariant();
        }

        public static string ExtractDomain(string hostname)
        {
            hostname = TryDecodeIDN(hostname);

            if (hostname == null)
            {
                return null;
            }

            var domainInfo = ExtractDomainInfo(hostname);

            if (domainInfo == null)
            {
                return null;
            }

            return domainInfo.RegistrableDomain;
        }

        public static DomainInfo ExtractDomainInfo(string hostname)
        {
            hostname = TryDecodeIDN(hostname);

            if (string.IsNullOrEmpty(hostname))
            {
                return null;
            }

            var parseHostnameTask = DomainParser.ParseAsync(hostname);

            try
            {
                parseHostnameTask.Wait();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "Could not parse hostname: {0} in HostnameSplitter with Exception: {1}", hostname, ex));
                return null;
            }

            return parseHostnameTask.Result;
        }

        private static HashSet<string> GenerateFirstLastLetterNGrams(List<string> dashParts, bool useBlacklist)
        {
            var ngrams = new HashSet<string>();

            foreach (var dashPart in dashParts)
            {
                ngrams.UnionWith(GenerateFirstLastLetterNGrams(dashPart, useBlacklist));
            }

            return ngrams;
        }

        private static HashSet<string> GenerateFirstLastLetterNGrams(string dashPart, bool useBlacklist)
        {
            var ngrams = new HashSet<string>();

            // UNLOCODE
            var first5 = FirstNLetters(dashPart, 5, useBlacklist);

            if (!string.IsNullOrEmpty(first5))
            {
                ngrams.Add(first5);
            }

            // CLLI
            var first6 = FirstNLetters(dashPart, 6, useBlacklist);

            if (!string.IsNullOrEmpty(first6))
            {
                ngrams.Add(first6);
            }

            // UNLOCODE
            var last5 = LastNLetters(dashPart, 5, useBlacklist);

            if (!string.IsNullOrEmpty(last5))
            {
                ngrams.Add(last5);
            }

            // CLLI
            var last6 = LastNLetters(dashPart, 6, useBlacklist);

            if (!string.IsNullOrEmpty(last6))
            {
                ngrams.Add(last6);
            }

            return ngrams;
        }

        private static string FirstNLetters(string part, int numberOfLetters, bool useBlacklist)
        {
            if (part == null || part.Length < numberOfLetters)
            {
                return null;
            }

            var ngram = part.Substring(0, numberOfLetters);

            if (useBlacklist == false || !Blacklist.Contains(ngram))
            {
                return ngram;
            }

            return null;
        }

        private static string LastNLetters(string part, int numberOfLetters, bool useBlacklist)
        {
            if (part == null || part.Length < numberOfLetters)
            {
                return null;
            }

            var ngram = part.Substring(part.Length - numberOfLetters);

            if (useBlacklist == false || !Blacklist.Contains(ngram))
            {
                return ngram;
            }

            return null;
        }

        private static HashSet<string> GenerateDashNgrams(List<string> dashParts, int ngramSize, bool useBlacklist)
        {
            var ngrams = new HashSet<string>();

            if (dashParts != null && dashParts.Count >= ngramSize)
            {
                for (var i = 0; i < dashParts.Count - ngramSize + 1; i++)
                {
                    var subList = dashParts.GetRange(i, ngramSize);

                    if (SublistValid(subList))
                    {
                        var ngram = string.Join("-", subList);

                        if (useBlacklist == false || !Blacklist.Contains(ngram))
                        {
                            ngrams.Add(ngram);
                        }
                    }
                }
            }

            return ngrams;
        }

        private static bool SublistValid(List<string> subList)
        {
            foreach (var item in subList)
            {
                if (ContainsOnlyNumbersOrEmpty(item))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsOnlyNumbersOrEmpty(string item)
        {
            foreach (var c in item)
            {
                if(!char.IsNumber(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
