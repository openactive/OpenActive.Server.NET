﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using OpenActive.NET.Rpde.Version1;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.StoreBooking;

namespace OpenActive.Server.NET.CustomBooking
{
    /// <summary>
    /// The AbstractBookingEngine provides a simple, basic and extremely flexible implementation of Open Booking API.
    /// 
    /// It is designed for systems where their needs are not met by StoreBookingEngine to provide a solid foundation for their implementations.
    /// 
    /// Methods of this class will return OpenActive POCO models that can be rendered using ToOpenActiveString(),
    /// and throw exceptions that subclass OpenActiveException, on which GetHttpStatusCode() and ToOpenActiveString() can
    /// be called to construct a response.
    /// </summary>
    public abstract class CustomBookingEngine : IBookingEngine
    {
        /// <summary>
        /// In this mode, the Booking Engine also handles generation of open data feeds and the dataset site
        /// 
        /// Note this is also the mode used by the StoreBookingEngine
        /// 
        /// In order to use RenderDatasetSite, DatasetSiteGeneratorSettings must be provided
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="datasetSettings"></param>
        public CustomBookingEngine(BookingEngineSettings settings, DatasetSiteGeneratorSettings datasetSettings) : this(settings, datasetSettings?.OpenBookingAPIBaseUrl, datasetSettings?.OpenDataFeedBaseUrl)
        {
            if (datasetSettings == null) throw new ArgumentNullException(nameof(datasetSettings));
            this.datasetSettings = datasetSettings;
        }

        /// <summary>
        /// In this mode, the Booking Engine additionally handles generation of open data feeds, but the dataset site is handled manually
        /// 
        /// In order to generate open data RPDE pages, OpenDataFeedBaseUrl must be provided
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="openBookingAPIBaseUrl"></param>
        /// <param name="openDataFeedBaseUrl"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "Exception relates to specific settings property being null")]
        public CustomBookingEngine(BookingEngineSettings settings, Uri openBookingAPIBaseUrl, Uri openDataFeedBaseUrl) : this (settings, openBookingAPIBaseUrl)
        {
            // Check constructor configuration is correct
            if (openDataFeedBaseUrl == null) throw new ArgumentNullException(nameof(openDataFeedBaseUrl));
            if (settings.OpenDataFeeds == null) throw new ArgumentNullException("settings.OpenDataFeeds");


            // Check Seller configuration is provided
            if (settings.SellerStore == null || settings.SellerIdTemplate == null)
            {
                throw new EngineConfigurationException("SellerStore and SellerIdTemplate must be specified in BookingEngineSettings");
            }

            this.openDataFeedBaseUrl = openDataFeedBaseUrl;

            foreach (var idConfiguration in settings.IdConfiguration) {
                idConfiguration.RequiredBaseUrl = settings.JsonLdIdBaseUrl;
            }
            settings.OrderIdTemplate.RequiredBaseUrl = openBookingAPIBaseUrl;
            settings.SellerIdTemplate.RequiredBaseUrl = settings.JsonLdIdBaseUrl;

            // Create a lookup of each IdTemplate to pass into the appropriate RpdeGenerator
            // TODO: Output better error if there is a feed assigned across two templates
            // (there should never be, as each template represents everyting you need in one feed)
            this.feedAssignedTemplates = settings.IdConfiguration.Select(t => t.IdConfigurations.Select(x => new
            {
                assignedFeed = x.AssignedFeed,
                bookablePairIdTemplate = t
            })).SelectMany(x => x.ToList()).ToDictionary(k => k.assignedFeed, v => v.bookablePairIdTemplate);

            // Create a lookup for the purposes of finding arbitary IdConfigurations, for use in the store
            // TODO: Pull this and the above into a function?
            this.OpportunityTemplateLookup = settings.IdConfiguration.Select(t => t.IdConfigurations.Select(x => new
            {
                opportunityType = x.OpportunityType,
                bookablePairIdTemplate = t
            })).SelectMany(x => x.ToList()).ToDictionary(k => k.opportunityType, v => v.bookablePairIdTemplate);

            // Setup each RPDEFeedGenerator with the relevant settings, including the relevant IdTemplate inferred from the config
            foreach (var kv in settings.OpenDataFeeds)
            {
                kv.Value.SetConfiguration(OpportunityTypes.Configurations[kv.Key], settings.JsonLdIdBaseUrl, settings.RPDEPageSize, this.feedAssignedTemplates[kv.Key], settings.SellerIdTemplate, openDataFeedBaseUrl);
            }

            // Note that this library does not currently support custom Orders Feed URLs
            var ordersFeedUrl = new Uri(openBookingAPIBaseUrl.ToString() + "orders-rpde");
            settings.OrderFeedGenerator.SetConfiguration(settings.RPDEPageSize, settings.OrderIdTemplate, settings.SellerIdTemplate, ordersFeedUrl);

            settings.SellerStore.SetConfiguration(settings.SellerIdTemplate);

            // Create a dictionary of RPDEFeedGenerator indexed by FeedPath
            this.feedLookup = settings.OpenDataFeeds.Values.ToDictionary(x => x.FeedPath);

            // Set supportedFeeds locally for use by dataset site
            this.supportedFeeds = settings.OpenDataFeeds.Keys.ToList();

            // Check that OpenDataFeeds match IdConfiguration
            if (supportedFeeds.Except(feedAssignedTemplates.Keys).Any() || feedAssignedTemplates.Keys.Except(supportedFeeds).Any())
            {
                throw new ArgumentException("Feeds configured in OpenDataFeeds must match those in IdConfiguration");
            }

            // Setup array of types for lookup of OrderItem, based on the type string that will be supplied with the opportunity
            this.idConfigurationLookup = settings.IdConfiguration.Select(t => t.IdConfigurations.Select(x => new
            {
                // TODO: Create an extra prop in DatasetSite lib so that we don't need to parse the URL here
                opportunityTypeName = OpportunityTypes.Configurations[x.OpportunityType].SameAs.AbsolutePath.Trim('/'),
                bookablePairIdTemplate = t
            })).SelectMany(x => x.ToList())
            .GroupBy(g => g.opportunityTypeName)
            .ToDictionary(k => k.Key, v => v.Select(y => y.bookablePairIdTemplate).ToList());

        }

