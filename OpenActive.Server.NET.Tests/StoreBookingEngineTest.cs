using Xunit;

namespace OpenActive.Server.NET.Tests
{
    public class StoreBookingEngineTest
    {


        [Fact]
        public void StoreBookingEngine_ProcessOrderQuoteCheckpoint()
        {
            // Set up engine with engine settings (outside of this test)
            //var openActiveEngine = new StoreBookingEngine();

            // Reset fake data
            // Push fake data in via openActiveEngine.processTestCommand

            var @requestJson = @"{
  ""@context"": ""https://openactive.io/"",
            ""@type"": ""OrderQuote"",
  ""brokerRole"": ""https://openactive.io/AgentBroker"",
  ""broker"": {
                ""@type"": ""Organization"",
    ""name"": ""MyFitnessApp"",
    ""url"": ""https://myfitnessapp.example.com"",
    ""description"": ""A fitness app for all the community"",
    ""logo"": {
                    ""@type"": ""ImageObject"",
      ""url"": ""http://data.myfitnessapp.org.uk/images/logo.png""
    },
    ""address"": {
                    ""@type"": ""PostalAddress"",
      ""streetAddress"": ""Alan Peacock Way"",
      ""addressLocality"": ""Village East"",
      ""addressRegion"": ""Middlesbrough"",
      ""postalCode"": ""TS4 3AE"",
      ""addressCountry"": ""GB""
    }
            },
  ""seller"": {
                ""@type"": ""Organization"",
    ""@id"": ""https://example.com/api/organisations/123""
  },
  ""orderedItem"": [
    {
      ""@type"": ""OrderItem"",
      ""acceptedOffer"": {
        ""@type"": ""Offer"",
        ""@id"": ""https://example.com/events/452#/offers/878""
      },
      ""orderedItem"": {
        ""@type"": ""ScheduledSession"",
        ""@id"": ""https://example.com/events/452/subEvents/132""
      }
    }
  ]
}";
            //var orderQuoteRequest = OpenActiveSerializer.Deserialize<OrderQuote>(@requestJson);

           // string orderQuoteResponse = openActiveEngine.ProcessOrderQuoteCheckpoint(orderQuote, id);
        }
    }
}
