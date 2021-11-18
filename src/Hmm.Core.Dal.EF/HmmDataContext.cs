using Hmm.Core.DomainEntity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF
{
    public class HmmDataContext : DbContext, IHmmDataContext
    {
        public HmmDataContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<HmmNote> Notes { get; set; }

        public DbSet<Author> Authors { get; set; }

        public DbSet<NoteRender> Renders { get; set; }

        public DbSet<Subsystem> Subsystems { get; set; }

        public DbSet<NoteCatalog> Catalogs { get; set; }

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
            modelBuilder.Entity<HmmNote>().ToTable("Notes")
                .Property(n => n.Version)
                .HasColumnName("Ts")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();
            modelBuilder.Entity<HmmNote>()
                .HasKey(n => n.Id);
            modelBuilder.Entity<Author>().ToTable("Authors");
            modelBuilder.Entity<Subsystem>().ToTable("Subsystems");
            modelBuilder.Entity<NoteCatalog>().ToTable("NoteCatalogs")
                .HasOne(c => c.Subsystem)
                .WithMany(s => s.NoteCatalogs);
            modelBuilder.Entity<NoteRender>().ToTable("NoteRenders");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .EnableSensitiveDataLogging();
        }
    }
}