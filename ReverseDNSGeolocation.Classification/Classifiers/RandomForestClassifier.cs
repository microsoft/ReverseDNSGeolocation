namespace ReverseDNSGeolocation.Classification
{
    using Accord.IO;
    using Accord.MachineLearning.DecisionTrees;
    using Features;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RandomForestClassifier : IClassifier
    {
        public RandomForestLearning RandomForestLearner { get; set; }

        public RandomForest RandomForest { get; set; }

        public IClassifier CreateInstance(FeatureValueTypes featureDefaultsValueTypes, FeatureGranularities featureGranularities, string serializedClassifierPath = null)
        {
            return new RandomForestClassifier(featureDefaultsValueTypes, featureGranularities, serializedClassifierPath);
        }

        public RandomForestClassifier()
        {
        }

        public RandomForestClassifier(FeatureValueTypes featureDefaultsValueTypes, FeatureGranularities featureGranularities, string serializedClassifierPath = null)
        {
            var featureKinds = new List<DecisionVariable>();

            foreach (var entry in featureDefaultsValueTypes)
            {
                var featureName = entry.Key.ToString();
                var featureType = entry.Value;

                var featureGranularity = featureGranularities[entry.Key];

                if (featureGranularity == FeatureGranularity.Continuous)
                {
                    featureKinds.Add(new DecisionVariable(featureName, DecisionVariableKind.Continuous));
                }
                else if (featureGranularity == FeatureGranularity.Discrete)
                {
                    var decisionVar = new DecisionVariable(featureName, DecisionVariableKind.Discrete);

                    // TODO: Fix uint, there is no Accord.UIntRange
                    if (featureType == (typeof(int)) || featureType == (typeof(int?)) || featureType == (typeof(uint)) || featureType == (typeof(uint?)))
                    {
                        decisionVar.Range = new Accord.IntRange(min: int.MinValue, max: int.MaxValue);
                    }
                    else if (featureType == (typeof(byte)) || featureType == (typeof(byte?)))
                    {
                        decisionVar.Range = new Accord.IntRange(min: byte.MinValue, max: byte.MaxValue);
                    }

                    featureKinds.Add(decisionVar);
                }
                else
                {
                    throw new ArgumentException("Unknown feature granularity");
                }
            }

            var featureKindsArr = featureKinds.ToArray<DecisionVariable>();

            this.RandomForestLearner = new RandomForestLearning(featureKindsArr)
            {
                NumberOfTrees = 10
            };

            if (serializedClassifierPath != null)
            {
                this.RandomForest = Serializer.Load<RandomForest>(serializedClassifierPath);
            }
        }

        public void Train(double[][] trainingInputs, int[] trainingOutputs)
        {
            this.RandomForest = this.RandomForestLearner.Learn(trainingInputs, trainingOutputs);
        }

        public int[] Decide(double[][] input)
        {
            return this.RandomForest.Decide(input);
        }

        public int Decide(double[] input)
        {
            return this.RandomForest.Decide(input);
        }

        public double Probability(double[] input, out int label)
        {
            var result = this.RandomForest.Decide(input);
            label = result;
            return result;
        }
    }
}
