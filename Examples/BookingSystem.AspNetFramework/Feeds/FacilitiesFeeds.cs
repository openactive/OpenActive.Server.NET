using OpenActive.DatasetSite.NET;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.NET.Rpde.Version1;
using OpenActive.Server.NET.OpenBookingHelper;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using BookingSystem.AspNetFramework.Helpers;

namespace BookingSystem
{
    public class AcmeFacilityUseRpdeGenerator : RpdeFeedModifiedTimestampAndIdLong<FacilityOpportunity, FacilityUse>
    {
        //public override string FeedPath { get; protected set; } = "example path override";

        // Example constructor that can set state
        private bool UseSingleSellerMode;
        public AcmeFacilityUseRpdeGenerator(bool UseSingleSellerMode)
        {
            this.UseSingleSellerMode = UseSingleSellerMode;
        }

        protected override List<RpdeItem<FacilityUse>> GetRpdeItems(long? afterTimestamp, long? afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var q = db.From<FacilityUseTable>()
                .Join<SellerTable>()
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.Id)
                .Where(x => !afterTimestamp.HasValue && !afterId.HasValue ||
                    x.Modified > afterTimestamp ||
                    x.Modified == afterTimestamp && x.Id > afterId &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(RpdePageSize);

                var query = db
                    .SelectMulti<FacilityUseTable, SellerTable>(q)
                    .Select(result => new RpdeItem<FacilityUse>
                    {
                        Kind = RpdeKind.FacilityUse,
                        Id = result.Item1.Id,
                        Modified = result.Item1.Modified,
                        State = result.Item1.Deleted ? RpdeState.Deleted : RpdeState.Updated,
                        Data = result.Item1.Deleted ? null : new FacilityUse
                        {
                            // QUESTION: Should the this.IdTemplate and this.BaseUrl be passed in each time rather than set on
                            // the parent class? Current thinking is it's more extensible on parent class as function signature remains
                            // constant as power of configuration through underlying class grows (i.e. as new properties are added)
                            Id = RenderOpportunityId(new FacilityOpportunity
                            {
                                OpportunityType = OpportunityType.FacilityUse, // isIndividual??
                                FacilityUseId = result.Item1.Id
                            }),
                            Name = result.Item1.Name,
                            Provider = UseSingleSellerMode ? new Organization
                            {
                                Id = RenderSingleSellerId(),
                                Name = "Test Seller",
                                TaxMode = TaxMode.TaxGross
                            } : new Organization
                            {
                                Id = RenderSellerId(new SellerIdComponents { SellerIdLong = result.Item2.Id }),
                                Name = result.Item2.Name,
                                TaxMode = TaxMode.TaxGross
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
            }
        }
    }

    public class AcmeFacilityUseSlotRpdeGenerator : RpdeFeedModifiedTimestampAndIdLong<FacilityOpportunity, Slot>
    {
        //public override string FeedPath { get; protected set; } = "example path override";

        protected override List<RpdeItem<Slot>> GetRpdeItems(long? afterTimestamp, long? afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var query = db.Select<SlotTable>()
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.Id)
                .Where(x => !afterTimestamp.HasValue && !afterId.HasValue ||
                    x.Modified > afterTimestamp ||
                    x.Modified == afterTimestamp && x.Id > afterId &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(RpdePageSize)
                .Select(x => new RpdeItem<Slot>
                {
                    Kind = RpdeKind.FacilityUseSlot,
                    Id = x.Id,
                    Modified = x.Modified,
                    State = x.Deleted ? RpdeState.Deleted : RpdeState.Updated,
                    Data = x.Deleted ? null : new Slot
                    {
                        // QUESTION: Should the this.IdTemplate and this.BaseUrl be passed in each time rather than set on
                        // the parent class? Current thinking is it's more extensible on parent class as function signature remains
                        // constant as power of configuration through underlying class grows (i.e. as new properties are added)
                        Id = RenderOpportunityId(new FacilityOpportunity
                        {
                            OpportunityType = OpportunityType.FacilityUseSlot,
                            FacilityUseId = x.FacilityUseId,
                            SlotId = x.Id
                        }),
                        FacilityUse = RenderOpportunityId(new FacilityOpportunity
                        {
                            OpportunityType = OpportunityType.FacilityUse,
                            FacilityUseId = x.FacilityUseId
                        }),
                        Identifier = x.Id,
                        StartDate = (DateTimeOffset)x.Start,
                        EndDate = (DateTimeOffset)x.End,
                        Duration = x.End - x.Start,
                        RemainingUses = x.RemainingUses - x.LeasedUses,
                        MaximumUses = x.MaximumUses,
                        Offers = new List<Offer> { new Offer
                                {
                                    Id = RenderOfferId(new FacilityOpportunity
                                    {
                                        OfferId = 0,
                                        OpportunityType = OpportunityType.FacilityUseSlot,
                                        FacilityUseId = x.FacilityUseId,
                                        SlotId = x.Id
                                    }),
                                    Price = x.Price,
                                    PriceCurrency = "GBP",
                                    AvailableChannel = new List<AvailableChannelType>
                                    {
                                        AvailableChannelType.OpenBookingPrepayment
                                    },
                                    OpenBookingFlowRequirement = x.RequiresApproval
                                        ? new List<OpenBookingFlowRequirement> { OpenBookingFlowRequirement.OpenBookingApproval }
                                        : null,
                                    ValidFromBeforeStartDate = x.ValidFromBeforeStartDate,
                                    Prepayment = x.Prepayment.Convert()
                                }
                            },
                    }
                });

                return query.ToList();
            }
        }
    }
}