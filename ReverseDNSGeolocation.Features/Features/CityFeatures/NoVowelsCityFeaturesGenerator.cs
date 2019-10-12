namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    [Serializable]
    public class NoVowelsCityFeaturesGenerator : CityFeaturesGenerator
    {
        private const int MinLetters = 3;
        private const int MaxLetters = 6;
        private const int MaxLettersToUseComplexAlgo = 15;

        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        private Dictionary<string, EntitiesToFeatures> variationsToEntitiesToFeatures = new Dictionary<string, EntitiesToFeatures>();

        public NoVowelsCityFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            if (this.FeaturesConfig.NullDefaultsAllowed)
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.NoVowelsCityNameMatch, false },
                    { CityFeatureType.NoVowelsCityNamePopulation, (uint?)null },
                    { CityFeatureType.NoVowelsCityNameLetters, (byte?)null },
                    { CityFeatureType.NoVowelsCityNameLettersRatio, (float?)null }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.NoVowelsCityRTLSlotIndex] = (byte?)null;
                    FeatureDefaults[CityFeatureType.NoVowelsCityLTRSlotIndex] = (byte?)null;
                }
            }
            else
            {
                FeatureDefaults = new Features()
                {
                    { CityFeatureType.NoVowelsCityNameMatch, false },
                    { CityFeatureType.NoVowelsCityNamePopulation, (uint?)0 },
                    { CityFeatureType.NoVowelsCityNameLetters, (byte?)0 },
                    { CityFeatureType.NoVowelsCityNameLettersRatio, (float?)0 }
                };

                if (this.FeaturesConfig.UseSlotIndex)
                {
                    FeatureDefaults[CityFeatureType.NoVowelsCityRTLSlotIndex] = (byte?)byte.MaxValue;
                    FeatureDefaults[CityFeatureType.NoVowelsCityLTRSlotIndex] = (byte?)byte.MaxValue;
                }
            }

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.NoVowelsCityNameMatch, typeof(bool) },
                { CityFeatureType.NoVowelsCityNamePopulation, typeof(uint?) },
                { CityFeatureType.NoVowelsCityNameLetters, typeof(byte?) },
                { CityFeatureType.NoVowelsCityNameLettersRatio, typeof(float?) }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureDefaultsValueTypes[CityFeatureType.NoVowelsCityRTLSlotIndex] = typeof(byte?);
                FeatureDefaultsValueTypes[CityFeatureType.NoVowelsCityLTRSlotIndex] = typeof(byte?);
            }

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.NoVowelsCityNameMatch, FeatureGranularity.Discrete },
                { CityFeatureType.NoVowelsCityNamePopulation, FeatureGranularity.Continuous },
                { CityFeatureType.NoVowelsCityNameLetters, FeatureGranularity.Continuous },
                { CityFeatureType.NoVowelsCityNameLettersRatio, FeatureGranularity.Continuous }
            };

            if (this.FeaturesConfig.UseSlotIndex)
            {
                FeatureGranularities[CityFeatureType.NoVowelsCityRTLSlotIndex] = FeatureGranularity.Discrete;
                FeatureGranularities[CityFeatureType.NoVowelsCityLTRSlotIndex] = FeatureGranularity.Discrete;
            }
        }

        public override void IngestCityEntity(GeonamesCityEntity entity)
        {
            var nameVariations = this.GenerateSimpleVariationsForName(entity.Name);
            var asciiNameVariations = this.GenerateSimpleVariationsForName(entity.AsciiName);
            nameVariations.UnionWith(asciiNameVariations);

            if (this.FeaturesConfig.UseComplexNoVowelsFeature)
            {
                var complexNameVariations = this.GenerateComplexVariationsForName(entity.Name, minLetters: MinLetters);
                nameVariations.UnionWith(complexNameVariations);

                var complexAsciiNameVariations = this.GenerateComplexVariationsForName(entity.AsciiName, minLetters: MinLetters);
                nameVariations.UnionWith(complexAsciiNameVariations);
            }

            foreach (var nameVariation in nameVariations)
            {
                var features = this.InitializeDefaultFeatureValues();

                features[CityFeatureType.NoVowelsCityNameMatch] = true;

                if (entity.Population > 0)
                {
                    features[CityFeatureType.NoVowelsCityNamePopulation] = (uint?)entity.Population;
                }

                features[CityFeatureType.NoVowelsCityNameLetters] = (byte?)nameVariation.Length;

                if (nameVariation.Length > 0)
                {
                    features[CityFeatureType.NoVowelsCityNameLettersRatio] = (float?)(nameVariation.Length / ((1.0f) * entity.Name.Length));
                }

                EntitiesToFeatures entitiesToFeatures;

                if (!variationsToEntitiesToFeatures.TryGetValue(nameVariation, out entitiesToFeatures))
                {
                    entitiesToFeatures = new EntitiesToFeatures();
                    variationsToEntitiesToFeatures[nameVariation] = entitiesToFeatures;
                }

                entitiesToFeatures[entity] = features;
            }
        }

        private HashSet<string> GenerateComplexVariationsForName(string name, int minLetters)
        {
            var variations = new HashSet<string>();

            if (string.IsNullOrEmpty(name))
            {
                return variations;
            }

            name = name.ToLowerInvariant();

            if (name.Length > MaxLettersToUseComplexAlgo)
            {
                return this.GenerateSimpleVariationsForName(name);
            }

            var targetLetters = this.SelectTargetLetters(name);

            if (targetLetters.Length == 0 || targetLetters.Length < minLetters)
            {
                return variations;
            }

            // All leters initially set to false
            var selectedLetters = new BitArray(targetLetters.Length);

            while (this.IncrementSelectedLetters(selectedLetters))
            {
                var trueCount = this.SelectedLettersTrueCount(selectedLetters);

                if (trueCount >= minLetters && trueCount <= MaxLetters)
                {
                    // return this.ApplyCombination(targetLetters, selectedLetters);

                    //if (!selectedLetters[0] && !selectedLetters[1])
                    if (!selectedLetters[0])
                    {
                        continue;
                    }

                    var combination = this.ApplyCombination(targetLetters, selectedLetters);
                    variations.Add(combination);
                }
            }

            return variations;
        }

        private int SelectedLettersTrueCount(BitArray selectedLetters)
        {
            var count = 0;

            for (var i = 0; i < selectedLetters.Count; i++)
            {
                if (selectedLetters[i])
                {
                    count++;
                }
            }

            return count;
        }

        private bool IncrementSelectedLetters(BitArray selectedLetters)
        {
            for (var i = selectedLetters.Count - 1; i>=0; i--)
            {
                if (selectedLetters[i] == false)
                {
                    selectedLetters[i] = true;

                    for (var j = i + 1; j < selectedLetters.Count; j++)
                    {
                        selectedLetters[j] = false;
                    }

                    return true;
                }
            }

            return false;
        }

        private string ApplyCombination(StringBuilder targetLetters, BitArray selectedLetters)
        {
            var combination = new StringBuilder();

            for (var i = 0; i <  selectedLetters.Count; i++)
            {
                if (selectedLetters[i])
                {
                    combination.Append(targetLetters[i]);
                }
            }

            return combination.ToString();
        }

        private StringBuilder SelectTargetLetters(string name)
        {
            var selectedLetters = new StringBuilder();

            var words = name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                for (var i = 0; i < word.Length; i++)
                {
                    var c = word[i];

                    // Always pick the first and last letters in a word
                    if (i == 0 || i == (word.Length - 1))
                    {
                        selectedLetters.Append(c);
                    }
                    else
                    {
                        if (c < 'a' || c > 'z')
                        {
                            continue;
                        }

                        if ("aeiou".IndexOf(c) < 0)
                        {
                            selectedLetters.Append(c);
                        }
                    }
                }
            }

            return selectedLetters;
        }

        private HashSet<string> GenerateSimpleVariationsForName(string name)
        {
            var variations = new HashSet<string>();

            if (string.IsNullOrEmpty(name))
            {
                return variations;
            }

            name = name.ToLowerInvariant();

            var words = name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var nameNoVowels = new StringBuilder();

            foreach (var word in words)
            {
                if (word.Length == 0)
                {
                    continue;
                }

                // Always add the first letter, regardless if it's a vowel or not
                nameNoVowels.Append(word[0]);

                for (var i = 1; i < word.Length; i++)
                {
                    var c = word[i];

                    if (c < 'a' || c > 'z')
                    {
                        return variations;
                    }

                    if ("aeiou".IndexOf(c) < 0)
                    {
                        nameNoVowels.Append(c);
                    }
                }
            }

            if (nameNoVowels.Length >= 3)
            {
                var nameNoVowelsStr = nameNoVowels.ToString();
                variations.Add(nameNoVowelsStr);

                if (nameNoVowelsStr.Length > 4)
                {
                    variations.Add(nameNoVowelsStr.Substring(0, 4));
                }

                if (nameNoVowelsStr.Length > 5)
                {
                    variations.Add(nameNoVowelsStr.Substring(0, 5));
                }

                if (nameNoVowelsStr.Length > 6)
                {
                    variations.Add(nameNoVowelsStr.Substring(0, 6));
                }
            }

            return variations;
        }

        public override Dictionary<GeonamesCityEntity, Features> GenerateCandidatesAndFeatures(HostnameSplitterResult parsedHostname)
        {
            var candidatesAndFeatures = new Dictionary<GeonamesCityEntity, Features>();

            if (parsedHostname?.SubdomainParts == null)
            {
                return candidatesAndFeatures;
            }

            foreach (var subdomainPart in parsedHostname.SubdomainParts)
            {
                EntitiesToFeatures entitiesToFeatures;

                if (variationsToEntitiesToFeatures.TryGetValue(subdomainPart.Substring, out entitiesToFeatures))
                {
                    foreach (var entry in entitiesToFeatures)
                    {
                        var features = new Features();

                        foreach (var featureEntry in entry.Value)
                        {
                            features[featureEntry.Key] = featureEntry.Value;
                        }

                        if (this.FeaturesConfig.UseSlotIndex)
                        {
                            features[CityFeatureType.NoVowelsCityRTLSlotIndex] = subdomainPart.RTLSlotIndex;
                            features[CityFeatureType.NoVowelsCityLTRSlotIndex] = subdomainPart.LTRSlotIndex;
                        }

                        candidatesAndFeatures[entry.Key] = features;
                    }
                }
            }

            return candidatesAndFeatures;
        }
    }
}