        private DatasetSiteGeneratorSettings datasetSettings = null;
        private readonly BookingEngineSettings settings;
        private Dictionary<string, IOpportunityDataRPDEFeedGenerator> feedLookup;
        private List<OpportunityType> supportedFeeds;
        private Uri openDataFeedBaseUrl;
        private Uri openBookingAPIBaseUrl;
        private Dictionary<string, List<IBookablePairIdTemplate>> idConfigurationLookup;
        private Dictionary<OpportunityType, IBookablePairIdTemplate> feedAssignedTemplates;

        protected Dictionary<OpportunityType, IBookablePairIdTemplate> OpportunityTemplateLookup { get; }

        /// <summary>
        /// In this mode, the Booking Engine does not handle open data feeds or dataset site rendering, and these must both be handled manually
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="openBookingAPIBaseUrl"></param>
        public CustomBookingEngine(BookingEngineSettings settings, Uri openBookingAPIBaseUrl)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (openBookingAPIBaseUrl == null) throw new ArgumentNullException(nameof(openBookingAPIBaseUrl));

            this.settings = settings;
        }

        /// <summary>
        /// Handler for Dataset Site endpoint
        /// </summary>
        /// <returns></returns>
        public ResponseContent RenderDatasetSite()
        {
            if (datasetSettings == null || supportedFeeds == null) throw new NotSupportedException("RenderDatasetSite is only supported if DatasetSiteGeneratorSettings are supplied to the IBookingEngine");
            // TODO add caching layer in front of dataset site rendering
            return ResponseContent.HtmlResponse(DatasetSiteGenerator.RenderSimpleDatasetSite(datasetSettings, supportedFeeds));
        }

        /// <summary>
        /// Handler for an RPDE endpoint - string only version
        /// Designed to be used on a single controller method with a "feedname" parameter,
        /// for uses in situations where the framework does not automatically validate numeric values
        /// </summary>
        /// <param name="feedname">The final component of the path of the feed, i.e. https://example.com/feeds/{feedname} </param>
        /// <param name="afterTimestamp">The "afterTimestamp" parameter from the URL</param>
        /// <param name="afterId">The "afterId" parameter from the URL</param>
        /// <param name="afterChangeNumber">The "afterChangeNumber" parameter from the URL</param>
        /// <returns></returns>
        public ResponseContent GetOpenDataRPDEPageForFeed(string feedname, string afterTimestamp, string afterId, string afterChangeNumber)
        {
            return ResponseContent.RpdeResponse(
                RouteOpenDataRPDEPageForFeed(
                    feedname,
                    RpdeOrderingStrategyRouter.ConvertStringToLongOrThrow(afterTimestamp, nameof(afterTimestamp)),
                    afterId,
                    RpdeOrderingStrategyRouter.ConvertStringToLongOrThrow(afterChangeNumber, nameof(afterChangeNumber))
                    ).ToString());
        }


