using Bogus;
using BookingSystem.AspNetCore.Helpers;
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
    public class AcmeFacilityUseRpdeGenerator : RpdeFeedModifiedTimestampAndIdLong<FacilityOpportunity, FacilityUse>
    {
        //public override string FeedPath { get; protected set; } = "example path override";
        private readonly AppSettings _appSettings;
        private readonly FakeBookingSystem _fakeBookingSystem;

        // Example constructor that can set state
        public AcmeFacilityUseRpdeGenerator(AppSettings appSettings, FakeBookingSystem fakeBookingSystem)
        {
            this._appSettings = appSettings;
            this._fakeBookingSystem = fakeBookingSystem;
        }

        protected override async Task<List<RpdeItem<FacilityUse>>> GetRpdeItems(long? afterTimestamp, long? afterId)
        {
            var facilityTypeId = Environment.GetEnvironmentVariable("FACILITY_TYPE_ID") ?? "https://openactive.io/facility-types#a1f82b7a-1258-4d9a-8dc5-bfc2ae961651";
            var facilityTypePrefLabel = Environment.GetEnvironmentVariable("FACILITY_TYPE_PREF_LABEL") ?? "Squash Court";

            using (var db = _fakeBookingSystem.Database.Mem.Database.Open())
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
                    .Select(result =>
                    {
                        var faker = new Faker() { Random = new Randomizer((int)result.Item1.Modified) };
                        var isGoldenRecord = faker.Random.Bool();

                        return new RpdeItem<FacilityUse>
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
                                Identifier = result.Item1.Id,
                                Name = GetNameAndFacilityTypeForFacility(result.Item1.Name, isGoldenRecord).Name,
                                Description = faker.Lorem.Paragraphs(isGoldenRecord ? 4 : faker.Random.Number(4)),
                                Provider = GenerateOrganizer(result.Item2),
                                Url = new Uri($"https://www.example.com/facilities/{result.Item1.Id}"),
                                AttendeeInstructions = FeedGenerationHelper.GenerateAttendeeInstructions(faker, isGoldenRecord),
                                AccessibilitySupport = FeedGenerationHelper.GenerateAccessibilitySupport(faker, isGoldenRecord),
                                AccessibilityInformation = faker.Lorem.Paragraphs(isGoldenRecord ? 2 : faker.Random.Number(2)),
                                IsWheelchairAccessible = isGoldenRecord || faker.Random.Bool() ? faker.Random.Bool() : faker.Random.ListItem(new List<bool?> { true, false, null, null }),
                                Category = GenerateCategory(faker, isGoldenRecord),
                                Image = FeedGenerationHelper.GenerateImages(faker, isGoldenRecord),
                                Video = isGoldenRecord || faker.Random.Bool() ? new List<VideoObject> { new VideoObject { Url = new Uri("https://www.youtube.com/watch?v=xvDZZLqlc-0") } } : null,
                                Location = FeedGenerationHelper.GetPlaceById(result.Item1.PlaceId),
                                FacilityType = GetNameAndFacilityTypeForFacility(result.Item1.Name, isGoldenRecord).Facility,
                                IndividualFacilityUse = result.Item1.IndividualFacilityUses != null ? result.Item1.IndividualFacilityUses.Select(ifu => new OpenActive.NET.IndividualFacilityUse
                                {
                                    Id = RenderOpportunityId(new FacilityOpportunity
                                    {
                                        OpportunityType = OpportunityType.IndividualFacilityUse,
                                        IndividualFacilityUseId = ifu.Id,
                                        FacilityUseId = result.Item1.Id
                                    }),
                                    Name = ifu.Name
                                }).ToList() : null,
                            }
                        };
                    });

                return query.ToList();
            }
        }

        private (string Name, List<Concept> Facility) GetNameAndFacilityTypeForFacility(string databaseTitle, bool isGoldenRecord)
        {
            // If both FACILITY_TYPE_ID and FACILITY_TYPE_PREF_LABEL env vars are set, these override the randomly generated activity. We also use these to generate an appropriate name
            if (Environment.GetEnvironmentVariable("FACILITY_TYPE_ID") != null && Environment.GetEnvironmentVariable("FACILITY_TYPE_PREF_LABEL") != null)
            {
                var name = $"{(isGoldenRecord ? "GOLDEN: " : "")} {Environment.GetEnvironmentVariable("FACILITY_TYPE_PREF_LABEL")} facility";
                var concept = new Concept
                {
                    Id = new Uri(Environment.GetEnvironmentVariable("FACILITY_TYPE_ID")),
                    PrefLabel = Environment.GetEnvironmentVariable("FACILITY_TYPE_PREF_LABEL"),
                    InScheme = new Uri("https://openactive.io/activity-list")
                };

                return (name, new List<Concept> { concept });
            }

            // If there isn't an override, we use the randomly generated name to derive the appropriate activity
            Concept facilityConcept;
            switch (databaseTitle)
            {
                case string a when a.Contains("Sports Hall"):
                    facilityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/facility-types#da364f9b-8bb2-490e-9e2f-1068790b9e35"),
                        PrefLabel = "Sports Hall",
                        InScheme = new Uri("https://openactive.io/facility-types")
                    };
                    break;
                case string a when a.Contains("Squash Court"):
                    facilityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/facility-types#a1f82b7a-1258-4d9a-8dc5-bfc2ae961651"),
                        PrefLabel = "Squash Court",
                        InScheme = new Uri("https://openactive.io/facility-types")
                    };
                    break;
                case string a when a.Contains("Badminton Court"):
                    facilityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/facility-types#9db5681e-700e-4b30-99a5-355885d94db2"),
                        PrefLabel = "Badminton Court",
                        InScheme = new Uri("https://openactive.io/facility-types")
                    };
                    break;
                case string a when a.Contains("Cricket Net"):
                    facilityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/facility-types#2d333183-6a6d-4a95-aad4-c5699f705b14"),
                        PrefLabel = "Cricket Net",
                        InScheme = new Uri("https://openactive.io/facility-types")
                    };
                    break;
                default:
                    facilityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/facility-types#a1f82b7a-1258-4d9a-8dc5-bfc2ae961651"),
                        PrefLabel = "Squash Court",
                        InScheme = new Uri("https://openactive.io/facility-types")
                    };
                    break;
            }

            var nameWithGolden = $"{(isGoldenRecord ? "GOLDEN: " : "")}{databaseTitle}";
            return (nameWithGolden, new List<Concept> { facilityConcept });

        }

        private Organization GenerateOrganizer(SellerTable seller)
        {
            return _appSettings.FeatureFlags.SingleSeller ? new Organization
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
            } : new Organization
            {
                Id = RenderSellerId(new SimpleIdComponents { IdLong = seller.Id }),
                Name = seller.Name,
                TaxMode = seller.IsTaxGross ? TaxMode.TaxGross : TaxMode.TaxNet,
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
            };
        }

        private List<string> GenerateCategory(Faker faker, bool isGoldenRecord)
        {
            var listOfPossibleCategories = new List<string>
            {
                 "Bookable Facilities",
                 "Ball Sports",
            };

            return FeedGenerationHelper.GetRandomElementsOf(faker, listOfPossibleCategories, isGoldenRecord, 1).ToList();
        }

        private List<OpeningHoursSpecification> GenerateOpeningHours(Faker faker)
        {
            return new List<OpeningHoursSpecification>
                        {
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Sunday }, Opens = $"{faker.Random.Number(9,12)}:00", Closes = $"{faker.Random.Number(15,17)}:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Monday }, Opens = $"{faker.Random.Number(6,10)}:00", Closes = $"{faker.Random.Number(18,21)}:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Tuesday }, Opens = $"{faker.Random.Number(6,10)}:00", Closes = $"{faker.Random.Number(18,21)}:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Wednesday }, Opens = $"{faker.Random.Number(6,10)}:00", Closes = $"{faker.Random.Number(18,21)}:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Thursday }, Opens = $"{faker.Random.Number(6,10)}:00", Closes = $"{faker.Random.Number(18,21)}:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Friday }, Opens = $"{faker.Random.Number(6,10)}:00", Closes = $"{faker.Random.Number(18,21)}:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Saturday }, Opens = $"{faker.Random.Number(9,12)}:00", Closes = $"{faker.Random.Number(15,17)}:30"}
                        };
        }
    }

    public class AcmeFacilityUseSlotRpdeGenerator : RpdeFeedModifiedTimestampAndIdLong<FacilityOpportunity, Slot>
    {
        //public override string FeedPath { get; protected set; } = "example path override";
        private readonly AppSettings _appSettings;
        private readonly FakeBookingSystem _fakeBookingSystem;

        // Example constructor that can set state
        public AcmeFacilityUseSlotRpdeGenerator(AppSettings appSettings, FakeBookingSystem fakeBookingSystem)
        {
            this._appSettings = appSettings;
            this._fakeBookingSystem = fakeBookingSystem;
        }

        protected override async Task<List<RpdeItem<Slot>>> GetRpdeItems(long? afterTimestamp, long? afterId)
        {
            using (var db = _fakeBookingSystem.Database.Mem.Database.Open())
            {
                var query = db.Select<SlotTable>()
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.Id)
                .Where(x => !afterTimestamp.HasValue && !afterId.HasValue ||
                    x.Modified > afterTimestamp ||
                    x.Modified == afterTimestamp && x.Id > afterId &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(RpdePageSize)
                .Select(x =>
                {
                    var faker = new Faker() { Random = new Randomizer((int)x.Modified) };
                    return new RpdeItem<Slot>
                    {
                        Kind = _appSettings.FeatureFlags.FacilityUseHasSlots ? RpdeKind.FacilityUseSlot : RpdeKind.IndividualFacilityUseSlot,
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
                                OpportunityType = _appSettings.FeatureFlags.FacilityUseHasSlots ? OpportunityType.FacilityUseSlot : OpportunityType.IndividualFacilityUseSlot,
                                FacilityUseId = x.FacilityUseId,
                                SlotId = x.Id,
                                IndividualFacilityUseId = !_appSettings.FeatureFlags.FacilityUseHasSlots ? x.IndividualFacilityUseId : null,
                            }),
                            FacilityUse = _appSettings.FeatureFlags.FacilityUseHasSlots ?
                           RenderOpportunityId(new FacilityOpportunity
                           {
                               OpportunityType = OpportunityType.FacilityUse,
                               FacilityUseId = x.FacilityUseId
                           })
                           : RenderOpportunityId(new FacilityOpportunity
                           {
                               OpportunityType = OpportunityType.IndividualFacilityUse,
                               IndividualFacilityUseId = x.IndividualFacilityUseId,
                               FacilityUseId = x.FacilityUseId,
                           }),
                            Identifier = x.Id,
                            StartDate = (DateTimeOffset)x.Start,
                            EndDate = (DateTimeOffset)x.End,
                            Duration = x.End - x.Start,
                            RemainingUses = x.RemainingUses - x.LeasedUses,
                            MaximumUses = x.MaximumUses,
                            Offers = GenerateOffers(faker, false, x)
                        }
                    };
                });

                return query.ToList();
            }
        }

        private List<Offer> GenerateOffers(Faker faker, bool isGoldenRecord, SlotTable slot)
        {
            var ageRangesForOffers = new List<QuantitativeValue>
            {
                new QuantitativeValue {MinValue = 18, MaxValue = 59, Name = "Adult"},
                new QuantitativeValue { MaxValue = 17, Name = "Junior"},
                new QuantitativeValue {MinValue = 60, Name = "Senior"},
                new QuantitativeValue {MinValue = 18, MaxValue = 59, Name = "Adult (off-peak)"},
            };

            Offer GenerateOffer(SlotTable slot, QuantitativeValue ageRange)
            {
                return new Offer
                {
                    Id = RenderOfferId(new FacilityOpportunity
                    {
                        OfferId = 0,
                        OpportunityType = _appSettings.FeatureFlags.FacilityUseHasSlots ? OpportunityType.FacilityUseSlot : OpportunityType.IndividualFacilityUseSlot,
                        FacilityUseId = slot.FacilityUseId,
                        SlotId = slot.Id,
                        IndividualFacilityUseId = !_appSettings.FeatureFlags.FacilityUseHasSlots ? slot.IndividualFacilityUseId : null,
                    }),
                    Price = slot.Price,
                    PriceCurrency = "GBP",
                    OpenBookingFlowRequirement = FeedGenerationHelper.OpenBookingFlowRequirement(slot.RequiresApproval, slot.RequiresAttendeeValidation, slot.RequiresAdditionalDetails, slot.AllowsProposalAmendment),
                    ValidFromBeforeStartDate = slot.ValidFromBeforeStartDate,
                    LatestCancellationBeforeStartDate = slot.LatestCancellationBeforeStartDate,
                    OpenBookingPrepayment = _appSettings.FeatureFlags.PrepaymentAlwaysRequired ? null : slot.Prepayment.Convert(),
                    AllowCustomerCancellationFullRefund = slot.AllowCustomerCancellationFullRefund,
                    AcceptedPaymentMethod = new List<PaymentMethod> { PaymentMethod.Cash, PaymentMethod.PaymentMethodCreditCard },
                };
            }

            var allOffersForAllAgeRanges = ageRangesForOffers.Select(ageRange => GenerateOffer(slot, ageRange)).ToList();

            return FeedGenerationHelper.GetRandomElementsOf(faker, allOffersForAllAgeRanges, isGoldenRecord, 1, 4).ToList();
        }

    }
}