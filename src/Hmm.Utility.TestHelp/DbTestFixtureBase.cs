// Ignore Spelling: Dbs

using AutoMapper;
using Hmm.Core.Dal.EF;
using Hmm.Core.Dal.EF.Repositories;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hmm.Utility.TestHelp
{
    /// <summary>
    /// The help class for setting up testing db environment and clean the testing environment
    /// by <see cref="Dispose()"/> method
    /// </summary>
    public class DbTestFixtureBase : IDisposable
    {
        private const string DatabaseName = "hmm_postgres";
        protected IHmmDataContext DbContext;
        protected IDbContextTransaction Transaction;
        private DbConnection? _sqliteInMemoryConnection;
        private TestDbProvider _provider;
        

        protected DbTestFixtureBase()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Configure Serilog
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(logger, true);
            });
            Logger = loggerFactory.CreateLogger("DbTestFixtureBase");

            SetDbEnvironment(config);
        }

        private void SetDbEnvironment(IConfiguration config)
        {
            _provider = Enum.Parse<TestDbProvider>(
                config["TestDb:Provider"] ?? "Postgres",
                ignoreCase: true);

            var cs = config["TestDb:ConnectionString"] ?? config["AppSettings:ConnectionString"];

            var optBuilder = new DbContextOptionsBuilder<HmmDataContext>();

            switch (_provider)
            {
                case TestDbProvider.Postgres:
                {
                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(cs);
                    dataSourceBuilder.MapEnum<NoteContentFormatType>();
                    var dataSource = dataSourceBuilder.Build();
                    optBuilder.UseNpgsql(dataSource);
                    break;
                }

                case TestDbProvider.SqlServer:
                {
                    optBuilder.UseSqlServer(cs);
                    break;
                }

                case TestDbProvider.SqliteMemory:
                {
                    // IMPORTANT: SQLite in-memory DB exists only while the connection stays OPEN
                    _sqliteInMemoryConnection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
                    _sqliteInMemoryConnection.Open();
                    optBuilder.UseSqlite(_sqliteInMemoryConnection);
                    break;
                }
            }

            DbContext = new HmmDataContext(optBuilder.Options);

            // ... init repositories exactly like you do today ...
            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<HmmMappingProfile>(), NullLoggerFactory.Instance);
            LookupRepository = new EfEntityLookup(DbContext, mapperConfig.CreateMapper());
            var dateProvider = new DateTimeAdapter();
            AuthorRepository = new AuthorEfRepository(DbContext, LookupRepository, Logger);
            ContactRepository = new ContactEfRepository(DbContext, LookupRepository, Logger);
            NoteRepository = new NoteEfRepository(DbContext, LookupRepository, dateProvider, Logger);
            CatalogRepository = new NoteCatalogEfRepository(DbContext, LookupRepository, dateProvider, Logger);
            TagRepository = new TagEfRepository(DbContext, LookupRepository, dateProvider, Logger);
            DateProvider = new DateTimeAdapter();

            ResetDatabase(config);
        }

        private void ResetDatabase(IConfiguration config)
        {
            var ef = (DbContext)DbContext;

            switch (_provider)
            {
                case TestDbProvider.Postgres:
                {
                    // Keep your existing behavior, but ONLY for Postgres
                    EnsureDatabaseDeleted(config.GetConnectionString("DefaultConnection"));
                    ef.Database.EnsureCreated();
                    break;
                }

                case TestDbProvider.SqlServer:
                {
                    // Works for LocalDB / dedicated test DB
                    ef.Database.EnsureDeleted();
                    ef.Database.EnsureCreated();
                    break;
                }

                case TestDbProvider.SqliteMemory:
                {
                    // In-memory: just ensure schema is created
                    ef.Database.EnsureCreated();
                    break;
                }
            }
        }

        protected IRepository<AuthorDao> AuthorRepository { get; private set; }

        protected IRepository<ContactDao> ContactRepository { get; private set; }

        protected IVersionRepository<HmmNoteDao> NoteRepository { get; private set; }

        protected IRepository<NoteCatalogDao> CatalogRepository { get; private set; }

        protected ICompositeEntityRepository<TagDao, HmmNoteDao> TagRepository { get; private set; }

        protected IEntityLookup LookupRepository { get; private set; }

        protected IDateTimeProvider DateProvider { get; private set; }

        protected Microsoft.Extensions.Logging.ILogger Logger { get; }

        protected async Task NoTrackingEntities(TagDao tag)
        {
            try
            {
                var tagSet = DbContext.Set<TagDao>();
                tagSet.Entry(tag).State = EntityState.Detached;
                await tagSet.LoadAsync();

                var entities = tagSet.Local.ToList<TagDao>();
                foreach (var entity in entities)
                {
                    if (entity.Id == tag.Id)
                    {
                        tagSet.Entry(entity).State = EntityState.Detached;
                    }
                }
                tagSet.Entry(tag).State = EntityState.Modified;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

                public void Dispose()
                {
                    (DbContext as DbContext)?.Dispose();
                    _sqliteInMemoryConnection?.Dispose();
                    GC.SuppressFinalize(this);
                }

                private void EnsureDatabaseDeleted(string connectionString)
                {
                    using var connection = new NpgsqlConnection(connectionString);
                    connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = $"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE);";
                    command.ExecuteNonQuery();
                }
            }
        }