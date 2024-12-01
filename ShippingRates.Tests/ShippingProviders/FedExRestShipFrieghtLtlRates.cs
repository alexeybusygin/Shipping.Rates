using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

using ShippingRates.ShippingProviders;
using ShippingRates.ShippingProviders.FedExRest;
using System.Net.Http;
using System.Net;

namespace ShippingRates.Tests.ShippingProviders
{
    public abstract class FedExRestShipFrieghtLtlRatesTestBase
    {
        protected readonly RateManager _rateManager;
        protected readonly RateManager _rateManagerNegotiated;
        protected readonly FedExRestFreightLtlProvider _provider;
        protected readonly FedExRestFreightLtlProvider _providerNegotiated;

        protected FedExRestShipFrieghtLtlRatesTestBase(HttpClient httpClient)
        {
            var config = ConfigHelper.GetApplicationConfiguration(TestContext.CurrentContext.TestDirectory);

            _provider = new FedExRestFreightLtlProvider(new FedExRestProviderConfiguration()
            {
                ClientId = config.FedExRestClientId,
                ClientSecret = config.FedExRestSecret,
                AccountNumber = config.FedExRestAccountNumber,
                UseProduction = config.FedExRestUseProduction
            }, httpClient);

            _rateManager = new RateManager();
            _rateManager.AddProvider(_provider);

            _providerNegotiated = new FedExRestFreightLtlProvider(new FedExRestProviderConfiguration()
            {
                ClientId = config.FedExRestClientId,
                ClientSecret = config.FedExRestSecret,
                AccountNumber = config.FedExRestAccountNumber,
                UseProduction = config.FedExRestUseProduction,
                UseNegotiatedRates = true
            }, httpClient);

            _rateManagerNegotiated = new RateManager();
            _rateManagerNegotiated.AddProvider(_providerNegotiated);
        }
    }

    [TestFixture]
    public class FedExRestShipFrieghtLtlRatesTest : FedExRestShipFrieghtLtlRatesTestBase
    {
        public FedExRestShipFrieghtLtlRatesTest() : base(new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })) {
        }

        //Require proper freight account setup in FedEx account
        //[Test]
        //public void FedExFreight()
        //{
        //    var from = new Address("Annapolis", "MD", "21401", "US");
        //    var to = new Address("Fitchburg", "WI", "53711", "US");
        //    var package = new Package(48, 48, 48, 120, 100, "CONTAINER");

        //    var rates = _rateManager.GetRates(from, to, package);

        //    Assert.Multiple(() =>
        //    {
        //        Assert.That(rates.Rates, Is.Not.Empty);
        //        Assert.That(rates.Rates.All(r => r.Name.Contains("Freight")), Is.True);
        //    });
        //}


        [Test]
        public void CanGetFedExServiceCodes()
        {
            var serviceCodes = _provider.GetServiceCodes();

            Assert.That(serviceCodes, Is.Not.Null);
            Assert.That(serviceCodes, Is.Not.Empty);
        }
    }
}
