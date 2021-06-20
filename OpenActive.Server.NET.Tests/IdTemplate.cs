using System;
using Xunit;
using OpenActive.Server.NET;
using OpenActive.DatasetSite.NET;
using OpenActive.Server.NET.OpenBookingHelper;

namespace OpenActive.Server.NET.Tests
{
    public class IdTemplateTest
    {
        public class SessionSeriesComponents : IBookableIdComponents
        {
            public string EventType { get; set; }
            public string SessionSeriesId { get; set; }
            public long? ScheduledSessionId { get; set; }
            public long? OfferId { get; set; }
            public OpportunityType? OpportunityType { get; set; }
        }

        [Fact]
        public void SingleIdTemplate_GetIdComponents()
        {
            var template = new SingleIdTemplate<SessionSeriesComponents>(
                "{+BaseUrl}api/{EventType}/{SessionSeriesId}/events/{ScheduledSessionId}"
                );
            template.RequiredBaseUrl = new Uri("https://example.com/");

            var components = template.GetIdComponents(new Uri("https://example.com/api/session-series/asdf/events/123"));

            Assert.Equal("session-series", components.EventType);
            Assert.Equal("asdf", components.SessionSeriesId);
            Assert.Equal(123, components.ScheduledSessionId);
        }

        [Fact]
        public void SingleIdTemplate_GetIdComponents_UrlEncoded()
        {
            var template = new SingleIdTemplate<SessionSeriesComponents>(
                "{+BaseUrl}api/{EventType}/{SessionSeriesId}/events/{ScheduledSessionId}"
                );
            template.RequiredBaseUrl = new Uri("https://example.com/");

            var components = template.GetIdComponents(new Uri("https://example.com/api/session-series/2020-02-23T17%3A30%3A00Z/events/123"));

            Assert.Equal("session-series", components.EventType);
            Assert.Equal("2020-02-23T17:30:00Z", components.SessionSeriesId);
            Assert.Equal(123, components.ScheduledSessionId);
        }

        [Fact]
        public void SingleIdTemplate_GetIdComponents_IdToEnum()
        {
            var template = new OrderIdTemplate(
                "{+BaseUrl}api/{OrderType}/{uuid}",
                "{+BaseUrl}api/{OrderType}/{uuid}#/orderedItems/{OrderItemIdLong}"
                );
            template.RequiredBaseUrl = new Uri("https://example.com/");

            var components = template.GetOrderItemIdComponents("client", new Uri("https://example.com/api/orders/3cbb8557-ca8f-4889-b21f-a59f860f8d25#/orderedItems/123"));

            Assert.NotNull(components);
            Assert.Equal(OrderType.Order, components.OrderType);
            Assert.Equal(new Guid("3cbb8557-ca8f-4889-b21f-a59f860f8d25"), components.uuid);
            Assert.Equal(123, components.OrderItemIdLong);
        }

        [Fact]
        public void SingleIdTemplate_GetIdComponents_EnumToId()
        {
            var template = new OrderIdTemplate(
                "{+BaseUrl}api/{OrderType}/{uuid}",
                "{+BaseUrl}api/{OrderType}/{uuid}#/orderedItems/{OrderItemIdLong}"
                );
            template.RequiredBaseUrl = new Uri("https://example.com/");

            OrderIdComponents components = new OrderIdComponents
            {
                uuid = new Guid("3cbb8557-ca8f-4889-b21f-a59f860f8d25"),
                OrderItemIdLong = 123,
                OrderType = OrderType.Order
            };

            var id = template.RenderOrderItemId(components);

            Assert.Equal(new Uri("https://example.com/api/orders/3cbb8557-ca8f-4889-b21f-a59f860f8d25#/orderedItems/123"), id);

        }

        [Fact]
        public void SingleIdTemplate_RenderId()
        {
            var template = new SingleIdTemplate<SessionSeriesComponents>(
                "{+BaseUrl}api/{EventType}/{SessionSeriesId}/events/{ScheduledSessionId}"
                );

            template.RequiredBaseUrl = new Uri("https://example.com/");

            var uri = new Uri("https://example.com/api/session-series/asdf/events/123");

            var components = template.GetIdComponents(uri);

            var outputUri = template.RenderId(components);
            
            Assert.Equal(uri, outputUri);
        }

        [Fact]
        public void SingleIdTemplate_RenderId_UrlEncoded()
        {
            var template = new SingleIdTemplate<SessionSeriesComponents>(
                "{+BaseUrl}api/{EventType}/{SessionSeriesId}/events/{ScheduledSessionId}"
                );

            template.RequiredBaseUrl = new Uri("https://example.com/");

            var uri = new Uri("https://example.com/api/session-series/2020-02-23T17%3A30%3A00Z/events/123");

            var components = template.GetIdComponents(uri);

            var outputUri = template.RenderId(components);

            Assert.Equal(uri, outputUri);
        }

        [Fact]
        public void BookablePairIdTemplate_GetIdComponents()
        {
            var template = new BookablePairIdTemplate<SessionSeriesComponents>(
                        // Opportunity
                        new OpportunityIdConfiguration
                        {
                            OpportunityType = OpportunityType.Event,
                            AssignedFeed = OpportunityType.Event,
                            OpportunityIdTemplate = "{+BaseUrl}api/{EventType}/{SessionSeriesId}/events/{ScheduledSessionId}",
                            OfferIdTemplate = "{+BaseUrl}api/{EventType}/{SessionSeriesId}/events/{ScheduledSessionId}#/offers/{OfferId}",
                            Bookable = true
                        });

            template.RequiredBaseUrl = new Uri("https://example.com/");

            var components = template.GetIdComponents(
                new Uri("https://example.com/api/session-series/asdf/events/123"),
                new Uri("https://example.com/api/session-series/asdf/events/123#/offers/456")
                );

            Assert.Equal("session-series", components.EventType);
            Assert.Equal("asdf", components.SessionSeriesId);
            Assert.Equal(123, components.ScheduledSessionId);
            Assert.Equal(456, components.OfferId);

            Assert.Throws<BookableOpportunityAndOfferMismatchException>(() => template.GetIdComponents(
                new Uri("https://example.com/api/session-series/asdf/events/123"),
                new Uri("https://example.com/api/session-series/asdf/events/124#/offers/456")
                ));

            Assert.Throws<BookableOpportunityAndOfferMismatchException>(() => template.GetIdComponents(
                new Uri("https://example.com/api/session-series/asdf/events/123"),
                new Uri("https://example.com/api/session-series/asdF/events/123#/offers/456")
                ));

            Assert.Throws<RequiredBaseUrlMismatchException>(() => template.GetIdComponents(
                new Uri("https://example1.com/api/session-series/asdf/events/123"),
                new Uri("https://example1.com/api/session-series/asdf/events/123#/offers/456")
            ));
        }
    }
}
