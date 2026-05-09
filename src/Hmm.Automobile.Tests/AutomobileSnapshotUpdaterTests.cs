using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutomobileSnapshotUpdaterTests
    {
        private readonly AutomobileInfo _auto = new()
        {
            Id = 5,
            VIN = "1HGBH41JXMN109186",
            Maker = "Subaru",
            Brand = "Outback",
            Model = "X",
            Plate = "ABC123",
            Year = 2020,
            MeterReading = 50000
        };

        [Fact]
        public async Task RecomputeInsuranceSnapshotAsync_PicksNewestActivePolicy()
        {
            var now = DateTime.UtcNow;
            var policies = new[]
            {
                Policy(provider: "Old", effective: now.AddYears(-3), expiry: now.AddYears(-2)),
                Policy(provider: "Current", effective: now.AddDays(-30), expiry: now.AddYears(1)),
                Policy(provider: "Newer", effective: now.AddDays(-5), expiry: now.AddYears(1))
            };

            var (updater, autoMock) = BuildUpdater(policies: policies);

            var result = await updater.RecomputeInsuranceSnapshotAsync(5);

            Assert.True(result.Success);
            autoMock.Verify(m => m.UpdateAsync(It.Is<AutomobileInfo>(a =>
                a.InsuranceProvider == "Newer"), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RecomputeInsuranceSnapshotAsync_NoActive_ClearsSnapshotFields()
        {
            _auto.InsuranceProvider = "Stale";
            _auto.InsurancePolicyNumber = "OLD";
            _auto.InsuranceExpiryDate = DateTime.UtcNow.AddYears(-1);

            var (updater, autoMock) = BuildUpdater(policies: Array.Empty<AutoInsurancePolicy>());

            var result = await updater.RecomputeInsuranceSnapshotAsync(5);

            Assert.True(result.Success);
            autoMock.Verify(m => m.UpdateAsync(It.Is<AutomobileInfo>(a =>
                a.InsuranceProvider == null &&
                a.InsurancePolicyNumber == null &&
                a.InsuranceExpiryDate == null), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RecomputeServiceSnapshotAsync_PicksMostRecentRecord()
        {
            var records = new[]
            {
                Record(date: new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc), mileage: 40000),
                Record(date: new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc), mileage: 45000),
                Record(date: new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc), mileage: 42000)
            };

            var (updater, autoMock) = BuildUpdater(records: records);

            var result = await updater.RecomputeServiceSnapshotAsync(5);

            Assert.True(result.Success);
            autoMock.Verify(m => m.UpdateAsync(It.Is<AutomobileInfo>(a =>
                a.LastServiceMeterReading == 45000 &&
                a.LastServiceDate == new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)
            ), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RecomputeServiceSnapshotAsync_NoRecords_ClearsSnapshot()
        {
            _auto.LastServiceDate = DateTime.UtcNow;
            _auto.LastServiceMeterReading = 99000;

            var (updater, autoMock) = BuildUpdater(records: Array.Empty<ServiceRecord>());

            var result = await updater.RecomputeServiceSnapshotAsync(5);

            Assert.True(result.Success);
            autoMock.Verify(m => m.UpdateAsync(It.Is<AutomobileInfo>(a =>
                a.LastServiceDate == null && a.LastServiceMeterReading == null
            ), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RecomputeScheduleSnapshotAsync_PicksEarliestActiveDueDate()
        {
            var schedules = new[]
            {
                Schedule(name: "Late", due: DateTime.UtcNow.AddYears(2), mileage: 80000),
                Schedule(name: "Soon", due: DateTime.UtcNow.AddMonths(1), mileage: 53000),
                Schedule(name: "Inactive", due: DateTime.UtcNow.AddDays(1), mileage: 50100, active: false)
            };

            var (updater, autoMock) = BuildUpdater(schedules: schedules);

            var result = await updater.RecomputeScheduleSnapshotAsync(5);

            Assert.True(result.Success);
            autoMock.Verify(m => m.UpdateAsync(It.Is<AutomobileInfo>(a =>
                a.NextServiceDueMeterReading == 53000), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RecomputeScheduleSnapshotAsync_NoneActive_ClearsSnapshot()
        {
            _auto.NextServiceDueDate = DateTime.UtcNow;
            _auto.NextServiceDueMeterReading = 99000;

            var (updater, autoMock) = BuildUpdater(schedules: Array.Empty<AutoScheduledService>());

            var result = await updater.RecomputeScheduleSnapshotAsync(5);

            Assert.True(result.Success);
            autoMock.Verify(m => m.UpdateAsync(It.Is<AutomobileInfo>(a =>
                a.NextServiceDueDate == null && a.NextServiceDueMeterReading == null
            ), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RecomputeInsuranceSnapshotAsync_AutomobileNotFound_ReturnsOkFalse()
        {
            var autoMock = new Mock<IAutoEntityManager<AutomobileInfo>>();
            autoMock.Setup(m => m.GetEntityByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.NotFound("not found"));

            var sp = new Mock<IServiceProvider>().Object;
            var updater = new AutomobileSnapshotUpdater(sp, autoMock.Object, NullLogger<AutomobileSnapshotUpdater>.Instance);

            var result = await updater.RecomputeInsuranceSnapshotAsync(123);
            Assert.True(result.Success);
            Assert.False(result.Value);
            autoMock.Verify(m => m.UpdateAsync(It.IsAny<AutomobileInfo>(), It.IsAny<bool>()), Times.Never);
        }

        private (AutomobileSnapshotUpdater updater, Mock<IAutoEntityManager<AutomobileInfo>> autoMock) BuildUpdater(
            AutoInsurancePolicy[] policies = null,
            ServiceRecord[] records = null,
            AutoScheduledService[] schedules = null)
        {
            var autoMock = new Mock<IAutoEntityManager<AutomobileInfo>>();
            autoMock.Setup(m => m.GetEntityByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Ok(_auto));
            autoMock.Setup(m => m.UpdateAsync(It.IsAny<AutomobileInfo>(), It.IsAny<bool>()))
                .ReturnsAsync((AutomobileInfo a, bool _) => ProcessingResult<AutomobileInfo>.Ok(a));

            var policyManager = new Mock<IAutoInsurancePolicyManager>();
            policyManager.Setup(p => p.GetByAutomobileAsync(It.IsAny<int>(), It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<AutoInsurancePolicy>>.Ok(BuildPage(policies ?? Array.Empty<AutoInsurancePolicy>())));

            var recordManager = new Mock<IServiceRecordManager>();
            recordManager.Setup(r => r.GetByAutomobileAsync(It.IsAny<int>(), It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<ServiceRecord>>.Ok(BuildPage(records ?? Array.Empty<ServiceRecord>())));

            var scheduleManager = new Mock<IAutoScheduledServiceManager>();
            scheduleManager.Setup(s => s.GetByAutomobileAsync(It.IsAny<int>(), It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<AutoScheduledService>>.Ok(BuildPage(schedules ?? Array.Empty<AutoScheduledService>())));

            var services = new ServiceCollection();
            services.AddSingleton(policyManager.Object);
            services.AddSingleton(recordManager.Object);
            services.AddSingleton(scheduleManager.Object);
            var sp = services.BuildServiceProvider();

            var updater = new AutomobileSnapshotUpdater(sp, autoMock.Object, NullLogger<AutomobileSnapshotUpdater>.Instance);
            return (updater, autoMock);
        }

        private static PageList<T> BuildPage<T>(IEnumerable<T> items)
        {
            var list = items.ToList();
            return new PageList<T>(list, list.Count, 1, list.Count == 0 ? 20 : list.Count);
        }

        private static AutoInsurancePolicy Policy(string provider, DateTime effective, DateTime expiry, bool active = true)
            => new()
            {
                Id = Guid.NewGuid().GetHashCode(),
                AutomobileId = 5,
                Provider = provider,
                PolicyNumber = "POL-" + provider,
                EffectiveDate = effective,
                ExpiryDate = expiry,
                Premium = new Money(800m, CurrencyCodeType.Cad),
                IsActive = active
            };

        private static ServiceRecord Record(DateTime date, int mileage)
            => new()
            {
                Id = Guid.NewGuid().GetHashCode(),
                AutomobileId = 5,
                Date = date,
                Mileage = mileage,
                Type = ServiceType.OilChange
            };

        private static AutoScheduledService Schedule(string name, DateTime due, int mileage, bool active = true)
            => new()
            {
                Id = Guid.NewGuid().GetHashCode(),
                AutomobileId = 5,
                Name = name,
                Type = ServiceType.OilChange,
                IntervalDays = 180,
                NextDueDate = due,
                NextDueMileage = mileage,
                IsActive = active
            };
    }
}
