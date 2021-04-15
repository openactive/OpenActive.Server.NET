using OpenActive.NET;
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
        IDatabaseTransaction BeginOrderTransaction(FlowStage stage);
        void CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        (string, OrderProposalStatus) CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        bool CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds);
        bool CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate);
        IStateContext InitialiseFlow(StoreBookingFlowContext flowContext);
        void UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        void UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        DeleteOrderResult DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        void DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
        void TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents idComponents);
        Order GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller);
        bool CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order);
    }

    public interface IOrderStoreSync : IOrderStore
    {
        Lease CreateLeaseSync(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        void UpdateLeaseSync(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
    }

    public interface IOrderStoreAsync : IOrderStore
    {
        Task<Lease> CreateLeaseAsync(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        Task UpdateLeaseAsync(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);

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

        public abstract (string, OrderProposalStatus) CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction databaseTransaction);
        public abstract void CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction databaseTransaction);

        public abstract TStateContext Initialise(StoreBookingFlowContext flowContext);
        public abstract void UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction);
        public abstract void UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction);

        public (string, OrderProposalStatus) CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            return CreateOrderProposal(responseOrderProposal, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }
        public void CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            CreateOrder(responseOrder, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public IStateContext InitialiseFlow(StoreBookingFlowContext flowContext)
        {
            return Initialise(flowContext);
        }

        public void UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            UpdateOrder(responseOrder, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }
        public void UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            UpdateOrderProposal(responseOrderProposal, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
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
        public abstract bool CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate);
        public abstract DeleteOrderResult DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        public abstract void DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
        public abstract void TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents idComponents);
        public abstract Order GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller);
        public abstract bool CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order);
    }
}