        /// <summary>
        /// Handler for an RPDE endpoint
        /// Designed to be used on a single controller method with a "feedname" parameter,
        /// for uses in situations where the framework does not automatically validate numeric values
        /// </summary>
        /// <param name="feedname">The final component of the path of the feed, i.e. https://example.com/feeds/{feedname} </param>
        /// <param name="afterTimestamp">The "afterTimestamp" parameter from the URL</param>
        /// <param name="afterId">The "afterId" parameter from the URL</param>
        /// <param name="afterChangeNumber">The "afterChangeNumber" parameter from the URL</param>
        /// <returns></returns>
        public ResponseContent GetOpenDataRPDEPageForFeed(string feedname, long? afterTimestamp, string afterId, long? afterChangeNumber)
        {
            return ResponseContent.RpdeResponse(RouteOpenDataRPDEPageForFeed(feedname, afterTimestamp, afterId, afterChangeNumber).ToString());
        }




        /// <summary>
        /// Handler for an RPDE endpoint
        /// Designed to be used on a single controller method with a "feedname" parameter
        /// </summary>
        /// <param name="feedname">The final component of the path of the feed, i.e. https://example.com/feeds/{feedname} </param>
        /// <param name="afterTimestamp">The "afterTimestamp" parameter from the URL</param>
        /// <param name="afterId">The "afterId" parameter from the URL</param>
        /// <param name="afterChangeNumber">The "afterChangeNumber" parameter from the URL</param>
        /// <returns></returns>
        private RpdePage RouteOpenDataRPDEPageForFeed(string feedname, long? afterTimestamp, string afterId, long? afterChangeNumber)
        {
            if (openDataFeedBaseUrl == null) throw new NotSupportedException("GetOpenDataRPDEPageForFeed is only supported if an OpenDataFeedBaseUrl and BookingEngineSettings.OpenDataFeed is supplied to the IBookingEngine");

            if (feedLookup.TryGetValue(feedname, out IOpportunityDataRPDEFeedGenerator generator))
            {
                return generator.GetRPDEPage(feedname, afterTimestamp, afterId, afterChangeNumber);
            } else
            {
                throw new OpenBookingException(new NotFoundError(), $"OpportunityTypeConfiguration for '{feedname}' not found.");
            }
        }

        /// <summary>
        /// Handler for an Orders RPDE endpoint (separate to the open data endpoint for security) - string only version
        /// Designed to be used on a single controller method with a "feedname" parameter,
        /// for uses in situations where the framework does not automatically validate numeric values
        /// </summary>
        /// <param name="clientId">Token designating the specific authenticated party for which the feed is intended</param>
        /// <param name="afterTimestamp">The "afterTimestamp" parameter from the URL</param>
        /// <param name="afterId">The "afterId" parameter from the URL</param>
        /// <param name="afterChangeNumber">The "afterChangeNumber" parameter from the URL</param>
        /// <returns></returns>
        public ResponseContent GetOrdersRPDEPageForFeed(string clientId, string afterTimestamp, string afterId, string afterChangeNumber)
        {
            return ResponseContent.RpdeResponse(
                RenderOrdersRPDEPageForFeed(
                    clientId,
                    RpdeOrderingStrategyRouter.ConvertStringToLongOrThrow(afterTimestamp, nameof(afterTimestamp)),
                    afterId,
                    RpdeOrderingStrategyRouter.ConvertStringToLongOrThrow(afterChangeNumber, nameof(afterChangeNumber))
                    ).ToString());
        }

