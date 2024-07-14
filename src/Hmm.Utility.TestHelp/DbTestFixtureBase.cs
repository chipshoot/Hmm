// Ignore Spelling: Dbs

using Hmm.Core.Dal.EF;
using Hmm.Core.Dal.EF.DbEntity;
using Hmm.Core.Dal.EF.Repositories;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Npgsql;

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

        protected IEntityLookup LookupRepository { get; private set; }

        protected IDateTimeProvider DateProvider { get; private set; }

        protected void NoTrackingEntities()
        {
            if (DbContext is DbContext context)
            {
                context.NoTracking();
            }
        }

        protected void SetupRecords(
           IEnumerable<AuthorDao> authors,
           IEnumerable<ContactDao> contactDbs,
           IEnumerable<NoteCatalogDao> catalogs)
        {
            Guard.Against<ArgumentNullException>(authors == null, nameof(authors));
            Guard.Against<ArgumentNullException>(contactDbs == null, nameof(contactDbs));
            Guard.Against<ArgumentNullException>(catalogs == null, nameof(catalogs));

            // ReSharper disable PossibleNullReferenceException
            try
            {
                foreach (var contact in contactDbs)
                {
                    ContactRepository.Add(contact);
                }

                foreach (var user in authors)
                {
                    AuthorRepository.Add(user);
                }

                foreach (var catalog in catalogs)
                {
                    CatalogRepository.Add(catalog);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            // ReSharper restore PossibleNullReferenceException
        }

        public void Dispose()
        {
            if (DbContext is DbContext context)
            {
                context.Reset();
            }

            ////var notes = LookupRepository.GetEntities<HmmNote>().ToList();
            ////foreach (var note in notes)
            ////{
            ////    NoteRepository.Delete(note);
            ////}

            //var catalogs = LookupRepository.GetEntities<NoteCatalog>().ToList();
            //foreach (var catalog in catalogs)
            //{
            //    CatalogRepository.Delete(catalog);
            //}

            //var authors = LookupRepository.GetEntities<AuthorDao>().ToList();
            //foreach (var author in authors)
            //{
            //    AuthorRepository.Delete(author);
            //}

            //var contactDbs = LookupRepository.GetEntities<ContactDao>().ToList();
            //foreach (var contact in contactDbs)
            //{
            //    ContactRepository.Delete(contact);
            //}

            //if (_dbContext is DbContext newContext)
            //{
            //    newContext.Reset();
            //}
            GC.SuppressFinalize(this);
        }

        private void SetDbEnvironment(string connectString)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectString);
            dataSourceBuilder.MapEnum<NoteContentFormatType>();
            var dataSource = dataSourceBuilder.Build();
            var optBuilder = new DbContextOptionsBuilder()
                .UseNpgsql(dataSource);
            DbContext = new HmmDataContext(optBuilder.Options);

            LookupRepository = new EfEntityLookup(DbContext);
            var dateProvider = new DateTimeAdapter();
            AuthorRepository = new AuthorEfRepository(DbContext, LookupRepository);
            ContactRepository = new ContactEfRepository(DbContext, LookupRepository);
            NoteRepository = new NoteEfRepository(DbContext, LookupRepository, dateProvider);
            CatalogRepository = new NoteCatalogEfRepository(DbContext, LookupRepository, dateProvider);
            DateProvider = new DateTimeAdapter();

            var contact = DbContext as DbContext;
            contact?.Database.EnsureCreated();
        }
    }
}