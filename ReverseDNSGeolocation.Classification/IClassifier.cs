namespace ReverseDNSGeolocation.Classification
{
    using ReverseDNSGeolocation.Features;

    public interface IClassifier
    {
        int[] Decide(double[][] input);

        int Decide(double[] input);

        double Probability(double[] input, out int label);

        void Train(double[][] trainingInputs, int[] trainingOutputs);

        IClassifier CreateInstance(FeatureValueTypes featureDefaultsValueTypes, FeatureGranularities featureGranularities, string serializedClassifierPath = null);
    }
}
