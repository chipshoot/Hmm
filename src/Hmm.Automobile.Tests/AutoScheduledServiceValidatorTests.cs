using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutoScheduledServiceValidatorTests : AutoTestFixtureBase
    {
        private readonly AutoScheduledServiceValidator _validator;

        public AutoScheduledServiceValidatorTests()
        {
            InsertSeedRecords();
            _validator = new AutoScheduledServiceValidator(LookupRepository);
        }

        [Fact]
        public async Task ValidSchedule_PassesValidation()
        {
            var result = await _validator.ValidateEntityAsync(CreateValidSchedule());
            Assert.True(result.Success);
        }

        [Fact]
        public async Task InvalidAuthor_FailsValidation()
        {
            var s = CreateValidSchedule();
            s.AuthorId = 0;
            var result = await _validator.ValidateEntityAsync(s);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task ZeroAutomobileId_FailsValidation()
        {
            var s = CreateValidSchedule();
            s.AutomobileId = 0;
            var result = await _validator.ValidateEntityAsync(s);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task EmptyName_FailsValidation()
        {
            var s = CreateValidSchedule();
            s.Name = string.Empty;
            var result = await _validator.ValidateEntityAsync(s);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task NoIntervals_FailsValidation()
        {
            var s = CreateValidSchedule();
            s.IntervalDays = null;
            s.IntervalMileage = null;
            var result = await _validator.ValidateEntityAsync(s);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task OnlyDaysInterval_PassesValidation()
        {
            var s = CreateValidSchedule();
            s.IntervalDays = 30;
            s.IntervalMileage = null;
            var result = await _validator.ValidateEntityAsync(s);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task OnlyMileageInterval_PassesValidation()
        {
            var s = CreateValidSchedule();
            s.IntervalDays = null;
            s.IntervalMileage = 5000;
            var result = await _validator.ValidateEntityAsync(s);
            Assert.True(result.Success);
        }

        private AutoScheduledService CreateValidSchedule() => new()
        {
            Id = 1,
            AuthorId = TestDefaultAuthor.Id,
            AutomobileId = 5,
            Name = "Oil change",
            Type = ServiceType.OilChange,
            IntervalDays = 180,
            IntervalMileage = 8000,
            IsActive = true
        };
    }
}
