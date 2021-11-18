using System.Threading.Tasks;
using Hmm.Core.DomainEntity;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Core.Dal.EF
{
    public interface IHmmDataContext
    {
        DbSet<HmmNote> Notes { get; set; }

        DbSet<Author> Authors { get; set; }

        DbSet<NoteRender> Renders { get; set; }

        DbSet<Subsystem> Subsystems { get; set; }

        DbSet<NoteCatalog> Catalogs { get; set; }

        void Save();

        Task SaveAsync();
    }
}