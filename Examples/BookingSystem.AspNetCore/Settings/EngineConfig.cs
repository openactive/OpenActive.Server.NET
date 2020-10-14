using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.StoreBooking;
using System;
using System.Collections.Generic;

namespace BookingSystem
{
    public static class EngineConfig
    {
        public static StoreBookingEngine CreateStoreBookingEngine(string baseUrl, bool UseSingleSellerMode)
        {
            return new StoreBookingEngine(
                new BookingEngineSettings
                {
                    // This assigns the ID pattern used for each ID
                    IdConfiguration = new List<IBookablePairIdTemplate> {
                        // Note that ScheduledSession is the only opportunity type that allows offer inheritance  
                        new BookablePairIdTemplateWithOfferInheritance<SessionOpportunity> (
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.ScheduledSession,
                                AssignedFeed = OpportunityType.ScheduledSession,
                                OpportunityIdTemplate = "{+BaseUrl}scheduled-sessions/{SessionSeriesId}/events/{ScheduledSessionId}",
                                OfferIdTemplate =       "{+BaseUrl}scheduled-sessions/{SessionSeriesId}/events/{ScheduledSessionId}#/offers/{OfferId}",
                                Bookable = true
                            },
                            // Parent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.SessionSeries,
                                AssignedFeed = OpportunityType.SessionSeries,
                                OpportunityIdTemplate = "{+BaseUrl}session-series/{SessionSeriesId}",
                                OfferIdTemplate =       "{+BaseUrl}session-series/{SessionSeriesId}#/offers/{OfferId}",
                                Bookable = false
                            }),

                        new BookablePairIdTemplate<FacilityOpportunity> (
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.FacilityUseSlot,
                                AssignedFeed = OpportunityType.FacilityUseSlot,
                                OpportunityIdTemplate = "{+BaseUrl}facility-uses/{FacilityUseId}/facility-use-slots/{SlotId}",
                                OfferIdTemplate =       "{+BaseUrl}facility-uses/{FacilityUseId}/facility-use-slots/{SlotId}#/offers/{OfferId}",
                                Bookable = true
                            },
                            // Parent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.FacilityUse,
                                AssignedFeed = OpportunityType.FacilityUse,
                                OpportunityIdTemplate = "{+BaseUrl}facility-uses/{FacilityUseId}"
                            })/*,

                        new BookablePairIdTemplate<ScheduledSessionOpportunity>(
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.HeadlineEventSubEvent,
                                AssignedFeed = OpportunityType.HeadlineEvent,
                                OpportunityUriTemplate = "{+BaseUrl}headline-events/{HeadlineEventId}/events/{EventId}",
                                OfferUriTemplate =       "{+BaseUrl}headline-events/{HeadlineEventId}/events/{EventId}#/offers/{OfferId}",
                                Bookable = true
                            },
                            // Parent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.HeadlineEvent,
                                AssignedFeed = OpportunityType.HeadlineEvent,
                                OpportunityUriTemplate = "{+BaseUrl}headline-events/{HeadlineEventId}",
                                OfferUriTemplate =       "{+BaseUrl}headline-events/{HeadlineEventId}#/offers/{OfferId}"
                            }),

                            new BookablePairIdTemplate<ScheduledSessionOpportunity>(
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.CourseInstanceSubEvent,
                                AssignedFeed = OpportunityType.CourseInstance,
                                OpportunityUriTemplate = "{+BaseUrl}courses/{CourseId}/events/{EventId}",
                                OfferUriTemplate =       "{+BaseUrl}courses/{CourseId}/events/{EventId}#/offers/{OfferId}"
                            },
                            // Parent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.CourseInstance,
                                AssignedFeed = OpportunityType.CourseInstance,
                                OpportunityUriTemplate = "{+BaseUrl}courses/{CourseId}",
                                OfferUriTemplate =       "{+BaseUrl}courses/{CourseId}#/offers/{OfferId}",
                                Bookable = true
                            }),

                        new BookablePairIdTemplate<ScheduledSessionOpportunity>(
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.Event,
                                AssignedFeed = OpportunityType.Event,
                                OpportunityUriTemplate = "{+BaseUrl}events/{EventId}",
                                OfferUriTemplate =       "{+BaseUrl}events/{EventId}#/offers/{OfferId}",
                                Bookable = true
                            })*/
                    
                    },

                    JsonLdIdBaseUrl = new Uri(baseUrl + "api/identifiers/"),

                    /*
                    // Multiple Seller Mode
                    SellerStore = new AcmeSellerStore(),
                    SellerIdTemplate = new SingleIdTemplate<SellerIdComponents>(
                        "{+BaseUrl}sellers/{SellerIdLong}"
                        ),
                    */

                    /*
                    // Single Seller Mode
                    SellerStore = new AcmeSellerStore(),
                    SellerIdTemplate = new SingleIdTemplate<SellerIdComponents>(
                        "{+BaseUrl}seller"
                        ),
                    HasSingleSeller = true,
                    */

                    // Reference implementation is configurable to allow both modes to be tested
                    SellerStore = new AcmeSellerStore(UseSingleSellerMode),
                    SellerIdTemplate = UseSingleSellerMode ?
                        new SingleIdTemplate<SellerIdComponents>(
                            "{+BaseUrl}seller"
                        ) :
                        new SingleIdTemplate<SellerIdComponents>(
                            "{+BaseUrl}sellers/{SellerIdLong}"
                        ),
                    HasSingleSeller = UseSingleSellerMode,

