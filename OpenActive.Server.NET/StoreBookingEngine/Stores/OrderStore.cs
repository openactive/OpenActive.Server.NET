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
        /// <summary>
        /// Stage is provided as it depending on the implementation (e.g. what level of leasing is applied)
        /// it might not be appropriate to create transactions for all stages.
        /// Null can be returned in the case that a transaction has not been created.
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        IDatabaseTransaction BeginOrderTransaction(FlowStage stage);
        (string, OrderProposalStatus) CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        bool CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds);
        bool CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate);
        IStateContext InitialiseFlow(StoreBookingFlowContext flowContext);
        void UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        DeleteOrderResult DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        void DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
        void TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents idComponents);
        Task<Order> GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller);
        bool CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order);
    }

    public interface IOrderStoreSync : IOrderStore
    {
        Lease CreateLeaseSync(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        void UpdateLeaseSync(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        void CreateOrderSync(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        void UpdateOrderSync(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
    }

    public interface IOrderStoreAsync : IOrderStore
    {
        Task<Lease> CreateLeaseAsync(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        Task UpdateLeaseAsync(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        Task CreateOrderAsync(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);
        Task UpdateOrderAsync(Order responseOrder, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction);

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
        public abstract TStateContext Initialise(StoreBookingFlowContext flowContext);
        public abstract void UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, TStateContext stateContext, TDatabaseTransaction dbTransaction);
        public (string, OrderProposalStatus) CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            return CreateOrderProposal(responseOrderProposal, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public IStateContext InitialiseFlow(StoreBookingFlowContext flowContext)
        {
            return Initialise(flowContext);
        }

        public void UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction dbTransaction)
        {
            UpdateOrderProposal(responseOrderProposal, flowContext, (TStateContext)stateContext, (TDatabaseTransaction)dbTransaction);
        }

        public abstract bool CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds);
        public abstract bool CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate);
        public abstract DeleteOrderResult DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId);
        public abstract void DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId);
        public abstract void TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents idComponents);
        public abstract Task<Order> GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller);
        public abstract bool CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order);
        public abstract IDatabaseTransaction BeginOrderTransaction(FlowStage stage);
    }
}
