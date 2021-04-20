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
        IDatabaseTransaction BeginOrderTransaction(FlowStage stage);
        Task<bool> CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds);
        Task<bool> CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate);
        IStateContext InitialiseFlow(StoreBookingFlowContext flowContext);
        Task<DeleteOrderResult> DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        Task DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
        Task TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents idComponents);
        Task<Order> GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller);
        Task<bool> CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order);

        ValueTask<Lease> CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction, bool useAsync);
        ValueTask UpdateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction, bool useAsync);
    }

    public interface IOrderStoreSync : IOrderStore
    {
        void CreateOrderSync(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        void UpdateOrderSync(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        (string, OrderProposalStatus) CreateOrderProposalSync(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        void UpdateOrderProposalSync(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
    }

    public interface IOrderStoreAsync : IOrderStore
    {
        Task CreateOrderAsync(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        Task UpdateOrderAsync(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        Task<(string, OrderProposalStatus)> CreateOrderProposalAsync(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        Task UpdateOrderProposalAsync(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
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
        public abstract TStateContext Initialise(StoreBookingFlowContext flowContext);
        public IStateContext InitialiseFlow(StoreBookingFlowContext flowContext)
        {
            return Initialise(flowContext);
        }
        public abstract Task<bool> CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds);
        public abstract Task<bool> CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate);
        public abstract Task<DeleteOrderResult> DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        public abstract Task DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
        public abstract Task TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents idComponents);
        public abstract Task<Order> GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller);
        public abstract Task<bool> CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order);
        public abstract IDatabaseTransaction BeginOrderTransaction(FlowStage stage);

        public abstract ValueTask<Lease> CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction, bool useAsync);
        public ValueTask<Lease> CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction, bool useAsync)
        {
            return CreateLease(responseOrderQuote, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction, useAsync);
        }

        public abstract ValueTask UpdateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction, bool useAsync);
        public ValueTask UpdateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction, bool useAsync)
        {
            return UpdateLease(responseOrderQuote, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction, useAsync);
        }


    }
}
