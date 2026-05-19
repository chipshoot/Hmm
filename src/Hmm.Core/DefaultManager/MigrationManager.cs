using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Map.Migration;
using Hmm.Core.Vault;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;

namespace Hmm.Core.DefaultManager
{
    /// <summary>
    /// Default <see cref="IMigrationManager"/>. See the interface for
    /// the contract; this implementation streams the zip directly
    /// into the supplied <c>Stream</c> for export and processes
    /// uploads record-by-record so a single bad record doesn't
    /// reject the whole envelope.
    /// </summary>
    public class MigrationManager : IMigrationManager
    {
        private static readonly JsonSerializerOptions ExportJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IVersionRepository<HmmNoteDao> _noteRepository;
        private readonly IRepository<MigrationLogDao> _logRepository;
        private readonly ITagManager _tagManager;
        private readonly IEntityLookup _lookup;
        private readonly IVaultBlobStore _vault;
        private readonly IHmmNoteManager _noteManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDateTimeProvider _dateProvider;
        private readonly AttachmentSettings _settings;

        public MigrationManager(
            IVersionRepository<HmmNoteDao> noteRepository,
            IRepository<MigrationLogDao> logRepository,
            ITagManager tagManager,
            IEntityLookup lookup,
            IVaultBlobStore vault,
            IHmmNoteManager noteManager,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDateTimeProvider dateProvider,
            AttachmentSettings settings)
        {
            ArgumentNullException.ThrowIfNull(noteRepository);
            ArgumentNullException.ThrowIfNull(logRepository);
            ArgumentNullException.ThrowIfNull(tagManager);
            ArgumentNullException.ThrowIfNull(lookup);
            ArgumentNullException.ThrowIfNull(vault);
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(dateProvider);
            ArgumentNullException.ThrowIfNull(settings);

            _noteRepository = noteRepository;
            _logRepository = logRepository;
            _tagManager = tagManager;
            _lookup = lookup;
            _vault = vault;
            _noteManager = noteManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dateProvider = dateProvider;
            _settings = settings;
        }

        // ============================================================
        // Upload / Replace
        // ============================================================

        public Task<ProcessingResult<MigrationUploadResult>> UploadAsync(
            int authorId,
            MigrationEnvelope envelope,
            IReadOnlyList<MigrationVaultBlob> blobs,
            CancellationToken cancellationToken = default)
            => IngestAsync(
                authorId, envelope, blobs,
                MigrationLogKind.UploadFromLocal,
                wipeFirst: false, cancellationToken);

        public Task<ProcessingResult<MigrationUploadResult>> ReplaceAsync(
            int authorId,
            MigrationEnvelope envelope,
            IReadOnlyList<MigrationVaultBlob> blobs,
            CancellationToken cancellationToken = default)
            => IngestAsync(
                authorId, envelope, blobs,
                MigrationLogKind.CloudReplaced,
                wipeFirst: true, cancellationToken);

