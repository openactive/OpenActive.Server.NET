using OpenActive.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenActive.Server.NET.StoreBooking;

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

        public static void ValidateAttendeeDetails(OrderItem requestOrderItem, OrderItem responseOrderItem)
        {
            if (responseOrderItem?.AttendeeDetailsRequired == null)
                return;

            if (responseOrderItem.Attendee == null)
                throw new OpenBookingException(new IncompleteAttendeeDetailsError());

            var values = (from uri in responseOrderItem.AttendeeDetailsRequired
                          let name = uri.ToString().Split('/').Last()
                          let property = PersonAttributes[name]
                          let value = property.GetValue(responseOrderItem.Attendee)
                          select value).ToArray();

            if (values.Length != responseOrderItem.AttendeeDetailsRequired.Count || values.Any(v => v == null))
                throw new OpenBookingException(new IncompleteAttendeeDetailsError());
        }

        public static void ValidateAdditionalDetails(OrderItem requestOrderItem, OrderItem responseOrderItem)
        {
            var properties = responseOrderItem?.OrderItemIntakeForm;
            if (properties == null)
                return;

            var values = responseOrderItem.OrderItemIntakeFormResponse;
            if (values == null)
                throw new OpenBookingException(new IncompleteAttendeeDetailsError());

            foreach (var property in properties)
            {
                var correspondingValues = values.Where(value => value.PropertyID == property.Id).ToArray();
                if (correspondingValues.Length > 1)
                    throw new OpenBookingException(new InvalidIntakeFormError());

                var correspondingValue = correspondingValues.SingleOrDefault();
                var required = property.ValueRequired ?? false;
                if (required && correspondingValue == null)
                    throw new OpenBookingException(new IncompleteAttendeeDetailsError());

                if (!required && correspondingValue == null)
                    continue;

                switch (property.Type)
                {
                    case "DropdownFormFieldSpecification" when !((DropdownFormFieldSpecification)property).ValueOption.Contains(correspondingValue.Value.Value):
                        throw new OpenBookingException(new InvalidIntakeFormError());
                    case "BooleanFormFieldSpecification" when !bool.TryParse((string)correspondingValue.Value.Value, out _):
                        throw new OpenBookingException(new InvalidIntakeFormError());
                }
            }
        }

        public static Event RenderOpportunityWithOnlyId(string jsonLdType, Uri id)
        {
            switch (jsonLdType)
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
                PriceCurrency = totalPaymentDueCurrency,
                Prepayment = GetRequiredStatusType(order.OrderedItem)
            };
        }

        private static RequiredStatusType? GetRequiredStatusType(IReadOnlyCollection<OrderItem> orderItems)
        {
            if (orderItems.Any(x => x.AcceptedOffer.Object.Prepayment == RequiredStatusType.Required ||
                                             x.AcceptedOffer.Object.Price != 0 && x.AcceptedOffer.Object.Prepayment == null))
                return RequiredStatusType.Required;

            if (orderItems.Any(x => x.AcceptedOffer.Object.Prepayment == RequiredStatusType.Optional) &&
                orderItems.All(x => x.AcceptedOffer.Object.Prepayment == RequiredStatusType.Optional ||
                                             x.AcceptedOffer.Object.Prepayment == RequiredStatusType.Unavailable ||
                                             x.AcceptedOffer.Object.Price == 0 && x.AcceptedOffer.Object.Prepayment == null))
                return RequiredStatusType.Optional;

            if (orderItems.All(x => x.AcceptedOffer.Object.Prepayment == RequiredStatusType.Unavailable ||
                                             x.AcceptedOffer.Object.Price == 0))
                return RequiredStatusType.Unavailable;

            return null;
        }
    }
}
