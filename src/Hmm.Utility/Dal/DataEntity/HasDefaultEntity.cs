namespace Hmm.Utility.Dal.DataEntity
{
    /// <summary>
    /// The entity has default record, this makes the system get default record when no record id provided
    /// Note: the table can only contains one default record.
    /// </summary>
    public class HasDefaultEntity : Entity
    {
        public bool IsDefault { get; set; }
    }
}