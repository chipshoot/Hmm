using System;
using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.MeasureUnit;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using Hmm.Automobile.Validator;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class DiscountManagerTests : AutoTestFixtureBase
    {
        private IAutoEntityManager<GasDiscount> _manager;
        private Guid _authorId;

        public DiscountManagerTests()
        {
            SetupDevEnv();
        }

        [Fact]
        public void CanCreateDiscount()
        {
            // Arrange
            var discount = new GasDiscount
            {
                Program = "Costco membership",
                Amount = 0.6m.GetCad(),
                DiscountType = GasDiscountType.PerLiter,
                Comment = "Test Discount",
                IsActive = true,
            };

            // Act
            var savedDisc = _manager.Create(discount);

            // Assert
            Assert.NotNull(savedDisc);
            Assert.True(savedDisc.Id >= 1, "savedDisc.Id>=1");
            Assert.Equal(_authorId.ToString(), savedDisc.AuthorId.ToString());
        }

        [Fact]
        public void CanUpdateDiscount()
        {
            // Arrange
            var discounts = SetupEnvironment();
            var discount = discounts.OrderBy(d => d.Id).FirstOrDefault();
            Assert.NotNull(discount);

            // Act
            discount.Program = "Petro-Canada";
            var savedDiscount = _manager.Update(discount);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedDiscount);
            Assert.True(savedDiscount.Program == "Petro-Canada", "savedDiscount.Program=='Petro-Canada'");
        }

        [Fact]
        public void CanGetDiscounts()
        {
            //Arrange
            SetupEnvironment();

            // Act
            var savedDiscounts = _manager.GetEntities().ToList();

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedDiscounts);
            Assert.Equal(2, savedDiscounts.Count);
        }

        [Fact]
        public void CanGetDiscountById()
        {
            //Arrange
            var discounts = SetupEnvironment();

            // Act
            var savedDiscount = _manager.GetEntityById(discounts.Select(d => d.Id).Max());

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedDiscount);
            Assert.True(savedDiscount.Id >= 1, "savedDiscount.Id >= 1");
            Assert.Equal(GasDiscountType.PerLiter, savedDiscount.DiscountType);
            Assert.Equal("Petro-Canada membership", savedDiscount.Program);
        }

        private List<GasDiscount> SetupEnvironment()
        {
            var discounts = new List<GasDiscount>();
            var discount = new GasDiscount
            {
                Program = "Costco membership",
                Amount = 0.6m.GetCad(),
                DiscountType = GasDiscountType.PerLiter,
                Comment = "Test Discount",
                IsActive = true,
            };
            var rec = _manager.Create(discount);
            discounts.Add(rec);

            discount = new GasDiscount
            {
                Program = "Petro-Canada membership",
                Amount = 0.2m.GetCad(),
                DiscountType = GasDiscountType.PerLiter,
                Comment = "Test Discount 2",
                IsActive = true,
            };

            rec = _manager.Create(discount);
            discounts.Add(rec);

            return discounts;
        }

        private void SetupDevEnv()
        {
            InsertSeedRecords();
            var catalog = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.GasDiscountCatalogName);
            Assert.NotNull(catalog);
            var noteSerializer = new GasDiscountXmlNoteSerializer(Application, new NullLogger<GasDiscountXmlNoteSerializer>());
            var noteManager = new HmmNoteManager(NoteRepository, new NoteValidator(NoteRepository), DateProvider);
            _manager = new DiscountManager(noteSerializer, new GasDiscountValidator(LookupRepo), noteManager, LookupRepo);
            _authorId = Application.GetApplication().DefaultAuthor.Id;
        }
    }
}