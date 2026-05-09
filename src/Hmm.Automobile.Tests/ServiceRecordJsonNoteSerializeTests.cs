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
    public class ServiceRecordJsonNoteSerializeTests : AutoTestFixtureBase
    {
        private ServiceRecordJsonNoteSerialize _serializer;

        public ServiceRecordJsonNoteSerializeTests()
        {
            InsertSeedRecords();
            _serializer = new ServiceRecordJsonNoteSerialize(CatalogProvider, new NullLogger<ServiceRecord>());
        }

        [Fact]
        public void GetNoteSerializationText_NullEntity_ReturnsEmptyString()
        {
            Assert.Empty(_serializer.GetNoteSerializationText(null));
        }

        [Fact]
        public async Task RoundTrip_ScalarFields_PreservesData()
        {
            var original = CreateValidRecord();

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(original.AutomobileId, result.Value.AutomobileId);
            Assert.Equal(original.Mileage, result.Value.Mileage);
            Assert.Equal(original.Type, result.Value.Type);
            Assert.Equal(original.Description, result.Value.Description);
            Assert.Equal(original.ShopName, result.Value.ShopName);
        }

        [Fact]
        public async Task RoundTrip_CostMoney_PreservesData()
        {
            var original = CreateValidRecord();
            original.Cost = new Money(120.50m, CurrencyCodeType.Cad);

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.NotNull(result.Value.Cost);
            Assert.Equal((double)120.50m, result.Value.Cost.InternalAmount);
        }

        [Fact]
        public async Task RoundTrip_PartsList_PreservesAllItems()
        {
            var original = CreateValidRecord();
            original.Parts = new List<PartItem>
            {
                new() { Name = "Oil filter", Quantity = 1, UnitCost = new Money(20m, CurrencyCodeType.Cad) },
                new() { Name = "5W-30 oil", Quantity = 5, UnitCost = new Money(8m, CurrencyCodeType.Cad) }
            };

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(2, result.Value.Parts.Count);
            Assert.Equal("Oil filter", result.Value.Parts[0].Name);
            Assert.Equal(1, result.Value.Parts[0].Quantity);
            Assert.Equal("5W-30 oil", result.Value.Parts[1].Name);
            Assert.Equal(5, result.Value.Parts[1].Quantity);
            Assert.NotNull(result.Value.Parts[0].UnitCost);
        }

        [Fact]
        public async Task RoundTrip_AllServiceTypes_PreserveType()
        {
            foreach (ServiceType type in Enum.GetValues<ServiceType>())
            {
                var original = CreateValidRecord();
                original.Type = type;
                var json = _serializer.GetNoteSerializationText(original);
                var note = CreateNote(json);
                var result = await _serializer.GetEntity(note);

                Assert.True(result.Success);
                Assert.Equal(type, result.Value.Type);
            }
        }

        [Fact]
        public async Task GetEntity_SetsIdAndAuthorIdFromNote()
        {
            var original = CreateValidRecord();
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, id: 99);

            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(99, result.Value.Id);
            Assert.Equal(TestDefaultAuthor.Id, result.Value.AuthorId);
        }

        [Fact]
        public async Task GetNote_SetsCorrectSubject()
        {
            var record = CreateValidRecord();
            var result = await _serializer.GetNote(record);

            Assert.True(result.Success);
            Assert.Equal(NoteSubjectBuilder.BuildServiceRecordSubject(record.AutomobileId), result.Value.Subject);
        }

        private ServiceRecord CreateValidRecord() => new()
        {
            Id = 1,
            AuthorId = TestDefaultAuthor.Id,
            AutomobileId = 5,
            Date = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            Mileage = 45000,
            Type = ServiceType.OilChange,
            Description = "5,000 km service",
            Cost = new Money(89m, CurrencyCodeType.Cad),
            ShopName = "Mr. Lube",
            Parts = new List<PartItem>(),
            Notes = "Synthetic oil",
            CreatedDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        private HmmNote CreateNote(string content, int id = 1) => new()
        {
            Id = id,
            Author = TestDefaultAuthor,
            Subject = NoteSubjectBuilder.BuildServiceRecordSubject(5),
            Content = content,
            CreateDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
    }
}
