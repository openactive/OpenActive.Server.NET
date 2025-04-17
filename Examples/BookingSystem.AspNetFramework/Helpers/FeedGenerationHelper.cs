using System;
using OpenActive.NET;
using System.Collections.Generic;
using OpenActive.FakeDatabase.NET;
using Bogus;
using System.Linq;
using Bogus.DataSets;
using OpenActive.Server.NET.OpenBookingHelper;
using System.Security.Policy;



namespace BookingSystem.AspNetCore.Helpers
{
    public static class FeedGenerationHelper
    {
        public static IList<T> GetRandomElementsOf<T>(Faker faker, IList<T> list, bool isGoldenRecord, int minimumNumberOfElements = 0, int maximumNumberOfElements = 0)
        {
            // If this is for the golden record, return the whole list so that all the possible data values are returned
            if (isGoldenRecord) return list;

            // If maximumNumberOfElements is the default value, use list.Count, if it's been set, use that
            var max = maximumNumberOfElements == 0 ? list.Count : maximumNumberOfElements;
            // Otherwise return a random number of elements from the list
            var randomNumberOfElementsToReturn = faker.Random.Number(minimumNumberOfElements, max);
            return faker.Random.ListItems(list, randomNumberOfElementsToReturn);
        }


        public static Place GetPlaceById(long placeId)
        {
            // Three hardcoded fake places
            switch (placeId)
            {
                case 1:
                    return new Place
                    {
                        Identifier = 1,
                        Id = new Uri($"https://example.com/place/{placeId}"),
                        Name = "Post-ercise Plaza",
                        Description = "Sorting Out Your Fitness One Parcel Lift at a Time! Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
                        Address = new PostalAddress
                        {
                            StreetAddress = "Kings Mead House",
                            AddressLocality = "Oxford",
                            AddressRegion = "Oxfordshire",
                            PostalCode = "OX1 1AA",
                            AddressCountry = "GB"
                        },
                        Geo = new GeoCoordinates
                        {
                            Latitude = (decimal?)51.7502,
                            Longitude = (decimal?)-1.2674
                        },
                        Image = new List<ImageObject> {
                            new ImageObject
                            {
                                Url = new Uri("https://upload.wikimedia.org/wikipedia/commons/e/e5/Oxford_StAldates_PostOffice.jpg")
                            },
                        },
                        Telephone = "01865 000001",
                        Url = new Uri("https://en.wikipedia.org/wiki/Post_Office_Limited"),
                        AmenityFeature = new List<LocationFeatureSpecification>
                        {
                            new ChangingFacilities { Name = "Changing Facilities", Value = true },
                            new Showers { Name = "Showers", Value = true },
                            new Lockers { Name = "Lockers", Value = true },
                            new Towels { Name = "Towels", Value = false },
                            new Creche { Name = "Creche", Value = false },
                            new Parking { Name = "Parking", Value = false }
                        },
                        OpeningHoursSpecification = new List<OpeningHoursSpecification>
                        {
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Sunday }, Opens = "09:00", Closes = "17:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Monday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Tuesday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Wednesday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Thursday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Friday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Saturday }, Opens = "09:00", Closes = "17:30"}
                        }
                    };
                case 2:
                    return new Place
                    {
                        Identifier = 2,
                        Id = new Uri($"https://example.com/place/{placeId}"),
                        Name = "Premier Lifters",
                        Description = "Where your Fitness Goals are Always Inn-Sight. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
                        Address = new PostalAddress
                        {
                            StreetAddress = "Greyfriars Court, Paradise Square",
                            AddressLocality = "Oxford",
                            AddressRegion = "Oxfordshire",
                            PostalCode = "OX1 1BB",
                            AddressCountry = "GB"
                        },
                        Geo = new GeoCoordinates
                        {
                            Latitude = (decimal?)51.7504933,
                            Longitude = (decimal?)-1.2620685
                        },
                        Image = new List<ImageObject> {
                            new ImageObject
                            {
                                Url = new Uri("https://upload.wikimedia.org/wikipedia/commons/5/53/Cambridge_Orchard_Park_Premier_Inn.jpg")
                            },
                        },
                        Telephone = "01865 000002",
                        Url = new Uri("https://en.wikipedia.org/wiki/Premier_Inn"),
                        AmenityFeature = new List<LocationFeatureSpecification>
                        {
                            new ChangingFacilities { Name = "Changing Facilities", Value = false },
                            new Showers { Name = "Showers", Value = false },
                            new Lockers { Name = "Lockers", Value = false },
                            new Towels { Name = "Towels", Value = true },
                            new Creche { Name = "Creche", Value = true },
                            new Parking { Name = "Parking", Value = true }
                        },
                        OpeningHoursSpecification = new List<OpeningHoursSpecification>
                        {
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Sunday }, Opens = "09:00", Closes = "17:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Monday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Tuesday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Wednesday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Thursday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Friday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Saturday }, Opens = "09:00", Closes = "17:30"}
                        }
                    };
                case 3:
                    return new Place
                    {
                        Identifier = 3,
                        Id = new Uri($"https://example.com/place/{placeId}"),
                        Name = "Stroll & Stretch",
                        Description = "Casual Calisthenics in the Heart of Commerce. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
                        Address = new PostalAddress
                        {
                            StreetAddress = "Norfolk Street",
                            AddressLocality = "Oxford",
                            AddressRegion = "Oxfordshire",
                            PostalCode = "OX1 1UU",
                            AddressCountry = "GB"
                        },
                        Geo = new GeoCoordinates
                        {
                            Latitude = (decimal?)51.749826,
                            Longitude = (decimal?)-1.261492
                        },
                        Image = new List<ImageObject> {
                            new ImageObject
                            {
                                Url = new Uri("https://upload.wikimedia.org/wikipedia/commons/2/28/Westfield_Garden_State_Plaza_-_panoramio.jpg")
                            },
                        },
                        Telephone = "01865 000003",
                        Url = new Uri("https://en.wikipedia.org/wiki/Shopping_center"),
                        OpeningHoursSpecification = new List<OpeningHoursSpecification>
                        {
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Sunday }, Opens = "09:00", Closes = "17:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Monday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Tuesday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Wednesday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Thursday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Friday }, Opens = "06:30", Closes = "21:30"},
                            new OpeningHoursSpecification {DayOfWeek = new List<Schema.NET.DayOfWeek> {Schema.NET.DayOfWeek.Saturday }, Opens = "09:00", Closes = "17:30"}
                        }
                    };
                default:
                    return null;
            }
        }

        public static List<OpenBookingFlowRequirement> OpenBookingFlowRequirement(bool requiresApproval, bool requiresAttendeeValidation, bool requiresAdditionalDetails, bool allowsProposalAmendment)
        {
            List<OpenBookingFlowRequirement> openBookingFlowRequirement = null;

            if (requiresApproval)
            {
                openBookingFlowRequirement = openBookingFlowRequirement ?? new List<OpenBookingFlowRequirement>();
                openBookingFlowRequirement.Add(OpenActive.NET.OpenBookingFlowRequirement.OpenBookingApproval);
            }

            if (requiresAttendeeValidation)
            {
                openBookingFlowRequirement = openBookingFlowRequirement ?? new List<OpenBookingFlowRequirement>();
                openBookingFlowRequirement.Add(OpenActive.NET.OpenBookingFlowRequirement.OpenBookingAttendeeDetails);
            }

            if (requiresAdditionalDetails)
            {
                openBookingFlowRequirement = openBookingFlowRequirement ?? new List<OpenBookingFlowRequirement>();
                openBookingFlowRequirement.Add(OpenActive.NET.OpenBookingFlowRequirement.OpenBookingIntakeForm);
            }

            if (allowsProposalAmendment)
            {
                openBookingFlowRequirement = openBookingFlowRequirement ?? new List<OpenBookingFlowRequirement>();
                openBookingFlowRequirement.Add(OpenActive.NET.OpenBookingFlowRequirement.OpenBookingNegotiation);
            }
            return openBookingFlowRequirement;
        }

        public static string GenerateAttendeeInstructions(Faker faker, bool isGoldenRecord)
        {
            var listOfPossibleInstructions = new List<string>(){
                "wear sportswear/gym clothes",
                "wear comfortable loose clothing",
                "come as you are",
                "bring trainers",
                "wear flat shoes",
                "no footwear required"
            };

            return $"Clothing instructions: {string.Join(", ", GetRandomElementsOf(faker, listOfPossibleInstructions, isGoldenRecord, 1))}";
        }

        public static List<Concept> GenerateAccessibilitySupport(Faker faker, bool isGoldenRecord)
        {
            var listOfAccessibilitySupports = new List<Concept>
            {
                new Concept {Id = new Uri("https://openactive.io/accessibility-support#1393f2dc-3fcc-4be9-a99f-f1e51f5ad277"), PrefLabel = "Visual Impairment",  InScheme = new Uri("https://openactive.io/accessibility-support")},
                new Concept {Id = new Uri("https://openactive.io/accessibility-support#2bfb7228-5969-4927-8435-38b5005a8771"), PrefLabel = "Hearing Impairment",  InScheme = new Uri("https://openactive.io/accessibility-support")},
                new Concept {Id = new Uri("https://openactive.io/accessibility-support#40b9b11f-bdd3-4aeb-8984-2ecf74a14c7a"), PrefLabel = "Mental health issues",  InScheme = new Uri("https://openactive.io/accessibility-support")}
            };

            return GetRandomElementsOf(faker, listOfAccessibilitySupports, isGoldenRecord, 1, 2).ToList();
        }

        public static List<ImageObject> GenerateImages(Faker faker, bool isGoldenRecord)
        {
            static Uri GenerateImageUrl(int width, int height, int seed)
            {
                return new Uri($"https://picsum.photos/{width}/{height}?image={seed}");
            }

            var images = new List<ImageObject>();
            var min = isGoldenRecord ? 4 : 1;
            var imageCount = faker.Random.Number(min, 3);
            for (var i = 0; i < imageCount; i++)
            {
                var imageSeed = faker.Random.Number(1083);
                var thumbnails = new List<ImageObject> {
                    new ImageObject{Url = GenerateImageUrl(672, 414, imageSeed), Width = 672, Height = 414},
                    new ImageObject{Url = GenerateImageUrl(300, 200, imageSeed), Width = 300, Height = 200},
                    new ImageObject{Url = GenerateImageUrl(100, 100, imageSeed), Width = 100, Height = 100}
                };
                var image = new ImageObject
                {
                    Url = GenerateImageUrl(1024, 724, imageSeed),
                    Thumbnail = GetRandomElementsOf(faker, thumbnails, isGoldenRecord, 1, 1).ToList()
                };
                images.Add(image);
            }
            return images;
        }

        public static Organization GenerateOrganization(Faker faker, SellerTable seller, bool isSingleSeller, Uri organizationId)
        {
            if (isSingleSeller)
                return new Organization
                {
                    Id = organizationId,
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
                    SameAs = new List<Uri> { new Uri("https://socialmedia.com/testseller") }
                };

            return new Organization
            {
                Id = organizationId,
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
                SameAs = new List<Uri> { new Uri($"https://socialmedia.com/{seller.Name.Replace(" ", "")}") }
            };


        }

    }
}
