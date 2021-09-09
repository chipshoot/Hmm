using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System;
using System.Linq;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class SubsystemRepositoryTests : DbTestFixtureBase
    {
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
            var newSys = SubsystemRepository.Add(sys);

            // Assert
            Assert.NotNull(newSys);
            Assert.True(newSys.Id > 0, "newSubsystem.Id >=1");
            Assert.Equal("Test subsystem", newSys.Name);
            Assert.True(SubsystemRepository.ProcessMessage.Success);
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
            var newSys = SubsystemRepository.Add(sys);
            var savedSys = LookupRepo.GetEntities<Subsystem>().FirstOrDefault(s => s.Id == newSys.Id);
            Assert.NotNull(savedSys);

            // Act
            savedSys.Description = "changed default Subsystem";
            var updatedSys = SubsystemRepository.Update(savedSys);

            // Assert
            Assert.NotNull(updatedSys);
            Assert.Equal("changed default Subsystem", updatedSys.Description);
            Assert.True(SubsystemRepository.ProcessMessage.Success);
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
            var newSys = SubsystemRepository.Add(sys);

            // Act
            var savedSys = SubsystemRepository.GetEntities().FirstOrDefault(c => c.Id == newSys.Id);

            // Assert
            Assert.NotNull(savedSys);
            Assert.Equal(savedSys.Name, sys.Name);
            Assert.True(SubsystemRepository.ProcessMessage.Success);
        }
    }
}