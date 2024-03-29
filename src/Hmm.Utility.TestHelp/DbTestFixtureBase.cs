﻿using Hmm.Core.Dal.EF;
using Hmm.Core.Dal.EF.Repositories;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Utility.TestHelp
{
    /// <summary>
    /// The help class for setting up testing db environment and clean the testing environment
    /// by <see cref="Dispose()"/> method
    /// </summary>
    public class DbTestFixtureBase : IDisposable
    {
        private IHmmDataContext _dbContext;

        protected DbTestFixtureBase()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var connectString = config["ConnectionStrings:DefaultConnection"];
            SetDbEnvironment(connectString);
        }

        protected IGuidRepository<Author> AuthorRepository { get; private set; }

        protected IVersionRepository<HmmNote> NoteRepository { get; private set; }

        protected IRepository<NoteCatalog> CatalogRepository { get; private set; }

        protected IRepository<Subsystem> SubsystemRepository { get; private set; }

        protected IRepository<NoteRender> RenderRepository { get; private set; }

        protected IEntityLookup LookupRepo { get; private set; }

        protected IDateTimeProvider DateProvider { get; private set; }

        protected void NoTrackingEntities()
        {
            if (_dbContext is DbContext context)
            {
                context.NoTracking();
            }
        }

        protected void SetupRecords(
           IEnumerable<Author> authors,
           IEnumerable<NoteRender> renders,
           IEnumerable<NoteCatalog> catalogs,
           IEnumerable<Subsystem> subsystems)
        {
            Guard.Against<ArgumentNullException>(authors == null, nameof(authors));
            Guard.Against<ArgumentNullException>(renders == null, nameof(renders));
            Guard.Against<ArgumentNullException>(catalogs == null, nameof(catalogs));
            Guard.Against<ArgumentNullException>(subsystems == null, nameof(subsystems));

            // ReSharper disable PossibleNullReferenceException
            try
            {
                foreach (var user in authors)
                {
                    AuthorRepository.Add(user);
                }

                //foreach (var render in renders)
                //{
                //    RenderRepository.Add(render);
                //}

                //foreach (var catalog in catalogs)
                //{
                //    CatalogRepository.Add(catalog);
                //}

                foreach (var sys in subsystems)
                {
                    SubsystemRepository.Add(sys);
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
            if (_dbContext is DbContext context)
            {
                context.Reset();
            }

            var systems = LookupRepo.GetEntities<Subsystem>().ToList();
            foreach (var sys in systems)
            {
                SubsystemRepository.Delete(sys);
            }
            var notes = LookupRepo.GetEntities<HmmNote>().ToList();
            foreach (var note in notes)
            {
                NoteRepository.Delete(note);
            }

            var catalogs = LookupRepo.GetEntities<NoteCatalog>().ToList();
            foreach (var catalog in catalogs)
            {
                CatalogRepository.Delete(catalog);
            }

            var renders = LookupRepo.GetEntities<NoteRender>().ToList();
            foreach (var render in renders)
            {
                RenderRepository.Delete(render);
            }

            var authors = LookupRepo.GetEntities<Author>().ToList();
            foreach (var author in authors)
            {
                AuthorRepository.Delete(author);
            }

            if (_dbContext is DbContext newContext)
            {
                newContext.Reset();
            }
            GC.SuppressFinalize(this);
        }

        private void SetDbEnvironment(string connectString)
        {
            var optBuilder = new DbContextOptionsBuilder()
                .UseSqlServer(connectString);
            _dbContext = new HmmDataContext(optBuilder.Options);
            LookupRepo = new EfEntityLookup(_dbContext);
            var dateProvider = new DateTimeAdapter();
            AuthorRepository = new AuthorEfRepository(_dbContext, LookupRepo);
            NoteRepository = new NoteEfRepository(_dbContext, LookupRepo, dateProvider);
            RenderRepository = new NoteRenderEfRepository(_dbContext, LookupRepo, dateProvider);
            CatalogRepository = new NoteCatalogEfRepository(_dbContext, LookupRepo, dateProvider);
            SubsystemRepository = new SubsystemEfRepository(_dbContext, LookupRepo, dateProvider);
            DateProvider = new DateTimeAdapter();
        }
    }
}