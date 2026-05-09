using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutoScheduledServiceJsonNoteSerializeTests : AutoTestFixtureBase
    {
        private AutoScheduledServiceJsonNoteSerialize _serializer;

        public AutoScheduledServiceJsonNoteSerializeTests()
        {
            InsertSeedRecords();
            _serializer = new AutoScheduledServiceJsonNoteSerialize(CatalogProvider, new NullLogger<AutoScheduledService>());
        }

        [Fact]
        public void GetNoteSerializationText_NullEntity_ReturnsEmptyString()
        {
            Assert.Empty(_serializer.GetNoteSerializationText(null));
        }

        [Fact]
        public async Task RoundTrip_ScalarFields_PreservesData()
        {
            var original = CreateValidSchedule();

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(original.AutomobileId, result.Value.AutomobileId);
            Assert.Equal(original.Name, result.Value.Name);
            Assert.Equal(original.Type, result.Value.Type);
            Assert.Equal(original.IsActive, result.Value.IsActive);
            Assert.Equal(original.Notes, result.Value.Notes);
        }

        [Fact]
        public async Task RoundTrip_NullableIntervals_PreservesNull()
        {
            var original = CreateValidSchedule();
            original.IntervalDays = null;
            original.IntervalMileage = null;
            original.NextDueDate = null;
            original.NextDueMileage = null;

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.Null(result.Value.IntervalDays);
            Assert.Null(result.Value.IntervalMileage);
            Assert.Null(result.Value.NextDueDate);
            Assert.Null(result.Value.NextDueMileage);
        }

        [Fact]
        public async Task RoundTrip_NextDueDate_PreservesUtcValue()
        {
            var original = CreateValidSchedule();
            original.NextDueDate = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            Assert.True(result.Success);
            Assert.NotNull(result.Value.NextDueDate);
            Assert.Equal(original.NextDueDate.Value.ToUniversalTime(), result.Value.NextDueDate.Value.ToUniversalTime());
        }

        [Fact]
        public async Task GetNote_SetsCorrectSubject()
        {
            var schedule = CreateValidSchedule();
            var result = await _serializer.GetNote(schedule);

            Assert.True(result.Success);
            Assert.Equal(NoteSubjectBuilder.BuildAutoScheduledServiceSubject(schedule.AutomobileId), result.Value.Subject);
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
            NextDueDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            NextDueMileage = 53000,
            IsActive = true,
            Notes = "Use synthetic",
            CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastModifiedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        private HmmNote CreateNote(string content, int id = 1) => new()
        {
            Id = id,
            Author = TestDefaultAuthor,
            Subject = NoteSubjectBuilder.BuildAutoScheduledServiceSubject(5),
            Content = content,
            CreateDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
    }
}
