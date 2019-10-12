namespace ReverseDNSGeolocation.Features
{
    public enum CityFeatureType
    {
        // Step 1 - Determine city candidates

        Unknown = 0,

        ExactCityNameMatch = 10, // Is there an exact city name match?
        ExactCityNamePopulation = 11, // What is the population of the exact city name match? Null is valid if ExactCityNameMatch value is 0 or if population information is not available
        ExactCityNameLetters = 12, // How many letters were the in the exact city name match?
        ExactCityNameRTLSlotIndex = 13, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        ExactCityNameLTRSlotIndex = 14, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)
        ExactCityNameAlternateNamesCount = 15, // If a city name matched for this city, what is the total number of alternate names this city has?

        CityAdmin1NameMatch = 20, // Is there an city + admin1 name match?
        CityAdmin1NamePopulation = 21, // What is the population of the city + admin1 name match?
        CityAdmin1LettersBoth = 22, // How many letters were the in the city + admin1 name match?
        CityAdmin1LettersCity = 23, // How many letters were the in the city name match?
        CityAdmin1LettersAdmin1 = 24, // How many letters were the in the admin1 match?
        CityAdmin1RTLSlotIndex = 25, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        CityAdmin1LTRSlotIndex = 26, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        CityCountryNameMatch = 30, // Is there an exact city + country name match?
        CityCountryNamePopulation = 31, // What is the population of the exact city + country name match? Null is valid if ExactCityCountryNameMatch value is 0 or if population information is not available
        CityCountryLettersBoth = 32,
        CityCountryLettersCity = 33,
        CityCountryLettersCountry = 34,
        CityCountryRTLSlotIndex = 35, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        CityCountryLTRSlotIndex = 36, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        NoVowelsCityNameMatch = 40,
        NoVowelsCityNamePopulation = 41,
        NoVowelsCityNameLetters = 42,
        NoVowelsCityNameLettersRatio = 43,
        NoVowelsCityRTLSlotIndex = 44, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        NoVowelsCityLTRSlotIndex = 45, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        AlternateCityNameMatch = 50, // Is there an exact city name match on an alternate name?
        AlternateCityNamePopulation = 51,
        AlternateCityNameLetters = 52,
        AlternateCityNameIsPreferredName = 53, // If this alternate name is an official/preferred name
        AlternateCityNameIsShortName = 54, // If this is a short name like 'California' for 'State of California'
        AlternateCityNameIsColloquial = 55, // If this alternate name is a colloquial or slang term
        AlternateCityNameIsHistoric = 56, // If this alternate name is historic and was used in the past
        AlternateCityNameRTLSlotIndex = 57, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        AlternateCityNameLTRSlotIndex = 58, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)
        AlternateCityNameAlternateNamesCount = 59, // If an alternate city name matched for this city, what is the total number of alternate names this city has?

        FirstLettersCityNameMatch = 60,
        FirstLettersCityNamePopulation = 61,
        FirstLettersCityNameLetters = 62,
        FirstLettersCityNameLettersRatio = 63,
        FirstLettersCityNameRTLSlotIndex = 64, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        FirstLettersCityNameLTRSlotIndex = 65, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        CityAbbreviationMatch = 70,
        CityAbbreviationPopulation = 71,
        CityAbbreviationLetters = 72,
        CityAbbreviationRTLSlotIndex = 73, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        CityAbbreviationLTRSlotIndex = 74, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        AlternateCityAbbreviationMatch = 80,
        AlternateCityAbbreviationPopulation = 81,
        AlternateCityAbbreviationLetters = 82,
        AlternateCityAbbreviationRTLSlotIndex = 83, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        AlternateCityAbbreviationLTRSlotIndex = 84, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        AirportCodeMatch = 90,
        AirportCodeCityPopulation = 91,
        AirportCodeLetters = 92,
        AirportCodeRTLSlotIndex = 93, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        AirportCodeLTRSlotIndex = 94, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        ExactCLLICodeMatch = 100, // Is there an exact CLLI code match?
        ExactCLLICodePopulation = 101, // What is the population of the exact CLLI code match? Null is valid if ExactCLLICodeMatch value is 0 or if population information is not available
        ExactCLLICodeRTLSlotIndex = 102, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        ExactCLLICodeLTRSlotIndex = 103, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        HostnamePatternMatch = 110, // Does the hostname contain precomputed pattern and is the geohash of the pattern close enough to the location candidate? (Rule example: for frontier.net, if both "evrt" and "wa" are present as atoms in the hostname subdomain, that is located in a certain geohash location close to Everett, Washington)
        HostnamePatternConfidence = 111,

        ExactUNLOCODECodeMatch = 120, // Is there an exact UNLOCODE code match?
        ExactUNLOCODECodePopulation = 121, // What is the population of the exact UNLOCODE code match? Null is valid if ExactUNLOCODECodeMatch value is 0 or if population information is not available
        ExactUNLOCODECodeRTLSlotIndex = 122, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        ExactUNLOCODECodeLTRSlotIndex = 123, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        // Step 2 - Add-on features after we computed the candidates

        TLDMatch = 180,

        ExactAdmin1NameMatch = 190,
        ExactAdmin1Letters = 191,
        ExactAdmin1RTLSlotIndex = 192, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        ExactAdmin1LTRSlotIndex = 193, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        ExactCountryNameMatch = 200,
        ExactCountryLetters = 201,
        ExactCountryRTLSlotIndex = 202, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        ExactCountryLTRSlotIndex = 203, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        FirstLettersAdmin1NameMatch = 210,
        FirstLettersAdmin1Letters = 211,
        FirstLettersAdmin1LettersRatio = 212,
        FirstLettersAdmin1RTLSlotIndex = 213, // Right to left slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 2 (0-indexed)
        FirstLettersAdmin1LTRSlotIndex = 214, // Left to right slot index. Example: If string is XXX and hostname is ZZZ.XXX.DDD.RRR, then this value would be 1 (0-indexed)

        Domain = 220 // The actual domain name from the hostname
    }
}
