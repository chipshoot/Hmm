using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.DataEntity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF
{
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

            modelBuilder.Entity<HmmNoteDao>().ToTable("notes");

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

            modelBuilder.Entity<HmmNoteDao>()
            .HasOne(n => n.Author)
            .WithMany()
            .HasForeignKey("authorid")
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HmmNoteDao>()
            .HasOne(n => n.Catalog)
            .WithMany()
            .HasForeignKey("catalogid")
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HmmNoteDao>()
                .Property(n => n.CreateDate)
                .HasConversion(v=>v, v=>DateTime.SpecifyKind(v, DateTimeKind.Utc));
            modelBuilder.Entity<HmmNoteDao>()
                .Property(n => n.LastModifiedDate)
                .HasConversion(v=>v, v=>DateTime.SpecifyKind(v, DateTimeKind.Utc));


            modelBuilder.Entity<HmmNoteDao>()
                .HasKey(n => n.Id);

            modelBuilder.Entity<AuthorDao>().ToTable("authors");
            modelBuilder.Entity<ContactDao>().ToTable("contacts");

            // Configure Schema property - use xml type only for PostgreSQL
            var catalogEntity = modelBuilder.Entity<NoteCatalogDao>().ToTable("notecatalogs");
            if (isPostgres)
            {
                catalogEntity.Property(e => e.Schema).HasColumnType("xml");
            }

            modelBuilder.Entity<TagDao>().ToTable("tags");

            modelBuilder.Entity<NoteTagRefDao>().ToTable("notetagrefs")
                .HasKey(nt => nt.Id);
            modelBuilder.Entity<NoteTagRefDao>()
                .HasOne(nt => nt.Note)
                .WithMany(n => n.Tags)
                .HasForeignKey(nt => nt.NoteId);
            modelBuilder.Entity<NoteTagRefDao>()
                .HasOne(nt => nt.Tag)
                .WithMany(t => t.Notes)
                .HasForeignKey(nt => nt.TagId);
        }
    }
}