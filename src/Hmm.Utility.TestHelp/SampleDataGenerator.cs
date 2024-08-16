// Ignore Spelling: Dao Daos

using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Utility.TestHelp;

public static class SampleDataGenerator
{
    public static Contact GetContact()
    {
        var contact = new Contact
        {
            FirstName = "John",
            LastName = "Doe",
            Emails = new List<Email>
            {
                new() { Address = "fchy@yahoo.com", Type = EmailType.Personal, IsPrimary = true },
                new() { Address = "fchy5979@gamil.com", Type = EmailType.Personal, IsPrimary = false },
                new() { Address = "fchy@outlook.com", Type = EmailType.Work, IsPrimary = false }
            },
            Phones = new List<Phone>
            {
                new() { Number = "123-456-7890", Type = TelephoneType.Home, IsPrimary = true },
                new() { Number = "098-765-4321", Type = TelephoneType.Work,  IsPrimary = false }
            },
            Addresses = new List<AddressInfo>
            {
                new() { Address = "123 Main St", City = "Springfield", State = "IL", PostalCode = "12345", Country = "USA", Type = AddressType.Home, IsPrimary = true},
                new() { Address = "456 Elm St", City = "Springfield", State = "IL", PostalCode = "12345", Country = "USA", Type = AddressType.Work, IsPrimary = false}
            },
            IsActivated = true,
            Description = "This is a testing contact to be mapped to DAO object"
        };

        return contact;
    }

    public static ContactDao GetContactDao()
    {
        var contactDao = new ContactDao
        {
            Id = 100,
            Contact = """
                      { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": 0, "IsPrimary": true }, { "Address": "fchy5979@gamil.com", "Type": 0, "IsPrimary": false }, { "Address": "fchy@outlook.com", "Type": 1, "IsPrimary": false } ], "Phones": [ { "Number": "123-456-7890", "Type": 0, "IsPrimary": true }, { "Number": "098-765-4321", "Type": 2, "IsPrimary": false } ], "Addresses": [ { "Address": "123 Main St", "City": "Springfield", "State": "IL", "PostalCode": "12345", "Country": "USA", "Type": 0, "IsPrimary": true }, { "Address": "456 Elm St", "City": "Springfield", "State": "IL", "PostalCode": "12345", "Country": "USA", "Type": 1, "IsPrimary": false } ] }
                      """,
            Description = "testing contact",
            IsActivated = true
        };

        return contactDao;
    }

    public static Author GetAuthor()
    {
        var author = new Author
        {
            AccountName = "fchy",
            ContactInfo = GetContact(),
            Role = Core.Map.DomainEntity.AuthorRoleType.Author,
            IsActivated = true
        };

        return author;
    }

    public static AuthorDao GetAuthorDao()
    {
        var authorDao = new AuthorDao
        {
            Id = 100,
            AccountName = "fchy",
            ContactInfo = GetContactDao(),
            Role = Core.Map.DbEntity.AuthorRoleType.Author,
            Description = "Testing Author",
            IsActivated = true
        };

        return authorDao;
    }

    public static List<AuthorDao> GetAuthorDaos()
    {
        var authors = new List<AuthorDao>
        {
            new AuthorDao
            {
                Id = 100,
                AccountName = "fchy",
                ContactInfo = GetContactDao(),
                Role = Core.Map.DbEntity.AuthorRoleType.Author,
                Description = "Testing Author",
                IsActivated = true
            },

            new AuthorDao
            {
                Id = 101,
                AccountName = "jfang",
                ContactInfo = GetContactDao(),
                Role = Core.Map.DbEntity.AuthorRoleType.Author,
                Description = "Testing Author2",
                IsActivated = true
            },
        };

        return authors;
    }

    public static NoteCatalog GetCatalog()
    {
        var catalog = new NoteCatalog
        {
            Name = "DiaryNote",
            Schema = "",
            Type = Core.Map.DomainEntity.NoteContentFormatType.PlainText,
            IsDefault = true,
            Description = "This is a testing catalog"
        };

        return catalog;
    }

