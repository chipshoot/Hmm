namespace Hmm.ServiceApi.DtoEntity.HmmNote;

public class ApiPhone
{
    public string Number { get; set; }
    public TelephoneType Type { get; set; }
    public bool IsPrimary { get; set; }
}

public enum TelephoneType
{
    Home,
    Mobile,
    Work,
    Other
}