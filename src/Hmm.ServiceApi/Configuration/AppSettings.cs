namespace Hmm.ServiceApi.Configuration;

public class AppSettings
{
    public string ConnectionString { get; set; }

    public string IdpBaseUrl { get; set; }

    /// <summary>
    /// The expected audience for JWT tokens. This should match the audience
    /// claim in tokens issued by the Identity Provider for this API.
    /// </summary>
    public string ApiAudience { get; set; }

    public AutomobileSetting Automobile { get; set; }

    public class AutomobileSetting
    {
        public SchemaSetting Schema { get; set; }

        public SeedingSetting Seeding { get; set; }
    }

    public class SchemaSetting
    {
        public string Vehicle { get; set; }
        public string GasDiscount { get; set; }
        public string GasLog { get; set; }
    }

    public class SeedingSetting
    {
        public bool AddSeedingEntity { get; set; }

        public string SeedingDataFile { get; set; }
    }
}