using AutoMapper;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.DtoEntity.Profiles;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Utility.TestHelp
{
    public class CoreTestFixtureBase : IDisposable
    {
        private const int PageIdx = 1;
        private const int PageSize = 20;

        private List<AuthorDao> _authorDaos;
        private List<ContactDao> _contactDaos;
        private List<NoteCatalogDao> _catalogDaos;
        private List<HmmNoteDao> _noteDaos;
        private List<TagDao> _tagDaos;

        protected CoreTestFixtureBase()
        {
            SetMockEnvironment();
        }

        #region Properties

        protected DateTime CurrentTime { get; set; } = DateTime.UtcNow;

        protected IRepository<AuthorDao> AuthorRepository { get; private set; }

        protected IRepository<ContactDao> ContactRepository { get; private set; }

        protected IVersionRepository<HmmNoteDao> NoteRepository { get; private set; }

        protected IRepository<NoteCatalogDao> CatalogRepository { get; private set; }

        protected ICompositeEntityRepository<TagDao, HmmNoteDao> TagRepository { get; private set; }

        protected IEntityLookup LookupRepository { get; private set; }

        protected IDateTimeProvider DateProvider { get; private set; }

        #endregion Properties

        #region Setup Mapper

        protected IMapper Mapper { get; private set; }

        protected IMapper ApiMapper { get; private set; }

        #endregion Setup Mapper

        protected async Task<Author> GetTestAuthor(string accountName = null)
        {
            AuthorDao authorDao;
            if (string.IsNullOrEmpty(accountName))
            {
                var authorDaosResult = await AuthorRepository.GetEntitiesAsync();
                authorDao = authorDaosResult.Value.FirstOrDefault();
                if (authorDao == null)
                {
                    return null;
                }
            }
            else
            {
                var authorDaosResult = await AuthorRepository.GetEntitiesAsync(a => a.AccountName == accountName);
                authorDao = authorDaosResult.Value.FirstOrDefault();
                if (authorDao == null)
                {
                    return null;
                }
            }

            var author = Mapper.Map<Author>(authorDao);
            return author;
        }

        protected async Task<NoteCatalog> GetTestCatalog()
        {
            var catalogDaosResult = await CatalogRepository.GetEntitiesAsync();
            var catalogDao = catalogDaosResult.Value.FirstOrDefault();
            if (catalogDao == null)
            {
                return null;
            }

            var catalog = Mapper.Map<NoteCatalog>(catalogDao);
            return catalog;
        }

        protected async Task<Tag> GetTestTag(string tagName = null)
        {
            TagDao tagDao;
            if (string.IsNullOrEmpty(tagName))
            {
                var tagDaosResult = await TagRepository.GetEntitiesAsync();
                tagDao = tagDaosResult.Value.FirstOrDefault();
                if (tagDao == null)
                {
                    return null;
                }
            }
            else
            {
                var tagDaosResult = await TagRepository.GetEntitiesAsync(t => t.Name == tagName);
                tagDao = tagDaosResult.Value.FirstOrDefault();
                if (tagDao == null)
                {
                    return null;
                }
            }

            var tag = Mapper.Map<Tag>(tagDao);
            return tag;
        }

        protected static string GetRandomString(int length)
        {
            if (length <= 0)
            {
                return string.Empty;
            }

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void SetMockEnvironment()
        {
            InsertSeedRecords();

            // set up for entity look up
            LookupRepository = GetLookupRepository();

            // Setup contact repository
            ContactRepository = GetContactRepository();

            // Setup author repository
            AuthorRepository = GetAuthorRepository();

            // Setup note repository
            NoteRepository = GetNoteRepository();

            // Setup note catalog
            CatalogRepository = GetNoteCatalogRepository();

            // Setup tag repository
            TagRepository = GetTagRepository();

            var mockDateProvider = new Mock<IDateTimeProvider>();
            mockDateProvider.Setup(t => t.UtcNow).Returns(() => CurrentTime);
            DateProvider = mockDateProvider.Object;

            // Config mapper
            Mapper = CreateMapper(cfg => cfg.AddProfile<HmmMappingProfile>());

            // Config API mapper
            ApiMapper = CreateMapper(cfg => cfg.AddProfile<ApiMappingProfile>());
        }

        private static IMapper CreateMapper(Action<IMapperConfigurationExpression> configure)
        {
            var config = new MapperConfiguration(configure);
            return config.CreateMapper();
        }

        private void InsertSeedRecords()
        {
            _contactDaos ??= [];
            _authorDaos ??= [];
            _catalogDaos ??= [];
            _noteDaos ??= [];
            _tagDaos ??= [];

            // Add default contacts
            _contactDaos.AddRange(SampleDataGenerator.GetContactDaos());

            // Add authors records
            _authorDaos.AddRange(SampleDataGenerator.GetAuthorDaos());

            // Add default note catalogs
            _catalogDaos.AddRange(SampleDataGenerator.GetCatalogDaos());

            // Add default notes
            _noteDaos.AddRange(SampleDataGenerator.GetNoteDaos());

            // Add default tags
            _tagDaos.AddRange(SampleDataGenerator.GetTagDaos());
        }

        protected void InsertTag(Tag tag)
        {
            if (tag == null)
            {
                return;
            }

            var tagDao = Mapper.Map<TagDao>(tag);
            _tagDaos.Add(tagDao);
        }

        protected void InsertAuthor(Author author)
        {
            if (author == null)
            {
                return;
            }

            var authorDao = Mapper.Map<AuthorDao>(author);
            _authorDaos.Add(authorDao);
        }

        protected void InsertNote(HmmNote note)
        {
            if (note == null)
            {
                return;
            }

            var noteDao = Mapper.Map<HmmNoteDao>(note);
            _noteDaos.Add(noteDao);
        }

        public void Dispose()
        {
            _contactDaos.Clear();
            _authorDaos.Clear();
            _catalogDaos.Clear();
            _noteDaos.Clear();
            _tagDaos.Clear();
            GC.SuppressFinalize(this);
        }

        private IEntityLookup GetLookupRepository()
        {
            try
            {
                var lookupMoc = new Mock<IEntityLookup>();
                lookupMoc.Setup(lk => lk.GetEntityAsync<ContactDao>(It.IsAny<int>())).ReturnsAsync((int id) =>
                {
                    var recFound = _contactDaos.FirstOrDefault(c => c.Id == id);
                    return recFound != null
                        ? ProcessingResult<ContactDao>.Ok(recFound)
                        : ProcessingResult<ContactDao>.NotFound($"Contact with ID {id} not found");
                });
                lookupMoc.Setup(lk => lk.GetEntityAsync<AuthorDao>(It.IsAny<int>())).ReturnsAsync((int id) =>
                {
                    var recFound = _authorDaos.FirstOrDefault(c => c.Id == id);
                    return recFound != null
                        ? ProcessingResult<AuthorDao>.Ok(recFound)
                        : ProcessingResult<AuthorDao>.NotFound($"Author with ID {id} not found");
                });
                lookupMoc.Setup(lk =>
                        lk.GetEntitiesAsync(It.IsAny<Expression<Func<AuthorDao, bool>>>(),
                            It.IsAny<ResourceCollectionParameters>()))
                    .ReturnsAsync(() => ProcessingResult<PageList<AuthorDao>>.Ok(
                        PageList<AuthorDao>.Create(_authorDaos.AsQueryable(), PageIdx, PageSize)));

                lookupMoc.Setup(lk => lk.GetEntityAsync<NoteCatalogDao>(It.IsAny<int>())).ReturnsAsync((int id) =>
                {
                    var recFound = _catalogDaos.FirstOrDefault(c => c.Id == id);
                    return recFound != null
                        ? ProcessingResult<NoteCatalogDao>.Ok(recFound)
                        : ProcessingResult<NoteCatalogDao>.NotFound($"NoteCatalog with ID {id} not found");
                });

                lookupMoc.Setup(lk => lk.GetEntityAsync<TagDao>(It.IsAny<int>())).ReturnsAsync((int id) =>
                {
                    var recFound = _tagDaos.FirstOrDefault(c => c.Id == id);
                    return recFound != null
                        ? ProcessingResult<TagDao>.Ok(recFound)
                        : ProcessingResult<TagDao>.NotFound($"Tag with ID {id} not found");
                });

                lookupMoc.Setup(lk => lk.GetEntitiesAsync(It.IsAny<Expression<Func<NoteCatalogDao, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                    .ReturnsAsync(() => ProcessingResult<PageList<NoteCatalogDao>>.Ok(
                        PageList<NoteCatalogDao>.Create(_catalogDaos.AsQueryable(), PageIdx, PageSize)));

                lookupMoc.Setup(lk => lk.GetEntityAsync<HmmNoteDao>(It.IsAny<int>()))
                    .ReturnsAsync((int id) =>
                    {
                        var rec = _noteDaos.FirstOrDefault(n => n.Id == id);
                        if (rec == null)
                        {
                            return ProcessingResult<HmmNoteDao>.EmptyOk($"Note with ID {id} not found");
                        }

                        switch (rec)
                        {
                            case { Tags: not null }:
                                {
                                    foreach (var tag in rec.Tags)
                                    {
                                        tag.Tag = _tagDaos.FirstOrDefault(t => t.Id == tag.TagId);
                                        tag.Note = _noteDaos.FirstOrDefault(n => n.Id == tag.NoteId);
                                    }

                                    break;
                                }
                        }

                        return ProcessingResult<HmmNoteDao>.Ok(rec);
                    });

                lookupMoc.Setup(lk =>
                        lk.GetEntitiesAsync(It.IsAny<Expression<Func<HmmNoteDao, bool>>>(),
                            It.IsAny<ResourceCollectionParameters>()))
                    .ReturnsAsync(() => ProcessingResult<PageList<HmmNoteDao>>.Ok(
                        PageList<HmmNoteDao>.Create(_noteDaos.AsQueryable(), PageIdx, PageSize)));

                lookupMoc.Setup(lk =>
                        lk.GetEntitiesAsync(It.IsAny<Expression<Func<TagDao, bool>>>(),
                            It.IsAny<ResourceCollectionParameters>()))
                    .ReturnsAsync((Expression<Func<TagDao, bool>> query, ResourceCollectionParameters para) =>
                    {
                        var filteredTags = _tagDaos.AsQueryable().Where(query).Select(t => t);
                        if (filteredTags.Any())
                        {
                            return ProcessingResult<PageList<TagDao>>.Ok(
                                PageList<TagDao>.Create(filteredTags, PageIdx, PageSize));
                        }
                        else
                        {
                            return ProcessingResult<PageList<TagDao>>.EmptyOk("No tags found matching the criteria");
                        }
                    });
                return lookupMoc.Object;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private IRepository<ContactDao> GetContactRepository()
        {
            var mockContacts = new Mock<IRepository<ContactDao>>();
            var nextId = _contactDaos.Count + 1;
            mockContacts.Setup(a => a.AddAsync(It.IsAny<ContactDao>())).ReturnsAsync((ContactDao contact) =>
            {
                var savedContact = contact.Clone();
                savedContact.Id = nextId++;
                _contactDaos.Add(contact);
                contact = savedContact.Clone(contact);
                return ProcessingResult<ContactDao>.Ok(contact);
            });
            mockContacts.Setup(a => a.UpdateAsync(It.IsAny<ContactDao>())).ReturnsAsync((ContactDao contact) =>
            {
                var oldContact = _contactDaos.FirstOrDefault(a => a.Id == contact.Id);
                if (oldContact == null)
                {
                    return ProcessingResult<ContactDao>.NotFound($"Contact with ID {contact.Id} not found");
                }

                oldContact = contact.Clone(oldContact);
                return ProcessingResult<ContactDao>.Ok(oldContact.Clone());
            });
            mockContacts
                .Setup(a => a.GetEntitiesAsync(It.IsAny<Expression<Func<ContactDao, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>())).ReturnsAsync(
                    (Expression<Func<ContactDao, bool>> query, ResourceCollectionParameters resourceCollectionParameters) =>
                        query == null
                            ? ProcessingResult<PageList<ContactDao>>.Ok(PageList<ContactDao>.Create(_contactDaos.AsQueryable(), PageIdx, PageSize))
                            : ProcessingResult<PageList<ContactDao>>.Ok(PageList<ContactDao>.Create(_contactDaos.AsQueryable().Where(query), PageIdx, PageSize)));
            mockContacts.Setup(a => a.GetEntityAsync(It.IsAny<int>())).ReturnsAsync((int id) =>
            {
                var contact = _contactDaos.FirstOrDefault(a => a.Id == id);
                return contact != null
                    ? ProcessingResult<ContactDao>.Ok(contact)
                    : ProcessingResult<ContactDao>.NotFound($"Contact with ID {id} not found");
            });
            return mockContacts.Object;
        }

        private IRepository<AuthorDao> GetAuthorRepository()
        {
            var mockAuthors = new Mock<IRepository<AuthorDao>>();
            var nextId = _authorDaos.Count + 1;

            mockAuthors.Setup(a => a.AddAsync(It.IsAny<AuthorDao>())).ReturnsAsync((AuthorDao author) =>
            {
                var userNames = _authorDaos.Select(a => a.AccountName).ToList();
                if (userNames.Contains(author.AccountName))
                {
                    return ProcessingResult<AuthorDao>.Invalid("Author account name already exists");
                }
                var savedAuthor = author.Clone();
                savedAuthor.Id = nextId++;
                _authorDaos.Add(savedAuthor);
                author = savedAuthor.Clone(author);

                // Check and add new contact if needed
                if (author.ContactInfo == null)
                {
                    return ProcessingResult<AuthorDao>.Ok(author);
                }

                if (author.ContactInfo.Id <= 0)
                {
                    author.ContactInfo.Id = _contactDaos.Count + 1;
                    _contactDaos.Add(author.ContactInfo);
                }
                else
                {
                    if (_contactDaos.All(c => c.Id != author.ContactInfo.Id))
                    {
                        author.ContactInfo.Id = _contactDaos.Count + 1;
                        _contactDaos.Add(author.ContactInfo);
                    }
                }

                return ProcessingResult<AuthorDao>.Ok(author);
            });
            mockAuthors.Setup(a => a.UpdateAsync(It.IsAny<AuthorDao>())).ReturnsAsync((AuthorDao author) =>
            {
                var oldAuthor = _authorDaos.FirstOrDefault(a => a.Id == author.Id);
                if (oldAuthor == null)
                {
                    return ProcessingResult<AuthorDao>.NotFound($"Author with ID {author.Id} not found");
                }

                oldAuthor = author.Clone(oldAuthor);
                return ProcessingResult<AuthorDao>.Ok(oldAuthor.Clone());
            });
            mockAuthors
                .Setup(a => a.GetEntitiesAsync(It.IsAny<Expression<Func<AuthorDao, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>())).ReturnsAsync(
                    (Expression<Func<AuthorDao, bool>> query, ResourceCollectionParameters resourceCollectionParameters) =>
                        query == null
                            ? ProcessingResult<PageList<AuthorDao>>.Ok(PageList<AuthorDao>.Create(_authorDaos.AsQueryable(), PageIdx, PageSize))
                            : ProcessingResult<PageList<AuthorDao>>.Ok(PageList<AuthorDao>.Create(_authorDaos.AsQueryable().Where(query), PageIdx, PageSize)));
            mockAuthors.Setup(a => a.GetEntityAsync(It.IsAny<int>())).ReturnsAsync((int id) =>
            {
                var author = _authorDaos.FirstOrDefault(a => a.Id == id);
                return author != null
                    ? ProcessingResult<AuthorDao>.Ok(author)
                    : ProcessingResult<AuthorDao>.NotFound($"Author with ID {id} not found");
            });
            return mockAuthors.Object;
        }

        private IRepository<NoteCatalogDao> GetNoteCatalogRepository()
        {
            var mockCatalogs = new Mock<IRepository<NoteCatalogDao>>();
            mockCatalogs.Setup(c => c.AddAsync(It.IsAny<NoteCatalogDao>())).ReturnsAsync((NoteCatalogDao catalog) =>
            {
                var savedCat = catalog.Clone();
                savedCat.Id += _catalogDaos.Count + 1;
                _catalogDaos.Add(savedCat);
                catalog = savedCat.Clone();
                return ProcessingResult<NoteCatalogDao>.Ok(catalog);
            });
            mockCatalogs.Setup(c => c.UpdateAsync(It.IsAny<NoteCatalogDao>())).ReturnsAsync((NoteCatalogDao cat) =>
            {
                var oldCat = _catalogDaos.FirstOrDefault(c => c.Id == cat.Id);
                if (oldCat == null)
                {
                    return ProcessingResult<NoteCatalogDao>.NotFound($"NoteCatalog with ID {cat.Id} not found");
                }

                oldCat = cat.Clone(oldCat);
                return ProcessingResult<NoteCatalogDao>.Ok(oldCat);
            });
            mockCatalogs
                .Setup(a => a.GetEntityAsync(It.IsAny<int>())).ReturnsAsync((int id) =>
                {
                    var savedCat = _catalogDaos.FirstOrDefault(n => n.Id == id);
                    var backCat = savedCat?.Clone();
                    return backCat != null
                        ? ProcessingResult<NoteCatalogDao>.Ok(backCat)
                        : ProcessingResult<NoteCatalogDao>.NotFound($"NoteCatalog with ID {id} not found");
                });
            mockCatalogs
                .Setup(c => c.GetEntitiesAsync(It.IsAny<Expression<Func<NoteCatalogDao, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>())).ReturnsAsync(
                    (Expression<Func<NoteCatalogDao, bool>> query,
                        ResourceCollectionParameters resourceCollectionParameters) => query == null
                        ? ProcessingResult<PageList<NoteCatalogDao>>.Ok(PageList<NoteCatalogDao>.Create(_catalogDaos.AsQueryable(), PageIdx, PageSize))
                        : ProcessingResult<PageList<NoteCatalogDao>>.Ok(PageList<NoteCatalogDao>.Create(_catalogDaos.AsQueryable().Where(query), PageIdx, PageSize)));

            return mockCatalogs.Object;
        }

        private ICompositeEntityRepository<TagDao, HmmNoteDao> GetTagRepository()
        {
            var mockTags = new Mock<ICompositeEntityRepository<TagDao, HmmNoteDao>>();
            mockTags.Setup(c => c.AddAsync(It.IsAny<TagDao>())).ReturnsAsync((TagDao tag) =>
            {
                var savedTag = tag.Clone();
                savedTag.Id += _tagDaos.Count + 1;
                _tagDaos.Add(savedTag);
                tag = savedTag.Clone();
                return ProcessingResult<TagDao>.Ok(tag);
            });
            mockTags.Setup(c => c.UpdateAsync(It.IsAny<TagDao>())).ReturnsAsync((TagDao tag) =>
            {
                var oldTag = _tagDaos.FirstOrDefault(c => c.Id == tag.Id);
                if (oldTag == null)
                {
                    return ProcessingResult<TagDao>.NotFound($"Tag with ID {tag.Id} not found");
                }

                oldTag = tag.Clone(oldTag);
                return ProcessingResult<TagDao>.Ok(oldTag);
            });
            mockTags
                .Setup(a => a.GetEntityAsync(It.IsAny<int>())).ReturnsAsync((int id) =>
                {
                    var savedTag = _tagDaos.FirstOrDefault(t => t.Id == id);
                    var backTag = savedTag?.Clone();
                    return backTag != null
                        ? ProcessingResult<TagDao>.Ok(backTag)
                        : ProcessingResult<TagDao>.NotFound($"Tag with ID {id} not found");
                });
            mockTags
                .Setup(c => c.GetEntitiesAsync(It.IsAny<Expression<Func<TagDao, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>())).ReturnsAsync(
                    (Expression<Func<TagDao, bool>> query,
                        ResourceCollectionParameters resourceCollectionParameters) => query == null
                        ? ProcessingResult<PageList<TagDao>>.Ok(PageList<TagDao>.Create(_tagDaos.Where(t => t.IsActivated).AsQueryable(), PageIdx, PageSize))
                        : ProcessingResult<PageList<TagDao>>.Ok(PageList<TagDao>.Create(_tagDaos.AsQueryable().Where(query),
                            PageIdx, PageSize)));

            return mockTags.Object;
        }

        private IVersionRepository<HmmNoteDao> GetNoteRepository()
        {
            var mockNotes = new Mock<IVersionRepository<HmmNoteDao>>();
            mockNotes.Setup(a => a.AddAsync(It.IsAny<HmmNoteDao>())).ReturnsAsync((HmmNoteDao note) =>
            {
                var savedNote = note.Clone();
                savedNote.Id = _noteDaos.Count + 1;
                savedNote.Version = Guid.NewGuid().ToByteArray();
                _noteDaos.Add(savedNote);
                note = savedNote.Clone(note);
                return ProcessingResult<HmmNoteDao>.Ok(note);
            });
            mockNotes.Setup(a => a.UpdateAsync(It.IsAny<HmmNoteDao>())).ReturnsAsync((HmmNoteDao note) =>
            {
                var oldNote = _noteDaos.FirstOrDefault(n => n.Id == note.Id);
                if (oldNote == null)
                {
                    return ProcessingResult<HmmNoteDao>.NotFound($"Note with ID {note.Id} not found");
                }

                oldNote = note.Clone(oldNote);
                oldNote.Version = Guid.NewGuid().ToByteArray();
                var ret = oldNote.Clone();
                foreach (var tag in ret.Tags)
                {
                    tag.Note = _noteDaos.Where(n => n.Id == tag.NoteId).Select(n => n).FirstOrDefault();
                    tag.Tag = _tagDaos.Where(t => t.Id == tag.TagId).Select(t => t).FirstOrDefault();
                }
                return ProcessingResult<HmmNoteDao>.Ok(ret);
            });
            mockNotes
                .Setup(a => a.GetEntityAsync(It.IsAny<int>())).ReturnsAsync((int id) =>
                {
                    var savedNote = _noteDaos.FirstOrDefault(n => n.Id == id);
                    var backNote = savedNote?.Clone();
                    if (backNote == null)
                    {
                        return ProcessingResult<HmmNoteDao>.NotFound($"Note with ID {id} not found");
                    }

                    switch (backNote)
                    {
                        case { Tags: not null }:
                            {
                                foreach (var tag in backNote.Tags)
                                {
                                    tag.Tag = _tagDaos.FirstOrDefault(t => t.Id == tag.TagId);
                                    tag.Note = _noteDaos.FirstOrDefault(n => n.Id == tag.NoteId);
                                }

                                break;
                            }
                    }
                    return ProcessingResult<HmmNoteDao>.Ok(backNote);
                });
            mockNotes
                .Setup(a => a.GetEntitiesAsync(It.IsAny<Expression<Func<HmmNoteDao, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>())).ReturnsAsync((Expression<Func<HmmNoteDao, bool>> query,
                        ResourceCollectionParameters resourceCollectionParameters) =>
                    query == null
                        ? ProcessingResult<PageList<HmmNoteDao>>.Ok(PageList<HmmNoteDao>.Create(_noteDaos.AsQueryable(), PageIdx, PageSize))
                        : ProcessingResult<PageList<HmmNoteDao>>.Ok(PageList<HmmNoteDao>.Create(_noteDaos.AsQueryable().Where(query), PageIdx, PageSize)));
            return mockNotes.Object;
        }

        #region Reset data

        protected void ResetDataSource(ElementType element)
        {
            switch (element)
            {
                case ElementType.Author:
                    _authorDaos.Clear();
                    break;

                case ElementType.Tag:
                    _tagDaos.Clear();
                    break;

                case ElementType.NoteCatalog:
                    _catalogDaos.Clear();
                    break;

                case ElementType.Contact:
                    _contactDaos.Clear();
                    break;

                case ElementType.Note:
                    _noteDaos.Clear();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(element), element, null);
            }
        }

        protected enum ElementType
        {
            Author,
            NoteCatalog,
            Contact,
            Tag,
            Note
        }

        #endregion Reset data
    }
}