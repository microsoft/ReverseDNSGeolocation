namespace ReverseDNSGeolocation.Console
{
    using Features;
    using GeonamesParsers;
    using ReverseDNSGeolocation;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Classification;
    using Louw.PublicSuffix;
    using Accord.IO;
    using Accord.MachineLearning.DecisionTrees;
    using Accord.Math;
    using Accord.MachineLearning.VectorMachines.Learning;
    using Accord.Statistics.Kernels;
    using Accord.Statistics.Models.Regression.Fitting;
    using Accord.Statistics.Models.Regression;
    using Accord.Math.Optimization.Losses;
    using Accord.Statistics.Analysis;
    using Features.CityFeatures.AddOnFeatures;
    using CLLI;
    using UNLOCODE;
    using System.Runtime.Serialization.Formatters.Binary;
    using Classification.ModelRunners;
    using Accord.MachineLearning.VectorMachines;
    using Classification.DatasetParsers;
    using Classification.BestGuess;
    using PatternMining;
    using MathNet.Numerics.Statistics;
    using System.Diagnostics;

    class Program
    {
        static void Main(string[] args)
        {
            var dataSubset = "";
            var maxPositiveSamplesPerDomain = 200;

            //var dataSubset = "-qwest";
            //var maxPositiveSamplesPerDomain = 20000;

            //var dataSubset = "-charter";
            //var maxPositiveSamplesPerDomain = 20000;

            //var dataSubset = "-nttpc";
            //var maxPositiveSamplesPerDomain = 20000;

            //var dataSubset = "-frontier";
            //var maxPositiveSamplesPerDomain = 20000;

            var featuresConfig = new FeaturesConfig()
            {
                UseComplexNoVowelsFeature = true,
                //UseHostnamePatternMatchingFeature = true,
                UseHostnamePatternMatchingFeature = true,
                //UseHostnamePatternMatchingFeature = false,
                TruePositiveMaximumDistanceKilometers = 50
            };

            // Geonames data folder
            var geonamesDatePrefix = "2018-03-06";
            var geonamesDataRootInputFolder = @"C:\Projects\ReverseDNS\Geonames\";

            // Folder to store or read serialized classifier from
            var classifierDatePrefix = "2018-03-06";
            var classifierOutputFolder = @"C:\Projects\ReverseDNS\SavedClassifiers\";

            // Folder to store or read training data from (precomputed parsed hostnames and features)
            var serializedTrainingDataDatePrefix = "2018-03-06";
            var serializedTrainingDataFolder = @"C:\Projects\ReverseDNS\SerializedIntermediaryData\";

            // Training ground truth data
            var trainingDateDatePrefix = "2018-02-28";
            var trainingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-train{1}.txt", trainingDateDatePrefix, dataSubset);

            var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test{1}.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-qwest-nttpc-verizon-charter-frontier.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-qwest-nttpc-verizon-charter-frontier-brasil-ertelecom-bell.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-frontier-charter-verizon-nttpc-qwest-brasiltelecom-ertelecom-bell-163data-optusnet.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-frontier-charter-nttpc-qwest-brasiltelecom-bell-163data-optusnet.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-bigpond.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-nttpc.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-bell.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-163data.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-optusnet.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-frontier.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-megared.txt", trainingDateDatePrefix, dataSubset);
            //var testingDataPath = string.Format(@"C:\Projects\ReverseDNS\ReverseDNSDataset\{0}-ReverseDNS-WithGT-shuffled-test-qwest.txt", trainingDateDatePrefix, dataSubset);

            var resultsDataPath = string.Format(@"C:\Projects\ReverseDNS\EvaluationResults\{0}-ReverseDNS-WithGT-shuffled-results{1}.txt", trainingDateDatePrefix, dataSubset);
            var featureDistributionDataPath = string.Format(@"C:\Projects\ReverseDNS\EvaluationResults\{0}-ReverseDNS-WithGT-shuffled-featureDistribution{1}.txt", trainingDateDatePrefix, dataSubset);

            // Dataset with CLLI codes mapped to geonames IDs
            var clliPath = @"C:\Projects\ReverseDNS\first6-city-geonames.tsv";

            // Dataset with UNLOCODE codes mapped to geonames IDs
            var unlocodePath = @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt";

            // Location of hostname pattern rules (Rule example: for frontier.net, if both "evrt" and "wa" are present as atoms in the hostname subdomain, that is located in a certain geohash location close to Everett, Washington)
            var hostnamePatternsDatePrefix = "2018-03-06";
            var hostnamesPatternsRootFolder = @"C:\Projects\ReverseDNS\HostnamePatterns\";

            /*
             * Defaults:
                    minimumRuleSupport: 1? 200? 500?,
                    minimimRuleOccFraction: 0.4,
                    pruneIntervalCount: 10000,
                    pruneMinKeepThreshold: 2
             */
            /*
           TrainHostnamePatterns(
               trainingDataPath: trainingDataPath,
               hostnamePatternsDatePrefix: hostnamePatternsDatePrefix,
               hostnamesPatternsRootFolder: hostnamesPatternsRootFolder,
               geonamesDatePrefix: geonamesDatePrefix,
               geonamesDataRootInputFolder: geonamesDataRootInputFolder,
               clliPath: clliPath,
               unlocodePath: unlocodePath,
               dataSubset: dataSubset,
               minimumRuleSupport: 50,
               minimimRuleOccFraction: 0.4d,
               pruneIntervalCount: 10000,
               pruneMinKeepThreshold: 2);*/

            /*
            TrainHostnamePatternClusters(
                trainingDataPath: trainingDataPath,
                hostnamePatternsDatePrefix: hostnamePatternsDatePrefix,
                hostnamesPatternsRootFolder: hostnamesPatternsRootFolder,
                dataSubset: dataSubset,
                minimumRuleSupport: 500,
                minimimRuleOccFraction: 0.7,
                pruneIntervalCount: 10000,
                pruneMinKeepThreshold: 10);*/

            /*
             * Defaults:
                    int minRuleOcc = 200,
                    int clusterThresholdKm = 15,
                    int minItemsPerCluster = 50,
                    double minSupportRatioPerCluster = 0.3d
            */
            /*
            TrainHostnamePatternCoarseClusters(
                trainingDataPath: trainingDataPath,
                hostnamePatternsDatePrefix: hostnamePatternsDatePrefix,
                hostnamesPatternsRootFolder: hostnamesPatternsRootFolder,
                geonamesDatePrefix: geonamesDatePrefix,
                geonamesDataRootInputFolder: geonamesDataRootInputFolder,
                clliPath: clliPath,
                dataSubset: dataSubset,
                minRuleOcc: 200,
                clusterThresholdKm: 20,
                minItemsPerCluster: 50,
                minSupportRatioPerCluster: 0.4d,
                pruneIntervalCountPerDomain: 10000,
                pruneMinKeepThreshold: 2);*/

            //// This was not commented out
            var externalCityFeatureGenerators = new List<CityFeaturesGenerator>();

            if (featuresConfig.UseHostnamePatternMatchingFeature)
            {
                var miner = new HostnamePatternMiner();
                //var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules.bin");
                var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");
                var reducedRules = PatternRulesSerializer.DeserializeReducedRules(patternsSerializationPath);

                // TODO: Remove extra class for unused add-on version of this feature
                externalCityFeatureGenerators.Add(new HostnamePatternsFeaturesGenerator(featuresConfig: featuresConfig, miner: miner, hostnamePatternRules: reducedRules));
            }

            var aggregator = new CityFeaturesAggregator(citiesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "cities1000.txt"),
                alternateNamesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "alternateNames.txt"),
                admin1Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin1CodesASCII.txt"),
                admin2Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin2Codes.txt"),
                countriesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "countryInfo.txt"),
                clliPath: clliPath,
                unlocodePath: unlocodePath,
                externalCityFeatureGenerators: externalCityFeatureGenerators,
                featuresConfig: featuresConfig);

            /*
            CreateSerializeTrainingData(
                aggregator: aggregator,
                featuresConfig: featuresConfig,
                groundTruthTrainingPath: trainingDataPath,
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                maxPositiveSamplesPerDomain: maxPositiveSamplesPerDomain,
                maxDiffMagnitude: 10d,
                forceAddMinPercentile: 0.2,
                dataSubset: dataSubset);*/

            // Logistic regression
            /*
            TrainClassifierFromSerializedTrainingData(
                aggregator: aggregator,
                featuresConfig: featuresConfig,
                classifierDatePrefix: classifierDatePrefix, 
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                classifierOutputFolder: classifierOutputFolder,
                dataSubset: dataSubset);
                */

            // C45 classification
            /*
            TrainC45ClassifierFromSerializedTrainingData(
                aggregator: aggregator,
                featuresConfig: featuresConfig,
                classifierDatePrefix: classifierDatePrefix,
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                classifierOutputFolder: classifierOutputFolder,
                dataSubset: dataSubset);
                */

            /*
            var hostnames = GetSampleHostnames();

            TestHostnamePatterns(
                hostnamePatternsDatePrefix: hostnamePatternsDatePrefix,
                hostnamesPatternsRootFolder: hostnamesPatternsRootFolder,
                dataSubset: dataSubset,
                hostnames: hostnames);
            */

            /*
            TestHostnamePatterns(
                hostnamePatternsDatePrefix: hostnamePatternsDatePrefix,
                hostnamesPatternsRootFolder: hostnamesPatternsRootFolder,
                dataSubset: dataSubset,
                testingDataPath: testingDataPath,
                useClosestCityInsteadOfRawCoordinates: true);*/

            // Test Logistic Classification - THIS
            /*
            TestClassifier(
                aggregator: aggregator,
                featuresConfig: featuresConfig,
                classifierDatePrefix: classifierDatePrefix,
                classifierFolder: classifierOutputFolder,
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                hostnamePatternsDatePrefix: hostnamePatternsDatePrefix,
                hostnamesPatternsRootFolder: hostnamesPatternsRootFolder,
                dataSubset: dataSubset,
                testingDataPath: testingDataPath,
                resultsPath: resultsDataPath,
                featureDistributionDataPath: featureDistributionDataPath,
                minProbability: 0);*/

            //// This was not commented out
            TestC45Classifier(
                aggregator: aggregator,
                featuresConfig: featuresConfig,
                classifierDatePrefix: classifierDatePrefix,
                classifierFolder: classifierOutputFolder,
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                hostnamePatternsDatePrefix: hostnamePatternsDatePrefix,
                hostnamesPatternsRootFolder: hostnamesPatternsRootFolder,
                dataSubset: dataSubset,
                testingDataPath: testingDataPath,
                resultsPath: resultsDataPath,
                featureDistributionDataPath: featureDistributionDataPath,
                minProbability: 0);

            // With sampler - Only use for 10-fold cross validation!
            /*
            TestLogisticRegressionClassifierWithSampler(
                aggregator,
                featuresConfig,
                testingDataPath,
                maxPositiveSamplesPerDomain,
                maxDiffMagnitude: 10d,
                forceAddMinPercentile: 0.2);*/

            /*
            // Without sampler - Only use for 10-fold cross validation!
            TestLogisticRegressionClassifierWithoutSampler(
                aggregator, 
                featuresConfig, 
                testingDataPath, 
                maxSamples: 100000);
                */

            /*
            var bestGuesser = new BestGuesser();

            CompareClassifiers(
                featuresConfig1: new FeaturesConfig()
                {
                    UseComplexNoVowelsFeature = true,
                    UseHostnamePatternMatchingFeature = false,
                    TruePositiveMaximumDistanceKilometers = 50
                },
                featuresConfig2: new FeaturesConfig()
                {
                    UseComplexNoVowelsFeature = true,
                    UseHostnamePatternMatchingFeature = true,
                    TruePositiveMaximumDistanceKilometers = 50
                },
                bestGuesser1: bestGuesser,
                bestGuesser2: bestGuesser,
                geonamesDataRootInputFolder: geonamesDataRootInputFolder,
                geonamesDatePrefix: geonamesDatePrefix,
                clliPath: clliPath,
                classifierDatePrefix: classifierDatePrefix,
                classifierFolder: classifierOutputFolder,
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                hostnamePatternsDatePrefix: hostnamePatternsDatePrefix,
                hostnamesPatternsRootFolder: hostnamesPatternsRootFolder,
                dataSubset: dataSubset,
                testingDataPath: testingDataPath,
                resultsPath: resultsDataPath);
                */

            /*
            var bestGuesser = new BestGuesser();

            var hostnames = GetSampleHostnames();

            DisplayHostameFeatures(
                featuresConfig: featuresConfig,
                bestGuesser: bestGuesser,
                geonamesDataRootInputFolder: geonamesDataRootInputFolder,
                geonamesDatePrefix: geonamesDatePrefix,
                clliPath: clliPath,
                classifierDatePrefix: classifierDatePrefix,
                classifierFolder: classifierOutputFolder,
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                hostnamePatternsDatePrefix: hostnamePatternsDatePrefix,
                hostnamesPatternsRootFolder: hostnamesPatternsRootFolder,
                dataSubset: dataSubset,
                hostnames: hostnames
                );*/

            /*
            var datasetParser = new GroundTruthParser();
            var maxMindEvaluation = new MaxMindEvaluation();
            maxMindEvaluation.Evaluate(datasetParser, inPath: testingDataPath);
            */

            /*
            var datasetParser = new GroundTruthParser();
            var ip2LocationEvaluation = new IP2LocationEvaluation();
            ip2LocationEvaluation.Evaluate(datasetParser, inPath: testingDataPath);
            */

            Console.WriteLine("Done!");
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
        }

        private static List<string> GetSampleHostnames()
        {
            return new List<string>()
            {
                /*
                "static-45-53-187-123.alen.id.frontiernet.net",
                "50-36-64-4.alma.mi.frontiernet.net",
                "static-50-40-60-166.bltn.il.frontiernet.net",
                "static-50-39-3-26.blyt.ca.frontiernet.net",
                "static-50-106-32-158.both.wa.frontiernet.net",
                "static-50-38-29-6.drr01.bvtn.or.frontiernet.net",
                "50-110-139-115.chtn.wv.frontiernet.net",
                "47-192-176-223.drr01.clwr.fl.frontiernet.net",
                "static-50-37-68-10.drr01.cral.id.frontiernet.net",
                "static-45-53-186-44.crdl.il.frontiernet.net",
                "static-50-44-206-0.crvl.il.frontiernet.net",
                "50-120-0-9.drr01.ekgv.ca.frontiernet.net",
                "static-45-53-0-11.ftwy.in.frontiernet.net ",
                "50-37-15-53.grdv.nv.frontiernet.net",
                "50-125-93-100.hllk.wa.frontiernet.net",
                "50-32-32-9.drr01.hrbg.pa.frontiernet.net",
                "static-47-190-17-9.irng.tx.frontiernet.net",
                "static-50-44-210-154.jrvl.il.frontiernet.net",
                "static-45-52-14-92.knwc.wa.frontiernet.net",
                "50-38-12-15.lagr.or.frontiernet.net",
                "47-192-0-17.drr01.lkld.fl.frontiernet.net",
                "50-48-39-226.dr04.mdtw.ny.frontiernet.net",
                "50-48-0-18.dsl1.monr.ny.frontiernet.net",
                "static-50-36-1-31.drr01.mybh.sc.frontiernet.net",
                "static-50-125-97-74.myvi.wa.frontiernet.net",
                "50-50-4-5.dr01.nwmd.wi.frontiernet.net",
                "static-50-125-99-146.rdmd.wa.frontiernet.net",
                "static-50-49-241-22.roch.ny.frontiernet.net",
                "static-50-39-12-16.ssvl.ca.frontiernet.net",
                "50-42-225-57.drr01.stbo.ga.frontiernet.net",
                "static-47-206-0-26.tamp.fl.frontiernet.net",
                "static-45-53-69-42.waus.wi.frontiernet.net",
                "static-32-213-18-68.wlfr.ct.frontiernet.net",
                "50-33-32-20.wyng.mn.frontiernet.net",
                "static-50-125-167-3.krld.wa.frontiernet.net",
                "static-50-125-96-54.mrwy.wa.frontiernet.net",
                "static-50-126-69-106.aloh.or.frontiernet.net",
                "static-50-126-80-6.mmvl.or.frontiernet.net",
                "65-37-111-106.dr1.tmn.ut.frontiernet.net",
                "65-73-128-196.dsl1.mon.ny.frontiernet.net",
                "65-73-219-152.bras01.mcl.id.frontiernet.net",
                "75-132-103-45.dhcp.stls.mo.charter.com",
                "24-151-249-130.dhcp.kgpt.tn.charter.com",
                "24-158-43-129.dhcp.hckr.nc.charter.com",
                */
                "66-188-196-227.dhcp.roch.mn.charter.com", // Rochester - MN - US     120.431624018752        Rochester - NY - US     1318.05405915951
                "71-80-61-56.dhcp.mant.nc.charter.com", //  Manteo - NC - US        582.362900534127        Mantorville - MN - US   1369.79233649264
                "71-91-80-50.dhcp.leds.al.charter.com", // Results 1 distance: 1458.52996331626    Results2 had no candidates
                "24-181-97-117.dhcp.leds.al.charter.com", // Results 1 distance: 1398.97966641205    Results2 had no candidates
                "97-90-205-107.dhcp.losa.ca.charter.com", // Results 1 distance: 1870.09827572754    Results2 had no candidates
                "24-159-152-140.dhcp.smrt.tn.charter.com", //Results 1 distance: 909.51963072249     Results2 had no candidates
                "75-137-138-78.dhcp.leds.al.charter.com", // Results 1 distance: 1393.65216569858    Results2 had no candidates
                "68-113-164-124.dhcp.plbg.ny.charter.com", // {Plattsburgh - NY - US} // {North Elba - NY - US}
                "71-80-61-56.dhcp.mant.nc.charter.com", // {Manteo - NC - US} // {Mantorville - MN - US}
                "97-88-57-240.dhcp.roch.mn.charter.com", // {Rochester - MN - US} // {Rochester - NY - US} // In the second case classifier picks Rochester - NY - US, even if ExactAdmin1 does not match. There is another candidate (Roch, MN) where it does match. The only other difference is that the population in NY is 200K, while in MN is 100K.
                "97-88-57-240.dhcp.roch.mn.charter.com", // {Kalamazoo - MI - US} // {Grand Rapids - MI - US}
                "68-118-201-248.dhcp.asfd.ct.charter.com", // classifier1 is wrong // {Ashford - AL - US} // {New Haven - CT - US}
                "66-215-173-28.dhcp.snbr.ca.charter.com" // classifier1 is wrong // {San Bruno - CA - US} // {San Bernardino - CA - US}
            };
        }

        private static void CompareClassifiers(
            FeaturesConfig featuresConfig1,
            FeaturesConfig featuresConfig2,
            IBestGuesser bestGuesser1,
            IBestGuesser bestGuesser2,
            string geonamesDataRootInputFolder,
            string geonamesDatePrefix,
            string clliPath,
            string unlocodePath,
            string classifierDatePrefix,
            string classifierFolder,
            string serializedTrainingDataFolder,
            string serializedTrainingDataDatePrefix,
            string hostnamePatternsDatePrefix,
            string hostnamesPatternsRootFolder,
            string dataSubset,
            string testingDataPath,
            string resultsPath)
        {
            var miner = new HostnamePatternMiner();
            var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");
            var reducedRules = PatternRulesSerializer.DeserializeReducedRules(patternsSerializationPath);

            var externalCityFeatureGenerators1 = new List<CityFeaturesGenerator>();

            if (featuresConfig1.UseHostnamePatternMatchingFeature)
            {
                externalCityFeatureGenerators1.Add(new HostnamePatternsFeaturesGenerator(featuresConfig: featuresConfig1, miner: miner, hostnamePatternRules: reducedRules));
            }

            var aggregator1 = new CityFeaturesAggregator(citiesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "cities1000.txt"),
                alternateNamesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "alternateNames.txt"),
                admin1Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin1CodesASCII.txt"),
                admin2Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin2Codes.txt"),
                countriesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "countryInfo.txt"),
                clliPath: clliPath,
                unlocodePath: unlocodePath,
                externalCityFeatureGenerators: externalCityFeatureGenerators1,
                featuresConfig: featuresConfig1);

            var externalCityFeatureGenerators2 = new List<CityFeaturesGenerator>();

            if (featuresConfig2.UseHostnamePatternMatchingFeature)
            {
                externalCityFeatureGenerators2.Add(new HostnamePatternsFeaturesGenerator(featuresConfig: featuresConfig2, miner: miner, hostnamePatternRules: reducedRules));
            }

            var aggregator2 = new CityFeaturesAggregator(
                citiesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "cities1000.txt"),
                alternateNamesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "alternateNames.txt"),
                admin1Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin1CodesASCII.txt"),
                admin2Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin2Codes.txt"),
                countriesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "countryInfo.txt"),
                clliPath: clliPath,
                unlocodePath: unlocodePath,
                externalCityFeatureGenerators: externalCityFeatureGenerators2,
                featuresConfig: featuresConfig2);

            var runner1 = GenerateRunner(
                featuresConfig: featuresConfig1,
                aggregator: aggregator1,
                classifierDatePrefix: classifierDatePrefix,
                classifierFolder: classifierFolder,
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                dataSubset: dataSubset);

            var runner2 = GenerateRunner(
                featuresConfig: featuresConfig2,
                aggregator: aggregator2,
                classifierDatePrefix: classifierDatePrefix,
                classifierFolder: classifierFolder,
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                dataSubset: dataSubset);

            var datasetParser = new GroundTruthParser();

            TestClassifiersOnHostnames(
                datasetParser, 
                inPath: testingDataPath, 
                runner1: runner1, 
                runner2: runner2,
                bestGuesser1: bestGuesser1,
                bestGuesser2: bestGuesser2,
                outPath: resultsPath, 
                minProbability: 0);
        }

        private static void DisplayHostameFeatures(
                    FeaturesConfig featuresConfig,
                    IBestGuesser bestGuesser,
                    string geonamesDataRootInputFolder,
                    string geonamesDatePrefix,
                    string clliPath,
                    string unlocodePath,
                    string classifierDatePrefix,
                    string classifierFolder,
                    string serializedTrainingDataFolder,
                    string serializedTrainingDataDatePrefix,
                    string hostnamePatternsDatePrefix,
                    string hostnamesPatternsRootFolder,
                    string dataSubset,
                    List<string> hostnames,
                    double minProbability = 0)
        {
            var miner = new HostnamePatternMiner();
            var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");
            var reducedRules = PatternRulesSerializer.DeserializeReducedRules(patternsSerializationPath);

            HostnamePatternsFeaturesGenerator patternsGenerator = null;
            var externalCityFeatureGenerators = new List<CityFeaturesGenerator>();

            if (featuresConfig.UseHostnamePatternMatchingFeature)
            {
                patternsGenerator = new HostnamePatternsFeaturesGenerator(featuresConfig: featuresConfig, miner: miner, hostnamePatternRules: reducedRules);
                externalCityFeatureGenerators.Add(patternsGenerator);
            }

            var aggregator = new CityFeaturesAggregator(citiesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "cities1000.txt"),
                alternateNamesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "alternateNames.txt"),
                admin1Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin1CodesASCII.txt"),
                admin2Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin2Codes.txt"),
                countriesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "countryInfo.txt"),
                clliPath: clliPath,
                unlocodePath: unlocodePath,
                externalCityFeatureGenerators: externalCityFeatureGenerators,
                featuresConfig: featuresConfig);

            var runner = GenerateRunner(
                featuresConfig: featuresConfig,
                aggregator: aggregator,
                classifierDatePrefix: classifierDatePrefix,
                classifierFolder: classifierFolder,
                serializedTrainingDataFolder: serializedTrainingDataFolder,
                serializedTrainingDataDatePrefix: serializedTrainingDataDatePrefix,
                dataSubset: dataSubset);

            var datasetParser = new GroundTruthParser();

            foreach (var hostname in hostnames)
            {
                Console.WriteLine("-----------------------------------");
                Console.WriteLine($"Processing hostname: {hostname}");

                var results = runner.ExtractCities(hostname);

                var bestResult = results.Count == 0 ? null : bestGuesser.PickBest(hostname, results, minProbability);

                if (bestResult == null)
                {
                    Console.WriteLine("bestResult was null");
                }
                else
                {
                    Console.WriteLine($"Best result: {bestResult.City}");
                    Console.WriteLine("---");

                    foreach (var feature in bestResult.NonDefaultFeatures)
                    {
                        Console.WriteLine($"{feature.Key} - {feature.Value}");
                    }
                }
            }
        }

        private static ModelRunner GenerateRunner(
            FeaturesConfig featuresConfig,
            CityFeaturesAggregator aggregator,
            string classifierDatePrefix,
            string classifierFolder,
            string serializedTrainingDataFolder,
            string serializedTrainingDataDatePrefix,
            string dataSubset)
        {
            var serializedTrainingPath = Path.Combine(serializedTrainingDataFolder, $"{serializedTrainingDataDatePrefix}-{featuresConfig}-training-data{dataSubset}.bin");
            var trainingData = TrainingData.DeserializeFrom(serializedTrainingPath, aggregator);

            var logisticClassifierPath = Path.Combine(classifierFolder, $"{classifierDatePrefix}-{featuresConfig}-logistic-classifier{dataSubset}.bin");
            var logisticClassifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities, serializedClassifierPath: logisticClassifierPath);

            var runner = new ModelRunner(aggregator, trainingData, logisticClassifier, debugMode: false);

            return runner;
        }

        private static void TestClassifiersOnHostnames(
            IDataSetParser datasetParser, 
            string inPath, 
            ModelRunner runner1,
            ModelRunner runner2,
            IBestGuesser bestGuesser1,
            IBestGuesser bestGuesser2,
            string outPath,
            double minProbability = 0)
        {
            var total = 0;
            var bothNull = 0;

            var distance1MoreThanDistance2 = 0;
            var distance1EqualDistance2 = 0;
            var distance1LessThanDistance2 = 0;

            var res1NullAndDistance2Smaller80 = 0;
            var res2NullAndDistance1Smaller80 = 0;

            var distance1Greater500 = 0;
            var distance2Greater500 = 0;

            var nonNullLocationsSame = 0;
            var nonNullLocationsDifferent = 0;

            using (var writer = new StreamWriter(outPath))
            {
                foreach (var item in datasetParser.Parse(inPath, populateTextualLocationInfo: true))
                {
                    var hostname = item.Hostname;

                    var results1 = runner1.ExtractCities(hostname);
                    var results2 = runner2.ExtractCities(hostname);

                    var bestResult1 = results1.Count == 0 ? null : bestGuesser1.PickBest(hostname, results1, minProbability);
                    var bestResult2 = results2.Count == 0 ? null : bestGuesser2.PickBest(hostname, results2, minProbability);

                    total++;

                    if (bestResult1 == null && bestResult2 == null)
                    {
                        // Display nothing
                        bothNull++;
                    }
                    else if (bestResult1 != null && bestResult2 == null)
                    {
                        var distance1 = DistanceHelper.Distance(item.Latitude, item.Longitude, bestResult1.City.Latitude, bestResult1.City.Longitude, DistanceUnit.Kilometer);
                        //Console.WriteLine($"{hostname}\tResults 1 distance: {distance1}\tResults2 had no candidates");

                        if (distance1 <= 80)
                        {
                            res2NullAndDistance1Smaller80++;
                        }

                        if (distance1 > 500)
                        {
                            distance1Greater500++;
                        }
                    }
                    else if (bestResult1 == null && bestResult2 != null)
                    {
                        var distance2 = DistanceHelper.Distance(item.Latitude, item.Longitude, bestResult2.City.Latitude, bestResult2.City.Longitude, DistanceUnit.Kilometer);
                        //Console.WriteLine($"{hostname}\tResults1 had no candidates\tResults 2 distance: {distance2}");

                        if (distance2 <= 80)
                        {
                            res1NullAndDistance2Smaller80++;
                        }

                        if (distance2 > 500)
                        {
                            distance2Greater500++;
                        }
                    }
                    else if (bestResult1 != null && bestResult2 != null)
                    {
                        var distance1 = DistanceHelper.Distance(item.Latitude, item.Longitude, bestResult1.City.Latitude, bestResult1.City.Longitude, DistanceUnit.Kilometer);
                        var distance2 = DistanceHelper.Distance(item.Latitude, item.Longitude, bestResult2.City.Latitude, bestResult2.City.Longitude, DistanceUnit.Kilometer);

                        if (distance1 > 500)
                        {
                            distance1Greater500++;
                        }

                        if (distance2 > 500)
                        {
                            distance2Greater500++;
                        }

                        if (distance1 > distance2)
                        {
                            distance1MoreThanDistance2++;
                        }
                        else if (distance1 == distance2)
                        {
                            distance1EqualDistance2++;
                        }
                        else if (distance1 < distance2)
                        {
                            distance1LessThanDistance2++;
                        }

                        if (bestResult1.City?.Id == bestResult2.City?.Id)
                        {
                            nonNullLocationsSame++;
                        }
                        else
                        {
                            nonNullLocationsDifferent++;
                            //Console.WriteLine($"{hostname}\t{bestResult1.City}\t{distance1}\t{bestResult2.City}\t{distance2}");
                        }
                    }

                    if (total % 10000 == 0)
                    {
                        PrintStatsTestClassifiers(
                                    total,
                                    bothNull,
                                    distance1MoreThanDistance2,
                                    distance1EqualDistance2,
                                    distance1LessThanDistance2,
                                    res1NullAndDistance2Smaller80,
                                    res2NullAndDistance1Smaller80,
                                    distance1Greater500,
                                    distance2Greater500,
                                    nonNullLocationsSame,
                                    nonNullLocationsDifferent);
                    }
                }
            }

            PrintStatsTestClassifiers(
                        total,
                        bothNull,
                        distance1MoreThanDistance2,
                        distance1EqualDistance2,
                        distance1LessThanDistance2,
                        res1NullAndDistance2Smaller80,
                        res2NullAndDistance1Smaller80,
                        distance1Greater500,
                        distance2Greater500,
                        nonNullLocationsSame,
                        nonNullLocationsDifferent);

            /*
            Console.WriteLine("Best guess results");
            Console.WriteLine($"distanceGood = {distanceGood}, distanceBad = {distanceBad}, good/total = {Math.Round(distanceGood / (1.0 * total), 2) * 100}");
            DisplayStats(realDistanceBuckets);
            Console.WriteLine("Smallest theoretical distance results");
            DisplayStats(bestCaseDistanceBuckets);
            */
        }

        private static void PrintStatsTestClassifiers(
            int total,
            int bothNull,
            int distance1MoreThanDistance2,
            int distance1EqualDistance2,
            int distance1LessThanDistance2,
            int res1NullAndDistance2Smaller80,
            int res2NullAndDIstance1Smaller80,
            int distance1Greater500,
            int distance2Greater500,
            int nonNullLocationsSame,
            int nonNullLocationsDifferent)
        {
            Console.WriteLine($"total = {total}");
            Console.WriteLine($"bothNull = {bothNull}");
            Console.WriteLine($"distance1MoreThanDistance2 = {distance1MoreThanDistance2}");
            Console.WriteLine($"distance1EqualDistance2 = {distance1EqualDistance2}");
            Console.WriteLine($"distance1LessThanDistance2 = {distance1LessThanDistance2}");
            Console.WriteLine($"res1NullAndDistance2Smaller80 = {res1NullAndDistance2Smaller80}");
            Console.WriteLine($"res2NullAndDistance1Smaller80 = {res2NullAndDIstance1Smaller80}");
            Console.WriteLine($"distance1Greater500 = {distance1Greater500}");
            Console.WriteLine($"distance2Greater500 = {distance2Greater500}");
            Console.WriteLine($"nonNullLocationsSame = {nonNullLocationsSame}");
            Console.WriteLine($"nonNullLocationsDifferent = {nonNullLocationsDifferent}");
        }

        private static void CreateSerializeTrainingData(
            CityFeaturesAggregator aggregator,
            FeaturesConfig featuresConfig,
            string groundTruthTrainingPath,
            string serializedTrainingDataFolder,
            string serializedTrainingDataDatePrefix,
            int maxPositiveSamplesPerDomain,
            double maxDiffMagnitude,
            double forceAddMinPercentile,
            string dataSubset)
        {
            var serializedTrainingPath = Path.Combine(serializedTrainingDataFolder, $"{serializedTrainingDataDatePrefix}-{featuresConfig}-training-data{dataSubset}.bin");

            SampleGenerateTrainingData(aggregator, featuresConfig, groundTruthTrainingPath, serializedTrainingPath, maxPositiveSamplesPerDomain, maxDiffMagnitude, forceAddMinPercentile);
        }

        private static void TrainClassifierFromSerializedTrainingData(
            CityFeaturesAggregator aggregator,
            FeaturesConfig featuresConfig,
            string classifierDatePrefix,
            string serializedTrainingDataFolder,
            string serializedTrainingDataDatePrefix,
            string classifierOutputFolder,
            string dataSubset)
        {
            var serializedTrainingPath = Path.Combine(serializedTrainingDataFolder, $"{serializedTrainingDataDatePrefix}-{featuresConfig}-training-data{dataSubset}.bin");
            var trainingData = TrainingData.DeserializeFrom(serializedTrainingPath, aggregator);

            var logisticClassifierPath = Path.Combine(classifierOutputFolder, $"{classifierDatePrefix}-{featuresConfig}-logistic-classifier{dataSubset}.bin");
            var logisticClassifier = TrainLogisticClassifier(aggregator, trainingData);
            SaveLogisticClassifier(logisticClassifier, logisticClassifierPath);
        }

        private static void TrainC45ClassifierFromSerializedTrainingData(
            CityFeaturesAggregator aggregator,
            FeaturesConfig featuresConfig,
            string classifierDatePrefix,
            string serializedTrainingDataFolder,
            string serializedTrainingDataDatePrefix,
            string classifierOutputFolder,
            string dataSubset)
        {
            var serializedTrainingPath = Path.Combine(serializedTrainingDataFolder, $"{serializedTrainingDataDatePrefix}-{featuresConfig}-training-data{dataSubset}.bin");
            var trainingData = TrainingData.DeserializeFrom(serializedTrainingPath, aggregator);

            var c45ClassifierPath = Path.Combine(classifierOutputFolder, $"{classifierDatePrefix}-{featuresConfig}-c45-classifier{dataSubset}.bin");
            var c45Classifier = TrainC45Classifier(aggregator, trainingData);
            SaveC45Classifier(c45Classifier, c45ClassifierPath);
        }

        private static void TestClassifier(
            CityFeaturesAggregator aggregator,
            FeaturesConfig featuresConfig,
            string classifierDatePrefix,
            string classifierFolder,
            string serializedTrainingDataFolder,
            string serializedTrainingDataDatePrefix,
            string hostnamePatternsDatePrefix,
            string hostnamesPatternsRootFolder,
            string dataSubset,
            string testingDataPath,
            string resultsPath,
            string featureDistributionDataPath,
            double minProbability = 0)
        {
            var serializedTrainingPath = Path.Combine(serializedTrainingDataFolder, $"{serializedTrainingDataDatePrefix}-{featuresConfig}-training-data{dataSubset}.bin");
            var trainingData = TrainingData.DeserializeFrom(serializedTrainingPath, aggregator);

            var logisticClassifierPath = Path.Combine(classifierFolder, $"{classifierDatePrefix}-{featuresConfig}-logistic-classifier{dataSubset}.bin");
            var logisticClassifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities, serializedClassifierPath: logisticClassifierPath);

            var runner = new ModelRunner(aggregator, trainingData, logisticClassifier, debugMode: false);

            var miner = new HostnamePatternMiner();

            var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");
            var reducedRules = PatternRulesSerializer.DeserializeReducedRules(patternsSerializationPath);

            var bestGuesser = new BestGuesser();
            //var bestGuesser = new PatternMiningBestGuesser(miner, reducedRules, distanceThresholdKm: 100, forceIntersect: true);
            //var bestGuesser = new PatternMiningBestGuesserWithRadius(miner, reducedRules, distanceThresholdKm: 300);
            //var bestGuesser = new PatternMiningBestGuesser(miner, reducedRules, distanceThresholdKm: 100, forceIntersect: false);

            var datasetParser = new GroundTruthParser();

            TestClassifierOnHostnames(datasetParser, inPath: testingDataPath, runner: runner, outPath: resultsPath, featureDistributionDataPath: featureDistributionDataPath, bestGuesser: bestGuesser, minProbability: minProbability);
        }

        private static void TestC45Classifier(
            CityFeaturesAggregator aggregator,
            FeaturesConfig featuresConfig,
            string classifierDatePrefix,
            string classifierFolder,
            string serializedTrainingDataFolder,
            string serializedTrainingDataDatePrefix,
            string hostnamePatternsDatePrefix,
            string hostnamesPatternsRootFolder,
            string dataSubset,
            string testingDataPath,
            string resultsPath,
            string featureDistributionDataPath,
            double minProbability = 0)
        {
            var serializedTrainingPath = Path.Combine(serializedTrainingDataFolder, $"{serializedTrainingDataDatePrefix}-{featuresConfig}-training-data{dataSubset}.bin");
            var trainingData = TrainingData.DeserializeFrom(serializedTrainingPath, aggregator);

            var c45ClassifierPath = Path.Combine(classifierFolder, $"{classifierDatePrefix}-{featuresConfig}-c45-classifier{dataSubset}.bin");
            var c45Classifier = new C45Classifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities, serializedClassifierPath: c45ClassifierPath);

            var runner = new ModelRunner(aggregator, trainingData, c45Classifier, debugMode: false);

            var miner = new HostnamePatternMiner();

            var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");
            var reducedRules = PatternRulesSerializer.DeserializeReducedRules(patternsSerializationPath);

            var bestGuesser = new BestGuesser();
            //var bestGuesser = new PatternMiningBestGuesser(miner, reducedRules, distanceThreshold: 100, forceIntersect: true);
            //var bestGuesser = new PatternMiningBestGuesser(miner, reducedRules, distanceThresholdKm: 100, forceIntersect: false);

            var datasetParser = new GroundTruthParser();

            TestClassifierOnHostnames(datasetParser, inPath: testingDataPath, runner: runner, outPath: resultsPath, featureDistributionDataPath: featureDistributionDataPath, bestGuesser: bestGuesser, minProbability: minProbability);
        }

        private static void TrainTestClassifier()
        {
            ////PrintFeaturesForHostname(hostname: "amsterdamnl.seattlewa.ce-salmor0w03w.cpe.or.portland.comcast.net");
            ////TestInternationalHostnames();

            /*
            TestC45Classifier(groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", printRules: true, featuresConfig: new FeaturesConfig()
            {
                ////UseSlotIndex = true,
                ////UseAlternateNameCategories = false
                UseAlternateNamesCount = true
            });
            */



            /*
            var outputFolder = @"C:\Projects\ReverseDNS\SavedClassifiers\";
            var c45ClassifierPath = Path.Combine(outputFolder, "2017-09-30-c45-classifier.bin");
            var logisticClassifierPath = Path.Combine(outputFolder, "2017-10-08-logistic-classifier.bin");
            */

            //var c45Bundle = TrainSaveC45Classifier(c45ClassifierPath, featuresConfig: featuresConfig);
            ////var logisticBundle = TrainSaveLogisticRegressionClassifier(logisticClassifierPath, featuresConfig: featuresConfig);
            //TrainSaveLogisticRegressionClassifier(logisticClassifierPath, featuresConfig: featuresConfig);

            /*
            var hostnames = new List<string>();
            string line;

            using (var file = new StreamReader(@"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier.txt"))
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

                    hostnames.Add(parts[3]);
                }
            }
            */

            /*
            var hostnames = new List<string>()
            {
                "static-45-53-187-123.alen.id.frontiernet.net",
                "50-36-64-4.alma.mi.frontiernet.net",
                "static-50-40-60-166.bltn.il.frontiernet.net",
                "static-50-39-3-26.blyt.ca.frontiernet.net",
                "static-50-106-32-158.both.wa.frontiernet.net",
                "static-50-38-29-6.drr01.bvtn.or.frontiernet.net",
                "50-110-139-115.chtn.wv.frontiernet.net",
                "47-192-176-223.drr01.clwr.fl.frontiernet.net",
                "static-50-37-68-10.drr01.cral.id.frontiernet.net",
                "static-45-53-186-44.crdl.il.frontiernet.net",
                "static-50-44-206-0.crvl.il.frontiernet.net",
                "50-120-0-9.drr01.ekgv.ca.frontiernet.net",
                "static-45-53-0-11.ftwy.in.frontiernet.net ",
                "50-37-15-53.grdv.nv.frontiernet.net",
                "50-125-93-100.hllk.wa.frontiernet.net",
                "50-32-32-9.drr01.hrbg.pa.frontiernet.net",
                "static-47-190-17-9.irng.tx.frontiernet.net",
                "static-50-44-210-154.jrvl.il.frontiernet.net",
                "static-45-52-14-92.knwc.wa.frontiernet.net",
                "50-38-12-15.lagr.or.frontiernet.net",
                "47-192-0-17.drr01.lkld.fl.frontiernet.net",
                "50-48-39-226.dr04.mdtw.ny.frontiernet.net",
                "50-48-0-18.dsl1.monr.ny.frontiernet.net",
                "static-50-36-1-31.drr01.mybh.sc.frontiernet.net",
                "static-50-125-97-74.myvi.wa.frontiernet.net",
                "50-50-4-5.dr01.nwmd.wi.frontiernet.net",
                "static-50-125-99-146.rdmd.wa.frontiernet.net",
                "static-50-49-241-22.roch.ny.frontiernet.net",
                "static-50-39-12-16.ssvl.ca.frontiernet.net",
                "50-42-225-57.drr01.stbo.ga.frontiernet.net",
                "static-47-206-0-26.tamp.fl.frontiernet.net",
                "static-45-53-69-42.waus.wi.frontiernet.net",
                "static-32-213-18-68.wlfr.ct.frontiernet.net",
                "50-33-32-20.wyng.mn.frontiernet.net",
                "static-50-125-167-3.krld.wa.frontiernet.net",
                "static-50-125-96-54.mrwy.wa.frontiernet.net",
                "static-50-126-69-106.aloh.or.frontiernet.net",
                "static-50-126-80-6.mmvl.or.frontiernet.net",
                "65-37-111-106.dr1.tmn.ut.frontiernet.net",
                "65-73-128-196.dsl1.mon.ny.frontiernet.net",
                "65-73-219-152.bras01.mcl.id.frontiernet.net"
            };
            */

            //TestC45ClassifierOnHostnames(hostnames, serializedClassifierPath: c45ClassifierPath);
            //TestC45ClassifierOnHostnames(hostnames, c45Bundle);

            ////TestLogisticRegressionClassifierOnHostnames(hostnames, serializedClassifierPath: logisticClassifierPath, featuresConfig: featuresConfig);

            //TestC45ClassifierOnHostname("pl1409.nas81e.soka.nttpc.ne.jp", serializedClassifierPath: classifierPath);

            ////TestLogisticRegressionClassifier();

            ////TestLogisticRegressionClassifierOnGT(groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier.txt", serializedClassifierPath: logisticClassifierPath, debugMode: false);

            /*
            TestRandomForestClassifier(groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", printRules: true, featuresConfig: new FeaturesConfig()
            {
                ////UseSlotIndex = true,
                ////UseAlternateNameCategories = false
                UseAlternateNamesCount = true
            });
            */

            //var classifierDatePrefix = "2017-10-16";
            //var classifierDatePrefix = "2017-10-17-qwest";
            var classifierDatePrefix = "2017-10-16";
            var trainingDataDatePrefix = "2017-10-16";

            var featuresConfig = new FeaturesConfig()
            {
                UseComplexNoVowelsFeature = true,
                /*UseSlotIndex = true*/
            };

            var aggregator = new CityFeaturesAggregator(citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt",
                featuresConfig: featuresConfig);

            var intermediaryDataOutputFolder = @"C:\Projects\ReverseDNS\SerializedIntermediaryData\";
            var serializedTrainingPath = Path.Combine(intermediaryDataOutputFolder, $"{trainingDataDatePrefix}-training-data.bin");

            ////////////////// !!!!! Generate raw classifier training (inputs - aka features - is double[][] and outputs - aka labels - is int[])
            ////var groundTruthPath = @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt";
            var groundTruthPath = @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-training.txt";
            ////SampleGenerateTrainingData(aggregator, featuresConfig, groundTruthPath, serializedTrainingPath);

            var trainingData = TrainingData.DeserializeFrom(serializedTrainingPath, aggregator);

            var classifierOutputFolder = @"C:\Projects\ReverseDNS\SavedClassifiers\";

            var logisticClassifierPath = Path.Combine(classifierOutputFolder, $"{classifierDatePrefix}-logistic-classifier.bin");
            //var logisticClassifier = TrainLogisticClassifier(aggregator, trainingData);
            //SaveLogisticClassifier(logisticClassifier, logisticClassifierPath);
            var logisticClassifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities, serializedClassifierPath: logisticClassifierPath);

            /*
            var c45ClassifierPath = Path.Combine(classifierOutputFolder, $"{datePrefix}-c45-classifier.bin");
            //var c45Classifier = TrainC45Classifier(aggregator, trainingData);
            //SaveC45Classifier(c45Classifier, c45ClassifierPath);
            var c45Classifier = new C45Classifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities, serializedClassifierPath: c45ClassifierPath);
            */

            /*
            var randomForestClassifierPath = Path.Combine(classifierOutputFolder, $"{datePrefix}-random-forest-classifier.bin");
            //var randomForestClassifier = TrainRandomForestClassifier(aggregator, trainingData);
            //SaveRandomForestClassifier(randomForestClassifier, randomForestClassifierPath);
            var randomForestClassifier = new RandomForestClassifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities, serializedClassifierPath: randomForestClassifierPath);
            */

            /*
            var svmClassifierPath = Path.Combine(classifierOutputFolder, $"{datePrefix}-svm-classifier.bin");
            var svmClassifier = TrainSVMClassifier(aggregator, trainingData);
            SaveSVMClassifier(svmClassifier, svmClassifierPath);
            //var svmClassifier = new SVMClassifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities, serializedClassifierPath: svmClassifierPath);
            */

            var hostnames = new List<string>()
            {
                "static-45-53-187-123.alen.id.frontiernet.net",
                "50-36-64-4.alma.mi.frontiernet.net",
                "static-50-40-60-166.bltn.il.frontiernet.net",
                "static-50-39-3-26.blyt.ca.frontiernet.net",
                "static-50-106-32-158.both.wa.frontiernet.net",
                "static-50-38-29-6.drr01.bvtn.or.frontiernet.net",
                "50-110-139-115.chtn.wv.frontiernet.net",
                "47-192-176-223.drr01.clwr.fl.frontiernet.net",
                "static-50-37-68-10.drr01.cral.id.frontiernet.net",
                "static-45-53-186-44.crdl.il.frontiernet.net",
                "static-50-44-206-0.crvl.il.frontiernet.net",
                "50-120-0-9.drr01.ekgv.ca.frontiernet.net",
                "static-45-53-0-11.ftwy.in.frontiernet.net ",
                "50-37-15-53.grdv.nv.frontiernet.net",
                "50-125-93-100.hllk.wa.frontiernet.net",
                "50-32-32-9.drr01.hrbg.pa.frontiernet.net",
                "static-47-190-17-9.irng.tx.frontiernet.net",
                "static-50-44-210-154.jrvl.il.frontiernet.net",
                "static-45-52-14-92.knwc.wa.frontiernet.net",
                "50-38-12-15.lagr.or.frontiernet.net",
                "47-192-0-17.drr01.lkld.fl.frontiernet.net",
                "50-48-39-226.dr04.mdtw.ny.frontiernet.net",
                "50-48-0-18.dsl1.monr.ny.frontiernet.net",
                "static-50-36-1-31.drr01.mybh.sc.frontiernet.net",
                "static-50-125-97-74.myvi.wa.frontiernet.net",
                "50-50-4-5.dr01.nwmd.wi.frontiernet.net",
                "static-50-125-99-146.rdmd.wa.frontiernet.net",
                "static-50-49-241-22.roch.ny.frontiernet.net",
                "static-50-39-12-16.ssvl.ca.frontiernet.net",
                "50-42-225-57.drr01.stbo.ga.frontiernet.net",
                "static-47-206-0-26.tamp.fl.frontiernet.net",
                "static-45-53-69-42.waus.wi.frontiernet.net",
                "static-32-213-18-68.wlfr.ct.frontiernet.net",
                "50-33-32-20.wyng.mn.frontiernet.net",
                "static-50-125-167-3.krld.wa.frontiernet.net",
                "static-50-125-96-54.mrwy.wa.frontiernet.net",
                "static-50-126-69-106.aloh.or.frontiernet.net",
                "static-50-126-80-6.mmvl.or.frontiernet.net",
                "65-37-111-106.dr1.tmn.ut.frontiernet.net",
                "65-73-128-196.dsl1.mon.ny.frontiernet.net",
                "65-73-219-152.bras01.mcl.id.frontiernet.net"
            };

            var runner = new ModelRunner(aggregator, trainingData, logisticClassifier, debugMode: false);
            //var runner = new ModelRunner(aggregator, trainingData, c45Classifier, debugMode: false);
            //var runner = new ModelRunner(aggregator, trainingData, randomForestClassifier, debugMode: false);
            //var runner = new ModelRunner(aggregator, trainingData, svmClassifier, debugMode: false);

            var patternsSerializationPath = @"C:\Projects\ReverseDNS\ReverseDNSDataset\2018-03-06-HostnamePatternigMining-ReducedRules.bin";
            var miner = new HostnamePatternMiner();
            var reducedRules = PatternRulesSerializer.DeserializeReducedRules(patternsSerializationPath);

            var bestGuesser = new BestGuesser();
            //var bestGuesser = new PatternMiningBestGuesser(miner, reducedRules, distanceThreshold: 100, forceIntersect: true);

            var datasetParser = new GroundTruthParser();

            TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier.txt", runner: runner, outPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier-rdns\2017-01-18-ReverseDNS-WithGT-frontier-rdns.txt", featureDistributionDataPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier-rdns\2017-01-18-ReverseDNS-WithGT-frontier-featureDistribution-rdns.txt", bestGuesser: bestGuesser, minProbability: 0);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-qwest.txt", runner: runner, outPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-qwest-rdns\2017-01-18-ReverseDNS-WithGT-qwest-rdns.txt", bestGuesser: bestGuesser, minProbability: 0);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-verizon.txt", runner: runner, outPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-verizon-rdns\2017-01-18-ReverseDNS-WithGT-verizon-rdns.txt", bestGuesser: bestGuesser, minProbability: 0);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-charter.txt", runner: runner, outPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-charter-rdns\2017-01-18-ReverseDNS-WithGT-charter-rdns.txt", bestGuesser: bestGuesser, minProbability: 0);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-level3.txt", runner: runner, outPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-level3-rdns\2017-01-18-ReverseDNS-WithGT-level3-rdns.txt", minProbability: 0);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-comcast.txt", runner: runner, outPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-comcast-rdns\2017-01-18-ReverseDNS-WithGT-comcast-rdns.txt", minProbability: 0);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-nttpc-jp.txt", runner: runner, outPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-nttpc-jp-rdns\2017-01-18-ReverseDNS-WithGT-nttpc-jp-rdns.txt", bestGuesser: bestGuesser, minProbability: 0);

            //var occurrences = CountDomainOccurrencesInGT(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt");
            //PrintDomainOccurrencesInGt(occurrences);

            /*
            //var occurrences = miner.MineCommonStringsFromGT(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-nttpc-jp.txt");
            var occurrences = miner.MineCommonStringsFromGT(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt");
            PrintOccurences(occurrences, topToShowPerDomain: 20);
            */

            /*
            var rawRules = miner.MineCommonStringGeohashesFromGT(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier.txt");
            // TODO: Use default values for minimumRuleSupport and minimimRuleOccFraction once we implement more sophisticated FindBestCoordinates?
            var filteredRawRules = miner.FilterRules(rawRules, minimumRuleSupport: 500, minimimRuleOccFraction: 0.8);
            var reducedRules = miner.ReduceRules(filteredRawRules);
            PatternRulesSerializer.SerializeReducedRules(reducedRules, outputPath: patternsSerializationPath);
            */

            //var occurrences = miner.MineCommonStringGeohashesFromGT(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-qwest.txt");
            //var occurrences = miner.MineCommonStringGeohashesFromGT(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-verizon.txt");
            //var occurrences = miner.MineCommonStringGeohashesFromGT(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt");

            // !! Setting minProbability to 0.9 yields too few results for some of the datasets (for example QWEST)
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier.txt", runner: runner, minProbability: 0.9);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-qwest.txt", runner: runner, minProbability: 0.9);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-verizon.txt", runner: runner, minProbability: 0.9);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-charter.txt", runner: runner, minProbability: 0.9);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-level3.txt", runner: runner, minProbability: 0.9);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-comcast.txt", runner: runner, minProbability: 0.9);
            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-nttpc-jp.txt", runner: runner, minProbability: 0.9);

            //TestClassifierOnHostnames(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", runner: runner, minProbability: 0.95);

            /*
            foreach (var hostname in hostnames)
            {
                Console.WriteLine($"Running classifier on hostname: {hostname}");

                var matchingCities = runner.ExtractCities(hostname);

                Console.WriteLine("----------------------------");
                Console.ReadKey();
            }
            */

            ///var datasetParser = new GroundTruthParser();

            /*
            var maxMindEvaluation = new MaxMindEvaluation();
            maxMindEvaluation.Evaluate(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-qwest.txt");
            */

            /*
            var ip2LocationEvaluation = new IP2LocationEvaluation();
            ip2LocationEvaluation.Evaluate(datasetParser, inPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier.txt");
            */

            Console.WriteLine("Done!");
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
        }

        private static void TrainHostnamePatterns(
            string trainingDataPath, 
            string hostnamePatternsDatePrefix,
            string hostnamesPatternsRootFolder,
            string geonamesDatePrefix,
            string geonamesDataRootInputFolder,
            string clliPath,
            string unlocodePath,
            string dataSubset,
            int minimumRuleSupport = 500,
            double minimimRuleOccFraction = 0.7,
            int pruneIntervalCount = 10000,
            int pruneMinKeepThreshold = 10)
        {
            var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");

            var datasetParser = new GroundTruthParser();
            var miner = new HostnamePatternMiner();

            var rawRules = miner.MineCommonStringGeohashesFromGT(
                datasetParser, 
                trainingDataPath,
                pruneIntervalCount: pruneIntervalCount,
                pruneMinKeepThreshold: pruneMinKeepThreshold);

            // TODO: Use default values for minimumRuleSupport and minimimRuleOccFraction once we implement more sophisticated FindBestCoordinates?
            var filteredRawRules = miner.FilterRules(
                rawRules, 
                minimumRuleSupport: minimumRuleSupport, 
                minimimRuleOccFraction: minimimRuleOccFraction);

            var reducedRules = miner.ReduceRules(filteredRawRules);

            var closestCityFinder = new PatternMiningClosestCityFinder(
                citiesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "cities1000.txt"),
                alternateNamesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "alternateNames.txt"),
                admin1Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin1CodesASCII.txt"),
                admin2Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin2Codes.txt"),
                countriesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "countryInfo.txt"),
                clliPath: clliPath,
                unlocodePath: unlocodePath);

            closestCityFinder.FindClosestCitiesForRules(reducedRules);

            PatternRulesSerializer.SerializeReducedRules(reducedRules, outputPath: patternsSerializationPath);
        }

        /*
        private static void TrainHostnamePatternClusters(
            string trainingDataPath, 
            string hostnamePatternsDatePrefix,
            string hostnamesPatternsRootFolder,
            string dataSubset,
            int minimumRuleSupport = 500,
            double minimimRuleOccFraction = 0.7,
            int pruneIntervalCount = 10000,
            int pruneMinKeepThreshold = 10)
        {
            var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");

            var datasetParser = new GroundTruthParser();
            var miner = new HostnamePatternClustersMiner();

            var domainsToRulesToCentroids = miner.MinePatternsFromGT(
                datasetParser,
                trainingDataPath,
                minRuleOcc: 200,
                clusterThresholdKm: 20,
                minItemsPerCluster: 50,
                minSupportRatioPerCluster: 0.4d);

            // TODO: This code is incomplete and it is also missing mapping locations to the closest city

            Console.WriteLine(domainsToRulesToCentroids);
        }
        */

        private static void TrainHostnamePatternCoarseClusters(
            string trainingDataPath,
            string hostnamePatternsDatePrefix,
            string hostnamesPatternsRootFolder,
            string geonamesDatePrefix,
            string geonamesDataRootInputFolder,
            string clliPath,
            string unlocodePath,
            string dataSubset,
            int minRuleOcc = 200,
            int clusterThresholdKm = 20,
            int minItemsPerCluster = 50,
            double minSupportRatioPerCluster = 0.4d,
            int pruneIntervalCountPerDomain = 10000,
            int pruneMinKeepThreshold = 2)
        {
            var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");

            var datasetParser = new GroundTruthParser();
            var miner = new HostnamePatternCoarseClustersMiner();

            var domainsToRulesToCentroids = miner.MinePatternsFromGT(
                datasetParser,
                trainingDataPath,
                minRuleOcc: minRuleOcc,
                clusterThresholdKm: clusterThresholdKm,
                minItemsPerCluster: minItemsPerCluster,
                minSupportRatioPerCluster: minSupportRatioPerCluster,
                pruneIntervalCountPerDomain: pruneIntervalCountPerDomain,
                pruneMinKeepThreshold: pruneMinKeepThreshold);

            var finalResults = new Dictionary<string, Dictionary<PatternRule, PatternMiningCoordinates>>();

            foreach (var domainsToRulesToCentroidsEntry in domainsToRulesToCentroids)
            {
                var domain = domainsToRulesToCentroidsEntry.Key;
                var rulesToCentroids = domainsToRulesToCentroidsEntry.Value;

                Dictionary<PatternRule, PatternMiningCoordinates> finalRulesToCoordinates;

                if (!finalResults.TryGetValue(domain, out finalRulesToCoordinates))
                {
                    finalRulesToCoordinates = new Dictionary<PatternRule, PatternMiningCoordinates>();
                    finalResults[domain] = finalRulesToCoordinates;
                }

                foreach (var rulesToCentroidsEntry in rulesToCentroids)
                {
                    var rule = rulesToCentroidsEntry.Key;
                    var centroidsList = rulesToCentroidsEntry.Value;

                    finalRulesToCoordinates[rule] = centroidsList[0];
                }
            }

            var closestCityFinder = new PatternMiningClosestCityFinder(
                citiesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "cities1000.txt"),
                alternateNamesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "alternateNames.txt"),
                admin1Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin1CodesASCII.txt"),
                admin2Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin2Codes.txt"),
                countriesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "countryInfo.txt"),
                clliPath: clliPath,
                unlocodePath: unlocodePath);

            closestCityFinder.FindClosestCitiesForRules(finalResults);

            PatternRulesSerializer.SerializeReducedRules(finalResults, outputPath: patternsSerializationPath);
        }

        private static void TestHostnamePatterns(
            string hostnamePatternsDatePrefix,
            string hostnamesPatternsRootFolder,
            string dataSubset,
            List<string> hostnames)
        {
            var miner = new HostnamePatternMiner();
            var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");
            var reducedRules = PatternRulesSerializer.DeserializeReducedRules(patternsSerializationPath);

            foreach (var hostname in hostnames)
            {
                Console.WriteLine($"# Hostname: {hostname}");

                var parsedHostname = HostnameSplitter.Split(hostname);

                var domain = parsedHostname.DomainInfo.RegistrableDomain;

                Dictionary<PatternRule, PatternMiningCoordinates> rulesToCoordinates;

                if (!reducedRules.TryGetValue(domain, out rulesToCoordinates))
                {
                    continue;
                }

                var subdomainParts = parsedHostname.SubdomainParts;

                if (subdomainParts == null || subdomainParts.Count == 0)
                {
                    continue;
                }

                var ruleAtoms = miner.CreateRuleAtoms(subdomainParts);

                if (ruleAtoms == null || ruleAtoms.Count == 0)
                {
                    continue;
                }

                var rules = miner.GeneratePossibleRules(ruleAtoms);

                if (rules == null || rules.Count == 0)
                {
                    continue;
                }

                PatternMiningCoordinates coordinates;

                foreach (var rule in rules)
                {
                    if (rulesToCoordinates.TryGetValue(rule, out coordinates))
                    {
                        Console.WriteLine($"Rule: {rule}, Coordinates: {coordinates}");
                    }
                }

                Console.WriteLine("-------------------------------");
            }
        }

        private static void TestHostnamePatterns(
            string hostnamePatternsDatePrefix,
            string hostnamesPatternsRootFolder,
            string dataSubset,
            string testingDataPath,
            bool useClosestCityInsteadOfRawCoordinates)
        {
            var miner = new HostnamePatternMiner();
            var patternsSerializationPath = Path.Combine(hostnamesPatternsRootFolder, $"{hostnamePatternsDatePrefix}-HostnamePatternigMining-ReducedRules{dataSubset}.bin");
            var reducedRules = PatternRulesSerializer.DeserializeReducedRules(patternsSerializationPath);

            var datasetParser = new GroundTruthParser();

            var total = 0;
            var hostnamesWithRules = 0;
            var realDistanceBuckets = new Dictionary<string, int>();

            foreach (var item in datasetParser.Parse(testingDataPath, populateTextualLocationInfo: true))
            {
                total++;

                if (total % 10000 == 0)
                {
                    Console.WriteLine(total);
                }

                //Console.WriteLine($"# Hostname: {item.Hostname}");

                var parsedHostname = HostnameSplitter.Split(item.Hostname);

                var domain = parsedHostname?.DomainInfo?.RegistrableDomain;

                if (domain == null)
                {
                    continue;
                }

                Dictionary<PatternRule, PatternMiningCoordinates> rulesToCoordinates;

                if (!reducedRules.TryGetValue(domain, out rulesToCoordinates))
                {
                    continue;
                }

                var subdomainParts = parsedHostname.SubdomainParts;

                if (subdomainParts == null || subdomainParts.Count == 0)
                {
                    continue;
                }

                var ruleAtoms = miner.CreateRuleAtoms(subdomainParts);

                if (ruleAtoms == null || ruleAtoms.Count == 0)
                {
                    continue;
                }

                var rules = miner.GeneratePossibleRules(ruleAtoms);

                if (rules == null || rules.Count == 0)
                {
                    continue;
                }

                var anyCoordMatch = false;

                PatternMiningCoordinates coordinatesWithHighestConfidence = null;
                var highestConfidence = -1d;

                foreach (var rule in rules)
                {
                    PatternMiningCoordinates currentCoordinates;

                    if (rulesToCoordinates.TryGetValue(rule, out currentCoordinates))
                    {
                        if (currentCoordinates.Confidence > highestConfidence)
                        {
                            anyCoordMatch = true;
                            highestConfidence = currentCoordinates.Confidence;
                            coordinatesWithHighestConfidence = currentCoordinates;
                        }

                        /*
                        anyCoordMatch = true;

                        //Console.WriteLine($"Rule: {rule}, Coordinates: {currentCoordinates}");

                        var distance = DistanceHelper.Distance(item.Latitude, item.Longitude, currentCoordinates.Latitude, currentCoordinates.Longitude, DistanceUnit.Kilometer);
                        var realDistanceBucket = DistanceHelper.GetDistanceBucketKM(bucketSize: 10, distanceKilometers: distance, gteThreshold: 220);
                        IncrementCount(realDistanceBuckets, realDistanceBucket);
                        */
                    }
                }

                if (anyCoordMatch)
                {
                    double distance;

                    if (useClosestCityInsteadOfRawCoordinates)
                    {
                        if (coordinatesWithHighestConfidence.ClosestCity == null)
                        {
                            continue;
                        }

                        distance = DistanceHelper.Distance(item.Latitude, item.Longitude, coordinatesWithHighestConfidence.ClosestCity.Latitude, coordinatesWithHighestConfidence.ClosestCity.Longitude, DistanceUnit.Kilometer);
                    }
                    else
                    {
                        distance = DistanceHelper.Distance(item.Latitude, item.Longitude, coordinatesWithHighestConfidence.Latitude, coordinatesWithHighestConfidence.Longitude, DistanceUnit.Kilometer);
                    }

                    var realDistanceBucket = DistanceHelper.GetDistanceBucketKM(bucketSize: 10, distanceKilometers: distance, gteThreshold: 220);
                    IncrementCount(realDistanceBuckets, realDistanceBucket);

                    hostnamesWithRules++;
                }

                //Console.WriteLine("-------------------------------");
            }

            Console.WriteLine("Done!");
            Console.WriteLine($"hostnamesWithRules: {hostnamesWithRules}");
            Console.WriteLine($"total: {total}");
            Console.WriteLine("-------------------------------");
            DisplayDistanceBucketStats(realDistanceBuckets);
        }

        private static Dictionary<string, int> CountDomainOccurrencesInGT(GroundTruthParser datasetParser, string inPath)
        {
            var occurrences = new Dictionary<string, int>();

            var count = 0;

            foreach (var item in datasetParser.Parse(inPath, populateTextualLocationInfo: true))
            {
                var hostname = item.Hostname;
                var domain = HostnameSplitter.ExtractDomain(hostname);

                if (domain != null)
                {
                    count++;

                    int localCount;

                    if (!occurrences.TryGetValue(domain, out localCount))
                    {
                        localCount = 0;
                    }

                    localCount++;
                    occurrences[domain] = localCount;

                    if (count % 1000000 == 0)
                    {
                        PrintOccurences(occurrences);
                    }
                }
            }

            return occurrences;
        }

        private static void PrintOccurences(Dictionary<string, Dictionary<string, int>> occurrences, int topToShowPerDomain)
        {
            foreach (var domainItem in occurrences)
            {
                var domain = domainItem.Key;

                var count = 0;

                Console.WriteLine("------------------------");
                Console.WriteLine(domain);
                Console.WriteLine("---");

                foreach (var item in domainItem.Value.OrderByDescending(key => key.Value))
                {
                    count++;

                    Console.WriteLine($"{item.Key} - {item.Value}"); ;

                    if (count >= topToShowPerDomain)
                    {
                        break;
                    }
                }

                Console.WriteLine();
            }
        }

        private static void PrintOccurences(Dictionary<string, int> occurrences, int minValue = -1)
        {
            foreach (var item in occurrences.OrderByDescending(key => key.Value))
            {
                if (minValue > 0 && item.Value >= minValue)
                {
                    Console.WriteLine($"{item.Key} - {item.Value}"); ;
                }
                else if (minValue < 0)
                {
                    Console.WriteLine($"{item.Key} - {item.Value}"); ;
                }
            }

            Console.WriteLine("-------------------------");
        }

        private static void GoodVsBad(Dictionary<string, int> distanceGoodPerDoman, Dictionary<string, int> distanceBadPerDoman)
        {
            Console.WriteLine("GoodVsBad START");

            foreach (var goodEntry in distanceGoodPerDoman)
            {
                var goodDomain = goodEntry.Key;
                var goodCount = goodEntry.Value;

                if (goodCount < 100)
                {
                    continue;
                }

                int badCount = 0;

                if (!distanceBadPerDoman.TryGetValue(goodDomain, out badCount))
                {
                    badCount = 0;
                }

                if (badCount != 0)
                {
                    var ratio = goodCount / (1.0d * (goodCount + badCount));

                    if (ratio >= 0.5)
                    {
                        Console.WriteLine(goodDomain);
                    }
                }
            }

            Console.WriteLine("GoodVsBad END");
        }

        private static void TestClassifierOnHostnames(IDataSetParser datasetParser, string inPath, ModelRunner runner, string outPath, string featureDistributionDataPath, IBestGuesser bestGuesser, double minProbability = 0)
        {
            var distanceGood = 0;
            var distanceBad = 0;
            var totalHostnamesAttempted = 0;
            var totalWithAtLeastOneResult = 0;

            var distanceGoodPerDoman = new Dictionary<string, int>();
            var distanceBadPerDoman = new Dictionary<string, int>();

            var decisionsPerDomainCount = new Dictionary<string, int>();
            var featuresDistribution = new Dictionary<CityFeatureType, int>();
            var featuresDistributionPerDomain = new Dictionary<string, Dictionary<CityFeatureType, int>>();

            var decisionsPerDomainGoodDistanceCount = new Dictionary<string, int>();
            var featuresDistributionGoodDistance = new Dictionary<CityFeatureType, int>();
            var featuresDistributionPerDomainGoodDistance = new Dictionary<string, Dictionary<CityFeatureType, int>>();

            var realDistanceBuckets = new Dictionary<string, int>();
            var bestCaseDistanceBuckets = new Dictionary<string, int>();

            var allErrorDistances = new List<double>();

            var candidatesGenerationTime = new Stopwatch();

            /*
        public Stopwatch HostnameSplittingTime { get; set; }

        public Stopwatch CandidateGenerationTime { get; set; }

        public Stopwatch ClassificationTime { get; set; }

        public Stopwatch TotalExtractCitiesTime { get; set; }
             */

            var domainsFilter = new HashSet<string>()
            {
                "sbcglobal.net",
                "verizon.net",
                "atlanticbb.net",
                "brasiltelecom.net.br",
                "163data.com.cn",
                "rr.com",
                "suddenlink.net",
                "frontiernet.net",
                "cox.net",
                "bell.ca",
                "shawcable.net",
                "qwest.net",
                "virginm.net",
                "wind.it",
                "ziggo.nl",
                "airtelbroadband.in",
                "ptd.net",
                "ocn.ne.jp",
                "bellsouth.net",
                "asianet.co.th",
                "ertelecom.ru",
                "vtr.net",
                "videotron.ca",
                "ny.adsl",
                "bresnan.net",
                "home.ne.jp",
                "eonet.ne.jp",
                "megared.net.mx",
                "rcncustomer.com",
                "fastwebnet.it",
                "so-net.ne.jp",
                "tedata.net",
                "fuse.net",
                "hinet.net",
                "telus.net",
                "plala.or.jp",
                "tds.net",
                "bellaliant.net"
            };

            using (var writer = new StreamWriter(outPath))
            {
                foreach (var item in datasetParser.Parse(inPath, populateTextualLocationInfo: true))
                {
                    totalHostnamesAttempted++;

                    var hostname = item.Hostname;

                    ////Console.WriteLine($"Running classifier on hostname: {hostname}");

                    var results = runner.ExtractCities(hostname);

                    if (results.Count > 0)
                    {
                        var smallestDistance = double.MaxValue;
                        GeonamesCityEntity smallestDistanceCity = null;

                        foreach (var result in results)
                        {
                            var distance = DistanceHelper.Distance(item.Latitude, item.Longitude, result.City.Latitude, result.City.Longitude, DistanceUnit.Kilometer);

                            if (smallestDistance > distance)
                            {
                                smallestDistance = distance;
                                smallestDistanceCity = result.City;
                            }
                        }

                        var bestResult = bestGuesser.PickBest(hostname, results, minProbability);

                        if (bestResult != null)
                        {
                            var domain = HostnameSplitter.ExtractDomain(hostname);

                            if (!domainsFilter.Contains(domain.ToLowerInvariant()))
                            {
                                continue;
                            }

                            totalWithAtLeastOneResult++;
                            IncrementCount(decisionsPerDomainCount, domain);

                            var distance = DistanceHelper.Distance(item.Latitude, item.Longitude, bestResult.City.Latitude, bestResult.City.Longitude, DistanceUnit.Kilometer);
                            allErrorDistances.Add(distance);

                            foreach (var feature in bestResult.NonDefaultFeatures.Keys)
                            {
                                IncrementFeatureDistributionCount(featuresDistribution, feature);
                                IncrementFeatureDistributionCountPerDomain(featuresDistributionPerDomain, domain, feature);
                            }

                            if (distance <= 80)
                            {
                                IncrementCount(decisionsPerDomainGoodDistanceCount, domain);

                                foreach (var feature in bestResult.NonDefaultFeatures.Keys)
                                {
                                    // TODO: Remove:
                                    //if (!bestResult.NonDefaultFeatures.ContainsKey(CityFeatureType.HostnamePatternMatch) && feature == CityFeatureType.FirstLettersAdmin1NameMatch)
                                    //if (!bestResult.NonDefaultFeatures.ContainsKey(CityFeatureType.HostnamePatternMatch) && feature == CityFeatureType.CityAdmin1NameMatch && bestResult.City.CountryCode != "US")
                                    //if (!bestResult.NonDefaultFeatures.ContainsKey(CityFeatureType.HostnamePatternMatch) && feature == CityFeatureType.HostnamePatternMatch && bestResult.City.CountryCode != "US")
                                    /*
                                    if (feature == CityFeatureType.HostnamePatternMatch && bestResult.City.CountryCode != "US")
                                    {
                                        Console.WriteLine();
                                    }
                                    */

                                    IncrementFeatureDistributionCount(featuresDistributionGoodDistance, feature);
                                    IncrementFeatureDistributionCountPerDomain(featuresDistributionPerDomainGoodDistance, domain, feature);
                                }

                                /*
                                if (bestResult.NonDefaultFeatures.ContainsKey(CityFeatureType.CityCountryNameMatch))
                                {
                                    //  && bestResult.City.CountryCode != "US"
                                    Console.WriteLine("Match");
                                }
                                */

                                distanceGood++;
                                IncrementCount(distanceGoodPerDoman, domain);
                            }
                            else
                            {
                                distanceBad++;
                                IncrementCount(distanceBadPerDoman, domain);
                            }

                            /*
                            if (distance > 500)
                            {
                                Console.WriteLine(hostname);
                            }
                            */

                            var realDistanceBucket = DistanceHelper.GetDistanceBucketKM(bucketSize: 10, distanceKilometers: distance, gteThreshold: 220);
                            IncrementCount(realDistanceBuckets, realDistanceBucket);

                            var bestCaseDistanceBucket = DistanceHelper.GetDistanceBucketKM(bucketSize: 10, distanceKilometers: smallestDistance, gteThreshold: 220);
                            IncrementCount(bestCaseDistanceBuckets, bestCaseDistanceBucket);

                            ////Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t\t\t\t{1}\t\t\t\t{2}\t\t\t\t{3}\t\t\t\t{4}\t\t\t\t{5}", hostname, bestResult.City, Math.Round((double)bestResult.Score, 3), Math.Round(distance, 1), smallestDistanceCity, Math.Round(smallestDistance, 1)));

                            writer.WriteLine($"{CleanText(hostname)}\t{bestResult.City?.Latitude}\t{bestResult.City?.Longitude}\t{Math.Round((double)bestResult.Score, 3)}\t{Math.Round(distance, 1)}\t{CleanText(bestResult.City?.Name)}\t{CleanText(bestResult.City?.Admin1Entity?.Name)}\t{CleanText(bestResult.City?.CountryEntity?.Name)}\t{item.Latitude}\t{item.Longitude}\t{item.RealtimeAccuracyKm}\t{item.City}\t{item.State}\t{item.Country}");

                            ////Console.WriteLine("----------------------------");
                            ////Console.ReadKey();

                            if (totalWithAtLeastOneResult % 500 == 0)
                            {
                                ////GoodVsBad(distanceGoodPerDoman, distanceBadPerDoman);

                                Console.WriteLine("Best guess results");
                                Console.WriteLine($"distanceGood = {distanceGood}, distanceBad = {distanceBad}, good/total = {Math.Round(distanceGood / (1.0 * totalWithAtLeastOneResult), 2) * 100}");
                                DisplayDistanceBucketStats(realDistanceBuckets);
                                ////Console.WriteLine("Feature distribution");
                                ////DisplayFeatureDistributionStats(distanceGoodFeaturesDistribution);
                                //Console.WriteLine("Smallest theoretical distance results");
                                //DisplayDistanceBucketStats(bestCaseDistanceBuckets);

                                Console.WriteLine("Error distance median");
                                DisplayErrorDistanceMedian(allErrorDistances);

                                Console.WriteLine("RMS");
                                DisplayRMS(allErrorDistances);
                            }

                            /*
                            if (totalWithAtLeastOneResult % 10000 == 0)
                            {
                                Console.WriteLine("Error distance median");
                                DisplayErrorDistanceMedian(allErrorDistances);

                                Console.WriteLine("RMS");
                                DisplayRMS(allErrorDistances);
                            }
                            */

                            /*
                            if (total > 100000)
                            {
                                break;
                            }
                            */

                            /*
                            if (total > 100000)
                            {
                                break;
                            }
                            */
                        }
                        else
                        {
                            writer.WriteLine($"{CleanText(hostname)}\t\t\t\t\t\t\t\t{item.Latitude}\t{item.Longitude}\t{item.RealtimeAccuracyKm}\t{item.City}\t{item.State}\t{item.Country}");
                        }
                    }
                }
            }

            Console.WriteLine("FINAL RESULTS - Best guess results");
            Console.WriteLine($"distanceGood = {distanceGood}, distanceBad = {distanceBad}, good/total = {Math.Round(distanceGood / (1.0 * totalWithAtLeastOneResult), 2) * 100}");
            DisplayDistanceBucketStats(realDistanceBuckets);
            ////Console.WriteLine("FINAL RESULTS - Feature distribution");
            ////DisplayFeatureDistributionStats(distanceGoodFeaturesDistribution);
            Console.WriteLine("FINAL RESULTS - Smallest theoretical distance results");
            DisplayDistanceBucketStats(bestCaseDistanceBuckets);
            Console.WriteLine("FINAL RESULTS - Error distance median");
            DisplayErrorDistanceMedian(allErrorDistances);
            Console.WriteLine("FINAL RESULTS - RMS");
            DisplayRMS(allErrorDistances);

            StoreFeaturesDistribution(featureDistributionDataPath, decisionsPerDomainCount, featuresDistribution, featuresDistributionPerDomain);
        }

        private static void StoreFeaturesDistribution(string featureDistributionDataPath, Dictionary<string, int> decisionsPerDomainCount, Dictionary<CityFeatureType, int> featuresDistribution, Dictionary<string, Dictionary<CityFeatureType, int>> featuresDistributionPerDomain)
        {
            using (var writer = new StreamWriter(featureDistributionDataPath))
            {
                var minDecisionsDomainCount = 200;
                var minFeaturesOverThreadhold = 2;
                var threshold = 0.1;

                foreach (var domainEntry in decisionsPerDomainCount)
                {
                    var domain = domainEntry.Key;
                    var decisionsPerDomain = domainEntry.Value;
                    var featuresOverThresholdForDomain = 0;

                    if (decisionsPerDomain < minDecisionsDomainCount)
                    {
                        continue;
                    }

                    Dictionary<CityFeatureType, int> featuresDistributionForDomain;

                    if (featuresDistributionPerDomain.TryGetValue(domain, out featuresDistributionForDomain))
                    {
                        foreach (var featuresDistributionForDomainEntry in featuresDistributionForDomain)
                        {
                            var featureName = featuresDistributionForDomainEntry.Key.ToString();

                            if (!featureName.EndsWith("Match"))
                            {
                                continue;
                            }

                            var featureCount = featuresDistributionForDomainEntry.Value;

                            if ((featureCount / ((1.0d) * decisionsPerDomain)) > threshold)
                            {
                                featuresOverThresholdForDomain++;
                            }
                        }

                        if (featuresOverThresholdForDomain >= minFeaturesOverThreadhold)
                        {
                            var line = $"{domain}\t{featuresOverThresholdForDomain}\t{decisionsPerDomain}";
                            writer.WriteLine(line);
                            Console.WriteLine(line);
                        }
                    }
                }
            }
        }

        private static string CleanText(string text)
        {
            if (text == null)
            {
                return text;
            }

            return text.Replace("\t", " ");
        }

        // allErrorDistances

        private static void DisplayErrorDistanceMedian(List<double> errorDistances)
        {
            var median = errorDistances.Median();
            Console.WriteLine($"Median:\t{median}");
        }

        private static void DisplayRMS(List<double> errorDistances)
        {
            var rms = errorDistances.RootMeanSquare();
            Console.WriteLine($"RMS:\t{rms}");
        }

        private static void DisplayDistanceBucketStats(Dictionary<string, int> distanceBuckets)
        {
            var sortedDistanceBuckets = distanceBuckets.OrderBy(pair => pair.Key);
            Console.WriteLine();

            foreach (var distanceBucketItem in sortedDistanceBuckets)
            {
                Console.WriteLine($"{distanceBucketItem.Key}\t{distanceBucketItem.Value}");
            }

            Console.WriteLine();
        }

        private static void DisplayFeatureDistributionStats(Dictionary<CityFeatureType, int> featuresDistribution)
        {
            var sortedFeatureDistributionValues = featuresDistribution.OrderBy(pair => pair.Key);
            Console.WriteLine();

            foreach (var featureCount in sortedFeatureDistributionValues)
            {
                Console.WriteLine($"{featureCount.Key}\t{featureCount.Value}");
            }

            Console.WriteLine();
        }

        private static void IncrementCount(Dictionary<string, int> dict, string domain)
        {
            var count = 0;
            dict.TryGetValue(domain, out count);
            count++;
            dict[domain] = count;
        }

        private static void IncrementFeatureDistributionCountPerDomain(Dictionary<string, Dictionary<CityFeatureType, int>> featuresDistributionPerDomain, string domain, CityFeatureType feature)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                return;
            }

            Dictionary<CityFeatureType, int> featuresDistribution;

            if (!featuresDistributionPerDomain.TryGetValue(domain, out featuresDistribution))
            {
                featuresDistribution = new Dictionary<CityFeatureType, int>();
                featuresDistributionPerDomain[domain] = featuresDistribution;
            }

            IncrementFeatureDistributionCount(featuresDistribution, feature);
        }

        private static void IncrementFeatureDistributionCount(Dictionary<CityFeatureType, int> featuresDistribution, CityFeatureType feature)
        {
            var count = 0;
            featuresDistribution.TryGetValue(feature, out count);
            count++;
            featuresDistribution[feature] = count;
        }

        private static TrainingData SampleGenerateTrainingData(
            CityFeaturesAggregator aggregator, 
            FeaturesConfig featuresConfig, 
            string groundTruthPath, 
            string trainingOutputPath, 
            int maxPositiveSamplesPerDomain,
            double maxDiffMagnitude,
            double forceAddMinPercentile)
        {
            var hostnameSampler = new TrainingDataSampler(aggregator: aggregator,
                groundTruthPath: groundTruthPath,
                trueNegativesMultiplier: 3,
                featuresConfig: featuresConfig,
                maxPositiveSamplesPerDomain: maxPositiveSamplesPerDomain,
                maxDiffMagnitude: maxDiffMagnitude,
                forceAddMinPercentile: forceAddMinPercentile);

            var samplerToTraining = new SamplerToTrainingDataConverter();
            var trainingData = samplerToTraining.Convert(aggregator, hostnameSampler);

            trainingData.SerializeTo(trainingOutputPath);

            /*
            trainingData.SerializeInputsTo(trainingInputsDataPath);
            trainingData.SerializeOutputsTo(trainingOutputsDataPath);
            */

            return trainingData;
        }

        private static LogisticRegressionClassifier TrainLogisticClassifier(CityFeaturesAggregator aggregator, TrainingData trainingData)
        {
            var classifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities);
            classifier.Train(trainingData.Inputs, trainingData.Outputs);

            return classifier;
        }

        private static void SaveLogisticClassifier(LogisticRegressionClassifier classifier, string logisticClassifierPath)
        {
            classifier.Regression.Save<LogisticRegression>(logisticClassifierPath);
        }

        private static C45Classifier TrainC45Classifier(CityFeaturesAggregator aggregator, TrainingData trainingData)
        {
            var classifier = new C45Classifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities);
            classifier.Train(trainingData.Inputs, trainingData.Outputs);

            return classifier;
        }

        private static RandomForestClassifier TrainRandomForestClassifier(CityFeaturesAggregator aggregator, TrainingData trainingData)
        {
            var classifier = new RandomForestClassifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities);
            classifier.Train(trainingData.Inputs, trainingData.Outputs);

            return classifier;
        }

        private static SVMClassifier TrainSVMClassifier(CityFeaturesAggregator aggregator, TrainingData trainingData)
        {
            var classifier = new SVMClassifier(featureDefaultsValueTypes: aggregator.FeatureDefaultsValueTypes, featureGranularities: aggregator.FeatureGranularities);
            classifier.Train(trainingData.Inputs, trainingData.Outputs);

            return classifier;
        }

        private static void SaveC45Classifier(C45Classifier classifier, string c45ClassifierPath)
        {
            var tree = classifier.Tree;
            tree.Save<DecisionTree>(c45ClassifierPath);
        }

        private static void SaveRandomForestClassifier(RandomForestClassifier classifier, string randomForestClassifierPath)
        {
            classifier.RandomForest.Save<RandomForest>(randomForestClassifierPath);
        }

        private static void SaveSVMClassifier(SVMClassifier classifier, string svmClassifierPath)
        {
            classifier.Svm.Save<SupportVectorMachine<Gaussian>>(svmClassifierPath);
        }

        private static void TestComcastC45()
        {
            var hostnamesProcessingAttemptedCounter = 0;
            var globalCounter = 0;

            //var sampler = new DataSampler();
            var sampler = new StratifiedDataSampler();

            var comcastTrainingData = sampler.Sample(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt",
                groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-comcast.txt",
                trueNegativesMultiplier: 5,
                shouldProcessHostname: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    globalCounter++;

                    if (globalCounter % 10000 == 0)
                    {
                        Console.WriteLine($"globalCounter = {globalCounter}");
                        Console.WriteLine();
                    }

                    try
                    {
                        if (string.IsNullOrEmpty(hostname) || !hostname.Contains(".") || hostname.Contains(" ") || hostname.Contains(","))
                        {
                            return false;
                        }

                        if (hostname.ToLowerInvariant().Contains("comcast.net"))
                        {
                            hostnamesProcessingAttemptedCounter++;
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ////Console.WriteLine($"Exception, could not parse hostname: {hostname} due to Exception: {ex}");
                    }

                    return false;
                },
                shouldContinueIngestingNewHostnames: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    //return hostnamesProcessingAttemptedCounter < 10000;
                    return true;
                },
                showConsoleStats: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount, bool lastRow)
                {
                });

            comcastTrainingData.FinalizeData();

            var classifier = new C45Classifier(featureDefaultsValueTypes: comcastTrainingData.FeatureDefaultsValueTypes, featureGranularities: comcastTrainingData.FeatureGranularities, serializedClassifierPath: @".\c45-classifier-nocomcast.bin");

            var validationPredicted = classifier.Decide(comcastTrainingData.Inputs);
            double validationError = new ZeroOneLoss(comcastTrainingData.Outputs).Loss(validationPredicted);

            var confusionMatrix = new ConfusionMatrix(validationPredicted, comcastTrainingData.Outputs, positiveValue: 1, negativeValue: 0);

            Console.WriteLine($"validationError: {validationError}\tconfusionMatrix.Accuracy: {confusionMatrix.Accuracy}\tconfusionMatrix.TruePositives: {confusionMatrix.TruePositives}\tconfusionMatrix.TrueNegatives: {confusionMatrix.TrueNegatives}\tconfusionMatrix.FalsePositives: {confusionMatrix.FalsePositives}\tconfusionMatrix.FalseNegatives: {confusionMatrix.FalseNegatives}\tconfusionMatrix.FalsePositiveRate: {confusionMatrix.FalsePositiveRate}");
        }

        private static ClassifierBundle TrainSaveC45Classifier(string outputPath, string groundTruthPath = @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt")
        {
            var hostnameCounts = new Dictionary<string, int>();

            var hostnamesProcessingAttemptedCounter = 0;
            var globalCounter = 0;

            //var sampler = new DataSampler();
            var sampler = new StratifiedDataSampler();
            var trainingData = sampler.Sample(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt",
                groundTruthPath: groundTruthPath,
                /*trueNegativesMultiplier: 5,*/
                trueNegativesMultiplier: 1,
                shouldProcessHostname: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    globalCounter++;

                    if (globalCounter % 10000 == 0)
                    {
                        Console.WriteLine($"globalCounter = {globalCounter}");
                        Console.WriteLine();
                    }

                    try
                    {
                        if (parsedHostname?.DomainInfo?.RegistrableDomain == null || string.IsNullOrEmpty(hostname) || !hostname.Contains(".") || hostname.Contains(" ") || hostname.Contains(","))
                        {
                            return false;
                        }

                        /*
                        if (hostname.ToLowerInvariant().Contains("comcast.net"))
                        {
                            return false;
                        }
                        */

                        if (!string.IsNullOrEmpty(parsedHostname?.DomainInfo?.RegistrableDomain))
                        {
                            int currentHostnameCount;

                            if (hostnameCounts.TryGetValue(parsedHostname.DomainInfo.RegistrableDomain, out currentHostnameCount))
                            {
                                if (currentHostnameCount < 1000)
                                {
                                    currentHostnameCount++;
                                    hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                    hostnamesProcessingAttemptedCounter++;
                                    return true;
                                }
                            }
                            else
                            {
                                currentHostnameCount = 1;
                                hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                hostnamesProcessingAttemptedCounter++;
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ////Console.WriteLine($"Exception, could not parse hostname: {hostname} due to Exception: {ex}");
                    }

                    //if (!hostname.Contains("comcast"))
                    //{
                    //    hostnamesProcessingAttemptedCounter++;
                    //    return true;
                    //}

                    return false;
                },
                shouldContinueIngestingNewHostnames: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    //return hostnamesProcessingAttemptedCounter < 500000;
                    //return hostnamesProcessingAttemptedCounter < 3000000;
                    return true;
                },
                showConsoleStats: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount, bool lastRow)
                {
                    if (lastRow)
                    {
                        Console.WriteLine("!!! Last Row !!!");
                    }

                    if (lastRow || (hostnamesProcessingAttemptedCounter % 10000 == 0))
                    {
                        Console.WriteLine($"hostnamesProcessingAttemptedCounter = {hostnamesProcessingAttemptedCounter}");
                        Console.WriteLine($"storedTruePositivesCount = {storedTruePositivesCount}");
                        Console.WriteLine($"storedTrueNegativesCount = {storedTrueNegativesCount}");
                        Console.WriteLine();
                    }
                });

            trainingData.FinalizeData();

            var classifier = new C45Classifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities);

            classifier.Train(trainingData.Inputs, trainingData.Outputs);

            var tree = classifier.Tree;

            tree.Save<DecisionTree>(outputPath);

            return new ClassifierBundle()
            {
                Aggregator = sampler.Aggregator,
                TrainingData = sampler.TrainingData,
                Classifier = classifier
            };
        }

        private static ClassifierBundle TrainSaveLogisticRegressionClassifier(string outputPath, string groundTruthPath = @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", FeaturesConfig featuresConfig = null, bool debugMode = true)
        {
            var hostnameCounts = new Dictionary<string, int>();

            var hostnamesProcessingAttemptedCounter = 0;
            var globalCounter = 0;

            //var sampler = new DataSampler();
            var sampler = new StratifiedDataSampler();
            var trainingData = sampler.Sample(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt",
                groundTruthPath: groundTruthPath,
                trueNegativesMultiplier: 1,
                featuresConfig: featuresConfig,
                shouldProcessHostname: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    globalCounter++;

                    if (globalCounter % 10000 == 0)
                    {
                        Console.WriteLine($"globalCounter = {globalCounter}");
                        Console.WriteLine();
                    }

                    try
                    {
                        if (parsedHostname?.DomainInfo?.RegistrableDomain == null || string.IsNullOrEmpty(hostname) || !hostname.Contains(".") || hostname.Contains(" ") || hostname.Contains(","))
                        {
                            return false;
                        }

                        /*
                        if (hostname.ToLowerInvariant().Contains("comcast.net"))
                        {
                            return false;
                        }
                        */

                        if (!string.IsNullOrEmpty(parsedHostname.DomainInfo.RegistrableDomain))
                        {
                            int currentHostnameCount;

                            if (hostnameCounts.TryGetValue(parsedHostname.DomainInfo.RegistrableDomain, out currentHostnameCount))
                            {
                                if (currentHostnameCount < 1000)
                                {
                                    currentHostnameCount++;
                                    hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                    hostnamesProcessingAttemptedCounter++;
                                    return true;
                                }
                            }
                            else
                            {
                                currentHostnameCount = 1;
                                hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                hostnamesProcessingAttemptedCounter++;
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ////Console.WriteLine($"Exception, could not parse hostname: {hostname} due to Exception: {ex}");
                    }

                    //if (!hostname.Contains("comcast"))
                    //{
                    //    hostnamesProcessingAttemptedCounter++;
                    //    return true;
                    //}

                    return false;
                },
                shouldContinueIngestingNewHostnames: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    //return hostnamesProcessingAttemptedCounter < 200000;
                    //return hostnamesProcessingAttemptedCounter < 500000;
                    //////////// !!!!!!!!!!!!!!!!!!!!!!
                    //////return globalCounter < 20000000;
                    return globalCounter < 10000000;
                    //return true;
                },
                showConsoleStats: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount, bool lastRow)
                {
                    if (lastRow)
                    {
                        Console.WriteLine("!!! Last Row !!!");
                    }

                    if (lastRow || (hostnamesProcessingAttemptedCounter % 10000 == 0))
                    {
                        Console.WriteLine($"hostnamesProcessingAttemptedCounter = {hostnamesProcessingAttemptedCounter}");
                        Console.WriteLine($"storedTruePositivesCount = {storedTruePositivesCount}");
                        Console.WriteLine($"storedTrueNegativesCount = {storedTrueNegativesCount}");
                        Console.WriteLine();
                    }
                });

            trainingData.FinalizeData();

            var classifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities);

            classifier.Train(trainingData.Inputs, trainingData.Outputs);

            classifier.Regression.Save<LogisticRegression>(outputPath);

            return new ClassifierBundle()
            {
                Aggregator = sampler.Aggregator,
                TrainingData = sampler.TrainingData,
                Classifier = classifier
            };
        }

        private static void TestLogisticRegressionClassifier(string groundTruthPath = @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt")
        {
            var hostnameCounts = new Dictionary<string, int>();

            var hostnamesProcessingAttemptedCounter = 0;
            var globalCounter = 0;

            //var sampler = new DataSampler();
            var sampler = new StratifiedDataSampler();
            var trainingData = sampler.Sample(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt",
                groundTruthPath: groundTruthPath,
                /*trueNegativesMultiplier: 5,*/
                trueNegativesMultiplier: 1,
                shouldProcessHostname: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    globalCounter++;

                    if (globalCounter % 10000 == 0)
                    {
                        Console.WriteLine($"globalCounter = {globalCounter}");
                        Console.WriteLine();
                    }

                    try
                    {
                        if (parsedHostname?.DomainInfo?.RegistrableDomain == null || string.IsNullOrEmpty(hostname) || !hostname.Contains(".") || hostname.Contains(" ") || hostname.Contains(","))
                        {
                            return false;
                        }

                        /*
                        if (hostname.ToLowerInvariant().Contains("comcast.net"))
                        {
                            return false;
                        }
                        */

                        if (!string.IsNullOrEmpty(parsedHostname.DomainInfo.RegistrableDomain))
                        {
                            int currentHostnameCount;

                            if (hostnameCounts.TryGetValue(parsedHostname.DomainInfo.RegistrableDomain, out currentHostnameCount))
                            {
                                if (currentHostnameCount < 500)
                                {
                                    currentHostnameCount++;
                                    hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                    hostnamesProcessingAttemptedCounter++;
                                    return true;
                                }
                            }
                            else
                            {
                                currentHostnameCount = 1;
                                hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                hostnamesProcessingAttemptedCounter++;
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ////Console.WriteLine($"Exception, could not parse hostname: {hostname} due to Exception: {ex}");
                    }

                    //if (!hostname.Contains("comcast"))
                    //{
                    //    hostnamesProcessingAttemptedCounter++;
                    //    return true;
                    //}

                    return false;
                },
                shouldContinueIngestingNewHostnames: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    //////////////////////////// !!!!!!!!!!!!!!!!!!!!!!
                    //////////////////////////// !!!!!!!!!!!!!!!!!!!!!!
                    //////////////////////////// !!!!!!!!!!!!!!!!!!!!!!
                    //////////////////////////// !!!!!!!!!!!!!!!!!!!!!!
                    return hostnamesProcessingAttemptedCounter < 500000;
                    //return hostnamesProcessingAttemptedCounter < 500000;
                    ////return true;
                },
                showConsoleStats: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount, bool lastRow)
                {
                    if (lastRow)
                    {
                        Console.WriteLine("!!! Last Row !!!");
                    }

                    if (lastRow || (hostnamesProcessingAttemptedCounter % 10000 == 0))
                    {
                        Console.WriteLine($"hostnamesProcessingAttemptedCounter = {hostnamesProcessingAttemptedCounter}");
                        Console.WriteLine($"storedTruePositivesCount = {storedTruePositivesCount}");
                        Console.WriteLine($"storedTrueNegativesCount = {storedTrueNegativesCount}");
                        Console.WriteLine();
                    }
                });

            trainingData.FinalizeData();

            var classifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities);

            /*
            classifier.Train(trainingData.Inputs, trainingData.Outputs);
            classifier.Save<LogisticRegressionClassifier>(@".\logistic-regression-classifier.bin");
            */

            var crossValidator = new CrossValidator();
            var validationResult = crossValidator.Validate(classifier, trainingData);

            double trainingErrors = validationResult.Training.Mean;
            double validationErrors = validationResult.Validation.Mean;
            

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Training error mean: {0}", trainingErrors));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Validation error mean: {0}", validationErrors));
        }

        private static void TestLogisticRegressionClassifierWithSampler(
            CityFeaturesAggregator aggregator,
            FeaturesConfig featuresConfig,
            string groundTruthPath,
            int maxPositiveSamplesPerDomain,
            double maxDiffMagnitude,
            double forceAddMinPercentile)
        {
            var hostnameCounts = new Dictionary<string, int>();

            var hostnamesProcessingAttemptedCounter = 0;
            var globalCounter = 0;

            //var sampler = new DataSampler();
            var sampler = new StratifiedDataSampler();

            var hostnameSampler = new TrainingDataSampler(aggregator: aggregator,
                groundTruthPath: groundTruthPath,
                trueNegativesMultiplier: 3,
                featuresConfig: featuresConfig,
                maxPositiveSamplesPerDomain: maxPositiveSamplesPerDomain,
                maxDiffMagnitude: maxDiffMagnitude,
                forceAddMinPercentile: forceAddMinPercentile);

            var samplerToTraining = new SamplerToTrainingDataConverter();
            var trainingData = samplerToTraining.Convert(aggregator, hostnameSampler);

            var classifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities);

            /*
            classifier.Train(trainingData.Inputs, trainingData.Outputs);
            classifier.Save<LogisticRegressionClassifier>(@".\logistic-regression-classifier.bin");
            */

            var crossValidator = new CrossValidator();
            var validationResult = crossValidator.Validate(classifier, trainingData);

            double trainingErrors = validationResult.Training.Mean;
            double validationErrors = validationResult.Validation.Mean;


            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Training error mean: {0}", trainingErrors));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Validation error mean: {0}", validationErrors));
        }

        private static void TestLogisticRegressionClassifierWithoutSampler(
                    CityFeaturesAggregator aggregator,
                    FeaturesConfig featuresConfig,
                    string groundTruthPath,
                    int maxSamples)
        {
            var hostnameCounts = new Dictionary<string, int>();

            var hostnamesProcessingAttemptedCounter = 0;
            var globalCounter = 0;

            //var sampler = new DataSampler();
            var sampler = new StratifiedDataSampler();

            var hostnameSampler = new DummySampler(aggregator: aggregator,
                groundTruthPath: groundTruthPath,
                trueNegativesMultiplier: 3,
                featuresConfig: featuresConfig,
                maxSamples: maxSamples);

            var samplerToTraining = new SamplerToTrainingDataConverter();
            var trainingData = samplerToTraining.Convert(aggregator, hostnameSampler);

            var classifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities);

            /*
            classifier.Train(trainingData.Inputs, trainingData.Outputs);
            classifier.Save<LogisticRegressionClassifier>(@".\logistic-regression-classifier.bin");
            */

            var crossValidator = new CrossValidator();
            var validationResult = crossValidator.Validate(classifier, trainingData);

            double trainingErrors = validationResult.Training.Mean;
            double validationErrors = validationResult.Validation.Mean;


            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Training error mean: {0}", trainingErrors));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Validation error mean: {0}", validationErrors));
        }

        private static void TestC45Classifier(string groundTruthPath = @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", HashSet<string> whitelistDomains = null, HashSet<string> blacklistDomains = null, bool printRules = false, FeaturesConfig featuresConfig = null)
        {
            var hostnameCounts = new Dictionary<string, int>();

            var hostnamesProcessingAttemptedCounter = 0;
            var globalCounter = 0;

            //var sampler = new DataSampler();
            var sampler = new StratifiedDataSampler();
            var trainingData = sampler.Sample(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt",
                groundTruthPath: groundTruthPath,
                /*trueNegativesMultiplier: 5,*/
                trueNegativesMultiplier: 1,
                featuresConfig: featuresConfig,
                shouldProcessHostname: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    globalCounter++;

                    if (globalCounter % 10000 == 0)
                    {
                        Console.WriteLine($"globalCounter = {globalCounter}");
                        Console.WriteLine();
                    }

                    try
                    {
                        if (parsedHostname?.DomainInfo?.RegistrableDomain == null || string.IsNullOrEmpty(hostname) || !hostname.Contains(".") || hostname.Contains(" ") || hostname.Contains(","))
                        {
                            return false;
                        }

                        if (whitelistDomains != null)
                        {
                            foreach (var whitelistDomain in whitelistDomains)
                            {
                                if (hostname.ToLowerInvariant().EndsWith(whitelistDomain))
                                {
                                    hostnamesProcessingAttemptedCounter++;
                                    return true;
                                }
                            }

                            return false;
                        }

                        if (blacklistDomains != null)
                        {
                            foreach (var blacklistDomain in blacklistDomains)
                            {
                                if (hostname.ToLowerInvariant().EndsWith(blacklistDomain))
                                {
                                    return false;
                                }
                            }
                        }

                        if (parsedHostname?.DomainInfo?.RegistrableDomain == null || !string.IsNullOrEmpty(parsedHostname.DomainInfo.RegistrableDomain))
                        {
                            int currentHostnameCount;

                            if (hostnameCounts.TryGetValue(parsedHostname.DomainInfo.RegistrableDomain, out currentHostnameCount))
                            {
                                if (currentHostnameCount < 1000)
                                {
                                    currentHostnameCount++;
                                    hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                    hostnamesProcessingAttemptedCounter++;
                                    return true;
                                }
                            }
                            else
                            {
                                currentHostnameCount = 1;
                                hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                hostnamesProcessingAttemptedCounter++;
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ////Console.WriteLine($"Exception, could not parse hostname: {hostname} due to Exception: {ex}");
                    }

                    //if (!hostname.Contains("comcast"))
                    //{
                    //    hostnamesProcessingAttemptedCounter++;
                    //    return true;
                    //}

                    return false;
                },
                shouldContinueIngestingNewHostnames: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    //return hostnamesProcessingAttemptedCounter < 5000;
                    //return hostnamesProcessingAttemptedCounter < 100000;
                    return hostnamesProcessingAttemptedCounter < 500000;
                    //return hostnamesProcessingAttemptedCounter < 500000;
                    //return true;
                },
                showConsoleStats: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount, bool lastRow)
                {
                    if (lastRow)
                    {
                        Console.WriteLine("!!! Last Row !!!");
                    }

                    if (lastRow || (hostnamesProcessingAttemptedCounter % 10000 == 0))
                    {
                        Console.WriteLine($"hostnamesProcessingAttemptedCounter = {hostnamesProcessingAttemptedCounter}");
                        Console.WriteLine($"storedTruePositivesCount = {storedTruePositivesCount}");
                        Console.WriteLine($"storedTrueNegativesCount = {storedTrueNegativesCount}");
                        Console.WriteLine();
                    }
                });

            trainingData.FinalizeData();

            var classifier = new C45Classifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities);

            var crossValidator = new CrossValidator();
            var validationResult = crossValidator.Validate(classifier, trainingData);

            double trainingErrors = validationResult.Training.Mean;
            double validationErrors = validationResult.Validation.Mean;

            if (printRules)
            {
                classifier.Train(trainingData.Inputs, trainingData.Outputs);
                var tree = classifier.Tree;
                Console.WriteLine(tree.ToRules());
            }

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Training error mean: {0}", trainingErrors));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Validation error mean: {0}", validationErrors));
        }

        private static void TestRandomForestClassifier(string groundTruthPath = @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", HashSet<string> whitelistDomains = null, HashSet<string> blacklistDomains = null, bool printRules = false, FeaturesConfig featuresConfig = null)
        {
            var hostnameCounts = new Dictionary<string, int>();

            var hostnamesProcessingAttemptedCounter = 0;
            var globalCounter = 0;

            //var sampler = new DataSampler();
            var sampler = new StratifiedDataSampler();
            var trainingData = sampler.Sample(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt",
                groundTruthPath: groundTruthPath,
                /*trueNegativesMultiplier: 5,*/
                trueNegativesMultiplier: 1,
                featuresConfig: featuresConfig,
                shouldProcessHostname: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    globalCounter++;

                    if (globalCounter % 10000 == 0)
                    {
                        Console.WriteLine($"globalCounter = {globalCounter}");
                        Console.WriteLine();
                    }

                    try
                    {
                        if (parsedHostname?.DomainInfo?.RegistrableDomain == null || string.IsNullOrEmpty(hostname) || !hostname.Contains(".") || hostname.Contains(" ") || hostname.Contains(","))
                        {
                            return false;
                        }

                        if (whitelistDomains != null)
                        {
                            foreach (var whitelistDomain in whitelistDomains)
                            {
                                if (hostname.ToLowerInvariant().EndsWith(whitelistDomain))
                                {
                                    hostnamesProcessingAttemptedCounter++;
                                    return true;
                                }
                            }

                            return false;
                        }

                        if (blacklistDomains != null)
                        {
                            foreach (var blacklistDomain in blacklistDomains)
                            {
                                if (hostname.ToLowerInvariant().EndsWith(blacklistDomain))
                                {
                                    return false;
                                }
                            }
                        }

                        if (parsedHostname?.DomainInfo?.RegistrableDomain == null || !string.IsNullOrEmpty(parsedHostname.DomainInfo.RegistrableDomain))
                        {
                            int currentHostnameCount;

                            if (hostnameCounts.TryGetValue(parsedHostname.DomainInfo.RegistrableDomain, out currentHostnameCount))
                            {
                                if (currentHostnameCount < 1000)
                                {
                                    currentHostnameCount++;
                                    hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                    hostnamesProcessingAttemptedCounter++;
                                    return true;
                                }
                            }
                            else
                            {
                                currentHostnameCount = 1;
                                hostnameCounts[parsedHostname.DomainInfo.RegistrableDomain] = currentHostnameCount;

                                hostnamesProcessingAttemptedCounter++;
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ////Console.WriteLine($"Exception, could not parse hostname: {hostname} due to Exception: {ex}");
                    }

                    //if (!hostname.Contains("comcast"))
                    //{
                    //    hostnamesProcessingAttemptedCounter++;
                    //    return true;
                    //}

                    return false;
                },
                shouldContinueIngestingNewHostnames: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount)
                {
                    //return hostnamesProcessingAttemptedCounter < 5000;
                    //return hostnamesProcessingAttemptedCounter < 100000;
                    return hostnamesProcessingAttemptedCounter < 200000;
                    //return hostnamesProcessingAttemptedCounter < 500000;
                    //return hostnamesProcessingAttemptedCounter < 500000;
                    //return true;
                },
                showConsoleStats: delegate (string hostname, HostnameSplitterResult parsedHostname, int storedTruePositivesCount, int storedTrueNegativesCount, bool lastRow)
                {
                    if (lastRow)
                    {
                        Console.WriteLine("!!! Last Row !!!");
                    }

                    if (lastRow || (hostnamesProcessingAttemptedCounter % 10000 == 0))
                    {
                        Console.WriteLine($"hostnamesProcessingAttemptedCounter = {hostnamesProcessingAttemptedCounter}");
                        Console.WriteLine($"storedTruePositivesCount = {storedTruePositivesCount}");
                        Console.WriteLine($"storedTrueNegativesCount = {storedTrueNegativesCount}");
                        Console.WriteLine();
                    }
                });

            trainingData.FinalizeData();

            var classifier = new RandomForestClassifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities);

            var crossValidator = new CrossValidator();
            var validationResult = crossValidator.Validate(classifier, trainingData);

            double trainingErrors = validationResult.Training.Mean;
            double validationErrors = validationResult.Validation.Mean;

            if (printRules)
            {
                classifier.Train(trainingData.Inputs, trainingData.Outputs);
                var trees = classifier.RandomForest.Trees;

                Console.WriteLine("-----------------------------------------------------------------------------------");

                foreach (var tree in trees)
                {
                    Console.WriteLine(tree.ToRules());
                    Console.WriteLine("-----------------------------------------------------------------------------------");
                }
            }

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Training error mean: {0}", trainingErrors));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Validation error mean: {0}", validationErrors));
        }

        private static void PrintFeaturesForHostname(string hostname)
        {
            var aggregator = new CityFeaturesAggregator(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt",
                featuresConfig: new FeaturesConfig()
                {
                    InitializeDefaultFeatures = true,
                    /*MinimumPopulation = 10000*/
                });

            var subdomainParts = HostnameSplitter.Split(hostname);

            var candidatesAndFeatures = aggregator.GenerateCandidatesForHostname(subdomainParts);

            Console.WriteLine($"Hostname: {hostname}");
            Console.WriteLine("------");

            foreach (var candidateAndFeatures in candidatesAndFeatures)
            {
                var entity = candidateAndFeatures.Key;
                var features = candidateAndFeatures.Value;

                Console.WriteLine($"Entity: {entity} (size in bytes: {FeatureUtils.EstimateObjectSizeInBytes(features)})");

                foreach (var entry in features)
                {
                    Console.WriteLine($"{entry.Key} = {entry.Value}");
                }

                Console.WriteLine("------------------------------------");
            }
        }

        private static void TestInternationalHostnames()
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            string line;
            var idnMapping = new IdnMapping();

            using (var file = new StreamReader(@"C:\Projects\ReverseDNS\ReverseDNSDataset\20170118-rdns.utf8.tsv"))
            {
                var counter = 0;
                var withUnicode = 0;

                while ((line = file.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    counter++;

                    var parts = line.Split(',').ToList<string>();

                    var hostname = string.Join(",", parts.GetRange(1, parts.Count - 1));

                    if (string.IsNullOrWhiteSpace(hostname))
                    {
                        continue;
                    }

                    var unicodeHostname = HostnameSplitter.TryDecodeIDN(hostname);

                    if (hostname != unicodeHostname)
                    {
                        withUnicode++;
                        Console.WriteLine($"{hostname}|{unicodeHostname}|");
                    }

                    if (counter % 10000000 == 0)
                    {
                        Console.WriteLine(string.Format("{0:#,###,###.##}\t\t{1:#,###,###.##}\t\t{2:#,###,###.##}", withUnicode, counter-withUnicode, counter));
                    }
                }
            }
        }

        private static void TestC45ClassifierOnHostname(string hostname, string serializedClassifierPath)
        {
            TestC45ClassifierOnHostnames(new List<string> { hostname }, serializedClassifierPath);
        }

        private static void TestC45ClassifierOnHostnames(List<string> hostnames, string serializedClassifierPath)
        {
            var aggregator = new CityFeaturesAggregator(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt");
            var trainingData = new TrainingData(tableName: "ReverseDNSGeolocation Training", featuresAggregator: aggregator);
            var classifier = new C45Classifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities, serializedClassifierPath: serializedClassifierPath);

            //Console.WriteLine(classifier.Tree.ToRules());

            foreach (var hostname in hostnames)
            {
                var subdomainParts = HostnameSplitter.Split(hostname);
                var candidatesAndFeatures = aggregator.GenerateCandidatesForHostname(subdomainParts);

                Console.WriteLine($"\r\nEvaluating: {hostname}");

                foreach (var candidateAndFeatures in candidatesAndFeatures)
                {
                    var entity = candidateAndFeatures.Key;
                    var features = candidateAndFeatures.Value;

                    var featuresRow = trainingData.CreateTrainingRow(features);
                    var featuresRowArr = featuresRow.ToArray<double>(trainingData.InputColumnNames);

                    var label = classifier.Decide(featuresRowArr);

                    if (label == 1)
                    {
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Hostname: {0}", hostname));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Location candidate: {0}", entity));
                        Console.WriteLine("---");

                        foreach (var entry in features)
                        {
                            var featureName = entry.Key;
                            var featureValue = entry.Value;

                            var featureDefaultValue = trainingData.FeatureDefaults[featureName];

                            // Only show a feature if its value is different than the default
                            if (featureValue != featureDefaultValue)
                            {
                                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", featureName, featureValue));
                            }
                        }

                        Console.WriteLine("---");
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Output label: {0}", label));
                        Console.WriteLine();

                        Console.WriteLine("------------------------");
                        Console.WriteLine();
                    }
                }
            }
        }

        private static void TestC45ClassifierOnHostnames(List<string> hostnames, ClassifierBundle bundle)
        {
            var aggregator = bundle.Aggregator;
            var trainingData = bundle.TrainingData;
            var classifier = bundle.Classifier;

            //Console.WriteLine(classifier.Tree.ToRules());

            foreach (var hostname in hostnames)
            {
                var subdomainParts = HostnameSplitter.Split(hostname);
                var candidatesAndFeatures = aggregator.GenerateCandidatesForHostname(subdomainParts);

                Console.WriteLine($"\r\nEvaluating: {hostname}");

                foreach (var candidateAndFeatures in candidatesAndFeatures)
                {
                    var entity = candidateAndFeatures.Key;
                    var features = candidateAndFeatures.Value;

                    var featuresRow = trainingData.CreateTrainingRow(features);
                    var featuresRowArr = featuresRow.ToArray<double>(trainingData.InputColumnNames);

                    var label = classifier.Decide(featuresRowArr);

                    if (label == 1)
                    {
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Hostname: {0}", hostname));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Location candidate: {0}", entity));
                        Console.WriteLine("---");

                        foreach (var entry in features)
                        {
                            var featureName = entry.Key;
                            var featureValue = entry.Value;

                            var featureDefaultValue = trainingData.FeatureDefaults[featureName];

                            // Only show a feature if its value is different than the default
                            if (featureValue != featureDefaultValue)
                            {
                                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", featureName, featureValue));
                            }
                        }

                        Console.WriteLine("---");
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Output label: {0}", label));
                        Console.WriteLine();

                        Console.WriteLine("------------------------");
                        Console.WriteLine();
                    }
                }
            }
        }

        private static void TestLogisticRegressionClassifierOnHostname(string hostname, string serializedClassifierPath, FeaturesConfig featuresConfig = null, bool debugMode = true)
        {
            TestLogisticRegressionClassifierOnHostnames(new List<string> { hostname }, serializedClassifierPath, featuresConfig, debugMode);
        }

        private static void TestLogisticRegressionClassifierOnGT(string groundTruthPath, string serializedClassifierPath, bool debugMode = true)
        {
            var aggregator = new CityFeaturesAggregator(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt");
            var trainingData = new TrainingData(tableName: "ReverseDNSGeolocation Training", featuresAggregator: aggregator);
            var classifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities, serializedClassifierPath: serializedClassifierPath);

            var total = 0;
            var withResults = 0;
            var withSingleResults = 0;
            var withMultiResults = 0;
            var withoutResults = 0;
            var uniqueLocations = new HashSet<string>();

            var runner = new ModelRunner(aggregator, trainingData, classifier, debugMode);

            string line;

            using (var file = new StreamReader(@"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier.txt"))
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

                    var hostname = parts[3];

                    total++;

                    var results = runner.ExtractCities(hostname);

                    if (results.Count > 0)
                    {
                        withResults++;

                        var firstResult = results[0];

                        var locationDescription = firstResult.City.ToString();

                        if (results.Count == 1)
                        {
                            withSingleResults++;
                        }
                        else
                        {
                            withMultiResults++;
                        }

                        /*
                        if (!uniqueLocations.Contains(locationDescription))
                        {
                            uniqueLocations.Add(locationDescription);
                            Console.WriteLine($"{hostname}\t\t\t{locationDescription}\t\t\t{Math.Round(results[0].Score.Value, 2)}\t\t\t1 match out of {results.Count}\t\t\twithResults = {withResults} | withSingleResults = {withSingleResults} | withMultiResults = {withMultiResults} | withoutResults = {withoutResults} | total = {total}");
                        }
                        */
                    }
                    else
                    {
                        withoutResults++;
                        //Console.WriteLine(hostname);

                        /*
                        if (withoutResults % 200 == 0)
                        {
                            Console.ReadKey();
                        }
                        */
                    }
                }
            }

            Console.WriteLine($"END:\t\t\twithResults = {withResults} | withSingleResults = {withSingleResults} | withMultiResults = {withMultiResults} | withoutResults = {withoutResults} | total = {total}");
        }

        private static void TestLogisticRegressionClassifierOnHostnames(List<string> hostnames, string serializedClassifierPath, FeaturesConfig featuresConfig = null, bool debugMode = true)
        {
            var aggregator = new CityFeaturesAggregator(
                citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
                alternateNamesPath: @"C:\Projects\ReverseDNS\alternateNames.txt",
                admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
                admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
                countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
                clliPath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv",
                unlocodePath: @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt",
                featuresConfig: featuresConfig);
            var trainingData = new TrainingData(tableName: "ReverseDNSGeolocation Training", featuresAggregator: aggregator);
            var classifier = new LogisticRegressionClassifier(featureDefaultsValueTypes: trainingData.FeatureDefaultsValueTypes, featureGranularities: trainingData.FeatureGranularities, serializedClassifierPath: serializedClassifierPath);

            var runner = new ModelRunner(aggregator, trainingData, classifier, debugMode);

            foreach (var hostname in hostnames)
            {
                /*
                var subdomainParts = HostnameSplitter.Split(hostname);
                var candidatesAndFeatures = aggregator.GenerateCandidatesForHostname(subdomainParts);

                foreach (var candidateAndFeatures in candidatesAndFeatures)
                {
                    var entity = candidateAndFeatures.Key;
                    var features = candidateAndFeatures.Value;

                    var featuresRow = trainingData.CreateTrainingRow(features);
                    var featuresRowArr = featuresRow.ToArray<double>(trainingData.InputColumnNames);

                    int label;
                    var probability = classifier.Probability(featuresRowArr, out label);

                    if (label == 1)
                    {
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Hostname: {0}", hostname));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Location candidate: {0}", entity));
                        Console.WriteLine("---");

                        foreach (var entry in features)
                        {
                            var featureName = entry.Key;
                            var featureValue = entry.Value;

                            var featureDefaultValue = trainingData.FeatureDefaults[featureName];

                            // Only show a feature if its value is different than the default
                            if (featureValue != featureDefaultValue)
                            {
                                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", featureName, featureValue));
                            }
                        }

                        Console.WriteLine("---");
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Output label: {0} (Probability: {1})", label, probability));
                        Console.WriteLine();

                        Console.WriteLine("------------------------");
                        Console.WriteLine();
                    }
                }
                */

                Console.WriteLine($"Running classifier on hostname: {hostname}");

                var matchingCities = runner.ExtractCities(hostname);

                Console.WriteLine("----------------------------");
                Console.WriteLine("----------------------------");
                Console.WriteLine("----------------------------");
            }
        }

        /*
        Console.WriteLine("Loading cities");
        var matcher = new HostnameMatcher();
        matcher.LoadGeonamesCities(@"C:\Projects\Whois\DNS\cities1000.txt");
        Console.WriteLine("Done loading cities");

        var intersector = new HostnameMatchesIntersector();

        string line;

        //using (var file = new StreamReader(@"C:\Users\zmarty\Downloads\20161109-rdns\frontiernet.net"))
        using (var file = new StreamReader(@"C:\Users\zmarty\Downloads\20161109-rdns\comcast.nohsd1.net"))
        {
            while ((line = file.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = new List<string>(line.Split(new char[] { ',' }));

                if (parts.Count != 2)
                {
                    continue;
                }

                var hostname = parts[1];

                if (hostname == "ge-1-0-3-3777-sur03.pompanobeach.fl.pompano.comcast.net")
                {
                    Console.WriteLine(hostname);
                }

                var matches = matcher.FindMatches(hostname);

                if (matches.Count > 0)
                {
                    Console.WriteLine("############ " + hostname + " ############");
                    intersector.Intersect(matches);
                    Console.ReadKey();
                }
            }
        }
        */

        ////var path = @"C:\Projects\Whois\DNS\cities1000.txt";

        /*
        var exactCityFeaturesGenerator = new ExactCityFeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(path))
        {
            exactCityFeaturesGenerator.IngestCityEntity(entity);
        }

        var candidatesAndFeatures = exactCityFeaturesGenerator.GenerateCandidatesAndFeatures("ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.comcast.net");
        */

        /*
        var exactCityAdmin1FeaturesGenerator = new ExactCityAdmin1FeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(path))
        {
            exactCityAdmin1FeaturesGenerator.IngestCityEntity(entity);
        }

        string line;

        using (var file = new StreamReader(@"C:\Users\zmarty\Downloads\20161109-rdns\comcast.nohsd1.net"))
        {
            while ((line = file.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = new List<string>(line.Split(new char[] { ',' }));

                if (parts.Count != 2)
                {
                    continue;
                }

                var hostname = parts[1];

                var candidatesAndFeatures = exactCityAdmin1FeaturesGenerator.GenerateCandidatesAndFeatures(hostname);

                if (candidatesAndFeatures.Count > 0)
                {
                    Console.WriteLine(candidatesAndFeatures);
                }
            }
        }
        */

        /*
        var exactCityCountryFeaturesGenerator = new ExactCityCountryFeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(path))
        {
            exactCityCountryFeaturesGenerator.IngestCityEntity(entity);
        }

        string line;

        using (var file = new StreamReader(@"C:\Users\zmarty\Downloads\20161109-rdns\frontiernet.net"))
        {
            while ((line = file.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = new List<string>(line.Split(new char[] { ',' }));

                if (parts.Count != 2)
                {
                    continue;
                }

                var hostname = parts[1];

                var candidatesAndFeatures = exactCityCountryFeaturesGenerator.GenerateCandidatesAndFeatures(hostname);

                if (candidatesAndFeatures.Count > 0)
                {
                    Console.WriteLine(candidatesAndFeatures);
                }
            }
        }
        */

        /*
        //var hostname = "ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.comcast.net";
        var hostname = "ce-salmor0w03w.cpe.or.portland.comcast.net";
        var matches = matcher.FindMatches(hostname);

        foreach (var match in matches)
        {
            if (match.Value.Count >= 2)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, " ---> {0}", string.Join(", ", match.Value)));
            }
        }

        Console.WriteLine(matches);
        */

        /*
        var admin1Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin1CodesASCII.txt");
        var admin2Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin2Codes.txt");
        var countriesDict = GeonamesCountriesParser.ParseToDict(@"C:\Projects\Whois\DNS\countryInfo.txt");

        foreach (var entity in GeonamesCitiesParser.Parse(@"C:\Projects\Whois\DNS\cities1000.txt", admin1Dict, admin2Dict, countriesDict))
        {
            Console.WriteLine(entity);
        }
        */

        /*
        var admin1Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin1CodesASCII.txt");
        var admin2Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin2Codes.txt");
        var countriesDict = GeonamesCountriesParser.ParseToDict(@"C:\Projects\Whois\DNS\countryInfo.txt");

        var exactCityCountryFeaturesGenerator = new ExactCityCountryFeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(@"C:\Projects\Whois\DNS\cities1000.txt", admin1Dict, admin2Dict, countriesDict))
        {
            exactCityCountryFeaturesGenerator.IngestCityEntity(entity);
        }

        var candidatesAndFeatures = exactCityCountryFeaturesGenerator.GenerateCandidatesAndFeatures("ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.amsterdamnl.comcast.net");

        Console.WriteLine(candidatesAndFeatures);
        */

        /*
        var admin1Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin1CodesASCII.txt");
        var admin2Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin2Codes.txt");
        var countriesDict = GeonamesCountriesParser.ParseToDict(@"C:\Projects\Whois\DNS\countryInfo.txt");

        var cityAdmin1FeaturesGenerator = new CityAdmin1FeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(@"C:\Projects\Whois\DNS\cities1000.txt", admin1Dict, admin2Dict, countriesDict))
        {
            cityAdmin1FeaturesGenerator.IngestCityEntity(entity);
        }

        var candidatesAndFeatures = cityAdmin1FeaturesGenerator.GenerateCandidatesAndFeatures("ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.amsterdamnl.seattlewa.comcast.net");

        Console.WriteLine(candidatesAndFeatures);
        */

        /*
        var admin1Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin1CodesASCII.txt");
        var admin2Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin2Codes.txt");
        var countriesDict = GeonamesCountriesParser.ParseToDict(@"C:\Projects\Whois\DNS\countryInfo.txt");

        var exactCityFeaturesGenerator = new ExactCityFeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(@"C:\Projects\Whois\DNS\cities1000.txt", admin1Dict, admin2Dict, countriesDict))
        {
            exactCityFeaturesGenerator.IngestCityEntity(entity);
        }

        var candidatesAndFeatures = exactCityFeaturesGenerator.GenerateCandidatesAndFeatures("ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.amsterdamnl.comcast.net");

        Console.WriteLine(candidatesAndFeatures);
        */

        /*
        var admin1Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin1CodesASCII.txt");
        var admin2Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin2Codes.txt");
        var countriesDict = GeonamesCountriesParser.ParseToDict(@"C:\Projects\Whois\DNS\countryInfo.txt");

        var alternateCityFeaturesGenerator = new AlternateCityFeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(@"C:\Projects\Whois\DNS\cities1000.txt", admin1Dict, admin2Dict, countriesDict))
        {
            alternateCityFeaturesGenerator.IngestCityEntity(entity);
        }

        var candidatesAndFeatures = alternateCityFeaturesGenerator.GenerateCandidatesAndFeatures("ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.amsterdamnl.comcast.net");

        Console.WriteLine(candidatesAndFeatures);
        */

        /*
        var admin1Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin1CodesASCII.txt");
        var admin2Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin2Codes.txt");
        var countriesDict = GeonamesCountriesParser.ParseToDict(@"C:\Projects\Whois\DNS\countryInfo.txt");

        var noVowelsCityFeaturesGenerator = new NoVowelsCityFeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(@"C:\Projects\Whois\DNS\cities1000.txt", admin1Dict, admin2Dict, countriesDict))
        {
            noVowelsCityFeaturesGenerator.IngestCityEntity(entity);
        }

        var candidatesAndFeatures = noVowelsCityFeaturesGenerator.GenerateCandidatesAndFeatures("ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.amsterdamnl.seattlewa.sttl.comcast.net");
        */

        /*
        var admin1Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin1CodesASCII.txt");
        var admin2Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin2Codes.txt");
        var countriesDict = GeonamesCountriesParser.ParseToDict(@"C:\Projects\Whois\DNS\countryInfo.txt");

        var firstLettersCityFeaturesGenerator = new FirstLettersCityFeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(@"C:\Projects\Whois\DNS\cities1000.txt", admin1Dict, admin2Dict, countriesDict))
        {
            firstLettersCityFeaturesGenerator.IngestCityEntity(entity);
        }

        var candidatesAndFeatures = firstLettersCityFeaturesGenerator.GenerateCandidatesAndFeatures("ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.amsterdamnl.seattlewa.sttl.seat.comcast.net");

        Console.WriteLine(candidatesAndFeatures);
        */

        /*
        var admin1Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin1CodesASCII.txt");
        var admin2Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin2Codes.txt");
        var countriesDict = GeonamesCountriesParser.ParseToDict(@"C:\Projects\Whois\DNS\countryInfo.txt");

        var cityAbbreviationsFeaturesGenerator = new CityAbbreviationsFeaturesGenerator();

        foreach (var entity in GeonamesCitiesParser.Parse(@"C:\Projects\Whois\DNS\cities1000.txt", admin1Dict, admin2Dict, countriesDict))
        {
            cityAbbreviationsFeaturesGenerator.IngestCityEntity(entity);
        }

        var candidatesAndFeatures = cityAbbreviationsFeaturesGenerator.GenerateCandidatesAndFeatures("ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.amsterdamnl.seattlewa.sttl.seat.comcast.net");

        Console.WriteLine(candidatesAndFeatures);
        */

        /*
        var admin1Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin1CodesASCII.txt");
        var admin2Dict = GeonamesAdminParser.ParseToDict(@"C:\Projects\Whois\DNS\admin2Codes.txt");
        var countryEntities = GeonamesCountriesParser.ParseToList(@"C:\Projects\Whois\DNS\countryInfo.txt");
        var countryCodesDict = GeonamesCountriesParser.ListToISOCodeDict(countryEntities);

        var cityFeatureGenerators = new List<CityFeaturesGenerator>()
        {
            new AlternateCityAbbreviationsFeaturesGenerator(),
            new AlternateCityFeaturesGenerator(),
            new CityAbbreviationsFeaturesGenerator(),
            new CityAdmin1FeaturesGenerator(),
            new CityCountryFeaturesGenerator(),
            new ExactCityFeaturesGenerator(),
            new FirstLettersCityFeaturesGenerator(),
            new NoVowelsCityFeaturesGenerator()
        };

        foreach (var entity in GeonamesCitiesParser.Parse(@"C:\Projects\Whois\DNS\cities1000.txt", admin1Dict, admin2Dict, countryCodesDict))
        {
            foreach (var cityFeatureGenerator in cityFeatureGenerators)
            {
                cityFeatureGenerator.IngestCityEntity(entity);
            }
        }

        var hostname = "ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.amsterdamnl.seattlewa.sttl.seat.comcast.net";

        var candidatesAndFeatures = new Dictionary<GeonamesCityEntity, Features>();

        foreach (var cityFeatureGenerator in cityFeatureGenerators)
        {
            var localCandidatesAndFeatures = cityFeatureGenerator.GenerateCandidatesAndFeatures(hostname);

            foreach (var localCandidatesAndFeaturesEntry in localCandidatesAndFeatures)
            {
                var cityEntity = localCandidatesAndFeaturesEntry.Key;
                var localFeatures = localCandidatesAndFeaturesEntry.Value;

                Features globalFeatures;

                if (!candidatesAndFeatures.TryGetValue(cityEntity, out globalFeatures))
                {
                    globalFeatures = new Features();
                    candidatesAndFeatures[cityEntity] = globalFeatures;
                }

                foreach (var localFeatureEntry in localFeatures)
                {
                    // TODO: Remove extra check
                    if (globalFeatures.ContainsKey(localFeatureEntry.Key))
                    {
                        Console.WriteLine("This should not happen");
                    }

                    globalFeatures[localFeatureEntry.Key] = localFeatureEntry.Value;
                }
            }
        }

        foreach (var candidatesAndFeaturesEntry in candidatesAndFeatures)
        {
            var globalFeatures = candidatesAndFeaturesEntry.Value;

            foreach (var cityFeatureGenerator in cityFeatureGenerators)
            {
                var defaultFeatures = cityFeatureGenerator.FeatureDefaults;

                foreach (var defaultFeaturesEntry in defaultFeatures)
                {
                    if (!globalFeatures.ContainsKey(defaultFeaturesEntry.Key))
                    {
                        globalFeatures[defaultFeaturesEntry.Key] = defaultFeaturesEntry.Value;
                    }
                }
            }
        }

        Console.WriteLine(candidatesAndFeatures);
        */

        /*
        var aggregator = new CityFeaturesAggregator(
            citiesPath: @"C:\Projects\Whois\DNS\cities1000.txt",
            admin1Path: @"C:\Projects\Whois\DNS\admin1CodesASCII.txt",
            admin2Path: @"C:\Projects\Whois\DNS\admin2Codes.txt",
            countriesPath: @"C:\Projects\Whois\DNS\countryInfo.txt");

        //var hostname = "ge-0-8-3-5-3570-sur01.elmhurst.il.chicago.amsterdamnl.seattlewa.sttl.seat.comcast.net.bucharest.ro";
        var hostname = "sttl.wa.usa.frontiernet.us";
        var hostnameParts = HostnameSplitter.Split(hostname);
        var candidatesAndFeatures = aggregator.GenerateCandidatesForHostname(hostnameParts);

        foreach (var candidateEntry in candidatesAndFeatures)
        {
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine(candidateEntry.Key);
            Console.WriteLine("----------------");

            foreach (var featureEntry in candidateEntry.Value)
            {
                if (featureEntry.Value != null)
                {
                    if (featureEntry.Value is bool && !((bool)featureEntry.Value))
                    {
                        continue;
                    }

                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} = {1}", featureEntry.Key, featureEntry.Value));
                }
            }
        }
        */

        /*
        var aggregator = new CityFeaturesAggregator(
            citiesPath: @"C:\Projects\Whois\DNS\cities1000.txt",
            admin1Path: @"C:\Projects\Whois\DNS\admin1CodesASCII.txt",
            admin2Path: @"C:\Projects\Whois\DNS\admin2Codes.txt",
            countriesPath: @"C:\Projects\Whois\DNS\countryInfo.txt");

        string line;
        var counter = 0;
        var globalCombinedCounts = new Dictionary<string, int>();

        ////using (var file = new StreamReader(@"C:\Projects\ReverseDNSDataset\20161109-rdns\comcast.nohsd1.net"))
        using (var file = new StreamReader(@"C:\Projects\ReverseDNSDataset\20161109-rdns\20161109-rdns"))
        {
            while ((line = file.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                counter++;

                if (counter % 10000 == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine(counter);
                    Console.WriteLine();

                    foreach (var globalCombinedCountsEntry in globalCombinedCounts)
                    {
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} = {1} ({2}%)", globalCombinedCountsEntry.Key, globalCombinedCountsEntry.Value, Math.Round((globalCombinedCountsEntry.Value / ((1.0f)*counter)) * 100, 1) ));
                    }
                }

                var parts = new List<string>(line.Split(new char[] { ',' }));

                if (parts.Count != 2)
                {
                    continue;
                }

                var hostname = parts[1];
                var hostnameParts = HostnameSplitter.Split(hostname);
                var candidatesAndFeatures = aggregator.GenerateCandidatesForHostname(hostnameParts);

                var localAggregatedFeatureNames = new HashSet<string>();

                foreach (var candidateEntry in candidatesAndFeatures)
                {
                    var cityEntity = candidateEntry.Key;

                    foreach (var featureEntry in candidateEntry.Value)
                    {
                        if (featureEntry.Value != null)
                        {
                            if (featureEntry.Value is bool && !((bool)featureEntry.Value))
                            {
                                continue;
                            }

                            localAggregatedFeatureNames.Add(featureEntry.Key.ToString());
                        }
                    }
                }

                foreach (var aggregatedFeatureName in localAggregatedFeatureNames)
                {
                    if (!globalCombinedCounts.ContainsKey(aggregatedFeatureName))
                    {
                        globalCombinedCounts[aggregatedFeatureName] = 0;
                    }

                    globalCombinedCounts[aggregatedFeatureName]++;
                }
            }
        }
        */

        /*
        var domainParser = new DomainParser(new FileTldRuleProvider("public_suffix_list.dat"));
        var hostnameCounts = new Dictionary<string, int>();

        var hostnamesProcessingAttemptedCounter = 0;
        var globalCounter = 0;

        //var sampler = new DataSampler();
        var sampler = new StratifiedDataSampler();
        var trainingData = sampler.Sample(
            citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
            admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
            admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
            countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt",
            groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt",
            trueNegativesMultiplier: 5,
            shouldProcessHostname: delegate(string hostname, List<string> hostnameParts, int storedTruePositivesCount, int storedTrueNegativesCount)
            {
                globalCounter++;

                if (globalCounter % 10000 == 0)
                {
                    Console.WriteLine($"globalCounter = {globalCounter}");
                    Console.WriteLine();
                }

                try
                {
                    if (string.IsNullOrEmpty(hostname) || !hostname.Contains(".") || hostname.Contains(" ") || hostname.Contains(","))
                    {
                        return false;
                    }

                    if (hostname.ToLowerInvariant().Contains("comcast.net"))
                    {
                        return false;
                    }

                    var parseHostnameTask = domainParser.ParseAsync(hostname);
                    parseHostnameTask.Wait();
                    var parsedHostname = parseHostnameTask.Result;

                    if (!string.IsNullOrEmpty(parsedHostname.RegistrableDomain))
                    {
                        int currentHostnameCount;

                        if (hostnameCounts.TryGetValue(parsedHostname.RegistrableDomain, out currentHostnameCount))
                        {
                            if (currentHostnameCount < 500)
                            {
                                currentHostnameCount++;
                                hostnameCounts[parsedHostname.RegistrableDomain] = currentHostnameCount;

                                hostnamesProcessingAttemptedCounter++;
                                return true;
                            }
                        }
                        else
                        {
                            currentHostnameCount = 1;
                            hostnameCounts[parsedHostname.RegistrableDomain] = currentHostnameCount;

                            hostnamesProcessingAttemptedCounter++;
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ////Console.WriteLine($"Exception, could not parse hostname: {hostname} due to Exception: {ex}");
                }

                //if (!hostname.Contains("comcast"))
                //{
                //    hostnamesProcessingAttemptedCounter++;
                //    return true;
                //}

                return false;
            },
            shouldContinueIngestingNewHostnames: delegate (string hostname, List<string> hostnameParts, int storedTruePositivesCount, int storedTrueNegativesCount)
            {
                return hostnamesProcessingAttemptedCounter < 500000;
            },
            showConsoleStats: delegate (string hostname, List<string> hostnameParts, int storedTruePositivesCount, int storedTrueNegativesCount, bool lastRow)
            {
                if (lastRow)
                {
                    Console.WriteLine("!!! Last Row !!!");
                }

                if (lastRow || (hostnamesProcessingAttemptedCounter % 10000 == 0))
                {
                    Console.WriteLine($"hostnamesProcessingAttemptedCounter = {hostnamesProcessingAttemptedCounter}");
                    Console.WriteLine($"storedTruePositivesCount = {storedTruePositivesCount}");
                    Console.WriteLine($"storedTrueNegativesCount = {storedTrueNegativesCount}");
                    Console.WriteLine();
                }
            });
        */

        /*
        var classifier = new C45Classifier();
        var crossValidator = new CrossValidator();
        var validationResult = crossValidator.Validate(classifier, trainingData);
        */

        /*
        double trainingErrors = validationResult.Training.Mean;
        double validationErrors = validationResult.Validation.Mean;

        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Training error mean: {0}", trainingErrors));
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Validation error mean: {0}", validationErrors));
        */

        /*
        var classifier = new C45Classifier(trainingData);
        classifier.Train(trainingData.Inputs, trainingData.Outputs);

        var tree = classifier.Tree;
        tree.Save<DecisionTree>(@".\c45-classifier.bin");
        */

        /*
        var aggregator = new CityFeaturesAggregator(
            citiesPath: @"C:\Projects\ReverseDNS\cities1000.txt",
            admin1Path: @"C:\Projects\ReverseDNS\admin1CodesASCII.txt",
            admin2Path: @"C:\Projects\ReverseDNS\admin2Codes.txt",
            countriesPath: @"C:\Projects\ReverseDNS\countryInfo.txt");
        var trainingData = new TrainingData(tableName: "ReverseDNSGeolocation Training", featuresAggregator: aggregator);
        var classifier = new C45Classifier(trainingData, serializedClassifierPath: @".\c45-classifier.bin");

        var rules = classifier.Tree.ToRules();
        Console.WriteLine(rules);
        */

        /*
        var classifier = new C45Classifier(trainingData);
        classifier.Train(trainingData.Inputs, trainingData.Outputs);

        //var hostname = "sttl.wa.usa.frontiernet.net";
        var targetHostname = "ce-salmor0w03w.cpe.or.portland.comcast.net";
        var targetHostnameParts = HostnameSplitter.SplitExceptBlacklist(targetHostname);
        var candidatesAndFeatures = trainingData.FeaturesAggregator.GenerateCandidatesForHostname(targetHostnameParts);

        foreach (var candidateEntry in candidatesAndFeatures)
        {
            var candidate = candidateEntry.Key;
            var features = candidateEntry.Value;

            var row = trainingData.CreateTrainingRow(features);
            var rowArr = row.ToArray<int>(trainingData.InputColumnNames);

            var label = classifier.Tree.Decide(rowArr);

            if (label == 1)
            {
                Console.WriteLine($"{candidate} - {label}");
                Console.WriteLine("---");

                foreach (var featureEntry in trainingData.FeatureDefaultsValueTypes)
                {
                    Console.WriteLine($"{featureEntry.Key} - {features[featureEntry.Key]}");
                }

                Console.WriteLine("-------------------------------------------");
                Console.WriteLine();
            }
        }
        */


        /*
        var teacher = new SequentialMinimalOptimization<Gaussian>()
        {
            UseComplexityHeuristic = true,
            UseKernelEstimation = true // Estimate the kernel from the data
        };

        var svm = teacher.Learn(trainingData.Inputs, trainingData.Outputs);

        var targetHostname = "ce-salmor0w03w.cpe.or.portland.comcast.net";
        var targetHostnameParts = HostnameSplitter.SplitExceptBlacklist(targetHostname);
        var candidatesAndFeatures = trainingData.FeaturesAggregator.GenerateCandidatesForHostname(targetHostnameParts);

        foreach (var candidateEntry in candidatesAndFeatures)
        {
            var candidate = candidateEntry.Key;
            var features = candidateEntry.Value;

            var row = trainingData.CreateTrainingRow(features);
            var rowArr = row.ToArray<double>(trainingData.InputColumnNames);

            var label = svm.Decide(rowArr);

            if (label)
            {
                Console.WriteLine($"{candidate} - {label}");
                Console.WriteLine("---");

                foreach (var featureEntry in trainingData.FeatureDefaultsValueTypes)
                {
                    Console.WriteLine($"{featureEntry.Key} - {features[featureEntry.Key]}");
                }

                Console.WriteLine("-------------------------------------------");
                Console.WriteLine();
            }
        }
        */

        /*
        var learner = new IterativeReweightedLeastSquares<LogisticRegression>()
        {
            Tolerance = 1e-4,  // Let's set some convergence parameters
            Iterations = 100,  // maximum number of iterations to perform
            Regularization = 0
        };

        var regression = learner.Learn(trainingData.Inputs, trainingData.Outputs);

        ////var targetHostname = "ce-salmor0w03w.cpe.or.portland.comcast.net";
        ////var targetHostname = "47-144-198-239.lsan.ca.frontiernet.net";
        var targetHostname = "47-184-244-118.dlls.tx.frontiernet.net";

        var targetHostnameParts = HostnameSplitter.SplitExceptBlacklist(targetHostname);
        var candidatesAndFeatures = trainingData.FeaturesAggregator.GenerateCandidatesForHostname(targetHostnameParts);

        foreach (var candidateEntry in candidatesAndFeatures)
        {
            var candidate = candidateEntry.Key;
            var features = candidateEntry.Value;

            var row = trainingData.CreateTrainingRow(features);
            var rowArr = row.ToArray<double>(trainingData.InputColumnNames);

            bool label;

            var probability = regression.Probability(rowArr, out label);

            if (label)
            {
                Console.WriteLine($"{candidate} - {label} - {probability}");
                Console.WriteLine("---");

                foreach (var featureEntry in trainingData.FeatureDefaultsValueTypes)
                {
                    Console.WriteLine($"{featureEntry.Key} - {features[featureEntry.Key]}");
                }

                Console.WriteLine("-------------------------------------------");
                Console.WriteLine();
                ////Console.ReadKey();
            }
        }
        */

        /*
        //var classifier = new LogisticRegressionClassifier();
        var classifier = new C45Classifier();
        //var classifier = new SVMClassifier();
        var crossValidator = new CrossValidator();
        var validationResult = crossValidator.Validate(classifier, trainingData);

        double trainingErrors = validationResult.Training.Mean;
        double validationErrors = validationResult.Validation.Mean;

        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Training error mean: {0}", trainingErrors));
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Validation error mean: {0}", validationErrors));
        */

        /*
        // False below is for argument nullDefaultsAllowed
        var generatorInstancesDict = ReflectionUtils.GetInstanceDictOfType<CityFeaturesGenerator>(false);

        // Load all add-on feature generators
        var cityAddOnFeatureGenerators = ReflectionUtils.GetInstancesOfType<AddOnCityFeaturesGenerator>(false);

        Console.WriteLine(generatorInstancesDict);
        Console.WriteLine(cityAddOnFeatureGenerators);
        */

        //TrainSaveC45Classifier();
        //TestComcastC45();

        //TrainSaveLogisticRegressionClassifier();
        //TestLogisticRegressionClassifier();
        //TestC45Classifier(groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-frontier.txt", whitelistDomains: new HashSet<string>() { "frontiernet.net" }, printRules: true);
        //TestC45Classifier(groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", whitelistDomains: new HashSet<string>() { "ocn.ne.jp" }, printRules: true);
        //TestC45Classifier(groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT-comcast.txt", whitelistDomains: new HashSet<string>() { "comcast.net" }, printRules: true);
        //TestC45Classifier(groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", whitelistDomains: new HashSet<string>() { "frontiernet.net", "comcast.net" }, printRules: true);
        //TestC45Classifier(groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", printRules: true);
        //TestC45Classifier(groundTruthPath: @"C:\Projects\ReverseDNS\ReverseDNSDataset\2017-01-18-ReverseDNS-WithGT.txt", whitelistDomains: new HashSet<string>() { "comcast.net" }, printRules: true);

        //TestC45ClassifierOnHostname("c-76-111-167-75.hsd1.pa.comcast.net", serializedClassifierPath: @".\c45-classifier.bin");
        //TestC45ClassifierOnHostname("lax.hawkhost.com", serializedClassifierPath: @".\c45-classifier.bin");
        //TestC45ClassifierOnHostname("50-46-223-84.evrt.wa.frontiernet.net", serializedClassifierPath: @".\c45-classifier.bin");
        //TestC45ClassifierOnHostname("22.65.0.1.megaegg.ne.jp", serializedClassifierPath: @".\c45-classifier.bin");
        //TestC45ClassifierOnHostname("loop0.brg_mel01_7206.bigredgroup.net.au", serializedClassifierPath: @".\c45-classifier.bin");
        //TestC45ClassifierOnHostname("pl1409.nas81e.soka.nttpc.ne.jp", serializedClassifierPath: @".\c45-classifier.bin");
        //TestC45ClassifierOnHostname("pdx1-a5u56-acc.sd.dreamhost.com", serializedClassifierPath: @".\c45-classifier.bin");
        //TestLogisticRegressionClassifierOnHostname("50-46-223-84.evrt.wa.frontiernet.net", serializedClassifierPath: @".\logistic-regression-classifier.bin");

        /*
        var domainParser = new DomainParser(new FileTldRuleProvider("public_suffix_list.dat"));
        var parseHostnameTask = domainParser.ParseAsync("118.41");
        parseHostnameTask.Wait();
        Console.WriteLine(parseHostnameTask.Result);
        */

        ////Console.WriteLine(PublicSuffixMatcher.HostnameHasValidSuffix("bla1.bla2.bla3.triton.zone"));
        ////Console.WriteLine(PublicSuffixMatcher.HostnameHasValidSuffix("bla.cloudapp.nex"));

        /*
        var languages = new HashSet<string>();

        foreach (var entity in GeonamesAlternateNamesParser.Parse(@"C:\Projects\ReverseDNS\alternateNames.txt"))
        {
            ////if (entity.ISOLanguage.Length >= 4)
            if (entity.ISOLanguage == string.Empty)
            {
                languages.Add(entity.ISOLanguage);
            }
        }
        */

        /*
        CLLIGeo.FindLocations(
            first6CityTsvPath: @"C:\Projects\ReverseDNS\first6-city.csv", 
            username: "zmarty",
            outFilePath: @"C:\Projects\ReverseDNS\first6-city-geonames.tsv");
            */

        /*
        UNLOCODEGeo.OutputLowercaseCodes(
            unlocodeCsvPath: @"C:\Projects\ReverseDNS\unlocode.csv",
            justLocationOutputPath: @"C:\Projects\ReverseDNS\unlocode-location-codes.txt",
            combinedCodeLocationOutputPath: @"C:\Projects\ReverseDNS\unlocode-combined-codes.txt");
        */
    }
}
