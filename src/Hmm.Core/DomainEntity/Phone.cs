namespace Hmm.Core.DomainEntity;


public class Phone
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
