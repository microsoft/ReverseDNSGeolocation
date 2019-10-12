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

    public class DummySampler : ITrainingDataSampler
    {
        public int ValidLinesCounter { get; set; }

        public int ProcessingAttemptedCounter { get; set; }

        public CityFeaturesAggregator Aggregator { get; set; }

        public string GroundTruthPath { get; set; }

        public int TrueNegativesMultiplier { get; set; }

        public FeaturesConfig FeaturesConfig { get; set; }

        public Dictionary<string, int> PositiveSamplesFeatureDistribution { get; set; }

        public Dictionary<string, int> NegativeSamplesFeatureDistribution { get; set; }

        public Random Rand { get; set; }

        public int SelectedTruePositivesCount { get; set; }
        public int SelectedTrueNegativesCount { get; set; }

        private int maxSamples;

        public DummySampler(
            CityFeaturesAggregator aggregator,
            string groundTruthPath,
            int trueNegativesMultiplier,
            FeaturesConfig featuresConfig,
            int maxSamples)
        {
            this.ValidLinesCounter = 0;
            this.ProcessingAttemptedCounter = 0;

            this.Aggregator = aggregator;
            this.GroundTruthPath = groundTruthPath;
            this.TrueNegativesMultiplier = trueNegativesMultiplier;
            this.FeaturesConfig = featuresConfig;

            this.PositiveSamplesFeatureDistribution = new Dictionary<string, int>();
            this.NegativeSamplesFeatureDistribution = new Dictionary<string, int>();

            this.Rand = new Random(Seed: 7);

            this.SelectedTruePositivesCount = 0;
            this.SelectedTrueNegativesCount = 0;

            this.maxSamples = maxSamples;
        }

        private bool ShouldAcceptDomain(string hostname, HostnameSplitterResult parsedHostname)
        {
            if (parsedHostname?.DomainInfo?.RegistrableDomain == null || string.IsNullOrEmpty(hostname) || !hostname.Contains(".") || hostname.Contains(" ") || hostname.Contains(","))
            {
                return false;
            }

            return true;
        }

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

                    if (this.ProcessingAttemptedCounter > this.maxSamples)
                    {
                        break;
                    }

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

                            truesPositives.Add(new TrainingDataSample()
                            {
                                Hostname = hostname,
                                City = locationCandidate,
                                FeaturesSignature = positiveSampleFeaturesSignature,
                                Features = locationFeatures,
                                IsPositiveExample = true
                            });
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

                        this.SelectedTruePositivesCount += truesPositives.Count;
                    }

                    foreach (var trueNegativeItem in trueNegatives)
                    {
                        yield return trueNegativeItem;
                        this.SelectedTrueNegativesCount++;
                        this.IncrementFeatureDistribution(this.NegativeSamplesFeatureDistribution, trueNegativeItem.FeaturesSignature);
                    }
                }
            }
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

        private void IncrementFeatureDistribution(Dictionary<string, int> featuresDistribution, string signature)
        {
            var count = 0;
            featuresDistribution.TryGetValue(signature, out count);

            count++;
            featuresDistribution[signature] = count;
        }
    }
}
