using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using System;
using System.Collections.Generic;
using System.Text;

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
        IDatabaseTransaction BeginOrderTransaction(FlowStage stage);
        Lease CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        void CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        bool CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds);
        IStateContext InitialiseFlow(StoreBookingFlowContext flowContext);
        void UpdateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        void UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        DeleteOrderResult DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        void DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
    }

    public interface IStateContext 
    {
    }

    public abstract class OrderStore<TDatabaseTransaction, TStateContext> : IOrderStore where TDatabaseTransaction : IDatabaseTransaction where TStateContext : IStateContext
    {
        public abstract Lease CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction databaseTransaction);
        public abstract void CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction databaseTransaction);

        public abstract TStateContext Initialise(StoreBookingFlowContext flowContext);
        public abstract void UpdateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction);
        public abstract void UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction);

        public Lease CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            return CreateLease(responseOrderQuote, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public void CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            CreateOrder(responseOrder, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public IStateContext InitialiseFlow(StoreBookingFlowContext flowContext)
        {
            return Initialise(flowContext);
        }
        public void UpdateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            UpdateLease(responseOrderQuote, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }
        public void UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            UpdateOrder(responseOrder, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }


        /// <summary>
        /// Stage is provided as it depending on the implementation (e.g. what level of leasing is applied)
        /// it might not be appropriate to create transactions for all stages.
        /// Null can be returned in the case that a transaction has not been created.
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        protected abstract TDatabaseTransaction BeginOrderTransaction(FlowStage stage);

        IDatabaseTransaction IOrderStore.BeginOrderTransaction(FlowStage stage)
        {
            return BeginOrderTransaction(stage);
        }

        public abstract bool CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds);
        public abstract DeleteOrderResult DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        public abstract void DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
    }


}
