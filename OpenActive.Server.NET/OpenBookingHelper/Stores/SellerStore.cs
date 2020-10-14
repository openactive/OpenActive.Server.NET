using OpenActive.NET;
using System;


namespace OpenActive.Server.NET.OpenBookingHelper
{
    public abstract class SellerStore
    {
        private SingleIdTemplate<SellerIdComponents> IdTemplate { get; set; }

        internal void SetConfiguration(SingleIdTemplate<SellerIdComponents> template)
        {
            IdTemplate = template;
        }

        protected Uri RenderSellerId(SellerIdComponents sellerIdComponents)
        {
            return IdTemplate.RenderId(sellerIdComponents);
        }

        protected Uri RenderSingleSellerId()
        {
            return IdTemplate.RenderId(new SellerIdComponents());
        }

        internal ILegalEntity GetSellerById(SellerIdComponents sellerIdComponents)
        {
            // TODO: Include validation on the OrderItem created, to ensure it includes all the required fields
            return GetSeller(sellerIdComponents);
        }

        protected abstract ILegalEntity GetSeller(SellerIdComponents sellerId);

    }
}
