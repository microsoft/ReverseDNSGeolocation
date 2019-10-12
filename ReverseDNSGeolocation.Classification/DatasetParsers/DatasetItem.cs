namespace ReverseDNSGeolocation.Classification.DatasetParsers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class DatasetItem
    {
        public string RawIP { get; set; }

        public string Hostname { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double RealtimeAccuracyKm { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Country { get; set; }
    }
}
