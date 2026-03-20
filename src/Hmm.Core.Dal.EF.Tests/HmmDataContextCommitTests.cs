using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Hmm.Core.Dal.EF.Repositories;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.DataEntity;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hmm.Core.Dal.EF.Tests
{
    public class TestHmmDataContext(DbContextOptions options) : DbContext(options), IHmmDataContext
    {
        public DbSet<HmmNoteDao> Notes { get; set; }
        public DbSet<AuthorDao> Authors { get; set; }
        public DbSet<NoteCatalogDao> Catalogs { get; set; }
        public DbSet<TagDao> Tags { get; set; }
        public DbSet<NoteTagRefDao> NoteTagRefs { get; set; }

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

        public async Task<T> GetDefaultEntityAsync<T>() where T : HasDefaultEntity
        {
            return await Set<T>().FirstOrDefaultAsync(e => e.IsDefault);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HmmNoteDao>().ToTable("notes")
                .Property(n => n.Version)
                .HasColumnType("BLOB") // SQLite equivalent for bytea
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
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            modelBuilder.Entity<HmmNoteDao>()
                .Property(n => n.LastModifiedDate)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));


            modelBuilder.Entity<HmmNoteDao>()
                .HasKey(n => n.Id);

            modelBuilder.Entity<AuthorDao>().ToTable("authors");
            modelBuilder.Entity<ContactDao>().ToTable("contacts");
            modelBuilder.Entity<NoteCatalogDao>().ToTable("notecatalogs")
                .Property(e => e.Schema)
                .HasColumnType("TEXT"); // SQLite equivalent for XML/string
            modelBuilder.Entity<NoteCatalogDao>()
                .Property(e => e.FormatType)
                .HasConversion<int>(); // Map enum to int for SQLite

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

    public class HmmDataContextCommitTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly SqliteConnection _sharedConnection;

        public HmmDataContextCommitTests()
        {
            // Create a shared connection that stays open for all scopes
            _sharedConnection = new SqliteConnection("DataSource=:memory:");
            _sharedConnection.Open();

            var services = new ServiceCollection();

            // Register DbContext with the shared connection
            services.AddDbContext<TestHmmDataContext>(options => options.UseSqlite(_sharedConnection));
            services.AddScoped<IHmmDataContext>(sp => sp.GetRequiredService<TestHmmDataContext>());

            // Register repositories and other dependencies
            services.AddScoped<IVersionRepository<HmmNoteDao>, NoteEfRepository>();
            services.AddScoped<IRepository<AuthorDao>, AuthorEfRepository>();
            services.AddScoped<IRepository<NoteCatalogDao>, NoteCatalogEfRepository>();
            services.AddScoped<IEntityLookup, EfEntityLookup>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddAutoMapper(cfg => cfg.AddProfile<HmmMappingProfile>());
            services.AddSingleton<IDateTimeProvider, DateTimeAdapter>();
            services.AddSingleton<ILogger<NoteEfRepository>>(new Mock<ILogger<NoteEfRepository>>().Object);
            services.AddSingleton<ILogger<AuthorEfRepository>>(new Mock<ILogger<AuthorEfRepository>>().Object);
            services.AddSingleton<ILogger<NoteCatalogEfRepository>>(new Mock<ILogger<NoteCatalogEfRepository>>().Object);
            services.AddSingleton<ILogger<EfEntityLookup>>(new Mock<ILogger<EfEntityLookup>>().Object);

            _serviceProvider = services.BuildServiceProvider();

            // Ensure the database is created and seeded for each test run
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TestHmmDataContext>();
                context.Database.EnsureCreated();

                // Seed data for default author and catalog
                SeedData(context).Wait();
            }
        }

        private async Task SeedData(TestHmmDataContext context)
        {
            if (!context.Authors.Any())
            {
                var author = new AuthorDao { AccountName = "TestAuthor", Description = "Test Author" };
                context.Authors.Add(author);
                await context.SaveChangesAsync();
            }

            if (!context.Catalogs.Any())
            {
                var catalog = new NoteCatalogDao { Name = "DefaultCatalog", IsDefault = true, FormatType = NoteContentFormatType.PlainText, Schema = "<root/>" };
                context.Catalogs.Add(catalog);
                await context.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task Changes_Are_Persisted_When_Commit_Is_Called()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var noteRepository = scope.ServiceProvider.GetRequiredService<IVersionRepository<HmmNoteDao>>();
            var dataContext = scope.ServiceProvider.GetRequiredService<IHmmDataContext>();
            var author = await dataContext.Set<AuthorDao>().FirstAsync();
            var catalog = await dataContext.Set<NoteCatalogDao>().FirstAsync();

            var note = new HmmNoteDao
            {
                Author = author,
                Catalog = catalog,
                Subject = "Test Note Subject",
                Description = "Test Note Description",
                Content = "<root><data>some content</data></root>"
            };

            // Act
            var addResult = await noteRepository.AddAsync(note);
            Assert.True(addResult.Success);

            // Explicitly commit the changes
            await dataContext.CommitAsync();

            // Assert - use a new context to verify persistence
            using (var verificationScope = _serviceProvider.CreateScope())
            {
                var verificationContext = verificationScope.ServiceProvider.GetRequiredService<IHmmDataContext>();
                var persistedNote = await verificationContext.Set<HmmNoteDao>()
                                                            .Include(n => n.Author)
                                                            .Include(n => n.Catalog)
                                                            .FirstOrDefaultAsync(n => n.Id == note.Id);

                Assert.NotNull(persistedNote);
                Assert.Equal(note.Subject, persistedNote.Subject);
                Assert.Equal(note.Description, persistedNote.Description);
            }
        }

        [Fact]
        public async Task Changes_Are_NOT_Persisted_When_Commit_Is_NOT_Called()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var noteRepository = scope.ServiceProvider.GetRequiredService<IVersionRepository<HmmNoteDao>>();
            var dataContext = scope.ServiceProvider.GetRequiredService<IHmmDataContext>();
            var author = await dataContext.Set<AuthorDao>().FirstAsync();
            var catalog = await dataContext.Set<NoteCatalogDao>().FirstAsync();

            var note = new HmmNoteDao
            {
                Author = author,
                Catalog = catalog,
                Subject = "Uncommitted Note Subject",
                Description = "Uncommitted Note Description",
                Content = "<root><data>uncommitted content</data></root>"
            };

            // Act
            var addResult = await noteRepository.AddAsync(note);
            Assert.True(addResult.Success);

            // DO NOT call dataContext.CommitAsync();

            // Assert - use a new context to verify that changes were NOT persisted
            using (var verificationScope = _serviceProvider.CreateScope())
            {
                var persistedNote = await verificationScope.ServiceProvider.GetRequiredService<IHmmDataContext>()
                                                            .Set<HmmNoteDao>()
                                                            .FirstOrDefaultAsync(n => n.Id == note.Id);

                Assert.Null(persistedNote);
            }
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
            _sharedConnection.Dispose();
        }
    }
}