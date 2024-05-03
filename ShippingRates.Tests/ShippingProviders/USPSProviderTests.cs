using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using ShippingRates.ShippingProviders;
using ShippingRates.ShippingProviders.USPS;

namespace ShippingRates.Tests.ShippingProviders
{
    [TestFixture]
    public class USPSProviderTests
    {
        private readonly Address DomesticAddress1;
        private readonly Address DomesticAddress2;
        private readonly Address InternationalAddress1;
        private readonly Package Package1;
        private readonly Package Package1SignatureRequired;
        private readonly Package Package1WithInsurance;
        private readonly string _uspsUserId;

        public USPSProviderTests()
        {
            DomesticAddress1 = new Address("278 Buckley Jones Road", "", "", "Cleveland", "MS", "38732", "US");
            DomesticAddress2 = new Address("One Microsoft Way", "", "", "Redmond", "WA", "98052", "US");
            InternationalAddress1 = new Address("Jubail", "Jubail", "31951", "Saudi Arabia");

            Package1 = new Package(4, 4, 4, 5, 0);
            Package1SignatureRequired = new Package(4, 4, 4, 5, 0, null, true);
            Package1WithInsurance = new Package(4, 4, 4, 5, 50);

            _uspsUserId = ConfigHelper.GetApplicationConfiguration(TestContext.CurrentContext.TestDirectory)
                .USPSUserId;
        }

        [Test]
        public void USPS_Domestic_Returns_Multiple_Rates_When_Using_Valid_Addresses_For_All_Services()
        {
            var rateManager = new RateManager();
            rateManager.AddProvider(new USPSProvider(_uspsUserId));

            var response = rateManager.GetRates(DomesticAddress1, DomesticAddress2, Package1);

            Debug.WriteLine(string.Format("Rates returned: {0}", response.Rates.Any() ? response.Rates.Count.ToString() : "0"));

            Assert.NotNull(response);
            Assert.IsNotEmpty(response.Rates);
            Assert.IsEmpty(response.Errors);
            Assert.IsEmpty(response.InternalErrors);

            foreach (var rate in response.Rates)
            {
                Assert.NotNull(rate);
                Assert.True(rate.TotalCharges > 0);

                Debug.WriteLine(rate.Name + ": " + rate.TotalCharges);
            }
        }

        [Test]
        public void USPS_Domestic_Returns_Multiple_Rates_When_Using_Valid_Addresses_For_All_Services_And_Multiple_Packages()
        {
            var rateManager = new RateManager();
            rateManager.AddProvider(new USPSProvider(_uspsUserId));

            var response = rateManager.GetRates(DomesticAddress1, DomesticAddress2, Package1);

            Debug.WriteLine(string.Format("Rates returned: {0}", response.Rates.Any() ? response.Rates.Count.ToString() : "0"));

            Assert.NotNull(response);
            Assert.IsNotEmpty(response.Rates);
            Assert.IsEmpty(response.Errors);

            foreach (var rate in response.Rates)
            {
                Assert.NotNull(rate);
                Assert.True(rate.TotalCharges > 0);

                Debug.WriteLine(rate.Name + ": " + rate.TotalCharges);
            }
        }

        [Test]
        public void USPS_Domestic_Returns_No_Rates_When_Using_Invalid_Addresses_For_All_Services()
        {
            var rateManager = new RateManager();
            rateManager.AddProvider(new USPSProvider(_uspsUserId));

            var response = rateManager.GetRates(DomesticAddress1, InternationalAddress1, Package1);

            Debug.WriteLine(string.Format("Rates returned: {0}", response.Rates.Any() ? response.Rates.Count.ToString() : "0"));

            Assert.NotNull(response);
            Assert.IsEmpty(response.Rates);
            Assert.IsEmpty(response.Errors);
        }

        [Test]
        public void USPS_Domestic_Returns_No_Rates_When_Using_Invalid_Addresses_For_Single_Service()
        {
            var rateManager = new RateManager();
            rateManager.AddProvider(new USPSProvider(_uspsUserId, ShippingRates.ShippingProviders.USPS.Services.Priority));

            var response = rateManager.GetRates(DomesticAddress1, InternationalAddress1, Package1);

            Debug.WriteLine(string.Format("Rates returned: {0}", response.Rates.Any() ? response.Rates.Count.ToString() : "0"));

            Assert.NotNull(response);
            Assert.IsEmpty(response.Rates);
            Assert.IsEmpty(response.Errors);
        }

