namespace ReverseDNSGeolocation.Features
{
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [Serializable]
    public class EntitiesToFeatures : Dictionary<GeonamesCityEntity, Features>
    {
        public EntitiesToFeatures() 
            : base()
        {
        }

        public EntitiesToFeatures(SerializationInfo info, StreamingContext context) 
            : base (info, context)
        {
        }
    }
}
