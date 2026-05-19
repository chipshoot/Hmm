// Ignore Spelling: Dto

using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.Migration
{
    /// <summary>
    /// Response DTO for <c>POST /v1/migration/upload</c> +
    /// <c>/v1/migration/replace</c>. Server-computed counts and
    /// per-record errors.
    /// </summary>
    public class ApiMigrationUploadResult
    {
        public int NotesPersisted { get; set; }
        public int NotesFailed { get; set; }
        public int VaultFilesPersisted { get; set; }
        public long VaultBytes { get; set; }
        public IList<ApiMigrationRecordError> Errors { get; set; }
            = new List<ApiMigrationRecordError>();
    }

    public class ApiMigrationRecordError
    {
        public int Index { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
