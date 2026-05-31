// Ignore Spelling: Repo Ef

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hmm.Core.Dal.EF.Repositories
{
    /// <summary>
    /// EF repository for <see cref="AuthorSettingsDao"/>. The
    /// upsert-by-author orchestration lives in the manager; this layer
    /// is the plain CRUD the manager composes. One row per author is a
    /// DB-level invariant (unique index on <c>authorid</c>).
    /// </summary>
    public class AuthorSettingsEfRepository : IRepository<AuthorSettingsDao>
    {
        private readonly IHmmDataContext _dataContext;
        private readonly ILogger _logger;

        public AuthorSettingsEfRepository(
            IHmmDataContext dataContext,
            IEntityLookup lookupRepo,
            ILogger logger = null)
        {
            ArgumentNullException.ThrowIfNull(dataContext);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            // lookupRepo is accepted for ctor parity with the sibling
            // repositories (and DI), but this entity queries the Set
            // directly — see GetEntitiesAsync.
            _dataContext = dataContext;
            _logger = logger;
        }

        public async Task<ProcessingResult<PageList<AuthorSettingsDao>>> GetEntitiesAsync(
            Expression<Func<AuthorSettingsDao, bool>> query = null,
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            // Queried directly against the Set rather than via
            // IEntityLookup: that lookup has a hardcoded type switch
            // that only knows the original entities, and this is a
            // single-table entity with no navigations to Include.
            try
            {
                var (pageIdx, pageSize) =
                    resourceCollectionParameters.GetPaginationTuple();
                var entities = _dataContext.Set<AuthorSettingsDao>().AsNoTracking();
                var page = query == null
                    ? await PageList<AuthorSettingsDao>.CreateAsync(
                        entities, pageIdx, pageSize)
                    : await PageList<AuthorSettingsDao>.CreateAsync(
                        entities.Where(query), pageIdx, pageSize);
                var ok = ProcessingResult<PageList<AuthorSettingsDao>>.Ok(page);
                ok.LogMessages(_logger);
                return ok;
            }
            catch (Exception ex)
            {
                var err = ProcessingResult<PageList<AuthorSettingsDao>>.FromException(ex);
                err.LogMessages(_logger);
                return err;
            }
        }

        public async Task<ProcessingResult<AuthorSettingsDao>> GetEntityAsync(int id)
        {
            try
            {
                var entry = await _dataContext.Set<AuthorSettingsDao>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);
                if (entry == null)
                {
                    var nf = ProcessingResult<AuthorSettingsDao>.EmptyOk(
                        $"AuthorSettings with ID {id} not found");
                    nf.LogMessages(_logger);
                    return nf;
                }
                var ok = ProcessingResult<AuthorSettingsDao>.Ok(entry);
                ok.LogMessages(_logger);
                return ok;
            }
            catch (Exception ex)
            {
                var err = ProcessingResult<AuthorSettingsDao>.FromException(ex);
                err.LogMessages(_logger);
                return err;
            }
        }

        public Task<ProcessingResult<AuthorSettingsDao>> AddAsync(AuthorSettingsDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            try
            {
                entity.Id = 0;
                _dataContext.Set<AuthorSettingsDao>().Add(entity);
                var ok = ProcessingResult<AuthorSettingsDao>.Ok(
                    entity,
                    $"AuthorSettings for author {entity.AuthorId} staged (pending commit)");
                ok.LogMessages(_logger);
                return Task.FromResult(ok);
            }
            catch (Exception ex)
            {
                var err = ProcessingResult<AuthorSettingsDao>.FromException(ex);
                err.LogMessages(_logger);
                return Task.FromResult(err);
            }
        }

        public Task<ProcessingResult<AuthorSettingsDao>> UpdateAsync(AuthorSettingsDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            try
            {
                if (entity.Id <= 0)
                {
                    var invalid = ProcessingResult<AuthorSettingsDao>.Invalid(
                        $"Cannot update AuthorSettings with invalid id {entity.Id}");
                    invalid.LogMessages(_logger);
                    return Task.FromResult(invalid);
                }

                _dataContext.Set<AuthorSettingsDao>().Update(entity);
                var ok = ProcessingResult<AuthorSettingsDao>.Ok(
                    entity,
                    $"AuthorSettings for author {entity.AuthorId} updated (pending commit)");
                ok.LogMessages(_logger);
                return Task.FromResult(ok);
            }
            catch (Exception ex)
            {
                var err = ProcessingResult<AuthorSettingsDao>.FromException(ex);
                err.LogMessages(_logger);
                return Task.FromResult(err);
            }
        }

        public Task<ProcessingResult<Unit>> DeleteAsync(AuthorSettingsDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            try
            {
                _dataContext.Set<AuthorSettingsDao>().Remove(entity);
                var ok = ProcessingResult<Unit>.Ok(
                    Unit.Value,
                    $"AuthorSettings for author {entity.AuthorId} marked for deletion");
                ok.LogMessages(_logger);
                return Task.FromResult(ok);
            }
            catch (Exception ex)
            {
                var err = ProcessingResult<Unit>.FromException(ex);
                err.LogMessages(_logger);
                return Task.FromResult(err);
            }
        }

        public void Flush()
        {
            _dataContext.Commit();
        }
    }
}
