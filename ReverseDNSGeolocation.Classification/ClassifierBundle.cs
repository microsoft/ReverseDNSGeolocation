namespace ReverseDNSGeolocation.Classification
{
    using Features;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ClassifierBundle
    {
        public CityFeaturesAggregator Aggregator { get; set; }

        public TrainingData TrainingData { get; set; }

        public IClassifier Classifier { get; set; }
    }
}
