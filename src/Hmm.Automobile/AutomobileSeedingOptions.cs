namespace Hmm.Automobile
{
    /// <summary>
    /// Configuration options for automobile data seeding.
    /// </summary>
    /// <remarks>
    /// <para>Bind this class to the "Automobile:Seeding" configuration section:</para>
    /// <code>
    /// services.Configure&lt;AutomobileSeedingOptions&gt;(
    ///     configuration.GetSection("Automobile:Seeding"));
    /// </code>
    ///
    /// <para>Expected configuration structure:</para>
    /// <code>
    /// {
    ///   "Automobile": {
    ///     "Seeding": {
    ///       "AddSeedingEntity": true,
    ///       "SeedingDataFile": "path/to/seeding-data.json"
    ///     }
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public class AutomobileSeedingOptions
    {
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
