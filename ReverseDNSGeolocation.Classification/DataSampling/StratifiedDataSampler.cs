namespace ReverseDNSGeolocation.Classification
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Accord.MachineLearning.DecisionTrees;
    using Accord.MachineLearning.DecisionTrees.Learning;
    using Accord.Math;
    using Accord.Math.Optimization.Losses;
    using Accord.Statistics.Analysis;
    using Accord.Statistics.Filters;
    using ReverseDNSGeolocation.Features;

    public class StratifiedDataSampler
    {
        public delegate bool ShouldProcessHostname(string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount);

        public delegate bool ShouldContinueIngestingNewHostnames(string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount);

        public delegate void ShowConsoleStats(string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount, bool lastRow = false);

        public CityFeaturesAggregator Aggregator { get; set; }

        public TrainingData TrainingData { get; set; }

        public TrainingData Sample(
            string citiesPath,
            string alternateNamesPath,
            string admin1Path,
            string admin2Path,
            string countriesPath,
            string clliPath,
            string unlocodePath,
            string groundTruthPath,
            int trueNegativesMultiplier,
            ShouldProcessHostname shouldProcessHostname = null,
            ShouldContinueIngestingNewHostnames shouldContinueIngestingNewHostnames = null,
            ShowConsoleStats showConsoleStats = null,
            FeaturesConfig featuresConfig = null)
        {
            if (featuresConfig == null)
            {
                featuresConfig = new FeaturesConfig();
            }

            var aggregator = new CityFeaturesAggregator(citiesPath, alternateNamesPath, admin1Path, admin2Path, countriesPath, clliPath, unlocodePath, featuresConfig: featuresConfig);
            this.Aggregator = aggregator;

            var trainingData = new TrainingData(tableName: "ReverseDNSGeolocation Training", featuresAggregator: aggregator);
            this.TrainingData = trainingData;

            string line;
            var counter = 0;

            var storedTruePositivesCount = 0;
            var storedTrueNegativesCount = 0;

            var positivesFeaturesDistribution = new Dictionary<CityFeatureType, int>();

            var rand = new Random();

            using (var file = new StreamReader(groundTruthPath))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)
                        || (line.Length > 0 && line[0] == '#'))
                    {
                        continue;
                    }

                    var parts = new List<string>(line.Split(new char[] { '\t' }));

                    if (parts.Count != 13)
                    {
                        continue;
                    }

                    /*
                    0  RawIP   string
                    1  NumericIP   uint
                    2  Bucket  uint?
                    3  ReverseDNSHostname  string
                    4  RealtimeLatitude    double?
                    5  RealtimeLongitude   double?
                    6  RealtimeCountryISO  string
                    7  RealtimeCountryConfidence   byte
                    8  RealtimeState   string
                    9  RealtimeStateConfidence byte
                    10 RealtimeCity    string
                    11 RealtimeCityConfidence  byte
                    12 RealtimeAccuracyKm  double?
                    */

                    var hostname = parts[3];

                    var trueLatitudeStr = parts[4];
                    var trueLongitudeStr = parts[5];

                    if (string.IsNullOrWhiteSpace(hostname)
                        || string.IsNullOrWhiteSpace(trueLatitudeStr)
                        || string.IsNullOrWhiteSpace(trueLongitudeStr))
                    {
                        continue;
                    }

                    double trueLatitude;
                    double trueLongitude;

                    if (!double.TryParse(trueLatitudeStr, out trueLatitude)
                        || !double.TryParse(trueLongitudeStr, out trueLongitude))
                    {
                        continue;
                    }

                    var parsedHostname = HostnameSplitter.Split(hostname);

                    if (shouldProcessHostname != null && !shouldProcessHostname(hostname, parsedHostname, storedTruePositivesCount, storedTrueNegativesCount))
                    {
                        continue;
                    }

                    counter++;

                    if (showConsoleStats != null)
                    {
                        showConsoleStats(hostname, parsedHostname, storedTruePositivesCount, storedTrueNegativesCount);
                    }

                    var candidatesAndFeatures = aggregator.GenerateCandidatesForHostname(parsedHostname);

                    var truesPositives = new List<DataRow>();
                    var trueNegatives = new List<DataRow>();

                    foreach (var candidateEntry in candidatesAndFeatures)
                    {
                        var locationCandidate = candidateEntry.Key;
                        var locationFeatures = candidateEntry.Value;

                        var distance = DistanceHelper.Distance(trueLatitude, trueLongitude, locationCandidate.Latitude, locationCandidate.Longitude, DistanceUnit.Mile);

                        if (distance <= featuresConfig.TruePositiveMaximumDistanceKilometers)
                        {
                            if (this.ShouldAddPositiveExample(positivesFeaturesDistribution, locationFeatures))
                            {
                                var newRow = trainingData.CreateTrainingRow(locationFeatures, isValidLocation: true);
                                truesPositives.Add(newRow);

                                this.AddToFeaturesDistribution(positivesFeaturesDistribution, locationFeatures);
                            }

                            /*
                            if (storedTruePositivesCount % 100 == 0)
                            {
                                Console.WriteLine($"{hostname} - {locationCandidate}");
                                Console.WriteLine("---");

                                foreach (var feature in locationFeatures)
                                {
                                    Console.WriteLine($"{feature.Key} = {feature.Value}");
                                }

                                Console.WriteLine("---------------------------------");
                            }
                            */

                            /*
                            if ((bool)locationFeatures[CityFeatureType.ExactAdmin1NameMatch] == true && (bool)locationFeatures[CityFeatureType.CityAdmin1NameMatch] == true)
                            {
                                Console.WriteLine($"{hostname} - {locationCandidate}");
                                Console.WriteLine("---");

                                foreach (var feature in locationFeatures)
                                {
                                    Console.WriteLine($"{feature.Key} = {feature.Value}");
                                }

                                Console.WriteLine("---------------------------------");
                            }
                            */
                        }
                        else
                        {
                            var newRow = trainingData.CreateTrainingRow(locationFeatures, isValidLocation: false);
                            trueNegatives.Add(newRow);

                            /*
                            if ((bool)locationFeatures[CityFeatureType.ExactAdmin1NameMatch] == true && (bool)locationFeatures[CityFeatureType.CityAdmin1NameMatch] == true)
                            {
                                Console.WriteLine($"{hostname} - {locationCandidate}");
                                Console.WriteLine("---");

                                foreach (var feature in locationFeatures)
                                {
                                    Console.WriteLine($"{feature.Key} = {feature.Value}");
                                }

                                Console.WriteLine("---------------------------------");
                            }
                            */
                        }
                    }

                    // WARNING: Do not move this above the true negatives selection, at it will bias the data (even more)
                    if (truesPositives.Count > 0)
                    {
                        truesPositives.ForEach(tp => trainingData.AddTrainingRow(tp));
                        storedTruePositivesCount += truesPositives.Count;
                    }

                    var neededTrueNegativeItemsCount = storedTruePositivesCount * trueNegativesMultiplier;

                    if (trueNegatives.Count > 0 && storedTrueNegativesCount < neededTrueNegativeItemsCount)
                    {
                        var neededItemsCount = 0;

                        if (truesPositives.Count > 0)
                        {
                            neededItemsCount = Math.Min(truesPositives.Count, Math.Min(trueNegatives.Count, neededTrueNegativeItemsCount));
                        }
                        else
                        {
                            neededItemsCount = Math.Min(trueNegatives.Count, neededTrueNegativeItemsCount);
                        }

                        var extractedRandTrueNegativeItems = trueNegatives.OrderBy(x => rand.Next()).Take(neededItemsCount);

                        foreach (var trueNegativeItem in extractedRandTrueNegativeItems)
                        {
                            trainingData.AddTrainingRow(trueNegativeItem);
                            storedTrueNegativesCount++;
                        }
                    }

                    if (counter % 1000 == 0)
                    {
                        Console.WriteLine("------------------------------------");

                        foreach (var entry in positivesFeaturesDistribution)
                        {
                            Console.WriteLine($"Positive: {entry.Key}\t{entry.Value}");
                        }

                        Console.WriteLine("------------------------------------");
                    }

                    if (shouldContinueIngestingNewHostnames != null && !shouldContinueIngestingNewHostnames(hostname, parsedHostname, storedTruePositivesCount, storedTrueNegativesCount))
                    {
                        showConsoleStats(hostname, parsedHostname, storedTruePositivesCount, storedTrueNegativesCount, lastRow: true);
                        break;
                    }
                }
            }

            trainingData.FinalizeData();

            return trainingData;
        }

        private bool ShouldAddPositiveExample(Dictionary<CityFeatureType, int> featuresDistribution, Dictionary<CityFeatureType, object> newFeatures)
        {
            if (featuresDistribution.Count == 0 || newFeatures.Count == 0)
            {
                return true;
            }

            var boolTrueFeatures = this.ExtractBoolEqualTrueFeatures(newFeatures);

            if (boolTrueFeatures.Count == 0)
            {
                return true;
            }

            foreach (var boolFeature in boolTrueFeatures)
            {
                if (!featuresDistribution.ContainsKey(boolFeature))
                {
                    return true;
                }
            }

            var maxDiffMagnitude = 10d;
            var forceAddMinPercentile = 0.2d;

            var smallestFeature = CityFeatureType.Unknown;
            var largestFeature = CityFeatureType.Unknown;

            var smallestFeatureCount = int.MaxValue;
            var largestFeatureCount = 0;

            foreach (var entry in featuresDistribution)
            {
                var featureType = entry.Key;
                var featureCount = entry.Value;

                if (featureCount < smallestFeatureCount)
                {
                    smallestFeatureCount = featureCount;
                    smallestFeature = featureType;
                }

                if (featureCount > largestFeatureCount)
                {
                    largestFeatureCount = featureCount;
                    largestFeature = featureType;
                }
            }

            if (largestFeatureCount == 0 || smallestFeatureCount == int.MaxValue)
            {
                return true;
            }

            if (smallestFeature == largestFeature)
            {
                return true;
            }

            if ((largestFeatureCount / (1.0d * smallestFeatureCount)) < maxDiffMagnitude)
            {
                return true;
            }

            /*
            else if (boolFeatures.Contains(smallestFeature))
            {
                return true;
            }
            */

            var forceAddMinCount = forceAddMinPercentile * largestFeatureCount;

            foreach (var distributionEntry in featuresDistribution)
            {
                var featureType = distributionEntry.Key;
                var featureCount = distributionEntry.Value;

                if (featureCount <= forceAddMinCount && boolTrueFeatures.Contains(featureType))
                {
                    return true;
                }
            }

            return false;
        }

        private HashSet<CityFeatureType> ExtractBoolEqualTrueFeatures(Dictionary<CityFeatureType, object> newFeatures)
        {
            var boolFeatures = new HashSet<CityFeatureType>();

            foreach (var entry in newFeatures)
            {
                var featureType = entry.Key;
                var rawFeatureValue = entry.Value;

                if (rawFeatureValue is bool)
                {
                    var featureValue = (bool)rawFeatureValue;

                    if (featureValue)
                    {
                        boolFeatures.Add(featureType);
                    }
                }
            }

            return boolFeatures;
        }

        private void AddToFeaturesDistribution(Dictionary<CityFeatureType, int> featuresDistribution, Dictionary<CityFeatureType, object> newFeatures)
        {
            if (featuresDistribution != null && newFeatures != null)
            {
                foreach (var entry in newFeatures)
                {
                    var featureType = entry.Key;
                    var rawFeatureValue = entry.Value;

                    /*
                    if (rawFeatureValue is int?)
                    {
                        var featureValue = rawFeatureValue as int?;

                        if (featureValue.HasValue && featureValue.Value != 0)
                        {
                            this.IncrementFeatureDistribution(featuresDistribution, featureType);
                        }
                    }
                    else if (rawFeatureValue is uint?)
                    {
                        var featureValue = rawFeatureValue as uint?;

                        if (featureValue.HasValue && featureValue.Value != 0)
                        {
                            this.IncrementFeatureDistribution(featuresDistribution, featureType);
                        }
                    }
                    else if (rawFeatureValue is byte?)
                    {
                        var featureValue = rawFeatureValue as byte?;

                        if (featureValue.HasValue && featureValue.Value != 0)
                        {
                            this.IncrementFeatureDistribution(featuresDistribution, featureType);
                        }
                    }
                    else if (rawFeatureValue is bool)
                    {
                        var featureValue = (bool)rawFeatureValue;

                        if (featureValue)
                        {
                            this.IncrementFeatureDistribution(featuresDistribution, featureType);
                        }
                    }
                    */

                    if (rawFeatureValue is bool)
                    {
                        var featureValue = (bool)rawFeatureValue;

                        if (featureValue)
                        {
                            this.IncrementFeatureDistribution(featuresDistribution, featureType);
                        }
                    }
                }
            }
        }

        private void IncrementFeatureDistribution(Dictionary<CityFeatureType, int> featuresDistribution, CityFeatureType featureType)
        {
            var count = 0;
            featuresDistribution.TryGetValue(featureType, out count);

            count++;
            featuresDistribution[featureType] = count;
        }
    }
}
