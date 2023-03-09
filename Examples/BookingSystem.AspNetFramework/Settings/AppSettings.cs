namespace BookingSystem
{
    public class AppSettings
    {
        public string ApplicationHostBaseUrl { get; set; }
        public string OpenIdIssuerUrl { get; set; }
        public FeatureSettings FeatureFlags { get; set; }
        public PaymentSettings Payment { get; set; }
    }

    /**
    *  Note feature defaults are set here, and are used for the .NET Framework reference implementation
    */
    public class FeatureSettings
    {
        public bool EnableTokenAuth { get; set; } = true;
        public bool SingleSeller { get; set; } = false;
        public bool PaymentReconciliationDetailValidation { get; set; } = true;
        public bool OnlyFreeOpportunities { get; set; } = false;
        public bool PrepaymentAlwaysRequired { get; set; } = false;
    }

    public class PaymentSettings
    {
        public bool TaxCalculationB2B { get; set; }
        public bool TaxCalculationB2C { get; set; }
        public string AccountId { get; set; }
        public string PaymentProviderId { get; set; }
    }
}