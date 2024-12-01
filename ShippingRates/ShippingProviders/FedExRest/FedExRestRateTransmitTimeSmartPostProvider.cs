using ShippingRates.Helpers.Extensions;
using System.Collections.Generic;
using ShippingRates.OpenApi.FedEx.RateTransitTimes;
using System.Net.Http;

namespace ShippingRates.ShippingProviders.FedExRest
{
    /// <summary>
    ///     Provides SmartPost rates (only) from FedEx (Federal Express) REST API.
    /// </summary>
    public class FedExRestRateTransmitTimeSmartPostProvider : FedExRestRateTransmitTimesBaseProvider
    {
        public override string Name { get => "FedExSmartPost"; }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="hubId">If specified, the FedEx Rate API will only return SmartPost service type rates. Leave empty to get all service types.</param>
        public FedExRestRateTransmitTimeSmartPostProvider(string clientId, string clientSecret, string accountNumber, string hubId)
            : this(clientId, clientSecret, accountNumber, hubId, true) { }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="hubId">If specified, the FedEx Rate API will only return SmartPost service type rates. Leave empty to get all service types.</param>
        /// <param name="useProduction"></param>
        public FedExRestRateTransmitTimeSmartPostProvider(string clientId, string clientSecret, string accountNumber, string hubId, bool useProduction)
            : this(new FedExRestProviderConfiguration()
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                AccountNumber = accountNumber,
                UseProduction = useProduction,
                HubId = hubId,
            })
        {
        }

        public FedExRestRateTransmitTimeSmartPostProvider(FedExRestProviderConfiguration configuration)
            : base(configuration)
        {
            // SmartPost does not allow insured values
            _allowInsuredValues = false;
        }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="hubId">If specified, the FedEx Rate API will only return SmartPost service type rates. Leave empty to get all service types.</param>
        public FedExRestRateTransmitTimeSmartPostProvider(string clientId, string clientSecret, string accountNumber, string hubId, HttpClient httpClient)
            : this(clientId, clientSecret, accountNumber, hubId, true, httpClient) { }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="hubId">If specified, the FedEx Rate API will only return SmartPost service type rates. Leave empty to get all service types.</param>
        /// <param name="useProduction"></param>
        public FedExRestRateTransmitTimeSmartPostProvider(string clientId, string clientSecret, string accountNumber, string hubId, bool useProduction, HttpClient httpClient)
            : this(new FedExRestProviderConfiguration()
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                AccountNumber = accountNumber,
                UseProduction = useProduction,
                HubId = hubId,
            }, httpClient)
        {
        }

        public FedExRestRateTransmitTimeSmartPostProvider(FedExRestProviderConfiguration configuration, HttpClient httpClient)
            : base(configuration, httpClient)
        {
            // SmartPost does not allow insured values
            _allowInsuredValues = false;
        }

        /// <summary>
        /// Sets the service codes.
        /// </summary>
        protected override Dictionary<string, string> ServiceCodes => new Dictionary<string, string>
        {
            {"SMART_POST", "FedEx Smart Post"}
        };

        /// <summary>
        /// Sets shipment details
        /// </summary>
        /// <param name="request"></param>
        protected sealed override void SetShipmentDetails(Full_Schema_Quote_Rate request)
        {
            SetSmartPostDetails(request);
        }

        /// <summary>
        /// Sets SmartPost details
        /// </summary>
        /// <param name="request"></param>
        private void SetSmartPostDetails(Full_Schema_Quote_Rate request)
        {
            request.RequestedShipment.ServiceType = "SMART_POST";
            request.RequestedShipment.SmartPostInfoDetail = new SmartPostInfoDetail { HubId = _configuration.HubId, Indicia = SmartPostInfoDetailIndicia.PARCEL_SELECT };

            // Handle the various SmartPost Indicia scenarios
            // The ones we should mainly care about are as follows:
            // PRESORTED_STANDARD (less than 1 LB)
            // PARCEL_SELECT (1 LB through 70 LB)

            var weight = request.RequestedShipment.TotalWeight;
            if (weight < 1.0)
                request.RequestedShipment.SmartPostInfoDetail.Indicia = SmartPostInfoDetailIndicia.PRESORTED_STANDARD;
        }
    }
}
