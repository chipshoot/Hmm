namespace Hmm.ServiceApi.Configuration;

public class AppSettings
{
    public string ConnectionString { get; set; }

    /// <summary>
    /// Database provider: "PostgreSQL" (default) or "SqlServer"
    /// </summary>
    public string DatabaseProvider { get; set; } = "PostgreSQL";

    public string IdpBaseUrl { get; set; }

    /// <summary>
    /// The expected audience for JWT tokens. This should match the audience
    /// claim in tokens issued by the Identity Provider for this API.
    /// </summary>
    public string ApiAudience { get; set; }

    /// <summary>
    /// List of allowed CORS origins. Only requests from these origins will be accepted.
    /// For security, avoid using "*" in production. Example: ["https://myapp.com", "https://admin.myapp.com"]
    /// </summary>
    public string[] CorsOrigins { get; set; }

    /// <summary>
    /// External authentication provider settings (Firebase, Auth0, etc.)
    /// </summary>
    public ExternalAuthSettings ExternalAuth { get; set; }

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

/// <summary>
/// Configuration for external authentication providers
/// </summary>
public class ExternalAuthSettings
{
    /// <summary>
    /// Enable Firebase authentication
    /// </summary>
    public bool EnableFirebase { get; set; }

    /// <summary>
    /// Firebase project ID (found in Firebase Console > Project Settings)
    /// </summary>
    public string FirebaseProjectId { get; set; }

    /// <summary>
    /// Enable Auth0 authentication
    /// </summary>
    public bool EnableAuth0 { get; set; }

    /// <summary>
    /// Auth0 domain (e.g., "your-tenant.auth0.com")
    /// </summary>
    public string Auth0Domain { get; set; }

    /// <summary>
    /// Auth0 API audience/identifier
    /// </summary>
    public string Auth0Audience { get; set; }

    /// <summary>
    /// Enable Azure AD authentication
    /// </summary>
    public bool EnableAzureAd { get; set; }

    /// <summary>
    /// Azure AD tenant ID
    /// </summary>
    public string AzureAdTenantId { get; set; }

    /// <summary>
    /// Azure AD application (client) ID
    /// </summary>
    public string AzureAdClientId { get; set; }
}