using OpenActive.DatasetSite.NET;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.NET.Rpde.Version1;
using OpenActive.Server.NET.OpenBookingHelper;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BookingSystem
{
    public class AcmeScheduledSessionRPDEGenerator : RPDEFeedModifiedTimestampAndIDLong<SessionOpportunity, ScheduledSession>
    {
        //public override string FeedPath { get; protected set; } = "example path override";

        protected override List<RpdeItem<ScheduledSession>> GetRPDEItems(long? afterTimestamp, long? afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var query = db.Select<OccurrenceTable>()
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.Id)
                .Where(x => !afterTimestamp.HasValue && !afterId.HasValue ||
                    x.Modified > afterTimestamp ||
                    (x.Modified == afterTimestamp && x.Id > afterId) &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(this.RPDEPageSize)
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
                        Id = this.RenderOpportunityId(new SessionOpportunity
                        {
                            OpportunityType = OpportunityType.ScheduledSession,
                            SessionSeriesId = x.ClassId,
                            ScheduledSessionId = x.Id
                        }),
                        SuperEvent = this.RenderOpportunityId(new SessionOpportunity
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

    public class AcmeSessionSeriesRPDEGenerator : RPDEFeedModifiedTimestampAndIDLong<SessionOpportunity, SessionSeries>
    {
        protected override List<RpdeItem<SessionSeries>> GetRPDEItems(long? afterTimestamp, long? afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var q = db.From<ClassTable>()
                .Join<SellerTable>()
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.Id)
                .Where(x => !afterTimestamp.HasValue && !afterId.HasValue ||
                    x.Modified > afterTimestamp ||
                    (x.Modified == afterTimestamp && x.Id > afterId) &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(this.RPDEPageSize);

                var query = db
                    .SelectMulti<ClassTable, SellerTable>(q)
                    .Select((result) => new RpdeItem<SessionSeries>
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
                            Id = this.RenderOpportunityId(new SessionOpportunity
                            {
                                OpportunityType = OpportunityType.SessionSeries,
                                SessionSeriesId = result.Item1.Id
                            }),
                            Name = result.Item1.Title,
                            Organizer = result.Item2.IsIndividual ? (ILegalEntity)new Person
                            {
                                Id = this.RenderSellerId(new SellerIdComponents { SellerIdLong = result.Item2.Id }),
                                Name = result.Item2.Name,
                                TaxMode = TaxMode.TaxGross
                            } : (ILegalEntity)new Organization
                            {
                                Id = this.RenderSellerId(new SellerIdComponents { SellerIdLong = result.Item2.Id }),
                                Name = result.Item2.Name,
                                TaxMode = TaxMode.TaxGross
                            },
                            Offers = new List<Offer> { new Offer
                                {
                                    Id = this.RenderOfferId(new SessionOpportunity
                                    {
                                        OfferOpportunityType = OpportunityType.SessionSeries,
                                        SessionSeriesId = result.Item1.Id,
                                        OfferId = 0
                                    }),
                                    Price = result.Item1.Price,
                                    PriceCurrency = "GBP",
                                    AvailableChannel = new List<AvailableChannelType>
                                    {
                                        AvailableChannelType.OpenBookingPrepayment
                                    },
                                    OpenBookingFlowRequirement = result.Item1.RequiresApproval 
                                        ? new List<OpenBookingFlowRequirement> { OpenBookingFlowRequirement.OpenBookingApproval }
                                        : null,
                                }
                            },
                            Location = new Place
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
                                    Latitude = 0.1m,
                                    Longitude = 0.1m
                                }
                            },
                            Url = new Uri("https://www.example.com/a-session-age"),
                            Activity = new List<Concept> {
                                new Concept
                                {
                                    Id = new Uri("https://openactive.io/activity-list#c07d63a0-8eb9-4602-8bcc-23be6deb8f83"),
                                    PrefLabel = "Jet Skiing",
                                    InScheme = new Uri("https://openactive.io/activity-list")
                                }
                            }
                        }
                    });

                return query.ToList();
            };
        }
    }

}
