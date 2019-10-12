using ReverseDNSGeolocation.Classification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseDNSGeolocation.UNLOCODE.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var unlocodeCsvPaths = new List<string>()
            {
                @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\2017-2 UNLOCODE CodeListPart1.csv",
                @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\2017-2 UNLOCODE CodeListPart2.csv",
                @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\2017-2 UNLOCODE CodeListPart3.csv"
            };

            var outPath = @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt";

            // Geonames data folder
            var geonamesDatePrefix = "2018-03-06";
            var geonamesDataRootInputFolder = @"C:\Projects\ReverseDNS\Geonames\";

            // Dataset with CLLI codes mapped to geonames IDs
            var clliPath = @"C:\Projects\ReverseDNS\first6-city-geonames.tsv";
            var unlocodePath = @"C:\Projects\ReverseDNS\2017-02 UNLOCODE\unlocode-geonames.txt";

            var closestCityFinder = new ClosestCityFinder(citiesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "cities1000.txt"),
                alternateNamesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "alternateNames.txt"),
                admin1Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin1CodesASCII.txt"),
                admin2Path: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "admin2Codes.txt"),
                countriesPath: Path.Combine(geonamesDataRootInputFolder, geonamesDatePrefix, "countryInfo.txt"),
                clliPath: clliPath,
                unlocodePath: unlocodePath);

            UNLOCODEGeo.GeolocateCodes(closestCityFinder, unlocodeCsvPaths, outPath);
        }
    }
}
