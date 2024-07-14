//using FluentValidation;
//using FluentValidation.Results;
//using Hmm.Core.DefaultManager.Validator;
//using Hmm.Core.DomainEntity;
//using Hmm.Utility.Dal.Query;
//using Hmm.Utility.Dal.Repository;
//using Hmm.Utility.Misc;
//using Hmm.Utility.Validation;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Linq.Expressions;
//using Hmm.Core.DbEntity;

//namespace Hmm.Utility.TestHelp
//{
//    public class TestFixtureBase : IDisposable
//    {
//        private const int PageIdx = 1;
//        private const int PageSize = 20;
//        private List<AuthorDb> _authors;
//        private List<ContactDb> _contacts;
//        private List<NoteCatalog> _catalogs;
//        private List<HmmNote> _notes;

//        protected TestFixtureBase()
//        {
//        }

//        protected DateTime CurrentTime { get; set; } = DateTime.UtcNow;

//        protected IRepository<AuthorDb> AuthorRepository { get; private set; }
//        protected IRepository<ContactDb> ContactRepository { get; private set; }

//        protected IVersionRepository<HmmNote> NoteRepository { get; private set; }

//        protected IRepository<NoteCatalog> CatalogRepository { get; private set; }

//        protected IEntityLookup LookupRepository { get; private set; }

//        protected IDateTimeProvider DateProvider { get; private set; }

//        protected MockAuthorValidator FakeAuthorValidator => new(AuthorRepository);

//        protected static MockCatalogValidator FakeCatalogValidator => new();

//        protected MockNoteValidator FakeNoteValidator => new(NoteRepository);

//        protected virtual void InsertSeedRecords(
//            List<AuthorDb> authors = null,
//            List<NoteCatalog> catalogs = null,
//            List<ContactDb> contacts = null)
//        {
//            authors ??= [];
//            contacts ??= [];
//            catalogs ??= [];

//            // Add authors records
//            authors.Add(
//                new AuthorDb
//                {
//                    AccountName = "jfang",
//                    IsActivated = true,
//                    Description = "testing user"
//                });
//            contacts.Add(
//                new ContactDb
//                {
//                    Contact = "{}",
//                    IsActivated = true,
//                    Description = "testing user"
//                });

//            // Add default note catalogs
//            catalogs.Add(
//                new NoteCatalog
//                {
//                    Name = "DefaultNoteCatalog",
//                    Schema = "DefaultSchema",
//                    IsDefault = true,
//                    Description = "Testing catalog"
//                });

//            // Add default contact
//            //contacts.Add(
//            //    new ContactDb
//            //    {
//            //        Id = 1,
//            //        FirstName = "Chaoyang",
//            //        LastName = "Fang",
//            //        Addresses =
//            //        [
//            //            new AddressInfo
//            //            {
//            //                Address = "401-1750 Bloor St.", City = "Mississauga", Country = "Canada",
//            //                PostalCode = "L4X 1S9", State = "Ontario", IsPrimary = true, Type = AddressType.Home
//            //            }
//            //        ],
//            //        Emails =
//            //        [
//            //            new Email { Address = "fchy@yahoo.com", IsPrimary = false, Type = EmailType.Personal },
//            //            new Email { Address = "fchy5979@gmail.com", IsPrimary = true, Type = EmailType.Personal }
//            //        ],
//            //        Phones =
//            //        [
//            //            new Phone { Number = "647-291-5959", IsPrimary = true, Type = TelephoneType.Mobile },
//            //            new Phone { Number = "905-232-5979", IsPrimary = false, Type = TelephoneType.Home }
//            //        ],
//            //        Description = "Testing contact 1",
//            //        IsActivated = true
//            //    });
//        }

//        private void SetupRecords(
//            IEnumerable<AuthorDb> users,
//            IEnumerable<NoteCatalog> catalogs,
//            IEnumerable<Contact> contacts)
//        {
//            Guard.Against<ArgumentNullException>(users == null, nameof(users));
//            Guard.Against<ArgumentNullException>(catalogs == null, nameof(catalogs));
//            Guard.Against<ArgumentNullException>(contacts == null, nameof(contacts));

//            SetMockEnvironment();

//            // ReSharper disable PossibleNullReferenceException
//            foreach (var user in users)
//            {
//                AuthorRepository.Add(user);
//            }

//            foreach (var catalog in catalogs)
//            {
//                CatalogRepository.Add(catalog);
//            }

//            foreach (var contact in contacts)
//            {
//            }

//            // ReSharper restore PossibleNullReferenceException
//        }

//        public void Dispose()
//        {
//            _authors.Clear();
//            _catalogs.Clear();
//            _notes.Clear();
//            GC.SuppressFinalize(this);
//        }

//        protected void AddEntity<T>(T entity)
//        {
//            switch (entity)
//            {
//                case HmmNote note:
//                    _notes.Add(note);
//                    break;

