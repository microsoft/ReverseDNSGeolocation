namespace ReverseDNSGeolocation.Features
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [Serializable]
    public class Features : Dictionary<CityFeatureType, object>
    {
        public Features() : base()
        {
        }

        protected Features(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
