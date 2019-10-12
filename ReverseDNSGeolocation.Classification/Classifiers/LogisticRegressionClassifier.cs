namespace ReverseDNSGeolocation.Classification
{
    using Accord.IO;
    using Accord.Statistics.Models.Regression;
    using Accord.Statistics.Models.Regression.Fitting;
    using Features;

    public class LogisticRegressionClassifier : IClassifier
    {
        public IterativeReweightedLeastSquares<LogisticRegression> Learner { get; set; }

        public LogisticRegression Regression { get; set; }

        public IClassifier CreateInstance(FeatureValueTypes featureDefaultsValueTypes, FeatureGranularities featureGranularities, string serializedClassifierPath = null)
        {
            return new LogisticRegressionClassifier(featureDefaultsValueTypes, featureGranularities, serializedClassifierPath);
        }

        public LogisticRegressionClassifier()
        {
        }

        public LogisticRegressionClassifier(FeatureValueTypes featureDefaultsValueTypes, FeatureGranularities featureGranularities, string serializedClassifierPath = null)
        {
            Learner = new IterativeReweightedLeastSquares<LogisticRegression>()
            {
                Tolerance = 1e-4,  // Convergence parameters
                Iterations = 500,  // Maximum number of iterations to perform
                Regularization = 0
            };

            if (serializedClassifierPath != null)
            {
                this.Regression = Serializer.Load<LogisticRegression>(serializedClassifierPath);
            }
        }

        public void Train(double[][] trainingInputs, int[] trainingOutputs)
        {
            this.Regression = this.Learner.Learn(trainingInputs, trainingOutputs);
        }

        public int[] Decide(double[][] input)
        {
            var results = this.Regression.Decide(input);

            var intResults = new int[results.Length];

            for (var i = 0; i< results.Length; i++)
            {
                intResults[i] = results[i] ? 1 : 0;
            }

            return intResults;
        }

        public int Decide(double[] input)
        {
            var result = this.Regression.Decide(input);
            return result ? 1 : 0; 
        }

        public double Probability(double[] input, out int label)
        {
            bool rawLabel;

            var probability =  this.Regression.Probability(input, out rawLabel);

            label = rawLabel ? 1 : 0;

            return probability;
        }
    }
}
