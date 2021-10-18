//using FluentValidation;
//using FluentValidation.Results;
//using Hmm.Core.DefaultManager;
//using Hmm.Core.DefaultManager.Validator;
//using Hmm.Core.DomainEntity;
//using Hmm.Utility.Dal.Repository;
//using Hmm.Utility.TestHelp;
//using System;
//using System.Collections.Generic;
//using Xunit;

//namespace Hmm.Core.Tests
//{
//    public class ApplicationRegisterTests : TestFixtureBase
//    {
//        private ISubsystemManager _subSystemManager;
//        private IAuthorManager _authorManager;
//        private INoteCatalogManager _catalogManager;
//        private INoteRenderManager _renderManager;
//        private readonly IApplicationRegister _register;

//        public ApplicationRegisterTests()
//        {
//            SetupTestEnv();
//            _register = new ApplicationRegister(_subSystemManager, _authorManager, _catalogManager, _renderManager);
//        }

//        [Fact]
//        public void Can_Register_Application()
//        {
//            // Arrange
//            var author = new Author
//            {
//                Id = new Guid("3A4A0B4A-2B97-421C-BE39-C57774C1964C"),
//                AccountName = "FA96E03D-2211-4F07-A18C-E4D358DEF813",
//                Description = "Default author of automobile info manager",
//                IsActivated = true,
//                Role = AuthorRoleType.Author
//            };
//            var system = new Subsystem
//            {
//                Id = 1,
//                DefaultAuthor = author,
//                Description = "Automobile information manager system",
//                IsDefault = false,
//                Name = "Automobile",
//                NoteCatalogs = new List<NoteCatalog>()
//            };

//            // Act
//            var result = _register.Register(system);

//            // Assert
//            Assert.True(result);
//        }

//        [Fact]
//        public void Can_Check_Application_Is_Registered()
//        {
//            // Arrange
//            var system = new Subsystem
//            {
//                Id = 1,
//                DefaultAuthor = new Author
//                {
//                    Id = new Guid("3A4A0B4A-2B97-421C-BE39-C57774C1964C"),
//                    AccountName = "FA96E03D-2211-4F07-A18C-E4D358DEF813",
//                    Description = "Default author of automobile info manager",
//                    IsActivated = true,
//                    Role = AuthorRoleType.Author
//                },
//                Description = "Automobile information manager system",
//                IsDefault = false,
//                Name = "Automobile",
//                NoteCatalogs = new List<NoteCatalog>()
//            };
//            RegisterApp(system);

//            // Act
//            var isRegistered = _register.HasApplicationRegistered(system);

//            // Assert
//            Assert.True(isRegistered);
//        }

//        [Fact]
//        public void Can_Update_Application_Registered_Elements()
//        {
//            // Arrange
//            var system = new Subsystem
//            {
//                Id = 1,
//                DefaultAuthor = new Author
//                {
//                    Id = new Guid("3A4A0B4A-2B97-421C-BE39-C57774C1964C"),
//                    AccountName = "FA96E03D-2211-4F07-A18C-E4D358DEF813",
//                    Description = "Default author of automobile info manager",
//                    IsActivated = true,
//                    Role = AuthorRoleType.Author
//                },
//                Description = "Automobile information manager system",
//                IsDefault = false,
//                Name = "Automobile",
//                NoteCatalogs = new List<NoteCatalog>()
//            };
//            RegisterApp(system);
//            system.Description = "Automobile Information";

//            // Act
//            var updated = _register.UpdateElementSchema(system);

//            // Assert
//            Assert.True(updated);
//        }

//        private void RegisterApp(Subsystem system)
//        {
//            _register.Register(system);
//        }

//        private void SetupTestEnv()
//        {
//            InsertSeedRecords();
//            var testValidator = new FakeAuthorValidator(AuthorRepository);
//            _subSystemManager = new SubsystemManager(SubsystemRepository, new FakeSystemValidator());
//            _authorManager = new AuthorManager(AuthorRepository, testValidator);
//            _catalogManager = new NoteCatalogManager(CatalogRepository, new FakeCatalogValidator());
//            _renderManager = new NoteRenderManager(RenderRepository, new FakeRenderValidator());
//        }

//        private class FakeAuthorValidator : AuthorValidator
//        {
//            public FakeAuthorValidator(IGuidRepository<Author> authorRepo) : base(authorRepo)
//            {
//            }

//            private bool GetInvalidResult { get; set; }

//            public override ValidationResult Validate(ValidationContext<Author> context)
//            {
//                return GetInvalidResult
//                    ? new ValidationResult(new List<ValidationFailure> { new("Author", "Author is invalid") })
//                    : new ValidationResult();
//            }
//        }

//        private class FakeSystemValidator : SubsystemValidator
//        {
//            private bool GetInvalidResult { get; set; }

//            public override ValidationResult Validate(ValidationContext<Subsystem> context)
//            {
//                return GetInvalidResult
//                    ? new ValidationResult(new List<ValidationFailure> { new("Subsystem", "Subsystem is invalid") })
//                    : new ValidationResult();
//            }
//        }

//        private class FakeCatalogValidator : NoteCatalogValidator
//        {
//            private bool GetInvalidResult { get; set; }

//            public override ValidationResult Validate(ValidationContext<NoteCatalog> context)
//            {
//                return GetInvalidResult
//                    ? new ValidationResult(new List<ValidationFailure> { new("NoteCatalog", "note catalog is invalid") })
//                    : new ValidationResult();
//            }
//        }

//        private class FakeRenderValidator : NoteRenderValidator
//        {
//            private bool GetInvalidResult { get; set; }

//            public override ValidationResult Validate(ValidationContext<NoteRender> context)
//            {
//                return GetInvalidResult
//                    ? new ValidationResult(new List<ValidationFailure> { new("NoteRender", "note render is invalid") })
//                    : new ValidationResult();
//            }
//        }
//    }
//}