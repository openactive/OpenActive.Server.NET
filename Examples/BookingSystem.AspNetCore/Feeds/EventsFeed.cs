using OpenActive.DatasetSite.NET;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.NET.Rpde.Version1;
using OpenActive.Server.NET.OpenBookingHelper;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingSystem
{
    public class AcmeEventRpdeGenerator : RpdeFeedModifiedTimestampAndIdLong<EventOpportunity, Event>
    {
        private readonly bool _useSingleSellerMode;

        // Example constructor that can set state from EngineConfig
        public AcmeEventRpdeGenerator(bool useSingleSellerMode)
        {
            this._useSingleSellerMode = useSingleSellerMode;
        }

        protected async override Task<List<RpdeItem<Event>>> GetRpdeItems(long? afterTimestamp, long? afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var q = db.From<ClassTable>()
                .Join<SellerTable>()
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.Id)
                .Where(x => x.IsEvent) // Filters for Events only
                .Where(x => !afterTimestamp.HasValue && !afterId.HasValue ||
                    x.Modified > afterTimestamp ||
                    x.Modified == afterTimestamp && x.Id > afterId &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(RpdePageSize);

                var query = db
                    .SelectMulti<ClassTable, SellerTable>(q)
                    .Select(result => new RpdeItem<Event>
                    {
                        Kind = RpdeKind.Event,
                        Id = result.Item1.Id,
                        Modified = result.Item1.Modified,
                        State = result.Item1.Deleted ? RpdeState.Deleted : RpdeState.Updated,
                        Data = result.Item1.Deleted ? null : new Event
                        {
                            // QUESTION: Should the this.IdTemplate and this.BaseUrl be passed in each time rather than set on
                            // the parent class? Current thinking is it's more extensible on parent class as function signature remains
                            // constant as power of configuration through underlying class grows (i.e. as new properties are added)
                            Id = RenderOpportunityId(new EventOpportunity
                            {
                                OpportunityType = OpportunityType.Event,
                                EventId = result.Item1.Id,
                            }),
                            Name = result.Item1.Title,
                            EventAttendanceMode = FeedGeneratorHelper.MapAttendanceMode(result.Item1.AttendanceMode),
                            Organizer = _useSingleSellerMode ? new Organization
                            {
                                Id = RenderSingleSellerId(),
                                Name = "Test Seller",
                                TaxMode = TaxMode.TaxGross,
                                TermsOfService = new List<Terms>
                                {
                                    new PrivacyPolicy
                                    {
                                        Name = "Privacy Policy",
                                        Url = new Uri("https://example.com/privacy.html"),
                                        RequiresExplicitConsent = false
                                    }
                                },
                                IsOpenBookingAllowed = true,
                            } : result.Item2.IsIndividual ? (ILegalEntity)new Person
                            {
                                Id = RenderSellerId(new SellerIdComponents { SellerIdLong = result.Item2.Id }),
                                Name = result.Item2.Name,
                                TaxMode = result.Item2.IsTaxGross ? TaxMode.TaxGross : TaxMode.TaxNet,
                                IsOpenBookingAllowed = true,
                            } : (ILegalEntity)new Organization
                            {
                                Id = RenderSellerId(new SellerIdComponents { SellerIdLong = result.Item2.Id }),
                                Name = result.Item2.Name,
                                TaxMode = result.Item2.IsTaxGross ? TaxMode.TaxGross : TaxMode.TaxNet,
                                TermsOfService = new List<Terms>
                                {
                                    new PrivacyPolicy
                                    {
                                        Name = "Privacy Policy",
                                        Url = new Uri("https://example.com/privacy.html"),
                                        RequiresExplicitConsent = false
                                    }
                                },
                                IsOpenBookingAllowed = true,
                            },
                            Offers = new List<Offer> { new Offer
                                {
                                    Id = RenderOfferId(new EventOpportunity
                                    {
                                        OpportunityType = OpportunityType.Event,
                                        EventId = result.Item1.Id,
                                        OfferId = 0
                                    }),
                                    Price = result.Item1.Price,
                                    PriceCurrency = "GBP",
                                    OpenBookingFlowRequirement = FeedGeneratorHelper.OpenBookingFlowRequirement(
                                        result.Item1.RequiresApproval,
                                        result.Item1.RequiresAttendeeValidation,
                                        result.Item1.RequiresAdditionalDetails,
                                        result.Item1.AllowsProposalAmendment),
                                    ValidFromBeforeStartDate = result.Item1.ValidFromBeforeStartDate,
                                    LatestCancellationBeforeStartDate = result.Item1.LatestCancellationBeforeStartDate,
                                    OpenBookingPrepayment = result.Item1.Prepayment.Convert(),
                                    AllowCustomerCancellationFullRefund = result.Item1.AllowCustomerCancellationFullRefund
                                }
                            },
                            Location = result.Item1.AttendanceMode == AttendanceMode.Online ? null : new Place
                            {
                                Name = "Fake Pond",
                                Address = new PostalAddress
                                {
                                    StreetAddress = "1 Fake Park",
                                    AddressLocality = "Another town",
                                    AddressRegion = "Oxfordshire",
                                    PostalCode = "OX1 1AA",
                                    AddressCountry = "GB"
                                },
                                Geo = new GeoCoordinates
                                {
                                    Latitude = result.Item1.LocationLat,
                                    Longitude = result.Item1.LocationLng,
                                }
                            },
                            AffiliatedLocation = result.Item1.AttendanceMode == AttendanceMode.Offline ? null : new Place
                            {
                                Name = "Fake Pond",
                                Address = new PostalAddress
                                {
                                    StreetAddress = "1 Fake Park",
                                    AddressLocality = "Another town",
                                    AddressRegion = "Oxfordshire",
                                    PostalCode = "OX1 1AA",
                                    AddressCountry = "GB"
                                },
                                Geo = new GeoCoordinates
                                {
                                    Latitude = result.Item1.LocationLat,
                                    Longitude = result.Item1.LocationLng,
                                }
                            },
                            Url = new Uri("https://www.example.com/a-session-age"),
                            Activity = new List<Concept>
                            {
                                new Concept
                                {
                                    Id = new Uri("https://openactive.io/activity-list#c07d63a0-8eb9-4602-8bcc-23be6deb8f83"),
                                    PrefLabel = "Jet Skiing",
                                    InScheme = new Uri("https://openactive.io/activity-list")
                                }
                            },
                            StartDate = (DateTimeOffset)result.Item1.Start,
                            EndDate = (DateTimeOffset)result.Item1.End,
                            Duration = result.Item1.End - result.Item1.Start,
                            RemainingAttendeeCapacity = result.Item1.RemainingSpaces - result.Item1.LeasedSpaces,
                            MaximumAttendeeCapacity = result.Item1.TotalSpaces
                        }
                    });
                return query.ToList();
            }
        }
    }
}
