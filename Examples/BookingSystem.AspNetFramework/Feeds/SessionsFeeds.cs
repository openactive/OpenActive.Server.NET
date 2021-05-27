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
    public class AcmeScheduledSessionRpdeGenerator : RpdeFeedModifiedTimestampAndIdLong<SessionOpportunity, ScheduledSession>
    {
        //public override string FeedPath { get; protected set; } = "example path override";

        protected async override Task<List<RpdeItem<ScheduledSession>>> GetRpdeItems(long? afterTimestamp, long? afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var query = db.Select<OccurrenceTable>()
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.Id)
                .Where(x => !afterTimestamp.HasValue && !afterId.HasValue ||
                    x.Modified > afterTimestamp ||
                    x.Modified == afterTimestamp && x.Id > afterId &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(RpdePageSize)
                .Select(x => new RpdeItem<ScheduledSession>
                {
                    Kind = RpdeKind.ScheduledSession,
                    Id = x.Id,
                    Modified = x.Modified,
                    State = x.Deleted ? RpdeState.Deleted : RpdeState.Updated,
                    Data = x.Deleted ? null : new ScheduledSession
                    {
                        // QUESTION: Should the this.IdTemplate and this.BaseUrl be passed in each time rather than set on
                        // the parent class? Current thinking is it's more extensible on parent class as function signature remains
                        // constant as power of configuration through underlying class grows (i.e. as new properties are added)
                        Id = RenderOpportunityId(new SessionOpportunity
                        {
                            OpportunityType = OpportunityType.ScheduledSession,
                            SessionSeriesId = x.ClassId,
                            ScheduledSessionId = x.Id
                        }),
                        SuperEvent = RenderOpportunityId(new SessionOpportunity
                        {
                            OpportunityType = OpportunityType.SessionSeries,
                            SessionSeriesId = x.ClassId
                        }),
                        StartDate = (DateTimeOffset)x.Start,
                        EndDate = (DateTimeOffset)x.End,
                        Duration = x.End - x.Start,
                        RemainingAttendeeCapacity = x.RemainingSpaces - x.LeasedSpaces,
                        MaximumAttendeeCapacity = x.TotalSpaces
                    }
                });

                return query.ToList();
            }
        }
    }

    public class AcmeSessionSeriesRpdeGenerator : RpdeFeedModifiedTimestampAndIdLong<SessionOpportunity, SessionSeries>
    {
        private readonly bool _useSingleSellerMode;

        // Example constructor that can set state from EngineConfig
        public AcmeSessionSeriesRpdeGenerator(bool useSingleSellerMode)
        {
            this._useSingleSellerMode = useSingleSellerMode;
        }

        protected async override Task<List<RpdeItem<SessionSeries>>> GetRpdeItems(long? afterTimestamp, long? afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var q = db.From<ClassTable>()
                .Join<SellerTable>()
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.Id)
                .Where(x => !x.IsEvent) // Filters for SessionSeries only (as opposed to Events)
                .Where(x => !afterTimestamp.HasValue && !afterId.HasValue ||
                    x.Modified > afterTimestamp ||
                    x.Modified == afterTimestamp && x.Id > afterId &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(RpdePageSize);

                var query = db
                    .SelectMulti<ClassTable, SellerTable>(q)
                    .Select(result => new RpdeItem<SessionSeries>
                    {
                        Kind = RpdeKind.SessionSeries,
                        Id = result.Item1.Id,
                        Modified = result.Item1.Modified,
                        State = result.Item1.Deleted ? RpdeState.Deleted : RpdeState.Updated,
                        Data = result.Item1.Deleted ? null : new SessionSeries
                        {
                            // QUESTION: Should the this.IdTemplate and this.BaseUrl be passed in each time rather than set on
                            // the parent class? Current thinking is it's more extensible on parent class as function signature remains
                            // constant as power of configuration through underlying class grows (i.e. as new properties are added)
                            Id = RenderOpportunityId(new SessionOpportunity
                            {
                                OpportunityType = OpportunityType.SessionSeries,
                                SessionSeriesId = result.Item1.Id
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
                                    Id = RenderOfferId(new SessionOpportunity
                                    {
                                        OfferOpportunityType = OpportunityType.SessionSeries,
                                        SessionSeriesId = result.Item1.Id,
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
                            EventSchedule = HydatePartialSchedules(result.Item1.PartialScheduleDay, result.Item1.PartialScheduleTime, result.Item1.PartialScheduleDuration),
                        }
                    }); ;

                return query.ToList();
            }
        }

        private List<Schedule> HydatePartialSchedules(DayOfWeek partialScheduleDayOfWeek, DateTimeOffset partialScheduleStartTime, TimeSpan partialScheduleDuration)
        {
            var schedules = new List<Schedule>()
            {
                new PartialSchedule()
                {
                    ByDay = new List<string>() { $"https://schema.org/{partialScheduleDayOfWeek}" },
                    StartTime = partialScheduleStartTime,
                    Duration = partialScheduleDuration,
                    EndTime = partialScheduleStartTime.Add(partialScheduleDuration),
                    ScheduleTimezone = "Europe/London",
                }
            };

            return schedules;
        }
    }
}
