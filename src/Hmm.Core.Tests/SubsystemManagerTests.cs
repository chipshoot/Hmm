using FluentValidation;
using FluentValidation.Results;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class SubsystemManagerTests : TestFixtureBase
    {
        private readonly ISubsystemManager _manager;
        private readonly FakeValidator _testValidator;

        public SubsystemManagerTests()
        {
            InsertSeedRecords();
            _testValidator = new FakeValidator();
            _manager = new SubsystemManager(SubsystemRepository, _testValidator);
        }

        [Fact]
        public void Can_Add_Subsystem_To_DataSource()
        {
            // Arrange
            var sys = new Subsystem
            {
                Name = "Test subsystem",
                Description = "Default subsystem"
            };

            // Act
            var newSys = _manager.Create(sys);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(newSys);
            Assert.True(newSys.Id >= 1, "newSubsystem.Id >=1");
            Assert.Equal("Test subsystem", newSys.Name);
        }

        [Fact]
        public void Can_Update_Subsystem()
        {
            // Arrange
            var sys = new Subsystem
            {
                Name = "Test Subsystem",
                Description = "Default Subsystem"
            };
            _manager.Create(sys);
            var savedSys = _manager.GetEntities().FirstOrDefault(s => s.Id == 1);
            Assert.NotNull(savedSys);

            // Act
            savedSys.Description = "changed default Subsystem";
            var updatedSys = _manager.Update(savedSys);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(updatedSys);
            Assert.Equal("changed default Subsystem", updatedSys.Description);
        }

        [Fact]
        public void Cannot_Update_Invalid_Subsystem()
        {
            // Arrange
            var sys = new Subsystem
            {
                Name = "Test Subsystem",
                Description = "Default Subsystem"
            };
            var savedSys = _manager.Create(sys);
            _testValidator.GetInvalidResult = true;

            // Act
            savedSys.Name = "updated test Subsystem";
            var updatedRender = _manager.Update(savedSys);

            // Assert
            Assert.False(_manager.ProcessResult.Success);
            Assert.Null(updatedRender);
        }

        [Fact]
        public void Can_Search_Subsystem_By_Id()
        {
            // Arrange
            var sys = new Subsystem
            {
                Name = "Test Subsystem",
                Description = "Default Subsystem"
            };
            var newSys = _manager.Create(sys);

            // Act
            var savedSys = _manager.GetEntities().FirstOrDefault(c => c.Id == newSys.Id);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedSys);
            Assert.Equal(savedSys.Name, sys.Name);
        }

        private class FakeValidator : SubsystemValidator
        {
            public bool GetInvalidResult { get; set; }

            public override ValidationResult Validate(ValidationContext<Subsystem> context)
            {
                return GetInvalidResult
                    ? new ValidationResult(new List<ValidationFailure> { new("Subsystem", "Subsystem validator error") })
                    : new ValidationResult();
            }
        }
    }
}