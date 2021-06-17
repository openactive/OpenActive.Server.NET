﻿using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenActive.Server.NET.StoreBooking
{
    /// <summary>
    /// Result of deleting (or attempting to delete) an Order in a store
    /// </summary>
    public enum DeleteOrderResult
    {
        OrderSuccessfullyDeleted,
        OrderDidNotExist
    }

    public interface IOrderStore
    {
        void SetConfiguration(OrderIdTemplate orderIdTemplate, SingleIdTemplate<SellerIdComponents> sellerIdTemplate);
        /// <summary>
        /// Stage is provided as it depending on the implementation (e.g. what level of leasing is applied)
        /// it might not be appropriate to create transactions for all stages.
        /// Null can be returned in the case that a transaction has not been created.
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        ValueTask<IDatabaseTransaction> BeginOrderTransaction(FlowStage stage);
        Task<bool> CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, List<OrderIdComponents> orderItemIds);
        Task<bool> CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId);
        ValueTask<IStateContext> InitialiseFlow(StoreBookingFlowContext flowContext);
        Task<DeleteOrderResult> DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        Task DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
        Task TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents orderId);
        Task<Order> GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller);
        Task<bool> CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order);

        ValueTask<Lease> CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        ValueTask UpdateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        ValueTask CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        ValueTask UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        ValueTask<(Guid, OrderProposalStatus)> CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        ValueTask UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
    }

    public interface IStateContext
    {
    }

    public abstract class OrderStore<TDatabaseTransaction, TStateContext> : OrdersModelSupport, IOrderStore where TDatabaseTransaction : IDatabaseTransaction where TStateContext : IStateContext
    {
        void IOrderStore.SetConfiguration(OrderIdTemplate orderIdTemplate, SingleIdTemplate<SellerIdComponents> sellerIdTemplate)
        {
            base.SetConfiguration(orderIdTemplate, sellerIdTemplate);
        }
        public abstract ValueTask<TStateContext> Initialise(StoreBookingFlowContext flowContext);
        public async ValueTask<IStateContext> InitialiseFlow(StoreBookingFlowContext flowContext)
        {
            return await Initialise(flowContext);
        }
        public abstract Task<bool> CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, List<OrderIdComponents> orderItemIds);
        public virtual Task<bool> CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId) {
            // This will return an error to the Broker
            throw new OpenBookingException(new OpenBookingError(), "Order Proposals are not supported in this implementation");
        }
        public abstract Task<DeleteOrderResult> DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        public abstract Task DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
        public abstract Task TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents orderId);
        public virtual Task<Order> GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller) {
            // This will return an error to the Broker
            throw new OpenBookingException(new OpenBookingError(), "The Order Status endpoint is not supported in this implementation");
        }

        public virtual Task<bool> CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order) {
            throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "CreateOrderFromOrderProposal must be implemented when implementing Order Proposals");
        }
        public abstract ValueTask<IDatabaseTransaction> BeginOrderTransaction(FlowStage stage);

        public abstract ValueTask<Lease> CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction);
        public ValueTask<Lease> CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            return CreateLease(responseOrderQuote, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public virtual ValueTask UpdateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction)
        {
            // No-op if not implemented, as UpdateLease is optional
            return new ValueTask();
        }

        public ValueTask UpdateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            return UpdateLease(responseOrderQuote, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public abstract ValueTask CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction);
        public ValueTask CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            return CreateOrder(responseOrder, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public virtual ValueTask UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction)
        {
            // No-op if not implemented, as UpdateOrder is optional
            return new ValueTask();
        }

        public ValueTask UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            return UpdateOrder(responseOrder, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public virtual ValueTask<(Guid, OrderProposalStatus)> CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction)
        {
            // CreateOrderProposal gets called in the flow first, so this will return an error to the Broker
            throw new OpenBookingException(new OpenBookingError(), "Order Proposals are not supported in this implementation");
        }
        public ValueTask<(Guid, OrderProposalStatus)> CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            return CreateOrderProposal(responseOrderProposal, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public virtual ValueTask UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction)
        {
            // No-op if not implemented, as UpdateOrderProposal is optional
            return new ValueTask();
        }
        public ValueTask UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            return UpdateOrderProposal(responseOrderProposal, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }
    }
}
