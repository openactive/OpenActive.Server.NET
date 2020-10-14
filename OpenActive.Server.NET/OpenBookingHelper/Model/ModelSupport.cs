using OpenActive.DatasetSite.NET;
using System;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    public class ModelSupport<TComponents> where TComponents : class, IBookableIdComponents, new()
    {
        private BookablePairIdTemplate<TComponents> IdTemplate { get; set; }
        public SingleIdTemplate<SellerIdComponents> SellerIdTemplate { get; private set; }

        protected internal void SetConfiguration(BookablePairIdTemplate<TComponents> template, SingleIdTemplate<SellerIdComponents> sellerTemplate)
        {
            IdTemplate = template;
            SellerIdTemplate = sellerTemplate;
        }

        protected Uri RenderSellerId(SellerIdComponents components)
        {
            return SellerIdTemplate.RenderId(components);
        }

        protected Uri RenderSingleSellerId()
        {
            return SellerIdTemplate.RenderId(new SellerIdComponents());
        }

        /// <summary>
        /// Use OpportunityType from components
        /// </summary>
        /// <param name="components"></param>
        /// <returns></returns>
        protected string RenderOpportunityJsonLdType(TComponents components)
        {
            return IdTemplate.RenderOpportunityJsonLdType(components);
        }

        /// <summary>
        /// Use OpportunityType from components
        /// </summary>
        /// <param name="components"></param>
        /// <returns></returns>
        protected Uri RenderOpportunityId(TComponents components)
        {
            return IdTemplate.RenderOpportunityId(components);
        }
        /// <summary>
        /// Use OpportunityType from components
        /// </summary>
        /// <param name="components"></param>
        /// <returns></returns>
        protected Uri RenderOfferId(TComponents components)
        {
            return IdTemplate.RenderOfferId(components);
        }
        protected Uri RenderOpportunityId(OpportunityType opportunityType, TComponents components)
        {
            return IdTemplate.RenderOpportunityId(opportunityType, components);
        }

        protected Uri RenderOfferId(OpportunityType opportunityType, TComponents components)
        {
            return IdTemplate.RenderOfferId(opportunityType, components);
        }

        protected TComponents GetBookableOpportunityReference(OpportunityType opportunityType, Uri id)
        {
            var components = IdTemplate.GetOpportunityBookableIdComponents(id);
            if (components.OpportunityType != opportunityType)
                throw new ArgumentOutOfRangeException(nameof(id), "Provided opportunityType does not match provided id");

            return components;
        }
    }
}
