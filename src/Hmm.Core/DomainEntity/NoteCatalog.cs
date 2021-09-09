using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.DomainEntity
{
    public class NoteCatalog : HasDefaultEntity
    {
        public string Name { get; set; }

        public Subsystem Subsystem { get; set; }

        public NoteRender Render { get; set; }

        public string Schema { get; set; }
    }
}