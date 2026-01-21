namespace Hmm.Automobile
{
    /// <summary>
    /// Configuration options for automobile module.
    /// </summary>
    /// <remarks>
    /// <para>Bind this class to the "Automobile" configuration section:</para>
    /// <code>
    /// services.Configure&lt;AutomobileSeedingOptions&gt;(
    ///     configuration.GetSection("Automobile"));
    /// </code>
    ///
    /// <para>Expected configuration structure:</para>
    /// <code>
    /// {
    ///   "Automobile": {
    ///     "DefaultAuthorAccountName": "automobile-service",
    ///     "CreateDefaultAuthorIfMissing": true,
    ///     "AddSeedingEntity": true,
    ///     "SeedingDataFile": "path/to/seeding-data.json"
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public class AutomobileSeedingOptions
    {
        /// <summary>
        /// Gets or sets the account name for the default author used by automobile operations.
        /// </summary>
        /// <value>
        /// The account name to look up in the database. Required for automobile operations.
        /// </value>
        public string DefaultAuthorAccountName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create the default author if it doesn't exist.
        /// </summary>
        /// <value>
        /// <c>true</c> to auto-create the author; <c>false</c> to fail if author is missing. Default is <c>false</c>.
        /// </value>
        public bool CreateDefaultAuthorIfMissing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to seed initial data during registration.
        /// </summary>
        /// <value>
        /// <c>true</c> to enable seeding; <c>false</c> to skip seeding. Default is <c>false</c>.
        /// </value>
        public bool AddSeedingEntity { get; set; }

        /// <summary>
        /// Gets or sets the path to the JSON file containing seed data.
        /// </summary>
        /// <value>
        /// The file path (absolute or relative) to the seeding data JSON file.
        /// Can be null or empty if seeding is disabled.
        /// </value>
        public string SeedingDataFile { get; set; }
    }
}