//                case NoteCatalog cat:
//                    _catalogs.Add(cat);
//                    break;
//            }
//        }

//        protected static string GetRandomString(int length)
//        {
//            if (length < 0)
//            {
//                return null;
//            }

//            var random = new Random();

//            const string chars = "ABCEDFGHIJKLMNOPQRSTUVWXYZ1234567890";
//            return new string(Enumerable.Repeat(chars, length)
//                .Select(s => s[random.Next(s.Length)]).ToArray());
//        }

//        private void SetMockEnvironment()
//        {
//            _authors = [];
//            _catalogs = [];
//            _notes = [];

//            // set up for entity look up
//            LookupRepository = GetLookupRepo();

//            // Setup author repository
//            AuthorRepository = GetAuthorRepo();

//            // Setup note repository
//            NoteRepository = GetNoteRepo();

//            // Setup note catalog
//            CatalogRepository = GetNoteCatalog();

//            var mockDateProvider = new Mock<IDateTimeProvider>();
//            mockDateProvider.Setup(t => t.UtcNow).Returns(() => CurrentTime);
//            DateProvider = mockDateProvider.Object;
//        }

//        private IEntityLookup GetLookupRepo()
//        {
//            var lookupMoc = new Mock<IEntityLookup>();
//            lookupMoc.Setup(lk => lk.GetEntity<AuthorDb>(It.IsAny<int>())).Returns((int id) =>
//            {
//                var recFound = _authors.FirstOrDefault(c => c.Id == id);
//                return recFound;
//            });
//            lookupMoc.Setup(lk =>
//                    lk.GetEntities(It.IsAny<Expression<Func<AuthorDb, bool>>>(),
//                        It.IsAny<ResourceCollectionParameters>()))
//                .Returns(() => PageList<AuthorDb>.Create(_authors.AsQueryable(), PageIdx, PageSize));

//            lookupMoc.Setup(lk => lk.GetEntity<NoteCatalog>(It.IsAny<int>())).Returns((int id) =>
//            {
//                var recFound = _catalogs.FirstOrDefault(c => c.Id == id);
//                return recFound;
//            });
//            lookupMoc.Setup(lk => lk.GetEntities(It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
//                It.IsAny<ResourceCollectionParameters>()))
//                .Returns(() => PageList<NoteCatalog>.Create(_catalogs.AsQueryable(), PageIdx, PageSize));

//            lookupMoc.Setup(lk => lk.GetEntity<HmmNote>(It.IsAny<int>()))
//                .Returns((int id) =>
//                {
//                    var rec = _notes.FirstOrDefault(n => n.Id == id);
//                    return rec;
//                });
//            lookupMoc.Setup(lk =>
//                    lk.GetEntities(It.IsAny<Expression<Func<HmmNote, bool>>>(),
//                        It.IsAny<ResourceCollectionParameters>()))
//                .Returns(() => PageList<HmmNote>.Create(_notes.AsQueryable(), PageIdx, PageSize));

//            return lookupMoc.Object;
//        }

//        private IRepository<AuthorDb> GetAuthorRepo()
//        {
//            var mockAuthors = new Mock<IRepository<AuthorDb>>();
//            var nextId = 1;
//            mockAuthors.Setup(a => a.Add(It.IsAny<AuthorDb>())).Returns((AuthorDb author) =>
//            {
//                var savedAuthor = author.Clone();
//                savedAuthor.Id = nextId++;
//                _authors.Add(author);
//                author = savedAuthor.Clone(author);
//                return author;
//            });
//            mockAuthors.Setup(a => a.Update(It.IsAny<AuthorDb>())).Returns((AuthorDb author) =>
//            {
//                var oldAuthor = _authors.FirstOrDefault(a => a.Id == author.Id);
//                if (oldAuthor == null)
//                {
//                    return null;
//                }

//                oldAuthor = author.Clone(oldAuthor);
//                return oldAuthor.Clone();
//            });
//            mockAuthors
//                .Setup(a => a.GetEntities(It.IsAny<Expression<Func<AuthorDb, bool>>>(),
//                    It.IsAny<ResourceCollectionParameters>())).Returns(
//                    (Expression<Func<AuthorDb, bool>> query, ResourceCollectionParameters resourceCollectionParameters) =>
//                        query == null
//                            ? PageList<AuthorDb>.Create(_authors.AsQueryable(), PageIdx, PageSize)
//                            : PageList<AuthorDb>.Create(_authors.AsQueryable().Where(query), PageIdx, PageSize));
//            mockAuthors.Setup(a => a.GetEntity(It.IsAny<int>())).Returns((int id) => _authors.FirstOrDefault(a => a.Id == id));
//            return mockAuthors.Object;
//        }

