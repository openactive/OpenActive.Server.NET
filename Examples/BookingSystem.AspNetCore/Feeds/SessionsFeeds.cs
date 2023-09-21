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
using Bogus;
using BookingSystem.AspNetCore.Helpers;
using ServiceStack;
using System.Globalization;

namespace BookingSystem
{
    public class AcmeScheduledSessionRpdeGenerator : RpdeFeedModifiedTimestampAndIdLong<SessionOpportunity, ScheduledSession>
    {
        //public override string FeedPath { get; protected set; } = "example path override";
        private readonly FakeBookingSystem _fakeBookingSystem;

        // Example constructor that can set state
        public AcmeScheduledSessionRpdeGenerator(FakeBookingSystem fakeBookingSystem)
        {
            this._fakeBookingSystem = fakeBookingSystem;
        }

        protected override async Task<List<RpdeItem<ScheduledSession>>> GetRpdeItems(long? afterTimestamp, long? afterId)
        {
            using (var db = _fakeBookingSystem.Database.Mem.Database.Open())
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
        private readonly AppSettings _appSettings;
        private readonly FakeBookingSystem _fakeBookingSystem;


        // Example constructor that can set state from EngineConfig
        public AcmeSessionSeriesRpdeGenerator(AppSettings appSettings, FakeBookingSystem fakeBookingSystem)
        {
            this._appSettings = appSettings;
            this._fakeBookingSystem = fakeBookingSystem;

        }

        protected override async Task<List<RpdeItem<SessionSeries>>> GetRpdeItems(long? afterTimestamp, long? afterId)
        {
            using (var db = _fakeBookingSystem.Database.Mem.Database.Open())
            {
                var q = db.From<ClassTable>()
                .Join<SellerTable>()
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.Id)
                .Where(x => !afterTimestamp.HasValue && !afterId.HasValue ||
                    x.Modified > afterTimestamp ||
                    x.Modified == afterTimestamp && x.Id > afterId &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(RpdePageSize);

                var query = db
                    .SelectMulti<ClassTable, SellerTable>(q)
                    .Select(result =>
                    {
                        var intt = (int)result.Item1.Modified;

                        var faker = new Faker() { Random = new Randomizer(((int)result.Item1.Modified + (int)result.Item1.Id)) };
                        // here we randomly decide whether the item is going to be a golden record or not by using Faker
                        // See the README for more detail on golden records.
                        var isGoldenRecord = faker.Random.Bool();

                        return new RpdeItem<SessionSeries>
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
                                Identifier = result.Item1.Id,
                                Name = GetNameAndActivityForSessions(result.Item1.Title, isGoldenRecord).Name,
                                EventAttendanceMode = MapAttendanceMode(result.Item1.AttendanceMode),
                                Description = faker.Lorem.Paragraphs(isGoldenRecord ? 4 : faker.Random.Number(4)),
                                AttendeeInstructions = FeedGenerationHelper.GenerateAttendeeInstructions(faker, isGoldenRecord),
                                GenderRestriction = faker.Random.Enum<GenderRestrictionType>(),
                                AgeRange = GenerateAgeRange(faker, isGoldenRecord),
                                Level = faker.Random.ListItems(new List<string> { "Beginner", "Intermediate", "Advanced" }, 1).ToList(),
                                Organizer = GenerateOrganizerOrPerson(faker, result.Item2),
                                AccessibilitySupport = FeedGenerationHelper.GenerateAccessibilitySupport(faker, isGoldenRecord),
                                AccessibilityInformation = faker.Lorem.Paragraphs(isGoldenRecord ? 2 : faker.Random.Number(2)),
                                IsWheelchairAccessible = isGoldenRecord || faker.Random.Bool() ? faker.Random.Bool() : faker.Random.ListItem(new List<bool?> { true, false, null, null }),
                                Category = GenerateCategory(faker, isGoldenRecord),
                                Image = FeedGenerationHelper.GenerateImages(faker, isGoldenRecord),
                                Video = isGoldenRecord || faker.Random.Bool() ? new List<VideoObject> { new VideoObject { Url = new Uri("https://www.youtube.com/watch?v=xvDZZLqlc-0") } } : null,
                                Leader = GenerateListOfPersons(faker, isGoldenRecord, 2),
                                Contributor = GenerateListOfPersons(faker, isGoldenRecord, 2),
                                IsCoached = isGoldenRecord || faker.Random.Bool() ? faker.Random.Bool() : faker.Random.ListItem(new List<bool?> { true, false, null, null }),
                                Offers = GenerateOffers(faker, isGoldenRecord, result.Item1),
                                // location MUST not be provided for fully virtual sessions
                                Location = result.Item1.AttendanceMode == AttendanceMode.Online ? null : FeedGenerationHelper.GetPlaceById(result.Item1.PlaceId),
                                // beta:affiliatedLocation MAY be provided for fully virtual sessions
                                AffiliatedLocation = (result.Item1.AttendanceMode == AttendanceMode.Offline && faker.Random.Bool()) ? null : FeedGenerationHelper.GetPlaceById(result.Item1.PlaceId),
                                EventSchedule = GenerateSchedules(faker, isGoldenRecord),
                                SchedulingNote = GenerateSchedulingNote(faker, isGoldenRecord),
                                IsAccessibleForFree = result.Item1.Price == 0,
                                Url = new Uri($"https://www.example.com/sessions/{result.Item1.Id}"),
                                Activity = GetNameAndActivityForSessions(result.Item1.Title, isGoldenRecord).Activity,
                                Programme = GenerateBrand(faker, isGoldenRecord),
                                IsInteractivityPreferred = result.Item1.AttendanceMode == AttendanceMode.Offline ? null : (isGoldenRecord ? true : faker.Random.ListItem(new List<bool?> { true, false, null })),
                                IsVirtuallyCoached = result.Item1.AttendanceMode == AttendanceMode.Offline ? null : (isGoldenRecord ? true : faker.Random.ListItem(new List<bool?> { true, false, null })),
                                ParticipantSuppliedEquipment = result.Item1.AttendanceMode == AttendanceMode.Offline ? null : (isGoldenRecord ? OpenActive.NET.RequiredStatusType.Optional : faker.Random.ListItem(new List<OpenActive.NET.RequiredStatusType?> { OpenActive.NET.RequiredStatusType.Optional, OpenActive.NET.RequiredStatusType.Required, OpenActive.NET.RequiredStatusType.Unavailable, null })),
                            }
                        };
                    });

                return query.ToList();
            }
        }
        private static EventAttendanceModeEnumeration MapAttendanceMode(AttendanceMode attendanceMode)
        {
            switch (attendanceMode)
            {
                case AttendanceMode.Offline:
                    return EventAttendanceModeEnumeration.OfflineEventAttendanceMode;
                case AttendanceMode.Online:
                    return EventAttendanceModeEnumeration.OnlineEventAttendanceMode;
                case AttendanceMode.Mixed:
                    return EventAttendanceModeEnumeration.MixedEventAttendanceMode;
                default:
                    throw new OpenBookingException(new OpenBookingError(), $"AttendanceMode Type {attendanceMode} not supported");
            }
        }

