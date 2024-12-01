using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using ShippingRates.ShippingProviders;
using ShippingRates.ShippingProviders.FedExRest;

namespace ShippingRates.SampleApp
{
    class Program
    {
        static async Task Main()
        {
            var appSettings = ConfigurationManager.AppSettings;

            // You will need OAuth Client Id, Client Secret, and Account Number to use the UPS provider
            // More details: https://developer.ups.com/oauth-developer-guide?loc=en_US
            var upsClientId = appSettings["UPSClientId"];
            var upsClientSecret = appSettings["UPSClientSecret"];
            var upsAccountNumber = appSettings["UPSAccountNumber"];

            // You will need an account # and meter # to utilize the FedEx provider.
            var fedexKey = appSettings["FedExKey"];
            var fedexPassword = appSettings["FedExPassword"];
            var fedexAccountNumber = appSettings["FedExAccountNumber"];
            var fedexMeterNumber = appSettings["FedExMeterNumber"];
            var fedexHubId = appSettings["FedExHubId"]; // 5531 is the hubId to use in FedEx's test environment
            var fedexUseProduction = Convert.ToBoolean(appSettings["FedExUseProduction"]);

            // You will need an account #, client Id, client secret to utilize the FedEx REST API provider.
            var fedexRestClientId = appSettings["FedExRestClientId"];
            var fedexRestSecret = appSettings["FedExRestSecret"];
            var fedexRestAccountNumber = appSettings["FedExRestAccountNumber"];
            var fedexRestHubId = appSettings["FedExRestHubId"]; // 5531 is the hubId to use in FedEx's test environment
            var fedexRestUseProduction = Convert.ToBoolean(appSettings["FedExRestUseProduction"]);

            // You will need a userId to use the USPS provider. Your account will also need access to the production servers.
            var uspsUserId = appSettings["USPSUserId"];

            // You will need a Site ID and Password to use DHL provider.
            var dhlSiteId = appSettings["DHLSiteId"];
            var dhlPassword = appSettings["DHLPassword"];

            // Setup package and destination/origin addresses
            var packages = new List<Package>
            {
                new Package(12, 12, 12, 35, 150),
                new Package(4, 4, 6, 15, 250)
            };

            var origin = new Address("", "CT", "06405", "US");
            var destination = new Address("", "", "20852", "US"); // US Address
            //var origin = new Address("Amsterdam", "", "1043 AG", "NL"); // Netherlands Address
            //var destination  = new Address("London", "", "SW1A 2AA", "GB"); // Great Britain Address
            //var destination = new Address("", "", "88888", "US"); // Wrong US Address
            //var destination = new Address("Domont", "", "95330", "FR"); // France Address
            //var destination = new Address("", "", "00907", "PR"); // Puerto Rico Address
            //var destination = new Address("", "", "L4W 1S2", "CA"); // Canada Address
            //var destination = new Address("", "", "SW1E 5JL", "GB"); // UK Address
            //var destination = new Address("", "", "1042 AG", "NL");   // Netherlands Address

            // Create RateManager
            var rateManager = new RateManager();

            using (var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }))
            {
                // Add desired providers
                // UPS
                var upsConfiguration = new UPSProviderConfiguration()
                {
                    ClientId = upsClientId,
                    ClientSecret = upsClientSecret,
                    AccountNumber = upsAccountNumber,
                    UseProduction = false
                };
                rateManager.AddProvider(new UPSProvider(upsConfiguration, httpClient));

                // FedEx
                rateManager.AddProvider(new FedExProvider(fedexKey, fedexPassword, fedexAccountNumber, fedexMeterNumber, fedexUseProduction));
                rateManager.AddProvider(new FedExSmartPostProvider(fedexKey, fedexPassword, fedexAccountNumber, fedexMeterNumber, fedexHubId, fedexUseProduction));

                // FedEx REST API
                rateManager.AddProvider(new FedExRestRateTransmitTimeProvider(fedexRestClientId, fedexRestSecret, fedexRestAccountNumber,
                    fedexRestUseProduction, httpClient));
                rateManager.AddProvider(new FedExRestRateTransmitTimeSmartPostProvider(fedexRestClientId, fedexRestSecret, fedexAccountNumber, fedexHubId, fedexUseProduction, httpClient));
                rateManager.AddProvider(new FedExRestFreightLtlProvider(fedexRestClientId, fedexRestSecret, fedexAccountNumber, fedexUseProduction, httpClient));

                // USPS Domestic
                rateManager.AddProvider(new USPSProvider(new USPSProviderConfiguration(uspsUserId), httpClient));
                // USPS International
                rateManager.AddProvider(new USPSInternationalProvider(new USPSProviderConfiguration(uspsUserId), httpClient));

                // DHL
                var dhlConfiguration = new DHLProviderConfiguration(dhlSiteId, dhlPassword, useProduction: false)
                    .ExcludeServices(new char[] { 'C' });
                rateManager.AddProvider(new DHLProvider(dhlConfiguration, httpClient));

                // Call GetRates()
                var shipment = await rateManager.GetRatesAsync(origin, destination, packages,
                    new ShipmentOptions()
                    {
                        SaturdayDelivery = true
                    });

                // Iterate through the rates returned
                foreach (var rate in shipment.Rates)
                {
                    Console.WriteLine(rate);
                }

                // Iterate through the errors returned
                if (shipment.Errors.Count > 0)
                {
                    Console.WriteLine("Errors:");
                    foreach (var error in shipment.Errors)
                    {
                        Console.WriteLine(error.Number);
                        Console.WriteLine(error.Source);
                        Console.WriteLine(error.Description);
                    }
                }

                // Iterate through the internal errors
                if (shipment.InternalErrors.Count > 0)
                {
                    Console.WriteLine("Internal Errors:");
                    foreach (var error in shipment.InternalErrors)
                    {
                        Console.WriteLine(error);
                    }
                }
            }

            Console.WriteLine("Done!");
        }
    }
}
