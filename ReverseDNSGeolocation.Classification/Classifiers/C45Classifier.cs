namespace ReverseDNSGeolocation.Classification
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Accord.IO;
    using Accord.MachineLearning.DecisionTrees;
    using Accord.MachineLearning.DecisionTrees.Learning;
    using Features;

    public class C45Classifier : IClassifier
    {
        public C45Learning C45 { get; set; }

        public DecisionTree Tree { get; set; }

        public IClassifier CreateInstance(FeatureValueTypes featureDefaultsValueTypes, FeatureGranularities featureGranularities, string serializedClassifierPath = null)
        {
            return new C45Classifier(featureDefaultsValueTypes, featureGranularities, serializedClassifierPath);
        }

        public C45Classifier()
        {
        }

        public C45Classifier(FeatureValueTypes featureDefaultsValueTypes, FeatureGranularities featureGranularities, string serializedClassifierPath = null)
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
                    if (featureType == (typeof(int)) || featureType == (typeof(int?)) || featureType == (typeof(uint)) || featureType == (typeof(uint?)) )
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

            this.C45 = new C45Learning(featureKindsArr);

            if (serializedClassifierPath != null)
            {
                this.Tree = Serializer.Load<DecisionTree>(serializedClassifierPath);
            }
        }

        public void Train(double[][] trainingInputs, int[] trainingOutputs)
        {
            this.Tree = this.C45.Learn(trainingInputs, trainingOutputs);
        }

        public int[] Decide(double[][] input)
        {
            return this.Tree.Decide(input);
        }

        public int Decide(double[] input)
        {
            return this.Tree.Decide(input);
        }

        public double Probability(double[] input, out int label)
        {
            var result = this.Tree.Decide(input);
            label = result;
            return result;
        }
    }
}
