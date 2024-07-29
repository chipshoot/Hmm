// Ignore Spelling: Dao

using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using System;
using System.Collections.Generic;

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
            IsActivated = true
        };

        return tagDao;
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
}