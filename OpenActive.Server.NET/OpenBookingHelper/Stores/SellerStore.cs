using OpenActive.NET;
using System;
using System.Threading.Tasks;


namespace OpenActive.Server.NET.OpenBookingHelper
{
    public abstract class SellerStore
    {
        private SingleIdTemplate<SimpleIdComponents> IdTemplate { get; set; }

        internal void SetConfiguration(SingleIdTemplate<SimpleIdComponents> template)
        {
            this.IdTemplate = template;
        }

        protected Uri RenderSellerId(SimpleIdComponents simpleIdComponents)
        {
            return this.IdTemplate.RenderId(simpleIdComponents);
        }

        protected Uri RenderSingleSellerId()
        {
            return this.IdTemplate.RenderId(new SimpleIdComponents());
        }

        internal async ValueTask<ILegalEntity> GetSellerById(SimpleIdComponents simpleIdComponents)
        {
            // TODO: Include validation on the OrderItem created, to ensure it includes all the required fields
            return await GetSeller(simpleIdComponents);
        }

        protected abstract ValueTask<ILegalEntity> GetSeller(SimpleIdComponents sellerId);

    }
}