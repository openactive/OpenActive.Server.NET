using Xunit;
using System.Linq;
using Xunit.Abstractions;
using OpenActive.FakeDatabase.NET;

namespace OpenActive.FakeDatabase.NET.Test
{
    public class FakeBookingSystemTest
    {
        private readonly ITestOutputHelper output;

        public FakeBookingSystemTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void FakeDatabase_Exists()
        {
            var query = from classes in FakeBookingSystem.Database.Classes
                        join occurances in FakeBookingSystem.Database.Occurrences
                        on classes.Id equals occurances.ClassId
                        select new
                        {
                            title = classes.Title,
                            startDate = occurances.Start,
                        };

            var list = query.ToList();

            foreach (var result in list)
            {
                output.WriteLine(result.title + " " + result.startDate.ToString());
            }

            //var components = template.GetIdComponents(new Uri("https://example.com/api/session-series/asdf/events/123"));

            Assert.True(list.Count > 0);
            //Assert.Equal("session-series", components.EventType);
            //Assert.Equal("asdf", components.SessionSeriesId);
            //Assert.Equal(123, components.ScheduledSessionId);
        }

    }
}