                    OpenDataFeeds = new Dictionary<OpportunityType, IOpportunityDataRpdeFeedGenerator> {
                        {
                            OpportunityType.ScheduledSession, new AcmeScheduledSessionRpdeGenerator()
                        },
                        {
                            OpportunityType.SessionSeries, new AcmeSessionSeriesRpdeGenerator(UseSingleSellerMode)
                        },
                        {
                            OpportunityType.FacilityUse, new AcmeFacilityUseRpdeGenerator(UseSingleSellerMode)
                        }
                        ,
                        {
                            OpportunityType.FacilityUseSlot, new AcmeFacilityUseSlotRpdeGenerator()
                        }
                    },

                    // Note unlike other IDs this one needs to be resolvable
                    // and must match the controller configuration
                    OrderIdTemplate = new OrderIdTemplate(
                        "{+BaseUrl}{OrderType}/{uuid}",
                        "{+BaseUrl}{OrderType}/{uuid}#/orderedItems/{OrderItemIdLong}"
                        ),

                    OrderFeedGenerator = new AcmeOrdersFeedRpdeGenerator()
                },
                new DatasetSiteGeneratorSettings
                {
                    // QUESTION: Do the Base URLs need to come from config, or should they be detected from the request?
                    OpenDataFeedBaseUrl = (baseUrl + "feeds/").ParseUrlOrNull(),
                    DatasetSiteUrl = (baseUrl + "openactive/").ParseUrlOrNull(),
                    DatasetDiscussionUrl = "https://github.com/openactive/OpenActive.Server.NET/issues".ParseUrlOrNull(),
                    DatasetDocumentationUrl = "https://developer.openactive.io/".ParseUrlOrNull(),
                    DatasetLanguages = new List<string> { "en-GB" },
                    OrganisationName = "Example",
                    OrganisationUrl = "https://www.example.com/".ParseUrlOrNull(),
                    OrganisationLegalEntity = "Example",
                    OrganisationPlainTextDescription = "The Reference Implementation provides an example of an full conformant implementation of the OpenActive specifications.",
                    OrganisationLogoUrl = "http://data.better.org.uk/images/logo.png".ParseUrlOrNull(),
                    OrganisationEmail = "hello@example.com",
                    PlatformName = "OpenActive Reference Implementation",
                    PlatformUrl = "https://tutorials.openactive.io/open-booking-sdk/".ParseUrlOrNull(),
                    PlatformVersion = "1.0",
                    BackgroundImageUrl = "https://images.unsplash.com/photo-1594899756066-46964fff3add?fit=crop&w=1500&q=80".ParseUrlOrNull(),
                    DateFirstPublished = new DateTimeOffset(new DateTime(2019, 01, 14)),
                    OpenBookingAPIBaseUrl = (baseUrl + "api/openbooking/").ParseUrlOrNull(),
                    OpenBookingAPIRegistrationUrl = new Uri("https://example.com/api-landing-page"),
                    OpenBookingAPITermsOfServiceUrl = new Uri("https://example.com/api-terms-page")
                },
                new StoreBookingEngineSettings
                {
                    // A list of the supported fields that are accepted by your system for guest checkout bookings
                    // These are reflected back to the broker
                    // Note that only E-mail address is required, as per Open Booking API spec
                    CustomerPersonSupportedFields = p => new Person
                    {
                        Email = p.Email,
                        GivenName = p.GivenName,
                        FamilyName = p.FamilyName,
                        Telephone = p.Telephone,
                        Identifier = p.Identifier
                    },
                    // A list of the supported fields that are accepted by your system for guest checkout bookings
                    // These are reflected back to the broker
                    // Note that only E-mail address is required, as per Open Booking API spec
                    CustomerOrganizationSupportedFields = o => new Organization
                    {
                        Email = o.Email,
                        Name = o.Name,
                        Telephone = o.Telephone
                    },
                    // A list of the supported fields that are accepted by your system for broker details
                    // These are reflected back to the broker
                    // Note that storage of these details is entirely optional
                    BrokerSupportedFields = o => new Organization
                    {
                        Name = o.Name,
                        Url = o.Url,
                        Telephone = o.Telephone
                    },
                    // Details of your booking system, complete with an customer-facing terms and conditions
                    BookingServiceDetails = new BookingService
                    {
                        Name = "Acme booking system",
                        Url = new Uri("https://example.com"),
                        TermsOfService = new List<Terms>
                        {
                            new PrivacyPolicy
                            {
                                Name = "Privacy Policy",
                                Url = new Uri("https://example.com/privacy.html"),
                                RequiresExplicitConsent = false
                            }
                        }
                    },
                    // A list of the supported fields that are accepted by your system for payment details
                    // These are reflected back to the broker
                    PaymentSupportedFields = o => new Payment
                    {
                        Name = o.Name,
                        Identifier = o.Identifier,
                        AccountId = o.AccountId,
                        PaymentProviderId = o.PaymentProviderId
                    },
                    // List of _bookable_ opportunity types and which store to route to for each
                    OpportunityStoreRouting = new Dictionary<IOpportunityStore, List<OpportunityType>> {
                        {
                            new SessionStore(), new List<OpportunityType> { OpportunityType.ScheduledSession }
                        },
                        {
                            new FacilityStore(), new List<OpportunityType> { OpportunityType.FacilityUseSlot }
                        }
                    },
                    OrderStore = new AcmeOrderStore(),
                });
        }
    }
}
