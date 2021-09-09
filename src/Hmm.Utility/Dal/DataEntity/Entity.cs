namespace Hmm.Utility.Dal.DataEntity
{
    /// <summary>
    /// The base class of domain entity who use integer as its identity
    /// </summary>
    public class Entity : AbstractEntity<int>
    {
        public string Description { get; set; }
    }
}