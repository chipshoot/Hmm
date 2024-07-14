namespace Hmm.Utility.Dal.DataEntity
{
    /// <summary>
    /// The entity has is activated flag record, this makes the system get default record when no record id provided
    /// Note: the table can only contain one default record.
    /// </summary>
    public class HasActivateFlagEntity : Entity
    {
        public bool IsActivated { get; set; }
    }
}