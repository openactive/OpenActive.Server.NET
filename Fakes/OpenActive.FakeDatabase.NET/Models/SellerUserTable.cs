using ServiceStack.DataAnnotations;

namespace OpenActive.FakeDatabase.NET
{
    public class SellerUserTable
    {
        [PrimaryKey]
        public long Id { get; set; }

        public string Username { get; set; }

        [Ignore]
        public string PasswordRaw
        {
            get => _passwordRaw;
            set
            {
                _passwordRaw = value;
                PasswordHash = value.Sha256();
            }
        }
        private string _passwordRaw;

        public string PasswordHash { get; set; }

        [Reference]
        public SellerTable SellerTable { get; set; }

        [ForeignKey(typeof(SellerTable), OnDelete = "CASCADE")]
        public long SellerId { get; set; }
    }
}