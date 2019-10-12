namespace ReverseDNSGeolocation.Classification
{
    using Features;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SamplerToTrainingDataConverter
    {
        public TrainingData Convert(
            CityFeaturesAggregator aggregator,
            ITrainingDataSampler sampler)
        {
            var trainingData = new TrainingData(tableName: "ReverseDNSGeolocation Training", featuresAggregator: aggregator);

            foreach (var sample in sampler.Sample())
            {
                var newRow = trainingData.CreateTrainingRow(sample.Features, isValidLocation: sample.IsPositiveExample);
                trainingData.AddTrainingRow(newRow);
            }

            trainingData.FinalizeData();

            return trainingData;
        }
    }
}
