namespace ReverseDNSGeolocation.Features.CityFeatures.AddOnFeatures
{
    using ReverseDNSGeolocation.GeonamesParsers;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class TLDAddOnFeaturesGenerator : AddOnCityFeaturesGenerator
    {
        public override Features FeatureDefaults { get; set; }

        public override FeatureValueTypes FeatureDefaultsValueTypes { get; set; }

        public override FeatureGranularities FeatureGranularities { get; set; }

        public static Dictionary<string, string> DomainsToCountryTlds = new Dictionary<string, string>()
        {
            { "qwest.net", ".us" },
            { "verizon.net", ".us" },
            { "charter.com", ".us" },
            { "level3.net", ".us" }
        };

        public TLDAddOnFeaturesGenerator(FeaturesConfig featuresConfig) : base(featuresConfig)
        {
            FeatureDefaults = new Features()
            {
                { CityFeatureType.TLDMatch, false },
            };

            FeatureDefaultsValueTypes = new FeatureValueTypes()
            {
                { CityFeatureType.TLDMatch, typeof(bool) },
            };

            FeatureGranularities = new FeatureGranularities()
            {
                { CityFeatureType.TLDMatch, FeatureGranularity.Discrete }
            };
        }

        public override void AppendFeatures(HostnameSplitterResult parsedHostname, GeonamesCityEntity cityEntity, Features features)
        {
            if (parsedHostname == null || string.IsNullOrWhiteSpace(parsedHostname.TLD) || cityEntity == null || cityEntity.CountryEntity == null || string.IsNullOrWhiteSpace(cityEntity.CountryEntity.TLD) || features == null)
            {
                return;
            }

            var countryTld = cityEntity.CountryEntity.TLD.ToLowerInvariant();

            var hostnameTLD = parsedHostname.TLD;
            var hostnameTLDParts = hostnameTLD.Split('.');

            var lastPart = hostnameTLDParts[hostnameTLDParts.Length - 1];

            if (!string.IsNullOrWhiteSpace(lastPart))
            {
                string convertedTld;
                var hostnameTld = string.Format(CultureInfo.InvariantCulture, ".{0}", lastPart.ToLowerInvariant());

                if (countryTld == hostnameTld)
                {
                    features[CityFeatureType.TLDMatch] = true;
                }
                else if (
                    parsedHostname.DomainInfo?.RegistrableDomain != null 
                    && DomainsToCountryTlds.TryGetValue(parsedHostname.DomainInfo.RegistrableDomain, out convertedTld)
                    && countryTld == convertedTld)
                {
                    features[CityFeatureType.TLDMatch] = true;
                }
            }
        }
    }
}
