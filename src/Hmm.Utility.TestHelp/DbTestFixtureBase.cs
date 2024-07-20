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
using Npgsql;
using System;

namespace Hmm.Utility.TestHelp
{
    /// <summary>
    /// The help class for setting up testing db environment and clean the testing environment
    /// by <see cref="Dispose()"/> method
    /// </summary>
    public class DbTestFixtureBase : IDisposable
    {
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

            var connectString = config["ConnectionStrings:DefaultConnection"];
            SetDbEnvironment(connectString);
        }

        protected IRepository<AuthorDao> AuthorRepository { get; private set; }
        protected IRepository<ContactDao> ContactRepository { get; private set; }

        protected IVersionRepository<HmmNoteDao> NoteRepository { get; private set; }

        protected IRepository<NoteCatalogDao> CatalogRepository { get; private set; }

        protected IRepository<TagDao> TagRepository { get; private set; }

        private IEntityLookup LookupRepository { get; set; }

        protected IDateTimeProvider DateProvider { get; private set; }

        protected void NoTrackingEntities()
        {
            if (DbContext is DbContext context)
            {
                context.NoTracking();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        protected static ContactDao GetTestingContact()
        {
            var contact = new ContactDao
            {
                Contact = """
                      { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": "Personal", "IsPrimary": "false" }, { "Address": "fchy5979@gamil.com", "Type": "Personal", "IsPrimary": "true" }, { "Address": "fchy@outlook.com", "Type": "Work", "IsPrimary": "false" } ], "Phones": [ { "Type": "Home", "Number": "123-456-7890" }, { "Type": "Work", "Number": "456-789-0123" } ], "Addresses": [ { "Type": "Home", "Street": "123 Main St", "City": "Springfield", "State": "IL", "Zip": "62701" }, { "Type": "Work", "Street": "456 Elm St", "City": "Springfield", "State": "IL", "Zip": "62702" } ] }
                      """,
                Description = "testing contact",
                IsActivated = true
            };
            return contact;
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

            DbContext = new HmmDataContext(_dbContextOptions);

            LookupRepository = new EfEntityLookup(DbContext);
            var dateProvider = new DateTimeAdapter();
            AuthorRepository = new AuthorEfRepository(DbContext, LookupRepository);
            ContactRepository = new ContactEfRepository(DbContext, LookupRepository);
            NoteRepository = new NoteEfRepository(DbContext, LookupRepository, dateProvider);
            CatalogRepository = new NoteCatalogEfRepository(DbContext, LookupRepository, dateProvider);
            TagRepository = new TagEfRepository(DbContext, LookupRepository, dateProvider);
            DateProvider = new DateTimeAdapter();

            var contact = DbContext as DbContext;
            contact?.Database.EnsureCreated();
        }
    }
}