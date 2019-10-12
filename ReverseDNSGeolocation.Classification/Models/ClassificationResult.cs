namespace ReverseDNSGeolocation.Classification.Models
{
    using ReverseDNSGeolocation.Features;
    using GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ClassificationResult
    {
        public GeonamesCityEntity City { get; set; }

        public double? Score { get; set; }

        public Features AllFeatures { get; set; }

        public Features NonDefaultFeatures { get; set; }
    }
}
