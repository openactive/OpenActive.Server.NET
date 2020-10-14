using OpenActive.NET;
using System;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    public class OrdersModelSupport
    {
        private OrderIdTemplate OrderIdTemplate { get; set; }
        private SingleIdTemplate<SellerIdComponents> SellerIdTemplate { get; set; }

        protected internal void SetConfiguration(OrderIdTemplate orderIdTemplate, SingleIdTemplate<SellerIdComponents> sellerIdTemplate)
        {
            OrderIdTemplate = orderIdTemplate;
            SellerIdTemplate = sellerIdTemplate;
        }

        protected Uri RenderOrderId(OrderType orderType, string uuid)
        {
            return OrderIdTemplate.RenderOrderId(orderType, uuid);
        }

        //TODO reduce duplication of the strings / logic below
        protected Uri RenderOrderItemId(OrderType orderType, string uuid, string orderItemId)
        {
            return OrderIdTemplate.RenderOrderItemId(orderType, uuid, orderItemId);
        }
        protected Uri RenderOrderItemId(OrderType orderType, string uuid, long orderItemId)
        {
            return OrderIdTemplate.RenderOrderItemId(orderType, uuid, orderItemId);
        }

        protected Uri RenderSellerId(SellerIdComponents sellerIdComponents)
        {
            return SellerIdTemplate.RenderId(sellerIdComponents);
        }

        protected Uri RenderSingleSellerId()
        {
            return SellerIdTemplate.RenderId(new SellerIdComponents());
        }

        protected static Event RenderOpportunityWithOnlyId(string jsonLdType, Uri id)
        {
            return OrderCalculations.RenderOpportunityWithOnlyId(jsonLdType, id);
        }
    }
}
