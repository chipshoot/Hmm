using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.Currency;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutoInsurancePolicyValidatorTests : AutoTestFixtureBase
    {
        private readonly AutoInsurancePolicyValidator _validator;

        public AutoInsurancePolicyValidatorTests()
        {
            InsertSeedRecords();
            _validator = new AutoInsurancePolicyValidator(LookupRepository);
        }

        [Fact]
        public async Task ValidPolicy_PassesValidation()
        {
            var result = await _validator.ValidateEntityAsync(CreateValidPolicy());
            Assert.True(result.Success);
        }

        [Fact]
        public async Task InvalidAuthor_FailsValidation()
        {
            var policy = CreateValidPolicy();
            policy.AuthorId = 0;
            var result = await _validator.ValidateEntityAsync(policy);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task ZeroAutomobileId_FailsValidation()
        {
            var policy = CreateValidPolicy();
            policy.AutomobileId = 0;
            var result = await _validator.ValidateEntityAsync(policy);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task EmptyProvider_FailsValidation()
        {
            var policy = CreateValidPolicy();
            policy.Provider = string.Empty;
            var result = await _validator.ValidateEntityAsync(policy);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task EmptyPolicyNumber_FailsValidation()
        {
            var policy = CreateValidPolicy();
            policy.PolicyNumber = string.Empty;
            var result = await _validator.ValidateEntityAsync(policy);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task EffectiveDateAfterExpiry_FailsValidation()
        {
            var policy = CreateValidPolicy();
            policy.EffectiveDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            policy.ExpiryDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = await _validator.ValidateEntityAsync(policy);
            Assert.False(result.Success);
        }

        private AutoInsurancePolicy CreateValidPolicy() => new()
        {
            Id = 1,
            AuthorId = TestDefaultAuthor.Id,
            AutomobileId = 5,
            Provider = "ICBC",
            PolicyNumber = "POL-12345",
            EffectiveDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiryDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Premium = new Money(800m, CurrencyCodeType.Cad),
            IsActive = true
        };
    }
}
