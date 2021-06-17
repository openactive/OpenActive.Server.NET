﻿using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    public class OrdersModelSupport
    {
        protected OrderIdTemplate OrderIdTemplate { get; set; }
        private SingleIdTemplate<SellerIdComponents> SellerIdTemplate { get; set; }

        protected internal void SetConfiguration(OrderIdTemplate orderIdTemplate, SingleIdTemplate<SellerIdComponents> sellerIdTemplate)
        {
            this.OrderIdTemplate = orderIdTemplate;
            this.SellerIdTemplate = sellerIdTemplate;
        }

        protected Uri RenderOrderId(OrderType orderType, Guid uuid)
        {
            return this.OrderIdTemplate.RenderOrderId(orderType, uuid);
        }

        //TODO reduce duplication of the strings / logic below
        protected Uri RenderOrderItemId(OrderType orderType, Guid uuid, Guid orderItemId)
        {
            return this.OrderIdTemplate.RenderOrderItemId(orderType, uuid, orderItemId);
        }
        protected Uri RenderOrderItemId(OrderType orderType, Guid uuid, string orderItemId)
        {
            return this.OrderIdTemplate.RenderOrderItemId(orderType, uuid, orderItemId);
        }
        protected Uri RenderOrderItemId(OrderType orderType, Guid uuid, long orderItemId)
        {
            return this.OrderIdTemplate.RenderOrderItemId(orderType, uuid, orderItemId);
        }

        protected Uri RenderSellerId(SellerIdComponents sellerIdComponents)
        {
            return this.SellerIdTemplate.RenderId(sellerIdComponents);
        }

        protected Uri RenderSingleSellerId()
        {
            return this.SellerIdTemplate.RenderId(new SellerIdComponents());
        }
    }
}
