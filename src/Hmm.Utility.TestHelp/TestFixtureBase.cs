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
            lookupMoc.Setup(lk => lk.GetEntities(It.IsAny<Expression<Func<Author, bool>>>())).Returns(() => _authors.AsQueryable());

            lookupMoc.Setup(lk => lk.GetEntity<NoteRender>(It.IsAny<int>())).Returns((int id) =>
            {
                var recFound = _renders.FirstOrDefault(c => c.Id == id);
                return recFound;
            });
            lookupMoc.Setup(lk => lk.GetEntities(It.IsAny<Expression<Func<NoteRender, bool>>>())).Returns(() => _renders.AsQueryable());

            lookupMoc.Setup(lk => lk.GetEntity<NoteCatalog>(It.IsAny<int>())).Returns((int id) =>
            {
                var recFound = _catalogs.FirstOrDefault(c => c.Id == id);
                return recFound;
            });
            lookupMoc.Setup(lk => lk.GetEntities(It.IsAny<Expression<Func<NoteCatalog, bool>>>())).Returns(() => _catalogs.AsQueryable());

            lookupMoc.Setup(lk => lk.GetEntity<HmmNote>(It.IsAny<int>()))
                .Returns((int id) =>
                {
                    var rec = _notes.FirstOrDefault(n => n.Id == id);
                    return rec;
                });
            lookupMoc.Setup(lk => lk.GetEntities(It.IsAny<Expression<Func<HmmNote, bool>>>())).Returns(() => _notes.AsQueryable());

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
            mockAuthors.Setup(a => a.GetEntities(It.IsAny<Expression<Func<Author, bool>>>())).Returns(() => _authors.AsQueryable());
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
            mockCatalogs.Setup(c => c.GetEntities(It.IsAny<Expression<Func<NoteCatalog, bool>>>())).Returns(() => _catalogs.AsQueryable());

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
            mockRender.Setup(r => r.GetEntities(It.IsAny<Expression<Func<NoteRender, bool>>>())).Returns(() => _renders.AsQueryable());

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
                return system;
            });
            mockSystem.Setup(c => c.Update(It.IsAny<Subsystem>())).Returns((Subsystem system) =>
            {
                var oldSys = _systems.FirstOrDefault(c => c.Id == system.Id);
                var newSys = oldSys?.Clone();
                return newSys;
            });
            mockSystem.Setup(r => r.GetEntities(It.IsAny<Expression<Func<Subsystem, bool>>>())).Returns(() => _systems.AsQueryable());

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
            mockNotes.Setup(a => a.GetEntities(It.IsAny<Expression<Func<HmmNote, bool>>>())).Returns(() => _notes.AsQueryable());
            return mockNotes.Object;
        }
    }
}