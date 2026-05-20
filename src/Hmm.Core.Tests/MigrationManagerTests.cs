using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Map.Migration;
using Hmm.Core.Vault;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hmm.Core.Tests;

/// <summary>
/// Manager-level coverage for the Phase 7 migration stack. Upload /
/// Replace / Export / Log composed against the
/// <see cref="CoreTestFixtureBase"/> mocks + an in-memory vault
/// store.
/// </summary>
public class MigrationManagerTests : CoreTestFixtureBase, IAsyncLifetime
{
    private InMemoryVaultBlobStore _vault = null!;
    private List<MigrationLogDao> _logRows = null!;
    private Mock<IRepository<MigrationLogDao>> _logRepo = null!;
    private TagManager _tagManager = null!;
    private HmmNoteManager _noteManager = null!;
    private MigrationManager _manager = null!;
    private Author _author = null!;
    private NoteCatalog _catalog = null!;
    private readonly AttachmentSettings _settings = new()
    {
        // Match production defaults so the validation paths fire
        // realistically.
        MaxBytes = 8 * 1024 * 1024,
        AllowedContentTypes = new List<string>
        {
            "image/jpeg", "image/png", "image/heic", "image/webp",
        },
    };

    public async Task InitializeAsync()
    {
        _author = await GetTestAuthor();
        _catalog = await GetTestCatalog();
        _vault = new InMemoryVaultBlobStore();
        _logRows = new List<MigrationLogDao>();
        _logRepo = new Mock<IRepository<MigrationLogDao>>();
        _logRepo.Setup(r => r.AddAsync(It.IsAny<MigrationLogDao>()))
            .ReturnsAsync((MigrationLogDao d) =>
            {
                d.Id = _logRows.Count + 1;
                _logRows.Add(d);
                return ProcessingResult<MigrationLogDao>.Ok(d);
            });
        _logRepo.Setup(r => r.GetEntitiesAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MigrationLogDao, bool>>>(),
                It.IsAny<ResourceCollectionParameters>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<MigrationLogDao, bool>> q,
                ResourceCollectionParameters _) =>
            {
                var rows = q == null
                    ? _logRows.AsQueryable()
                    : _logRows.AsQueryable().Where(q);
                return ProcessingResult<PageList<MigrationLogDao>>.Ok(
                    PageList<MigrationLogDao>.Create(rows, 1, 50));
            });

        // The fixture leaves DeleteAsync un-mocked — Replace needs
        // it. Set it up to actually remove from the underlying
        // mocked NoteRepository. The fixture's repository is built
        // via Moq.Mock, so reach back through Mock.Get and add the
        // setup.
        var fixtureNoteMock = Mock.Get(NoteRepository);
        var noteList = GetSeededNoteListViaReflection();
        fixtureNoteMock.Setup(r => r.DeleteAsync(It.IsAny<HmmNoteDao>()))
            .ReturnsAsync((HmmNoteDao dao) =>
            {
                var existing = noteList.FirstOrDefault(n => n.Id == dao.Id);
                if (existing != null) noteList.Remove(existing);
                return ProcessingResult<Unit>.Ok(Unit.Value, "removed");
            });

        // Wire real managers on top of the fixture mocks so the
        // migration manager exercises the same flow the controller
        // sees in production.
        var tagValidator = new TagValidator(TagRepository);
        _tagManager = new TagManager(
            TagRepository, UnitOfWork, Mapper, LookupRepository, tagValidator);
        var noteValidator = new NoteValidator(LookupRepository);
        _noteManager = new HmmNoteManager(
            NoteRepository, UnitOfWork, Mapper, LookupRepository, DateProvider, noteValidator);

