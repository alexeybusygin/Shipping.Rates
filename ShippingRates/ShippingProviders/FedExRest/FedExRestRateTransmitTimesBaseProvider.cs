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
    public abstract class FedExRestRateTransmitTimesBaseProvider : FedExRestBaseProvider
    {
        public FedExRestRateTransmitTimesBaseProvider(FedExRestProviderConfiguration configuration) : base(configuration)
        {
        }

        public FedExRestRateTransmitTimesBaseProvider(FedExRestProviderConfiguration configuration, HttpClient httpClient)
            : base(configuration, httpClient)
        {
        }

        /// <summary>
        /// Creates the rate request
        /// </summary>
        /// <returns></returns>
        protected Full_Schema_Quote_Rate CreateRateRequest()
        {
            // Build the RateRequest
            var request = new Full_Schema_Quote_Rate
            {
                AccountNumber = new AccountNumber() { Value = _configuration.AccountNumber },
                RequestedShipment = new RequestedShipment
                {
                    Shipper = new RateParty
                    {
                        Address = new ShippingRates.OpenApi.FedEx.RateTransitTimes.Address
                        {
                            PostalCode = Shipment.OriginAddress.PostalCode,
                            CountryCode = Shipment.OriginAddress.CountryCode
                        }
                    },
                    Recipient = new RateParty
                    {
                        Address = new ShippingRates.OpenApi.FedEx.RateTransitTimes.Address
                        {
                            PostalCode = Shipment.DestinationAddress.PostalCode,
                            CountryCode = Shipment.DestinationAddress.CountryCode
                        }
                    },
                    RequestedPackageLineItems = new RequestedPackageLineItem[]
                    {
                    },
                    PickupType = RequestedShipmentPickupType.USE_SCHEDULED_PICKUP,
                    RateRequestType = GetRateRequestTypes().ToArray(),
                    PreferredCurrency = Shipment.Options.GetCurrencyCode(),
                    PackagingType = "YOUR_PACKAGING",
                    TotalPackageCount = Shipment.Packages.Count,
                },
            };

            if(Shipment.Options.ShippingDate != null)
            {
                request.RequestedShipment.ShipDateStamp = Shipment.Options.ShippingDate.Value.ToString("yyyy-MM-dd");
            }

            if (Shipment.Options.FedExOneRate)
            {
                if (!string.IsNullOrEmpty(Shipment.Options.FedExOneRatePackageOverride))
                {
                    request.RequestedShipment.PackagingType = Shipment.Options.FedExOneRatePackageOverride;
                }
                else
                {
                    request.RequestedShipment.PackagingType = "FEDEX_MEDIUM_BOX";
                }
                request.RequestedShipment.ShipmentSpecialServices = new ShipmentSpecialServicesRequested()
                {
                    SpecialServiceTypes = new[] { "FEDEX_ONE_RATE" }
                };
            }

            if (Shipment.Options.SaturdayDelivery)
            {
                request.RateRequestControlParameters = new RateRequestControlParameters
                {
                    ReturnTransitTimes = true,
                    VariableOptions = RateRequestControlParametersVariableOptions.SATURDAY_DELIVERY
                };
            }

            SetPackageLineItems(request);
            SetShipmentDetails(request);

            return request;
        }
        private IEnumerable<RateRequestType> GetRateRequestTypes()
        {
            yield return RateRequestType.LIST;
            yield return RateRequestType.ACCOUNT;
            if (!string.IsNullOrEmpty(Shipment.Options.PreferredCurrencyCode))
            {
                yield return RateRequestType.PREFERRED;
            }
        }

        /// <summary>
        /// Sets shipment details
        /// </summary>
        /// <param name="request"></param>
        protected abstract void SetShipmentDetails(Full_Schema_Quote_Rate request);

        /// <summary>
        /// Sets package line items
        /// </summary>
        /// <param name="request"></param>
        protected void SetPackageLineItems(Full_Schema_Quote_Rate request)
        {
            request.RequestedShipment.RequestedPackageLineItems = new List<RequestedPackageLineItem>();

            var i = 0;
            foreach (var package in Shipment.Packages)
            {
                var item = new RequestedPackageLineItem()
                {
                    // Package weight
                    Weight = new Weight()
                    {
                        Units = WeightUnits.LB,
                        Value = (double)Shipment.Packages[i].GetRoundedWeight(UnitsSystem.USCustomary),
                    },

                    // Package dimensions
                    Dimensions = new Dimensions()
                    {
                        Length = (int)Shipment.Packages[i].GetRoundedLength(UnitsSystem.USCustomary),
                        Width = (int)Shipment.Packages[i].GetRoundedWidth(UnitsSystem.USCustomary),
                        Height = (int)Shipment.Packages[i].GetRoundedHeight(UnitsSystem.USCustomary),
                        Units = DimensionsUnits.IN,
                    }
                };

                if (_allowInsuredValues && package.InsuredValue > 0)
                {
                    // package insured value
                    item.DeclaredValue = new Money
                    {
                        Amount = (double)package.InsuredValue,
                        Currency = "USD"
                    };
                }

                if (package.SignatureRequiredOnDelivery)
                {
                    item.PackageSpecialServices = new PackageSpecialServicesRequested()
                    {
                        SignatureOptionType = PackageSpecialServicesRequestedSignatureOptionType.DIRECT
                    };
                }

                request.RequestedShipment.RequestedPackageLineItems.Add(item);

                i++;
            }
        }

        /// <summary>
        /// Gets rates
        /// </summary>
        public override async Task GetRates()
        {
            var request = CreateRateRequest();

            var authorization = await FedExOAuthService.GetTokenAsync(_configuration, HttpClient, AddError).ConfigureAwait(false);

            var service = new Client(GetRequestUri(_configuration.UseProduction), HttpClient);
            try
            {
                // Call the web service passing in a RateRequest and returning a RateReply
                var reply = await service.Rate_and_Transit_timesAsync(
                    request,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    "application/json",
                    "en-US",
                    "Bearer " + authorization
                ).ConfigureAwait(false);

                if (reply.Output != null)
                {
                    ProcessReply(reply.Output);
                }
                else
                {
                    AddInternalError($"FedEx provider: API returned NULL result");
                }
            }
            catch (ApiException e)
            {
                ProcessErrors(e);
            }
            catch (Exception e)
            {
                AddInternalError($"FedEx provider exception: {e.Message}");
            }
        }

        /// <summary>
        /// Processes the reply
        /// </summary>
        /// <param name="reply"></param>
        protected void ProcessReply(RateOutputVO reply)
        {
            if (reply?.RateReplyDetails == null)
                return;

            foreach (var rateReplyDetail in reply.RateReplyDetails)
            {
                var key = rateReplyDetail.ServiceType.ToString();

                if (!ServiceCodes.Keys.Contains(key))
                {
                    AddInternalError($"Unknown FedEx rate code: {key}");
                }
                else
                {
                    var rateDetails = GetRateDetailsByRateType(rateReplyDetail);
                    var rates = rateDetails.Select(r => GetCurrencyConvertedRate(r));
                    rates = rates.Any(r => r.currencyCode == Shipment.Options.GetCurrencyCode())
                        ? rates.Where(r => r.currencyCode == Shipment.Options.GetCurrencyCode())
                        : rates;

                    var netCharge = rates.OrderByDescending(r => r.amount).FirstOrDefault();

                    DateTime? deliveryDate = null;
                    if (rateReplyDetail.OperationalDetail.DeliveryDate != null)
                    {
                        DateTime.TryParse(rateReplyDetail.OperationalDetail.DeliveryDate, out DateTime parsedDate);
                        deliveryDate = parsedDate;
                    }

                    if (deliveryDate == null)
                    {
                        deliveryDate = DateTime.Now.AddDays(30);
                    }


                    AddRate(key, ServiceCodes[key], netCharge.amount, deliveryDate.Value, new RateOptions()
                    {
                        SaturdayDelivery = rateReplyDetail.Commit?.SaturdayDelivery ?? false
                    },
                    netCharge.currencyCode);
                }
            }
        }

        private RatedShipmentDetail[] GetRateDetailsByRateType(RateReplyDetail rateReplyDetail)
        {
            var negotiatedRateTypes = new RatedShipmentDetailRateType[]
            {
                RatedShipmentDetailRateType.ACCOUNT,
                RatedShipmentDetailRateType.CUSTOM,
                RatedShipmentDetailRateType.ACTUAL
            };

            return rateReplyDetail.RatedShipmentDetails
                .Where(rsd =>
                    (_configuration.UseNegotiatedRates && negotiatedRateTypes.Contains(rsd.RateType)) ||
                    (!_configuration.UseNegotiatedRates && !negotiatedRateTypes.Contains(rsd.RateType)))
                .ToArray();
        }

        private (decimal amount, string currencyCode) GetCurrencyConvertedRate(RatedShipmentDetail rateDetail)
        {
            var shipmentCurrencyCode = Shipment.Options.GetCurrencyCode();

            if (rateDetail?.TotalNetCharge == null)
                return (0, shipmentCurrencyCode);

            var needCurrencyConversion = rateDetail.ShipmentRateDetail.Currency != shipmentCurrencyCode;
            if (!needCurrencyConversion)
                return ((decimal) rateDetail.TotalNetCharge, shipmentCurrencyCode);

            var canConvertCurrency = (rateDetail.ShipmentRateDetail.CurrencyExchangeRate != null)
                && rateDetail.ShipmentRateDetail.Currency == rateDetail.ShipmentRateDetail.CurrencyExchangeRate.IntoCurrency
                && shipmentCurrencyCode == rateDetail.ShipmentRateDetail.CurrencyExchangeRate.FromCurrency
                && rateDetail.ShipmentRateDetail.CurrencyExchangeRate.Rate != 1
                && rateDetail.ShipmentRateDetail.CurrencyExchangeRate.Rate != 0;

            if (!canConvertCurrency)
                return ((decimal) rateDetail.TotalNetCharge, rateDetail.ShipmentRateDetail.Currency);

            return ((decimal) Math.Round(rateDetail.TotalNetCharge / rateDetail.ShipmentRateDetail.CurrencyExchangeRate.Rate, 2), shipmentCurrencyCode);
        }

        private void ProcessErrors(ApiException exception)
        {
            var msg = exception.Message;
            if (exception is ApiException<ErrorResponseVO> typedResponseException)
            {
                if (typedResponseException.Result?.Errors != null)
                {
                    msg = string.Join(", ", typedResponseException.Result.Errors.Select(e => e.Code));
                }
            }
            else if (exception is ApiException<ErrorResponseVO_2> typedResponseException2)
            {
                if (typedResponseException2.Result?.Errors != null)
                {
                    msg = string.Join(", ", typedResponseException2.Result.Errors.Select(e => e.Code));
                }
            }

            var err = new Error
            {
                Description = msg,
                Source = exception.Source,
                Number = exception.StatusCode.ToString()
            };

            AddError(err);
        }
    }
}
