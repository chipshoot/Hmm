using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;

namespace Hmm.Core.DefaultManager
{
    /// <summary>
    /// Default <see cref="IUserSettingsManager"/>. Treats the settings
    /// bundle as opaque (just persists <see cref="AuthorSettings.SettingsJson"/>
    /// verbatim) and resolves conflicts by the
    /// <see cref="AuthorSettings.LastModified"/> monotonicity guard.
    /// </summary>
    public class UserSettingsManager : IUserSettingsManager
    {
        private readonly IRepository<AuthorSettingsDao> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDateTimeProvider _dateProvider;

        public UserSettingsManager(
            IRepository<AuthorSettingsDao> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDateTimeProvider dateProvider)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(dateProvider);

            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dateProvider = dateProvider;
        }

        private static ProcessingResult<AuthorSettings> Propagate<TSource>(
            ProcessingResult<TSource> source) =>
            ProcessingResult<AuthorSettings>.Fail(
                source.Messages.FirstOrDefault()?.Message ??
                "Settings persistence failed");

        public async Task<ProcessingResult<AuthorSettings>> GetByAuthorIdAsync(int authorId)
        {
            if (authorId <= 0)
            {
                return ProcessingResult<AuthorSettings>.Invalid(
                    $"Invalid author id {authorId}");
            }

            var lookup = await _repository.GetEntitiesAsync(s => s.AuthorId == authorId);
            if (!lookup.Success)
            {
                return Propagate(lookup);
            }

            var dao = lookup.Value?.FirstOrDefault();
            if (dao == null)
            {
                // Absent — successful result with a null value (HTTP 204).
                return ProcessingResult<AuthorSettings>.EmptyOk(
                    $"No settings for author {authorId}");
            }

            return ProcessingResult<AuthorSettings>.Ok(
                _mapper.Map<AuthorSettings>(dao));
        }

        public async Task<ProcessingResult<AuthorSettings>> UpsertAsync(AuthorSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (settings.AuthorId <= 0)
            {
                return ProcessingResult<AuthorSettings>.Invalid(
                    $"Invalid author id {settings.AuthorId}");
            }
            if (settings.SettingsJson == null)
            {
                return ProcessingResult<AuthorSettings>.Invalid(
                    "SettingsJson is required");
            }

            var lookup = await _repository.GetEntitiesAsync(
                s => s.AuthorId == settings.AuthorId);
            if (!lookup.Success)
            {
                return Propagate(lookup);
            }

            var existing = lookup.Value?.FirstOrDefault();
            var now = _dateProvider.UtcNow;

            if (existing == null)
            {
                // First write for this author.
                var dao = _mapper.Map<AuthorSettingsDao>(settings);
                dao.Id = 0;
                dao.UpdatedAt = now;
                var added = await _repository.AddAsync(dao);
                if (!added.Success)
                {
                    return Propagate(added);
                }
                _unitOfWork.Commit();
                return ProcessingResult<AuthorSettings>.Ok(
                    _mapper.Map<AuthorSettings>(added.Value));
            }

            // Monotonicity guard: a stale (or equal) write is a no-op;
            // the stored bundle wins and is returned unchanged.
            if (settings.LastModified <= existing.LastModified)
            {
                return ProcessingResult<AuthorSettings>.Ok(
                    _mapper.Map<AuthorSettings>(existing),
                    "Incoming settings are not newer than stored; kept stored.");
            }

            existing.SettingsJson = settings.SettingsJson;
            existing.LastModified = settings.LastModified;
            existing.UpdatedAt = now;
            var updated = await _repository.UpdateAsync(existing);
            if (!updated.Success)
            {
                return Propagate(updated);
            }
            _unitOfWork.Commit();
            return ProcessingResult<AuthorSettings>.Ok(
                _mapper.Map<AuthorSettings>(updated.Value));
        }
    }
}
