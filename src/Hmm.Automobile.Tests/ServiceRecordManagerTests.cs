using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Automobile.Validator;
using Hmm.Utility.Currency;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class ServiceRecordManagerTests : AutoTestFixtureBase
    {
        private IServiceRecordManager _manager;
        private Mock<IAutomobileSnapshotUpdater> _snapshotMock;

        public ServiceRecordManagerTests()
        {
            SetupDevEnv();
        }

        [Fact]
        public async Task CreateAsync_WithValidRecord_ReturnsCreatedAndCallsSnapshot()
        {
            var record = NewRecord();

            var result = await _manager.CreateAsync(record);

            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id >= 1);
            _snapshotMock.Verify(s => s.RecomputeServiceSnapshotAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetByAutomobileAsync_ReturnsOnlyMatching()
        {
            await _manager.CreateAsync(NewRecord(autoId: 5));
            await _manager.CreateAsync(NewRecord(autoId: 5));
            await _manager.CreateAsync(NewRecord(autoId: 7));

            var result = await _manager.GetByAutomobileAsync(5);

            Assert.True(result.Success);
            Assert.Equal(2, result.Value.Count);
        }

        [Fact]
        public async Task GetMostRecentForAutomobileAsync_ReturnsLatestByDate()
        {
            await _manager.CreateAsync(NewRecord(date: new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc), mileage: 40000));
            await _manager.CreateAsync(NewRecord(date: new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc), mileage: 45000));
            await _manager.CreateAsync(NewRecord(date: new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc), mileage: 42000));

            var result = await _manager.GetMostRecentForAutomobileAsync(5);

            Assert.True(result.Success);
            Assert.Equal(45000, result.Value.Mileage);
        }

        [Fact]
        public async Task GetMostRecentForAutomobileAsync_NoRecords_ReturnsNotFound()
        {
            var result = await _manager.GetMostRecentForAutomobileAsync(99);
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task UpdateAsync_TriggersSnapshot()
        {
            var created = await _manager.CreateAsync(NewRecord());
            var record = created.Value;
            record.Description = "Updated";

            var result = await _manager.UpdateAsync(record);

            Assert.True(result.Success);
            _snapshotMock.Verify(s => s.RecomputeServiceSnapshotAsync(5), Times.Exactly(2));
        }

        private ServiceRecord NewRecord(int autoId = 5, DateTime? date = null, int mileage = 45000) => new()
        {
            AutomobileId = autoId,
            Date = date ?? DateTime.UtcNow,
            Mileage = mileage,
            Type = ServiceType.OilChange,
            Description = "Oil change",
            Cost = new Money(89m, CurrencyCodeType.Cad),
            ShopName = "Mr. Lube"
        };

        private void SetupDevEnv()
        {
            InsertSeedRecords();

            _snapshotMock = new Mock<IAutomobileSnapshotUpdater>();
            _snapshotMock.Setup(s => s.RecomputeServiceSnapshotAsync(It.IsAny<int>()))
                .ReturnsAsync(ProcessingResult<bool>.Ok(true));

            var serializer = new ServiceRecordJsonNoteSerialize(CatalogProvider, new NullLogger<ServiceRecord>());
            var validator = new ServiceRecordValidator(LookupRepository);
            var noteManager = NoteManagerStubs.Build();

            _manager = new ServiceRecordManager(
                serializer,
                validator,
                noteManager,
                LookupRepository,
                CreateMockAuthorProvider(),
                _snapshotMock.Object);
        }
    }
}
