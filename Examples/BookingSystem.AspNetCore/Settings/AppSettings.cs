namespace BookingSystem
{
    public class AppSettings
    {
        public string BaseUrl { get; set; }
        public bool UseSingleSellerMode { get; set; }
        public bool UsePaymentReconciliationDetailValidation { get; set; }
        public string AccountId { get; set; }
        public string PaymentProviderId { get; set; }
    }
}