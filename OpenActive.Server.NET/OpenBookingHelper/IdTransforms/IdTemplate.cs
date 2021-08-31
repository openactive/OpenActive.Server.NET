using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenActive.NET;
using OpenActive.DatasetSite.NET;
using System.Runtime.Serialization;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    /// <summary>
    /// Class to represent unrecognised OrderItems
    /// </summary>
    public class NullBookableIdComponents : IBookableIdComponents
    {
        public OpportunityType? OpportunityType { get => null; set => throw new NotImplementedException(); }
    }

    public interface IBookableIdComponents
    {
        OpportunityType? OpportunityType { get; set; }
    }

    public interface IBookableIdComponentsWithInheritance : IBookableIdComponents
    {
        OpportunityType? OfferOpportunityType { get; set; }
    }

    public class RequiredBaseUrlMismatchException : Exception
    {
        public RequiredBaseUrlMismatchException()
        {
        }

        public RequiredBaseUrlMismatchException(string message)
            : base(message)
        {
        }

        public RequiredBaseUrlMismatchException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class BookableOpportunityAndOfferMismatchException : Exception
    {
        public BookableOpportunityAndOfferMismatchException()
        {
        }

        public BookableOpportunityAndOfferMismatchException(string message)
            : base(message)
        {
        }

        public BookableOpportunityAndOfferMismatchException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class ComponentFailedToParseException : Exception
    {
        public ComponentFailedToParseException()
        {
        }

        public ComponentFailedToParseException(string message)
            : base(message)
        {
        }

        public ComponentFailedToParseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public interface IBookablePairIdTemplate
    {
        //OpportunityIdConfiguration OpportunityIdConfiguration { get;  }
        //OpportunityIdConfiguration? ParentIdConfiguration { get; }
        //OpportunityIdConfiguration? GrandparentIdConfiguration { get; }
        List<OpportunityIdConfiguration> IdConfigurations { get; }

        IBookableIdComponents GetOpportunityReference(Uri opportunityId, Uri offerId);
        IBookableIdComponents GetOpportunityBookableIdComponents(Uri opportunityId);

        Uri RequiredBaseUrl { get; set; }

        //Uri RenderOfferId(OpportunityType opportunityType, IBookableIdComponents components);
        //Uri RenderOpportunityId(OpportunityType opportunityType, IBookableIdComponents components);

    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "<Pending>")]
    public class BookablePairIdTemplateWithOfferInheritance<TBookableIdComponents> : BookablePairIdTemplate<TBookableIdComponents> where TBookableIdComponents : class, IBookableIdComponentsWithInheritance, new()
    {

        public BookablePairIdTemplateWithOfferInheritance(
            OpportunityIdConfiguration opportunityIdConfiguration,
            OpportunityIdConfiguration? parentIdConfiguration = null,
            OpportunityIdConfiguration? grandparentIdConfiguration = null)
            : base(false, opportunityIdConfiguration, parentIdConfiguration, grandparentIdConfiguration)
        {
            // ScheduledSession with SessionSeries is the only opportunity type that allows Offer inheritance within Modelling Specification 2.0
            // Therefore the check below ensures that this class is only used in accordance with the spec
            if (opportunityIdConfiguration.OpportunityType != OpportunityType.ScheduledSession)
            {
                throw new NotSupportedException($"{nameof(BookablePairIdTemplateWithOfferInheritance<TBookableIdComponents>)} used with unsupported {nameof(OpportunityType)} pair. ScheduledSession (from SessionSeries) is the only opportunity type that allows Offer inheritance within Modelling Specification 2.0. Please use {nameof(BookablePairIdTemplate<TBookableIdComponents>)}.");
            }

        }

        /// <summary>
        /// This is used by the booking engine to resolve an OrderItem to its components, using only opportunityId and Uri offerId
        /// </summary>
        /// <param name="opportunityId"></param>
        /// <param name="offerId"></param>
        /// <returns>Null if either ID does not match the template for the Opportunity, with its own Offer or the Offer of its parent</returns>
        public override IBookableIdComponents GetOpportunityReference(Uri opportunityId, Uri offerId)
        {
            // Require both opportunityId and offerId to not be null
            if (opportunityId == null) throw new ArgumentNullException(nameof(opportunityId));
            // if (offerId == null) throw new ArgumentNullException(nameof(offerId));

            // As inheritance is in use, the Offer must be resolved against either: Opportunity with Offer; or Opportunity and _parent_ Offer
            // Note in OpenActive Modelling Specification 2.0 this behaviour is only applicable to SessionSeries and ScheduledSession
            // Note the grandparent (e.g. EventSeries) is never bookable

            // TODO: Make this check for this.OpportunityIdConfiguration.Bookable == true

            return GetIdComponentsWithOpportunityTypeAndInheritanceExt(this.OpportunityIdConfiguration.OpportunityType, this.OpportunityIdConfiguration.OpportunityType, opportunityId, offerId, null, null)
            ?? GetIdComponentsWithOpportunityTypeAndInheritanceExt(this.OpportunityIdConfiguration.OpportunityType, this.ParentIdConfiguration?.OpportunityType, opportunityId, null, null, offerId);
        }


        // Note this method exists just for type conversion to work as this is not C# 8.0
        private IBookableIdComponents GetIdComponentsWithOpportunityTypeAndInheritanceExt(OpportunityType? opportunityType, OpportunityType? orderOpportunityType, params Uri[] ids)
        {
            return this.GetIdComponentsWithOpportunityTypeAndInheritance(opportunityType, orderOpportunityType, ids);
        }

        protected TBookableIdComponents GetIdComponentsWithOpportunityTypeAndInheritance(OpportunityType? opportunityType, OpportunityType? orderOpportunityType, params Uri[] ids)
        {
            TBookableIdComponents components = base.GetIdComponentsWithOpportunityType(opportunityType, ids);
            if (components != null)
            {
                if (!orderOpportunityType.HasValue) throw new ArgumentNullException("Unexpected match with invalid order OpportunityIdConfiguration.");
                components.OfferOpportunityType = orderOpportunityType.Value;
                return components;
            }
            else
            {
                return null;
            }
        }
    }

    public struct OpportunityIdConfiguration
    {
        public OpportunityType OpportunityType { get; set; }
        public OpportunityType AssignedFeed { get; set; }
        public string OpportunityIdTemplate { get; set; }
        public string OfferIdTemplate { get; set; }
        public bool Bookable { get; set; }
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "<Pending>")]
    public class BookablePairIdTemplate<TBookableIdComponents> : IdTemplate<TBookableIdComponents>, IBookablePairIdTemplate where TBookableIdComponents : class, IBookableIdComponents, new()
    {
        public BookablePairIdTemplate(
            OpportunityIdConfiguration opportunityIdConfiguration,
            OpportunityIdConfiguration? parentIdConfiguration = null,
            OpportunityIdConfiguration? grandparentIdConfiguration = null)
            : this(false, opportunityIdConfiguration, parentIdConfiguration, grandparentIdConfiguration)
        {
            // ScheduledSession with SessionSeries is the only opportunity type that allows Offer inheritance within Modelling Specification 2.0
            // Therefore the check below ensures that this class is only used in accordance with the spec
            if (opportunityIdConfiguration.OpportunityType == OpportunityType.ScheduledSession)
            {
                throw new NotSupportedException($"{nameof(BookablePairIdTemplate<TBookableIdComponents>)} used with unsupported {nameof(OpportunityType)} pair. ScheduledSession with SessionSeries are the only opportunity types that allows Offer inheritance within Modelling Specification 2.0. Please use BookablePairIdTemplateWithOfferInheritance<IBookableIdComponentsWithInheritance>.");
            }
        }

        protected BookablePairIdTemplate(bool overrideSessionSeriesCheck,
            OpportunityIdConfiguration opportunityIdConfiguration,
            OpportunityIdConfiguration? parentIdConfiguration,
            OpportunityIdConfiguration? grandparentIdConfiguration)
            : base(opportunityIdConfiguration.OpportunityIdTemplate, opportunityIdConfiguration.OfferIdTemplate,
                  parentIdConfiguration?.OpportunityIdTemplate, parentIdConfiguration?.OfferIdTemplate,
                  grandparentIdConfiguration?.OpportunityIdTemplate, grandparentIdConfiguration?.OfferIdTemplate)
        {


            // SH-TODO: Add more code here to validate the combinations of child/parent/grandparent OpportunityType based on the Parent relationship
            // in OpportunityTypes.Configuration. Throw an error if the user attempts to create something invalid.
            // Also check that anything that's set as Bookable = true, at least ahs Bookable = true in OpportunityTypes.Configuration

            this.OpportunityIdConfiguration = opportunityIdConfiguration;
            this.ParentIdConfiguration = parentIdConfiguration;
            this.GrandparentIdConfiguration = grandparentIdConfiguration;

            // Create list to simplify access
            var list = new List<OpportunityIdConfiguration> { this.OpportunityIdConfiguration };
            if (this.ParentIdConfiguration.HasValue) list.Add(this.ParentIdConfiguration.Value);
            if (this.GrandparentIdConfiguration.HasValue) list.Add(this.GrandparentIdConfiguration.Value);
            this.IdConfigurations = list;
        }


        protected OpportunityIdConfiguration OpportunityIdConfiguration { get; }
        protected OpportunityIdConfiguration? ParentIdConfiguration { get; }
        protected OpportunityIdConfiguration? GrandparentIdConfiguration { get; }
        public List<OpportunityIdConfiguration> IdConfigurations { get; }

        /// <summary>
        /// This is used by the booking engine to resolve an OrderItem to its components, using only opportunityId and Uri offerId
        /// </summary>
        /// <param name="opportunityId"></param>
        /// <param name="offerId"></param>
        /// <returns>Null if either ID does not match the template</returns>
        public virtual IBookableIdComponents GetOpportunityReference(Uri opportunityId, Uri offerId)
        {
            // Design decision: this is virtual as the params are derived directly from the Open Booking API
            // and so are unlikely to change. Noting risks around maintaining a virtual method in a public library.

            // Require both opportunityId and offerId to not be null
            if (opportunityId == null) throw new ArgumentNullException(nameof(opportunityId));
            // if (offerId == null) throw new ArgumentNullException(nameof(offerId));

            // Without inheritance, the Offer must be resolved against either: Opportunity with Offer; or _parent_ Opportunity and parent Offer
            // Note that if any URL templates to be used for one of the checks below are null, the result for that check will be null
            // TODO: Document that this library purposely does not support Event with subEvent of Event, as this is not recommended in Model 2.0
            // and has many edge cases
            // Note the grandparent is never bookable

            return GetIdComponentsWithOpportunityTypeExt(this.OpportunityIdConfiguration.OpportunityType, opportunityId, offerId, null, null)
                ?? GetIdComponentsWithOpportunityTypeExt(this.ParentIdConfiguration?.OpportunityType, null, null, opportunityId, offerId);
        }

        /// <summary>
        /// This is used by the booking engine to resolve a bookable Opportunity ID to its components
        /// </summary>
        /// <param name="opportunityId"></param>
        /// <returns>Null if the ID does not match the template</returns>
        public TBookableIdComponents GetOpportunityBookableIdComponents(Uri opportunityId)
        {
            // Require opportunityId to not be null
            if (opportunityId == null) throw new ArgumentNullException(nameof(opportunityId));

            // This method not effected by inheritance, and will return the Opportunity or _parent_ Opportunity, if either are bookable
            // Note that if any URL templates to be used for one of the checks below are null, the result for that check will be null
            // Note the grandparent is never bookable

            return
                (
                this.OpportunityIdConfiguration.Bookable && OpportunityTypes.Configurations[this.OpportunityIdConfiguration.OpportunityType].Bookable ?
                    GetIdComponentsWithOpportunityType(this.OpportunityIdConfiguration.OpportunityType, opportunityId, null, null, null) : null
                    )
                ?? (
                this.ParentIdConfiguration.HasValue && this.ParentIdConfiguration.Value.Bookable && OpportunityTypes.Configurations[this.ParentIdConfiguration.Value.OpportunityType].Bookable ?
                    GetIdComponentsWithOpportunityType(this.ParentIdConfiguration?.OpportunityType, null, null, opportunityId, null) : null
                    )
                ;
        }

        IBookableIdComponents IBookablePairIdTemplate.GetOpportunityBookableIdComponents(Uri opportunityId)
        {
            return GetOpportunityBookableIdComponents(opportunityId);
        }



        // Note this method exists just for type conversion (from TBookableIdComponents to IBookableIdComponents) to work as this is not C# 8.0
        private IBookableIdComponents GetIdComponentsWithOpportunityTypeExt(OpportunityType? opportunityType, params Uri[] ids)
        {
            return this.GetIdComponentsWithOpportunityType(opportunityType, ids);
        }

        protected TBookableIdComponents GetIdComponentsWithOpportunityType(OpportunityType? opportunityType, params Uri[] ids)
        {
            var components = base.GetIdComponents((nameof(GetIdComponentsWithOpportunityType)), ids);
            if (components != null)
            {
                if (!opportunityType.HasValue) throw new ArgumentNullException("Unexpected match with invalid OpportunityIdConfiguration.");
                components.OpportunityType = opportunityType.Value;
                return components;
            }
            else
            {
                return null;
            }
        }

        public TBookableIdComponents GetIdComponents(Uri opportunityId)
        {
            return base.GetIdComponents(nameof(GetIdComponents), opportunityId);
        }
        public TBookableIdComponents GetIdComponents(Uri opportunityId, Uri offerId)
        {
            return base.GetIdComponents(nameof(GetIdComponents), opportunityId, offerId);
        }
        public TBookableIdComponents GetIdComponents(Uri opportunityId, Uri offerId, Uri parentOpportunityId)
        {
            return base.GetIdComponents(nameof(GetIdComponents), opportunityId, offerId, parentOpportunityId);
        }
        /*
         * Note: this is not provided as an option as it should never need to be used in a sane implementation of Open Booking API
        public T GetIdComponents(Uri opportunityId, Uri offerId, Uri parentOpportunityId, Uri parentOfferId)
        {
            return base.GetIdComponents(nameof(GetIdComponents), opportunityId, offerId, parentOpportunityId, parentOfferId);
        }*/

        // TODO: Fix strings below to include accurate error messages

        public Uri RenderOpportunityId(TBookableIdComponents components)
        {
            if (components == null || !components.OpportunityType.HasValue)
            {
                throw new ArgumentNullException("OpportunityType must be set on IBookableIdComponents");
            }
            return RenderOpportunityId(components.OpportunityType.Value, components);
        }

        public Uri RenderOpportunityId(OpportunityType opportunityType, TBookableIdComponents components)
        {
            if (components == null) throw new ArgumentNullException(nameof(components));

            if (opportunityType == OpportunityIdConfiguration.OpportunityType)
                return RenderId(0, components, nameof(RenderOpportunityId), "opportunityUriTemplate");
            else if (opportunityType == ParentIdConfiguration?.OpportunityType)
                return RenderId(2, components, nameof(RenderOpportunityId), "parentOpportunityUriTemplate");
            else if (opportunityType == GrandparentIdConfiguration?.OpportunityType)
                return RenderId(4, components, nameof(RenderOpportunityId), "parentOpportunityUriTemplate");
            else
                throw new ArgumentOutOfRangeException(nameof(opportunityType), "OpportunityType was not found within this template. Please check it is appropriate for this feed or OrderItem.");
        }

        public Uri RenderOfferId(TBookableIdComponents components)
        {
            // If inheritance is available on the IdComponent, ensure that it is respected (i.e. the correct offer is rendered, even if on the parent)
            switch (components)
            {
                case IBookableIdComponentsWithInheritance componentsWithInheritance:
                    if (componentsWithInheritance == null || !componentsWithInheritance.OfferOpportunityType.HasValue)
                    {
                        throw new ArgumentNullException(nameof(components), "OfferOpportunityType must be set on IBookableIdComponents when using RenderOfferId with inheritance enabled");
                    }
                    return RenderOfferId(componentsWithInheritance.OfferOpportunityType.Value, components);

                case IBookableIdComponents componentsWithoutInheritance:
                    if (componentsWithoutInheritance == null || !componentsWithoutInheritance.OpportunityType.HasValue)
                    {
                        throw new ArgumentNullException(nameof(components), "OpportunityType must be set on IBookableIdComponents");
                    }
                    return RenderOfferId(componentsWithoutInheritance.OpportunityType.Value, components);

                default:
                    throw new ArgumentOutOfRangeException(nameof(components), "Unexpected type mismatch for TBookableIdComponents,");
            }

        }

        public Uri RenderOfferId(OpportunityType opportunityType, TBookableIdComponents components)
        {
            if (opportunityType == OpportunityIdConfiguration.OpportunityType)
                return RenderId(1, components, nameof(RenderOfferId), "offerUriTemplate");
            else if (opportunityType == ParentIdConfiguration?.OpportunityType)
                return RenderId(3, components, nameof(RenderOfferId), "parentOfferUriTemplate");
            else if (opportunityType == GrandparentIdConfiguration?.OpportunityType)
                return RenderId(5, components, nameof(RenderOfferId), "parentOfferUriTemplate");
            else
                throw new ArgumentOutOfRangeException(nameof(opportunityType), "OpportunityType was not found within this template");
        }

        /*
        public Uri RenderOpportunityId(OpportunityType opportunityType, IBookableIdComponents components)
        {
            return RenderOpportunityId(opportunityType, (T)components);
        }

        public Uri RenderOfferId(OpportunityType opportunityType, IBookableIdComponents components)
        {
            return RenderOfferId(opportunityType, (T)components);
        }
        */

    }

    public class OrderIdTemplate : IdTemplate<OrderIdComponents>
    {
        public OrderIdTemplate(string orderIdTemplate, string orderItemIdTemplate) : base(orderIdTemplate, orderItemIdTemplate)
        {
            if (orderIdTemplate == null) throw new ArgumentNullException(nameof(orderIdTemplate));
            if (orderItemIdTemplate == null) throw new ArgumentNullException(nameof(orderItemIdTemplate));
        }

        public OrderIdComponents GetOrderIdComponents(string clientId, Uri id)
        {
            var orderId = base.GetIdComponents(nameof(GetIdComponents), id, null);
            if (orderId != null) orderId.ClientId = clientId;
            return orderId;
        }
        public OrderIdComponents GetOrderItemIdComponents(string clientId, Uri id)
        {
            var orderId = base.GetIdComponents(nameof(GetIdComponents), null, id);
            if (orderId != null) orderId.ClientId = clientId;
            return orderId;
        }

        // TODO: Later - check if RenderOrderId and RenderOrderItemId with multiple params can be moved back out to OrdersRPDEFeedGenerator?
        public Uri RenderOrderId(OrderType orderType, Guid uuid)
        {
            return this.RenderOrderId(new OrderIdComponents { OrderType = orderType, uuid = uuid });
        }

        //TODO reduce duplication of the strings / logic below
        public Uri RenderOrderItemId(OrderType orderType, Guid uuid, Guid orderItemId)
        {
            if (orderType != OrderType.Order) throw new ArgumentOutOfRangeException(nameof(orderType), "The Open Booking API 1.0 specification only permits OrderItem Ids to exist within Orders, not OrderQuotes or OrderProposals.");
            return this.RenderOrderItemId(new OrderIdComponents { OrderType = orderType, uuid = uuid, OrderItemIdGuid = orderItemId });
        }
        public Uri RenderOrderItemId(OrderType orderType, Guid uuid, string orderItemId)
        {
            if (orderType != OrderType.Order) throw new ArgumentOutOfRangeException(nameof(orderType), "The Open Booking API 1.0 specification only permits OrderItem Ids to exist within Orders, not OrderQuotes or OrderProposals.");
            return this.RenderOrderItemId(new OrderIdComponents { OrderType = orderType, uuid = uuid, OrderItemIdString = orderItemId });
        }
        public Uri RenderOrderItemId(OrderType orderType, Guid uuid, long orderItemId)
        {
            if (orderType != OrderType.Order) throw new ArgumentOutOfRangeException(nameof(orderType), "The Open Booking API 1.0 specification only permits OrderItem Ids to exist within Orders, not OrderQuotes or OrderProposals.");
            return this.RenderOrderItemId(new OrderIdComponents { OrderType = orderType, uuid = uuid, OrderItemIdLong = orderItemId });
        }


        public Uri RenderOrderId(OrderIdComponents components)
        {
            return RenderId(0, components, nameof(RenderId), "orderIdTemplate");
        }

        public Uri RenderOrderItemId(OrderIdComponents components)
        {
            return RenderId(1, components, nameof(RenderId), "orderItemIdTemplate");
        }
    }


    public class SingleIdTemplate<T> : IdTemplate<T> where T : new()
    {
        public SingleIdTemplate(string uriTemplate) : base(uriTemplate)
        {
            if (uriTemplate == null) throw new ArgumentNullException(nameof(uriTemplate));
        }

        public T GetIdComponents(Uri id)
        {
            return base.GetIdComponents(nameof(GetIdComponents), id);
        }

        public Uri RenderId(T components)
        {
            return RenderId(0, components, nameof(RenderId), "uriTemplate");
        }
    }

    /// <summary>
    /// Id transforms provide strongly typed
    /// </summary>
    public abstract class IdTemplate<T> where T : new()
    {
        private List<UriTemplate.Core.UriTemplate> uriTemplates;
        private const string BaseUrlPlaceholder = "BaseUrl";

        protected IdTemplate(params string[] uriTemplate)
        {
            uriTemplates = uriTemplate.Select(t => t == null ? null : new UriTemplate.Core.UriTemplate(t)).ToList();
        }

        protected IdTemplate(Uri requiredBaseUrl, params string[] uriTemplate) : this(uriTemplate)
        {
            RequiredBaseUrl = requiredBaseUrl;
        }

        /// <summary>
        /// If the RequiredBaseUrl is set, an exception is thrown where the {BaseUrl} does not match this value.
        /// </summary>
        public Uri RequiredBaseUrl { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        protected T GetIdComponents(string method, params Uri[] ids)
        {
            if (ids.Length > uriTemplates.Count)
                throw new ArgumentException("{method} must have a number of supplied id parameters that are at least covered by the number of templates supplied when using the constructor for this class. Pass null for these ids if not known.");

            var components = new T();
            var componentsType = typeof(T);

            // TODO: Create detailed tests around this and ensure that if components that match are zero length or whitespace,
            // it behaves the same as if no match has occurred

            for (var index = 0; index < ids.Length; index++)
            {
                // Ignore an id where it is supplied as null
                if (ids[index] == null) continue;

                // If one of the urlTemplates in scope of the match is null, return null
                if (uriTemplates[index] == null) return default(T);

                var match = uriTemplates[index].Match(ids[index]);

                // If ID does not match template, return null
                if (match == null || match.Bindings == null || match.Bindings.Count == 0) return default(T);

                // Set matching components in supplied POCO based on property name
                foreach (var binding in match.Bindings)
                {

                    if (binding.Key == BaseUrlPlaceholder && this.RequiredBaseUrl != null)
                    {
                        //Special behaviour for BaseUrl
                        var newValue = (binding.Value.Value as string).ParseUrlOrNull();
                        if (newValue != this.RequiredBaseUrl)
                        {
                            throw new RequiredBaseUrlMismatchException($"Base Url ('{newValue}') of the supplied Ids does not match expected default ('{this.RequiredBaseUrl}')");
                        }
                    }
                    else if (componentsType.GetProperty(binding.Key) == null)
                    {
                        throw new ComponentFailedToParseException("Supplied UriTemplates must match supplied component type properties");
                    }
                    else if (componentsType.GetProperty(binding.Key).PropertyType == typeof(long?))
                    {
                        if (long.TryParse(binding.Value.Value as string, out long newValue))
                        {
                            var existingValue = componentsType.GetProperty(binding.Key).GetValue(components) as long?;
                            if (existingValue != newValue && existingValue != null)
                            {
                                throw new BookableOpportunityAndOfferMismatchException($"Supplied Ids do not match on component '{binding.Value.Key}'");
                            }
                            componentsType.GetProperty(binding.Key).SetValue(components, newValue);
                        }
                        else
                        {
                            throw new ComponentFailedToParseException($"An integer in the template for binding {binding.Key} failed to parse.");
                        }
                    }
                    else if (componentsType.GetProperty(binding.Key).PropertyType == typeof(Guid?))
                    {
                        if (Guid.TryParse(binding.Value.Value as string, out Guid newValue))
                        {
                            var existingValue = componentsType.GetProperty(binding.Key).GetValue(components) as Guid?;
                            if (existingValue != newValue && existingValue != null)
                            {
                                throw new BookableOpportunityAndOfferMismatchException($"Supplied Ids do not match on component '{binding.Value.Key}'");
                            }
                            componentsType.GetProperty(binding.Key).SetValue(components, newValue);
                        }
                        else
                        {
                            throw new ComponentFailedToParseException($"A Guid in the template for binding {binding.Key} failed to parse.");
                        }
                    }
                    else if (componentsType.GetProperty(binding.Key).PropertyType == typeof(Guid))
                    {
                        if (Guid.TryParse(binding.Value.Value as string, out Guid newValue))
                        {
                            var existingValue = (Guid)componentsType.GetProperty(binding.Key).GetValue(components);
                            if (existingValue != newValue && existingValue != Guid.Empty)
                            {
                                throw new BookableOpportunityAndOfferMismatchException($"Supplied Ids do not match on component '{binding.Value.Key}'");
                            }
                            componentsType.GetProperty(binding.Key).SetValue(components, newValue);
                        }
                        else
                        {
                            throw new ComponentFailedToParseException($"A Guid in the template for binding {binding.Key} failed to parse.");
                        }
                    }
                    else if (componentsType.GetProperty(binding.Key).PropertyType == typeof(string))
                    {
                        var newValue = binding.Value.Value as string;
                        var existingValue = componentsType.GetProperty(binding.Key).GetValue(components) as string;
                        if (existingValue != newValue && existingValue != null)
                        {
                            throw new BookableOpportunityAndOfferMismatchException($"Supplied Ids do not match on component '{binding.Value.Key}'");
                        }
                        componentsType.GetProperty(binding.Key).SetValue(components, newValue);
                    }
                    else if (componentsType.GetProperty(binding.Key).PropertyType == typeof(Uri))
                    {
                        var newValue = (binding.Value.Value as string).ParseUrlOrNull();
                        var existingValue = componentsType.GetProperty(binding.Key).GetValue(components) as Uri;
                        if (existingValue != newValue && existingValue != null)
                        {
                            throw new BookableOpportunityAndOfferMismatchException($"Supplied Ids do not match on component '{binding.Value.Key}'");
                        }
                        componentsType.GetProperty(binding.Key).SetValue(components, newValue);
                    }
                    else if (Nullable.GetUnderlyingType(componentsType.GetProperty(binding.Key).PropertyType)?.IsEnum == true)
                    {
                        object existingValue = componentsType.GetProperty(binding.Key).GetValue(components);
                        object newValue = ToEnum(componentsType.GetProperty(binding.Key).PropertyType, binding.Value.Value as string);
                        if (newValue == null)
                        {
                            throw new ComponentFailedToParseException($"An enumeration in the template for binding {binding.Key} failed to parse.");
                        }
                        if (existingValue != newValue && existingValue != null)
                        {
                            throw new BookableOpportunityAndOfferMismatchException($"Supplied Ids do not match on component '{binding.Value.Key}'");
                        }
                        try
                        {
                            componentsType.GetProperty(binding.Key).SetValue(components, newValue);
                        }
                        catch (Exception ex)
                        {
                            throw new ComponentFailedToParseException($"An enumeration in the template for binding {binding.Key} failed to parse.", ex);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Only types long?, Uri, enum?, Guid? and string are supported within the component class used for IdTemplate.");
                    }
                }
            }

            return components;
        }

        public object ToEnumStringIfEnum(PropertyInfo prop, object value)
        {
            if (value == null) return null;
            if (prop.PropertyType == typeof(OpportunityType)) return value; // To optimise render, ignore this particular enum
            var enumType = Nullable.GetUnderlyingType(prop.PropertyType);
            if (enumType != null && enumType.IsEnum)
            {
                var name = Enum.GetName(enumType, value);
                var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).SingleOrDefault();
                return enumMemberAttribute?.Value ?? name;
            }
            else
            {
                return value;
            }
        }

        private static object ToEnum(Type nullableEnumType, string str)
        {
            Type enumType = Nullable.GetUnderlyingType(nullableEnumType);
            foreach (var name in Enum.GetNames(enumType))
            {
                var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
                if (enumMemberAttribute.Value == str) return Enum.Parse(enumType, name);
            }
            //throw exception or whatever handling you want or
            return null;
        }

        protected Uri RenderId(int index, T components, string method, string param)
        {
            if (uriTemplates.ElementAtOrDefault(index) == null)
            {
                throw new NotSupportedException($"{method} is not available as {param} was not specified when using the constructor for this class.");
            }

            if (components == null) throw new ArgumentNullException(nameof(components), $"{method} requires non-null components to be supplied");

            var componentDictionary = components.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .ToDictionary(prop => prop.Name, prop => ToEnumStringIfEnum(prop, prop.GetValue(components, null)));

            if (this.RequiredBaseUrl != null) componentDictionary[BaseUrlPlaceholder] = RequiredBaseUrl;

            return uriTemplates[index].BindByName(componentDictionary);
        }
    }

}
