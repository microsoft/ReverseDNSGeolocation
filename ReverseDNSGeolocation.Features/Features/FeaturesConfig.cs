namespace ReverseDNSGeolocation.Features
{
    using System;

    [Serializable]
    public class FeaturesConfig
    {
        // Use the actual domain name (hashed into an int) as a column/feature in the classifier
        public bool UseDomainAsFeature { get; set; }

        // Use the index of the position of the matched string in the domain
        // For example, in c-24-18-208-95.hsd1.wa.comcast.net the string wa would be at position 
        // 0 from right to left (ignoring the domain), and at position 2 from left to right (again
        // ignoring the domain)
        public bool UseSlotIndex { get; set; }

        // Should we include the following features: AlternateCityNameIsPreferredName, AlternateCityNameIsShortName
        // AlternateCityNameIsColloquial, and AlternateCityNameIsHistoric
        public bool UseAlternateNameCategories { get; set; }

        // Does the classification algorithm that we will use on this data allows for features that have a null value?
        // If yes, then set the features to null if they do not have a value
        // If no, set the features to a default non-null value
        public bool NullDefaultsAllowed { get; set; }

        // Should we set the value of the features even if they do not have any value?
        // If yes, before we overwrite any features, we will add all of them to the Dictionary with a default value
        // If no, we will not add any default values to the dictionary, so only the non-default values will be stored there
        public bool InitializeDefaultFeatures { get; set; }

        // The minimum population of a (Geonames) location so that it is included in feature generation
        // A value of 0 means include all locations regardless of their population
        public int MinimumPopulation { get; set; }

        // Fill in the ExactCityNameAlternateNamesCount and AlternateCityNameAlternateNamesCount features
        public bool UseAlternateNamesCount { get; set; }

        // The maximim distance in miles between the center of a city and a ground truth point to use when training true positives
        public int TruePositiveMaximumDistanceKilometers { get; set; }

        // Use the complex version of the NoVowelsCityFeaturesGenerator (uses a lot more RAM and takes longer)
        public bool UseComplexNoVowelsFeature { get; set; }

        // Use pre-trainerns patterns learned from ground truth which map string combinations to locations.
        // Example of learned pattern rule: if domain is frontiernet.net and the subdomain contains a part "lsan", then location is "Los Angeles"
        public bool UseHostnamePatternMatchingFeature { get; set; }

        public FeaturesConfig()
        {
            this.UseDomainAsFeature = false;
            this.UseSlotIndex = false;
            this.UseAlternateNameCategories = false;
            this.NullDefaultsAllowed = false;
            this.InitializeDefaultFeatures = true;
            this.MinimumPopulation = 0;
            this.UseAlternateNamesCount = true;
            //this.TruePositiveMaximumDistanceKilometers = 20;
            this.TruePositiveMaximumDistanceKilometers = 50;
            this.UseComplexNoVowelsFeature = false;
        }

        public override string ToString()
        {
            return string.Format(
                "{0}-{1}-{2}-{3}-{4}-{5}-{6}-{7}km-{8}-{9}",
                this.BoolToNumber(this.UseDomainAsFeature),
                this.BoolToNumber(this.UseSlotIndex),
                this.BoolToNumber(this.UseAlternateNameCategories),
                this.BoolToNumber(this.NullDefaultsAllowed),
                this.BoolToNumber(this.InitializeDefaultFeatures),
                this.MinimumPopulation,
                this.BoolToNumber(this.UseAlternateNamesCount),
                this.TruePositiveMaximumDistanceKilometers,
                this.BoolToNumber(this.UseComplexNoVowelsFeature),
                this.BoolToNumber(this.UseHostnamePatternMatchingFeature));
        }

        private int BoolToNumber(bool val)
        {
            return val ? 1 : 0;
        }
    }
}
