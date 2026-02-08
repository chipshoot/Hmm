namespace Hmm.ServiceApi.DtoEntity.HmmNote;

/// <summary>
/// Represents a phone number.
/// </summary>
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