        [Test]
        public void USPS_Domestic_Returns_Single_Rate_When_Using_Valid_Addresses_For_Single_Service()
        {
            var rateManager = new RateManager();
            rateManager.AddProvider(new USPSProvider(_uspsUserId, ShippingRates.ShippingProviders.USPS.Services.Priority));

            var response = rateManager.GetRates(DomesticAddress1, DomesticAddress2, Package1);

            Debug.WriteLine(string.Format("Rates returned: {0}", response.Rates.Any() ? response.Rates.Count.ToString() : "0"));

            Assert.NotNull(response);
            Assert.IsNotEmpty(response.Rates);
            Assert.IsEmpty(response.Errors);
            Assert.AreEqual(response.Rates.Count, 1);
            Assert.True(response.Rates.First().TotalCharges > 0);

            Debug.WriteLine(response.Rates.First().Name + ": " + response.Rates.First().TotalCharges);
        }

        [Test]
        public void CanGetUspsServiceCodes()
        {
            var provider = new USPSProvider(_uspsUserId);
            var serviceCodes = provider.GetServiceCodes();

            Assert.NotNull(serviceCodes);
            Assert.IsNotEmpty(serviceCodes);
        }

        [Test]
        public void Can_Get_Different_Rates_For_Signature_Required_Lookup()
        {
            var rateManager = new RateManager();
            rateManager.AddProvider(new USPSProvider(_uspsUserId, ShippingRates.ShippingProviders.USPS.Services.Priority));

            var nonSignatureResponse = rateManager.GetRates(DomesticAddress1, DomesticAddress2, Package1);
            var signatureResponse = rateManager.GetRates(DomesticAddress1, DomesticAddress2, Package1SignatureRequired);

            // Assert that we have a non-signature response
            AssertIsValidNonEmptyResponse(nonSignatureResponse);

            // Assert that we have a signature response
            AssertIsValidNonEmptyResponse(signatureResponse);

            // Now compare prices
            AssertRatesAreDifferent(signatureResponse.Rates, nonSignatureResponse.Rates);
        }

        [Test]
        public void Can_Get_Different_Rates_For_Insurance_Lookup()
        {
            var rateManager = new RateManager();
            rateManager.AddProvider(new USPSProvider(_uspsUserId, ShippingRates.ShippingProviders.USPS.Services.Library));

            var nonInsuranceResponse = rateManager.GetRates(DomesticAddress1, DomesticAddress2, Package1);
            var insuranceResponse = rateManager.GetRates(DomesticAddress1, DomesticAddress2, Package1WithInsurance);

            AssertIsValidNonEmptyResponse(nonInsuranceResponse);
            AssertIsValidNonEmptyResponse(insuranceResponse);

            AssertRatesAreDifferent(insuranceResponse.Rates, nonInsuranceResponse.Rates);
        }

        [Test]
        public void Can_Get_Different_Rates_For_Special_Services_Lookup()
        {
            var rateManager1 = new RateManager();
            rateManager1.AddProvider(new USPSProvider(_uspsUserId, ShippingRates.ShippingProviders.USPS.Services.Library));

            var rateManager2 = new RateManager();
            rateManager2.AddProvider(new USPSProvider(new USPSProviderConfiguration(_uspsUserId)
            {
                Service = ShippingRates.ShippingProviders.USPS.Services.Library,
                SpecialServices = new SpecialServices[] { SpecialServices.ScanRetention }
            }));

            var noSpecialServicesResponse = rateManager1.GetRates(DomesticAddress1, DomesticAddress2, Package1);
            var specialServicesResponse = rateManager2.GetRates(DomesticAddress1, DomesticAddress2, Package1);

            AssertIsValidNonEmptyResponse(noSpecialServicesResponse);
            AssertIsValidNonEmptyResponse(specialServicesResponse);

            AssertRatesAreDifferent(specialServicesResponse.Rates, noSpecialServicesResponse.Rates);
        }

