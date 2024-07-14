using Hmm.Core.Dal.EF.DbEntity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AuthorDao = Hmm.Core.Dal.EF.DbEntity.AuthorDao;
using ContactDao = Hmm.Core.Dal.EF.DbEntity.ContactDao;

namespace Hmm.Core.Dal.EF
{
    public interface IHmmDataContext
    {
        DbSet<HmmNoteDao> Notes { get; set; }

        DbSet<AuthorDao> Authors { get; set; }
        DbSet<ContactDao> Contacts { get; set; }

        DbSet<NoteCatalogDao> Catalogs { get; set; }

        void Save();

        Task SaveAsync();
    }
}