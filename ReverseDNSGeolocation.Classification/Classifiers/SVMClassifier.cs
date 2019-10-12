namespace ReverseDNSGeolocation.Classification
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Accord.IO;
    using Accord.MachineLearning.VectorMachines;
    using Accord.MachineLearning.VectorMachines.Learning;
    using Accord.Statistics.Kernels;
    using Features;

    public class SVMClassifier : IClassifier
    {
        public SequentialMinimalOptimization<Gaussian> Teacher { get; set; }

        public SupportVectorMachine<Gaussian> Svm { get; set; }

        public IClassifier CreateInstance(FeatureValueTypes featureDefaultsValueTypes, FeatureGranularities featureGranularities, string serializedClassifierPath = null)
        {
            return new SVMClassifier(featureDefaultsValueTypes, featureGranularities, serializedClassifierPath);
        }

        public SVMClassifier()
        {
        }

        public SVMClassifier(FeatureValueTypes featureDefaultsValueTypes, FeatureGranularities featureGranularities, string serializedClassifierPath = null)
        {
            Teacher = new SequentialMinimalOptimization<Gaussian>()
            {
                UseComplexityHeuristic = true,
                UseKernelEstimation = true // Estimate the kernel from the data
            };


            if (serializedClassifierPath != null)
            {
                this.Svm = Serializer.Load<SupportVectorMachine<Gaussian>>(serializedClassifierPath);
            }
        }

        public void Train(double[][] trainingInputs, int[] trainingOutputs)
        {
            this.Svm = this.Teacher.Learn(trainingInputs, trainingOutputs);
        }

        public int[] Decide(double[][] input)
        {
            var results = this.Svm.Decide(input);

            var intResults = new int[results.Length];

            for (var i = 0; i < results.Length; i++)
            {
                intResults[i] = results[i] ? 1 : 0;
            }

            return intResults;
        }

        public int Decide(double[] input)
        {
            var result = this.Svm.Decide(input);
            return result ? 1 : 0;
        }

        public double Probability(double[] input, out int label)
        {
            bool rawLabel;

            var probability = this.Svm.Probability(input, out rawLabel);

            label = rawLabel ? 1 : 0;

            return probability;
        }
    }
}
