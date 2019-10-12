namespace ReverseDNSGeolocation.Features
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [Serializable]
    public class FeatureGranularities : Dictionary<CityFeatureType, FeatureGranularity>
    {
        public FeatureGranularities() : base()
        {
        }

        protected FeatureGranularities(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
