using OpenActive.DatasetSite.NET;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.NET.Rpde.Version1;
using OpenActive.Server.NET.OpenBookingHelper;
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
            var query = from occurances in FakeBookingSystem.Database.Occurrences
                        orderby occurances.Modified.ToUnixTimeMilliseconds(), occurances.Id
                        where !afterTimestamp.HasValue && !afterId.HasValue ||
                              occurances.Modified.ToUnixTimeMilliseconds() > afterTimestamp ||  
                              (occurances.Modified.ToUnixTimeMilliseconds() == afterTimestamp && occurances.Id > afterId)
                              // Ensure the RPDE endpoint filters out all items with a "modified" date after 2 seconds in the past, to delay items appearing in the feed
                              // https://app.gitbook.com/@openactive/s/openactive-developer/publishing-data/data-feeds/implementing-rpde-feeds
                              && occurances.Modified < DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)

                        select new RpdeItem<ScheduledSession>
                        {
                            Kind = RpdeKind.ScheduledSession,
                            Id = occurances.Id,
                            Modified = occurances.Modified.ToUnixTimeMilliseconds(),
                            State = occurances.Deleted ? RpdeState.Deleted : RpdeState.Updated,
                            Data = occurances.Deleted ? null : new ScheduledSession
                            {
                                // QUESTION: Should the this.IdTemplate and this.BaseUrl be passed in each time rather than set on
                                // the parent class? Current thinking is it's more extensible on parent class as function signature remains
                                // constant as power of configuration through underlying class grows (i.e. as new properties are added)
                                Id = this.RenderOpportunityId(new SessionOpportunity
                                {
                                    OpportunityType = OpportunityType.ScheduledSession,
                                    SessionSeriesId = occurances.ClassId,
                                    ScheduledSessionId = occurances.Id
                                }),
                                SuperEvent = this.RenderOpportunityId(new SessionOpportunity
                                {
                                    OpportunityType = OpportunityType.SessionSeries,
                                    SessionSeriesId = occurances.ClassId
                                }),
                                StartDate = (DateTimeOffset)occurances.Start,
                                EndDate = (DateTimeOffset)occurances.End,
                                Duration = occurances.End - occurances.Start,
                                RemainingAttendeeCapacity  = occurances.RemainingSpaces,
                                MaximumAttendeeCapacity = occurances.TotalSpaces
                            }
                        };

            // Note there's a race condition in the in-memory database that allows records to be returned from the above query out of order when modified at the same time. The below ensures the correct order is returned.
            var items = query.ToList().Take(this.RPDEPageSize).ToList();

            /*
            // Filter out any that were updated while the query was running
            var lastItemModified = items.LastOrDefault()?.Modified;

            if (lastItemModified != null)
            {
                items = items.Where(x => x.Modified <= lastItemModified).ToList(); //.OrderBy(x => x.Modified).ThenBy(x => x.Id)
            }
            */
            return items;
        }
    }

    public class AcmeSessionSeriesRPDEGenerator : RPDEFeedModifiedTimestampAndIDLong<SessionOpportunity, SessionSeries>
    {
        protected override List<RpdeItem<SessionSeries>> GetRPDEItems(long? afterTimestamp, long? afterId)
        {
            var query = from @class in FakeBookingSystem.Database.Classes
                        join seller in FakeBookingSystem.Database.Sellers on @class.SellerId equals seller.Id
                        orderby @class.Modified.ToUnixTimeMilliseconds(), @class.Id
                        where !afterTimestamp.HasValue && !afterId.HasValue ||
                              @class.Modified.ToUnixTimeMilliseconds() > afterTimestamp ||
                              (@class.Modified.ToUnixTimeMilliseconds() == afterTimestamp && @class.Id > afterId)
                              // Ensure the RPDE endpoint filters out all items with a "modified" date after 2 seconds in the past, to delay items appearing in the feed
                              // https://app.gitbook.com/@openactive/s/openactive-developer/publishing-data/data-feeds/implementing-rpde-feeds
                              && @class.Modified < DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)

                        select new RpdeItem<SessionSeries>
                        {
                            Kind = RpdeKind.SessionSeries,
                            Id = @class.Id,
                            Modified = @class.Modified.ToUnixTimeMilliseconds(),
                            State = @class.Deleted ? RpdeState.Deleted : RpdeState.Updated,
                            Data = @class.Deleted ? null : new SessionSeries
                            {
                                // QUESTION: Should the this.IdTemplate and this.BaseUrl be passed in each time rather than set on
                                // the parent class? Current thinking is it's more extensible on parent class as function signature remains
                                // constant as power of configuration through underlying class grows (i.e. as new properties are added)
                                Id = this.RenderOpportunityId( new SessionOpportunity
                                {
                                    OpportunityType = OpportunityType.SessionSeries,
                                    SessionSeriesId = @class.Id
                                }),
                                Name = @class.Title,
                                Organizer = seller.IsIndividual ? (ILegalEntity)new Person
                                {
                                    Id = this.RenderSellerId(new SellerIdComponents { SellerIdLong = seller.Id }),
                                    Name = seller.Name,
                                    TaxMode = TaxMode.TaxGross
                                } : (ILegalEntity)new Organization
                                {
                                    Id = this.RenderSellerId(new SellerIdComponents { SellerIdLong = seller.Id }),
                                    Name = seller.Name,
                                    TaxMode = TaxMode.TaxGross
                                },
                                Offers = new List<Offer> { new Offer
                                    {
                                        Id = this.RenderOfferId(new SessionOpportunity
                                        {
                                            OfferOpportunityType = OpportunityType.SessionSeries,
                                            SessionSeriesId = @class.Id,
                                            OfferId = 0
                                        }),
                                        Price = @class.Price,
                                        PriceCurrency = "GBP",
                                        AvailableChannel = new List<AvailableChannelType>
                                        {
                                            AvailableChannelType.OpenBookingPrepayment
                                        }
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
                        };

            // Note there's a race condition in the in-memory database that allows records to be returned from the above query out of order when modified at the same time. The below ensures the correct order is returned.
            var items = query.ToList().Take(this.RPDEPageSize).ToList();

            /*
            // Filter out any that were updated while the query was running
            var lastItemModified = items.LastOrDefault()?.Modified;

            if (lastItemModified != null)
            {
                items = items.Where(x => x.Modified <= lastItemModified).ToList(); //.OrderBy(x => x.Modified).ThenBy(x => x.Id)
            }
            */
            return items;
        }
    }

}
