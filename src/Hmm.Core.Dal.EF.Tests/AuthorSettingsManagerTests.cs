using System;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core;
using Hmm.Core.Dal.EF.Repositories;
using Hmm.Core.DefaultManager;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    /// <summary>
    /// End-to-end Phase P1 coverage: the real <see cref="UserSettingsManager"/>
    /// over a real <see cref="AuthorSettingsEfRepository"/> + EF context.
    /// Exercises the round-trip, absent-returns-null, the upsert
    /// insert/update paths (one row per author), and the LastModified
    /// monotonicity guard.
    /// </summary>
    public class AuthorSettingsManagerTests : DbTestFixtureBase, IAsyncLifetime
    {
        private IUserSettingsManager _manager = null!;
        private int _authorId;

        private static readonly DateTime T1 =
            new(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc);

        public async Task InitializeAsync()
        {
            var mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<HmmMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();
            var repo = new AuthorSettingsEfRepository(DbContext, LookupRepository, Logger);
            _manager = new UserSettingsManager(repo, DbContext, mapper, DateProvider);

            var added = await AuthorRepository.AddAsync(new AuthorDao
            {
                AccountName = "settings-test-user",
                Description = "settings test",
                IsActivated = true,
            });
            await DbContext.CommitAsync();
            _authorId = added.Value.Id;
            NewRequestScope();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // Each manager call is a fresh DI scope in production (new
        // DbContext per request); clear the change tracker so a
        // long-lived test context doesn't trip "instance already
        // tracked" on the update path.
        private void NewRequestScope() =>
            ((DbContext)DbContext).ChangeTracker.Clear();

        private Task<Utility.Misc.ProcessingResult<AuthorSettings>> Upsert(
            string json, DateTime lastModified)
        {
            NewRequestScope();
            return _manager.UpsertAsync(new AuthorSettings
            {
                AuthorId = _authorId,
                SettingsJson = json,
                LastModified = lastModified,
            });
        }

        [Fact]
        public async Task GetByAuthorId_returns_success_with_null_when_absent()
        {
            var result = await _manager.GetByAuthorIdAsync(_authorId);

            Assert.True(result.Success);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task Upsert_inserts_then_Get_round_trips()
        {
            const string json = "{\"gasLog\":{},\"_v\":1}";
            var up = await Upsert(json, T1);

            Assert.True(up.Success);
            Assert.NotNull(up.Value);
            Assert.True(up.Value!.Id > 0);
            Assert.True(up.Value.UpdatedAt != default);

            var got = await _manager.GetByAuthorIdAsync(_authorId);
            Assert.True(got.Success);
            Assert.NotNull(got.Value);
            Assert.Equal(json, got.Value!.SettingsJson);
            Assert.Equal(T1, got.Value.LastModified);
            Assert.Equal(_authorId, got.Value.AuthorId);
        }

        [Fact]
        public async Task Upsert_newer_updates_in_place_keeping_one_row()
        {
            await Upsert("v1", T1);
            var up2 = await Upsert("v2", T1.AddMinutes(5));

            Assert.True(up2.Success);
            Assert.Equal("v2", up2.Value!.SettingsJson);
            Assert.Equal(T1.AddMinutes(5), up2.Value.LastModified);

            // Still exactly one row: a second author-scoped read sees v2.
            var got = await _manager.GetByAuthorIdAsync(_authorId);
            Assert.Equal("v2", got.Value!.SettingsJson);
        }

        [Fact]
        public async Task Upsert_stale_stamp_is_noop_stored_wins()
        {
            await Upsert("fresh", T1);
            var stale = await Upsert("stale", T1.AddMinutes(-5));

            Assert.True(stale.Success);
            Assert.Equal("fresh", stale.Value!.SettingsJson);
            Assert.Equal(T1, stale.Value.LastModified);

            var got = await _manager.GetByAuthorIdAsync(_authorId);
            Assert.Equal("fresh", got.Value!.SettingsJson);
        }

        [Fact]
        public async Task Upsert_equal_stamp_is_noop_stored_wins()
        {
            await Upsert("first", T1);
            var same = await Upsert("second", T1);

            Assert.Equal("first", same.Value!.SettingsJson);
        }

        [Fact]
        public async Task Upsert_rejects_invalid_author_id()
        {
            var result = await _manager.UpsertAsync(new AuthorSettings
            {
                AuthorId = 0,
                SettingsJson = "{}",
                LastModified = T1,
            });

            Assert.False(result.Success);
        }
    }
}