        [Test]
        public void USPSDiscountedRates()
        {
            var rateManager1 = new RateManager();
            rateManager1.AddProvider(new USPSProvider(_uspsUserId, ShippingRates.ShippingProviders.USPS.Services.All));

            var rateManager2 = new RateManager();
            rateManager2.AddProvider(new USPSProvider(_uspsUserId, ShippingRates.ShippingProviders.USPS.Services.Online));

            var rates = rateManager1.GetRates(DomesticAddress1, DomesticAddress2, Package1);
            var discountedRates = rateManager2.GetRates(DomesticAddress1, DomesticAddress2, Package1);

            AssertIsValidNonEmptyResponse(rates);
            AssertIsValidNonEmptyResponse(discountedRates);

            AssertRatesAreDifferent(rates.Rates, discountedRates.Rates);
        }

        private static void AssertIsValidNonEmptyResponse(Shipment shipment)
        {
            Assert.NotNull(shipment);
            Assert.IsNotEmpty(shipment.Rates);
            Assert.IsEmpty(shipment.Errors);
            Assert.True(shipment.Rates.First().TotalCharges > 0);
        }

        private static void AssertRatesAreDifferent(List<Rate> ratesA, List<Rate> ratesB)
        {
            var hasDifference = false;
            foreach (var rateA in ratesA)
            {
                var rateB = ratesB.FirstOrDefault(x => x.Name == rateA.Name);
                if (rateB != null)
                {
                    hasDifference |= (rateA.TotalCharges != rateB.TotalCharges);
                }
                if (hasDifference)
                    break;
            }

            Assert.IsTrue(hasDifference);
        }

        [Test]
        public async Task USPS_Domestic_Saturday_Delivery()
        {
            var rateManager = new RateManager();
            rateManager.AddProvider(new USPSProvider(_uspsUserId));

            var today = DateTime.Now;
            var nextFriday = today.AddDays(12 - (int)today.DayOfWeek).Date + new TimeSpan(10, 0, 0);
            var nextThursday = nextFriday.AddDays(-1);

            var origin = new Address("", "", "06405", "US");
            var destination = new Address("", "", "20852", "US");

            var response = await rateManager.GetRatesAsync(origin, destination, Package1, new ShipmentOptions()
            {
                ShippingDate = nextFriday,
                SaturdayDelivery = true
            });

            Assert.NotNull(response);
            Assert.IsNotEmpty(response.Rates);

            // Sometimes only Priority Mail Express 2-Day works and we have to try it on Thursday
            if (!response.Rates.Any(r => r.Options.SaturdayDelivery))
            {
                response = await rateManager.GetRatesAsync(origin, destination, Package1, new ShipmentOptions()
                {
                    ShippingDate = nextThursday,
                    SaturdayDelivery = true
                });

                Assert.NotNull(response);
                Assert.IsNotEmpty(response.Rates);
            }

            Debug.WriteLine(string.Format("Rates returned: {0}", response.Rates.Any() ? response.Rates.Count.ToString() : "0"));

            Assert.IsEmpty(response.Errors);
            Assert.True(response.Rates.Any(r => r.Options.SaturdayDelivery));

            foreach (var rate in response.Rates)
            {
                Assert.NotNull(rate);
                Assert.True(rate.TotalCharges > 0);

                Debug.WriteLine(rate.Name + ": " + rate.TotalCharges);
            }
        }

        [Test]
        public async Task USPS_ThreeAndAHalfOunceLetter_Qualifies_For_First_Class_Mail_Letter()
        {
            var rateManager = new RateManager();
            rateManager.AddProvider(new USPSProvider(_uspsUserId));

            const decimal firstClassLetterMaxWeight = 0.21875m; // 3.5 ounces
            var firstClassLetter = new DocumentsPackage(firstClassLetterMaxWeight, 0);

            var origin = new Address("", "", "06405", "US");
            var destination = new Address("", "", "20852", "US");

            var response = await rateManager.GetRatesAsync(origin, destination, firstClassLetter);

            Assert.True(response.Rates.Any(r =>
                r.ProviderCode is "First-Class Mail Stamped Letter" or "First-Class Mail Metered Letter"));
        }
    }
}
