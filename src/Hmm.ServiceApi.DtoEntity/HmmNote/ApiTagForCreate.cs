namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Data required to create a new tag.
    /// </summary>
    public class ApiTagForCreate
    {
        public string Name { get; set; }

        public string Description { get; set; }
    }
}