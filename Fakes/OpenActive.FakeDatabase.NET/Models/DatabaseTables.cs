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
        public long Modified { get; set; } = new DateTimeOffset(DateTime.Today).UtcTicks;
    }

    public static class DatabaseCreator
    {
        public static void CreateTables(OrmLiteConnectionFactory dbFactory, bool dropTablesOnRestart)
        {
            using (var db = dbFactory.Open())
            {
                if (dropTablesOnRestart)
                {
                    db.DropTable<SellerUserTable>();
                    db.DropTable<OrderItemsTable>();
                    db.DropTable<OccurrenceTable>();
                    db.DropTable<OrderTable>();
                    db.DropTable<ClassTable>();
                    db.DropTable<SlotTable>();
                    db.DropTable<FacilityUseTable>();
                    db.DropTable<GrantTable>();
                    db.DropTable<BookingPartnerTable>();
                    db.DropTable<SellerTable>();
                    db.CreateTable<GrantTable>();
                    db.CreateTable<BookingPartnerTable>();
                    db.CreateTable<SellerTable>();
                    db.CreateTable<ClassTable>();
                    db.CreateTable<OrderTable>();
                    db.CreateTable<OccurrenceTable>();
                    db.CreateTable<FacilityUseTable>();
                    db.CreateTable<SlotTable>();
                    db.CreateTable<OrderItemsTable>();
                    db.CreateTable<SellerUserTable>();
                }
                else
                {
                    if (!db.TableExists<GrantTable>())
                        db.CreateTable<GrantTable>();
                    if (!db.TableExists<BookingPartnerTable>())
                        db.CreateTable<BookingPartnerTable>();
                    if (!db.TableExists<SellerTable>())
                        db.CreateTable<SellerTable>();
                    if (!db.TableExists<ClassTable>())
                        db.CreateTable<ClassTable>();
                    if (!db.TableExists<OrderTable>())
                        db.CreateTable<OrderTable>();
                    if (!db.TableExists<OccurrenceTable>())
                        db.CreateTable<OccurrenceTable>();
                    if (!db.TableExists<FacilityUseTable>())
                        db.CreateTable<FacilityUseTable>();
                    if (!db.TableExists<SlotTable>())
                        db.CreateTable<SlotTable>();
                    if (!db.TableExists<OrderItemsTable>())
                        db.CreateTable<OrderItemsTable>();
                    if (!db.TableExists<SellerUserTable>())
                        db.CreateTable<SellerUserTable>();
                }
            }
        }
    }
}
