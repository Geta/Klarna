﻿using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;
using EPiServer.Reference.Commerce.Site.Features.Shared.Extensions;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Klarna.Common.Helpers;
using Klarna.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Klarna.Payments;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Security;
using Customer = Klarna.Payments.Models.Customer;

namespace EPiServer.Reference.Commerce.Site.Features.Checkout
{
    public class DemoSessionBuilder : ISessionBuilder
    {
        private Injected<UrlResolver> _urlResolver = default(Injected<UrlResolver>);
        private Injected<IContentRepository> _contentRepository = default(Injected<IContentRepository>);
        private Injected<ReferenceConverter> _referenceConverter = default(Injected<ReferenceConverter>);
        private Injected<IRelationRepository> _relationRepository = default(Injected<IRelationRepository>);

        public Klarna.Payments.Models.Session Build(
            Klarna.Payments.Models.Session session, ICart cart, PaymentsConfiguration paymentsConfiguration, IDictionary<string, object> dic = null, bool includePersonalInformation = false)
        {
            if (includePersonalInformation && paymentsConfiguration.CustomerPreAssessment)
            {
                session.Customer = new Customer
                {
                    DateOfBirth = "1980-01-01",
                    Gender = "Male",
                    LastFourSsn = "1234"
                };
            }
            session.MerchantReference2 = "12345";

            if (paymentsConfiguration.UseAttachments && PrincipalInfo.CurrentPrincipal.Identity.IsAuthenticated)
            {
                var converter = new IsoDateTimeConverter
                {
                    DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"
                };

                var customerContact = PrincipalInfo.CurrentPrincipal.GetCustomerContact();

                var customerAccountInfos = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            { "unique_account_identifier",  PrincipalInfo.CurrentPrincipal.GetContactId() },
                            { "account_registration_date", customerContact.Created },
                            { "account_last_modified", customerContact.Modified }
                        }
                    };

                var emd = new Dictionary<string, object>
                    {
                        { "customer_account_info", customerAccountInfos}
                    };

                session.Attachment = new Attachment
                {
                    ContentType = "application/vnd.klarna.internal.emd-v2+json",
                    Body = JsonConvert.SerializeObject(emd, converter)
                };
            }

            if (session.OrderLines != null)
            {
                foreach (var lineItem in session.OrderLines)
                {
                    if (lineItem.Type.Equals("physical"))
                    {
                        EntryContentBase entryContent = null;
                        FashionProduct product = null;
                        if (!string.IsNullOrEmpty(lineItem.Reference))
                        {
                            var contentLink = _referenceConverter.Service.GetContentLink(lineItem.Reference);
                            if (!ContentReference.IsNullOrEmpty(contentLink))
                            {
                                entryContent = _contentRepository.Service.Get<EntryContentBase>(contentLink);

                                var parentLink =
                                    entryContent.GetParentProducts(_relationRepository.Service).SingleOrDefault();
                                _contentRepository.Service.TryGet<FashionProduct>(parentLink, out product);
                            }
                        }

                        if (lineItem.ProductIdentifiers == null)
                        {
                            lineItem.ProductIdentifiers = new ProductIdentifiers();
                        }

                        lineItem.ProductIdentifiers.Brand = product?.Brand;
                        lineItem.ProductIdentifiers.GlobalTradeItemNumber = "GlobalTradeItemNumber test";
                        lineItem.ProductIdentifiers.ManufacturerPartNumber = "ManuFacturerPartNumber test";
                        lineItem.ProductIdentifiers.CategoryPath = "test / test";

                        if (paymentsConfiguration.SendProductAndImageUrlField && entryContent != null)
                        {
                            lineItem.ProductUrl = SiteUrlHelper.GetAbsoluteUrl()
                                                                       + entryContent.GetUrl(_relationRepository.Service, _urlResolver.Service);
                        }
                    }
                }
            }
            return session;
        }
    }
}