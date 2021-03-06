﻿using Klarna.Checkout.Models;
using Klarna.Common.Helpers;
using Mediachase.Commerce.Customers;

namespace Klarna.Checkout.Extensions
{
    public static class CustomerAddressExtensions
    {
        public static CheckoutAddressInfo ToAddress(this CustomerAddress customerAddress)
        {
            var address = new CheckoutAddressInfo
            {
                GivenName = customerAddress.FirstName,
                FamilyName = customerAddress.LastName,
                StreetAddress = customerAddress.Line1,
                StreetAddress2 = customerAddress.Line2,
                PostalCode = customerAddress.PostalCode,
                City = customerAddress.City,
                Email = customerAddress.Email,
                Phone = customerAddress.DaytimePhoneNumber ?? customerAddress.EveningPhoneNumber
            };

            var countryCode = CountryCodeHelper.GetTwoLetterCountryCode(customerAddress.CountryCode);
            address.Country = countryCode;
            if (customerAddress.CountryCode != null && customerAddress.RegionName != null)
            {
                address.Region = CountryCodeHelper.GetStateCode(countryCode, customerAddress.RegionName);
            }

            return address;
        }
    }
}
