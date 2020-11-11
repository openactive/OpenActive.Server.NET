using BookingSystem.AspNetFramework.Helpers;
using OpenActive.Server.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using System;
using System.Net.Http;
using System.Web.Http;
using OpenActive.NET;

namespace BookingSystem.AspNetFramework.Controllers
{
    [RoutePrefix("api/openbooking")]
    public class OpenBookingController : ApiController
    {
        private readonly IBookingEngine _bookingEngine;

        public OpenBookingController(IBookingEngine bookingEngine)
        {
            _bookingEngine = bookingEngine;
        }

        /// <summary>
        /// OrderQuote Creation C1
        /// GET api/openbooking/order-quote-templates/ABCD1234
        /// </summary>
        [HttpPut]
        [Route("order-quote-templates/{uuid}")]
        public HttpResponseMessage OrderQuoteCreationC1(string uuid, [FromBody] string orderQuote)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return _bookingEngine.ProcessCheckpoint1(clientId, sellerId, uuid, orderQuote).GetContentResult();
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
        [HttpPut]
        [Route("order-quotes/{uuid}")]
        public HttpResponseMessage OrderQuoteCreationC2(string uuid, [FromBody] string orderQuote)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return _bookingEngine.ProcessCheckpoint2(clientId, sellerId, uuid, orderQuote).GetContentResult();
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
        [HttpPut()]
        [Route("order-proposals/{uuid}")]
        public HttpResponseMessage OrderProposalCreationP(string uuid, [FromBody] string orderProposal)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return _bookingEngine.ProcessOrderProposalCreationP(clientId, sellerId, uuid, orderProposal).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        /// <summary>
        /// OrderQuote Deletion
        /// DELETE api/openbooking/order-quotes/ABCD1234
        /// </summary>
        [HttpDelete]
        [Route("order-quotes/{uuid}")]
        public HttpResponseMessage OrderQuoteDeletion(string uuid)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return _bookingEngine.DeleteOrderQuote(clientId, sellerId, uuid).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }


        /// <summary>
        /// Order Creation B
        /// PUT api/openbooking/orders/ABCD1234
        /// </summary>
        [HttpPut]
        [Route("orders/{uuid}")]
        public HttpResponseMessage OrderCreationB(string uuid, [FromBody] string order)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return _bookingEngine.ProcessOrderCreationB(clientId, sellerId, uuid, order).GetContentResult();
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
        [HttpDelete]
        [Route("orders/{uuid}")]
        public HttpResponseMessage OrderDeletion(string uuid)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return _bookingEngine.DeleteOrder(clientId, sellerId, uuid).GetContentResult();
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
        [HttpPatch]
        [Route("orders/{uuid}")]
        public HttpResponseMessage OrderUpdate(string uuid, [FromBody] string order)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return _bookingEngine.ProcessOrderUpdate(clientId, sellerId, uuid, order).GetContentResult();
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
        [HttpPatch()]
        [Route("order-proposals/{uuid}")]
        public HttpResponseMessage OrderProposalUpdate(string uuid, [FromBody] string order)
        {
            try
            {
                (string clientId, Uri sellerId) = AuthenticationHelper.GetIdsFromAuth(Request, User);
                return _bookingEngine.ProcessOrderProposalUpdate(clientId, sellerId, uuid, order).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        // GET api/openbooking/orders-rpde
        [HttpGet]
        [Route("orders-rpde")]
        public HttpResponseMessage GetOrdersFeed(long? afterTimestamp = (long?)null, string afterId = null, long? afterChangeNumber = (long?)null)
        {
            try
            {
                // Note only a subset of these parameters will be supplied when this endpoints is called
                // They are all provided here for the bookingEngine to choose the correct endpoint
                // The auth token must also be provided from the associated authentication method
                string clientId = AuthenticationHelper.GetClientIdFromAuth(Request, User);
                return _bookingEngine.GetOrdersRPDEPageForFeed(clientId, afterTimestamp, afterId, afterChangeNumber).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        // POST api/openbooking/test-interface/datasets/uat-ci/opportunities
        [HttpPost]
        [Route("test-interface/datasets/{testDatasetIdentifier}/opportunities")]
        public HttpResponseMessage TestInterfaceDatasetInsert(string testDatasetIdentifier, [FromBody] string @event)
        {
            try
            {
                return _bookingEngine.InsertTestOpportunity(testDatasetIdentifier, @event).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        // DELETE api/openbooking/test-interface/datasets/uat-ci
        [HttpDelete]
        [Route("test-interface/datasets/{testDatasetIdentifier}")]
        public HttpResponseMessage TestInterfaceDatasetDelete(string testDatasetIdentifier)
        {
            try
            {
                return _bookingEngine.DeleteTestDataset(testDatasetIdentifier).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }


        // POST api/openbooking/test-interface/actions
        [HttpPost]
        [Route("test-interface/actions")]
        public HttpResponseMessage TestInterfaceAction([FromBody] string action)
        {
            try
            {
                return _bookingEngine.TriggerTestAction(action).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

        [HttpGet]
        [Route("error/{code}")]
        public HttpResponseMessage Error(int code)
        {
            OpenBookingException error;
            switch (code)
            {
                case 404:
                    error = new OpenBookingException(new UnknownOrIncorrectEndpointError());
                    break;
                case 405:
                    error = new OpenBookingException(new MethodNotAllowedError());
                    break;
                case 429:
                    error = new OpenBookingException(new TooManyRequestsError());
                    break;
                case 403:
                    error = new OpenBookingException(new UnauthenticatedError());
                    break;
                default:
                    error = new InternalOpenBookingException(new InternalApplicationError());
                    break;
            }

            return error.ErrorResponseContent.GetContentResult();
        }

        // Catch-all route necessary to return custom 404s
        [HttpGet]
        [HttpPatch]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        [Route("{*url}", Order = 999)]
        public HttpResponseMessage CatchAll()
        {
            var error = new OpenBookingException(new UnknownOrIncorrectEndpointError());
            return error.ErrorResponseContent.GetContentResult();
        }
    }
}