        /// <summary>
        /// Handler for an Orders RPDE endpoint (separate to the open data endpoint for security)
        /// For uses in situations where the framework does not automatically validate numeric values
        /// </summary>
        /// <param name="clientId">Token designating the specific authenticated party for which the feed is intended</param>
        /// <param name="afterTimestamp">The "afterTimestamp" parameter from the URL</param>
        /// <param name="afterId">The "afterId" parameter from the URL</param>
        /// <param name="afterChangeNumber">The "afterChangeNumber" parameter from the URL</param>
        /// <returns></returns>
        public ResponseContent GetOrdersRPDEPageForFeed(string clientId, long? afterTimestamp, string afterId, long? afterChangeNumber)
        {
            return ResponseContent.RpdeResponse(RenderOrdersRPDEPageForFeed(clientId, afterTimestamp, afterId, afterChangeNumber).ToString());
        }

        /// <summary>
        /// Handler for Orders RPDE endpoint
        /// </summary>
        /// <param name="clientId">Token designating the specific authenticated party for which the feed is intended</param>
        /// <param name="afterTimestamp">The "afterTimestamp" parameter from the URL</param>
        /// <param name="afterId">The "afterId" parameter from the URL</param>
        /// <param name="afterChangeNumber">The "afterChangeNumber" parameter from the URL</param>
        /// <returns></returns>
        private RpdePage RenderOrdersRPDEPageForFeed(string clientId, long? afterTimestamp, string afterId, long? afterChangeNumber)
        {
            if (settings.OrderFeedGenerator != null)
            {
                // Add lookup against clientId and pass this into generator?
                return settings.OrderFeedGenerator.GetRPDEPage(clientId, afterTimestamp, afterId, afterChangeNumber);
            }
            else
            {
                // TODO: Change to Not Authorised Error
                throw new OpenBookingException(new NotFoundError(), $"Access to this endpoint is not authorised.");
            }
        }

        public ResponseContent GetOrderStatus(string clientId, Uri sellerId, string uuid)
        {
            var (orderId, sellerIdComponents, seller) = ConstructIdsFromRequest(clientId, sellerId, uuid, OrderType.Order);
            var result = ProcessGetOrderStatus(orderId, sellerIdComponents, seller);
            if (result == null)
            {
                throw new OpenBookingException(new UnknownOrderError());
            } else
            {
                return ResponseContent.OpenBookingResponse(OpenActiveSerializer.Serialize(result), HttpStatusCode.OK);
            }
        }

