namespace ReverseDNSGeolocation.Classification.ModelRunners
{
    using Accord.Math;
    using Features;
    using GeonamesParsers;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ModelRunner
    {
        public CityFeaturesAggregator Aggregator { get; set; }

        public TrainingData TrainingData { get; set; }

        public IClassifier Classifier { get; set; }

        public bool DebugMode { get; set; }

        public Stopwatch HostnameSplittingTime { get; set; }

        public Stopwatch CandidateGenerationTime { get; set; }

        public Stopwatch ClassificationTime { get; set; }

        public Stopwatch TotalExtractCitiesTime { get; set; }

        public ModelRunner(CityFeaturesAggregator aggregator, TrainingData trainingData, IClassifier classifier, bool debugMode = false)
        {
            this.Aggregator = aggregator;
            this.TrainingData = trainingData;
            this.Classifier = classifier;
            this.DebugMode = debugMode;

            this.HostnameSplittingTime = new Stopwatch();
            this.CandidateGenerationTime = new Stopwatch();
            this.ClassificationTime = new Stopwatch();
            this.TotalExtractCitiesTime = new Stopwatch();
        }

        public ModelRunner(ClassifierBundle bundle, bool debugMode = false)
        {
            this.Aggregator = bundle.Aggregator;
            this.TrainingData = bundle.TrainingData;
            this.Classifier = bundle.Classifier as IClassifier;
            this.DebugMode = debugMode;

            this.HostnameSplittingTime = new Stopwatch();
            this.CandidateGenerationTime = new Stopwatch();
            this.ClassificationTime = new Stopwatch();
            this.TotalExtractCitiesTime = new Stopwatch();
        }

        public List<ClassificationResult> ExtractCities(string hostname)
        {
            this.TotalExtractCitiesTime.Start();

            this.HostnameSplittingTime.Start();
            var subdomainParts = HostnameSplitter.Split(hostname);
            this.HostnameSplittingTime.Stop();

            var results = new List<ClassificationResult>();

            if (subdomainParts == null)
            {
                return results;
            }

            this.CandidateGenerationTime.Start();
            var candidatesAndFeatures = this.Aggregator.GenerateCandidatesForHostname(subdomainParts);
            this.CandidateGenerationTime.Stop();

            foreach (var candidateAndFeatures in candidatesAndFeatures)
            {
                var entity = candidateAndFeatures.Key;
                var features = candidateAndFeatures.Value;

                /*
                object val;

                if (!features.TryGetValue(CityFeatureType.HostnamePatternMatch, out val))
                {
                    Console.WriteLine("!!!");
                }

                var valBool = (bool)val;

                if (valBool)
                {
                    Console.WriteLine("!!!");
                }
                */

                var featuresRow = this.TrainingData.CreateTrainingRow(features);
                var featuresRowArr = featuresRow.ToArray<double>(this.TrainingData.InputColumnNames);

                int label;

                this.ClassificationTime.Start();
                var probability = this.Classifier.Probability(featuresRowArr, out label);
                this.ClassificationTime.Stop();

                if (label == 1)
                {
                    /*
                    if (valBool)
                    {
                        Console.WriteLine("!!!");
                    }
                    */

                    if (this.DebugMode)
                    {
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t\t\t\t{1}\t\t\t\t{2}", hostname, entity, Math.Round(probability, 3)));
                    }

                    var nonDefaultFeatures = new Features();

                    foreach (var entry in features)
                    {
                        var featureName = entry.Key;
                        var featureValue = entry.Value;

                        var featureDefaultValue = this.TrainingData.FeatureDefaults[featureName];

                        // Only show a feature if its value is different than the default
                        //if (featureValue != featureDefaultValue) // This does not work
                        if (!featureValue.Equals(featureDefaultValue)) // This DOES work
                        {
                            nonDefaultFeatures[featureName] = featureValue;

                            /*
                            if (this.DebugMode)
                            {
                                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", featureName, featureValue));
                            }
                            */
                        }
                    }

                    /*
                    if (this.DebugMode)
                    {
                        Console.WriteLine("---");
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Output label: {0} (Probability: {1})", label, probability));
                        Console.WriteLine();

                        Console.WriteLine("------------------------");
                        Console.WriteLine();
                    }
                    */

                    results.Add(new ClassificationResult()
                    {
                        City = entity,
                        AllFeatures = features,
                        NonDefaultFeatures = nonDefaultFeatures,
                        Score = probability
                    });
                }
            }

            if (results.Count > 1)
            {
                results = results.OrderByDescending(r => r.Score).ToList<ClassificationResult>();
            }

            this.TotalExtractCitiesTime.Stop();

            return results;
        }
    }
}