        _manager = new MigrationManager(
            NoteRepository,
            _logRepo.Object,
            _tagManager,
            LookupRepository,
            _vault,
            _noteManager,
            UnitOfWork,
            Mapper,
            DateProvider,
            Options.Create(_settings));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private List<HmmNoteDao> GetSeededNoteListViaReflection()
    {
        // Reach into the fixture's private _noteDaos so Replace's
        // hard-delete actually mutates the same list the fixture
        // uses for the rest of the mock surface.
        var field = typeof(CoreTestFixtureBase).GetField(
            "_noteDaos",
            System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        return (List<HmmNoteDao>)field!.GetValue(this)!;
    }

    private MigrationNoteRecord Record(
        string subject = "subject",
        string content = "content",
        string? attachmentsJson = null,
        IList<string>? tagNames = null,
        string? catalogName = null)
    {
        return new MigrationNoteRecord
        {
            Subject = subject,
            Content = content,
            CatalogName = catalogName ?? _catalog.Name,
            TagNames = tagNames ?? new List<string>(),
            AttachmentsJson = attachmentsJson,
            CreateDate = new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc),
            LastModifiedDate = new DateTime(2026, 5, 18, 11, 0, 0, DateTimeKind.Utc),
        };
    }

    private static MigrationVaultBlob Blob(
        string relativePath,
        byte[]? bytes = null,
        string contentType = "image/jpeg")
        => new()
        {
            RelativePath = relativePath,
            ContentType = contentType,
            Bytes = bytes ?? new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
        };

    // ============================================================
    // Upload
    // ============================================================

