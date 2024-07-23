namespace Hmm.Core.Map.DomainEntity;

public class AddressInfo
{
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
    public AddressType Type { get; set; }
    public bool IsPrimary { get; set; }
}

public enum AddressType
{
    Home,
    Work,
    Other
}