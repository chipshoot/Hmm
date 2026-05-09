using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Automobile.Validator;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutoInsurancePolicyManagerTests : AutoTestFixtureBase
    {
        private IAutoInsurancePolicyManager _manager;
        private Mock<IAutomobileSnapshotUpdater> _snapshotMock;

        public AutoInsurancePolicyManagerTests()
        {
            SetupDevEnv();
        }

        [Fact]
        public async Task CreateAsync_WithValidPolicy_ReturnsCreatedAndCallsSnapshot()
        {
            var policy = NewPolicy();

            var result = await _manager.CreateAsync(policy);

            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id >= 1);
            Assert.Equal("ICBC", result.Value.Provider);
            _snapshotMock.Verify(s => s.RecomputeInsuranceSnapshotAsync(5), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NullEntity_ReturnsInvalid()
        {
            var result = await _manager.CreateAsync(null);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetByAutomobileAsync_ReturnsOnlyMatchingPolicies()
        {
            await _manager.CreateAsync(NewPolicy(autoId: 5, provider: "A"));
            await _manager.CreateAsync(NewPolicy(autoId: 5, provider: "B"));
            await _manager.CreateAsync(NewPolicy(autoId: 7, provider: "C"));

            var result = await _manager.GetByAutomobileAsync(5);

            Assert.True(result.Success);
            Assert.Equal(2, result.Value.Count);
            Assert.All(result.Value, p => Assert.Equal(5, p.AutomobileId));
        }

        [Fact]
        public async Task GetActiveForAutomobileAsync_ReturnsCurrentlyActiveByEffectiveDate()
        {
            var now = DateTime.UtcNow;
            await _manager.CreateAsync(NewPolicy(autoId: 5, provider: "Old",
                effective: now.AddYears(-3), expiry: now.AddYears(-2)));
            await _manager.CreateAsync(NewPolicy(autoId: 5, provider: "Current",
                effective: now.AddDays(-30), expiry: now.AddYears(1)));
            await _manager.CreateAsync(NewPolicy(autoId: 5, provider: "Future",
                effective: now.AddDays(30), expiry: now.AddYears(1)));

            var result = await _manager.GetActiveForAutomobileAsync(5);

            Assert.True(result.Success);
            Assert.Equal("Current", result.Value.Provider);
        }

        [Fact]
        public async Task GetActiveForAutomobileAsync_NoneActive_ReturnsNotFound()
        {
            var result = await _manager.GetActiveForAutomobileAsync(99);
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task UpdateAsync_CopiesFieldsAndTriggersSnapshot()
        {
            var created = await _manager.CreateAsync(NewPolicy(provider: "Original"));
            Assert.True(created.Success);

            var policy = created.Value;
            policy.Provider = "Updated";

            var result = await _manager.UpdateAsync(policy);

            Assert.True(result.Success);
            Assert.Equal("Updated", result.Value.Provider);
            _snapshotMock.Verify(s => s.RecomputeInsuranceSnapshotAsync(5), Times.Exactly(2));
        }

        private AutoInsurancePolicy NewPolicy(int autoId = 5, string provider = "ICBC",
            DateTime? effective = null, DateTime? expiry = null) => new()
        {
            AutomobileId = autoId,
            Provider = provider,
            PolicyNumber = "POL-" + Guid.NewGuid().ToString("N")[..8],
            EffectiveDate = effective ?? DateTime.UtcNow.AddDays(-1),
            ExpiryDate = expiry ?? DateTime.UtcNow.AddYears(1),
            Premium = new Money(800m, CurrencyCodeType.Cad),
            IsActive = true
        };

        private void SetupDevEnv()
        {
            InsertSeedRecords();

            _snapshotMock = new Mock<IAutomobileSnapshotUpdater>();
            _snapshotMock.Setup(s => s.RecomputeInsuranceSnapshotAsync(It.IsAny<int>()))
                .ReturnsAsync(ProcessingResult<bool>.Ok(true));

            var serializer = new AutoInsurancePolicyJsonNoteSerialize(CatalogProvider, new NullLogger<AutoInsurancePolicy>());
            var validator = new AutoInsurancePolicyValidator(LookupRepository);
            var noteManager = NoteManagerStubs.Build();

            _manager = new AutoInsurancePolicyManager(
                serializer,
                validator,
                noteManager,
                LookupRepository,
                CreateMockAuthorProvider(),
                _snapshotMock.Object);
        }
    }

    internal static class NoteManagerStubs
    {
        public static IHmmNoteManager Build()
        {
            var mock = new Mock<IHmmNoteManager>();
            var notes = new List<HmmNote>();
            var idCounter = 1;

            mock.Setup(m => m.CreateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    note.Id = idCounter++;
                    notes.Add(note);
                    return ProcessingResult<HmmNote>.Ok(note);
                });

            mock.Setup(m => m.UpdateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    var existing = notes.FirstOrDefault(n => n.Id == note.Id);
                    if (existing != null)
                    {
                        notes.Remove(existing);
                        notes.Add(note);
                        return ProcessingResult<HmmNote>.Ok(note);
                    }
                    return ProcessingResult<HmmNote>.NotFound("Note not found");
                });

            mock.Setup(m => m.GetNoteByIdAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync((int id, bool includeDeleted) =>
                {
                    var note = notes.FirstOrDefault(n => n.Id == id);
                    return note != null
                        ? ProcessingResult<HmmNote>.Ok(note)
                        : ProcessingResult<HmmNote>.NotFound("Note not found");
                });

            mock.Setup(m => m.GetNotesAsync(
                    It.IsAny<Expression<Func<HmmNote, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync((Expression<Func<HmmNote, bool>> query, bool includeDeleted, ResourceCollectionParameters para) =>
                {
                    var filtered = notes.AsQueryable().Where(query).ToList();
                    var page = PageList<HmmNote>.Create(filtered.AsQueryable(), para?.PageNumber ?? 1, para?.PageSize ?? 20);
                    return ProcessingResult<PageList<HmmNote>>.Ok(page);
                });

            return mock.Object;
        }
    }
}
