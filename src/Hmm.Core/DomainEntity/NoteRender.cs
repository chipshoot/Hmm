using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.DomainEntity
{
    public class NoteRender : HasDefaultEntity
    {
        public string Name { get; set; }

        public string Namespace { get; set; }
    }
}