namespace IdentityServer
{
    public class AppSettings
    {
        public string JsonLdIdBaseUrl { get; set; }
        public FeatureSettings FeatureFlags { get; set; }
    }

    /**
    *  Note feature defaults are set here, and are used for the .NET Framework reference implementation
    */
    public class FeatureSettings
    {
        public bool FacilityUseHasSlots { get; set; } = false;
    }
}