    [Fact]
    public async Task Upload_persists_notes_and_vault_files()
    {
        var envelope = new MigrationEnvelope
        {
            Notes = new List<MigrationNoteRecord>
            {
                Record(subject: "one"),
                Record(subject: "two", tagNames: new List<string> { "new-tag" }),
            },
        };
        var blobs = new[]
        {
            Blob("attachments/note-1/a.jpg"),
        };

        var result = await _manager.UploadAsync(_author.Id, envelope, blobs);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.NotesPersisted);
        Assert.Equal(0, result.Value.NotesFailed);
        Assert.Equal(1, result.Value.VaultFilesPersisted);
        Assert.Equal(4L, result.Value.VaultBytes);
        Assert.Empty(result.Value.Errors);
        Assert.True(await _vault.ExistsAsync(_author.Id, "attachments/note-1/a.jpg"));
    }

    [Fact]
    public async Task Upload_reports_unknown_catalog_as_per_record_error()
    {
        var envelope = new MigrationEnvelope
        {
            Notes = new List<MigrationNoteRecord>
            {
                Record(subject: "good"),
                Record(subject: "bad", catalogName: "no-such-catalog"),
            },
        };

        var result = await _manager.UploadAsync(_author.Id, envelope, Array.Empty<MigrationVaultBlob>());

        Assert.True(result.Success);
        Assert.Equal(1, result.Value!.NotesPersisted);
        Assert.Equal(1, result.Value.NotesFailed);
        Assert.Single(result.Value.Errors);
        Assert.Equal(1, result.Value.Errors[0].Index);
        Assert.Contains("catalog", result.Value.Errors[0].Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_reports_invalid_attachments_as_per_record_error()
    {
        var envelope = new MigrationEnvelope
        {
            Notes = new List<MigrationNoteRecord>
            {
                Record(subject: "bad",
                    attachmentsJson: "{ \"primaryImage\": \"not-an-object\" }"),
            },
        };

        var result = await _manager.UploadAsync(_author.Id, envelope, Array.Empty<MigrationVaultBlob>());

        Assert.True(result.Success);
        Assert.Equal(0, result.Value!.NotesPersisted);
        Assert.Single(result.Value.Errors);
        Assert.Contains("attachments", result.Value.Errors[0].Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_rejects_oversize_blob_as_blob_level_error()
    {
        var envelope = new MigrationEnvelope
        {
            Notes = new List<MigrationNoteRecord> { Record() },
        };
        var huge = new byte[_settings.MaxBytes + 1];
        var blobs = new[] { Blob("attachments/note-1/big.jpg", huge) };

        var result = await _manager.UploadAsync(_author.Id, envelope, blobs);

        Assert.True(result.Success);
        Assert.Equal(0, result.Value!.VaultFilesPersisted);
        var err = Assert.Single(result.Value.Errors);
        Assert.Equal(-1, err.Index);
        Assert.Contains("exceeds max size", err.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_rejects_disallowed_content_type_as_blob_level_error()
    {
        var envelope = new MigrationEnvelope
        {
            Notes = new List<MigrationNoteRecord> { Record() },
        };
        var blobs = new[]
        {
            Blob("attachments/note-1/a.gif", contentType: "image/gif"),
        };

        var result = await _manager.UploadAsync(_author.Id, envelope, blobs);

        Assert.True(result.Success);
        Assert.Equal(0, result.Value!.VaultFilesPersisted);
        var err = Assert.Single(result.Value.Errors);
        Assert.Contains("not allowed", err.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_writes_migration_log_with_counts()
    {
        var envelope = new MigrationEnvelope
        {
            DeviceIdentifier = "test-device-1",
            ClientRecordCounts = "{\"resolvedPhAssets\":2}",
            Notes = new List<MigrationNoteRecord> { Record() },
        };

        await _manager.UploadAsync(_author.Id, envelope,
            new[] { Blob("attachments/note-1/a.jpg") });

        var log = Assert.Single(_logRows);
        Assert.Equal(_author.Id, log.AuthorId);
        Assert.Equal("test-device-1", log.DeviceIdentifier);
        Assert.Equal(MigrationLogKind.UploadFromLocal, log.Kind);
        Assert.NotNull(log.RecordCounts);
        using var doc = JsonDocument.Parse(log.RecordCounts!);
        Assert.Equal(2, doc.RootElement.GetProperty("resolvedPhAssets").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("notes").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("vaultFiles").GetInt32());
    }

    // ============================================================
    // Replace
    // ============================================================

    [Fact]
    public async Task Replace_wipes_existing_notes_and_vault_then_uploads_new()
    {
        // Pre-seed: upload one note + one blob.
        await _manager.UploadAsync(_author.Id,
            new MigrationEnvelope
            {
                Notes = new List<MigrationNoteRecord>
                {
                    Record(subject: "original"),
                },
            },
            new[] { Blob("attachments/note-1/orig.jpg") });
        Assert.True(await _vault.ExistsAsync(_author.Id, "attachments/note-1/orig.jpg"));

        // Replace: different note, different blob.
        var result = await _manager.ReplaceAsync(_author.Id,
            new MigrationEnvelope
            {
                Notes = new List<MigrationNoteRecord>
                {
                    Record(subject: "replacement"),
                },
            },
            new[] { Blob("attachments/note-2/new.jpg") });

        Assert.True(result.Success);
        Assert.Equal(1, result.Value!.NotesPersisted);
        Assert.Equal(1, result.Value.VaultFilesPersisted);
        // Old vault file gone, new one present.
        Assert.False(await _vault.ExistsAsync(_author.Id, "attachments/note-1/orig.jpg"));
        Assert.True(await _vault.ExistsAsync(_author.Id, "attachments/note-2/new.jpg"));
        // Two log rows — the seeded upload + the replace.
        Assert.Equal(2, _logRows.Count);
        Assert.Equal(MigrationLogKind.UploadFromLocal, _logRows[0].Kind);
        Assert.Equal(MigrationLogKind.CloudReplaced, _logRows[1].Kind);
    }

    // ============================================================
    // Export
    // ============================================================

    [Fact]
    public async Task Export_streams_records_json_and_vault_files_as_zip()
    {
        // Seed via upload so the records + vault are in a known
        // consistent state.
        await _manager.UploadAsync(_author.Id,
            new MigrationEnvelope
            {
                Notes = new List<MigrationNoteRecord>
                {
                    Record(subject: "exported"),
                },
            },
            new[] { Blob("attachments/note-1/x.jpg") });
        _logRows.Clear();

        using var ms = new MemoryStream();
        var result = await _manager.ExportAsync(_author.Id, ms);

        Assert.True(result.Success);
        Assert.Equal(1, result.Value!.VaultFilesPersisted);
        Assert.True(result.Value.NotesPersisted >= 1);
        // Inspect the produced zip.
        ms.Position = 0;
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var recordsEntry = Assert.Single(archive.Entries, e => e.FullName == "records.json");
        using var recordsStream = recordsEntry.Open();
        using var reader = new StreamReader(recordsStream, Encoding.UTF8);
        var recordsJson = await reader.ReadToEndAsync();
        Assert.Contains("exported", recordsJson);
        var vaultEntry = Assert.Single(archive.Entries,
            e => e.FullName == "attachments/note-1/x.jpg");
        Assert.Equal(4, vaultEntry.Length);
        // One log row for the export.
        var log = Assert.Single(_logRows);
        Assert.Equal(MigrationLogKind.ExportToLocal, log.Kind);
    }

    // ============================================================
    // Log
    // ============================================================

    [Fact]
    public async Task GetLogAsync_returns_recent_entries_for_author()
    {
        await _manager.UploadAsync(_author.Id,
            new MigrationEnvelope { Notes = new List<MigrationNoteRecord> { Record() } },
            Array.Empty<MigrationVaultBlob>());
        await _manager.UploadAsync(_author.Id,
            new MigrationEnvelope { Notes = new List<MigrationNoteRecord> { Record() } },
            Array.Empty<MigrationVaultBlob>());

        var logResult = await _manager.GetLogAsync(_author.Id, take: 10);

        Assert.True(logResult.Success);
        Assert.NotNull(logResult.Value);
        Assert.Equal(2, logResult.Value!.Count);
        Assert.All(logResult.Value, l => Assert.Equal(_author.Id, l.AuthorId));
    }

    // ============================================================
    // In-memory vault — minimal dict-backed IVaultBlobStore so the
    // manager exercises the same interface contract production
    // uses. Validates relativePath via VaultPathUtil exactly like
    // the filesystem impl.
    // ============================================================

    private sealed class InMemoryVaultBlobStore : IVaultBlobStore
    {
        private readonly Dictionary<(int authorId, string path), byte[]> _store = new();

        public Task PutBytesAsync(int authorId, string relativePath,
            ReadOnlyMemory<byte> bytes, string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            VaultPathUtil.Validate(relativePath);
            _store[(authorId, relativePath)] = bytes.ToArray();
            return Task.CompletedTask;
        }

        public Task<byte[]?> GetBytesAsync(int authorId, string relativePath,
            CancellationToken cancellationToken = default)
        {
            VaultPathUtil.Validate(relativePath);
            return Task.FromResult(_store.TryGetValue((authorId, relativePath), out var b)
                ? b : null);
        }

        public Task<bool> ExistsAsync(int authorId, string relativePath,
            CancellationToken cancellationToken = default)
        {
            VaultPathUtil.Validate(relativePath);
            return Task.FromResult(_store.ContainsKey((authorId, relativePath)));
        }

        public Task DeleteAsync(int authorId, string relativePath,
            CancellationToken cancellationToken = default)
        {
            VaultPathUtil.Validate(relativePath);
            _store.Remove((authorId, relativePath));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<VaultEntry>> ListAsync(int authorId, string prefix,
            CancellationToken cancellationToken = default)
        {
            var hits = _store
                .Where(kv => kv.Key.authorId == authorId
                             && (prefix.Length == 0 || kv.Key.path.StartsWith(prefix)))
                .Select(kv => new VaultEntry
                {
                    RelativePath = kv.Key.path,
                    ByteSize = kv.Value.Length,
                })
                .ToList();
            return Task.FromResult<IReadOnlyList<VaultEntry>>(hits);
        }
    }
}
