# Get the fedex rate JSON schema from
https://github.com/ShipStream/fedex-rest-php-sdk/blob/main/resources/models/rates-transit-times/v1.json

or taken from the fedex developer site, save as RateTransitTimes\fedex_rate_v1.json

# Get the fedex rate Freight LTL JSON schema from
https://github.com/ShipStream/fedex-rest-php-sdk/blob/main/resources/models/freight-ltl/v1.json

or taken from the fedex developer site, save as FrieghtLtl\fedex_freight_ltl_v1.json

# Make sure nswag is installed globally
```bash
npm install -g nswag
```

# Generate the client code from the JSON schema file in the current folder
In RateTransitTimes folder:
```bash
nswag openapi2csclient /input:fedex_rate_v1.json /output:FedExRateClient.cs /namespace:ShippingRates.OpenApi.FedEx.RateTransitTimes
```

In FrieghtLtl folder:
```bash
nswag openapi2csclient /input:fedex_freight_ltl_v1.json /output:FedExFreightLtlClient.cs /namespace:ShippingRates.OpenApi.FedEx.FrieghtLtl

```
