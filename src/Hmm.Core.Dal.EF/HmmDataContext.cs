using Hmm.Core.Map.DbEntity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF
{
    public class HmmDataContext(DbContextOptions options, IConfiguration config) : DbContext(options), IHmmDataContext
    {
        public DbSet<HmmNoteDao> Notes { get; set; }

        public DbSet<AuthorDao> Authors { get; set; }

        public DbSet<ContactDao> Contacts { get; set; }

        public DbSet<NoteCatalogDao> Catalogs { get; set; }

        public DbSet<TagDao> Tags { get; set; }

        public DbSet<NoteTagRefDao> NoteTagRefs { get; set; }

        public void Save()
        {
            try
            {
                base.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new DataSourceException(ex.Message, ex);
            }
        }

        public async Task SaveAsync()
        {
            await base.SaveChangesAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<NoteContentFormatType>("note_content_format_type");

            modelBuilder.Entity<HmmNoteDao>().ToTable("notes")
                .Property(n => n.Version)
                .HasColumnType("bytea")
                .HasColumnName("ts")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();
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
            modelBuilder.Entity<NoteCatalogDao>().ToTable("notecatalogs")
                .Property(e => e.Schema)
                .HasColumnType("xml");

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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = config.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.MapEnum<NoteContentFormatType>();

            optionsBuilder
                .UseNpgsql(dataSourceBuilder.Build())
                .EnableSensitiveDataLogging();
        }
    }
}