    public static NoteCatalogDao GetCatalogDao()
    {
        var catalogDao = new NoteCatalogDao
        {
            Id = 100,
            Name = "Diary",
            FormatType = Core.Map.DbEntity.NoteContentFormatType.Markdown,
            Description = "Testing note catalog",
            Schema = "",
            IsDefault = false
        };

        return catalogDao;
    }

    public static List<NoteCatalogDao> GetCatalogDaos()
    {
        var catalogs = new List<NoteCatalogDao>
        {
            new()
            {
                Id = 100,
                Name = "Diary",
                FormatType = Core.Map.DbEntity.NoteContentFormatType.Markdown,
                Description = "Testing note catalog",
                Schema = "",
                IsDefault = false
            },
            new()
            {
                Id = 101,
                Name = "GasLog",
                FormatType = Core.Map.DbEntity.NoteContentFormatType.Xml,
                Description = "Testing gas log catalog",
                Schema = "",
                IsDefault = false
            },
            new()
            {
                Id = 102,
                Name = "SystemLog",
                FormatType = Core.Map.DbEntity.NoteContentFormatType.PlainText,
                Description = "Testing system log catalog",
                Schema = "",
                IsDefault = false
            }
        };

        return catalogs;
    }

    public static HmmNote GetNote()
    {
        var b = new byte[8];
        var rnd = new Random();
        rnd.NextBytes(b);

        var note = new HmmNote
        {
            Id = 100,
            Subject = "SystemConfiguration",
            Content = "This is a test note",
            Author = GetAuthor(),
            Catalog = GetCatalog(),
            CreateDate = DateTime.Now,
            LastModifiedDate = DateTime.Now,
            IsDeleted = false,
            Description = "This is test note",
            Version = b
        };

        return note;
    }

    public static HmmNoteDao GetNoteDao()
    {
        var b = new byte[8];
        var rnd = new Random();
        rnd.NextBytes(b);
        var noteDao = new HmmNoteDao
        {
            Id = 100,
            Subject = "ComputerPeripheral",
            Content = "This is a test note",
            Author = GetAuthorDao(),
            Catalog = GetCatalogDao(),
            CreateDate = DateTime.Now,
            LastModifiedDate = DateTime.Now,
            IsDeleted = false,
            Description = "This is test note",
            Version = b
        };

        return noteDao;
    }

    public static Tag GetTag()
    {
        var tag = new Tag
        {
            Id = 100,
            Name = "SystemConfiguration",
            IsActivated = true,
        };

        return tag;
    }

    public static TagDao GetTagDao()
    {
        var tagDao = new TagDao
        {
            Id = 100,
            Name = "ComputerPeripheral",
            IsActivated = true,
            Description = "This is tag for testing"
        };

        return tagDao;
    }

    public static List<TagDao> GetTagDaos()
    {
        var tags = new List<TagDao>
        {
            new ()
            {
                Id = 100,
                Name = "ComputerPeripheral",
                IsActivated = true,
                Description = "This is tag for testing"
            },
            new ()
            {
                Id = 101,
                Name = "SystemConfiguration",
                IsActivated = true,
                Description = "This is tag for testing"
            },
            new ()
            {
                Id = 102,
                Name = "GasLog",
                IsActivated = true,
                Description = "This is tag for testing"
            },
            new ()
            {
                Id = 103,
                Name = "Diary",
                IsActivated = true,
                Description = "This is tag for testing"
            }
        };

        return tags;
    }

    // Extension methods

