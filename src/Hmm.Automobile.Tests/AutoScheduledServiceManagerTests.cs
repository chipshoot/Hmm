using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Automobile.Validator;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutoScheduledServiceManagerTests : AutoTestFixtureBase
    {
        private IAutoScheduledServiceManager _manager;
        private Mock<IAutomobileSnapshotUpdater> _snapshotMock;

        public AutoScheduledServiceManagerTests()
        {
            SetupDevEnv();
        }

        [Fact]
        public async Task CreateAsync_WithValidSchedule_ReturnsCreatedAndCallsSnapshot()
        {
            var schedule = NewSchedule();

            var result = await _manager.CreateAsync(schedule);

            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id >= 1);
            _snapshotMock.Verify(s => s.RecomputeScheduleSnapshotAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetSoonestDueForAutomobileAsync_ReturnsEarliestActiveDueDate()
        {
            await _manager.CreateAsync(NewSchedule(name: "Late", nextDue: DateTime.UtcNow.AddYears(2)));
            await _manager.CreateAsync(NewSchedule(name: "Soon", nextDue: DateTime.UtcNow.AddMonths(1)));
            await _manager.CreateAsync(NewSchedule(name: "Mid", nextDue: DateTime.UtcNow.AddMonths(6)));

            var result = await _manager.GetSoonestDueForAutomobileAsync(5);

            Assert.True(result.Success);
            Assert.Equal("Soon", result.Value.Name);
        }

        [Fact]
        public async Task GetSoonestDueForAutomobileAsync_InactiveExcluded()
        {
            await _manager.CreateAsync(NewSchedule(name: "Inactive", nextDue: DateTime.UtcNow.AddDays(1), isActive: false));
            await _manager.CreateAsync(NewSchedule(name: "Active", nextDue: DateTime.UtcNow.AddMonths(1), isActive: true));

            var result = await _manager.GetSoonestDueForAutomobileAsync(5);

            Assert.True(result.Success);
            Assert.Equal("Active", result.Value.Name);
        }

        [Fact]
        public async Task UpdateAsync_TriggersSnapshot()
        {
            var created = await _manager.CreateAsync(NewSchedule());
            var schedule = created.Value;
            schedule.Name = "Renamed";

            var result = await _manager.UpdateAsync(schedule);

            Assert.True(result.Success);
            _snapshotMock.Verify(s => s.RecomputeScheduleSnapshotAsync(5), Times.Exactly(2));
        }

        private AutoScheduledService NewSchedule(int autoId = 5, string name = "Oil change",
            DateTime? nextDue = null, bool isActive = true) => new()
        {
            AutomobileId = autoId,
            Name = name,
            Type = ServiceType.OilChange,
            IntervalDays = 180,
            IntervalMileage = 8000,
            NextDueDate = nextDue ?? DateTime.UtcNow.AddMonths(6),
            NextDueMileage = 53000,
            IsActive = isActive
        };

        private void SetupDevEnv()
        {
            InsertSeedRecords();

            _snapshotMock = new Mock<IAutomobileSnapshotUpdater>();
            _snapshotMock.Setup(s => s.RecomputeScheduleSnapshotAsync(It.IsAny<int>()))
                .ReturnsAsync(ProcessingResult<bool>.Ok(true));

            var serializer = new AutoScheduledServiceJsonNoteSerialize(CatalogProvider, new NullLogger<AutoScheduledService>());
            var validator = new AutoScheduledServiceValidator(LookupRepository);
            var noteManager = NoteManagerStubs.Build();

            _manager = new AutoScheduledServiceManager(
                serializer,
                validator,
                noteManager,
                LookupRepository,
                CreateMockAuthorProvider(),
                _snapshotMock.Object);
        }
    }
}
