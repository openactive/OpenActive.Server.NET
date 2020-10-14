﻿using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenActive.Server.NET.StoreBooking
{
    public interface IOpportunityStore
    {
        void SetConfiguration(IBookablePairIdTemplate template, SingleIdTemplate<SellerIdComponents> sellerTemplate);

        void GetOrderItems(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext);
        void LeaseOrderItems(Lease lease, List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext);
        void ProposeOrderItems(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext);
        void BookOrderItems(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext);
        Event CreateOpportunityWithinTestDataset(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, SellerIdComponents seller);
        void DeleteTestDataset(string testDatasetIdentifier);
        void TriggerTestAction(OpenBookingSimulateAction simulateAction, IBookableIdComponents idComponents);
    }


    //TODO: Remove duplication between this and RpdeBase if possible as they are using the same pattern?
    public abstract class OpportunityStore<TComponents, TDatabaseTransaction, TStateContext> : ModelSupport<TComponents>, IOpportunityStore where TComponents : class, IBookableIdComponents, new() where TDatabaseTransaction : IDatabaseTransaction where TStateContext : IStateContext
    {
        void IOpportunityStore.SetConfiguration(IBookablePairIdTemplate template, SingleIdTemplate<SellerIdComponents> sellerTemplate)
        {
            if (template as BookablePairIdTemplate<TComponents> == null)
            {
                throw new NotSupportedException($"{template.GetType().ToString()} does not match {typeof(BookablePairIdTemplate<TComponents>).ToString()}. All types of IBookableIdComponents (T) used for BookablePairIdTemplate<T> assigned to feeds via settings.IdConfiguration must match those used for RPDEFeedGenerator<T> in settings.OpenDataFeeds.");
            }

            base.SetConfiguration((BookablePairIdTemplate<TComponents>)template, sellerTemplate);
        }


        void IOpportunityStore.GetOrderItems(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext)
        {
            // TODO: Include validation on the OrderItem created, to ensure it includes all the required fields
            GetOrderItems(ConvertToSpecificComponents(orderItemContexts), flowContext, (TStateContext)stateContext);
        }

        void IOpportunityStore.LeaseOrderItems(Lease lease, List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext)
        {
            // TODO: Include validation on the OrderItem created, to ensure it includes all the required fields
            LeaseOrderItems(lease, ConvertToSpecificComponents(orderItemContexts), flowContext, (TStateContext)stateContext, (TDatabaseTransaction)databaseTransactionContext);
        }

        void IOpportunityStore.ProposeOrderItems(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext)
        {
            // TODO: Include validation on the OrderItem created, to ensure it includes all the required fields
            ProposeOrderItems(ConvertToSpecificComponents(orderItemContexts), flowContext, (TStateContext)stateContext, (TDatabaseTransaction)databaseTransactionContext);
        }

        void IOpportunityStore.BookOrderItems(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext)
        {
            // TODO: Include validation on the OrderItem created, to ensure it includes all the required fields
            BookOrderItems(ConvertToSpecificComponents(orderItemContexts), flowContext, (TStateContext)stateContext, (TDatabaseTransaction)databaseTransactionContext);
        }


        Event IOpportunityStore.CreateOpportunityWithinTestDataset(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, SellerIdComponents seller)
        {
            var components = CreateOpportunityWithinTestDataset(testDatasetIdentifier, opportunityType, criteria, seller);
            return OrderCalculations.RenderOpportunityWithOnlyId(RenderOpportunityJsonLdType(components), RenderOpportunityId(components));
        }

        void IOpportunityStore.DeleteTestDataset(string testDatasetIdentifier)
        {
            DeleteTestDataset(testDatasetIdentifier);
        }

        void IOpportunityStore.TriggerTestAction(OpenBookingSimulateAction simulateAction, IBookableIdComponents idComponents)
        {
            if (!(idComponents.GetType() == typeof(TComponents)))
            {
                throw new NotSupportedException($"OpportunityIdComponents does not match {typeof(BookablePairIdTemplate<TComponents>).ToString()}. All types of IBookableIdComponents (T) used for BookablePairIdTemplate<T> assigned to feeds via settings.IdConfiguration must match those used by the stores in storeSettings.OpenBookingStoreRouting.");
            }

            TriggerTestAction(simulateAction, (TComponents)idComponents);
        }


        private List<OrderItemContext<TComponents>> ConvertToSpecificComponents(List<IOrderItemContext> orderItemContexts)
        {
            if (orderItemContexts == null) throw new ArgumentNullException(nameof(orderItemContexts));

            if (!(orderItemContexts.Select(x => x.RequestBookableOpportunityOfferId).ToList().TrueForAll(x => x.GetType() == typeof(TComponents))))
            {
                throw new NotSupportedException($"OpportunityIdComponents does not match {typeof(BookablePairIdTemplate<TComponents>).ToString()}. All types of IBookableIdComponents (T) used for BookablePairIdTemplate<T> assigned to feeds via settings.IdConfiguration must match those used by the stores in storeSettings.OpenBookingStoreRouting.");
            }

            return orderItemContexts.ConvertAll<OrderItemContext<TComponents>>(x => (OrderItemContext<TComponents>)x);
        }


        protected abstract void GetOrderItems(List<OrderItemContext<TComponents>> orderItemContexts, StoreBookingFlowContext flowContext, TStateContext stateContext);
        protected abstract void LeaseOrderItems(Lease lease, List<OrderItemContext<TComponents>> orderItemContexts, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction databaseTransactionContext);
        /// <summary>
        /// ProposeOrderItems will always succeed or throw an error on failure.
        /// Note that responseOrderItems provided by GetOrderItems are supplied for cases where Sales Invoices or other audit records
        /// need to be written that require prices. As GetOrderItems occurs outside of the transaction.
        /// </summary>
        protected abstract void ProposeOrderItems(List<OrderItemContext<TComponents>> orderItemContexts, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction databaseTransactionContext);

        /// <summary>
        /// BookOrderItems will always succeed or throw an error on failure.
        /// Note that responseOrderItems provided by GetOrderItems are supplied for cases where Sales Invoices or other audit records
        /// need to be written that require prices. As GetOrderItems occurs outside of the transaction.
        /// </summary>
        protected abstract void BookOrderItems(List<OrderItemContext<TComponents>> orderItemContexts, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction databaseTransactionContext);

        protected abstract TComponents CreateOpportunityWithinTestDataset(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, SellerIdComponents seller);
        protected abstract void DeleteTestDataset(string testDatasetIdentifier);
        protected abstract void TriggerTestAction(OpenBookingSimulateAction simulateAction, TComponents idComponents);

    }
}
