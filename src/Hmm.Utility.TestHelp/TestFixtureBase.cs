﻿using FluentValidation;
using FluentValidation.Results;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Hmm.Utility.TestHelp
{
    public class TestFixtureBase : IDisposable
    {
        private const int PageIdx = 1;
        private const int PageSize = 20;
        private List<Author> _authors;
        private List<NoteRender> _renders;
        private List<NoteCatalog> _catalogs;
        private List<HmmNote> _notes;
        private List<Subsystem> _systems;

        protected TestFixtureBase()
        {
        }

        protected DateTime CurrentTime { get; set; } = DateTime.UtcNow;

        protected IGuidRepository<Author> AuthorRepository { get; private set; }

        protected IVersionRepository<HmmNote> NoteRepository { get; private set; }

        protected IRepository<NoteCatalog> CatalogRepository { get; private set; }

        protected IRepository<NoteRender> RenderRepository { get; private set; }

        protected IRepository<Subsystem> SubsystemRepository { get; private set; }

        protected IEntityLookup LookupRepo { get; private set; }

        protected IDateTimeProvider DateProvider { get; private set; }

        protected MockAuthorValidator FakeAuthorValidator => new(AuthorRepository);

        protected static MockCatalogValidator FakeCatalogValidator => new();

        protected static MockRenderValidator FakeRenderValidator => new();

        protected MockSubsystemValidator FakeSubsystemValidator => new(AuthorRepository);

        protected MockNoteValidator FakeNoteValidator => new(NoteRepository);

        protected virtual void InsertSeedRecords(
            List<Subsystem> systems = null,
            List<Author> authors = null,
            List<NoteRender> renders = null,
            List<NoteCatalog> catalogs = null)
        {
            systems ??= new List<Subsystem>();
            authors ??= new List<Author>();
            renders ??= new List<NoteRender>();
            catalogs ??= new List<NoteCatalog>();

            // Add subsystem records
            systems.Add(
                new Subsystem
                {
                    Name = "HmmNote",
                    Description = "Default HMM note management"
                });

            // Add authors records
            authors.Add(
                new Author
                {
                    AccountName = "jfang",
                    IsActivated = true,
                    Description = "testing user"
                });
            authors.Add(
                new Author
                {
                    AccountName = "awang",
                    IsActivated = true,
                    Description = "testing user"
                });

            // Add default note render records
            renders.Add(
                new NoteRender
                {
                    Name = "DefaultNoteRender",
                    Namespace = "Hmm.Renders",
                    IsDefault = true,
                    Description = "Testing default note render"
                });

            // Add default note catalogs
            catalogs.Add(
                new NoteCatalog
                {
                    Name = "DefaultNoteCatalog",
                    Schema = "DefaultSchema",
                    Render = renders[0],
                    Subsystem = new Subsystem { Name = "default subsystem" },
                    IsDefault = true,
                    Description = "Testing catalog"
                });

            SetupRecords(authors, renders, catalogs, systems);
        }

        private void SetupRecords(
            IEnumerable<Author> users,
            IEnumerable<NoteRender> renders,
            IEnumerable<NoteCatalog> catalogs,
            IEnumerable<Subsystem> subsystems)
        {
            Guard.Against<ArgumentNullException>(users == null, nameof(users));
            Guard.Against<ArgumentNullException>(renders == null, nameof(renders));
            Guard.Against<ArgumentNullException>(catalogs == null, nameof(catalogs));
            Guard.Against<ArgumentNullException>(subsystems == null, nameof(subsystems));

            SetMockEnvironment();

            // ReSharper disable PossibleNullReferenceException
            foreach (var user in users)
            {
                AuthorRepository.Add(user);
            }

            foreach (var render in renders)
            {
                RenderRepository.Add(render);
            }

            foreach (var catalog in catalogs)
            {
                if (catalog.Render != null)
                {
                    var render = LookupRepo.GetEntities<NoteRender>()
                        .FirstOrDefault(r => r.Name == catalog.Render.Name);
                    if (render != null)
                    {
                        catalog.Render = render;
                    }
                    else
                    {
                        throw new InvalidDataException($"Cannot find render {catalog.Render.Name} from data source");
                    }
                }
                else
                {
                    var render = LookupRepo.GetEntities<NoteRender>().FirstOrDefault();
                    if (render != null)
                    {
                        catalog.Render = render;
                    }
                    else
                    {
                        throw new InvalidDataException("Cannot find default render from data source");
                    }
                }

                CatalogRepository.Add(catalog);
            }

            // ReSharper restore PossibleNullReferenceException
        }

        public void Dispose()
        {
            _authors.Clear();
            _renders.Clear();
            _catalogs.Clear();
            _notes.Clear();
            GC.SuppressFinalize(this);
        }

        protected void AddEntity<T>(T entity)
        {
            switch (entity)
            {
                case HmmNote note:
                    _notes.Add(note);
                    break;

                case NoteCatalog cat:
                    _catalogs.Add(cat);
                    break;
            }
        }

        protected static string GetRandomString(int length)
        {
            if (length < 0)
            {
                return null;
            }

            var random = new Random();

            const string chars = "ABCEDFGHIJKLMNOPQRSTUVWXYZ1234567890";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void SetMockEnvironment()
        {
            _authors = new List<Author>();
            _renders = new List<NoteRender>();
            _catalogs = new List<NoteCatalog>();
            _notes = new List<HmmNote>();
            _systems = new List<Subsystem>();

            // set up for entity look up
            LookupRepo = GetLookupRepo();

            // Setup author repository
            AuthorRepository = GetAuthorRepo();

            // Setup note repository
            NoteRepository = GetNoteRepo();

            // Setup note catalog
            CatalogRepository = GetNoteCatalog();

            // Setup note render
            RenderRepository = GetNoteRender();

            // Setup subsystem
            SubsystemRepository = GetSubsystem();

            var mockDateProvider = new Mock<IDateTimeProvider>();
            mockDateProvider.Setup(t => t.UtcNow).Returns(() => CurrentTime);
            DateProvider = mockDateProvider.Object;
        }

        private IEntityLookup GetLookupRepo()
        {
            var lookupMoc = new Mock<IEntityLookup>();
            lookupMoc.Setup(lk => lk.GetEntity<Author>(It.IsAny<Guid>())).Returns((Guid id) =>
            {
                var recFound = _authors.FirstOrDefault(c => c.Id == id);
                return recFound;
            });
            lookupMoc.Setup(lk =>
                    lk.GetEntities(It.IsAny<Expression<Func<Author, bool>>>(),
                        It.IsAny<ResourceCollectionParameters>()))
                .Returns(() => PageList<Author>.Create(_authors.AsQueryable(), PageIdx, PageSize));

            lookupMoc.Setup(lk => lk.GetEntity<NoteRender>(It.IsAny<int>())).Returns((int id) =>
            {
                var recFound = _renders.FirstOrDefault(c => c.Id == id);
                return recFound;
            });
            lookupMoc.Setup(lk => lk.GetEntities(It.IsAny<Expression<Func<NoteRender, bool>>>(),
                It.IsAny<ResourceCollectionParameters>())).Returns(() =>
                PageList<NoteRender>.Create(_renders.AsQueryable(), PageIdx, PageSize));

            lookupMoc.Setup(lk => lk.GetEntity<NoteCatalog>(It.IsAny<int>())).Returns((int id) =>
            {
                var recFound = _catalogs.FirstOrDefault(c => c.Id == id);
                return recFound;
            });
            lookupMoc.Setup(lk => lk.GetEntities(It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                It.IsAny<ResourceCollectionParameters>()))
                .Returns(() => PageList<NoteCatalog>.Create(_catalogs.AsQueryable(), PageIdx, PageSize));

            lookupMoc.Setup(lk => lk.GetEntity<HmmNote>(It.IsAny<int>()))
                .Returns((int id) =>
                {
                    var rec = _notes.FirstOrDefault(n => n.Id == id);
                    return rec;
                });
            lookupMoc.Setup(lk =>
                    lk.GetEntities(It.IsAny<Expression<Func<HmmNote, bool>>>(),
                        It.IsAny<ResourceCollectionParameters>()))
                .Returns(() => PageList<HmmNote>.Create(_notes.AsQueryable(), PageIdx, PageSize));

            lookupMoc.Setup(sys => sys.GetEntity<Subsystem>(It.IsAny<int>())).Returns((int id) =>
            {
                var recFound = _systems.FirstOrDefault(s => s.Id == id);
                return recFound;
            });

            return lookupMoc.Object;
        }

        private IGuidRepository<Author> GetAuthorRepo()
        {
            var mockAuthors = new Mock<IGuidRepository<Author>>();
            mockAuthors.Setup(a => a.Add(It.IsAny<Author>())).Returns((Author author) =>
            {
                var savedAuthor = author.Clone();
                savedAuthor.Id = Guid.NewGuid();
                _authors.Add(author);
                author = savedAuthor.Clone(author);
                return author;
            });
            mockAuthors.Setup(a => a.Update(It.IsAny<Author>())).Returns((Author author) =>
            {
                var oldAuthor = _authors.FirstOrDefault(a => a.Id == author.Id);
                if (oldAuthor == null)
                {
                    return null;
                }

                oldAuthor = author.Clone(oldAuthor);
                return oldAuthor.Clone();
            });
            mockAuthors
                .Setup(a => a.GetEntities(It.IsAny<Expression<Func<Author, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>())).Returns(
                    (Expression<Func<Author, bool>> query, ResourceCollectionParameters resourceCollectionParameters) =>
                        query == null
                            ? PageList<Author>.Create(_authors.AsQueryable(), PageIdx, PageSize)
                            : PageList<Author>.Create(_authors.AsQueryable().Where(query), PageIdx, PageSize));
            mockAuthors.Setup(a => a.GetEntity(It.IsAny<Guid>())).Returns((Guid id) => _authors.FirstOrDefault(a => a.Id == id));
            return mockAuthors.Object;
        }

        private IRepository<NoteCatalog> GetNoteCatalog()
        {
            var mockCatalogs = new Mock<IRepository<NoteCatalog>>();
            mockCatalogs.Setup(c => c.Add(It.IsAny<NoteCatalog>())).Returns((NoteCatalog catalog) =>
            {
                var savedCat = catalog.Clone();
                savedCat.Id += _catalogs.Count + 1;
                _catalogs.Add(savedCat);
                catalog = savedCat.Clone();
                if (_renders.All(r => r.Id != catalog.Render.Id))
                {
                    var savedRender = catalog.Render.Clone();
                    savedRender.Id += _renders.Count + 1;
                    _renders.Add(savedRender);
                    catalog.Render = savedRender.Clone();
                }
                else
                {
                    catalog.Render = _renders.FirstOrDefault(r => r.Id == catalog.Render.Id);
                }
                return catalog;
            });
            mockCatalogs.Setup(c => c.Update(It.IsAny<NoteCatalog>())).Returns((NoteCatalog cat) =>
            {
                var oldCat = _catalogs.FirstOrDefault(c => c.Id == cat.Id);
                if (oldCat == null)
                {
                    return null;
                }

                oldCat = cat.Clone(oldCat);
                return oldCat;
            });
            mockCatalogs
                .Setup(a => a.GetEntity(It.IsAny<int>())).Returns((int id) =>
                {
                    var savedCat = _catalogs.FirstOrDefault(n => n.Id == id);
                    var backCat = savedCat?.Clone();
                    return backCat;
                });
            mockCatalogs
                .Setup(c => c.GetEntities(It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>())).Returns(
                    (Expression<Func<NoteCatalog, bool>> query,
                        ResourceCollectionParameters resourceCollectionParameters) => query == null
                        ? PageList<NoteCatalog>.Create(_catalogs.AsQueryable(), PageIdx, PageSize)
                        : PageList<NoteCatalog>.Create(_catalogs.AsQueryable().Where(query), PageIdx, PageSize));

            return mockCatalogs.Object;
        }

        private IRepository<NoteRender> GetNoteRender()
        {
            var mockRender = new Mock<IRepository<NoteRender>>();
            mockRender.Setup(c => c.Add(It.IsAny<NoteRender>())).Returns((NoteRender render) =>
            {
                var savedRender = render.Clone();
                savedRender.Id += _renders.Count + 1;
                _renders.Add(savedRender);
                render = savedRender.Clone();
                return render;
            });
            mockRender.Setup(c => c.Update(It.IsAny<NoteRender>())).Returns((NoteRender render) =>
            {
                var renderId = render.Id;
                var oldRender = _renders.FirstOrDefault(c => c.Id == renderId);
                if (oldRender == null)
                {
                    return null;
                }

                render = render.Clone(oldRender);
                return render;
            });
            mockRender
                .Setup(a => a.GetEntity(It.IsAny<int>())).Returns((int id) =>
                {
                    var savedRender = _renders.FirstOrDefault(n => n.Id == id);
                    var backRender = savedRender?.Clone();
                    return backRender;
                });
            mockRender
                .Setup(r => r.GetEntities(It.IsAny<Expression<Func<NoteRender, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>())).Returns(
                    (Expression<Func<NoteRender, bool>> query,
                            ResourceCollectionParameters resourceCollectionParameters) =>
                        query == null
                            ? PageList<NoteRender>.Create(_renders.AsQueryable(), PageIdx, PageSize)
                            : PageList<NoteRender>.Create(_renders.AsQueryable().Where(query), PageIdx, PageSize));

            return mockRender.Object;
        }

        private IRepository<Subsystem> GetSubsystem()
        {
            var mockSystem = new Mock<IRepository<Subsystem>>();
            mockSystem.Setup(c => c.Add(It.IsAny<Subsystem>())).Returns((Subsystem system) =>
            {
                var savedSys = system.Clone();
                savedSys.Id += _systems.Count + 1;
                _systems.Add(savedSys);
                system = savedSys.Clone();

                if (_authors.All(a => a.Id != system.DefaultAuthor.Id))
                {
                    var savedAuthor = system.DefaultAuthor.Clone();
                    savedAuthor.Id = Guid.NewGuid();
                    _authors.Add(savedAuthor);
                    system.DefaultAuthor = savedAuthor.Clone();
                }
                else
                {
                    system.DefaultAuthor = _authors.FirstOrDefault(u => u.Id == system.DefaultAuthor.Id);
                }

                if (system.NoteCatalogs != null && system.NoteCatalogs.Any())
                {
                    var newCatalogs = new List<NoteCatalog>();
                    foreach (var catalog in system.NoteCatalogs)
                    {
                        if (_catalogs.All(c => c.Id != catalog.Id))
                        {
                            var savedCatalog = catalog.Clone();
                            savedCatalog.Id += _catalogs.Count + 1;
                            _catalogs.Add(savedCatalog);
                            newCatalogs.Add(savedCatalog.Clone());
                        }
                        else
                        {
                            newCatalogs.Add(_catalogs.FirstOrDefault(c => c.Id == catalog.Id));
                        }
                        if (_renders.All(r => r.Id != catalog.Render.Id))
                        {
                            var savedRender = catalog.Render.Clone();
                            savedRender.Id += _renders.Count + 1;
                            _renders.Add(savedRender);
                            catalog.Render = savedRender.Clone();
                        }
                        else
                        {
                            catalog.Render = _renders.FirstOrDefault(r => r.Id == catalog.Render.Id);
                        }
                    }

                    system.NoteCatalogs = newCatalogs;
                }

                return system;
            });
            mockSystem.Setup(c => c.Update(It.IsAny<Subsystem>())).Returns((Subsystem system) =>
            {
                var oldSys = _systems.FirstOrDefault(c => c.Id == system.Id);
                var newSys = oldSys?.Clone();
                return newSys;
            });
            mockSystem.Setup(r => r.GetEntities(It.IsAny<Expression<Func<Subsystem, bool>>>(),
                It.IsAny<ResourceCollectionParameters>())).Returns(() =>
                PageList<Subsystem>.Create(_systems.AsQueryable(), PageIdx, PageSize));

            return mockSystem.Object;
        }

        private IVersionRepository<HmmNote> GetNoteRepo()
        {
            var mockNotes = new Mock<IVersionRepository<HmmNote>>();
            mockNotes.Setup(a => a.Add(It.IsAny<HmmNote>())).Returns((HmmNote note) =>
            {
                var savedNote = note.Clone();
                savedNote.Id = _notes.Count + 1;
                savedNote.Version = Guid.NewGuid().ToByteArray();
                _notes.Add(savedNote);
                note = savedNote.Clone(note);
                return note;
            });
            mockNotes.Setup(a => a.Update(It.IsAny<HmmNote>())).Returns((HmmNote note) =>
            {
                var oldNote = _notes.FirstOrDefault(n => n.Id == note.Id);
                if (oldNote == null)
                {
                    return null;
                }

                oldNote = note.Clone(oldNote);
                oldNote.Version = Guid.NewGuid().ToByteArray();
                return oldNote.Clone();
            });
            mockNotes
                .Setup(a => a.GetEntity(It.IsAny<int>())).Returns((int id) =>
                {
                    var savedNote = _notes.FirstOrDefault(n => n.Id == id);
                    var backNote = savedNote?.Clone();
                    return backNote;
                });
            mockNotes
                .Setup(a => a.GetEntities(It.IsAny<Expression<Func<HmmNote, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>())).Returns((Expression<Func<HmmNote, bool>> query,
                        ResourceCollectionParameters resourceCollectionParameters) =>
                    query == null
                        ? PageList<HmmNote>.Create(_notes.AsQueryable(), PageIdx, PageSize)
                        : PageList<HmmNote>.Create(_notes.AsQueryable().Where(query), PageIdx, PageSize));
            return mockNotes.Object;
        }

        protected class MockSubsystemValidator : SubsystemValidator
        {
            public MockSubsystemValidator(IGuidRepository<Author> authorRepo) : base(authorRepo)
            {
            }

            public bool GetInvalidResult { get; set; }

            public override ValidationResult Validate(ValidationContext<Subsystem> context)
            {
                return GetInvalidResult
                    ? new ValidationResult(new List<ValidationFailure> { new("Subsystem", "Subsystem validator error") })
                    : new ValidationResult();
            }
        }

        protected class MockAuthorValidator : AuthorValidator
        {
            public MockAuthorValidator(IGuidRepository<Author> authorRepo) : base(authorRepo)
            {
            }

            public bool GetInvalidResult { get; set; }

            public override ValidationResult Validate(ValidationContext<Author> context)
            {
                return GetInvalidResult
                    ? new ValidationResult(new List<ValidationFailure> { new("Author", "Author is invalid") })
                    : new ValidationResult();
            }
        }

        protected class MockCatalogValidator : NoteCatalogValidator
        {
            public bool GetInvalidResult { get; set; }

            public override ValidationResult Validate(ValidationContext<NoteCatalog> context)
            {
                return GetInvalidResult
                    ? new ValidationResult(new List<ValidationFailure> { new("NoteCatalog", "note catalog is invalid") })
                    : new ValidationResult();
            }
        }

        protected class MockRenderValidator : NoteRenderValidator
        {
            public bool GetInvalidResult { get; set; }

            public override ValidationResult Validate(ValidationContext<NoteRender> context)
            {
                return GetInvalidResult
                    ? new ValidationResult(new List<ValidationFailure> { new("NoteRender", "note render is invalid") })
                    : new ValidationResult();
            }
        }

        protected class MockNoteValidator : NoteValidator
        {
            public MockNoteValidator(IVersionRepository<HmmNote> noteRepo) : base(noteRepo)
            {
            }

            public bool GetInvalidResult { get; set; }

            public override ValidationResult Validate(ValidationContext<HmmNote> context)
            {
                return GetInvalidResult
                    ? new ValidationResult(new List<ValidationFailure> { new("Note", "note is invalid") })
                    : new ValidationResult();
            }
        }
    }
}