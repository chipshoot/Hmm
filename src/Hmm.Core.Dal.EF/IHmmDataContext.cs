using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.DataEntity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF
{
    public interface IHmmDataContext
    {
        DbSet<HmmNoteDao> Notes { get; set; }

        DbSet<AuthorDao> Authors { get; set; }
        DbSet<ContactDao> Contacts { get; set; }

        DbSet<NoteCatalogDao> Catalogs { get; set; }

        DbSet<TagDao> Tags { get; set; }

        DbSet<NoteTagRefDao> NoteTagRefs { get; set; }

        void Save();

        Task SaveAsync();

        /// <summary>
        /// Gets the default entity for any type that extends HasDefaultEntity.
        /// This enables Open/Closed principle compliance by avoiding type-specific checks in repositories.
        /// </summary>
        /// <typeparam name="T">Entity type that extends HasDefaultEntity</typeparam>
        /// <returns>The default entity or null if not found</returns>
        Task<T> GetDefaultEntityAsync<T>() where T : HasDefaultEntity;
    }
}