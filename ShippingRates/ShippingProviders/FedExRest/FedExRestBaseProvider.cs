using ShippingRates.Helpers.Extensions;
using ShippingRates.OpenApi.FedEx.RateTransitTimes;
using ShippingRates.Services.FedEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShippingRates.ShippingProviders.FedExRest
{
    public abstract class FedExRestBaseProvider : AbstractShippingProvider
    {
        protected abstract Dictionary<string, string> ServiceCodes { get; }

        protected readonly FedExRestProviderConfiguration _configuration;

        public FedExRestBaseProvider(FedExRestProviderConfiguration configuration)
        {
            HttpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public FedExRestBaseProvider(FedExRestProviderConfiguration configuration, HttpClient httpClient)
            : this(configuration)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        ///     FedEx allows insured values for items being shipped except when utilizing SmartPost.
        ///     This setting will this value to be overwritten.
        /// </summary>
        protected bool _allowInsuredValues = true;

        /// <summary>
        /// Gets service codes.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> GetServiceCodes() => (ServiceCodes?.Count ?? 0) > 0 ? ServiceCodes : null;

        public string GetRequestUri(bool isProduction)
            => $"https://{(isProduction ? "apis" : "apis-sandbox")}.fedex.com";

    }
}
