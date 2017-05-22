﻿using System;
using System.Linq;
using EPiServer.Globalization;
using Klarna.Common.Extensions;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;
using Newtonsoft.Json;

namespace Klarna.Payments.Extensions
{
    public static class PaymentMethodDtoExtensions
    {
        public static Configuration GetConfiguration(this PaymentMethodDto paymentMethodDto,
            MarketId marketId)
        {
            var allConfigurations = JsonConvert.DeserializeObject<Configuration[]>(paymentMethodDto.GetParameter(Common.Constants.KlarnaSerializedMarketOptions, "[]"));
            var configurationForMarket = allConfigurations.FirstOrDefault(x => x.MarketId.Equals(marketId.ToString(), StringComparison.InvariantCultureIgnoreCase));
            if (configurationForMarket == null)
            {
                throw new Exception(
                    $"PaymentMethod {Constants.KlarnaPaymentSystemKeyword} is not configured for market {marketId} and language {ContentLanguage.PreferredCulture.Name}");
            }
            return configurationForMarket;
        }
    }
}
