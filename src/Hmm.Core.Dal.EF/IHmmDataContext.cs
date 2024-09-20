using Hmm.Core.Map.DbEntity;
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
    }
}