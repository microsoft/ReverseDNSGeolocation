namespace ReverseDNSGeolocation.Classification.DatasetParsers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class GroundTruthParser : IDataSetParser
    {
        public IEnumerable<DatasetItem> Parse(string inPath, bool populateTextualLocationInfo)
        {
            var count = 0;

            string line;

            using (var file = new StreamReader(inPath))
            {
                while ((line = file.ReadLine()) != null)
                {
                    count++;

                    /*
                    if (count > 10000)
                    {
                        break;
                    }
                    */

                    if (string.IsNullOrWhiteSpace(line)
                        || (line.Length > 0 && line[0] == '#'))
                    {
                        continue;
                    }

                    var parts = new List<string>(line.Split(new char[] { '\t' }));

                    if (parts.Count != 13)
                    {
                        continue;
                    }

                    /*
                    0  RawIP   string
                    1  NumericIP   uint
                    2  Bucket  uint?
                    3  ReverseDNSHostname  string
                    4  RealtimeLatitude    double?
                    5  RealtimeLongitude   double?
                    6  RealtimeCountryISO  string
                    7  RealtimeCountryConfidence   byte
                    8  RealtimeState   string
                    9  RealtimeStateConfidence byte
                    10 RealtimeCity    string
                    11 RealtimeCityConfidence  byte
                    12 RealtimeAccuracyKm  double?
                    */

                    var hostname = parts[3];

                    var trueLatitudeStr = parts[4];
                    var trueLongitudeStr = parts[5];

                    if (string.IsNullOrWhiteSpace(hostname)
                        || string.IsNullOrWhiteSpace(trueLatitudeStr)
                        || string.IsNullOrWhiteSpace(trueLongitudeStr))
                    {
                        continue;
                    }

                    double trueLatitude;
                    double trueLongitude;

                    if (!double.TryParse(trueLatitudeStr, out trueLatitude)
                        || !double.TryParse(trueLongitudeStr, out trueLongitude))
                    {
                        continue;
                    }

                    var realtimeAccuracyKmStr = parts[12];

                    if (string.IsNullOrWhiteSpace(realtimeAccuracyKmStr))
                    {
                        continue;
                    }

                    double realtimeAccuracyKm;

                    if (!double.TryParse(realtimeAccuracyKmStr, out realtimeAccuracyKm))
                    {
                        continue;
                    }

                    var item = new DatasetItem()
                    {
                        RawIP = parts[0],
                        Hostname = hostname,
                        Latitude = trueLatitude,
                        Longitude = trueLongitude,
                        RealtimeAccuracyKm = realtimeAccuracyKm
                    };

                    if (populateTextualLocationInfo)
                    {
                        var city = parts[10];

                        if (!string.IsNullOrWhiteSpace(city))
                        {
                            item.City = city.Trim();
                        }

                        var state = parts[8];

                        if (!string.IsNullOrWhiteSpace(state))
                        {
                            item.State = state.Trim();
                        }

                        var country = parts[6];

                        if (!string.IsNullOrWhiteSpace(country))
                        {
                            item.Country = country.Trim();
                        }
                    }

                    yield return item;
                }
            }
        }
    }
}