        private (string Name, List<Concept> Activity) GetNameAndActivityForSessions(string databaseTitle, bool isGoldenRecord)
        {
            // If both ACTIVITY_ID and ACTIVITY_PREF_LABEL env vars are set, these override the randomly generated activity. We also use these to generate an appropriate name
            if (Environment.GetEnvironmentVariable("ACTIVITY_ID") != null && Environment.GetEnvironmentVariable("ACTIVITY_PREF_LABEL") != null)
            {
                var name = $"{(isGoldenRecord ? "GOLDEN: " : "")} {Environment.GetEnvironmentVariable("ACTIVITY_PREF_LABEL")} class";
                var concept = new Concept
                {
                    Id = new Uri(Environment.GetEnvironmentVariable("ACTIVITY_ID")),
                    PrefLabel = Environment.GetEnvironmentVariable("ACTIVITY_PREF_LABEL"),
                    InScheme = new Uri("https://openactive.io/activity-list")
                };

                return (name, new List<Concept> { concept });
            }

            // If there isn't an override, we use the randomly generated name to derive the appropriate activity
            Concept activityConcept;
            switch (databaseTitle)
            {
                case string a when a.Contains("Yoga"):
                    activityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/activity-list#bf1a5e00-cdcf-465d-8c5a-6f57040b7f7e"),
                        PrefLabel = "Yoga",
                        InScheme = new Uri("https://openactive.io/activity-list")
                    };
                    break;
                case string a when a.Contains("Zumba"):
                    activityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/activity-list#78503fa2-ed24-4a80-a224-e2e94581d8a8"),
                        PrefLabel = "Zumba®",
                        InScheme = new Uri("https://openactive.io/activity-list")
                    };
                    break;
                case string a when a.Contains("Walking"):
                    activityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/activity-list#95092977-5a20-4d6e-b312-8fddabe71544"),
                        PrefLabel = "Walking",
                        InScheme = new Uri("https://openactive.io/activity-list")
                    };
                    break;
                case string a when a.Contains("Cycling"):
                    activityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/activity-list#4a19873e-118e-43f4-b86e-05acba8fb1de"),
                        PrefLabel = "Cycling",
                        InScheme = new Uri("https://openactive.io/activity-list")
                    };
                    break;
                case string a when a.Contains("Running"):
                    activityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/activity-list#72ddb2dc-7d75-424e-880a-d90eabe91381"),
                        PrefLabel = "Running",
                        InScheme = new Uri("https://openactive.io/activity-list")
                    };
                    break;
                case string a when a.Contains("Jumping"):
                    activityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/activity-list#8a4abff3-c616-4f33-80a1-398b88c672a3"),
                        PrefLabel = "World Jumping®",
                        InScheme = new Uri("https://openactive.io/activity-list")
                    };
                    break;
                default:
                    activityConcept = new Concept
                    {
                        Id = new Uri("https://openactive.io/activity-list#c07d63a0-8eb9-4602-8bcc-23be6deb8f83"),
                        PrefLabel = "Jet Skiing",
                        InScheme = new Uri("https://openactive.io/activity-list")
                    };
                    break;
            }

            var nameWithGolden = $"{(isGoldenRecord ? "GOLDEN: " : "")}{databaseTitle}";
            return (nameWithGolden, new List<Concept> { activityConcept });

        }

        private QuantitativeValue GenerateAgeRange(Faker faker, bool isGoldenRecord)
        {
            var ageRange = new QuantitativeValue();
            if (isGoldenRecord || faker.Random.Bool()) ageRange.MaxValue = faker.Random.Number(16, 100);
            if (isGoldenRecord || faker.Random.Bool()) ageRange.MinValue = faker.Random.Number(0, ageRange.MaxValue == null ? (int)ageRange.MaxValue : 100);

            if (ageRange.MaxValue == null && ageRange.MinValue == null) ageRange.MinValue = 0;
            return ageRange;
        }

        private ILegalEntity GenerateOrganizerOrPerson(Faker faker, SellerTable seller)
        {
            if (_appSettings.FeatureFlags.SingleSeller)
                return new Organization
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
                    Telephone = faker.Phone.PhoneNumber("0#### ######"),
                    SameAs = new List<Uri> { new Uri("https://socialmedia/testseller") }
                };
            if (seller.IsIndividual)
                return new OpenActive.NET.Person
                {
                    Id = RenderSellerId(new SimpleIdComponents { IdLong = seller.Id }),
                    Name = seller.Name,
                    TaxMode = seller.IsTaxGross ? TaxMode.TaxGross : TaxMode.TaxNet,
                    IsOpenBookingAllowed = true,
                    Telephone = faker.Phone.PhoneNumber("07### ######")
                };
            return new Organization
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
                Url = new Uri(faker.Internet.Url()),
                Telephone = faker.Phone.PhoneNumber("0#### ######"),
                SameAs = new List<Uri> { new Uri($"https://socialmedia/{seller.Name}") }
            };
        }

        private List<string> GenerateCategory(Faker faker, bool isGoldenRecord)
        {
            var listOfPossibleCategories = new List<string>
            {
                 "Group Exercise Classes",
                 "Toning & Strength",
                 "Group Exercise - Virtual"
            };

            return FeedGenerationHelper.GetRandomElementsOf(faker, listOfPossibleCategories, isGoldenRecord, 1).ToList();
        }

        private List<OpenActive.NET.Person> GenerateListOfPersons(Faker faker, bool isGoldenRecord, int possibleMax)
        {
            static OpenActive.NET.Person GeneratePerson(Faker faker, bool isGoldenRecord)
            {
                var id = faker.Finance.Bic();
                var genderIndex = faker.Random.Number(1);
                var gender = (Bogus.DataSets.Name.Gender)genderIndex;
                var givenName = faker.Name.FirstName(gender);
                var familyName = faker.Name.LastName(gender);
                var name = $"{givenName} {familyName}";
                var isLiteRecord = isGoldenRecord ? false : faker.Random.Bool();

                return new OpenActive.NET.Person
                {
                    Id = new Uri($"https://example.com/people/{id}"),
                    Identifier = id,
                    Name = name,
                    GivenName = isLiteRecord ? null : givenName,
                    FamilyName = isLiteRecord ? null : familyName,
                    Gender = genderIndex == 1 ? Schema.NET.GenderType.Female : Schema.NET.GenderType.Male,
                    JobTitle = faker.Random.ListItem(new List<string> { "Leader", "Team leader", "Host", "Instructor", "Coach" }),
                    Telephone = isLiteRecord ? null : faker.Phone.PhoneNumber("07## ### ####"),
                    Email = isLiteRecord ? null : faker.Internet.ExampleEmail(),
                    Url = new Uri($"{faker.Internet.Url()}/profile/{faker.Random.Number(50)}"),
                    Image = new Schema.NET.ImageObject { Url = new Uri(faker.Internet.Avatar()) }
                };
            }

            var output = new List<OpenActive.NET.Person>();
            var max = isGoldenRecord ? possibleMax : faker.Random.Number(possibleMax);
            for (var i = 0; i < max; i++)
            {
                output.Add(GeneratePerson(faker, isGoldenRecord));
            }
            return output;
        }

        private List<Schedule> GenerateSchedules(Faker faker, bool isGoldenRecord)
        {
            var schedules = new List<Schedule>();
            PartialSchedule GenerateSchedule(Faker faker)
            {
                var startTimeString = $"{faker.Random.Number(min: 10, max: 22)}:{faker.Random.ListItem(new List<string> { "00", "15", "30", "45" })}:00";
                var startTime = new TimeValue(startTimeString);
                var duration = faker.Random.ListItem(new List<TimeSpan> { new TimeSpan(0, 30, 0), new TimeSpan(1, 0, 0), new TimeSpan(1, 30, 0), new TimeSpan(2, 0, 0) });
                var startTimeSpan = TimeSpan.Parse(startTimeString);

                var endTime = new DateTime(startTimeSpan.Add(duration).Ticks);
                var endTimeString = endTime.ToString("HH:mm");
                var endTimeTM = new TimeValue(endTimeString);
                var startDateFaker = faker.Date.Soon();
                var startDate = new DateValue(startDateFaker);
                var endDate = new DateValue(faker.Date.Soon(28, startDateFaker));

                var partialSchedule = new PartialSchedule
                {
                    StartTime = startTime,
                    Duration = duration,
                    EndTime = endTimeTM,
                    StartDate = startDate,
                    EndDate = endDate,
                    RepeatFrequency = faker.Random.ListItem(new List<TimeSpan> { new TimeSpan(7, 0, 0, 0), new TimeSpan(14, 0, 0, 0) }),
                    ByDay = faker.Random.EnumValues<Schema.NET.DayOfWeek>().ToList()
                };
                return partialSchedule;
            }

            for (var i = 0; i < 2; i++)
            {
                schedules.Add(GenerateSchedule(faker));
            }

            return FeedGenerationHelper.GetRandomElementsOf(faker, schedules, isGoldenRecord, 1, 1).ToList();
        }

        private string GenerateSchedulingNote(Faker faker, bool isGoldenRecord)
        {
            var allSchedulingNotes = new List<string>
            {
                "Sessions are not running during school holidays.",
                "Sessions may be cancelled with 15 minutes notice, please keep an eye on your e-mail.",
                "Sessions are scheduled with best intentions, but sometimes need to be rescheduled due to venue availability. Ensure that you contact the organizer before turning up."
            };

            if (isGoldenRecord) return faker.Random.ListItem(allSchedulingNotes);
            return faker.Random.Bool() ? faker.Random.ListItem(allSchedulingNotes) : null;
        }

        private List<Offer> GenerateOffers(Faker faker, bool isGoldenRecord, ClassTable @class)
        {
            var ageRangesForOffers = new List<QuantitativeValue>
            {
                new QuantitativeValue {MinValue = 18, MaxValue = 59, Name = "Adult"},
                new QuantitativeValue { MaxValue = 17, Name = "Junior"},
                new QuantitativeValue {MinValue = 60, Name = "Senior"},
                new QuantitativeValue {MinValue = 18, MaxValue = 59, Name = "Adult (off-peak)"},
            };

            Offer GenerateOffer(ClassTable @class, QuantitativeValue ageRange)
            {
                return new Offer
                {
                    Id = RenderOfferId(new SessionOpportunity
                    {
                        OfferOpportunityType = OpportunityType.SessionSeries,
                        SessionSeriesId = @class.Id,
                        OfferId = 0
                    }),
                    Price = @class.Price,
                    PriceCurrency = "GBP",
                    Name = ageRange.Name,
                    OpenBookingFlowRequirement = FeedGenerationHelper.OpenBookingFlowRequirement(@class.RequiresApproval, @class.RequiresAttendeeValidation, @class.RequiresAdditionalDetails, @class.AllowsProposalAmendment),
                    ValidFromBeforeStartDate = @class.ValidFromBeforeStartDate,
                    LatestCancellationBeforeStartDate = @class.LatestCancellationBeforeStartDate,
                    OpenBookingPrepayment = _appSettings.FeatureFlags.PrepaymentAlwaysRequired ? null : @class.Prepayment.Convert(),
                    AllowCustomerCancellationFullRefund = @class.AllowCustomerCancellationFullRefund,
                    AcceptedPaymentMethod = new List<PaymentMethod> { PaymentMethod.Cash, PaymentMethod.PaymentMethodCreditCard },
                    AgeRestriction = ageRange,
                };
            }

            var allOffersForAllAgeRanges = ageRangesForOffers.Select(ageRange => GenerateOffer(@class, ageRange)).ToList();

            return FeedGenerationHelper.GetRandomElementsOf(faker, allOffersForAllAgeRanges, isGoldenRecord, 1, 2).ToList();
        }

        private Brand GenerateBrand(Faker faker, bool isGoldenRecord)
        {
            return new Brand
            {
                Name = faker.Random.ListItem(new List<string> { "Keyways Active", "This Girl Can", "Back to Activity", "Mega-active Super Dads" }),
                Url = new Uri(faker.Internet.Url()),
                Description = faker.Lorem.Paragraphs(isGoldenRecord ? 4 : faker.Random.Number(4)),
                Logo = new ImageObject { Url = new Uri(faker.Internet.Avatar()) },
                Video = new List<VideoObject> { new VideoObject { Url = new Uri("https://www.youtube.com/watch?v=N268gBOvnzo") } }
            };
        }
    }
}
