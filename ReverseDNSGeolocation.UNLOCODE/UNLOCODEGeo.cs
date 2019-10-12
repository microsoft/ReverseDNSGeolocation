namespace ReverseDNSGeolocation.UNLOCODE
{
    using System.Globalization;
    using System.IO;
    using CsvHelper;
    using System.Collections.Generic;
    using System;
    using Classification;

    public static class UNLOCODEGeo
    {
        /*
        public static void OutputLowercaseCodes(string unlocodeCsvPath, string justLocationOutputPath, string combinedCodeLocationOutputPath)
        {
            using (var inFile = new StreamReader(unlocodeCsvPath))
            using (var locationOutFile = new StreamWriter(justLocationOutputPath))
            using (var combinedOutFile = new StreamWriter(combinedCodeLocationOutputPath))
            {
                var csv = new CsvReader(inFile);
                csv.Configuration.IgnoreQuotes = false;
                csv.Configuration.HasHeaderRecord = true;
                csv.Configuration.IgnoreHeaderWhiteSpace = true;

                while (csv.Read())
                {
                    var countryCodeLower = csv.GetField<string>("Country").Trim().ToLowerInvariant();
                    var locationCodeLower = csv.GetField<string>("Location").Trim().ToLowerInvariant();

                    if (countryCodeLower.Length > 0 && locationCodeLower.Length > 0)
                    {
                        locationOutFile.WriteLine(locationCodeLower);
                        combinedOutFile.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}{1}", countryCodeLower, locationCodeLower));
                    }
                }
            }
        }
        */

        public static void GeolocateCodes(ClosestCityFinder closestCityFinder, List<string> unlocodeCsvPaths, string outPath)
        {
            var countWithCoordinates = 0;
            var countWithCoordinatesIncorrect = 0;
            var countWithCoordinatesFound = 0;
            var countWithNameOnly = 0;

            foreach (var unlocodeCsvPath in unlocodeCsvPaths)
            {
                using (var inFile = new StreamReader(unlocodeCsvPath))
                {
                    using (var outFile = new StreamWriter(outPath))
                    {
                        var csv = new CsvReader(inFile);
                        csv.Configuration.IgnoreQuotes = false;
                        csv.Configuration.HasHeaderRecord = false;
                        csv.Configuration.IgnoreHeaderWhiteSpace = true;

                        while (csv.Read())
                        {
                            // Columns: Change,Country,Location,Name,NameWoDiacritics,Subdivision,Status,Function,Date,IATA,Coordinates,Remarks
                            // Example: ,AF,BAG,Bagram,Bagram,PAR,RL,--3-----,0307,,3457N 06915E,
                            var entry = csv.GetRecord<UNLOCODEEntry>();

                            if (string.IsNullOrWhiteSpace(entry.Country) || string.IsNullOrWhiteSpace(entry.Location))
                            {
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(entry.Coordinates))
                            {
                                countWithCoordinates++;

                                var latitudeRaw = ParseLatitude(entry.Coordinates);
                                var longitudeRaw = ParseLongitude(entry.Coordinates);

                                if (latitudeRaw != null && longitudeRaw != null)
                                {
                                    var latitude = (double)latitudeRaw;
                                    var longitude = (double)longitudeRaw;

                                    var closestCity = closestCityFinder.FindClosestCityForCoordinates(latitude, longitude);

                                    if (closestCity != null)
                                    {
                                        countWithCoordinatesFound++;

                                        outFile.WriteLine($"{entry.Country}\t{entry.Location}\t{closestCity.Id}");
                                    }
                                }
                                else
                                {
                                    countWithCoordinatesIncorrect++;
                                }
                            }
                            else
                            {
                                countWithNameOnly++;
                            }

                            /*
                            var countryCodeLower = csv.GetField<string>("Country").Trim().ToLowerInvariant();
                            var locationCodeLower = csv.GetField<string>("Location").Trim().ToLowerInvariant();

                            if (countryCodeLower.Length > 0 && locationCodeLower.Length > 0)
                            {
                                locationOutFile.WriteLine(locationCodeLower);
                                combinedOutFile.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}{1}", countryCodeLower, locationCodeLower));
                            }
                            */
                        }
                    }
                }
            }

            Console.WriteLine("Done");
        }

        // https://github.com/argonavis80/CSharp-Unloc-Loader/blob/master/Sources/UnlocLoader/Core/PositionParser.cs
        // MIT License: https://github.com/argonavis80/CSharp-Unloc-Loader/blob/master/LICENSE
        public static double? ParseLatitude(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var latString = token.Split(' ')[0];

            if (latString.Length != 5)
            {
                return null;
            }

            var latDeg = latString.Substring(0, 2);
            var latMin = latString.Substring(2, 2);
            var latHs = latString.Substring(4, 1);

            var deg = double.Parse(latDeg);
            var min = double.Parse(latMin);

            var result = deg + min / 60;

            result = latHs == "N" ? result : -result;

            return result;
        }

        // https://github.com/argonavis80/CSharp-Unloc-Loader/blob/master/Sources/UnlocLoader/Core/PositionParser.cs
        // MIT License: https://github.com/argonavis80/CSharp-Unloc-Loader/blob/master/LICENSE
        public static double? ParseLongitude(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var lngString = token.Split(' ')[1];

            if (lngString.Length != 6)
            {
                return null;
            }

            var lngDeg = lngString.Substring(0, 3);
            var lngMin = lngString.Substring(3, 2);
            var lngHs = lngString.Substring(5, 1);

            var deg = double.Parse(lngDeg);
            var min = double.Parse(lngMin);

            var result = deg + min / 60;

            result = lngHs == "E" ? result : -result;

            return result;
        }
    }
}
