﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingSystem.AspNetCore.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenActive.NET;
using OpenActive.NET.Rpde.Version1;
using OpenActive.Server.NET;
using OpenActive.Server.NET.OpenBookingHelper;

namespace BookingSystem.AspNetCore.Controllers
{
    //[Authorize]
    [Route("api/openbooking")]
    [ApiController]
    [Consumes(OpenActiveMediaTypes.OpenBooking.Version1)]
    public class OpenBookingController : ControllerBase
    {
        // Open Booking Errors must be handled as thrown exceptions and the ErrorResponseContent of the exception returned.
        // Note that exceptions may be caught and logged in the usual way, and such error handling moved to a filter or middleware as required,
        // provided that the ErrorResponseContent is still returned. 

        /// Note that this interface expects JSON requests to be supplied as strings, and provides JSON responses as strings.
        /// This ensures that deserialisation is always correct, regardless of the configuration of the web framework.
        /// It also removes the need to expose OpenActive (de)serialisation settings and parsers to the implementer, and makes
        /// this interface more maintainble as OpenActive.NET will likely upgrade to use the new System.Text.Json in time.

        /// <summary>
        /// OrderQuote Creation C1
        /// GET api/openbooking/order-quote-templates/ABCD1234
        /// </summary>
        [HttpPut("order-quote-templates/{uuid}")]
        public ContentResult OrderQuoteCreationC1([FromServices] IBookingEngine bookingEngine, string uuid, [FromBody] string orderQuote)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return bookingEngine.ProcessCheckpoint1(clientId, sellerId, uuid, orderQuote).GetContentResult();                
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        /// <summary>
        /// OrderQuote Creation C2
        /// GET api/openbooking/order-quotes/ABCD1234
        /// </summary>
        [HttpPut("order-quotes/{uuid}")]
        public ContentResult OrderQuoteCreationC2([FromServices] IBookingEngine bookingEngine, string uuid, [FromBody] string orderQuote)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return bookingEngine.ProcessCheckpoint2(clientId, sellerId, uuid, orderQuote).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        /// <summary>
        /// OrderProposal Creation P
        /// GET api/openbooking/order-proposals/ABCD1234
        /// </summary>
        [HttpPut("order-proposals/{uuid}")]
        public ContentResult OrderProposalCreationP([FromServices] IBookingEngine bookingEngine, string uuid, [FromBody] string orderProposal)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return bookingEngine.ProcessOrderProposalCreationP(clientId, sellerId, uuid, orderProposal).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        /// <summary>
        /// OrderQuote Deletion
        /// DELETE api/openbooking/orders-quotes/ABCD1234
        /// </summary>
        [HttpDelete("orders-quotes/{uuid}")]
        public IActionResult OrderQuoteDeletion([FromServices] IBookingEngine bookingEngine, string uuid)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return bookingEngine.DeleteOrderQuote(clientId, sellerId, uuid).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        /// <summary>
        /// Order Creation B
        /// GET api/openbooking/orders/ABCD1234
        /// </summary>
        [HttpPut("orders/{uuid}")]
        public ContentResult OrderCreationB([FromServices] IBookingEngine bookingEngine, string uuid, [FromBody] string order)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return bookingEngine.ProcessOrderCreationB(clientId, sellerId, uuid, order).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        /// <summary>
        /// Order Deletion
        /// DELETE api/openbooking/orders/ABCD1234
        /// </summary>
        [HttpDelete("orders/{uuid}")]
        public IActionResult OrderDeletion([FromServices] IBookingEngine bookingEngine, string uuid)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return bookingEngine.DeleteOrder(clientId, sellerId, uuid).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        /// <summary>
        /// Order Cancellation
        /// PATCH api/openbooking/orders/ABCD1234
        /// </summary>
        [HttpPatch("orders/{uuid}")]
        public IActionResult OrderUpdate([FromServices] IBookingEngine bookingEngine, string uuid, [FromBody] string order)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return bookingEngine.ProcessOrderUpdate(clientId, sellerId, uuid, order).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }


        /// <summary>
        /// OrderProposal Update
        /// PATCH api/openbooking/order-proposals/ABCD1234
        /// </summary>
        [HttpPatch("order-proposals/{uuid}")]
        public IActionResult OrderProposalUpdate([FromServices] IBookingEngine bookingEngine, string uuid, [FromBody] string order)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return bookingEngine.ProcessOrderProposalUpdate(clientId, sellerId, uuid, order).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        // GET api/openbooking/orders/ABCD1234
        [HttpGet("orders/{uuid}")]
        public IActionResult GetOrderStatus([FromServices] IBookingEngine bookingEngine, string uuid)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return bookingEngine.GetOrderStatus(clientId, sellerId, uuid).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        // GET api/openbooking/orders-rpde
        [HttpGet("orders-rpde")]
        public IActionResult GetOrdersFeed([FromServices] IBookingEngine bookingEngine, long? afterTimestamp, string afterId, long? afterChangeNumber)
        {
            try
            {
                // Note only a subset of these parameters will be supplied when this endpoints is called
                // They are all provided here for the bookingEngine to choose the correct endpoint
                // The auth token must also be provided from the associated authentication method
                string clientId = AuthenticationHelper.GetClientIdFromAuth(Request, User);
                return bookingEngine.GetOrdersRPDEPageForFeed(clientId, afterTimestamp, afterId, afterChangeNumber).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }


        // POST api/openbooking/test-interface/datasets/uat-ci/opportunities
        [HttpPost("test-interface/datasets/{testDatasetIdentifier}/opportunities")]
        public IActionResult TestInterfaceDatasetInsert([FromServices] IBookingEngine bookingEngine, string testDatasetIdentifier, [FromBody] string @event)
        {
            try
            {
                return bookingEngine.InsertTestOpportunity(testDatasetIdentifier, @event).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        // DELETE api/openbooking/test-interface/datasets/uat-ci
        [HttpDelete("test-interface/datasets/{testDatasetIdentifier}")]
        public IActionResult TestInterfaceDatasetDelete([FromServices] IBookingEngine bookingEngine, string testDatasetIdentifier)
        {
            try
            {
                return bookingEngine.DeleteTestDataset(testDatasetIdentifier).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        // POST api/openbooking/test-interface/actions
        [HttpPost("test-interface/actions")]
        public IActionResult TestInterfaceAction([FromServices] IBookingEngine bookingEngine, [FromBody] string action)
        {
            try
            {
                return bookingEngine.TriggerTestAction(action).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

    }
}
