namespace Hmm.ServiceApi.DtoEntity.HmmNote;

public class ApiEmail
{
    public string Address { get; set; }

    public EmailType Type { get; set; }

    public bool IsPrimary { get; set; }
}

public enum EmailType
{
    Personal,
    Work,
    Other
}
