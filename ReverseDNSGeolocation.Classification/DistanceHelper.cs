using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseDNSGeolocation.Classification
{
    public static class DistanceHelper
    {
        public const double NauticalMilesPerDegree = 60;

        public const double NauticalMileToMiles = 1.1515; // 1.1508 for international Nautical Mile.

        public const double MileToKilometer = 1.609344;

        public const double MileToNauticalMiles = 0.8684;

        /// <summary>
        /// Great Circle Geocode distance according to:
        /// http://sgowtham.net/ramblings/2009/08/04/php-calculating-distance-between-two-locations-given-their-gps-coordinates/
        /// </summary>
        /// <param name="lat1">The lat1.</param>
        /// <param name="lon1">The lon1.</param>
        /// <param name="lat2">The lat2.</param>
        /// <param name="lon2">The lon2.</param>
        /// <param name="unit">The unit. Mile is the default value</param>
        /// <returns>The greater circle distance</returns>
        public static double Distance(
            double lat1, double lon1, double lat2, double lon2, DistanceUnit unit = DistanceUnit.Mile)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(DegreesToRadians(lat1)) * Math.Sin(DegreesToRadians(lat2))
                          + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                          * Math.Cos(DegreesToRadians(theta));

            if (dist > 1)
            {
                dist = 1;
            }

            dist = Math.Acos(dist);
            dist = RadiansToDegrees(dist);
            dist = dist * NauticalMilesPerDegree * NauticalMileToMiles;

            switch (unit)
            {
                case DistanceUnit.Kilometer:
                    dist = dist * MileToKilometer;
                    break;

                case DistanceUnit.NauticalMile:
                    dist = dist * MileToNauticalMiles;
                    break;

                case DistanceUnit.Mile:
                default:
                    // already in miles
                    break;
            }

            return dist;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians / Math.PI * 180.0;
        }

        public static double ClosestToPower(double number, int power)
        {
            return Math.Pow(power, Math.Ceiling(Math.Log(number) / Math.Log(power)));
        }

        public static string GetDistanceBucketKM(int bucketSize, double distanceKilometers, int gteThreshold = 100)
        {
            var paddingWidth = gteThreshold.ToString().Length;

            if (distanceKilometers < 0)
            {
                return "Distance-Invalid";
            }
            else if (distanceKilometers >= gteThreshold)
            {
                return string.Format(CultureInfo.InvariantCulture, "Distance-GTE-{0}-Km", gteThreshold.ToString().PadLeft(paddingWidth, '0'));
            }
            else
            {
                int lowerBucket = ((int)Math.Floor((1.0 * distanceKilometers) / (1.0 * bucketSize))) * bucketSize;
                int upperBucket = lowerBucket + bucketSize;

                return string.Format(CultureInfo.InvariantCulture, "Distance-GTE-{0}-LT-{1}-Km", lowerBucket.ToString().PadLeft(paddingWidth, '0'), upperBucket.ToString().PadLeft(paddingWidth, '0'));
            }
        }
    }
}