        private async Task<ProcessingResult<MigrationUploadResult>> IngestAsync(
            int authorId,
            MigrationEnvelope envelope,
            IReadOnlyList<MigrationVaultBlob> blobs,
            MigrationLogKind kind,
            bool wipeFirst,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            ArgumentNullException.ThrowIfNull(blobs);

            try
            {
                if (wipeFirst)
                {
                    var wipeResult = await WipeAuthorAsync(authorId, cancellationToken);
                    if (!wipeResult.Success)
                    {
                        return ProcessingResult<MigrationUploadResult>.Fail(
                            wipeResult.ErrorMessage, wipeResult.ErrorType);
                    }
                }

                var author = await ResolveAuthorAsync(authorId);
                if (author == null)
                {
                    return ProcessingResult<MigrationUploadResult>.NotFound(
                        $"Author {authorId} not found.");
                }

                var catalogs = await GetCatalogIndexAsync();
                var errors = new List<MigrationRecordError>();

                int notesPersisted = 0;
                int notesFailed = 0;
                for (int i = 0; i < envelope.Notes.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var record = envelope.Notes[i];

                    if (!catalogs.TryGetValue(record.CatalogName, out var catalog))
                    {
                        errors.Add(new MigrationRecordError
                        {
                            Index = i,
                            Message = $"Catalog \"{record.CatalogName}\" not found.",
                        });
                        notesFailed++;
                        continue;
                    }

                    NoteAttachments attachments;
                    try
                    {
                        attachments = NoteAttachmentsCodec.Decode(record.AttachmentsJson);
                    }
                    catch (FormatException ex)
                    {
                        errors.Add(new MigrationRecordError
                        {
                            Index = i,
                            Message = $"Invalid attachments: {ex.Message}",
                        });
                        notesFailed++;
                        continue;
                    }

                    var tags = await UpsertTagsAsync(record.TagNames);
                    var note = new HmmNote
                    {
                        Author = author,
                        Catalog = catalog,
                        Subject = record.Subject,
                        Content = record.Content,
                        Description = record.Description,
                        CreateDate = record.CreateDate == default
                            ? _dateProvider.UtcNow
                            : record.CreateDate,
                        LastModifiedDate = record.LastModifiedDate == default
                            ? _dateProvider.UtcNow
                            : record.LastModifiedDate,
                        IsDeleted = record.IsDeleted,
                        PrimaryImage = attachments.PrimaryImage,
                        Images = attachments.Images.ToList(),
                        Tags = tags,
                    };

                    var createResult = await _noteManager.CreateAsync(note, commitChanges: false);
                    if (!createResult.Success)
                    {
                        errors.Add(new MigrationRecordError
                        {
                            Index = i,
                            Message = createResult.ErrorMessage ?? "Unknown error.",
                        });
                        notesFailed++;
                    }
                    else
                    {
                        notesPersisted++;
                    }
                }

                // Vault bytes. Per-blob validation; bad blobs surface
                // as errors rather than aborting the whole upload.
                int vaultFilesPersisted = 0;
                long vaultBytes = 0;
                foreach (var blob in blobs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var blobError = ValidateBlob(blob);
                    if (blobError != null)
                    {
                        errors.Add(new MigrationRecordError
                        {
                            Index = -1,
                            Message = $"Blob {blob.RelativePath}: {blobError}",
                        });
                        continue;
                    }

                    try
                    {
                        await _vault.PutBytesAsync(
                            authorId, blob.RelativePath, blob.Bytes,
                            blob.ContentType, cancellationToken);
                        vaultFilesPersisted++;
                        vaultBytes += blob.Bytes.LongLength;
                    }
                    catch (ArgumentException ex)
                    {
                        errors.Add(new MigrationRecordError
                        {
                            Index = -1,
                            Message = $"Blob {blob.RelativePath}: {ex.Message}",
                        });
                    }
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                var result = new MigrationUploadResult
                {
                    NotesPersisted = notesPersisted,
                    NotesFailed = notesFailed,
                    VaultFilesPersisted = vaultFilesPersisted,
                    VaultBytes = vaultBytes,
                    Errors = errors,
                };

                // Audit row goes in via its own commit so the log is
                // never lost if the calling layer rolls back. Same
                // shape on success and partial-success.
                await WriteLogAsync(
                    authorId, envelope.DeviceIdentifier, kind,
                    BuildCounts(result, envelope.ClientRecordCounts),
                    cancellationToken);

                return ProcessingResult<MigrationUploadResult>.Ok(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return ProcessingResult<MigrationUploadResult>.FromException(ex);
            }
        }

        // ============================================================
        // Export
        // ============================================================

        public async Task<ProcessingResult<MigrationUploadResult>> ExportAsync(
            int authorId,
            Stream zipStream,
            string? deviceIdentifier = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(zipStream);
            try
            {
                var notesDaoResult = await _noteRepository.GetEntitiesAsync(
                    n => n.Author.Id == authorId && !n.IsDeleted,
                    new ResourceCollectionParameters
                    {
                        PageSize = int.MaxValue,
                    });
                if (!notesDaoResult.Success)
                {
                    return ProcessingResult<MigrationUploadResult>.Fail(
                        notesDaoResult.ErrorMessage, notesDaoResult.ErrorType);
                }
                var notes = _mapper.Map<PageList<HmmNote>>(notesDaoResult.Value);

                int notesPersisted = 0;
                int vaultFilesPersisted = 0;
                long vaultBytes = 0;

                // Leave-open the underlying stream so the caller
                // controls flush + disposal.
                using (var archive = new ZipArchive(
                    zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    // records.json — one entry containing the entire
                    // record set. Streamed straight into the archive
                    // entry, no intermediate buffer for the whole
                    // file.
                    var recordsEntry = archive.CreateEntry(
                        "records.json", CompressionLevel.Fastest);
                    using (var entryStream = recordsEntry.Open())
                    {
                        var records = notes
                            .Select(ToExportRecord)
                            .ToList();
                        await JsonSerializer.SerializeAsync(
                            entryStream, records, ExportJson, cancellationToken);
                    }
                    notesPersisted = notes.Count;

                    // Vault contents — copy every file the author
                    // owns into the archive at its full relative
                    // path so the layout mirrors the live vault.
                    var entries = await _vault.ListAsync(
                        authorId, prefix: string.Empty, cancellationToken);
                    foreach (var entry in entries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var bytes = await _vault.GetBytesAsync(
                            authorId, entry.RelativePath, cancellationToken);
                        if (bytes == null) continue;
                        var archiveEntry = archive.CreateEntry(
                            entry.RelativePath, CompressionLevel.NoCompression);
                        using var es = archiveEntry.Open();
                        await es.WriteAsync(bytes, cancellationToken);
                        vaultFilesPersisted++;
                        vaultBytes += bytes.LongLength;
                    }
                }

                var result = new MigrationUploadResult
                {
                    NotesPersisted = notesPersisted,
                    VaultFilesPersisted = vaultFilesPersisted,
                    VaultBytes = vaultBytes,
                };
                await WriteLogAsync(
                    authorId, deviceIdentifier, MigrationLogKind.ExportToLocal,
                    BuildCounts(result, clientCounts: null),
                    cancellationToken);

                return ProcessingResult<MigrationUploadResult>.Ok(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return ProcessingResult<MigrationUploadResult>.FromException(ex);
            }
        }

        // ============================================================
        // Log
        // ============================================================

        public async Task<ProcessingResult<IReadOnlyList<MigrationLog>>> GetLogAsync(
            int authorId,
            int take = 20,
            CancellationToken cancellationToken = default)
        {
            if (take <= 0) take = 20;
            try
            {
                var daoResult = await _logRepository.GetEntitiesAsync(
                    m => m.AuthorId == authorId,
                    new ResourceCollectionParameters
                    {
                        PageSize = take,
                        OrderBy = "At desc",
                    });
                if (!daoResult.Success)
                {
                    return ProcessingResult<IReadOnlyList<MigrationLog>>.Fail(
                        daoResult.ErrorMessage, daoResult.ErrorType);
                }
                var logs = daoResult.Value
                    .Select(d => _mapper.Map<MigrationLog>(d))
                    .ToList();
                return ProcessingResult<IReadOnlyList<MigrationLog>>.Ok(logs);
            }
            catch (Exception ex)
            {
                return ProcessingResult<IReadOnlyList<MigrationLog>>.FromException(ex);
            }
        }

        // ============================================================
        // Helpers
        // ============================================================

        private async Task<ProcessingResult<Unit>> WipeAuthorAsync(
            int authorId, CancellationToken cancellationToken)
        {
            // Vault first — even if note delete fails we want the
            // bytes gone (the user asked for a replace).
            var entries = await _vault.ListAsync(
                authorId, prefix: string.Empty, cancellationToken);
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _vault.DeleteAsync(authorId, entry.RelativePath, cancellationToken);
            }

            // Notes. Pull every row (including soft-deleted) and
            // hard-delete via the repository so the join-table FK
            // cascade clears NoteTagRefs.
            var noteResult = await _noteRepository.GetEntitiesAsync(
                n => n.Author.Id == authorId,
                new ResourceCollectionParameters { PageSize = int.MaxValue });
            if (!noteResult.Success)
            {
                return ProcessingResult<Unit>.Fail(
                    noteResult.ErrorMessage, noteResult.ErrorType);
            }
            foreach (var dao in noteResult.Value)
            {
                var delete = await _noteRepository.DeleteAsync(dao);
                if (!delete.Success)
                {
                    return ProcessingResult<Unit>.Fail(
                        delete.ErrorMessage, delete.ErrorType);
                }
            }
            await _unitOfWork.CommitAsync(cancellationToken);
            return ProcessingResult<Unit>.Ok(Unit.Value, "Author vault + notes wiped.");
        }

        private async Task<Author?> ResolveAuthorAsync(int authorId)
        {
            var result = await _lookup.GetEntityAsync<AuthorDao>(authorId);
            if (!result.Success || result.Value == null) return null;
            return _mapper.Map<Author>(result.Value);
        }

        private async Task<Dictionary<string, NoteCatalog>> GetCatalogIndexAsync()
        {
            var result = await _lookup.GetEntitiesAsync<NoteCatalogDao>(null);
            if (!result.Success || result.Value == null)
            {
                return new Dictionary<string, NoteCatalog>(StringComparer.OrdinalIgnoreCase);
            }
            return result.Value
                .Select(d => _mapper.Map<NoteCatalog>(d))
                .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<List<Tag>> UpsertTagsAsync(IList<string> tagNames)
        {
            var tags = new List<Tag>();
            if (tagNames == null || tagNames.Count == 0) return tags;

            var distinct = tagNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (distinct.Count == 0) return tags;

            var lookupResult = await _tagManager.GetTagsByNamesAsync(distinct);
            var existing = lookupResult.Success && lookupResult.Value != null
                ? lookupResult.Value
                : new Dictionary<string, Tag>();

            foreach (var name in distinct)
            {
                if (existing.TryGetValue(name.ToLowerInvariant(), out var existingTag))
                {
                    tags.Add(existingTag);
                    continue;
                }
                var created = await _tagManager.CreateAsync(new Tag
                {
                    Name = name,
                    IsActivated = true,
                });
                if (created.Success && created.Value != null)
                {
                    tags.Add(created.Value);
                }
                // Silent skip on failure — the note still imports
                // without the unknown tag (a tag failure shouldn't
                // sink the record).
            }
            return tags;
        }

        private string? ValidateBlob(MigrationVaultBlob blob)
        {
            if (string.IsNullOrEmpty(blob.RelativePath))
                return "missing relative path";
            try { VaultPathUtil.Validate(blob.RelativePath); }
            catch (ArgumentException ex) { return ex.Message; }

            var ct = (blob.ContentType ?? string.Empty).Trim();
            var semi = ct.IndexOf(';');
            if (semi >= 0) ct = ct[..semi].Trim();
            if (!_settings.AllowedContentTypes.Contains(ct))
            {
                return $"content-type \"{ct}\" not allowed";
            }
            if (blob.Bytes == null || blob.Bytes.Length == 0)
                return "empty body";
            if (blob.Bytes.LongLength > _settings.MaxBytes)
                return $"exceeds max size {_settings.MaxBytes}";
            return null;
        }

        private async Task WriteLogAsync(
            int authorId, string? deviceIdentifier, MigrationLogKind kind,
            string? counts, CancellationToken cancellationToken)
        {
            var dao = new MigrationLogDao
            {
                AuthorId = authorId,
                DeviceIdentifier = deviceIdentifier,
                Kind = kind,
                RecordCounts = counts,
                At = _dateProvider.UtcNow,
            };
            await _logRepository.AddAsync(dao);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        private static string BuildCounts(
            MigrationUploadResult result, string? clientCounts)
        {
            // Merge: client-supplied counts first (e.g. resolvedPhAssets,
            // resolvedCloudFiles, unresolvedRefs from Free → Paid
            // resolution) + server-computed counts on top. Server
            // values win on key collision.
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms))
            {
                writer.WriteStartObject();
                if (!string.IsNullOrWhiteSpace(clientCounts))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(clientCounts);
                        if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                if (IsServerOwned(prop.Name)) continue;
                                prop.WriteTo(writer);
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Garbage in → ignore the client counts; the
                        // server-computed fields are the source of
                        // truth for an audit row.
                    }
                }
                writer.WriteNumber("notes", result.NotesPersisted);
                writer.WriteNumber("notesFailed", result.NotesFailed);
                writer.WriteNumber("vaultFiles", result.VaultFilesPersisted);
                writer.WriteNumber("vaultBytes", result.VaultBytes);
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static bool IsServerOwned(string key) => key switch
        {
            "notes" or "notesFailed" or "vaultFiles" or "vaultBytes" => true,
            _ => false,
        };

        private static object ToExportRecord(HmmNote note)
        {
            // Anonymous shape on purpose — matches the
            // MigrationNoteRecord wire shape exactly so a future
            // re-import is symmetric with the export.
            return new
            {
                subject = note.Subject,
                content = note.Content,
                catalogName = note.Catalog?.Name,
                tagNames = note.Tags?.Select(t => t.Name).ToList()
                    ?? new List<string>(),
                attachmentsJson = NoteAttachmentsCodec.Encode(
                    new NoteAttachments(note.PrimaryImage, note.Images)),
                description = note.Description,
                createDate = note.CreateDate,
                lastModifiedDate = note.LastModifiedDate,
                isDeleted = note.IsDeleted,
            };
        }
    }
}
