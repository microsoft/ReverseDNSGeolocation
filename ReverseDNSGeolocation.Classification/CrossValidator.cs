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
    using Accord.MachineLearning;
    using Accord.MachineLearning.DecisionTrees;
    using Accord.MachineLearning.DecisionTrees.Learning;
    using Accord.Math;
    using Accord.Math.Optimization.Losses;
    using Accord.Statistics.Analysis;
    using Accord.Statistics.Filters;
    using ReverseDNSGeolocation.Features;

    public class CrossValidator
    {
        public CrossValidationResult<object> Validate(IClassifier classifier, TrainingData trainingData, int folds = 10)
        {
            var crossValidation = new CrossValidation(size: trainingData.Inputs.Length, folds: folds);

            crossValidation.Fitting = delegate (int k, int[] indicesTrain, int[] indicesValidation)
            {
                var trainingInputs = trainingData.Inputs.Get(indicesTrain);
                var trainingOutputs = trainingData.Outputs.Get(indicesTrain);

                var validationInputs = trainingData.Inputs.Get(indicesValidation);
                var validationOutputs = trainingData.Outputs.Get(indicesValidation);

                var foldClassifier = classifier.CreateInstance(trainingData.FeatureDefaultsValueTypes, trainingData.FeatureGranularities);
                foldClassifier.Train(trainingInputs, trainingOutputs);

                var trainingPredicted = foldClassifier.Decide(trainingInputs);
                var validationPredicted = foldClassifier.Decide(validationInputs);

                double trainingError = new ZeroOneLoss(trainingOutputs).Loss(trainingPredicted);
                double validationError = new ZeroOneLoss(validationOutputs).Loss(validationPredicted);

                var confusionMatrix = new ConfusionMatrix(validationPredicted, validationOutputs, positiveValue: 1, negativeValue: 0);

                Console.WriteLine($"{k}\t{trainingError}\t{validationError}\t{confusionMatrix.Accuracy}\t{confusionMatrix.TruePositives}\t{confusionMatrix.TrueNegatives}\t{confusionMatrix.FalsePositives}\t{confusionMatrix.FalseNegatives}\t{confusionMatrix.FalsePositiveRate}");

                return new CrossValidationValues(foldClassifier, trainingError, validationError);
            };

            var result = crossValidation.Compute();
            return result;
        }
    }
}
