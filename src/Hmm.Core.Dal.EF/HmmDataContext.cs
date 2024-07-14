using Hmm.Core.Dal.EF.DbEntity;
using Hmm.Core.DomainEntity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using AuthorDao = Hmm.Core.Dal.EF.DbEntity.AuthorDao;
using ContactDao = Hmm.Core.Dal.EF.DbEntity.ContactDao;

namespace Hmm.Core.Dal.EF
{
    public class HmmDataContext(DbContextOptions options) : DbContext(options), IHmmDataContext
    {
        public DbSet<HmmNoteDao> Notes { get; set; }

        public DbSet<AuthorDao> Authors { get; set; }

        public DbSet<ContactDao> Contacts { get; set; }

        public DbSet<NoteCatalogDao> Catalogs { get; set; }


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
            modelBuilder.Entity<HmmNoteDao>().ToTable("notes")
                .Property(n => n.Version)
                .HasColumnType("bytea")
                .HasColumnName("ts")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();
            //modelBuilder.Entity<HmmNote>()
            //    .HasKey(n => n.Id);
            modelBuilder.HasPostgresEnum<NoteContentFormatType>();
            modelBuilder.Entity<AuthorDao>().ToTable("authors");
            modelBuilder.Entity<ContactDao>().ToTable("contacts");
            modelBuilder.Entity<NoteCatalogDao>().ToTable("notecatalogs")
                .Property(e => e.Schema)
                .HasColumnType("xml");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .EnableSensitiveDataLogging();
        }
    }
}