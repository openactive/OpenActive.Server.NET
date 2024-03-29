﻿using OpenActive.DatasetSite.NET;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.StoreBooking;
using System;
using System.Collections.Generic;

namespace BookingSystem
{
    public static class EngineConfig
    {
        public static StoreBookingEngine CreateStoreBookingEngine(AppSettings appSettings, FakeBookingSystem fakeBookingSystem)
        {
            var facilityBookablePaidIdTemplate = appSettings.FeatureFlags.FacilityUseHasSlots ?
                new BookablePairIdTemplate<FacilityOpportunity>(
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.FacilityUseSlot,
                                AssignedFeed = OpportunityType.FacilityUseSlot,
                                OpportunityIdTemplate = "{+BaseUrl}/facility-uses/{FacilityUseId}/slots/{SlotId}",
                                OfferIdTemplate = "{+BaseUrl}/facility-uses/{FacilityUseId}/slots/{SlotId}#/offers/{OfferId}",
                                Bookable = true
                            },
                            // Parent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.FacilityUse,
                                AssignedFeed = OpportunityType.FacilityUse,
                                OpportunityIdTemplate = "{+BaseUrl}/facility-uses/{FacilityUseId}"
                            })
                :
                new BookablePairIdTemplate<FacilityOpportunity>(
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.IndividualFacilityUseSlot,
                                AssignedFeed = OpportunityType.IndividualFacilityUseSlot,
                                OpportunityIdTemplate = "{+BaseUrl}/facility-uses/{FacilityUseId}/individual-facility-uses/{IndividualFacilityUseId}/slots/{SlotId}",
                                OfferIdTemplate = "{+BaseUrl}/facility-uses/{FacilityUseId}/individual-facility-uses/{IndividualFacilityUseId}/slots/{SlotId}#/offers/{OfferId}",
                                Bookable = true
                            },
                            // Parent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.IndividualFacilityUse,
                                AssignedFeed = OpportunityType.FacilityUse,
                                OpportunityIdTemplate = "{+BaseUrl}/facility-uses/{FacilityUseId}/individual-facility-uses/{IndividualFacilityUseId}"
                            },
                            // Grandparent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.FacilityUse,
                                AssignedFeed = OpportunityType.FacilityUse,
                                OpportunityIdTemplate = "{+BaseUrl}/facility-uses/{FacilityUseId}"
                            })
                ;

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
                                OpportunityIdTemplate = "{+BaseUrl}/scheduled-sessions/{SessionSeriesId}/events/{ScheduledSessionId}",
                                OfferIdTemplate =       "{+BaseUrl}/scheduled-sessions/{SessionSeriesId}/events/{ScheduledSessionId}#/offers/{OfferId}",
                                Bookable = true
                            },
                            // Parent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.SessionSeries,
                                AssignedFeed = OpportunityType.SessionSeries,
                                OpportunityIdTemplate = "{+BaseUrl}/session-series/{SessionSeriesId}",
                                OfferIdTemplate =       "{+BaseUrl}/session-series/{SessionSeriesId}#/offers/{OfferId}",
                                Bookable = false
                            }),

                        facilityBookablePaidIdTemplate,
                        /*
                        new BookablePairIdTemplate<ScheduledSessionOpportunity>(
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.HeadlineEventSubEvent,
                                AssignedFeed = OpportunityType.HeadlineEvent,
                                OpportunityUriTemplate = "{+BaseUrl}/headline-events/{HeadlineEventId}/events/{EventId}",
                                OfferUriTemplate =       "{+BaseUrl}/headline-events/{HeadlineEventId}/events/{EventId}#/offers/{OfferId}",
                                Bookable = true
                            },
                            // Parent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.HeadlineEvent,
                                AssignedFeed = OpportunityType.HeadlineEvent,
                                OpportunityUriTemplate = "{+BaseUrl}/headline-events/{HeadlineEventId}",
                                OfferUriTemplate =       "{+BaseUrl}/headline-events/{HeadlineEventId}#/offers/{OfferId}"
                            }),

                            new BookablePairIdTemplate<ScheduledSessionOpportunity>(
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.CourseInstanceSubEvent,
                                AssignedFeed = OpportunityType.CourseInstance,
                                OpportunityUriTemplate = "{+BaseUrl}/courses/{CourseId}/events/{EventId}",
                                OfferUriTemplate =       "{+BaseUrl}/courses/{CourseId}/events/{EventId}#/offers/{OfferId}"
                            },
                            // Parent
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.CourseInstance,
                                AssignedFeed = OpportunityType.CourseInstance,
                                OpportunityUriTemplate = "{+BaseUrl}/courses/{CourseId}",
                                OfferUriTemplate =       "{+BaseUrl}/courses/{CourseId}#/offers/{OfferId}",
                                Bookable = true
                            }),

                        new BookablePairIdTemplate<ScheduledSessionOpportunity>(
                            // Opportunity
                            new OpportunityIdConfiguration
                            {
                                OpportunityType = OpportunityType.Event,
                                AssignedFeed = OpportunityType.Event,
                                OpportunityUriTemplate = "{+BaseUrl}/events/{EventId}",
                                OfferUriTemplate =       "{+BaseUrl}/events/{EventId}#/offers/{OfferId}",
                                Bookable = true
                            })*/
                    
                    },

                    JsonLdIdBaseUrl = new Uri($"{appSettings.ApplicationHostBaseUrl}/api/identifiers"),

                    /*
                    // Multiple Seller Mode
                    SellerStore = new AcmeSellerStore(),
                    SellerIdTemplate = new SingleIdTemplate<SimpleIdComponents>(
                        "{+BaseUrl}/sellers/{IdLong}"
                        ),
                    */

                    /*
                    // Single Seller Mode
                    SellerStore = new AcmeSellerStore(),
                    SellerIdTemplate = new SingleIdTemplate<SimpleIdComponents>(
                        "{+BaseUrl}/seller"
                        ),
                    HasSingleSeller = true,
                    */

                    // Reference implementation is configurable to allow both modes to be tested
                    SellerStore = new AcmeSellerStore(appSettings.FeatureFlags.SingleSeller, fakeBookingSystem),
                    SellerIdTemplate = appSettings.FeatureFlags.SingleSeller ?
                        new SingleIdTemplate<SimpleIdComponents>(
                            "{+BaseUrl}/seller"
                        ) :
                        new SingleIdTemplate<SimpleIdComponents>(
                            "{+BaseUrl}/sellers/{IdLong}"
                        ),
                    HasSingleSeller = appSettings.FeatureFlags.SingleSeller,

                    // IdempotencyStore used for storing the response to Order Creation B/P requests
                    IdempotencyStore = new AcmeIdempotencyStore(),

                    OpenDataFeeds = new Dictionary<OpportunityType, IOpportunityDataRpdeFeedGenerator> {
                        {
                            OpportunityType.ScheduledSession, new AcmeScheduledSessionRpdeGenerator(fakeBookingSystem)
                        },
                        {
                            OpportunityType.SessionSeries, new AcmeSessionSeriesRpdeGenerator(appSettings, fakeBookingSystem)
                        },
                        {
                            OpportunityType.FacilityUse, new AcmeFacilityUseRpdeGenerator(appSettings, fakeBookingSystem)
                        },
                        {
                            appSettings.FeatureFlags.FacilityUseHasSlots ? OpportunityType.FacilityUseSlot : OpportunityType.IndividualFacilityUseSlot, new AcmeFacilityUseSlotRpdeGenerator(appSettings,fakeBookingSystem)
                        }
                    },

                    // Note unlike other IDs this one needs to be resolvable
                    // and must match the controller configuration
                    OrderIdTemplate = new OrderIdTemplate(
                        "{+BaseUrl}/{OrderType}/{uuid}",
                        "{+BaseUrl}/{OrderType}/{uuid}#/orderedItems/{OrderItemIdLong}"),

                    OrdersFeedGenerator = new AcmeOrdersFeedRpdeGenerator(appSettings, fakeBookingSystem),
                    OrderProposalsFeedGenerator = new AcmeOrderProposalsFeedRpdeGenerator(appSettings, fakeBookingSystem)
                },
                new DatasetSiteGeneratorSettings
                {
                    // QUESTION: Do the Base URLs need to come from config, or should they be detected from the request?
                    OpenDataFeedBaseUrl = $"{appSettings.ApplicationHostBaseUrl}/feeds".ParseUrlOrNull(),
                    OpenBookingAPIAuthenticationAuthorityUrl = appSettings.FeatureFlags.EnableTokenAuth ? appSettings.OpenIdIssuerUrl.ParseUrlOrNull() : null,
                    DatasetSiteUrl = $"{appSettings.ApplicationHostBaseUrl}/openactive/".ParseUrlOrNull(),
                    DatasetDiscussionUrl = "https://github.com/openactive/OpenActive.Server.NET/issues".ParseUrlOrNull(),
                    DatasetDocumentationUrl = "https://developer.openactive.io/".ParseUrlOrNull(),
                    DatasetLanguages = new List<string> { "en-GB" },
                    OrganisationName = "Example",
                    OrganisationUrl = "https://www.example.com/".ParseUrlOrNull(),
                    OrganisationLegalEntity = "Example",
                    OrganisationPlainTextDescription = "The Reference Implementation provides an example of an full conformant implementation of the OpenActive specifications.",
                    OrganisationLogoUrl = $"{appSettings.ApplicationHostBaseUrl}/images/placeholder-logo.png".ParseUrlOrNull(),
                    OrganisationEmail = "hello@example.com",
                    PlatformName = appSettings.FeatureFlags.CustomBuiltSystem ? null : "OpenActive Reference Implementation",
                    PlatformUrl = appSettings.FeatureFlags.CustomBuiltSystem ? null : "https://tutorials.openactive.io/open-booking-sdk/".ParseUrlOrNull(),
                    PlatformVersion = appSettings.FeatureFlags.CustomBuiltSystem ? null : "1.0",
                    BackgroundImageUrl = $"{appSettings.ApplicationHostBaseUrl}/images/placeholder-dataset-site-background.jpg".ParseUrlOrNull(),
                    DateFirstPublished = new DateTimeOffset(new DateTime(2019, 01, 14)),
                    OpenBookingAPIBaseUrl = $"{appSettings.ApplicationHostBaseUrl}/api/openbooking".ParseUrlOrNull(),
                    OpenBookingAPIRegistrationUrl = new Uri("https://example.com/api-landing-page"),
                    OpenBookingAPITermsOfServiceUrl = new Uri("https://example.com/api-terms-page"),
                    TestSuiteCertificateUrl = new Uri("https://certificates.reference-implementation.openactive.io/examples/all-features/controlled/")
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
                        Name = o?.Name,
                        Url = o?.Url,
                        Telephone = o?.Telephone
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
                    PaymentSupportedFields = o =>
                        appSettings.FeatureFlags.PaymentReconciliationDetailValidation ?
                            new Payment
                            {
                                Name = o.Name,
                                Identifier = o.Identifier,
                                AccountId = o.AccountId,
                                PaymentProviderId = o.PaymentProviderId
                            } :
                            new Payment
                            {
                                Identifier = o.Identifier
                            },
                    // List of _bookable_ opportunity types and which store to route to for each
                    OpportunityStoreRouting = new Dictionary<IOpportunityStore, List<OpportunityType>> {
                        {
                            new SessionStore(appSettings, fakeBookingSystem), new List<OpportunityType> { OpportunityType.ScheduledSession }
                        },
                        {
                            new FacilityStore(appSettings, fakeBookingSystem), new List<OpportunityType> { appSettings.FeatureFlags.FacilityUseHasSlots ? OpportunityType.FacilityUseSlot : OpportunityType.IndividualFacilityUseSlot }
                        }
                    },
                    OrderStore = new AcmeOrderStore(appSettings, fakeBookingSystem),
                    BusinessToBusinessTaxCalculation = appSettings.Payment.TaxCalculationB2B,
                    BusinessToConsumerTaxCalculation = appSettings.Payment.TaxCalculationB2C,
                    EnforceSyncWithinOrderTransactions = false,
                    PrepaymentAlwaysRequired = appSettings.FeatureFlags.PrepaymentAlwaysRequired
                });
        }
    }
}
