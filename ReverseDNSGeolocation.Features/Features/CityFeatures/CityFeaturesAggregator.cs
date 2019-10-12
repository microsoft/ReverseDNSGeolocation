namespace ReverseDNSGeolocation.Features
{
    using CityFeatures.AddOnFeatures;
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Threading.Tasks;


    [Serializable]
    public class CityFeaturesAggregator
    {
        private List<CityFeaturesGenerator> cityFeatureGenerators;
        private List<AddOnCityFeaturesGenerator> cityAddOnFeatureGenerators;
        private FeaturesConfig featuresConfig;

        public Dictionary<string, Stopwatch> PrimaryFeatureStopWatches { get; set; }
        public Stopwatch SecondaryFeaturesStopWatch { get; set; }
        public Dictionary<string, int> FeatureHostnameHitCounts { get; set; }
        public int TotalHostnamesSeen { get; set; }
        public int TotalCandidatesSeen { get; set; }

        public CityFeaturesAggregator(
            string citiesPath, 
            string alternateNamesPath, 
            string admin1Path, 
            string admin2Path, 
            string countriesPath, 
            string clliPath, 
            string unlocodePath,
            List<CityFeaturesGenerator> cityFeatureGenerators = null, 
            List<AddOnCityFeaturesGenerator> cityAddOnFeatureGenerators = null,
            List<CityFeaturesGenerator> externalCityFeatureGenerators = null,
            List<AddOnCityFeaturesGenerator> externalCityAddOnFeatureGenerators = null,
            FeaturesConfig featuresConfig = null) 
            : this(cityFeatureGenerators, cityAddOnFeatureGenerators, externalCityFeatureGenerators, externalCityAddOnFeatureGenerators, featuresConfig)
        {
            var alternateNamesDict = GeonamesAlternateNamesParser.ParseToDict(alternateNamesPath);
            var admin1Dict = GeonamesAdminParser.ParseToDict(admin1Path);
            var admin2Dict = GeonamesAdminParser.ParseToDict(admin2Path);
            var countryEntities = GeonamesCountriesParser.ParseToList(countriesPath);
            var countryCodesDict = GeonamesCountriesParser.ListToISOCodeDict(countryEntities);
            var geonamesIdsToCLLICodes = CLLICodesParser.ParseToDict(clliPath);
            
            Dictionary<int, HashSet<string>> geonamesIdsToUNLOCODECodes = null;

            // Needed if enabling ExactUNLOCODEFeaturesGenerator below
            /*
            if (unlocodePath != null)
            {
                geonamesIdsToUNLOCODECodes = UNLOCODECodesParser.ParseToDict(unlocodePath);
            }
            */

            var total = 0;
            var withPop = 0;

            foreach (var entity in GeonamesCitiesParser.Parse(citiesPath, alternateNamesDict, admin1Dict, admin2Dict, countryCodesDict, geonamesIdsToCLLICodes, geonamesIdsToUNLOCODECodes))
            {
                total++;

                if (this.featuresConfig.MinimumPopulation == 0 || entity.Population >= this.featuresConfig.MinimumPopulation)
                {
                    withPop++;

                    foreach (var cityFeatureGenerator in this.cityFeatureGenerators)
                    {
                        cityFeatureGenerator.IngestCityEntity(entity);
                    }
                }

                if (total % 1000 == 0)
                {
                    Console.WriteLine($"Loading cities - total: {total}, withPop: {withPop}");
                }
            }

            /*
            foreach (var cityFeatureGenerator in this.cityFeatureGenerators)
            {
                try
                {
                    Console.WriteLine($"Estimated bytes for {cityFeatureGenerator.GetType().FullName}: {FeatureUtils.EstimateObjectSizeInBytes(cityFeatureGenerator)}");
                }
                catch (Exception)
                {
                    Console.WriteLine($"Could not estimate bytes for  {cityFeatureGenerator.GetType().FullName}");
                }
            }
            */
        }

        // Serialization fails due to too much data
        /*
        public void SerializeTo(string outPath)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new FileStream(path: outPath, mode: FileMode.Create, access: FileAccess.Write, share: FileShare.None))
            {
                formatter.Serialize(stream, this);
                stream.Close();
            }
        }

        public static CityFeaturesAggregator DeserializeFrom(string inPath)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(path: inPath, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.Read))
            {
                var aggregator = (CityFeaturesAggregator)formatter.Deserialize(stream);
                stream.Close();

                return aggregator;
            }
        }
        */

        public CityFeaturesAggregator(
            List<CityFeaturesGenerator> cityFeatureGenerators = null, 
            List<AddOnCityFeaturesGenerator> cityAddOnFeatureGenerators = null,
            List<CityFeaturesGenerator> externalCityFeatureGenerators = null,
            List<AddOnCityFeaturesGenerator> externalCityAddOnFeatureGenerators = null,
            FeaturesConfig featuresConfig = null)
        {
            this.PrimaryFeatureStopWatches = new Dictionary<string, Stopwatch>();
            this.SecondaryFeaturesStopWatch = new Stopwatch();
            this.FeatureHostnameHitCounts = new Dictionary<string, int>();
            this.TotalHostnamesSeen = 0;
            this.TotalCandidatesSeen = 0;

            if (featuresConfig == null)
            {
                this.featuresConfig = new FeaturesConfig();
            }
            else
            {
                this.featuresConfig = featuresConfig;
            }

            if (cityFeatureGenerators == null)
            {
                this.cityFeatureGenerators = new List<CityFeaturesGenerator>()
                {
                    new AlternateCityAbbreviationsFeaturesGenerator(this.featuresConfig),
                    new AlternateCityFeaturesGenerator(this.featuresConfig),
                    new CityAbbreviationsFeaturesGenerator(this.featuresConfig),
                    new CityAdmin1FeaturesGenerator(this.featuresConfig),
                    new CityCountryFeaturesGenerator(this.featuresConfig),
                    new ExactCityFeaturesGenerator(this.featuresConfig),
                    new FirstLettersCityFeaturesGenerator(this.featuresConfig),
                    new NoVowelsCityFeaturesGenerator(this.featuresConfig),
                    new AirportCodeFeaturesGenerator(this.featuresConfig),
                    new ExactCLLIFeaturesGenerator(this.featuresConfig),

                    //// If you want to uncomment this, also make sure to incomment loading its dictionary above in this file!
                    ////new ExactUNLOCODEFeaturesGenerator(this.featuresConfig) //// UNLOCODE disabled
                };
            }
            else
            {
                this.cityFeatureGenerators = cityFeatureGenerators;
            }

            if (cityAddOnFeatureGenerators == null)
            {
                this.cityAddOnFeatureGenerators = new List<AddOnCityFeaturesGenerator>()
                {
                    new ExactAdmin1AddOnFeaturesGenerator(this.featuresConfig),
                    new ExactCountryAddOnFeaturesGenerator(this.featuresConfig),
                    new TLDAddOnFeaturesGenerator(this.featuresConfig),
                    new FirstLettersAdmin1AddOnFeaturesGenerator(this.featuresConfig),
                    new DomainNameAddOnFeaturesGenerator(this.featuresConfig)
                };
            }
            else
            {
                this.cityAddOnFeatureGenerators = cityAddOnFeatureGenerators;
            }

            if (externalCityFeatureGenerators != null)
            {
                this.cityFeatureGenerators.AddRange(externalCityFeatureGenerators);
            }

            if (externalCityAddOnFeatureGenerators != null)
            {
                this.cityAddOnFeatureGenerators.AddRange(externalCityAddOnFeatureGenerators);
            }

            foreach (var generator in this.cityFeatureGenerators)
            {
                var stopwatchName = generator.GetType().ToString();
                PrimaryFeatureStopWatches[stopwatchName] = new Stopwatch();
                FeatureHostnameHitCounts[stopwatchName] = 0;
            }
        }

        public Dictionary<GeonamesCityEntity, Features> GenerateCandidatesForHostname(HostnameSplitterResult parsedHostname)
        {
            this.TotalHostnamesSeen++;

            var candidatesAndFeatures = new Dictionary<GeonamesCityEntity, Features>();

            foreach (var cityFeatureGenerator in this.cityFeatureGenerators)
            {
                var stopwatchName = cityFeatureGenerator.GetType().ToString();
                PrimaryFeatureStopWatches[stopwatchName].Start();
                var localCandidatesAndFeatures = cityFeatureGenerator.GenerateCandidatesAndFeatures(parsedHostname);
                PrimaryFeatureStopWatches[stopwatchName].Stop();

                if (localCandidatesAndFeatures.Count > 0)
                {
                    FeatureHostnameHitCounts[stopwatchName]++;
                }

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
                        globalFeatures[localFeatureEntry.Key] = localFeatureEntry.Value;
                    }
                }
            }

            foreach (var candidatesAndFeaturesEntry in candidatesAndFeatures)
            {
                var cityEntity = candidatesAndFeaturesEntry.Key;
                var features = candidatesAndFeaturesEntry.Value;
                
                foreach (var addOnGenerator in this.cityAddOnFeatureGenerators)
                {
                    this.SecondaryFeaturesStopWatch.Start();
                    addOnGenerator.AppendFeatures(parsedHostname, cityEntity, features);
                    this.SecondaryFeaturesStopWatch.Stop();
                }
            }

            // We need to add the defaults outside of the feature generation because
            // if a feature does not match at all, we might still want to fill in the defaults
            if (this.featuresConfig.InitializeDefaultFeatures)
            {
                foreach (var candidatesAndFeaturesEntry in candidatesAndFeatures)
                {
                    var features = candidatesAndFeaturesEntry.Value;

                    foreach (var cityFeatureGenerator in cityFeatureGenerators)
                    {
                        var defaultFeatures = cityFeatureGenerator.FeatureDefaults;

                        foreach (var defaultFeaturesEntry in defaultFeatures)
                        {
                            if (!features.ContainsKey(defaultFeaturesEntry.Key))
                            {
                                features[defaultFeaturesEntry.Key] = defaultFeaturesEntry.Value;
                            }
                        }
                    }

                    foreach (var addOnGenerator in this.cityAddOnFeatureGenerators)
                    {
                        var defaultAddOnFeatures = addOnGenerator.FeatureDefaults;

                        foreach (var defaultAddOnFeaturesEntry in defaultAddOnFeatures)
                        {
                            if (!features.ContainsKey(defaultAddOnFeaturesEntry.Key))
                            {
                                features[defaultAddOnFeaturesEntry.Key] = defaultAddOnFeaturesEntry.Value;
                            }
                        }
                    }
                }
            }

            TotalCandidatesSeen += candidatesAndFeatures.Count;

            return candidatesAndFeatures;
        }

        public Features FeatureDefaults
        {
            get
            {
                var defaults = new Features();

                foreach (var cityFeatureGenerator in cityFeatureGenerators)
                {
                    var defaultFeatures = cityFeatureGenerator.FeatureDefaults;

                    foreach (var defaultFeaturesEntry in defaultFeatures)
                    {
                        defaults[defaultFeaturesEntry.Key] = defaultFeaturesEntry.Value;
                    }
                }

                foreach (var addOnGenerator in this.cityAddOnFeatureGenerators)
                {
                    var defaultAddOnFeatures = addOnGenerator.FeatureDefaults;

                    foreach (var defaultAddOnFeaturesEntry in defaultAddOnFeatures)
                    {
                        defaults[defaultAddOnFeaturesEntry.Key] = defaultAddOnFeaturesEntry.Value;
                    }
                }

                return defaults;
            }
        }

        public FeatureValueTypes FeatureDefaultsValueTypes
        {
            get
            {
                var defaults = new FeatureValueTypes();

                foreach (var cityFeatureGenerator in cityFeatureGenerators)
                {
                    var defaultFeatures = cityFeatureGenerator.FeatureDefaultsValueTypes;

                    foreach (var defaultFeaturesEntry in defaultFeatures)
                    {
                        defaults[defaultFeaturesEntry.Key] = defaultFeaturesEntry.Value;
                    }
                }

                foreach (var addOnGenerator in this.cityAddOnFeatureGenerators)
                {
                    var defaultAddOnFeatures = addOnGenerator.FeatureDefaultsValueTypes;

                    foreach (var defaultAddOnFeaturesEntry in defaultAddOnFeatures)
                    {
                        defaults[defaultAddOnFeaturesEntry.Key] = defaultAddOnFeaturesEntry.Value;
                    }
                }

                return defaults;
            }
        }

        public FeatureGranularities FeatureGranularities
        {
            get
            {
                var granularities = new FeatureGranularities();

                foreach (var cityFeatureGenerator in cityFeatureGenerators)
                {
                    var featureGranularities = cityFeatureGenerator.FeatureGranularities;

                    foreach (var featureGranularity in featureGranularities)
                    {
                        granularities[featureGranularity.Key] = featureGranularity.Value;
                    }
                }

                foreach (var addOnGenerator in this.cityAddOnFeatureGenerators)
                {
                    var addOnFeatureGranularities = addOnGenerator.FeatureGranularities;

                    foreach (var addOnFeatureGranularity in addOnFeatureGranularities)
                    {
                        granularities[addOnFeatureGranularity.Key] = addOnFeatureGranularity.Value;
                    }
                }

                return granularities;
            }
        }
    }
}
