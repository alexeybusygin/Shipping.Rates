﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShippingRates.ShippingProviders
{
    public class FedExProviderConfiguration
    {
        public string AccountNumber { get; set; }
        public string Key { get; set; }
        public string MeterNumber { get; set; }
        public string Password { get; set; }
        public bool UseProduction { get; set; }
        public bool UseNegotiatedRates { get; set; } = false;
        /// <summary>
        /// Hub ID for FedEx SmartPost.
        /// If not using the production Rate API, you can use 5531 as the HubID per FedEx documentation.
        /// </summary>
        public string HubId { get; set; }
    }
}
