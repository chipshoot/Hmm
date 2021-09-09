using System;

namespace Hmm.Utility.Dal.DataEntity
{
    /// <summary>
    /// The base class of domain entity who use GUID as its identity
    /// </summary>
    public class GuidEntity : AbstractEntity<Guid>
    {
        public string Description { get; set; }
    }
}