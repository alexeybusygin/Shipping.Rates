using ShippingRates.OpenApi.FedEx.RateTransitTimes;
using System.Collections.Generic;
using System.Net.Http;

namespace ShippingRates.ShippingProviders.FedExRest
{
    /// <summary>
    ///     Provides rates from FedEx (Federal Express) REST API excluding SmartPost. Please use <see cref="FedExRestSmartPostProvider"/> for SmartPost rates.
    /// </summary>
    public class FedExRestRateTransmitTimeProvider : FedExRestRateTransmitTimesBaseProvider
    {
        public override string Name { get => "FedExRest"; }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        public FedExRestRateTransmitTimeProvider(string clientId, string clientSecret, string accountNumber)
            : this(clientId, clientSecret, accountNumber, true) { }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="useProduction"></param>
        public FedExRestRateTransmitTimeProvider(string clientId, string clientSecret, string accountNumber, bool useProduction)
            : this(new FedExRestProviderConfiguration()
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                AccountNumber = accountNumber,
                UseProduction = useProduction,
            })
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="configuration"></param>
        public FedExRestRateTransmitTimeProvider(FedExRestProviderConfiguration configuration)
            : base(configuration) { }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="httpClient"></param>
        public FedExRestRateTransmitTimeProvider(string clientId, string clientSecret, string accountNumber, HttpClient httpClient)
            : this(clientId, clientSecret, accountNumber, true, httpClient) { }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="useProduction"></param>
        /// <param name="httpClient"></param>
        public FedExRestRateTransmitTimeProvider(string clientId, string clientSecret, string accountNumber, bool useProduction, HttpClient httpClient)
            : this(new FedExRestProviderConfiguration()
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                AccountNumber = accountNumber,
                UseProduction = useProduction,
            }, httpClient)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="httpClient"></param>
        public FedExRestRateTransmitTimeProvider(FedExRestProviderConfiguration configuration, HttpClient httpClient)
            : base(configuration, httpClient) { }

        /// <summary>
        /// Sets service codes.
        /// </summary>
        protected override Dictionary<string, string> ServiceCodes => new Dictionary<string, string>
        {
            {"PRIORITY_OVERNIGHT", "FedEx Priority Overnight"},
            {"FEDEX_2_DAY", "FedEx 2nd Day"},
            {"FEDEX_2_DAY_AM", "FedEx 2nd Day A.M."},
            {"STANDARD_OVERNIGHT", "FedEx Standard Overnight"},
            {"FIRST_OVERNIGHT", "FedEx First Overnight"},
            {"FEDEX_EXPRESS_SAVER", "FedEx Express Saver"},
            {"FEDEX_GROUND", "FedEx Ground"},
            {"GROUND_HOME_DELIVERY", "FedEx Ground Residential"},
            {"INTERNATIONAL_GROUND", "FedEx International Ground"},
            {"INTERNATIONAL_FIRST", "FedEx International First"},
            {"INTERNATIONAL_ECONOMY", "FedEx International Economy"},
            {"INTERNATIONAL_PRIORITY", "FedEx International Priority"},
            {"EUROPE_FIRST_INTERNATIONAL_PRIORITY", "FedEx International Priority" },
            {"FEDEX_INTERNATIONAL_PRIORITY", "FedEx International Priority" },
            {"FEDEX_INTERNATIONAL_PRIORITY_EXPRESS", "FedEx International Priority Express" },
            // Freight
            {"FEDEX_FIRST_FREIGHT", "FedEx First Freight" },
            {"FEDEX_1_DAY_FREIGHT", "FedEx 1 Day Freight" },
            {"FEDEX_2_DAY_FREIGHT", "FedEx 2 Day Freight" },
            {"FEDEX_3_DAY_FREIGHT", "FedEx 3 Day Freight" },
            {"FEDEX_FREIGHT_PRIORITY", "FedEx Freight Priority" },
            {"FEDEX_FREIGHT_ECONOMY", "FedEx Freight Economy" },
            {"INTERNATIONAL_PRIORITY_FREIGHT", "FedEx International Priority Freight" },
            {"INTERNATIONAL_ECONOMY_FREIGHT", "FedEx International Economy Freight" },
        };

        /// <summary>
        /// Sets shipment details
        /// </summary>
        /// <param name="request"></param>
        protected sealed override void SetShipmentDetails(Full_Schema_Quote_Rate request)
        {
        }
    }
}
