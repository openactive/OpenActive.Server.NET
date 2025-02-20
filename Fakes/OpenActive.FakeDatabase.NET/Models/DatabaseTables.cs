using System;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace OpenActive.FakeDatabase.NET
{
    public enum BrokerRole { AgentBroker, ResellerBroker, NoBroker }

    public enum BookingStatus { None, CustomerCancelled, SellerCancelled, Confirmed, Attended, Absent }

    public enum ProposalStatus { AwaitingSellerConfirmation, SellerAccepted, SellerRejected, CustomerRejected }

    public enum FeedVisibility { None, Visible, Archived }

    public enum OrderMode { Lease, Proposal, Booking }

    public enum RequiredStatusType { Required, Optional, Unavailable }

    public enum AttendanceMode { Offline, Online, Mixed }

    public enum AdditionalDetailTypes { Age, PhotoConsent, Experience, Gender, FileUpload }

    [CompositeIndex(nameof(Modified), nameof(Id))]
    public abstract class Table
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }
        public bool Deleted { get; set; }
        public long Modified { get; set; } = new DateTimeOffset(DateTime.Now).UtcTicks;
    }

    public static class DatabaseCreator
    {
        private static readonly Type[] TableTypesToCreate = new[]
        {
            typeof(GrantTable),
            typeof(BookingPartnerTable),
            typeof(SellerTable),
            typeof(ClassTable),
            typeof(OrderTable),
            typeof(OccurrenceTable),
            typeof(OrderItemsTable),
            typeof(FacilityUseTable),
            typeof(SlotTable),
            typeof(SellerUserTable)
        };

        private static readonly Type[] TableTypesToDrop = new []
        {
            typeof(SellerUserTable),
            typeof(OrderItemsTable),
            typeof(OccurrenceTable),
            typeof(OrderTable),
            typeof(ClassTable),
            typeof(SellerTable),
            typeof(FacilityUseTable),
            typeof(SlotTable),
            typeof(GrantTable),
            typeof(BookingPartnerTable)
        };

        /// <returns>True if the tables were created, false if they already existed</returns>
        public static bool CreateTables(OrmLiteConnectionFactory dbFactory, bool dropTablesOnRestart)
        {
            using (var db = dbFactory.Open())
            {
                if (dropTablesOnRestart)
                {
                    // Drop tables in reverse order to handle dependencies
                    foreach (var tableType in TableTypesToDrop)
                    {
                        db.DropTable(tableType);
                    }
                    foreach (var tableType in TableTypesToCreate)
                    {
                        db.CreateTable(false, tableType);
                    }
                    return true;
                }
                else
                {
                    var tablesAlreadyExist = db.TableExists(TableTypesToCreate[0].Name);
                    Console.WriteLine($"Tables already exist: {tablesAlreadyExist} (using {TableTypesToCreate[0].Name})");
                    if (tablesAlreadyExist)
                    {
                        foreach (var tableType in TableTypesToCreate)
                        {
                            if (!db.TableExists(tableType.Name))
                            {
                                throw new Exception($"Database is in unexpected state (perhaps the code has changed, changing the schema). As migrations are not supported, please restart with PERSIST_PREVIOUS_DATABASE=false\n" +
                                    $"Table {TableTypesToCreate[0].Name} exists but table is: {tableType.Name}");
                            }
                        }
                        return false;
                    }
                    else
                    {
                        foreach (var tableType in TableTypesToCreate)
                        {
                            db.CreateTable(false, tableType);
                        }
                        return true;
                    }
                }
            }
        }
    }
}
