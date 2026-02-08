namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Represents a tag in API responses.
    /// </summary>
    public class ApiTag : ApiEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}