using System;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using System.Collections.Generic;

namespace BookingSystem
{
    public static class PropertyValueSpecificationHelper
    {

        public static List<PropertyValueSpecification> HydrateAdditionalDetailsIntoPropertyValueSpecifications(List<AdditionalDetailTypes> requiredAdditionalDetails)
        {
            var hydratedAdditionalDetails = new List<PropertyValueSpecification>();
            foreach (AdditionalDetailTypes additionalDetail in requiredAdditionalDetails)
            {
                switch (additionalDetail)
                {
                    case AdditionalDetailTypes.Age:
                        {
                            hydratedAdditionalDetails.Add(new DropdownFormFieldSpecification()
                            {
                                Id = new Uri("https://example.com/age"),
                                Name = "Age",
                                Description = "Your age is useful for us to place you in the correct group on the day",
                                ValueOption = new List<string>() { "0-18", "18-30", "30+" },
                                ValueRequired = true
                            });
                        }
                        break;
                    case AdditionalDetailTypes.Experience:
                        {
                            hydratedAdditionalDetails.Add(new ShortAnswerFormFieldSpecification()
                            {
                                Id = new Uri("https://example.com/experience"),
                                Name = "Experience",
                                Description = "Have you played before? Are you a complete beginner or seasoned pro?",
                                ValueRequired = true
                            });
                        }
                        break;
                    case AdditionalDetailTypes.Gender:
                        {
                            hydratedAdditionalDetails.Add(new DropdownFormFieldSpecification()
                            {
                                Id = new Uri("https://example.com/gender"),
                                Name = "Gender",
                                Description = "We use this information for equality and diversity monitoring",
                                ValueOption = new List<string>() { "Male", "Female", "Non-Binary", "Other" },
                                ValueRequired = true
                            });
                        }
                        break;
                    case AdditionalDetailTypes.PhotoConsent:
                        {
                            hydratedAdditionalDetails.Add(new BooleanFormFieldSpecification()
                            {
                                Id = new Uri("https://example.com/photoconsent"),
                                Name = "Photo Consent",
                                Description = "Are you happy for us to include photos of you in our marketing materials?"
                            });
                        }
                        break;
                    case AdditionalDetailTypes.FileUpload:
                        {
                            hydratedAdditionalDetails.Add(new FileUploadFormFieldSpecification()
                            {
                                Id = new Uri("https://example.com/insurance"),
                                Name = "PLI Certificate",
                                Description = "Please upload your PLI certificate",
                                ValueRequired = true
                            });
                        }
                        break;
                }
            }

            return hydratedAdditionalDetails;
        }
    }
}