    public static HmmNoteDao Clone(this HmmNoteDao source, HmmNoteDao targetNote = null)
    {
        if (source == null)
        {
            return null;
        }

        if (targetNote == null)
        {
            var tags = source.Tags.Select(tag => tag.Clone()).ToList();

            var target = new HmmNoteDao
            {
                Id = source.Id,
                Subject = source.Subject,
                Content = source.Content,
                Description = source.Description,
                Author = source.Author.Clone(),
                Catalog = source.Catalog.Clone(),
                CreateDate = source.CreateDate,
                IsDeleted = source.IsDeleted,
                LastModifiedDate = source.LastModifiedDate,
                Tags = tags,
                Version = source.Version
            };
            return target;
        }
        else
        {
            targetNote.Id = source.Id;
            targetNote.Subject = source.Subject;
            targetNote.Content = source.Content;
            targetNote.Description = source.Description;
            targetNote.Author = source.Author.Clone();
            targetNote.Catalog = source.Catalog.Clone();
            targetNote.CreateDate = source.CreateDate;
            targetNote.IsDeleted = source.IsDeleted;
            targetNote.LastModifiedDate = source.LastModifiedDate;
            targetNote.Tags = source.Tags.Select(tag => tag.Clone()).ToList();
            targetNote.Version = source.Version;

            return targetNote;
        }
    }

    public static ContactDao Clone(this ContactDao source, ContactDao targetContact = null)
    {
        if (source == null)
        {
            return null;
        }

        if (targetContact == null)
        {
            var target = new ContactDao
            {
                Id = source.Id,
                Contact = source.Contact,
                Description = source.Description,
                IsActivated = source.IsActivated
            };
            return target;
        }
        else
        {
            targetContact.Id = source.Id;
            targetContact.Contact = source.Contact;
            targetContact.Description = source.Description;
            targetContact.IsActivated = source.IsActivated;

            return targetContact;
        }
    }

    public static AuthorDao Clone(this AuthorDao source, AuthorDao targetAuthor = null)
    {
        if (source == null)
        {
            return null;
        }

        if (targetAuthor == null)
        {
            var target = new AuthorDao
            {
                Id = source.Id,
                AccountName = source.AccountName,
                ContactInfo = source.ContactInfo,
                Role = source.Role,
                Description = source.Description,
                IsActivated = source.IsActivated
            };
            return target;
        }
        else
        {
            targetAuthor.Id = source.Id;
            targetAuthor.AccountName = source.AccountName;
            targetAuthor.ContactInfo = source.ContactInfo;
            targetAuthor.Role = source.Role;
            targetAuthor.Description = source.Description;
            targetAuthor.IsActivated = source.IsActivated;

            return targetAuthor;
        }
    }

    public static NoteCatalogDao Clone(this NoteCatalogDao source, NoteCatalogDao targetCatalog = null)
    {
        if (source == null)
        {
            return null;
        }

        if (targetCatalog == null)
        {
            var target = new NoteCatalogDao
            {
                Id = source.Id,
                Name = source.Name,
                Schema = source.Schema
            };
            return target;
        }

        targetCatalog.Id = source.Id;
        targetCatalog.Name = source.Name;
        targetCatalog.Schema = source.Schema;

        return targetCatalog;
    }

    public static TagDao Clone(this TagDao source, TagDao targetTag = null)
    {
        if (source == null)
        {
            return null;
        }

        if (targetTag == null)
        {
            var target = new TagDao
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                IsActivated = source.IsActivated,
                Notes = source.Notes
            };
            return target;
        }

        targetTag.Id = source.Id;
        targetTag.Name = source.Name;
        targetTag.Description = source.Description;
        targetTag.IsActivated = source.IsActivated;
        targetTag.Notes = source.Notes;

        return targetTag;
    }

    private static NoteTagRefDao Clone(this NoteTagRefDao source, NoteTagRefDao targetNoteTag = null)
    {
        if (source == null)
        {
            return null;
        }

        if (targetNoteTag == null)
        {
            var target = new NoteTagRefDao
            {
                NoteId = source.NoteId,
                TagId = source.TagId,
                Note = source.Note,
                Tag = source.Tag
            };
            return target;
        }

        targetNoteTag.Note = source.Note;
        targetNoteTag.Tag = source.Tag;

        return targetNoteTag;
    }
}