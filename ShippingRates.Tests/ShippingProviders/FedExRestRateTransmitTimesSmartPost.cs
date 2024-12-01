using NUnit.Framework;

using ShippingRates.ShippingProviders;
using ShippingRates.ShippingProviders.FedExRest;

namespace ShippingRates.Tests.ShippingProviders
{
    public abstract class FedExRestSmartPostShipRatesTestsBase
    {
        protected readonly RateManager _rateManager;
        protected readonly FedExRestRateTransmitTimeSmartPostProvider _provider;

        protected FedExRestSmartPostShipRatesTestsBase()
        {
            var config = ConfigHelper.GetApplicationConfiguration(TestContext.CurrentContext.TestDirectory);

            _provider = new FedExRestRateTransmitTimeSmartPostProvider(new FedExRestProviderConfiguration()
            {
                ClientId = config.FedExRestClientId,
                ClientSecret = config.FedExRestSecret,
                AccountNumber = config.FedExRestAccountNumber,
                HubId = config.FedExRestHubId,
                UseProduction = config.FedExRestUseProduction
            });

            _rateManager = new RateManager();
            _rateManager.AddProvider(_provider);
        }
    }

    [TestFixture]
    public class FedExRestRateTransmitTimesSmartPost : FedExRestSmartPostShipRatesTestsBase
    {
        [Test]
        public void CanGetFedExServiceCodes()
        {
            var serviceCodes = _provider.GetServiceCodes();

            Assert.That(serviceCodes, Is.Not.Null);
            Assert.That(serviceCodes, Is.Not.Empty);
        }
    }
}