//        private IRepository<NoteCatalog> GetNoteCatalog()
//        {
//            var mockCatalogs = new Mock<IRepository<NoteCatalog>>();
//            mockCatalogs.Setup(c => c.Add(It.IsAny<NoteCatalog>())).Returns((NoteCatalog catalog) =>
//            {
//                var savedCat = catalog.Clone();
//                savedCat.Id += _catalogs.Count + 1;
//                _catalogs.Add(savedCat);
//                catalog = savedCat.Clone();
//                return catalog;
//            });
//            mockCatalogs.Setup(c => c.Update(It.IsAny<NoteCatalog>())).Returns((NoteCatalog cat) =>
//            {
//                var oldCat = _catalogs.FirstOrDefault(c => c.Id == cat.Id);
//                if (oldCat == null)
//                {
//                    return null;
//                }

//                oldCat = cat.Clone(oldCat);
//                return oldCat;
//            });
//            mockCatalogs
//                .Setup(a => a.GetEntity(It.IsAny<int>())).Returns((int id) =>
//                {
//                    var savedCat = _catalogs.FirstOrDefault(n => n.Id == id);
//                    var backCat = savedCat?.Clone();
//                    return backCat;
//                });
//            mockCatalogs
//                .Setup(c => c.GetEntities(It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
//                    It.IsAny<ResourceCollectionParameters>())).Returns(
//                    (Expression<Func<NoteCatalog, bool>> query,
//                        ResourceCollectionParameters resourceCollectionParameters) => query == null
//                        ? PageList<NoteCatalog>.Create(_catalogs.AsQueryable(), PageIdx, PageSize)
//                        : PageList<NoteCatalog>.Create(_catalogs.AsQueryable().Where(query), PageIdx, PageSize));

//            return mockCatalogs.Object;
//        }

//        private IVersionRepository<HmmNote> GetNoteRepo()
//        {
//            var mockNotes = new Mock<IVersionRepository<HmmNote>>();
//            mockNotes.Setup(a => a.Add(It.IsAny<HmmNote>())).Returns((HmmNote note) =>
//            {
//                var savedNote = note.Clone();
//                savedNote.Id = _notes.Count + 1;
//                savedNote.Version = Guid.NewGuid().ToByteArray();
//                _notes.Add(savedNote);
//                note = savedNote.Clone(note);
//                return note;
//            });
//            mockNotes.Setup(a => a.Update(It.IsAny<HmmNote>())).Returns((HmmNote note) =>
//            {
//                var oldNote = _notes.FirstOrDefault(n => n.Id == note.Id);
//                if (oldNote == null)
//                {
//                    return null;
//                }

//                oldNote = note.Clone(oldNote);
//                oldNote.Version = Guid.NewGuid().ToByteArray();
//                return oldNote.Clone();
//            });
//            mockNotes
//                .Setup(a => a.GetEntity(It.IsAny<int>())).Returns((int id) =>
//                {
//                    var savedNote = _notes.FirstOrDefault(n => n.Id == id);
//                    var backNote = savedNote?.Clone();
//                    return backNote;
//                });
//            mockNotes
//                .Setup(a => a.GetEntities(It.IsAny<Expression<Func<HmmNote, bool>>>(),
//                    It.IsAny<ResourceCollectionParameters>())).Returns((Expression<Func<HmmNote, bool>> query,
//                        ResourceCollectionParameters resourceCollectionParameters) =>
//                    query == null
//                        ? PageList<HmmNote>.Create(_notes.AsQueryable(), PageIdx, PageSize)
//                        : PageList<HmmNote>.Create(_notes.AsQueryable().Where(query), PageIdx, PageSize));
//            return mockNotes.Object;
//        }

//        protected class MockAuthorValidator : AuthorValidator
//        {
//            public MockAuthorValidator(IRepository<AuthorDb> authorRepository) : base(authorRepository)
//            {
//            }

//            public bool GetInvalidResult { get; set; }

//            public override ValidationResult Validate(ValidationContext<AuthorDb> context)
//            {
//                return GetInvalidResult
//                    ? new ValidationResult(new List<ValidationFailure> { new("Author", "Author is invalid") })
//                    : new ValidationResult();
//            }
//        }

//        protected class MockCatalogValidator : NoteCatalogValidator
//        {
//            public bool GetInvalidResult { get; set; }

//            public override ValidationResult Validate(ValidationContext<NoteCatalog> context)
//            {
//                return GetInvalidResult
//                    ? new ValidationResult(new List<ValidationFailure> { new("NoteCatalog", "note catalog is invalid") })
//                    : new ValidationResult();
//            }
//        }

//        protected class MockNoteValidator : NoteValidator
//        {
//            public MockNoteValidator(IVersionRepository<HmmNote> noteRepository) : base(noteRepository)
//            {
//            }

//            public bool GetInvalidResult { get; set; }

//            public override ValidationResult Validate(ValidationContext<HmmNote> context)
//            {
//                return GetInvalidResult
//                    ? new ValidationResult(new List<ValidationFailure> { new("Note", "note is invalid") })
//                    : new ValidationResult();
//            }
//        }
//    }
//}