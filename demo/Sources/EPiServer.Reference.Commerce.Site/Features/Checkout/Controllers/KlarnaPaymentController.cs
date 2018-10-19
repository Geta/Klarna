﻿using System.Linq;
using System.Web.Http;
using EPiServer.Commerce.Order;
using EPiServer.Reference.Commerce.Site.Features.Cart.Services;
using Klarna.Payments;
using EPiServer.Reference.Commerce.Site.Infrastructure.Facades;
using Klarna.Common.Extensions;
using Klarna.Common.Models;
using Klarna.Payments.Models;

namespace EPiServer.Reference.Commerce.Site.Features.Checkout.Controllers
{
    [RoutePrefix("klarnaapi")]
    public class KlarnaPaymentController : ApiController
    {
        private readonly CustomerContextFacade _customerContextFacade;
        private readonly IKlarnaPaymentsService _klarnaPaymentsService;
        private readonly ICartService _cartService;

        public KlarnaPaymentController(
            CustomerContextFacade customerContextFacade,
            IKlarnaPaymentsService klarnaPaymentsService,
            ICartService cartService)
        {
            _klarnaPaymentsService = klarnaPaymentsService;
            _customerContextFacade = customerContextFacade;
            _cartService = cartService;
        }

        [Route("personal")]
        [AcceptVerbs("POST")]
        [HttpPost]
        public IHttpActionResult GetpersonalInformation([FromBody]string billingAddressId)
        {
            if (string.IsNullOrWhiteSpace(billingAddressId))
            {
                return BadRequest();
            }

            var cart = _cartService.LoadCart(_cartService.DefaultCartName);
            var sessionRequest = GetPersonalInformationSession(cart, billingAddressId);

            return Ok(sessionRequest);
        }

        [Route("personal/allow")]
        [AcceptVerbs("POST")]
        [HttpPost]
        public IHttpActionResult AllowSharingOfPersonalInformation()
        {
            var cart = _cartService.LoadCart(_cartService.DefaultCartName);
            if (_klarnaPaymentsService.AllowSharingOfPersonalInformation(cart))
            {
                return Ok();
            }

            return InternalServerError();
        }

        [Route("fraud")]
        [AcceptVerbs("Post")]
        [HttpPost]
        public IHttpActionResult FraudNotification(NotificationModel notification)
        {
            _klarnaPaymentsService.FraudUpdate(notification);
            return Ok();
        }

        [Route("cart/{orderGroupId}/push")]
        [AcceptVerbs("POST")]
        [HttpPost]
        public IHttpActionResult Push(int orderGroupId, string klarna_order_id)
        {
            if (klarna_order_id == null)
            {
                return BadRequest();
            }
            var purchaseOrder = GetOrCreatePurchaseOrder(orderGroupId, klarna_order_id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return Ok();
        }

        private PersonalInformationSession GetPersonalInformationSession(ICart cart, string billingAddressId)
        {
            var request = _klarnaPaymentsService.GetPersonalInformationSession(cart);

            // Get billling address info
            var billingAddress =
                _customerContextFacade.CurrentContact.ContactAddresses.FirstOrDefault(x => x.Name == billingAddressId)?
                    .ToAddress();
            request.BillingAddress = billingAddress;

            return request;
        }

        private IPurchaseOrder GetOrCreatePurchaseOrder(int orderGroupId, string klarnaOrderId)
        {
            // Check if the order has been created already
            var purchaseOrder = _klarnaPaymentsService.GetPurchaseOrderByKlarnaOrderId(klarnaOrderId);
            if (purchaseOrder != null)
            {
                return purchaseOrder;
            }

            //<todo>Implement logic to Check cart for klarna payment and create purchase order</todo>

            return null;
        }
    }
}