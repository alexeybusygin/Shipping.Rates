using ShippingRates.Helpers.Extensions;
using System.Collections.Generic;
using ShippingRates.OpenApi.FedEx.FrieghtLtl;
using ShippingRates.Services.FedEx;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace ShippingRates.ShippingProviders.FedExRest
{
    /// <summary>
    ///     Provides Freight LTL rates (only) from FedEx (Federal Express) REST API.
    /// </summary>
    public class FedExRestFreightLtlProvider : FedExRestBaseProvider
    {
        public override string Name { get => "FedExSmartFreightLtl"; }


        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        public FedExRestFreightLtlProvider(string clientId, string clientSecret, string accountNumber)
            : this(clientId, clientSecret, accountNumber, true) { }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="useProduction"></param>
        public FedExRestFreightLtlProvider(string clientId, string clientSecret, string accountNumber, bool useProduction)
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
        public FedExRestFreightLtlProvider(FedExRestProviderConfiguration configuration)
            : base(configuration) { }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="httpClient"></param>
        public FedExRestFreightLtlProvider(string clientId, string clientSecret, string accountNumber, HttpClient httpClient)
            : this(clientId, clientSecret, accountNumber, true, httpClient) { }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="accountNumber"></param>
        /// <param name="useProduction"></param>
        /// <param name="httpClient"></param>
        public FedExRestFreightLtlProvider(string clientId, string clientSecret, string accountNumber, bool useProduction, HttpClient httpClient)
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
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="httpClient"></param>
        public FedExRestFreightLtlProvider(FedExRestProviderConfiguration configuration, HttpClient httpClient)
            : base(configuration, httpClient)
        {
        }


        /// <summary>
        /// Sets the service codes.
        /// </summary>
        protected override Dictionary<string, string> ServiceCodes => new Dictionary<string, string>
        {
            {"FEDEX_FREIGHT_PRIORITY", "FEDEX_FREIGHT_ECONOMY"}
        };

        /// <summary>
        /// Creates the rate request
        /// </summary>
        /// <returns></returns>
        protected FullSchema CreateRateRequest()
        {
            // Build the RateRequest
            var request = new FullSchema
            {
                AccountNumber = new LTLRootAccountNumber() { Value = _configuration.AccountNumber },
                FreightRequestedShipment = new LTLRequestedShipment
                {
                    Shipper = new RateParty
                    {
                        Address = new ShippingRates.OpenApi.FedEx.FrieghtLtl.Address
                        {
                            PostalCode = Shipment.OriginAddress.PostalCode,
                            CountryCode = Shipment.OriginAddress.CountryCode
                        }
                    },
                    Recipient = new RateParty
                    {
                        Address = new ShippingRates.OpenApi.FedEx.FrieghtLtl.Address
                        {
                            PostalCode = Shipment.DestinationAddress.PostalCode,
                            CountryCode = Shipment.DestinationAddress.CountryCode
                        }
                    },
                    RequestedPackageLineItems = new LTLRequestedPackageLineItem[]
                    {
                    },
                    RateRequestType = GetRateRequestTypes().ToArray(),
                    PreferredCurrency = Shipment.Options.GetCurrencyCode(),
                    TotalPackageCount = Shipment.Packages.Count,
                },
            };

            var shipDateStamp = Shipment.Options.ShippingDate == null ? null : (DateTime.Now).ToString("yyyy-MM-dd"); // Shipping date and time
            if (shipDateStamp != null)
            {
                request.FreightRequestedShipment.ShipDateStamp = shipDateStamp;
            }

            if (Shipment.Options.SaturdayDelivery)
            {
                request.RateRequestControlParameters = new LTLRateRequestControlParameters
                {
                    VariableOptions = LTLRateRequestControlParametersVariableOptions.SATURDAY_DELIVERY
                };
            }

            SetPackageLineItems(request);

            return request;
        }
        private IEnumerable<RateRequestType> GetRateRequestTypes()
        {
            yield return RateRequestType.LIST;
            yield return RateRequestType.ACCOUNT;
        }

        /// <summary>
        /// Sets package line items
        /// </summary>
        /// <param name="request"></param>
        protected void SetPackageLineItems(FullSchema request)
        {
            request.FreightRequestedShipment.RequestedPackageLineItems = new List<LTLRequestedPackageLineItem>();

            var i = 0;
            foreach (var package in Shipment.Packages)
            {
                var item = new LTLRequestedPackageLineItem()
                {
                    SubPackagingType = Shipment.Packages[i].Container ?? "CONTAINER",
                    // Package weight
                    Weight = new Weight()
                    {
                        Units = WeightUnits.KG,
                        Value = (double)Shipment.Packages[i].GetRoundedWeight(UnitsSystem.Metric),
                    },

                    // Package dimensions
                    Dimensions = new Dimensions()
                    {
                        Length = (int)Shipment.Packages[i].GetRoundedLength(UnitsSystem.Metric),
                        Width = (int)Shipment.Packages[i].GetRoundedWidth(UnitsSystem.Metric),
                        Height = (int)Shipment.Packages[i].GetRoundedHeight(UnitsSystem.Metric),
                        Units = DimensionsUnits.CM,
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

                request.FreightRequestedShipment.RequestedPackageLineItems.Add(item);

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
                var reply = await service.Freight_RateQuoteAsync(
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

        private RatedShipmentDetail[] GetRateDetailsByRateType(LTLRateReplyDetail rateReplyDetail)
        {
            var negotiatedRateTypes = new RatedShipmentDetailRateType[]
            {
                RatedShipmentDetailRateType.ACCOUNT,
                RatedShipmentDetailRateType.CUSTOM,
                RatedShipmentDetailRateType.INCENTIVE,
                RatedShipmentDetailRateType.ACTUAL
            };

            return negotiatedRateTypes.Contains(rateReplyDetail.RatedShipmentDetails.RateType) ?
                new RatedShipmentDetail[] { rateReplyDetail.RatedShipmentDetails } :
                new RatedShipmentDetail[0];
        }

        private (decimal amount, string currencyCode) GetCurrencyConvertedRate(RatedShipmentDetail rateDetail)
        {
            var shipmentCurrencyCode = Shipment.Options.GetCurrencyCode();

            if (rateDetail?.TotalNetCharge == null)
                return (0, shipmentCurrencyCode);

            var needCurrencyConversion = rateDetail.ShipmentRateDetail.Currency != shipmentCurrencyCode;
            if (!needCurrencyConversion)
                return ((decimal)rateDetail.TotalNetCharge, shipmentCurrencyCode);

            var canConvertCurrency = (rateDetail.ShipmentRateDetail.CurrencyExchangeRate != null)
                && rateDetail.ShipmentRateDetail.Currency == rateDetail.ShipmentRateDetail.CurrencyExchangeRate.IntoCurrency
                && shipmentCurrencyCode == rateDetail.ShipmentRateDetail.CurrencyExchangeRate.FromCurrency
                && rateDetail.ShipmentRateDetail.CurrencyExchangeRate.Rate != 1
                && rateDetail.ShipmentRateDetail.CurrencyExchangeRate.Rate != 0;

            if (!canConvertCurrency)
                return ((decimal)rateDetail.TotalNetCharge, rateDetail.ShipmentRateDetail.Currency);

            return ((decimal)Math.Round(rateDetail.TotalNetCharge / rateDetail.ShipmentRateDetail.CurrencyExchangeRate.Rate, 2), shipmentCurrencyCode);
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
