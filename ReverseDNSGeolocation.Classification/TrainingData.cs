namespace ReverseDNSGeolocation.Classification
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Accord.Math;
    using Accord.Statistics.Filters;
    using Features;
    using GeonamesParsers;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;
    using System.Runtime.Serialization;

    [Serializable]
    public class TrainingData
    {
        private const string outputColumnName = "Label";

        public DataTable Table { get; set; }

        public FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public FeatureGranularities FeatureGranularities { get; set; }

        public Features FeatureDefaults { get; set; }

        public string[] InputColumnNames { get; set; }

        public double[][] Inputs { get; set; }

        public int[] Outputs { get; set; }

        public CityFeaturesAggregator FeaturesAggregator
        {
            get
            {
                return this.backingFeaturesAggregator;
            }

            set
            {
                this.backingFeaturesAggregator = value;
            }
        }

        [NonSerialized]
        private CityFeaturesAggregator backingFeaturesAggregator = null;

        public TrainingData(string tableName, CityFeaturesAggregator featuresAggregator)
        {
            this.Table = new DataTable(tableName);
            this.FeaturesAggregator = featuresAggregator;
            this.FeatureDefaultsValueTypes = featuresAggregator.FeatureDefaultsValueTypes;
            this.FeatureDefaults = featuresAggregator.FeatureDefaults;
            this.FeatureGranularities = featuresAggregator.FeatureGranularities;

            var inputColumnNames = new List<string>();

            foreach (var entry in this.FeatureDefaultsValueTypes)
            {
                var featureName = entry.Key.ToString();
                var featureType = entry.Value;

                inputColumnNames.Add(featureName);

                if (featureType.IsGenericType && featureType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // All rows in DataTable are considered to be Nullable, but we need to specify the non-nullable types when we define the columns... for some reason.
                    var nonNullableType = Nullable.GetUnderlyingType(featureType);
                    this.Table.Columns.Add(featureName, nonNullableType);
                }
                else
                {
                    this.Table.Columns.Add(featureName, featureType);
                }
            }

            this.InputColumnNames = inputColumnNames.ToArray<string>();

            this.Table.Columns.Add(outputColumnName, typeof(bool));
        }

        /*
        public static double[][] DeserializeInputs(string inPath)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(path: inPath, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.Read))
            {
                var inputs = (double[][])formatter.Deserialize(stream);
                stream.Close();
                return inputs;
            }
        }

        public static int[] DeserializeOutputs(string inPath)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(path: inPath, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.Read))
            {
                var inputs = (int[])formatter.Deserialize(stream);
                stream.Close();
                return inputs;
            }
        }

        public void SerializeInputsTo(string outPath)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new FileStream(path: outPath, mode: FileMode.Create, access: FileAccess.Write, share: FileShare.None))
            {
                formatter.Serialize(stream, this.Inputs);
                stream.Close();
            }
        }

        public void SerializeOutputsTo(string outPath)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new FileStream(path: outPath, mode: FileMode.Create, access: FileAccess.Write, share: FileShare.None))
            {
                formatter.Serialize(stream, this.Outputs);
                stream.Close();
            }
        }
        */

        public void SerializeTo(string outPath)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new FileStream(path: outPath, mode: FileMode.Create, access: FileAccess.Write, share: FileShare.None))
            {
                formatter.Serialize(stream, this);
                stream.Close();
            }
        }

        public static TrainingData DeserializeFrom(string inPath, CityFeaturesAggregator aggregator)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(path: inPath, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.Read))
            {
                var trainingData = (TrainingData)formatter.Deserialize(stream);
                stream.Close();

                trainingData.backingFeaturesAggregator = aggregator;

                return trainingData;
            }
        }

        public DataRow CreateTrainingRow(Features locationFeatures, bool? isValidLocation = false)
        {
            var newRow = this.Table.NewRow();

            foreach (var entry in locationFeatures)
            {
                var featureName = entry.Key.ToString();
                var featureValue = entry.Value;

                if (featureValue != null)
                {
                    newRow[featureName] = featureValue;
                }
            }

            if (isValidLocation != null)
            {
                newRow[outputColumnName] = isValidLocation.Value;
            }

            return newRow;
        }

        public void AddTrainingRow(DataRow newRow)
        {
            this.Table.Rows.Add(newRow);
        }

        public void FinalizeData()
        {
            this.Inputs = this.Table.ToJagged(this.InputColumnNames);
            this.Outputs = this.Table.ToArray<int>(outputColumnName);
        }
    }
}
