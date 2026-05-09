using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.Currency;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class ServiceRecordValidatorTests : AutoTestFixtureBase
    {
        private readonly ServiceRecordValidator _validator;

        public ServiceRecordValidatorTests()
        {
            InsertSeedRecords();
            _validator = new ServiceRecordValidator(LookupRepository);
        }

        [Fact]
        public async Task ValidRecord_PassesValidation()
        {
            var result = await _validator.ValidateEntityAsync(CreateValidRecord());
            Assert.True(result.Success);
        }

        [Fact]
        public async Task InvalidAuthor_FailsValidation()
        {
            var record = CreateValidRecord();
            record.AuthorId = 0;
            var result = await _validator.ValidateEntityAsync(record);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task ZeroAutomobileId_FailsValidation()
        {
            var record = CreateValidRecord();
            record.AutomobileId = 0;
            var result = await _validator.ValidateEntityAsync(record);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task DefaultDate_FailsValidation()
        {
            var record = CreateValidRecord();
            record.Date = default;
            var result = await _validator.ValidateEntityAsync(record);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task NegativeMileage_FailsValidation()
        {
            var record = CreateValidRecord();
            record.Mileage = -1;
            var result = await _validator.ValidateEntityAsync(record);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task NullCost_PassesValidation()
        {
            var record = CreateValidRecord();
            record.Cost = null;
            var result = await _validator.ValidateEntityAsync(record);
            Assert.True(result.Success);
        }

        private ServiceRecord CreateValidRecord() => new()
        {
            Id = 1,
            AuthorId = TestDefaultAuthor.Id,
            AutomobileId = 5,
            Date = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            Mileage = 45000,
            Type = ServiceType.OilChange,
            Description = "Routine oil change",
            Cost = new Money(89m, CurrencyCodeType.Cad),
            ShopName = "Mr. Lube"
        };
    }
}
