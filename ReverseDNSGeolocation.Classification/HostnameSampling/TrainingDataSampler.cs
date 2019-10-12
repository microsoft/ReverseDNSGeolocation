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
    using GeonamesParsers;

    public class TrainingDataSampler : ITrainingDataSampler
    {
        public int ValidLinesCounter { get; set; }

        public int ProcessingAttemptedCounter { get; set; }

        public Dictionary<string, int> HostnameCounts { get; set; }

        public CityFeaturesAggregator Aggregator { get; set; }

        public string GroundTruthPath { get; set; }

        public int TrueNegativesMultiplier { get; set; }

        public FeaturesConfig FeaturesConfig { get; set; }

        public Dictionary<string, int> PositiveSamplesFeatureDistribution { get; set; }

        public Dictionary<string, int> NegativeSamplesFeatureDistribution { get; set; }

        public Random Rand { get; set; }

        public LRUCache<GeonamesCityEntity> LatestPositiveSampleCities { get; set; }

        public LRUCache<GeonamesCityEntity> LatestNegativeSampleCities { get; set; }

        public int SelectedTruePositivesCount { get; set; }
        public int SelectedTrueNegativesCount { get; set; }

        private int maxPositiveSamplesPerDomain;

        private double maxDiffMagnitude;
        private double forceAddMinPercentile;

        public TrainingDataSampler(
            CityFeaturesAggregator aggregator,
            string groundTruthPath,
            int trueNegativesMultiplier,
            FeaturesConfig featuresConfig,
            int maxPositiveSamplesPerDomain,
            double maxDiffMagnitude = 10d,
            double forceAddMinPercentile = 0.2d)
        {
            this.ValidLinesCounter = 0;
            this.ProcessingAttemptedCounter = 0;
            this.HostnameCounts = new Dictionary<string, int>();

            this.Aggregator = aggregator;
            this.GroundTruthPath = groundTruthPath;
            this.TrueNegativesMultiplier = trueNegativesMultiplier;
            this.FeaturesConfig = featuresConfig;

            this.PositiveSamplesFeatureDistribution = new Dictionary<string, int>();
            this.NegativeSamplesFeatureDistribution = new Dictionary<string, int>();

            this.Rand = new Random(Seed: 7);

            this.LatestPositiveSampleCities = new LRUCache<GeonamesCityEntity>(10);
            this.LatestNegativeSampleCities = new LRUCache<GeonamesCityEntity>(10);

            this.SelectedTruePositivesCount = 0;
            this.SelectedTrueNegativesCount = 0;

            this.maxPositiveSamplesPerDomain = maxPositiveSamplesPerDomain;
            this.maxDiffMagnitude = maxDiffMagnitude;
            this.forceAddMinPercentile = forceAddMinPercentile;
        }

        private void IncrementHostnameCount(HostnameSplitterResult parsedHostname)
        {
            var domain = parsedHostname?.DomainInfo?.RegistrableDomain;

            if (string.IsNullOrWhiteSpace(domain))
            {
                return;
            }

            int currentHostnameCount;

            if (!this.HostnameCounts.TryGetValue(domain, out currentHostnameCount))
            {
                currentHostnameCount = 0;
            }

            this.HostnameCounts[domain] = currentHostnameCount + 1;
        }

        private bool ShouldAcceptDomain(string hostname, HostnameSplitterResult parsedHostname)
        {
            if (parsedHostname?.DomainInfo?.RegistrableDomain == null || string.IsNullOrEmpty(hostname) || !hostname.Contains(".") || hostname.Contains(" ") || hostname.Contains(","))
            {
                return false;
            }

            var domain = parsedHostname.DomainInfo.RegistrableDomain;

            /*
            if (hostname.ToLowerInvariant().Contains("comcast.net"))
            {
                return false;
            }
            */

            int currentHostnameCount;

            if (this.HostnameCounts.TryGetValue(domain, out currentHostnameCount))
            {
                return currentHostnameCount <= this.maxPositiveSamplesPerDomain;
            }
            else
            {
                return true;
            }
        }

        private bool ShouldContinueIngestingNewHostnames(string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
        {
            ///if (this.ValidLinesCounter > 10000000)
            /*
            if (this.ValidLinesCounter > 30000000)
            {
                return false;
            }
            */

            return true;
        }

        //// !!!!!! public abstract void ShowConsoleStats(string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount, bool lastRow = false);

        public virtual IEnumerable<TrainingDataSample> Sample()
        {
            string line;

            using (var file = new StreamReader(this.GroundTruthPath))
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

                    this.ValidLinesCounter++;

                    if (this.ValidLinesCounter % 100000 == 0)
                    {
                        Console.WriteLine($"ValidLinesCounter = {this.ValidLinesCounter}, ProcessingAttemptedCounter = {this.ProcessingAttemptedCounter}, SelectedTruePositivesCount = {this.SelectedTruePositivesCount}, SelectedTrueNegativesCount = {this.SelectedTrueNegativesCount}");
                    }

                    var parsedHostname = HostnameSplitter.Split(hostname);

                    if (!this.ShouldAcceptDomain(hostname, parsedHostname))
                    {
                        continue;
                    }

                    this.ProcessingAttemptedCounter++;

                    //// !!!!!! this.ShowConsoleStats(hostname, parsedHostname, storedTruePositivesCount, storedTrueNegativesCount);

                    var candidatesAndFeatures = this.Aggregator.GenerateCandidatesForHostname(parsedHostname);

                    var truesPositives = new List<TrainingDataSample>();
                    var trueNegatives = new List<TrainingDataSample>();

                    foreach (var candidateEntry in candidatesAndFeatures)
                    {
                        var locationCandidate = candidateEntry.Key;
                        var locationFeatures = candidateEntry.Value;

                        //var distance = DistanceHelper.Distance(trueLatitude, trueLongitude, locationCandidate.Latitude, locationCandidate.Longitude, DistanceUnit.Mile);
                        var distance = DistanceHelper.Distance(trueLatitude, trueLongitude, locationCandidate.Latitude, locationCandidate.Longitude, DistanceUnit.Kilometer);

                        //if (distance <= this.FeaturesConfig.TruePositiveMaximumDistanceMiles)
                        if (distance <= this.FeaturesConfig.TruePositiveMaximumDistanceKilometers)
                        {
                            var positiveSampleFeaturesSignature = this.GenerateFeaturesBoolSignature(locationFeatures, isPositiveSample: true);

                            if (!this.LatestPositiveSampleCities.Contains(locationCandidate) && this.ShouldAddSample(this.PositiveSamplesFeatureDistribution, positiveSampleFeaturesSignature, isPositiveSample: true))
                            {
                                truesPositives.Add(new TrainingDataSample()
                                {
                                    Hostname = hostname,
                                    City = locationCandidate,
                                    FeaturesSignature = positiveSampleFeaturesSignature,
                                    Features = locationFeatures,
                                    IsPositiveExample = true
                                });

                                this.IncrementFeatureDistribution(this.PositiveSamplesFeatureDistribution, positiveSampleFeaturesSignature);
                                this.LatestPositiveSampleCities.Increment(locationCandidate);
                            }
                        }
                        else
                        {
                            var negativeSampleFeaturesSignature = this.GenerateFeaturesBoolSignature(locationFeatures, isPositiveSample: false);

                            trueNegatives.Add(new TrainingDataSample()
                            {
                                Hostname = hostname,
                                City = locationCandidate,
                                FeaturesSignature = negativeSampleFeaturesSignature,
                                Features = locationFeatures,
                                IsPositiveExample = false
                            });
                        }
                    }

                    // WARNING: Do not move this above the true negatives selection, at it will bias the data (even more)
                    if (truesPositives.Count > 0)
                    {
                        foreach (var truePositive in truesPositives)
                        {
                            yield return truePositive;
                        }

                        this.IncrementHostnameCount(parsedHostname);
                        this.SelectedTruePositivesCount += truesPositives.Count;
                    }

                    var neededTrueNegativeItemsCount = this.SelectedTruePositivesCount * this.TrueNegativesMultiplier - this.SelectedTrueNegativesCount;

                    if (trueNegatives.Count > 0 && neededTrueNegativeItemsCount > 0)
                    {
                        var neededItemsCount = Math.Min(trueNegatives.Count, neededTrueNegativeItemsCount);

                        if (neededItemsCount > 0)
                        {
                            var addedItems = 0;

                            foreach (var trueNegativeItem in trueNegatives)
                            {
                                if (addedItems >= neededItemsCount)
                                {
                                    break;
                                }

                                if (!this.LatestNegativeSampleCities.Contains(trueNegativeItem.City) && this.ShouldAddSample(this.PositiveSamplesFeatureDistribution, trueNegativeItem.FeaturesSignature, isPositiveSample: false))
                                {
                                    yield return trueNegativeItem;
                                    this.SelectedTrueNegativesCount++;
                                    this.IncrementFeatureDistribution(this.NegativeSamplesFeatureDistribution, trueNegativeItem.FeaturesSignature);
                                    addedItems++;
                                    this.LatestNegativeSampleCities.Increment(trueNegativeItem.City);
                                }
                            }
                        }
                    }

                    /*
                    if (counter % 1000 == 0)
                    {
                        Console.WriteLine("------------------------------------");

                        foreach (var entry in positivesFeaturesDistribution)
                        {
                            Console.WriteLine($"Positive: {entry.Key}\t{entry.Value}");
                        }

                        Console.WriteLine("------------------------------------");
                    }
                    */

                    if (!this.ShouldContinueIngestingNewHostnames(hostname, parsedHostname, this.SelectedTruePositivesCount, this.SelectedTrueNegativesCount))
                    {
                        //// !!!!!! this.ShowConsoleStats(hostname, parsedHostname, storedTruePositivesCount, storedTrueNegativesCount, lastRow: true);
                        break;
                    }
                }
            }
        }

        private bool ShouldAddSample(Dictionary<string, int> featuresDistribution, string featuresSignature, bool isPositiveSample)
        {
            if (featuresDistribution.Count == 0)
            {
                return true;
            }

            int currentFeaturesSignatureCount;

            if (!featuresDistribution.TryGetValue(featuresSignature, out currentFeaturesSignatureCount))
            {
                return true;
            }

            //var maxDiffMagnitude = 10d;
            //var forceAddMinPercentile = 0.2d;

            string smallestFeatureSignature = null;
            string largestFeatureSignature = null;

            var smallestFeatureSignatureCount = int.MaxValue;
            var largestFeatureSignatureCount = 0;

            foreach (var entry in featuresDistribution)
            {
                var featureType = entry.Key;
                var featureCount = entry.Value;

                if (featureCount < smallestFeatureSignatureCount)
                {
                    smallestFeatureSignatureCount = featureCount;
                    smallestFeatureSignature = featureType;
                }

                if (featureCount > largestFeatureSignatureCount)
                {
                    largestFeatureSignatureCount = featureCount;
                    largestFeatureSignature = featureType;
                }
            }

            // TODO: Config
            if (largestFeatureSignatureCount < 1000)
            {
                return true;
            }

            if (largestFeatureSignatureCount == 0 || smallestFeatureSignatureCount == int.MaxValue)
            {
                return true;
            }

            if (smallestFeatureSignature == largestFeatureSignature)
            {
                return true;
            }

            if ((largestFeatureSignatureCount / (1.0d * smallestFeatureSignatureCount)) < this.maxDiffMagnitude)
            {
                return true;
            }

            var forceAddMinCount = this.forceAddMinPercentile * largestFeatureSignatureCount;

            if (currentFeaturesSignatureCount <= forceAddMinCount)
            {
                return true;
            }

            return false;
        }

        private string GenerateFeaturesBoolSignature(Dictionary<CityFeatureType, object> newFeatures, bool isPositiveSample)
        {
            var signature = new StringBuilder();

            if (isPositiveSample)
            {
                signature.Append("1-");
            }
            else
            {
                signature.Append("0-");
            }

            foreach (var featureType in Enum.GetValues(typeof(CityFeatureType)).Cast<CityFeatureType>())
            {
                object rawFeatureValue;

                if (newFeatures.TryGetValue(featureType, out rawFeatureValue))
                {
                    if (rawFeatureValue is bool && ((bool)rawFeatureValue))
                    {
                        signature.Append("1");
                    }
                    else
                    {
                        signature.Append("-");
                    }
                }
                else
                {
                    signature.Append("-");
                }
            }

            return signature.ToString();
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

        private void IncrementFeatureDistribution(Dictionary<string, int> featuresDistribution, string signature)
        {
            var count = 0;
            featuresDistribution.TryGetValue(signature, out count);

            count++;
            featuresDistribution[signature] = count;
        }
    }
}
