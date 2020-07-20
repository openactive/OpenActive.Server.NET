using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NPoco;

namespace OpenActive.FakeDatabase.NET
{

    public enum BrokerRole { AgentBroker, ResellerBroker, NoBroker }

    public enum BookingStatus { CustomerCancelled, SellerCancelled, Confirmed, Attended }


    [PrimaryKey("RpdeId", AutoIncrement = true)]
    public abstract class Table
    {
        [Column("RpdeId")]
        public long Id { get; set; }
        public bool Deleted { get; set; } = false;
        public long Modified { get; set; } = DateTimeOffset.Now.UtcTicks;
    }

    public class ClassTable : Table
    {
        public string TestDatasetIdentifier { get; set; }
        public string Title { get; set; }
        public long SellerId { get; set; }
        public decimal? Price { get; set; }
    }



    public class OccurrenceTable : Table
    {
        public string TestDatasetIdentifier { get; set; }
        public long ClassId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public long TotalSpaces { get; set; }
        public long LeasedSpaces { get; set; }
        public long RemainingSpaces { get; set; }
    }


    public class OrderItemsTable : Table
    {
        public string ClientId { get; internal set; }
        public string OpportunityJsonLdType { get; set; }
        public string OpportunityJsonLdId { get; set; }
        public string OfferJsonLdId { get; set; }
        public string OrderId { get; set; }
        public long OccurrenceId { get; set; }
        [ColumnType(typeof(string))]
        public BookingStatus Status { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderTable : Table
    {
        public string ClientId { get; set; }
        public string OrderId { get; set; }
        public long SellerId { get; set; }
        public bool CustomerIsOrganization { get; set; }
        [ColumnType(typeof(string))]
        public BrokerRole BrokerRole { get; set; }
        public string BrokerName { get; set; }
        public string CustomerEmail { get; set; }
        public string PaymentIdentifier { get; set; }
        public decimal TotalOrderPrice { get; set; }
        public bool IsLease { get; set; }
        public DateTime LeaseExpires { get; set; }
        public bool VisibleInFeed { get; set; }
    }

    public class SellerTable : Table
    {
        public string Name { get; set; }
        public bool IsIndividual { get; set; }
    }

    public static class DatabaseCreator
    {
        public static List<string> GetCreateTableStatements(NPoco.Database db)
        {
            var subclassTypes = Assembly
            .GetAssembly(typeof(Table))
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Table)));

            var createStatements = new List<string>();

            foreach (var subclassType in subclassTypes)
            {
                var table = db.PocoDataFactory.ForType(subclassType);
                var tableName = table.TableInfo.TableName;

                var columnDefinition = new List<string>();

                foreach (var column in table.AllColumns)
                {
                    if (column.ColumnName == "RpdeId")
                    {
                        columnDefinition.Add("RpdeId INTEGER PRIMARY KEY");
                    } else
                    {
                        columnDefinition.Add($"{column.ColumnName} {ConvertColumnType(column.ColumnType)}");
                    }
                }

                var columnDefinitions = string.Join(", ", columnDefinition);
                createStatements.Add($"CREATE TABLE IF NOT EXISTS {tableName} ({columnDefinitions})");
            }

            return createStatements;
        }

        public static Dictionary<Type, string> TypeLookup = new Dictionary<Type, string> {
            { typeof(string), "TEXT NULL" },
            { typeof(int), "INTEGER NULL" },
            { typeof(long), "INTEGER NULL" },
            { typeof(DateTime), "DATETIME NULL" },
            { typeof(bool), "BOOLEAN NULL" },
            { typeof(decimal), "DECIMAL(5,2) NULL" },
            { typeof(decimal?), "DECIMAL(5,2) NULL" },
        };

        public static string ConvertColumnType(Type type)
        {
            if (TypeLookup.ContainsKey(type))
            {
                return TypeLookup[type];
            } else
            {
                return "TEXT NULL";
            }
        }
    }
}
