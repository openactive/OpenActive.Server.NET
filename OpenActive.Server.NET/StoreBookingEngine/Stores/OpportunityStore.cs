using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.StoreBooking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenActive.Server.NET.StoreBooking
{
    public interface IOpportunityStore
    {
        void SetConfiguration(IBookablePairIdTemplate template, SingleIdTemplate<SellerIdComponents> sellerTemplate);
        Task GetOrderItems(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext);
        Task<Event> CreateOpportunityWithinTestDataset(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, SellerIdComponents seller);
        Task DeleteTestDataset(string testDatasetIdentifier);
        Task TriggerTestAction(OpenBookingSimulateAction simulateAction, IBookableIdComponents idComponents);
    }

    public interface IOpportunityStoreSync : IOpportunityStore
    {
        void LeaseOrderItemsSync(Lease lease, List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext);
        /// <summary>
        /// BookOrderItems will always succeed or throw an error on failure.
        /// Note that responseOrderItems provided by GetOrderItems are supplied for cases where Sales Invoices or other audit records
        /// need to be written that require prices. As GetOrderItems occurs outside of the transaction.
        /// </summary>
        void BookOrderItemsSync(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext);
        /// <summary>
        /// ProposeOrderItems will always succeed or throw an error on failure.
        /// Note that responseOrderItems provided by GetOrderItems are supplied for cases where Sales Invoices or other audit records
        /// need to be written that require prices. As GetOrderItems occurs outside of the transaction.
        /// </summary>
        void ProposeOrderItemsSync(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext);

    }

    public interface IOpportunityStoreAsync : IOpportunityStore
    {
        Task LeaseOrderItemsAsync(Lease lease, List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext);
        /// <summary>
        /// BookOrderItems will always succeed or throw an error on failure.
        /// Note that responseOrderItems provided by GetOrderItems are supplied for cases where Sales Invoices or other audit records
        /// need to be written that require prices. As GetOrderItems occurs outside of the transaction.
        /// </summary>
        Task BookOrderItemsAsync(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext);
        /// <summary>
        /// ProposeOrderItems will always succeed or throw an error on failure.
        /// Note that responseOrderItems provided by GetOrderItems are supplied for cases where Sales Invoices or other audit records
        /// need to be written that require prices. As GetOrderItems occurs outside of the transaction.
        /// </summary>
        Task ProposeOrderItemsAsync(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext);
    }


    //TODO: Remove duplication between this and RpdeBase if possible as they are using the same pattern?
    public abstract class OpportunityStore<TComponents, TDatabaseTransaction, TStateContext> : ModelSupport<TComponents>, IOpportunityStore where TComponents : class, IBookableIdComponents, new() where TDatabaseTransaction : IDatabaseTransaction where TStateContext : IStateContext
    {
        // async methoids that are never called in transactions
        void IOpportunityStore.SetConfiguration(IBookablePairIdTemplate template, SingleIdTemplate<SellerIdComponents> sellerTemplate)
        {
            if (template as BookablePairIdTemplate<TComponents> == null)
            {
                throw new NotSupportedException($"{template.GetType()} does not match {typeof(BookablePairIdTemplate<TComponents>).ToString()}. All types of IBookableIdComponents (T) used for BookablePairIdTemplate<T> assigned to feeds via settings.IdConfiguration must match those used for RPDEFeedGenerator<T> in settings.OpenDataFeeds.");
            }

            base.SetConfiguration((BookablePairIdTemplate<TComponents>)template, sellerTemplate);
        }

        Task IOpportunityStore.GetOrderItems(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext)
        {
            // TODO: Include validation on the OrderItem created, to ensure it includes all the required fields
            return GetOrderItems(ConvertToSpecificComponents(orderItemContexts), flowContext, (TStateContext)stateContext);
        }

        async Task<Event> IOpportunityStore.CreateOpportunityWithinTestDataset(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, SellerIdComponents seller)
        {
            var components = await CreateOpportunityWithinTestDataset(testDatasetIdentifier, opportunityType, criteria, seller);
            return OrderCalculations.RenderOpportunityWithOnlyId(RenderOpportunityJsonLdType(components), RenderOpportunityId(components));
        }

        async Task IOpportunityStore.DeleteTestDataset(string testDatasetIdentifier)
        {
            await DeleteTestDataset(testDatasetIdentifier);
        }

        async Task IOpportunityStore.TriggerTestAction(OpenBookingSimulateAction simulateAction, IBookableIdComponents idComponents)
        {
            if (!(idComponents.GetType() == typeof(TComponents)))
            {
                throw new NotSupportedException($"OpportunityIdComponents does not match {typeof(BookablePairIdTemplate<TComponents>).ToString()}. All types of IBookableIdComponents (T) used for BookablePairIdTemplate<T> assigned to feeds via settings.IdConfiguration must match those used by the stores in storeSettings.OpenBookingStoreRouting.");
            }

            await TriggerTestAction(simulateAction, (TComponents)idComponents);
        }

        protected List<OrderItemContext<TComponents>> ConvertToSpecificComponents(List<IOrderItemContext> orderItemContexts)
        {
            if (orderItemContexts == null) throw new ArgumentNullException(nameof(orderItemContexts));

            if (!(orderItemContexts.Select(x => x.RequestBookableOpportunityOfferId).ToList().TrueForAll(x => x.GetType() == typeof(TComponents))))
            {
                throw new NotSupportedException($"OpportunityIdComponents does not match {typeof(BookablePairIdTemplate<TComponents>).ToString()}. All types of IBookableIdComponents (T) used for BookablePairIdTemplate<T> assigned to feeds via settings.IdConfiguration must match those used by the stores in storeSettings.OpenBookingStoreRouting.");
            }

            return orderItemContexts.ConvertAll<OrderItemContext<TComponents>>(x => (OrderItemContext<TComponents>)x);
        }

        protected abstract Task GetOrderItems(List<OrderItemContext<TComponents>> orderItemContexts, StoreBookingFlowContext flowContext, TStateContext stateContext);
        protected abstract Task<TComponents> CreateOpportunityWithinTestDataset(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, SellerIdComponents seller);
        protected abstract Task DeleteTestDataset(string testDatasetIdentifier);
        protected abstract Task TriggerTestAction(OpenBookingSimulateAction simulateAction, TComponents idComponents);

    }
}
