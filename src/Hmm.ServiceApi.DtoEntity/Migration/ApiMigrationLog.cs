// Ignore Spelling: Dto

using System;

namespace Hmm.ServiceApi.DtoEntity.Migration
{
    /// <summary>
    /// Read DTO for <c>GET /v1/migration/log</c>. Mirrors the
    /// MigrationLog domain entity 1:1.
    /// </summary>
    public class ApiMigrationLog
    {
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public string? DeviceIdentifier { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string? RecordCounts { get; set; }
        public DateTime At { get; set; }
    }
}
