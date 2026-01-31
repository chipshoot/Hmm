using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.DataEntity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF
{
    /// <summary>
    /// Foreign key constraint name constants for explicit naming convention.
    /// Using constants prevents magic strings and ensures consistency.
    /// </summary>
    internal static class ForeignKeyNames
    {
        public const string FK_Notes_Authors = "fk_notes_authors";
        public const string FK_Notes_Catalogs = "fk_notes_catalogs";
        public const string FK_NoteTagRefs_Notes = "fk_notetagrefs_notes";
        public const string FK_NoteTagRefs_Tags = "fk_notetagrefs_tags";
    }

    /// <summary>
    /// Index name constants for explicit naming convention.
    /// </summary>
    internal static class IndexNames
    {
        public const string IX_Notes_AuthorId = "ix_notes_authorid";
        public const string IX_Notes_CatalogId = "ix_notes_catalogid";
        public const string IX_NoteTagRefs_NoteId = "ix_notetagrefs_noteid";
        public const string IX_NoteTagRefs_TagId = "ix_notetagrefs_tagid";
        public const string IX_Authors_AccountName = "ix_authors_accountname";
        public const string IX_Tags_Name = "ix_tags_name";

        // Unique constraint names (Issue #43 fix - prevent race conditions in uniqueness validation)
        public const string UQ_Authors_AccountName = "uq_authors_accountname";
        public const string UQ_Tags_Name = "uq_tags_name";
        public const string UQ_NoteCatalogs_Name = "uq_notecatalogs_name";
    }

    public class HmmDataContext(DbContextOptions options) : DbContext(options), IHmmDataContext
    {
        public DbSet<HmmNoteDao> Notes { get; set; }

        public DbSet<AuthorDao> Authors { get; set; }

        public DbSet<ContactDao> Contacts { get; set; }

        public DbSet<NoteCatalogDao> Catalogs { get; set; }

        public DbSet<TagDao> Tags { get; set; }

        public DbSet<NoteTagRefDao> NoteTagRefs { get; set; }

        /// <inheritdoc />
        public int Commit()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <inheritdoc />
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Gets the default entity for any type that extends HasDefaultEntity.
        /// Uses EF Core's Set&lt;T&gt;() to dynamically access the DbSet, enabling Open/Closed principle compliance.
        /// </summary>
        public async Task<T> GetDefaultEntityAsync<T>() where T : HasDefaultEntity
        {
            return await Set<T>().FirstOrDefaultAsync(e => e.IsDefault);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var isPostgres = Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

            if (isPostgres)
            {
                modelBuilder.HasPostgresEnum<NoteContentFormatType>("note_content_format_type");
            }

            // Apply UTC conversion globally to all DateTime properties (Issue #26 fix)
            // This ensures consistent timezone handling across all entities
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }

            // ============================================================
            // HmmNoteDao Configuration
            // ============================================================
            modelBuilder.Entity<HmmNoteDao>().ToTable("notes");
            modelBuilder.Entity<HmmNoteDao>().HasKey(n => n.Id);

            // Configure Version property differently based on provider
            var versionProperty = modelBuilder.Entity<HmmNoteDao>()
                .Property(n => n.Version)
                .HasColumnName("ts")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();

            if (isPostgres)
            {
                versionProperty.HasColumnType("bytea");
            }

            // FK to Author with explicit constraint name and index (Issue #25 fix)
            modelBuilder.Entity<HmmNoteDao>()
                .HasOne(n => n.Author)
                .WithMany()
                .HasForeignKey("authorid")
                .HasConstraintName(ForeignKeyNames.FK_Notes_Authors)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HmmNoteDao>()
                .HasIndex("authorid")
                .HasDatabaseName(IndexNames.IX_Notes_AuthorId);

            // FK to Catalog with explicit constraint name and index
            modelBuilder.Entity<HmmNoteDao>()
                .HasOne(n => n.Catalog)
                .WithMany()
                .HasForeignKey("catalogid")
                .HasConstraintName(ForeignKeyNames.FK_Notes_Catalogs)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HmmNoteDao>()
                .HasIndex("catalogid")
                .HasDatabaseName(IndexNames.IX_Notes_CatalogId);

            // ============================================================
            // AuthorDao Configuration
            // ============================================================
            modelBuilder.Entity<AuthorDao>().ToTable("authors");

            // Unique constraint on AccountName (Issue #43 fix - prevents race conditions)
            // This database-level constraint ensures uniqueness even when concurrent requests
            // pass the application-level validation check simultaneously
            modelBuilder.Entity<AuthorDao>()
                .HasIndex(a => a.AccountName)
                .IsUnique()
                .HasDatabaseName(IndexNames.UQ_Authors_AccountName);

            // ============================================================
            // ContactDao Configuration
            // ============================================================
            modelBuilder.Entity<ContactDao>().ToTable("contacts");

            // ============================================================
            // NoteCatalogDao Configuration
            // ============================================================
            var catalogEntity = modelBuilder.Entity<NoteCatalogDao>().ToTable("notecatalogs");
            if (isPostgres)
            {
                catalogEntity.Property(e => e.Schema).HasColumnType("xml");
            }

            // ============================================================
            // TagDao Configuration
            // ============================================================
            modelBuilder.Entity<TagDao>().ToTable("tags");

            // Unique constraint on Name (Issue #43 fix - prevents race conditions)
            // This database-level constraint ensures uniqueness even when concurrent requests
            // pass the application-level validation check simultaneously
            modelBuilder.Entity<TagDao>()
                .HasIndex(t => t.Name)
                .IsUnique()
                .HasDatabaseName(IndexNames.UQ_Tags_Name);

            // ============================================================
            // NoteTagRefDao Configuration (Join Table)
            // ============================================================
            modelBuilder.Entity<NoteTagRefDao>().ToTable("notetagrefs");
            modelBuilder.Entity<NoteTagRefDao>().HasKey(nt => nt.Id);

            // FK to Note with explicit constraint name and index
            modelBuilder.Entity<NoteTagRefDao>()
                .HasOne(nt => nt.Note)
                .WithMany(n => n.Tags)
                .HasForeignKey(nt => nt.NoteId)
                .HasConstraintName(ForeignKeyNames.FK_NoteTagRefs_Notes);

            modelBuilder.Entity<NoteTagRefDao>()
                .HasIndex(nt => nt.NoteId)
                .HasDatabaseName(IndexNames.IX_NoteTagRefs_NoteId);

            // FK to Tag with explicit constraint name and index
            modelBuilder.Entity<NoteTagRefDao>()
                .HasOne(nt => nt.Tag)
                .WithMany(t => t.Notes)
                .HasForeignKey(nt => nt.TagId)
                .HasConstraintName(ForeignKeyNames.FK_NoteTagRefs_Tags);

            modelBuilder.Entity<NoteTagRefDao>()
                .HasIndex(nt => nt.TagId)
                .HasDatabaseName(IndexNames.IX_NoteTagRefs_TagId);
        }
    }
}