        protected abstract Order ProcessGetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller);


        protected bool IsOpportunityTypeRecognised(string opportunityTypeString)
        {
            return this.idConfigurationLookup.ContainsKey(opportunityTypeString);
        }

        // Note this is not a helper as it relies on engine settings state
        protected IBookableIdComponents ResolveOpportunityID(string opportunityTypeString, Uri opportunityId, Uri offerId)
        {
            // Return the first matching ID combination for the opportunityId and offerId provided.
            // TODO: Make this more efficient?
            return this.idConfigurationLookup[opportunityTypeString]
                .Select(x => x.GetOpportunityReference(opportunityId, offerId))
                .Where(x => x != null)
                .FirstOrDefault();
        }

        // Note this is not a helper as it relies on engine settings state
        protected IBookableIdComponents ResolveOpportunityID(string opportunityTypeString, Uri opportunityId)
        {
            // Return the first matching ID combination for the opportunityId and offerId provided.
            // TODO: Make this more efficient?
            return this.idConfigurationLookup[opportunityTypeString]
                .Select(x => x.GetOpportunityBookableIdComponents(opportunityId))
                .Where(x => x != null)
                .FirstOrDefault();
        }

        public ResponseContent ProcessCheckpoint1(string clientId, Uri sellerId, string uuid, string orderQuoteJson)
        {
            return ProcessCheckpoint(clientId, sellerId, uuid, orderQuoteJson, FlowStage.C1, OrderType.OrderQuote);
        }
        public ResponseContent ProcessCheckpoint2(string clientId, Uri sellerId, string uuid, string orderQuoteJson)
        {
            return ProcessCheckpoint(clientId, sellerId, uuid, orderQuoteJson, FlowStage.C2, OrderType.OrderQuote);
        }
        private ResponseContent ProcessCheckpoint(string clientId, Uri sellerId, string uuid, string orderQuoteJson, FlowStage flowStage, OrderType orderType)
        {
            OrderQuote orderQuote = OpenActiveSerializer.Deserialize<OrderQuote>(orderQuoteJson);
            if (orderQuote == null || orderQuote.GetType() != typeof(OrderQuote))
            {
                throw new OpenBookingException(new UnexpectedOrderTypeError(), "OrderQuote is required for C1 and C2");
            }
            var (orderId, sellerIdComponents, seller) = ConstructIdsFromRequest(clientId, sellerId, uuid, orderType);
            var orderResponse = ProcessFlowRequest(ValidateFlowRequest<OrderQuote>(orderId, sellerIdComponents, seller, flowStage, orderQuote), orderQuote);
            // Return a 409 status code if any OrderItem level errors exist
            return ResponseContent.OpenBookingResponse(OpenActiveSerializer.Serialize(orderResponse),
                orderResponse.OrderedItem.Exists(x => x.Error?.Count > 0) ? HttpStatusCode.Conflict : HttpStatusCode.OK);
        }
        public ResponseContent ProcessOrderCreationB(string clientId, Uri sellerId, string uuid, string orderJson)
        {
            // Note B will never contain OrderItem level errors, and any issues that occur will be thrown as exceptions.
            // If C1 and C2 are used correctly, B should not fail except in very exceptional cases.
            Order order = OpenActiveSerializer.Deserialize<Order>(orderJson);
            if (order == null || order.GetType() != typeof(Order))
            {
                throw new OpenBookingException(new UnexpectedOrderTypeError(), "Order is required for B");
            }
            var (orderId, sellerIdComponents, seller) = ConstructIdsFromRequest(clientId, sellerId, uuid, OrderType.Order);
            var response = order.OrderProposalVersion != null ?
                 ProcessOrderCreationFromOrderProposal(orderId, settings.OrderIdTemplate, seller, sellerIdComponents, order) :
                 ProcessFlowRequest(ValidateFlowRequest<Order>(orderId, sellerIdComponents, seller, FlowStage.B, order), order);
            return ResponseContent.OpenBookingResponse(OpenActiveSerializer.Serialize(response), HttpStatusCode.OK);
        }

        public ResponseContent ProcessOrderProposalCreationP(string clientId, Uri sellerId, string uuid, string orderJson)
        {
            // Note B will never contain OrderItem level errors, and any issues that occur will be thrown as exceptions.
            // If C1 and C2 are used correctly, P should not fail except in very exceptional cases.
            OrderProposal order = OpenActiveSerializer.Deserialize<OrderProposal>(orderJson);
            if (order == null || order.GetType() != typeof(OrderProposal))
            {
                throw new OpenBookingException(new UnexpectedOrderTypeError(), "OrderProposal is required for P");
            }
            var (orderId, sellerIdComponents, seller) = ConstructIdsFromRequest(clientId, sellerId, uuid, OrderType.OrderProposal);
            return ResponseContent.OpenBookingResponse(OpenActiveSerializer.Serialize(ProcessFlowRequest(ValidateFlowRequest<OrderProposal>(orderId, sellerIdComponents, seller, FlowStage.P, order), order)), HttpStatusCode.OK);
        }

        private SellerIdComponents GetSellerIdComponentsFromApiKey(Uri sellerId)
        {
            // Return empty SellerIdComponents in Single Seller mode, as it is not required in the API Key
            if (settings.HasSingleSeller == true) return new SellerIdComponents();

            var sellerIdComponents = settings.SellerIdTemplate.GetIdComponents(sellerId);
            if (sellerIdComponents == null) throw new OpenBookingException(new InvalidAPITokenError());
            return sellerIdComponents;
        }

        public ResponseContent DeleteOrder(string clientId, Uri sellerId, string uuid)
        {
            var result = ProcessOrderDeletion(new OrderIdComponents { ClientId = clientId, OrderType = OrderType.Order, uuid = uuid }, GetSellerIdComponentsFromApiKey(sellerId));
            switch (result)
            {
                case DeleteOrderResult.OrderSuccessfullyDeleted:
                    return ResponseContent.OpenBookingNoContentResponse();
                case DeleteOrderResult.OrderDidNotExist:
                    throw new OpenBookingException(new UnknownOrderError());
                default:
                    throw new OpenBookingException(new OpenBookingError(), $"Unexpected DeleteOrderResult: {result}");
            }
        }

        protected abstract DeleteOrderResult ProcessOrderDeletion(OrderIdComponents orderId, SellerIdComponents sellerId);

        public ResponseContent DeleteOrderQuote(string clientId, Uri sellerId, string uuid)
        {
            ProcessOrderQuoteDeletion(new OrderIdComponents { ClientId = clientId, OrderType = OrderType.OrderQuote, uuid = uuid }, GetSellerIdComponentsFromApiKey(sellerId));
            return ResponseContent.OpenBookingNoContentResponse();
        }

        protected abstract void ProcessOrderQuoteDeletion(OrderIdComponents orderId, SellerIdComponents sellerId);

        public ResponseContent ProcessOrderUpdate(string clientId, Uri sellerId, string uuid, string orderJson)
        {
            Order order = OpenActiveSerializer.Deserialize<Order>(orderJson);
            SellerIdComponents sellerIdComponents = GetSellerIdComponentsFromApiKey(sellerId);

            if (order == null || order.GetType() != typeof(Order))
            {
                throw new OpenBookingException(new UnexpectedOrderTypeError(), "Order is required for Order Cancellation");
            }

            // Check for PatchContainsExcessiveProperties
            Order orderWithOnlyAllowedProperties = new Order
            {
                OrderedItem = order.OrderedItem.Select(x => new OrderItem { Id = x.Id, OrderItemStatus = x.OrderItemStatus }).ToList()
            };
            if (OpenActiveSerializer.Serialize<Order>(order) != OpenActiveSerializer.Serialize<Order>(orderWithOnlyAllowedProperties)) {
                throw new OpenBookingException(new PatchContainsExcessivePropertiesError());
            }

            // Check for PatchNotAllowedOnProperty
            if (!order.OrderedItem.TrueForAll(x => x.OrderItemStatus == OrderItemStatus.CustomerCancelled))
            {
                throw new OpenBookingException(new PatchNotAllowedOnPropertyError(), "Only 'https://openactive.io/CustomerCancelled' is permitted for this property.");
            }

            var orderItemIds = order.OrderedItem.Select(x => settings.OrderIdTemplate.GetOrderItemIdComponents(clientId, x.Id)).ToList();

            // Check for mismatching UUIDs
            if (!orderItemIds.TrueForAll(x => x != null))
            {
                throw new OpenBookingException(new OrderItemIdInvalidError());
            }

            // Check for mismatching UUIDs
            if (!orderItemIds.TrueForAll(x => x.OrderType == OrderType.Order && x.uuid == uuid))
            {
                throw new OpenBookingException(new OrderItemNotWithinOrderError());
            }

            ProcessCustomerCancellation(new OrderIdComponents { ClientId = clientId, OrderType = OrderType.Order, uuid = uuid }, sellerIdComponents, settings.OrderIdTemplate, orderItemIds);

            return ResponseContent.OpenBookingNoContentResponse();
        }

        public abstract void ProcessCustomerCancellation(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds);


        public ResponseContent ProcessOrderProposalUpdate(string clientId, Uri sellerId, string uuid, string orderProposalJson)
        {
            OrderProposal orderProposal = OpenActiveSerializer.Deserialize<OrderProposal>(orderProposalJson);
            SellerIdComponents sellerIdComponents = GetSellerIdComponentsFromApiKey(sellerId);

            if (orderProposal == null || orderProposal.GetType() != typeof(Order))
            {
                throw new OpenBookingException(new UnexpectedOrderTypeError(), "OrderProposal is required for Order Cancellation");
            }

            // Check for PatchContainsExcessiveProperties
            OrderProposal orderProposalWithOnlyAllowedProperties = new OrderProposal
            {
                OrderProposalStatus = orderProposal.OrderProposalStatus,
                OrderCustomerNote = orderProposal.OrderCustomerNote
            };
            if (OpenActiveSerializer.Serialize<OrderProposal>(orderProposal) != OpenActiveSerializer.Serialize<OrderProposal>(orderProposalWithOnlyAllowedProperties))
            {
                throw new OpenBookingException(new PatchContainsExcessivePropertiesError());
            }

            // Check for PatchNotAllowedOnProperty
            if (orderProposal.OrderProposalStatus != OrderProposalStatus.CustomerRejected)
            {
                throw new OpenBookingException(new PatchNotAllowedOnPropertyError(), "Only 'https://openactive.io/CustomerRejected' is permitted for this property.");
            }

            ProcessOrderProposalCustomerRejection(new OrderIdComponents { ClientId = clientId, OrderType = OrderType.OrderProposal, uuid = uuid }, sellerIdComponents, settings.OrderIdTemplate);

            return ResponseContent.OpenBookingNoContentResponse();
        }

        public abstract void ProcessOrderProposalCustomerRejection(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate);


        ResponseContent IBookingEngine.InsertTestOpportunity(string testDatasetIdentifier, string eventJson)
        {
            Event genericEvent = OpenActiveSerializer.Deserialize<Event>(eventJson);

            // Note opportunityType is required here to facilitate routing to the correct store to handle the request
            OpportunityType? opportunityType = null;
            ILegalEntity seller = null;

            switch (genericEvent) {
                case ScheduledSession scheduledSession:
                    switch (scheduledSession.SuperEvent.Value)
                    {
                        case SessionSeries sessionSeries:
                            opportunityType = OpportunityType.ScheduledSession;
                            seller = sessionSeries.Organizer;
                            break;
                        default:
                           throw new OpenBookingException(new OpenBookingError(), "ScheduledSession must have superEvent of SessionSeries");
                    }
                    break;
                case Slot slot:
                    switch (slot.FacilityUse.Value)
                    {
                        case IndividualFacilityUse individualFacilityUse:
                            opportunityType = OpportunityType.IndividualFacilityUseSlot;
                            seller = individualFacilityUse.Provider;
                            break;
                        case FacilityUse facilityUse:
                            opportunityType = OpportunityType.FacilityUseSlot;
                            seller = facilityUse.Provider;
                            break;
                        default:
                            throw new OpenBookingException(new OpenBookingError(), "Slot must have facilityUse of FacilityUse or IndividualFacilityUse");
                    }
                    break;
                case CourseInstance courseInstance:
                    opportunityType = OpportunityType.CourseInstance;
                    seller = courseInstance.Organizer;
                    break;
                case HeadlineEvent headlineEvent:
                    opportunityType = OpportunityType.HeadlineEvent;
                    seller = headlineEvent.Organizer;
                    break;
                case OnDemandEvent onDemandEvent:
                    switch (onDemandEvent.SuperEvent)
                    {
                        case EventSeries eventSeries:
                            opportunityType = OpportunityType.OnDemandEvent;
                            seller = eventSeries.Organizer;
                            break;
                        case null:
                            opportunityType = OpportunityType.OnDemandEvent;
                            seller = onDemandEvent.Organizer;
                            break;
                        default:
                            throw new OpenBookingException(new OpenBookingError(), "OnDemandEvent has unrecognised @type of superEvent");
                    }
                    break;
                case Event @event:
                    switch (@event.SuperEvent) {
                        case HeadlineEvent headlineEvent:
                            opportunityType = OpportunityType.HeadlineEventSubEvent;
                            seller = headlineEvent.Organizer;
                            break;
                        case CourseInstance courseInstance:
                            opportunityType = OpportunityType.CourseInstanceSubEvent;
                            seller = courseInstance.Organizer;
                            break;
                        case EventSeries eventSeries:
                            opportunityType = OpportunityType.Event;
                            seller = eventSeries.Organizer;
                            break;
                        case null:
                            opportunityType = OpportunityType.Event;
                            seller = @event.Organizer;
                            break;
                        default:
                            throw new OpenBookingException(new OpenBookingError(), "Event has unrecognised @type of superEvent");
                    }
                    break;
                default:
                    throw new OpenBookingException(new OpenBookingError(), "Only bookable opportunities are permitted in the test interface");

                    // TODO: add this error class to the library
            }

            if (!genericEvent.TestOpportunityCriteria.HasValue)
            {
                throw new OpenBookingException(new OpenBookingError(), "test:testOpportunityCriteria must be supplied.");
            }

            if (seller?.Id == null) throw new OpenBookingException(new SellerMismatchError(), "No seller ID was specified");
            var sellerIdComponents = settings.SellerIdTemplate.GetIdComponents(seller.Id);
            if (sellerIdComponents == null) throw new OpenBookingException(new SellerMismatchError(), "Seller ID format was invalid");

            // Returns a matching Event subclass that will only include "@type" and "@id" properties
            var createdEvent = this.InsertTestOpportunity(testDatasetIdentifier, opportunityType.Value, genericEvent.TestOpportunityCriteria.Value, sellerIdComponents);

            if (createdEvent.Type != genericEvent.Type)
            {
                throw new OpenBookingException(new OpenBookingError(), "Type of created test Event does not match type of requested Event");
            }

            return ResponseContent.OpenBookingResponse(OpenActiveSerializer.Serialize(createdEvent), HttpStatusCode.OK);
        }

        protected abstract Event InsertTestOpportunity(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, SellerIdComponents seller);

        ResponseContent IBookingEngine.DeleteTestDataset(string testDatasetIdentifier)
        {
            this.DeleteTestDataset(testDatasetIdentifier);
            
            return ResponseContent.OpenBookingNoContentResponse();
        }

        protected abstract void DeleteTestDataset(string testDatasetIdentifier);

        ResponseContent IBookingEngine.TriggerTestAction(string actionJson)
        {
            OpenBookingSimulateAction action = OpenActiveSerializer.Deserialize<OpenBookingSimulateAction>(actionJson);

            if (action == null)
            {
                throw new OpenBookingException(new OpenBookingError(), "Invalid type specified. Type must subclass OpenBookingSimulateAction.");
            }

            if (!action.Object.HasValue || ((Schema.NET.JsonLdObject)action.Object.Value).Id == null)
            {
                throw new OpenBookingException(new OpenBookingError(), "Invalid OpenBookingSimulateAction object specified.");
            }

            this.TriggerTestAction(action, settings.OrderIdTemplate);

            return ResponseContent.OpenBookingNoContentResponse();
        }

        protected abstract void TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdTemplate orderIdTemplate);

        private (OrderIdComponents orderId, SellerIdComponents sellerIdComponents, ILegalEntity seller) ConstructIdsFromRequest(string clientId, Uri authenticationSellerId, string uuid, OrderType orderType)
        {
            var orderId = new OrderIdComponents
            {
                ClientId = clientId,
                uuid = uuid,
                OrderType = orderType
            };

            // TODO: Add more request validation rules here

            SellerIdComponents sellerIdComponents = GetSellerIdComponentsFromApiKey(authenticationSellerId);

            ILegalEntity seller = settings.SellerStore.GetSellerById(sellerIdComponents);

            if (seller == null)
            {
                throw new OpenBookingException(new SellerNotFoundError());
            }

            return (orderId, sellerIdComponents, seller);
        }

        //TODO: Should we move Seller into the Abstract level? Perhaps too much complexity
        protected BookingFlowContext ValidateFlowRequest<O>(OrderIdComponents orderId, SellerIdComponents sellerIdComponents, ILegalEntity seller, FlowStage stage, O order) where O : Order, new()
        {
            if (order?.Seller?.Id != null && seller?.Id != order?.Seller?.Id)
            {
                throw new OpenBookingException(new SellerMismatchError());
            }

            // Check that taxMode is set in Seller
            if (!(seller?.TaxMode == TaxMode.TaxGross || seller?.TaxMode == TaxMode.TaxNet))
            {
                throw new EngineConfigurationException("taxMode must always be set in the Seller");
            }

            // Default to BusinessToConsumer if no customer provided
            TaxPayeeRelationship taxPayeeRelationship =
                order.Customer == null ? 
                    TaxPayeeRelationship.BusinessToConsumer :
                    order.BrokerRole == BrokerType.ResellerBroker || order.Customer.IsOrganization
                        ? TaxPayeeRelationship.BusinessToBusiness : TaxPayeeRelationship.BusinessToConsumer;

            if (order.BrokerRole == null)
            {
                throw new OpenBookingException(new IncompleteBrokerDetailsError());
            }

            var payer = order.BrokerRole == BrokerType.ResellerBroker ? order.Broker : order.Customer;

            return new BookingFlowContext {
                Stage = stage,
                OrderId = orderId,
                OrderIdTemplate = settings.OrderIdTemplate,
                Seller = seller,
                SellerId = sellerIdComponents,
                TaxPayeeRelationship = taxPayeeRelationship,
                Payer = payer 
            };
        }

        public abstract TOrder ProcessFlowRequest<TOrder>(BookingFlowContext request, TOrder order) where TOrder : Order, new();

        public abstract Order ProcessOrderCreationFromOrderProposal(OrderIdComponents orderId, OrderIdTemplate orderIdTemplate, ILegalEntity seller, SellerIdComponents sellerId, Order order);

    }
}
