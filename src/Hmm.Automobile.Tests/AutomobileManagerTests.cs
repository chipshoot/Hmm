using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using Hmm.Automobile.Validator;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutomobileManagerTests : AutoTestFixtureBase
    {
        private IAutoEntityManager<AutomobileInfo> _manager;

        public AutomobileManagerTests()
        {
            SetupDevEnv();
        }

        [Fact]
        public void CanCreateAutoMobile()
        {
            // Arrange
            var car = new AutomobileInfo
            {
                Brand = "AutoBack",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234",
                Color = "Blue",
                Plate = "BCTT208"
            };

            // Act
            var savedCar = _manager.Create(car);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedCar);
            Assert.True(savedCar.Id >= 1, "car.Id >= 1");
        }

        [Fact]
        public void CanUpdateAutoMobile()
        {
            // Arrange
            var car = SetupEnvironment().FirstOrDefault();
            Assert.NotNull(car);

            // Act
            car.Brand = "AutoBack1";
            var savedCar = _manager.Update(car);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedCar);
            Assert.True(savedCar.Brand == "AutoBack1", "savedCar.Brand=='AutoBack1'");
        }

        [Fact]
        public void CanGetAutoMobile()
        {
            // Arrange
            SetupEnvironment();

            // Act
            var savedCars = _manager.GetEntities();

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedCars);
            var automobiles = savedCars.ToList();
            Assert.True(automobiles.Count == 1, "savedCars.Count == 1");
            var savedCar = automobiles.FirstOrDefault();
            Assert.NotNull(savedCar);
            Assert.Equal("AutoBack", savedCar.Brand);
            Assert.True(savedCar.Id >= 1, "savedCar.Id>=1");
        }

        private IEnumerable<AutomobileInfo> SetupEnvironment()
        {
            var author = AuthorRepository.GetEntities().FirstOrDefault();
            var car = new AutomobileInfo
            {
                Brand = "AutoBack",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234",
                Plate = "BCTT208",
                Color = "Blue"
            };
            Assert.NotNull(author);
            _manager.Create(car);

            return _manager.GetEntities().ToList();
        }

        private void SetupDevEnv()
        {
            InsertSeedRecords();

            // add testing note catalog
            var catalog = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.AutoMobileInfoCatalogName);
            Assert.NotNull(catalog);
            var noteSerializer = new AutomobileXmlNoteSerializer(Application, new NullLogger<AutomobileXmlNoteSerializer>());
            var noteManager = new HmmNoteManager(NoteRepository, new NoteValidator(NoteRepository), DateProvider);
            _manager = new AutomobileManager(noteSerializer, new AutomobileValidator(LookupRepo), noteManager, LookupRepo);
        }
    }
}