// Ignore Spelling: Dbs

using Hmm.Core.Dal.EF;
using Hmm.Core.Dal.EF.Repositories;
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
using System.Linq;
using System.Threading.Tasks;

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
        private static bool _initialized;
        private static readonly object Lock = new();
        private static DbContextOptions _dbContextOptions;

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

            var connectString = config["AppSettings:ConnectionString"];
            SetDbEnvironment(connectString);
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
                DbContext.Tags.Entry(tag).State = EntityState.Detached;
                await DbContext.Tags.LoadAsync();

                var entities = DbContext.Tags.Local.ToList<TagDao>();
                foreach (var entity in entities)
                {
                    if (entity.Id == tag.Id)
                    {
                        DbContext.Tags.Entry(entity).State = EntityState.Detached;
                    }
                }
                DbContext.Tags.Entry(tag).State = EntityState.Modified;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private void SetDbEnvironment(string connectString)
        {
            if (!_initialized)
            {
                lock (Lock)
                {
                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectString);
                    dataSourceBuilder.MapEnum<NoteContentFormatType>();
                    var dataSource = dataSourceBuilder.Build();
                    var optBuilder = new DbContextOptionsBuilder()
                        .UseNpgsql(dataSource);
                    _dbContextOptions = optBuilder.Options;
                    _initialized = true;
                }
            }

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            DbContext = new HmmDataContext(_dbContextOptions, config);

            LookupRepository = new EfEntityLookup(DbContext);
            var dateProvider = new DateTimeAdapter();
            AuthorRepository = new AuthorEfRepository(DbContext, LookupRepository, Logger);
            ContactRepository = new ContactEfRepository(DbContext, LookupRepository, Logger);
            NoteRepository = new NoteEfRepository(DbContext, LookupRepository, dateProvider, Logger);
            CatalogRepository = new NoteCatalogEfRepository(DbContext, LookupRepository, dateProvider, Logger);
            TagRepository = new TagEfRepository(DbContext, LookupRepository, dateProvider, Logger);
            DateProvider = new DateTimeAdapter();

            var contact = DbContext as DbContext;
            EnsureDatabaseDeleted(config.GetConnectionString("DefaultConnection"));
            EnsureDatabaseCreated();
        }

        private void EnsureDatabaseDeleted(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE);";
            command.ExecuteNonQuery();
        }

        private void EnsureDatabaseCreated()
        {
            var context = DbContext as DbContext;
            context?.Database.EnsureCreated();
        }
    }
}