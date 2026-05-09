using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutoInsurancePolicyJsonNoteSerializeTests : AutoTestFixtureBase
    {
        private AutoInsurancePolicyJsonNoteSerialize _serializer;

        public AutoInsurancePolicyJsonNoteSerializeTests()
        {
            InsertSeedRecords();
            _serializer = new AutoInsurancePolicyJsonNoteSerialize(CatalogProvider, new NullLogger<AutoInsurancePolicy>());
        }

        [Fact]
        public void GetNoteSerializationText_NullEntity_ReturnsEmptyString()
        {
            Assert.Empty(_serializer.GetNoteSerializationText(null));
        }

        [Fact]
        public void GetNoteSerializationText_ValidEntity_ContainsAllFields()
        {
            var policy = CreateValidPolicy();
            var json = _serializer.GetNoteSerializationText(policy);

            Assert.Contains("\"automobileId\":", json);
            Assert.Contains("\"provider\":", json);
            Assert.Contains("\"policyNumber\":", json);
            Assert.Contains("\"effectiveDate\":", json);
            Assert.Contains("\"expiryDate\":", json);
            Assert.Contains("\"premium\":", json);
            Assert.Contains("\"coverage\":", json);
        }

        [Fact]
        public async Task RoundTrip_ScalarFields_PreservesData()
        {
            var original = CreateValidPolicy();

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(original.AutomobileId, result.Value.AutomobileId);
            Assert.Equal(original.Provider, result.Value.Provider);
            Assert.Equal(original.PolicyNumber, result.Value.PolicyNumber);
            Assert.Equal(original.IsActive, result.Value.IsActive);
            Assert.Equal(original.Notes, result.Value.Notes);
        }

        [Fact]
        public async Task RoundTrip_PremiumMoney_PreservesAmountAndCurrency()
        {
            var original = CreateValidPolicy();
            original.Premium = new Money(1234.56m, CurrencyCodeType.Usd);

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.NotNull(result.Value.Premium);
            Assert.Equal((double)1234.56m, result.Value.Premium.InternalAmount);
            Assert.Equal(CurrencyCodeType.Usd, result.Value.Premium.Currency);
        }

        [Fact]
        public async Task RoundTrip_CoverageList_PreservesAllItems()
        {
            var original = CreateValidPolicy();
            original.Coverage = new List<CoverageItem>
            {
                new() { Type = "Liability", Limit = 1000000, Deductible = 500, Currency = "CAD" },
                new() { Type = "Collision", Limit = 50000, Deductible = 1000, Currency = "CAD" }
            };

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(2, result.Value.Coverage.Count);
            Assert.Equal("Liability", result.Value.Coverage[0].Type);
            Assert.Equal(1000000m, result.Value.Coverage[0].Limit);
            Assert.Equal("Collision", result.Value.Coverage[1].Type);
            Assert.Equal(1000m, result.Value.Coverage[1].Deductible);
        }

        [Fact]
        public async Task RoundTrip_NullDeductible_PreservesNull()
        {
            var original = CreateValidPolicy();
            original.Deductible = null;

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.Null(result.Value.Deductible);
        }

        [Fact]
        public async Task GetEntity_SetsIdAndAuthorIdFromNote()
        {
            var original = CreateValidPolicy();
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, id: 42);

            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(42, result.Value.Id);
            Assert.Equal(TestDefaultAuthor.Id, result.Value.AuthorId);
        }

        [Fact]
        public async Task GetEntity_InvalidJson_ReturnsError()
        {
            var note = CreateNote("not-valid-json");
            var result = await _serializer.GetEntity(note);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetNote_SetsCorrectSubject()
        {
            var policy = CreateValidPolicy();
            var result = await _serializer.GetNote(policy);

            Assert.True(result.Success);
            Assert.Equal(NoteSubjectBuilder.BuildAutoInsurancePolicySubject(policy.AutomobileId), result.Value.Subject);
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
            Deductible = 500m,
            Coverage = new List<CoverageItem>(),
            Notes = "Standard coverage",
            IsActive = true,
            CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastModifiedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        private HmmNote CreateNote(string content, int id = 1) => new()
        {
            Id = id,
            Author = TestDefaultAuthor,
            Subject = NoteSubjectBuilder.BuildAutoInsurancePolicySubject(5),
            Content = content,
            CreateDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
    }
}
