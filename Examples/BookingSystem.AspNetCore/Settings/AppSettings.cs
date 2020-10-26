namespace BookingSystem
{
    public class AppSettings
    {
        public string ApplicationHostBaseUrl { get; set; }
        public FeatureSettings FeatureFlags { get; set; }
        public PaymentSettings Payment { get; set; }
    }

    public class FeatureSettings
    {
        public bool SingleSeller { get; set; }
        public bool PaymentReconciliationDetailValidation { get; set; }
    }

    public class PaymentSettings
    {
        public string AccountId { get; set; }
        public string PaymentProviderId { get; set; }
    }
}