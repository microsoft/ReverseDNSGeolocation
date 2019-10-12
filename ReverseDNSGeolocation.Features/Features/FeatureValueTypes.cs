namespace ReverseDNSGeolocation.Features
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [Serializable]
    public class FeatureValueTypes : Dictionary<CityFeatureType, Type>
    {
        public FeatureValueTypes() : base()
        {
        }

        protected FeatureValueTypes(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
