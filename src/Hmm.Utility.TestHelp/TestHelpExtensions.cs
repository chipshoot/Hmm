using Hmm.Core.DomainEntity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Hmm.Utility.TestHelp
{
    public static class TestHelpExtensions
    {
        public static HmmNote Clone(this HmmNote source, HmmNote targetNote = null)
        {
            if (source == null)
            {
                return null;
            }

            if (targetNote == null)
            {
                var target = new HmmNote
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

        public static Author Clone(this Author source, Author targetAuthor = null)
        {
            if (source == null)
            {
                return null;
            }

            if (targetAuthor == null)
            {
                var target = new Author
                {
                    Id = source.Id,
                    AccountName = source.AccountName,
                    Role = source.Role,
                    IsActivated = source.IsActivated
                };
                return target;
            }
            else
            {
                targetAuthor.Id = source.Id;
                targetAuthor.AccountName = source.AccountName;
                targetAuthor.Role = source.Role;
                targetAuthor.IsActivated = source.IsActivated;

                return targetAuthor;
            }
        }

        public static NoteCatalog Clone(this NoteCatalog source, NoteCatalog targetCatalog = null)
        {
            if (source == null)
            {
                return null;
            }

            if (targetCatalog == null)
            {
                var target = new NoteCatalog
                {
                    Id = source.Id,
                    Name = source.Name,
                    Subsystem = source.Subsystem,
                    Render = source.Render.Clone(),
                    Schema = source.Schema
                };
                return target;
            }

            targetCatalog.Id = source.Id;
            targetCatalog.Name = source.Name;
            targetCatalog.Subsystem = source.Subsystem;
            targetCatalog.Render = source.Render.Clone();
            targetCatalog.Schema = source.Schema;

            return targetCatalog;
        }

        public static NoteRender Clone(this NoteRender source, NoteRender targetRender = null)
        {
            if (source == null)
            {
                return null;
            }

            if (targetRender == null)
            {
                var target = new NoteRender
                {
                    Id = source.Id,
                    Name = source.Name,
                    Namespace = source.Namespace
                };
                return target;
            }

            targetRender.Id = source.Id;
            targetRender.Name = source.Name;
            targetRender.Namespace = source.Namespace;

            return targetRender;
        }

        public static Subsystem Clone(this Subsystem source, Subsystem targetSys = null)
        {
            if (source == null)
            {
                return null;
            }

            if (targetSys == null)
            {
                var target = new Subsystem
                {
                    Id = source.Id,
                    Name = source.Name,
                    Description = source.Description
                };
                return target;
            }

            targetSys.Id = source.Id;
            targetSys.Name = source.Name;
            targetSys.Description = source.Description;

            return targetSys;
        }

        public static void Reset(this DbContext context)
        {
            var entries = context.ChangeTracker
                .Entries()
                .Where(e => e.State != EntityState.Unchanged)
                .ToArray();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.State = EntityState.Unchanged;
                        break;

                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;

                    case EntityState.Deleted:
                        entry.Reload();
                        break;
                }
            }
        }

        public static void NoTracking(this DbContext context)
        {
            var entries = context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToArray();

            foreach (var entry in entries)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}