using OpenActive.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenActive.Server.NET.StoreBooking;
using OpenActive.DatasetSite.NET;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    public static class OrderCalculations
    {
        private static readonly IDictionary<string, PropertyInfo> PersonAttributes;

        static OrderCalculations()
        {
            var attributes = from property in typeof(Person).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             where property.DeclaringType == typeof(Person)
                             let name = property.GetCustomAttributes<DataMemberAttribute>().SingleOrDefault()?.Name
                             where name != null
                             select new { name, property };
            PersonAttributes = attributes.ToDictionary(t => t.name, t => t.property);
        }

        public static IncompleteAttendeeDetailsError ValidateAttendeeDetails(OrderItem responseOrderItem, FlowStage flowStage)
        {
            // Validation is needed for C2, B, and P
            if (flowStage == FlowStage.C1)
                return null;

            if (responseOrderItem?.AttendeeDetailsRequired == null)
                return null;

            if (responseOrderItem.Attendee == null)
                return new IncompleteAttendeeDetailsError();

            var values = (from namespacedName in responseOrderItem.AttendeeDetailsRequired
                          let titleCasedName = namespacedName.ToString().Split('.').Last()
                          let name = char.ToLowerInvariant(titleCasedName[0]) + titleCasedName.Substring(1)
                          let property = PersonAttributes[name]
                          let value = property.GetValue(responseOrderItem.Attendee)
                          select value).ToArray();

            if (values.Length != responseOrderItem.AttendeeDetailsRequired.Count || values.Any(v => v == null))
                return new IncompleteAttendeeDetailsError();

            return null;
        }

        public static List<OpenBookingError> ValidateAdditionalDetails(OrderItem responseOrderItem, FlowStage flowStage)
        {
            var validationErrorArray = new List<OpenBookingError>();
            // Validation is needed for C2, B, and P
            if (flowStage == FlowStage.C1)
                return validationErrorArray;

            var properties = responseOrderItem?.OrderItemIntakeForm;
            if (properties == null)
                return validationErrorArray;

            var values = responseOrderItem.OrderItemIntakeFormResponse;

            foreach (var property in properties)
            {
                var required = false;
                if (property is BooleanFormFieldSpecification)
                {
                    required = true;
                }
                else
                {
                    required = property.ValueRequired ?? false;
                }
                if (required && values == null)
                {
                    var error = new IncompleteIntakeFormError();
                    error.Instance = property.Id;
                    error.Description = "Incomplete additional details supplied";
                    validationErrorArray.Add(error);
                    continue;
                }

                var correspondingValues = values.Where(value => value.PropertyID == property.Id).ToArray();
                if (correspondingValues.Length > 1)
                {
                    var error = new InvalidIntakeFormError();
                    error.Instance = property.Id;
                    error.Description = $"More than one Response provided for {property.Id}";
                    validationErrorArray.Add(error);
                    continue;
                }

                var correspondingValue = correspondingValues.SingleOrDefault();
                if (required && correspondingValue == null)
                {
                    var error = new IncompleteIntakeFormError();
                    error.Instance = property.Id;
                    error.Description = "Incomplete additional details supplied";
                    validationErrorArray.Add(error);
                    continue;
                }

                if (!required && correspondingValue == null)
                    continue;

                switch (property)
                {
                    case DropdownFormFieldSpecification p when !p.ValueOption.Contains(correspondingValue.Value.Value):
                        {
                            var error = new InvalidIntakeFormError();
                            error.Instance = property.Id;
                            error.Description = "Value provided is not one of ValueOptions provided";
                            validationErrorArray.Add(error);
                            break;
                        }
                    case BooleanFormFieldSpecification _ when !correspondingValue.Value.HasValueOfType<bool?>():
                        {
                            var error = new InvalidIntakeFormError();
                            error.Instance = property.Id;
                            error.Description = "Value provided is not a boolean";
                            validationErrorArray.Add(error);
                            break;
                        }
                    case ShortAnswerFormFieldSpecification _ when !correspondingValue.Value.HasValueOfType<string>():
                        {
                            var error = new InvalidIntakeFormError();
                            error.Instance = property.Id;
                            error.Description = "Value provided is not a string";
                            validationErrorArray.Add(error);
                        }
                        break;
                    case FileUploadFormFieldSpecification _ when !correspondingValue.Value.HasValueOfType<Uri>():
                        {
                            var error = new InvalidIntakeFormError();
                            error.Instance = property.Id;
                            error.Description = "Value provided is not a Url";
                            validationErrorArray.Add(error);
                        }
                        break;
                }
            }
            return validationErrorArray;
        }

        public static Event RenderOpportunityWithOnlyId(OpportunityType opportunityType, Uri id)
        {
            // TODO: Create an extra prop in DatasetSite lib so that we don't need to parse the URL here
            switch (OpportunityTypes.Configurations[opportunityType].SameAs.AbsolutePath.Trim('/'))
            {
                case nameof(Event):
                    return new Event { Id = id };
                case nameof(ScheduledSession):
                    return new ScheduledSession { Id = id };
                case nameof(HeadlineEvent):
                    return new HeadlineEvent { Id = id };
                case nameof(Slot):
                    return new Slot { Id = id };
                case nameof(CourseInstance):
                    return new CourseInstance { Id = id };
                case nameof(OnDemandEvent):
                    return new OnDemandEvent { Id = id };
                default:
                    return null;
            }
        }

        public static TaxChargeSpecification AddTaxes(TaxChargeSpecification x, TaxChargeSpecification y)
        {
            // If one is null, return the other. If both are null, return null.
            if (x == null || y == null) return x ?? y;

            // Check that taxes are compatible
            if (x.Name != y.Name)
            {
                throw new ArgumentException("Different types of taxes cannot be added together");
            }
            if (x.Rate != y.Rate)
            {
                throw new ArgumentException("Taxes with the same name must have the same rate");
            }
            if (x.Identifier != y.Identifier)
            {
                throw new ArgumentException("Taxes with the same name must have the same identifier");
            }
            if (x.PriceCurrency != y.PriceCurrency)
            {
                throw new ArgumentException("Taxes with the same name must have the same currency");
            }

            // If compatible, return the sum
            return new TaxChargeSpecification
            {
                Name = x.Name,
                Price = x.Price + y.Price,
                PriceCurrency = x.PriceCurrency,
                Rate = x.Rate,
                Identifier = x.Identifier
            };
        }

        public static void AugmentOrderWithTotals<TOrder>(
            TOrder order, StoreBookingFlowContext context, bool businessToConsumerTaxCalculation, bool businessToBusinessTaxCalculation)
            where TOrder : Order
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            // Calculate total payment due
            decimal totalPaymentDuePrice = 0;
            string totalPaymentDueCurrency = null;
            var totalPaymentTaxMap = new Dictionary<string, TaxChargeSpecification>();

            foreach (OrderItem orderedItem in order.OrderedItem)
            {
                // Only items with no errors associated are included in the total price
                if (!(orderedItem.Error?.Count > 0))
                {
                    // Keep track of total price
                    totalPaymentDuePrice += orderedItem.AcceptedOffer.Object.Price ?? 0;

                    // Set currency based on first item
                    if (totalPaymentDueCurrency == null)
                    {
                        totalPaymentDueCurrency = orderedItem.AcceptedOffer.Object.PriceCurrency;
                    }
                    else if (totalPaymentDueCurrency != orderedItem.AcceptedOffer.Object.PriceCurrency)
                    {
                        throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "All currencies in an Order must match");
                    }

                    // Add the taxes to the map
                    if (orderedItem.UnitTaxSpecification != null)
                    {
                        foreach (TaxChargeSpecification taxChargeSpecification in orderedItem.UnitTaxSpecification)
                        {
                            if (totalPaymentTaxMap.TryGetValue(taxChargeSpecification.Name, out TaxChargeSpecification currentTaxValue))
                            {
                                totalPaymentTaxMap[taxChargeSpecification.Name] = AddTaxes(currentTaxValue, taxChargeSpecification);
                            }
                            else
                            {
                                totalPaymentTaxMap[taxChargeSpecification.Name] = taxChargeSpecification;
                            }
                        }
                    }
                }
            }

            switch (context.TaxPayeeRelationship)
            {
                case TaxPayeeRelationship.BusinessToBusiness when businessToBusinessTaxCalculation:
                case TaxPayeeRelationship.BusinessToConsumer when businessToConsumerTaxCalculation:
                    order.TotalPaymentTax = totalPaymentTaxMap.Values.ToListOrNullIfEmpty();
                    break;
                case TaxPayeeRelationship.BusinessToBusiness:
                case TaxPayeeRelationship.BusinessToConsumer:
                    if (order.OrderedItem.Any(o => o.UnitTaxSpecification != null))
                        throw new OpenBookingException(new InternalLibraryConfigurationError());

                    order.TotalPaymentTax = null;
                    order.TaxCalculationExcluded = true;
                    break;
            }

            // If we're in Net taxMode, tax must be added to get the total price
            if (order.Seller.Object.TaxMode == TaxMode.TaxNet)
            {
                totalPaymentDuePrice += order.TotalPaymentTax.Sum(x => x.Price ?? 0);
            }

            order.TotalPaymentDue = new PriceSpecification
            {
                Price = totalPaymentDuePrice,
                PriceCurrency = totalPaymentDueCurrency
            };

            OrderCalculations.AugmentOrderWithCalculations(
                order, context, businessToConsumerTaxCalculation, businessToBusinessTaxCalculation);
        }

        public static void AugmentOrderWithCalculations<TOrder>(
            TOrder order, StoreBookingFlowContext context, bool businessToConsumerTaxCalculation, bool businessToBusinessTaxCalculation)
            where TOrder : Order
        {
            order.TotalPaymentDue.OpenBookingPrepayment = GetRequiredStatusType(order.OrderedItem);
        }

        public static RequiredStatusType? GetRequiredStatusType(IReadOnlyCollection<OrderItem> orderItems)
        {
            if (orderItems.Any(x => x.AcceptedOffer.Object?.OpenBookingPrepayment == RequiredStatusType.Required ||
                                             x.AcceptedOffer.Object?.Price != 0 && x.AcceptedOffer.Object?.OpenBookingPrepayment == null))
                return RequiredStatusType.Required;

            if (orderItems.Any(x => x.AcceptedOffer.Object?.OpenBookingPrepayment == RequiredStatusType.Optional) &&
                orderItems.All(x => x.AcceptedOffer.Object?.OpenBookingPrepayment == RequiredStatusType.Optional ||
                                             x.AcceptedOffer.Object?.OpenBookingPrepayment == RequiredStatusType.Unavailable ||
                                             x.AcceptedOffer.Object?.Price == 0 && x.AcceptedOffer.Object?.OpenBookingPrepayment == null))
                return RequiredStatusType.Optional;

            if (orderItems.All(x => x.AcceptedOffer.Object?.OpenBookingPrepayment == RequiredStatusType.Unavailable ||
                                             x.AcceptedOffer.Object?.Price == 0))
                return RequiredStatusType.Unavailable;

            return null;
        }
    }
}
