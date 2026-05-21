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
    /// EF repository for <see cref="MigrationLogDao"/>. Append-only —
    /// the migration manager only ever calls
    /// <see cref="AddAsync(MigrationLogDao)"/> and reads via
    /// <see cref="GetEntitiesAsync(Expression{Func{MigrationLogDao, bool}}, ResourceCollectionParameters)"/>.
    /// Update / Delete are present to satisfy
    /// <see cref="IRepository{T}"/> but aren't part of the migration
    /// flow.
    /// </summary>
    public class MigrationLogEfRepository : IRepository<MigrationLogDao>
    {
        private readonly IHmmDataContext _dataContext;
        private readonly IEntityLookup _lookupRepo;
        private readonly ILogger? _logger;

        public MigrationLogEfRepository(
            IHmmDataContext dataContext,
            IEntityLookup lookupRepo,
            ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(dataContext);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            _dataContext = dataContext;
            _lookupRepo = lookupRepo;
            _logger = logger;
        }

        public async Task<ProcessingResult<PageList<MigrationLogDao>>> GetEntitiesAsync(
            Expression<Func<MigrationLogDao, bool>>? query = null,
            ResourceCollectionParameters? resourceCollectionParameters = null)
        {
            var result = await _lookupRepo.GetEntitiesAsync<MigrationLogDao>(
                query, resourceCollectionParameters);
            result.LogMessages(_logger);
            return result;
        }

        public async Task<ProcessingResult<MigrationLogDao>> GetEntityAsync(int id)
        {
            try
            {
                var entry = await _dataContext.Set<MigrationLogDao>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (entry == null)
                {
                    var nf = ProcessingResult<MigrationLogDao>.EmptyOk(
                        $"MigrationLog with ID {id} not found");
                    nf.LogMessages(_logger);
                    return nf;
                }
                var ok = ProcessingResult<MigrationLogDao>.Ok(entry);
                ok.LogMessages(_logger);
                return ok;
            }
            catch (Exception ex)
            {
                var err = ProcessingResult<MigrationLogDao>.FromException(ex);
                err.LogMessages(_logger);
                return err;
            }
        }

        public Task<ProcessingResult<MigrationLogDao>> AddAsync(MigrationLogDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            try
            {
                entity.Id = 0;
                _dataContext.Set<MigrationLogDao>().Add(entity);
                var ok = ProcessingResult<MigrationLogDao>.Ok(
                    entity,
                    $"MigrationLog for author {entity.AuthorId} ({entity.Kind}) staged");
                ok.LogMessages(_logger);
                return Task.FromResult(ok);
            }
            catch (Exception ex)
            {
                var err = ProcessingResult<MigrationLogDao>.FromException(ex);
                err.LogMessages(_logger);
                return Task.FromResult(err);
            }
        }

        public Task<ProcessingResult<MigrationLogDao>> UpdateAsync(MigrationLogDao entity)
        {
            // MigrationLog is append-only by design; surface Update
            // attempts as Invalid rather than silently allowing them.
            var invalid = ProcessingResult<MigrationLogDao>.Invalid(
                "MigrationLog is append-only; updates are not supported.");
            invalid.LogMessages(_logger);
            return Task.FromResult(invalid);
        }

        public Task<ProcessingResult<Unit>> DeleteAsync(MigrationLogDao entity)
        {
            var invalid = ProcessingResult<Unit>.Invalid(
                "MigrationLog is append-only; deletes are not supported.");
            invalid.LogMessages(_logger);
            return Task.FromResult(invalid);
        }

        public void Flush()
        {
            _dataContext.Commit();
        }
    }
}
